using System.ComponentModel.DataAnnotations;

namespace EntityTestApi.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        // Foreign key for Category
        public int CategoryId { get; set; }

        // Navigation property for Category (virtual for lazy loading)
        public virtual Category Category { get; set; } = null!;

        // Navigation property for ProductDetail
        public virtual ProductDetail? ProductDetail { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }
}
