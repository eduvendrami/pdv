namespace PDV.Application.DTOs;

public class SalesReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetRevenue { get; set; }
    public List<SaleSummaryDto> Sales { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
}

public class SaleSummaryDto
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string? CustomerName { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class TopProductDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class StockReportDto
{
    public int TotalProducts { get; set; }
    public int LowStockCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public List<ProductStockDto> Products { get; set; } = new();
}

public class ProductStockDto
{
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockQuantity { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockValue { get; set; }
    public bool IsLowStock { get; set; }
}
