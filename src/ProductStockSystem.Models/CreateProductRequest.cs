using System.ComponentModel.DataAnnotations;

namespace ProductStockSystem.Models;

public class CreateProductRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int InitialStock { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }
}
