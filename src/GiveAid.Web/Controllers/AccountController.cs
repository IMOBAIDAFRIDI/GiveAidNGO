using System.Security.Cryptography;
using GiveAid.Web.Data;
using GiveAid.Web.Models.Entities;
using GiveAid.Web.Models.Enums;
using GiveAid.Web.Models.ViewModels.Account;
using GiveAid.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Member");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cleanEmail = model.Email.Trim().ToLowerInvariant();

        // 1. Check if user is currently locked from OTP attempts
        var activeLock = await _context.EmailVerificationOtps
            .Where(o => o.Email == cleanEmail && o.LockedUntil != null && o.LockedUntil > DateTime.UtcNow)
            .OrderByDescending(o => o.LockedUntil)
            .FirstOrDefaultAsync();

        if (activeLock != null && activeLock.LockedUntil > DateTime.UtcNow)
        {
            var lockRemainingMins = (int)Math.Ceiling((activeLock.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
            ModelState.AddModelError(string.Empty, $"Too many attempts. Registration for this email is locked for another {lockRemainingMins} minute(s). Please try again later.");
            return View(model);
        }

        // 2. Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
        if (existingUser != null)
        {
            if (existingUser.EmailConfirmed)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email address already exists. Please sign in.");
                return View(model);
            }
            else
            {
                // Account created earlier but email never confirmed: update profile details
                existingUser.FullName = model.FullName.Trim();
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.City = model.City;
                existingUser.Profession = model.Profession;
                existingUser.UpdatedAt = DateTime.UtcNow;

                // Update password if changed
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                await _userManager.ResetPasswordAsync(existingUser, token, model.Password);
                await _userManager.UpdateAsync(existingUser);
            }
        }
        else
        {
            // Create new pending user
            var newUser = new ApplicationUser
            {
                UserName = cleanEmail,
                Email = cleanEmail,
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber,
                City = model.City,
                Profession = model.Profession,
                Role = SystemRoles.Member,
                Status = "PendingVerification",
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newUser, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(newUser, SystemRoles.Member);
        }

        // 3. Invalidate previous unused OTPs
        var oldOtps = await _context.EmailVerificationOtps
            .Where(o => o.Email == cleanEmail && !o.IsUsed)
            .ToListAsync();
        foreach (var old in oldOtps)
        {
            old.IsUsed = true;
        }

        // 4. Generate 6-digit OTP code (1 minute expiry)
        var otpCode = GenerateNumericOtp();
        var otpEntry = new EmailVerificationOtp
        {
            Email = cleanEmail,
            OtpCode = otpCode,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1), // 1 Minute Expiration
            IsUsed = false,
            ResendCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _context.EmailVerificationOtps.AddAsync(otpEntry);
        await _context.SaveChangesAsync();

        // 5. Send email asynchronously in background thread so page redirects in milliseconds and email arrives in 2 seconds!
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendOtpEmailAsync(cleanEmail, model.FullName, otpCode, expiryMinutes: 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background OTP email dispatch failed for {Email}", cleanEmail);
            }
        });

        TempData["SuccessMessage"] = $"A 6-digit verification code has been dispatched to {cleanEmail}. Valid for 1 minute.";
        return RedirectToAction(nameof(VerifyEmailOtp), new { email = cleanEmail });
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmailOtp(string? email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Register));
        }

        var cleanEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(cleanEmail);
        if (user == null)
        {
            return RedirectToAction(nameof(Register));
        }

        if (user.EmailConfirmed)
        {
            TempData["SuccessMessage"] = "Your email has already been verified. You can sign in.";
            return RedirectToAction(nameof(Login));
        }

        var vm = new VerifyEmailOtpViewModel
        {
            Email = cleanEmail,
            ReturnUrl = returnUrl
        };

        await PopulateOtpViewModelMeta(vm, cleanEmail);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmailOtp(VerifyEmailOtpViewModel model)
    {
        var cleanEmail = model.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(cleanEmail);
        if (user == null)
        {
            return RedirectToAction(nameof(Register));
        }

        if (user.EmailConfirmed)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Member");
        }

        if (!ModelState.IsValid)
        {
            await PopulateOtpViewModelMeta(model, cleanEmail);
            return View(model);
        }

        var latestOtp = await _context.EmailVerificationOtps
            .Where(o => o.Email == cleanEmail && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestOtp == null)
        {
            ModelState.AddModelError(nameof(model.OtpCode), "No active verification code found. Please request a new code.");
            await PopulateOtpViewModelMeta(model, cleanEmail);
            return View(model);
        }

        // Check if currently locked
        if (latestOtp.LockedUntil != null && latestOtp.LockedUntil > DateTime.UtcNow)
        {
            var lockMins = (int)Math.Ceiling((latestOtp.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
            ModelState.AddModelError(string.Empty, $"Verification is temporarily locked. Try again in {lockMins} minute(s) (2 hours lockout).");
            await PopulateOtpViewModelMeta(model, cleanEmail);
            return View(model);
        }

        // Check 1-minute expiration
        if (latestOtp.ExpiresAt < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(model.OtpCode), "This code has expired (1 minute validity). Please click 'Resend Code' below.");
            await PopulateOtpViewModelMeta(model, cleanEmail);
            return View(model);
        }

        // Check code match
        if (!string.Equals(latestOtp.OtpCode.Trim(), model.OtpCode.Trim(), StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.OtpCode), "Incorrect code. Please double-check the 6 digits sent to your email.");
            await PopulateOtpViewModelMeta(model, cleanEmail);
            return View(model);
        }

        // Successful Verification
        latestOtp.IsUsed = true;
        user.EmailConfirmed = true;
        user.Status = UserStatuses.Active;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("User {Email} email verified via OTP.", user.Email);

        TempData["SuccessMessage"] = "Email verified successfully! Welcome to GIVE-AID Humanitarian Org.";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Member");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(string email, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(nameof(Register));
        }

        var cleanEmail = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(cleanEmail);
        if (user == null)
        {
            return RedirectToAction(nameof(Register));
        }

        if (user.EmailConfirmed)
        {
            TempData["SuccessMessage"] = "Your email has already been verified. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        // 1. Check if currently locked
        var activeLock = await _context.EmailVerificationOtps
            .Where(o => o.Email == cleanEmail && o.LockedUntil != null && o.LockedUntil > DateTime.UtcNow)
            .OrderByDescending(o => o.LockedUntil)
            .FirstOrDefaultAsync();

        if (activeLock != null && activeLock.LockedUntil > DateTime.UtcNow)
        {
            var remainingMins = (int)Math.Ceiling((activeLock.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
            TempData["ErrorMessage"] = $"Resend is locked. You can request a new code in {remainingMins} minute(s) (2 hours cooldown).";
            return RedirectToAction(nameof(VerifyEmailOtp), new { email = cleanEmail, returnUrl });
        }

        // 2. Count attempts in the last 2 hours
        var recentOtps = await _context.EmailVerificationOtps
            .Where(o => o.Email == cleanEmail && o.CreatedAt >= DateTime.UtcNow.AddHours(-2))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Maximum 2 resends allowed (total 3 sends: initial + 2 resends). 3rd attempt locks for 2 hours!
        if (recentOtps.Count >= 3)
        {
            var lockTime = DateTime.UtcNow.AddHours(2);
            var lockEntry = new EmailVerificationOtp
            {
                Email = cleanEmail,
                OtpCode = "LOCK",
                ExpiresAt = lockTime,
                IsUsed = true,
                ResendCount = 3,
                LockedUntil = lockTime,
                CreatedAt = DateTime.UtcNow
            };

            await _context.EmailVerificationOtps.AddAsync(lockEntry);
            await _context.SaveChangesAsync();

            TempData["ErrorMessage"] = "You have exhausted your 2 resend attempts. Resend is now locked for 2 hours.";
            return RedirectToAction(nameof(VerifyEmailOtp), new { email = cleanEmail, returnUrl });
        }

        // Invalidate prior unused OTPs
        foreach (var old in recentOtps.Where(o => !o.IsUsed))
        {
            old.IsUsed = true;
        }

        var resendAttempt = recentOtps.Count; // 1 for first resend, 2 for second resend
        var newOtp = GenerateNumericOtp();
        var newEntry = new EmailVerificationOtp
        {
            Email = cleanEmail,
            OtpCode = newOtp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(1), // 1 Minute Validity
            IsUsed = false,
            ResendCount = resendAttempt,
            CreatedAt = DateTime.UtcNow
        };

        await _context.EmailVerificationOtps.AddAsync(newEntry);
        await _context.SaveChangesAsync();

        // Send email in background so response redirects immediately in 0.05s
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendOtpEmailAsync(cleanEmail, user.FullName, newOtp, expiryMinutes: 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background OTP resend failed for {Email}", cleanEmail);
            }
        });

        TempData["SuccessMessage"] = $"A new verification code has been dispatched to {cleanEmail}. (Resend {resendAttempt} of 2 used)";
        return RedirectToAction(nameof(VerifyEmailOtp), new { email = cleanEmail, returnUrl });
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(SystemRoles.Admin))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return RedirectToAction("Index", "Member");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        // Verify password first
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!isPasswordValid)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // If email is not confirmed, redirect to OTP verification
        if (!user.EmailConfirmed)
        {
            TempData["ErrorMessage"] = "Your email address has not been verified yet. Please enter your verification code.";
            return RedirectToAction(nameof(VerifyEmailOtp), new { email = user.Email, returnUrl = model.ReturnUrl });
        }

        if (user.Status != UserStatuses.Active)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully.", user.Email);
            TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            if (await _userManager.IsInRoleAsync(user, SystemRoles.Admin))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Member");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["SuccessMessage"] = "You have been logged out successfully.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static string GenerateNumericOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private async Task PopulateOtpViewModelMeta(VerifyEmailOtpViewModel vm, string cleanEmail)
    {
        var latestOtp = await _context.EmailVerificationOtps
            .Where(o => o.Email == cleanEmail)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        var isLocked = latestOtp?.LockedUntil != null && latestOtp.LockedUntil > DateTime.UtcNow;
        var lockMins = isLocked ? (int)Math.Ceiling((latestOtp!.LockedUntil!.Value - DateTime.UtcNow).TotalMinutes) : 0;

        var remainingSeconds = 0;
        if (!isLocked && latestOtp != null && latestOtp.ExpiresAt > DateTime.UtcNow)
        {
            remainingSeconds = (int)Math.Max(0, (latestOtp.ExpiresAt - DateTime.UtcNow).TotalSeconds);
        }

        var recentOtps = await _context.EmailVerificationOtps
            .CountAsync(o => o.Email == cleanEmail && o.CreatedAt >= DateTime.UtcNow.AddHours(-2));

        vm.RemainingSeconds = remainingSeconds;
        vm.ResendAttemptsUsed = Math.Max(0, recentOtps - 1);
        vm.IsLocked = isLocked;
        vm.LockRemainingMinutes = lockMins;
    }
}
