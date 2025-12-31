using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityTestApi.Models
{
    public class ProductDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Details { get; set; } = null!;

        // Foreign key for Product
        public int ProductId { get; set; }

        // Navigation property for Product
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}
