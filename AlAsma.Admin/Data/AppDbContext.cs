using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlAsma.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace AlAsma.Admin.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Author> Authors { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<Operation> Operations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Author configuration
            modelBuilder.Entity<Author>(eb =>
            {
                eb.HasIndex(a => a.Code).IsUnique();
                eb.Property(a => a.IsDeleted).HasDefaultValue(false);
                eb.Property(a => a.BasicFees).HasDefaultValue(0m).HasPrecision(18, 2);
                eb.HasQueryFilter(a => !a.IsDeleted);
            });

            // Sale configuration
            modelBuilder.Entity<Sale>(sb =>
            {
                sb.Property(s => s.SaleDate).HasDefaultValueSql("GETUTCDATE()");

                // Decimal precision for monetary values
                sb.Property(s => s.SalePrice).HasPrecision(18, 2);
                sb.Property(s => s.BasicExpenses).HasPrecision(18, 2);
                sb.Property(s => s.TotalAmount).HasPrecision(18, 2);

                sb.HasOne(s => s.Author)
                  .WithMany(a => a.Sales)
                  .HasForeignKey(s => s.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            // Operation configuration
            modelBuilder.Entity<Operation>(ob =>
            {
                ob.Property(o => o.OperationDate).HasDefaultValueSql("GETUTCDATE()");

                ob.Property(o => o.ExpenseAmount).HasPrecision(18, 2);
                ob.Property(o => o.TotalAmount).HasPrecision(18, 2);

                ob.HasOne(o => o.Author)
                  .WithMany()
                  .HasForeignKey(o => o.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries<BaseEntity>();
            var utcNow = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.UpdatedAt = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Prevent changes to CreatedAt
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Entity.UpdatedAt = utcNow;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
