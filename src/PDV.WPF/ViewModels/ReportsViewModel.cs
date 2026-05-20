using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;

namespace PDV.WPF.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IReportService _reportService;

    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private SalesReportDto? _salesReport;
    [ObservableProperty] private StockReportDto? _stockReport;
    [ObservableProperty] private int _selectedTab;

    public ReportsViewModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [RelayCommand]
    public async Task LoadSalesReportAsync()
    {
        IsBusy = true;
        try
        {
            SalesReport = await _reportService.GetSalesReportAsync(StartDate, EndDate);
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task LoadStockReportAsync()
    {
        IsBusy = true;
        try
        {
            StockReport = await _reportService.GetStockReportAsync();
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task ExportSalesPdfAsync()
    {
        if (SalesReport == null) { SetStatus("Gere o relatório primeiro."); return; }
        IsBusy = true;
        try
        {
            var bytes = await _reportService.ExportSalesReportToPdfAsync(SalesReport);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"relatorio_vendas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(path, bytes);
            SetStatus($"PDF exportado: {path}");
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task ExportSalesExcelAsync()
    {
        if (SalesReport == null) { SetStatus("Gere o relatório primeiro."); return; }
        IsBusy = true;
        try
        {
            var bytes = await _reportService.ExportSalesReportToExcelAsync(SalesReport);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"relatorio_vendas_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await File.WriteAllBytesAsync(path, bytes);
            SetStatus($"Excel exportado: {path}");
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task ExportStockExcelAsync()
    {
        if (StockReport == null) { SetStatus("Gere o relatório primeiro."); return; }
        IsBusy = true;
        try
        {
            var bytes = await _reportService.ExportStockReportToExcelAsync(StockReport);
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"relatorio_estoque_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await File.WriteAllBytesAsync(path, bytes);
            SetStatus($"Excel exportado: {path}");
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }
}
