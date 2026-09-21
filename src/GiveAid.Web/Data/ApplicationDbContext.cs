using GiveAid.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cause> Causes => Set<Cause>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Programme> Programmes => Set<Programme>();
    public DbSet<ProgrammeInterest> ProgrammeInterests => Set<ProgrammeInterest>();
    public DbSet<AboutSection> AboutSections => Set<AboutSection>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<NGO> NGOs => Set<NGO>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
    public DbSet<Query> Queries => Set<Query>();
    public DbSet<QueryReply> QueryReplies => Set<QueryReply>();
    public DbSet<ContactInfo> ContactInfos => Set<ContactInfo>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<AdminActivityLog> AdminActivityLogs => Set<AdminActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customize ASP.NET Identity tables with clean names
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired().HasDefaultValue("Member");
            entity.Property(u => u.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Active");
            entity.Property(u => u.CreatedAt).HasColumnType("datetime2");
            entity.Property(u => u.UpdatedAt).HasColumnType("datetime2");
            entity.Property(u => u.DateOfBirth).HasColumnType("datetime2");
        });

        builder.Entity<IdentityRole<int>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        // Cause
        builder.Entity<Cause>(entity =>
        {
            entity.ToTable("Causes");
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Slug).HasMaxLength(200).IsRequired();
            entity.Property(c => c.TargetAmount).HasColumnType("decimal(12,2)");
            entity.Property(c => c.RaisedAmount).HasColumnType("decimal(12,2)");
            entity.Property(c => c.CreatedAt).HasColumnType("datetime2");
            entity.Property(c => c.UpdatedAt).HasColumnType("datetime2");
            entity.Property(c => c.DeletedAt).HasColumnType("datetime2");
            entity.HasQueryFilter(c => c.DeletedAt == null);
        });

        // Donation (Financial Integrity: no cascade delete)
        builder.Entity<Donation>(entity =>
        {
            entity.ToTable("Donations");
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.ReferenceNo).IsUnique();
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.CreatedAt);
            entity.Property(d => d.ReferenceNo).HasMaxLength(64).IsRequired();
            entity.Property(d => d.Amount).HasColumnType("decimal(12,2)").IsRequired();
            entity.Property(d => d.Currency).HasMaxLength(10).IsRequired().HasDefaultValue("USD");
            entity.Property(d => d.PaymentMethod).HasMaxLength(50).IsRequired();
            entity.Property(d => d.Status).HasMaxLength(50).IsRequired();
            entity.Property(d => d.CreatedAt).HasColumnType("datetime2");
            entity.Property(d => d.PaidAt).HasColumnType("datetime2");

            entity.HasOne(d => d.Cause)
                .WithMany(c => c.Donations)
                .HasForeignKey(d => d.CauseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User)
                .WithMany(u => u.Donations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Programme
        builder.Entity<Programme>(entity =>
        {
            entity.ToTable("Programmes");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.StartAt);
            entity.Property(p => p.Title).HasMaxLength(250).IsRequired();
            entity.Property(p => p.Slug).HasMaxLength(250).IsRequired();
            entity.Property(p => p.Category).HasMaxLength(100);
            entity.Property(p => p.StartAt).HasColumnType("datetime2").IsRequired();
            entity.Property(p => p.EndAt).HasColumnType("datetime2");
            entity.Property(p => p.CreatedAt).HasColumnType("datetime2");
            entity.Property(p => p.UpdatedAt).HasColumnType("datetime2");
            entity.Property(p => p.DeletedAt).HasColumnType("datetime2");
            entity.HasQueryFilter(p => p.DeletedAt == null);
        });

        // ProgrammeInterest - composite unique to prevent duplicate interest registrations
        builder.Entity<ProgrammeInterest>(entity =>
        {
            entity.ToTable("ProgrammeInterests");
            entity.HasKey(pi => pi.Id);
            entity.HasIndex(pi => new { pi.ProgrammeId, pi.UserId }).IsUnique();
            entity.Property(pi => pi.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Interested");
            entity.Property(pi => pi.InterestedAt).HasColumnType("datetime2");
            entity.Property(pi => pi.CreatedAt).HasColumnType("datetime2");

            entity.HasOne(pi => pi.Programme)
                .WithMany(p => p.Interests)
                .HasForeignKey(pi => pi.ProgrammeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pi => pi.User)
                .WithMany(u => u.Interests)
                .HasForeignKey(pi => pi.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AboutSection - unified 7-section content model
        builder.Entity<AboutSection>(entity =>
        {
            entity.ToTable("AboutSections");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Slug).IsUnique();
            entity.Property(a => a.Slug).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Title).HasMaxLength(250).IsRequired();
            entity.Property(a => a.CreatedAt).HasColumnType("datetime2");
            entity.Property(a => a.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(a => a.UpdatedByUser)
                .WithMany(u => u.UpdatedAboutSections)
                .HasForeignKey(a => a.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Partner
        builder.Entity<Partner>(entity =>
        {
            entity.ToTable("Partners");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Slug).HasMaxLength(200).IsRequired();
            entity.Property(p => p.CreatedAt).HasColumnType("datetime2");
            entity.Property(p => p.UpdatedAt).HasColumnType("datetime2");
            entity.Property(p => p.DeletedAt).HasColumnType("datetime2");
            entity.HasQueryFilter(p => p.DeletedAt == null);
        });

        // NGO
        builder.Entity<NGO>(entity =>
        {
            entity.ToTable("NGOs");
            entity.HasKey(n => n.Id);
            entity.HasIndex(n => n.Slug).IsUnique();
            entity.Property(n => n.Name).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Slug).HasMaxLength(200).IsRequired();
            entity.Property(n => n.CreatedAt).HasColumnType("datetime2");
            entity.Property(n => n.UpdatedAt).HasColumnType("datetime2");
            entity.Property(n => n.DeletedAt).HasColumnType("datetime2");
            entity.HasQueryFilter(n => n.DeletedAt == null);
        });

        // GalleryImage
        builder.Entity<GalleryImage>(entity =>
        {
            entity.ToTable("GalleryImages");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.ImagePath).HasMaxLength(500).IsRequired();
            entity.Property(g => g.CreatedAt).HasColumnType("datetime2");
            entity.Property(g => g.UpdatedAt).HasColumnType("datetime2");
            entity.Property(g => g.DeletedAt).HasColumnType("datetime2");

            entity.HasOne(g => g.Programme)
                .WithMany(p => p.GalleryImages)
                .HasForeignKey(g => g.ProgrammeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(g => g.UploadedByUser)
                .WithMany(u => u.UploadedImages)
                .HasForeignKey(g => g.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(g => g.DeletedAt == null);
        });

        // Query
        builder.Entity<Query>(entity =>
        {
            entity.ToTable("Queries");
            entity.HasKey(q => q.Id);
            entity.HasIndex(q => q.Status);
            entity.Property(q => q.Name).HasMaxLength(150).IsRequired();
            entity.Property(q => q.Email).HasMaxLength(200).IsRequired();
            entity.Property(q => q.Subject).HasMaxLength(250).IsRequired();
            entity.Property(q => q.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Open");
            entity.Property(q => q.CreatedAt).HasColumnType("datetime2");
            entity.Property(q => q.UpdatedAt).HasColumnType("datetime2");

            entity.HasOne(q => q.User)
                .WithMany(u => u.Queries)
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // QueryReply
        builder.Entity<QueryReply>(entity =>
        {
            entity.ToTable("QueryReplies");
            entity.HasKey(qr => qr.Id);
            entity.Property(qr => qr.Message).IsRequired();
            entity.Property(qr => qr.RepliedAt).HasColumnType("datetime2");
            entity.Property(qr => qr.CreatedAt).HasColumnType("datetime2");

            entity.HasOne(qr => qr.Query)
                .WithMany(q => q.Replies)
                .HasForeignKey(qr => qr.QueryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(qr => qr.AdminUser)
                .WithMany(u => u.QueryReplies)
                .HasForeignKey(qr => qr.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ContactInfo
        builder.Entity<ContactInfo>(entity =>
        {
            entity.ToTable("ContactInfos");
            entity.HasKey(ci => ci.Id);
            entity.Property(ci => ci.Label).HasMaxLength(100).IsRequired();
            entity.Property(ci => ci.Address).HasMaxLength(300).IsRequired();
            entity.Property(ci => ci.Phone).HasMaxLength(50).IsRequired();
            entity.Property(ci => ci.Email).HasMaxLength(200).IsRequired();
            entity.Property(ci => ci.CreatedAt).HasColumnType("datetime2");
            entity.Property(ci => ci.UpdatedAt).HasColumnType("datetime2");
        });

        // Invitation
        builder.Entity<Invitation>(entity =>
        {
            entity.ToTable("Invitations");
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.Token).IsUnique();
            entity.Property(i => i.RecipientEmail).HasMaxLength(200).IsRequired();
            entity.Property(i => i.Token).HasMaxLength(128).IsRequired();
            entity.Property(i => i.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Pending");
            entity.Property(i => i.SentAt).HasColumnType("datetime2");
            entity.Property(i => i.ExpiresAt).HasColumnType("datetime2");
            entity.Property(i => i.CreatedAt).HasColumnType("datetime2");

            entity.HasOne(i => i.InviterUser)
                .WithMany(u => u.Invitations)
                .HasForeignKey(i => i.InviterUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AdminActivityLog
        builder.Entity<AdminActivityLog>(entity =>
        {
            entity.ToTable("AdminActivityLogs");
            entity.HasKey(al => al.Id);
            entity.Property(al => al.Action).HasMaxLength(100).IsRequired();
            entity.Property(al => al.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(al => al.CreatedAt).HasColumnType("datetime2");

            entity.HasOne(al => al.AdminUser)
                .WithMany(u => u.AdminActivityLogs)
                .HasForeignKey(al => al.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
