using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;

namespace PDV.WPF.ViewModels;

public partial class ProductEditViewModel : BaseViewModel
{
    private readonly IProductService  _productService;
    private readonly ISupplierService _supplierService;
    private int? _editingId;

    // ── Fields ────────────────────────────────────────────────────────────
    [ObservableProperty] private string  _name          = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private string? _internalCode;
    [ObservableProperty] private decimal _costPrice;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _stockQuantity;
    [ObservableProperty] private decimal _minStockQuantity;
    [ObservableProperty] private UnitOfMeasure _unitOfMeasure = UnitOfMeasure.Unidade;
    [ObservableProperty] private int?    _categoryId;
    [ObservableProperty] private int?    _supplierId;
    [ObservableProperty] private string  _dialogTitle   = "Novo Produto";
    [ObservableProperty] private string  _errorMessage  = string.Empty;

    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = new();
    [ObservableProperty] private ObservableCollection<SupplierDto> _suppliers  = new();

    // ── Derived / read-only ───────────────────────────────────────────────
    public bool    IsEdit  => _editingId.HasValue;
    public decimal Margin  => CostPrice > 0
        ? Math.Round((SalePrice - CostPrice) / CostPrice * 100, 1)
        : 0;
    public decimal Profit  => SalePrice - CostPrice;

    public IEnumerable<UnitOfMeasure> Units => Enum.GetValues<UnitOfMeasure>();

    // ── Role permissions ──────────────────────────────────────────────────
    public bool CanEditCost => Helpers.SessionManager.IsAdmin; // only Admin sees cost price

    // ── Events ────────────────────────────────────────────────────────────
    public event Action<bool>? RequestClose;

    public ProductEditViewModel(IProductService productService, ISupplierService supplierService)
    {
        _productService  = productService;
        _supplierService = supplierService;
    }

    public async Task InitializeAsync(ProductDto? product)
    {
        ErrorMessage = string.Empty;

        var cats  = await _productService.GetCategoriesAsync();
        var supps = await _supplierService.GetAllAsync();
        Categories = new ObservableCollection<CategoryDto>(cats);
        Suppliers  = new ObservableCollection<SupplierDto>(supps.Where(s => s.IsActive));

        if (product != null)
        {
            _editingId      = product.Id;
            DialogTitle     = "Editar Produto";
            Name            = product.Name;
            Description     = product.Description ?? string.Empty;
            Barcode         = product.Barcode;
            InternalCode    = product.InternalCode;
            CostPrice       = product.CostPrice;
            SalePrice       = product.SalePrice;
            StockQuantity   = product.StockQuantity;
            MinStockQuantity= product.MinStockQuantity;
            UnitOfMeasure   = product.UnitOfMeasure;
            CategoryId      = product.CategoryId;
            SupplierId      = product.SupplierId;
        }
        else
        {
            _editingId = null;
            DialogTitle     = "Novo Produto";
            Name = Description = string.Empty;
            Barcode = InternalCode = null;
            CostPrice = SalePrice = StockQuantity = MinStockQuantity = 0;
            UnitOfMeasure = UnitOfMeasure.Unidade;
            CategoryId = SupplierId = null;
        }

        RefreshMargin();
    }

    // Recalculate margin whenever cost or sale price changes
    partial void OnCostPriceChanged(decimal value)  => RefreshMargin();
    partial void OnSalePriceChanged(decimal value)  => RefreshMargin();
    private void RefreshMargin()
    {
        OnPropertyChanged(nameof(Margin));
        OnPropertyChanged(nameof(Profit));
    }

    // ── Save ──────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "O campo Nome é obrigatório.";
            return;
        }
        if (SalePrice <= 0)
        {
            ErrorMessage = "O Preço de Venda deve ser maior que zero.";
            return;
        }
        if (CostPrice > SalePrice)
        {
            ErrorMessage = "O Preço de Custo não pode ser maior que o de Venda.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(Barcode) && Barcode.Length < 8)
        {
            ErrorMessage = "Código de barras inválido (mínimo 8 dígitos).";
            return;
        }

        IsBusy = true;
        try
        {
            var dto = new CreateProductDto
            {
                Name             = Name.Trim(),
                Description      = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                Barcode          = string.IsNullOrWhiteSpace(Barcode)      ? null : Barcode.Trim(),
                InternalCode     = string.IsNullOrWhiteSpace(InternalCode) ? null : InternalCode.Trim(),
                CostPrice        = CostPrice,
                SalePrice        = SalePrice,
                StockQuantity    = StockQuantity,
                MinStockQuantity = MinStockQuantity,
                UnitOfMeasure    = UnitOfMeasure,
                CategoryId       = CategoryId,
                SupplierId       = SupplierId
            };

            if (_editingId.HasValue)
                await _productService.UpdateAsync(_editingId.Value, dto);
            else
                await _productService.CreateAsync(dto);

            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            ErrorMessage = msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                ? "Já existe um produto com esse código de barras ou código interno."
                : $"Erro ao salvar: {msg}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void Cancel() => RequestClose?.Invoke(false);
}
