using System.ComponentModel.DataAnnotations;

namespace ProductStockSystem.Models;

public class UpdateStockRequest
{
    [Required]
    public StockMovementType MovementType { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
