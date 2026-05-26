using ClosedXML.Excel;
using PDV.Application.DTOs;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PDV.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public ReportService(IUnitOfWorkFactory uowFactory)
    {
        _uowFactory = uowFactory;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime start, DateTime end, bool includeCancelled = false)
    {
        using var uow = _uowFactory.Create();
        var sales = await uow.Sales.GetByDateRangeAsync(start, end.AddDays(1).AddSeconds(-1));

        // Totais e métricas consideram apenas vendas finalizadas (canceladas tiveram estoque estornado).
        var salesList = sales.Where(s => s.Status == SaleStatus.Finalizada).ToList();

        var report = new SalesReportDto
        {
            StartDate = start,
            EndDate = end,
            TotalSales = salesList.Count,
            TotalRevenue = salesList.Sum(s => s.TotalAmount),
            TotalDiscount = salesList.Sum(s => s.DiscountAmount),
            NetRevenue = salesList.Sum(s => s.FinalAmount)
        };

        // A lista exibida inclui canceladas apenas quando o flag estiver ligado (só para consulta).
        var listSource = includeCancelled
            ? sales.Where(s => s.Status is SaleStatus.Finalizada or SaleStatus.Cancelada)
            : salesList;

        report.Sales = listSource
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleSummaryDto
            {
                Id = s.Id,
                SaleNumber = s.SaleNumber,
                SaleDate = s.SaleDate,
                CustomerName = s.Customer?.Name,
                FinalAmount = s.FinalAmount,
                Status = s.Status.ToString()
            }).ToList();

        var itemGroups = salesList
            .SelectMany(s => s.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new TopProductDto
            {
                ProductName = g.First().Product?.Name ?? "Desconhecido",
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(10)
            .ToList();
        report.TopProducts = itemGroups;

        var paymentGroups = salesList
            .SelectMany(s => s.Payments)
            .GroupBy(p => p.Method.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
        report.RevenueByPaymentMethod = paymentGroups;

        return report;
    }

    public async Task<StockReportDto> GetStockReportAsync()
    {
        using var uow = _uowFactory.Create();
        var products = await uow.Products.FindAsync(p => p.IsActive);
        var productList = products.ToList();

        return new StockReportDto
        {
            TotalProducts = productList.Count,
            LowStockCount = productList.Count(p => p.StockQuantity <= p.MinStockQuantity),
            TotalStockValue = productList.Sum(p => p.StockQuantity * p.SalePrice),
            Products = productList.Select(p => new ProductStockDto
            {
                Name = p.Name,
                CategoryName = p.Category?.Name,
                StockQuantity = p.StockQuantity,
                MinStockQuantity = p.MinStockQuantity,
                SalePrice = p.SalePrice,
                StockValue = p.StockQuantity * p.SalePrice,
                IsLowStock = p.StockQuantity <= p.MinStockQuantity
            }).ToList()
        };
    }

    public Task<byte[]> ExportSalesReportToPdfAsync(SalesReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Relatório de Vendas — {report.StartDate:dd/MM/yyyy} a {report.EndDate:dd/MM/yyyy}")
                        .FontSize(16).Bold();
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text($"Total de Vendas: {report.TotalSales}");
                        row.RelativeItem().Text($"Receita Líquida: {report.NetRevenue:C}");
                    });
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Nº Venda").Bold();
                            h.Cell().Text("Data").Bold();
                            h.Cell().Text("Cliente").Bold();
                            h.Cell().Text("Total").Bold();
                        });
                        foreach (var s in report.Sales)
                        {
                            table.Cell().Text(s.SaleNumber);
                            table.Cell().Text(s.SaleDate.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Text(s.CustomerName ?? "Consumidor Final");
                            table.Cell().Text(s.FinalAmount.ToString("C"));
                        }
                    });
                });
            });
        });

        var bytes = document.GeneratePdf();
        return Task.FromResult(bytes);
    }

    public Task<byte[]> ExportSalesReportToExcelAsync(SalesReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Vendas");
        ws.Cell(1, 1).Value = "Nº Venda";
        ws.Cell(1, 2).Value = "Data";
        ws.Cell(1, 3).Value = "Cliente";
        ws.Cell(1, 4).Value = "Total";
        ws.Cell(1, 5).Value = "Status";

        for (int i = 0; i < report.Sales.Count; i++)
        {
            var s = report.Sales[i];
            ws.Cell(i + 2, 1).Value = s.SaleNumber;
            ws.Cell(i + 2, 2).Value = s.SaleDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(i + 2, 3).Value = s.CustomerName ?? "Consumidor Final";
            ws.Cell(i + 2, 4).Value = s.FinalAmount;
            ws.Cell(i + 2, 5).Value = s.Status;
        }
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    public Task<byte[]> ExportStockReportToExcelAsync(StockReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Estoque");
        ws.Cell(1, 1).Value = "Produto";
        ws.Cell(1, 2).Value = "Categoria";
        ws.Cell(1, 3).Value = "Estoque Atual";
        ws.Cell(1, 4).Value = "Estoque Mínimo";
        ws.Cell(1, 5).Value = "Preço Venda";
        ws.Cell(1, 6).Value = "Valor em Estoque";

        for (int i = 0; i < report.Products.Count; i++)
        {
            var p = report.Products[i];
            ws.Cell(i + 2, 1).Value = p.Name;
            ws.Cell(i + 2, 2).Value = p.CategoryName ?? "";
            ws.Cell(i + 2, 3).Value = p.StockQuantity;
            ws.Cell(i + 2, 4).Value = p.MinStockQuantity;
            ws.Cell(i + 2, 5).Value = p.SalePrice;
            ws.Cell(i + 2, 6).Value = p.StockValue;
        }
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}
