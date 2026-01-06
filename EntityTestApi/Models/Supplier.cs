using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EntityTestApi.Models
{
	public class Supplier
	{
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string Name { get; set; } = null!;

		[MaxLength(1000)]
		public string? Description { get; set; }

		[MaxLength(100)]
		public string? ContactEmail { get; set; }
	}
}
