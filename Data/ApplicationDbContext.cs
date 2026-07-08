using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<CandidateProfile> CandidateProfiles { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }


        public DbSet<JobPosting> JobPostings { get; set; }

        public DbSet<Interview> Interviews { get; set; }

        public DbSet<CandidateEvaluation> CandidateEvaluations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.FullName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(u => u.Email)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(u => u.PasswordHash)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(u => u.Role)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(u => u.IsActive)
                      .HasDefaultValue(true);

                entity.Property(u => u.CreatedAt)
                      .IsRequired();
            });

            modelBuilder.Entity<CandidateProfile>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasIndex(c => c.UserId)
                      .IsUnique();

                entity.Property(c => c.Phone)
                      .HasMaxLength(20);

                entity.Property(c => c.Location)
                      .HasMaxLength(100);

                entity.Property(c => c.Summary)
                      .HasMaxLength(1000);

                entity.Property(c => c.Skills)
                      .HasMaxLength(1000);

                entity.Property(c => c.ResumeUrl)
                      .HasMaxLength(500);

                entity.HasOne(c => c.User)
                      .WithOne()
                      .HasForeignKey<CandidateProfile>(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<JobPosting>(entity =>
            {
                entity.HasKey(j => j.Id);

                entity.Property(j => j.Title)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(j => j.Description)
                      .HasMaxLength(3000)
                      .IsRequired();

                entity.Property(j => j.RequiredSkills)
                      .HasMaxLength(1000)
                      .IsRequired();

                entity.Property(j => j.Location)
                      .HasMaxLength(100);

                entity.Property(j => j.EmploymentType)
                      .HasMaxLength(50);

                entity.Property(j => j.Salary)
                      .HasPrecision(18, 2);

                entity.Property(j => j.IsActive)
                      .HasDefaultValue(true);

                entity.HasOne(j => j.Recruiter)
                      .WithMany()
                      .HasForeignKey(j => j.RecruiterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<JobApplication>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasIndex(a => new { a.JobPostingId, a.CandidateProfileId })
                      .IsUnique();

                entity.Property(a => a.Status)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(a => a.AiMatchScore)
                      .IsRequired();

                entity.Property(a => a.AppliedAt)
                      .IsRequired();

                entity.Property(a => a.UpdatedAt)
                      .IsRequired();

                entity.HasOne(a => a.JobPosting)
                      .WithMany()
                      .HasForeignKey(a => a.JobPostingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.CandidateProfile)
                      .WithMany()
                      .HasForeignKey(a => a.CandidateProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

            });
            modelBuilder.Entity<Interview>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Mode)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(i => i.MeetingLink)
                      .HasMaxLength(500);

                entity.Property(i => i.Location)
                      .HasMaxLength(200);

                entity.Property(i => i.Status)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.Property(i => i.Notes)
                      .HasMaxLength(1000);

                entity.HasOne(i => i.JobApplication)
                      .WithMany()
                      .HasForeignKey(i => i.JobApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.ScheduledByUser)
                      .WithMany()
                      .HasForeignKey(i => i.ScheduledByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CandidateEvaluation>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.JobApplicationId)
                      .IsUnique();

                entity.Property(e => e.Feedback)
                      .HasMaxLength(2000);

                entity.Property(e => e.Decision)
                      .HasMaxLength(30)
                      .IsRequired();

                entity.HasOne(e => e.JobApplication)
                      .WithOne()
                      .HasForeignKey<CandidateEvaluation>(e => e.JobApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.EvaluatorUser)
                      .WithMany()
                      .HasForeignKey(e => e.EvaluatorUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Action)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(a => a.EntityName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(a => a.Details)
                      .HasMaxLength(2000);

                entity.Property(a => a.IpAddress)
                      .HasMaxLength(100);

                entity.Property(a => a.CreatedAt)
                      .IsRequired();

                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}