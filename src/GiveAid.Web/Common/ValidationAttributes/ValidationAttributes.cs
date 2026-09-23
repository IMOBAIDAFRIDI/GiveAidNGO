using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GiveAid.Web.Common.ValidationAttributes;

/// <summary>
/// Enforces strict email formatting requiring a valid username, '@' symbol, domain name, and TLD.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class StrictEmailAddressAttribute : RegularExpressionAttribute
{
    public const string DefaultPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

    public StrictEmailAddressAttribute()
        : base(DefaultPattern)
    {
        ErrorMessage = "Please enter a valid email address with a domain (e.g. name@example.com).";
    }
}

/// <summary>
/// Enforces strong password complexity: minimum 8 characters, at least 1 uppercase, 1 lowercase, 1 digit, and 1 special symbol.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class StrongPasswordAttribute : RegularExpressionAttribute
{
    public const string DefaultPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$";

    public StrongPasswordAttribute()
        : base(DefaultPattern)
    {
        ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character (e.g. !@#$%^&*).";
    }
}

/// <summary>
/// Validates that a credit/debit card expiration date (MM/YY or MM/YYYY) is valid and strictly in the future.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class FutureCardExpiryAttribute : ValidationAttribute
{
    private static readonly Regex ExpiryRegex = new(@"^(0[1-9]|1[0-2])\/?([0-9]{2}|[0-9]{4})$", RegexOptions.Compiled);

    public FutureCardExpiryAttribute()
    {
        ErrorMessage = "The card has expired. Please enter a valid future expiration date (MM/YY).";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult("Card expiration date is required.");
        }

        var str = value.ToString()!.Trim();
        var match = ExpiryRegex.Match(str);
        if (!match.Success)
        {
            return new ValidationResult("Expiration date must be in MM/YY format with a valid month (01-12).");
        }

        if (!int.TryParse(match.Groups[1].Value, out var month) || month < 1 || month > 12)
        {
            return new ValidationResult("Expiration month must be between 01 and 12.");
        }

        var yearStr = match.Groups[2].Value;
        var year = yearStr.Length == 2 ? 2000 + int.Parse(yearStr) : int.Parse(yearStr);

        // Calculate the end of the expiration month in UTC
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var endOfExpiryMonth = new DateTime(year, month, daysInMonth, 23, 59, 59, DateTimeKind.Utc);

        if (endOfExpiryMonth < DateTime.UtcNow)
        {
            return new ValidationResult(ErrorMessage ?? "The card has expired. Please enter a valid future expiration date (MM/YY).");
        }

        // Also prevent absurd dates like 30 years in future
        if (year > DateTime.UtcNow.Year + 25)
        {
            return new ValidationResult("Expiration year is too far in the future.");
        }

        return ValidationResult.Success;
    }
}
