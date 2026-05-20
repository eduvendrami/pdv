using PDV.Domain.Enums;

namespace PDV.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? InternalCode { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockQuantity { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLowStock  => StockQuantity <= MinStockQuantity && MinStockQuantity > 0;

    /// <summary>Markup sobre custo: ((Venda - Custo) / Custo) * 100</summary>
    public decimal Margin => CostPrice > 0
        ? Math.Round((SalePrice - CostPrice) / CostPrice * 100, 1)
        : 0;
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? InternalCode { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockQuantity { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Unidade;
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
}
