using backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campus> Campuses => Set<Campus>();

    public DbSet<Career> Careers => Set<Career>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<ExchangePeriod> ExchangePeriods => Set<ExchangePeriod>();

    public DbSet<CertificateSubmission> CertificateSubmissions => Set<CertificateSubmission>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Campus>(entity =>
        {
            entity.ToTable("Campuses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Career>(entity =>
        {
            entity.ToTable("Careers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StudentCode).HasMaxLength(30).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(30);
            entity.HasIndex(x => x.StudentCode).IsUnique();

            entity.HasOne(x => x.Campus)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Career)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.CareerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExchangePeriod>(entity =>
        {
            entity.ToTable("ExchangePeriods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Semester).HasMaxLength(20).IsRequired();
            entity.Property(x => x.HostCountry).HasMaxLength(100).IsRequired();
            entity.Property(x => x.HostInstitution).HasMaxLength(150).IsRequired();

            entity.HasOne(x => x.Student)
                .WithMany(x => x.ExchangePeriods)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Campus)
                .WithMany(x => x.ExchangePeriods)
                .HasForeignKey(x => x.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CertificateSubmission>(entity =>
        {
            entity.ToTable("CertificateSubmissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReviewNotes).HasMaxLength(1000);

            entity.HasOne(x => x.Student)
                .WithMany(x => x.CertificateSubmissions)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ExchangePeriod)
                .WithMany(x => x.CertificateSubmissions)
                .HasForeignKey(x => x.ExchangePeriodId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecipientRole).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(1000).IsRequired();

            entity.HasOne(x => x.Student)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.CertificateSubmission)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.CertificateSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}