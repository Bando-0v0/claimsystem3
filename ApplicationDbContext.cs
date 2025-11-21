using Microsoft.EntityFrameworkCore;
using claimSystem3.Models;

namespace claimSystem3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MonthlyClaim> MonthlyClaims { get; set; } = null!;
        public DbSet<Lecturer> Lecturers { get; set; } = null!;
        public DbSet<ClaimApproval> ClaimApprovals { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Configure relationships
            builder.Entity<MonthlyClaim>()
                .HasOne(mc => mc.Lecturer)
                .WithMany(l => l.Claims)
                .HasForeignKey(mc => mc.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ClaimApproval>()
                .HasOne(ca => ca.Claim)
                .WithMany(c => c.Approvals)
                .HasForeignKey(ca => ca.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}