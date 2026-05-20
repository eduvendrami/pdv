using PDV.Application.DTOs;

namespace PDV.Application.Services;

public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime start, DateTime end);
    Task<StockReportDto> GetStockReportAsync();
    Task<byte[]> ExportSalesReportToPdfAsync(SalesReportDto report);
    Task<byte[]> ExportSalesReportToExcelAsync(SalesReportDto report);
    Task<byte[]> ExportStockReportToExcelAsync(StockReportDto report);
}
