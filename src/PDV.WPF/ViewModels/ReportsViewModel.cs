using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.WPF.Helpers;

namespace PDV.WPF.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    private readonly ISaleService _saleService;

    [ObservableProperty] private DateTime _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Today;
    [ObservableProperty] private SalesReportDto? _salesReport;
    [ObservableProperty] private StockReportDto? _stockReport;
    [ObservableProperty] private int _selectedTab;

    /// <summary>Quando ligado, vendas canceladas também aparecem na lista (sem somar nos totais).</summary>
    [ObservableProperty] private bool _showCancelled;

    // ── Vendas do dia ──────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _dailyDate = DateTime.Today;
    [ObservableProperty] private SalesReportDto? _dailyReport;

    partial void OnShowCancelledChanged(bool value)
    {
        if (SalesReport != null) _ = LoadSalesReportAsync();
        if (DailyReport  != null) _ = LoadDailyReportAsync();
    }

    public ReportsViewModel(IReportService reportService, ISaleService saleService)
    {
        _reportService = reportService;
        _saleService = saleService;
    }

    /// <summary>
    /// Reabre o detalhe (comprovante) de uma venda do relatório, permitindo gerar o PDF novamente.
    /// </summary>
    [RelayCommand]
    public async Task OpenSaleDetailAsync(SaleSummaryDto? summary)
    {
        if (summary is null) return;
        IsBusy = true;
        try
        {
            var sale = await _saleService.GetByIdAsync(summary.Id);
            if (sale is null) { SetStatus("Venda não encontrada."); return; }

            var receipt = new Views.SaleReceiptWindow(sale) { Owner = App.GetMainWindow() };
            receipt.ShowDialog();
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// Cancela (exclui) uma venda: marca como Cancelada e estorna o estoque.
    /// </summary>
    [RelayCommand]
    public async Task CancelSaleAsync(SaleSummaryDto? summary)
    {
        if (summary is null) return;

        if (!string.Equals(summary.Status, "Finalizada", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("Apenas vendas finalizadas podem ser canceladas.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Cancelar a venda {summary.SaleNumber}?\n\nO estoque dos itens será estornado. Esta ação não pode ser desfeita.",
            "Cancelar venda",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var ok = await _saleService.CancelSaleAsync(summary.Id, SessionManager.CurrentUser!.Id);
            if (!ok) { SetStatus("Não foi possível cancelar a venda."); return; }

            SetStatus($"Venda {summary.SaleNumber} cancelada.");

            // Atualiza automaticamente os relatórios abertos (período e/ou dia).
            if (SalesReport != null) await LoadSalesReportAsync();
            if (DailyReport != null) await LoadDailyReportAsync();
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task LoadSalesReportAsync()
    {
        IsBusy = true;
        try
        {
            SalesReport = await _reportService.GetSalesReportAsync(StartDate, EndDate, ShowCancelled);
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task LoadDailyReportAsync()
    {
        IsBusy = true;
        try
        {
            DailyReport = await _reportService.GetSalesReportAsync(DailyDate.Date, DailyDate.Date, ShowCancelled);
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
