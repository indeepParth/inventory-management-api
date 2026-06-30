using InventoryManagement.Domain.Entities;
using InventoryManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Product> Products => Set<Product>();

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Supplier> Suppliers => Set<Supplier>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasIndex(x => x.Token)
                      .IsUnique();

                entity.Property(x => x.Token)
                      .IsRequired();

                entity.Property(x => x.UserId)
                      .IsRequired();
            });

            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(x => x.Name)
                      .IsUnique();

                entity.Property(x => x.Name)
                      .IsRequired();

                entity.Property(x => x.Description)
                      .IsRequired();

                entity.Property(x => x.IsActive)
                      .IsRequired();
            });

            builder.Entity<Supplier>(entity =>
            {
                entity.HasIndex(x => x.Name)
                      .IsUnique();

                entity.HasIndex(x => x.GstNumber)
                      .IsUnique();

                entity.Property(x => x.Name)
                      .UseCollation("NOCASE")
                      .IsRequired();

                entity.Property(x => x.GstNumber)
                      .UseCollation("NOCASE");
            });

            builder.Entity<Product>(entity =>
            {
                entity.Property(x => x.Quantity)
                      .HasPrecision(18, 3);

                entity.Property(x => x.DefaultSellingPrice)
                      .HasPrecision(18, 2);

                entity.Property(x => x.AverageCost)
                      .HasPrecision(18, 2);

                entity.Property(x => x.BaseUnit)
                      .IsRequired();

                entity.HasOne(x => x.Category)
                      .WithMany(x => x.Products)
                      .HasForeignKey(x => x.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();

                entity.HasOne(x => x.Supplier)
                      .WithMany(x => x.Products)
                      .HasForeignKey(x => x.SupplierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
