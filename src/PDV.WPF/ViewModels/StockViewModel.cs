using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;
using PDV.WPF.Helpers;

namespace PDV.WPF.ViewModels;

public partial class StockViewModel : BaseViewModel
{
    private readonly IStockService _stockService;
    private readonly IProductService _productService;

    [ObservableProperty] private ObservableCollection<ProductDto> _lowStockProducts = new();
    [ObservableProperty] private ObservableCollection<StockMovementDto> _movements = new();
    [ObservableProperty] private ProductDto? _selectedProduct;
    [ObservableProperty] private decimal _adjustQuantity;
    [ObservableProperty] private StockMovementType _adjustType = StockMovementType.Entrada;
    [ObservableProperty] private string? _adjustReason;
    [ObservableProperty] private IEnumerable<ProductDto> _allProducts = Enumerable.Empty<ProductDto>();
    [ObservableProperty] private string _searchTerm = string.Empty;

    public IEnumerable<StockMovementType> MovementTypes => Enum.GetValues<StockMovementType>();

    public StockViewModel(IStockService stockService, IProductService productService)
    {
        _stockService = stockService;
        _productService = productService;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var low = await _stockService.GetLowStockProductsAsync();
            LowStockProducts = new ObservableCollection<ProductDto>(low);
            var movs = await _stockService.GetMovementsAsync();
            Movements = new ObservableCollection<StockMovementDto>(movs.OrderByDescending(m => m.CreatedAt).Take(100));
            AllProducts = await _productService.GetAllAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task AdjustStockAsync()
    {
        if (SelectedProduct == null) { SetStatus("Selecione um produto."); return; }
        if (AdjustQuantity <= 0) { SetStatus("Informe uma quantidade válida."); return; }

        IsBusy = true;
        try
        {
            await _stockService.AdjustStockAsync(new AdjustStockDto
            {
                ProductId = SelectedProduct.Id,
                Quantity = AdjustQuantity,
                Type = AdjustType,
                Reason = AdjustReason
            }, SessionManager.CurrentUser!.Id);
            SetStatus("Estoque ajustado com sucesso!");
            AdjustQuantity = 0;
            AdjustReason = null;
            await LoadAsync();
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }
}
