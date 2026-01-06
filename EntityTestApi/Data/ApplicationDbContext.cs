using Microsoft.EntityFrameworkCore;
using EntityTestApi.Models;

namespace EntityTestApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<ProductDetail> ProductDetails { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed a default category
            modelBuilder.Entity<Category>().HasData(new Category { Id = 1, Name = "Default" });

            // Seed initial suppliers
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "Acme Supplies", Description = "Leading supplier of office products.", ContactEmail = "contact@acme.com" },
                new Supplier { Id = 2, Name = "Global Tech", Description = "Electronics and IT supplier.", ContactEmail = "info@globaltech.com" }
            );

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.RowVersion).IsRowVersion();

                // Configure relationship: Product has one Category, Category has many Products
                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Set default value for CategoryId for existing products
                entity.Property(p => p.CategoryId).HasDefaultValue(1);

                // Configure one-to-one relationship: Product has one ProductDetail
                entity.HasOne(p => p.ProductDetail)
                    .WithOne(pd => pd.Product)
                    .HasForeignKey<ProductDetail>(pd => pd.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
