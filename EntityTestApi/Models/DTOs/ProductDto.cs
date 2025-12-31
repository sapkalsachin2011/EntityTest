namespace EntityTestApi.Models.DTOs
{
    /// <summary>
    /// Data Transfer Object for Product - No circular references
    /// </summary>
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}
