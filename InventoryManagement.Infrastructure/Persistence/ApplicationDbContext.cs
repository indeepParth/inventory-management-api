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

        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

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

            builder.Entity<Customer>(entity =>
            {
                entity.HasIndex(x => x.Name)
                      .IsUnique();

                entity.HasIndex(x => x.GstNumber)
                      .IsUnique();

                entity.Property(x => x.Name)
                      .UseCollation("NOCASE")
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.ContactPerson).HasMaxLength(150);
                entity.Property(x => x.Phone).HasMaxLength(30);
                entity.Property(x => x.Email).HasMaxLength(254);
                entity.Property(x => x.BillingAddress).HasMaxLength(500);
                entity.Property(x => x.DeliveryAddress).HasMaxLength(500);
                entity.Property(x => x.GstNumber)
                      .UseCollation("NOCASE")
                      .HasMaxLength(15);
                entity.Property(x => x.CreditLimit).HasPrecision(18, 2);
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
            });

            builder.Entity<StockMovement>(entity =>
            {
                entity.Property(x => x.QuantityChange).HasPrecision(18, 3);
                entity.Property(x => x.BalanceBefore).HasPrecision(18, 3);
                entity.Property(x => x.BalanceAfter).HasPrecision(18, 3);
                entity.Property(x => x.UnitCost).HasPrecision(18, 2);
                entity.Property(x => x.SourceType).IsRequired();
                entity.Property(x => x.CreatedBy).IsRequired();

                entity.HasIndex(x => new { x.ProductId, x.OccurredAtUtc });
                entity.HasIndex(x => new { x.MovementType, x.OccurredAtUtc });

                entity.HasOne(x => x.Product)
                      .WithMany(x => x.StockMovements)
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired();
            });
        }
    }
}
