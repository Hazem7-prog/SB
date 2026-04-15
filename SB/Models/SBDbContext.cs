using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SB.Models
{
    public class SBDbContext : IdentityDbContext<User>
    {
        public SBDbContext(DbContextOptions<SBDbContext> options)
             : base(options)
        {
        }

        public DbSet<Child> Children { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

                builder.Entity<Child>(b =>
            {
                b.HasKey(c => c.ChildId);

                b.Property(c => c.SimCardNum)
                 .HasMaxLength(32);

                // Optional FK to User. When User is deleted, set Child.UserId = NULL.
                b.HasOne(c => c.User)
                 .WithMany(u => u.Children)
                 .HasForeignKey(c => c.UserId)
                 .IsRequired(false)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // keep other model configuration here...
        }
    }
}           
