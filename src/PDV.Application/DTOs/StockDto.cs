using PDV.Domain.Enums;

namespace PDV.Application.DTOs;

public class StockMovementDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdjustStockDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public StockMovementType Type { get; set; }
    public string? Reason { get; set; }
}
