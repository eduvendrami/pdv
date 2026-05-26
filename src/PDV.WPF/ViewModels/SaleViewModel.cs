using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;
using PDV.WPF.Helpers;
using PDV.WPF.Services;

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

    partial void OnQuantityChanged(decimal value)       => OnPropertyChanged(nameof(TotalPrice));
    partial void OnUnitPriceChanged(decimal value)      => OnPropertyChanged(nameof(TotalPrice));
    partial void OnDiscountAmountChanged(decimal value) => OnPropertyChanged(nameof(TotalPrice));
}

public partial class SaleViewModel : BaseViewModel
{
    private readonly IProductService  _productService;
    private readonly ISaleService     _saleService;
    private readonly ICustomerService _customerService;
    private readonly IDialogService   _dialogService;

    // Quantity / prefix state
    private decimal _pendingQty          = 1;
    private bool    _pendingHasQtyPrefix = false;

    // Debounce for live search
    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string  _searchInput       = string.Empty;
    [ObservableProperty] private ObservableCollection<ProductDto> _searchResults = new();
    [ObservableProperty] private bool    _showSearchResults;
    [ObservableProperty] private int     _selectedResultIndex = -1;

    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<SaleItemLine> Items { get; } = new();

    public decimal Subtotal => Items.Sum(i => i.TotalPrice);
    public decimal Total    => Subtotal - DiscountAmount;

    public bool CanApplyDiscount    => SessionManager.CanApplyDiscount;
    public bool CanEditItemDiscount => SessionManager.CanApplyDiscount;
    public bool CanEditItemPrice    => SessionManager.CanEditPrice;

    public event Action? FocusSearchRequested;
    public event Action? FocusListRequested;

    public SaleViewModel(IProductService productService, ISaleService saleService,
        ICustomerService customerService, IDialogService dialogService)
    {
        _productService  = productService;
        _saleService     = saleService;
        _customerService = customerService;
        _dialogService   = dialogService;
    }

    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
    }

    partial void OnDiscountAmountChanged(decimal value) => NotifyTotals();

    // ── FEATURE 1: Busca ao digitar (debounce 350 ms) ────────────────────
    private static readonly Regex QtyPrefix = new(
        @"^(\d+(?:[,\.]\d+)?)[*xX\s]\s*(.+)$", RegexOptions.Compiled);

    partial void OnSearchInputChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        _ = LiveSearchAsync(value, _searchCts.Token);
    }

    private async Task LiveSearchAsync(string input, CancellationToken ct)
    {
        try
        {
            // Extrai o termo real (ignora prefixo de qtd)
            var m = QtyPrefix.Match(input);
            var term = m.Success ? m.Groups[2].Value.Trim() : input.Trim();

            if (term.Length < 2)
            {
                ShowSearchResults = false;
                SearchResults.Clear();
                return;
            }

            await Task.Delay(350, ct);
            if (ct.IsCancellationRequested) return;

            var results = (await _productService.SearchAsync(term)).Take(10).ToList();
            if (ct.IsCancellationRequested) return;

            SearchResults        = new ObservableCollection<ProductDto>(results);
            ShowSearchResults    = results.Count > 0;
            SelectedResultIndex  = results.Count > 0 ? 0 : -1;
        }
        catch (OperationCanceledException) { /* normal — nova digitação cancelou */ }
    }

    // ── FEATURE 2: Botão "Buscar" / Enter ────────────────────────────────
    [RelayCommand]
    public async Task SearchAsync()
    {
        var raw = SearchInput.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return;

        decimal qty  = 1;
        string  term = raw;
        bool hasPrefix = false;

        var m = QtyPrefix.Match(raw);
        if (m.Success &&
            decimal.TryParse(m.Groups[1].Value.Replace(',', '.'),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsedQty) && parsedQty > 0)
        {
            qty = parsedQty; term = m.Groups[2].Value.Trim(); hasPrefix = true;
        }

        // 1. Barcode exato → scanner — adiciona direto (qtd do prefixo ou dialog)
        var exact = await _productService.GetByBarcodeAsync(term);
        if (exact != null)
        {
            if (hasPrefix)
                AddOrUpdateItem(exact, qty);
            else
            {
                var q = await AskQuantityAsync(exact);
                if (q.HasValue) AddOrUpdateItem(exact, q.Value);
            }
            ClearSearch();
            return;
        }

        // 2. Busca parcial
        var results = (await _productService.SearchAsync(term)).ToList();

        if (results.Count == 0)
        {
            StatusMessage = $"Nenhum produto encontrado para \"{term}\".";
            ShowSearchResults = false;
            return;
        }

        if (results.Count == 1)
        {
            decimal finalQty = hasPrefix ? qty : (await AskQuantityAsync(results[0]) ?? -1);
            if (finalQty <= 0) return;
            AddOrUpdateItem(results[0], finalQty);
            ClearSearch();
            return;
        }

        // Múltiplos resultados → exibe lista
        _pendingQty          = qty;
        _pendingHasQtyPrefix = hasPrefix;
        SearchResults        = new ObservableCollection<ProductDto>(results);
        ShowSearchResults    = true;
        SelectedResultIndex  = 0;
        StatusMessage            = string.Empty;
        FocusListRequested?.Invoke();
    }

    // ── Dialog de quantidade ─────────────────────────────────────────────
    private async Task<decimal?> AskQuantityAsync(ProductDto product, decimal defaultQty = 1)
    {
        // Pequena pausa para deixar o UI atualizar antes de abrir o dialog
        await Task.Yield();
        return _dialogService.AskQuantity(product, defaultQty);
    }

    // ── Confirmar item da lista ──────────────────────────────────────────
    public void MoveSelection(int delta)
    {
        if (!ShowSearchResults || SearchResults.Count == 0) return;
        var next = SelectedResultIndex + delta;
        if (next < 0) next = 0;
        if (next >= SearchResults.Count) next = SearchResults.Count - 1;
        SelectedResultIndex = next;
    }

    /// <summary>Called from code-behind (async fire-and-forget).</summary>
    public async Task ConfirmSelectedResultAsync()
    {
        if (!ShowSearchResults || SelectedResultIndex < 0 || SelectedResultIndex >= SearchResults.Count)
            return;

        var product  = SearchResults[SelectedResultIndex];
        decimal finalQty;

        if (_pendingHasQtyPrefix)
            finalQty = _pendingQty;
        else
        {
            var q = await AskQuantityAsync(product, _pendingQty);
            if (!q.HasValue) return;
            finalQty = q.Value;
        }

        _pendingQty          = 1;
        _pendingHasQtyPrefix = false;
        AddOrUpdateItem(product, finalQty);
        ClearSearch();
    }

    [RelayCommand]
    public void AddProduct(ProductDto product)
    {
        AddOrUpdateItem(product, _pendingQty);
        _pendingQty          = 1;
        _pendingHasQtyPrefix = false;
        ClearSearch();
    }

    private void AddOrUpdateItem(ProductDto product, decimal qty = 1)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
            existing.Quantity += qty;
        else
        {
            var item = new SaleItemLine
            {
                ProductId   = product.Id,
                ProductName = product.Name,
                Barcode     = product.Barcode,
                Quantity    = qty,
                UnitPrice   = product.SalePrice,
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
        StatusMessage = string.Empty;
        FocusSearchRequested?.Invoke();
    }

    private void ClearSearch()
    {
        _searchCts?.Cancel();
        SearchInput       = string.Empty;
        ShowSearchResults = false;
        SearchResults.Clear();
        StatusMessage = string.Empty;
    }

    // ── FEATURE 3: Finalizar + mostrar resumo ────────────────────────────
    [RelayCommand]
    public async Task FinalizeSaleAsync()
    {
        if (!Items.Any()) { StatusMessage = "Adicione pelo menos um produto."; return; }

        var payments = _dialogService.RequestPayment(Total);
        if (payments == null) return;

        IsBusy = true;
        try
        {
            var dto = new CreateSaleDto
            {
                CustomerId     = SelectedCustomer?.Id,
                DiscountAmount = DiscountAmount,
                Notes          = Notes,
                Items = Items.Select(i => new CreateSaleItemDto
                {
                    ProductId      = i.ProductId,
                    Quantity       = i.Quantity,
                    UnitPrice      = i.UnitPrice,
                    DiscountAmount = i.DiscountAmount
                }).ToList(),
                Payments = payments.ToList()
            };

            var sale = await _saleService.CreateSaleAsync(dto, SessionManager.CurrentUser!.Id);

            ClearSale();
            StatusMessage = $"✓ Venda {sale.SaleNumber} finalizada!";

            // Exibe cupom/resumo
            _dialogService.ShowReceipt(sale);
        }
        catch (Exception ex) { StatusMessage = $"Erro: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void ClearSale()
    {
        foreach (var item in Items) item.PropertyChanged -= OnItemPropertyChanged;
        Items.Clear();
        SelectedCustomer = null;
        DiscountAmount   = 0;
        Notes            = null;
        ClearSearch();
        NotifyTotals();
        FocusSearchRequested?.Invoke();
    }
}
