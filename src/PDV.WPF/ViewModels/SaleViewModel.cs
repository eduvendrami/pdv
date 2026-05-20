using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;
using PDV.WPF.Helpers;

namespace PDV.WPF.ViewModels;

public partial class SaleItemLine : ObservableObject
{
    public int ProductId { get; set; }
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private string? _barcode;
    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private decimal _discountAmount;
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public decimal TotalPrice => (Quantity * UnitPrice) - DiscountAmount;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(TotalPrice));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalPrice));
    partial void OnDiscountAmountChanged(decimal value) => OnPropertyChanged(nameof(TotalPrice));
}

public partial class SaleViewModel : BaseViewModel
{
    private readonly IProductService _productService;
    private readonly ISaleService _saleService;
    private readonly ICustomerService _customerService;

    // Quantity pending when user types "N*TERM" or "N TERM" prefix
    private decimal _pendingQty = 1;

    [ObservableProperty] private string _searchInput = string.Empty;
    [ObservableProperty] private ObservableCollection<ProductDto> _searchResults = new();
    [ObservableProperty] private bool _showSearchResults;
    [ObservableProperty] private int _selectedResultIndex = -1;

    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string _statusMsg = string.Empty;

    public ObservableCollection<SaleItemLine> Items { get; } = new();

    public decimal Subtotal => Items.Sum(i => i.TotalPrice);
    public decimal Total => Subtotal - DiscountAmount;

    // ── Role-based permissions (evaluated once per VM lifetime) ───────────
    public bool CanApplyDiscount    => Helpers.SessionManager.CanApplyDiscount;
    public bool CanEditItemDiscount => Helpers.SessionManager.CanApplyDiscount;

    // View subscribes to move keyboard focus back to search box
    public event Action? FocusSearchRequested;
    // View subscribes to move keyboard focus to the results list automatically
    public event Action? FocusListRequested;

    public SaleViewModel(IProductService productService, ISaleService saleService, ICustomerService customerService)
    {
        _productService = productService;
        _saleService = saleService;
        _customerService = customerService;
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
    }

    partial void OnDiscountAmountChanged(decimal value) => NotifyTotals();

    // ── Search ────────────────────────────────────────────────────────────────

    // Parses optional "N*TERM" or "N TERM" prefix (e.g. "3*areia" or "5 cimento").
    private static readonly Regex QtyPrefix = new(
        @"^(\d+(?:[,\.]\d+)?)[*xX\s]\s*(.+)$", RegexOptions.Compiled);

    [RelayCommand]
    public async Task SearchAsync()
    {
        var raw = SearchInput.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        // Parse optional quantity prefix: "3*cimento" or "5 areia"
        decimal qty = 1;
        string term = raw;
        var m = QtyPrefix.Match(raw);
        if (m.Success &&
            decimal.TryParse(m.Groups[1].Value.Replace(',', '.'),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedQty) &&
            parsedQty > 0)
        {
            qty = parsedQty;
            term = m.Groups[2].Value.Trim();
        }

        // 1. Exact barcode match (scanner path — instant add)
        var exact = await _productService.GetByBarcodeAsync(term);
        if (exact != null)
        {
            AddOrUpdateItem(exact, qty);
            ClearSearch();
            return;
        }

        // 2. Partial search (name / barcode / internal code)
        var results = (await _productService.SearchAsync(term)).ToList();

        if (results.Count == 0)
        {
            StatusMsg = $"Nenhum produto encontrado para \"{term}\".";
            ShowSearchResults = false;
            return;
        }

        if (results.Count == 1)
        {
            AddOrUpdateItem(results[0], qty);
            ClearSearch();
            return;
        }

        // Multiple matches — show picker and auto-focus the list
        _pendingQty = qty;
        SearchResults = new ObservableCollection<ProductDto>(results);
        ShowSearchResults = true;
        SelectedResultIndex = 0;
        StatusMsg = string.Empty;
        FocusListRequested?.Invoke();
    }

    public void MoveSelection(int delta)
    {
        if (!ShowSearchResults || SearchResults.Count == 0) return;
        var next = SelectedResultIndex + delta;
        if (next < 0) next = 0;
        if (next >= SearchResults.Count) next = SearchResults.Count - 1;
        SelectedResultIndex = next;
    }

    public void ConfirmSelectedResult()
    {
        if (!ShowSearchResults || SelectedResultIndex < 0 || SelectedResultIndex >= SearchResults.Count)
            return;
        AddOrUpdateItem(SearchResults[SelectedResultIndex], _pendingQty);
        _pendingQty = 1;
        ClearSearch();
    }

    [RelayCommand]
    public void AddProduct(ProductDto product)
    {
        AddOrUpdateItem(product, _pendingQty);
        _pendingQty = 1;
        ClearSearch();
    }

    private void AddOrUpdateItem(ProductDto product, decimal qty = 1)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity += qty;
        }
        else
        {
            var item = new SaleItemLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = product.Barcode,
                Quantity = qty,
                UnitPrice = product.SalePrice,
                UnitOfMeasure = product.UnitOfMeasure
            };
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }
        NotifyTotals();
        FocusSearchRequested?.Invoke();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SaleItemLine.TotalPrice)
                           or nameof(SaleItemLine.Quantity)
                           or nameof(SaleItemLine.UnitPrice)
                           or nameof(SaleItemLine.DiscountAmount))
            NotifyTotals();
    }

    [RelayCommand]
    public void RemoveItem(SaleItemLine item)
    {
        item.PropertyChanged -= OnItemPropertyChanged;
        Items.Remove(item);
        NotifyTotals();
        FocusSearchRequested?.Invoke();
    }

    [RelayCommand]
    public void ClearSearchResults()
    {
        ShowSearchResults = false;
        SearchResults.Clear();
        StatusMsg = string.Empty;
        FocusSearchRequested?.Invoke();
    }

    private void ClearSearch()
    {
        SearchInput = string.Empty;
        ShowSearchResults = false;
        SearchResults.Clear();
        StatusMsg = string.Empty;
    }

    // ── Finalize ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task FinalizeSaleAsync()
    {
        if (!Items.Any())
        {
            StatusMsg = "Adicione pelo menos um produto.";
            return;
        }

        // Open payment dialog
        var paymentVm = new PaymentDialogViewModel(Total);
        var dialog = new Views.PaymentDialog { DataContext = paymentVm };
        dialog.Owner = System.Windows.Application.Current.MainWindow;

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        try
        {
            var dto = new CreateSaleDto
            {
                CustomerId = SelectedCustomer?.Id,
                DiscountAmount = DiscountAmount,
                Notes = Notes,
                Items = Items.Select(i => new CreateSaleItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountAmount = i.DiscountAmount
                }).ToList(),
                Payments = paymentVm.GetPayments()
            };

            var sale = await _saleService.CreateSaleAsync(dto, SessionManager.CurrentUser!.Id);
            StatusMsg = $"✓ Venda {sale.SaleNumber} finalizada!";
            ClearSale();
        }
        catch (Exception ex)
        {
            StatusMsg = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void ClearSale()
    {
        foreach (var item in Items)
            item.PropertyChanged -= OnItemPropertyChanged;
        Items.Clear();
        SelectedCustomer = null;
        DiscountAmount = 0;
        Notes = null;
        ClearSearch();
        NotifyTotals();
        FocusSearchRequested?.Invoke();
    }
}
