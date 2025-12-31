using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EntityTestApi.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        // Navigation property for related products
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
