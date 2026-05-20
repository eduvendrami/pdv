using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;

namespace PDV.WPF.ViewModels;

public partial class ProductListViewModel : BaseViewModel
{
    private readonly IProductService _productService;

    // ── Raw list from service (before client-side filter) ─────────────────
    private List<ProductDto> _allProducts = new();

    // ── Bound to DataGrid ─────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ProductDto> _products = new();

    // ── Filter state ──────────────────────────────────────────────────────
    [ObservableProperty] private string  _searchTerm       = string.Empty;
    [ObservableProperty] private bool    _showLowStockOnly = false;
    [ObservableProperty] private bool    _showInactive     = false;   // Admin only
    [ObservableProperty] private int?    _filterCategoryId = null;

    // ── Lookups ───────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CategoryDto> _categories = new();

    // ── Selection ─────────────────────────────────────────────────────────
    [ObservableProperty] private ProductDto? _selectedProduct;

    // ── Role-based permissions ────────────────────────────────────────────
    public bool CanEdit       => Helpers.SessionManager.CanEditProducts;
    public bool CanDelete     => Helpers.SessionManager.CanDeleteProducts;
    public bool CanToggle     => Helpers.SessionManager.CanDeleteProducts; // Admin only
    public bool CanSeeInactive => Helpers.SessionManager.IsAdmin;

    public ProductListViewModel(IProductService productService)
    {
        _productService = productService;
        _ = LoadAsync();
    }

    // ── Load + filter ─────────────────────────────────────────────────────
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            // Load categories for the filter combo
            var cats = await _productService.GetCategoriesAsync();
            Categories = new ObservableCollection<CategoryDto>(cats);

            // Load products — Admin can see inactive too
            IEnumerable<ProductDto> raw;
            if (Helpers.SessionManager.IsAdmin)
                raw = await _productService.GetAllIncludingInactiveAsync();
            else
                raw = await _productService.GetAllAsync();

            _allProducts = raw.ToList();
            ApplyFilter();
        }
        finally { IsBusy = false; }
    }

    private void ApplyFilter()
    {
        var list = _allProducts.AsEnumerable();

        // Active / inactive
        if (!ShowInactive || !Helpers.SessionManager.IsAdmin)
            list = list.Where(p => p.IsActive);

        // Text search
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim().ToLowerInvariant();
            list = list.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
             || (p.Barcode       != null && p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase))
             || (p.InternalCode  != null && p.InternalCode.Contains(term, StringComparison.OrdinalIgnoreCase))
             || (p.CategoryName  != null && p.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        // Category filter
        if (FilterCategoryId.HasValue)
            list = list.Where(p => p.CategoryId == FilterCategoryId);

        // Low-stock filter
        if (ShowLowStockOnly)
            list = list.Where(p => p.IsLowStock);

        Products = new ObservableCollection<ProductDto>(list.OrderBy(p => p.Name));
    }

    // Reapply filter when user changes any filter field
    partial void OnSearchTermChanged(string value)       => ApplyFilter();
    partial void OnShowLowStockOnlyChanged(bool value)   => ApplyFilter();
    partial void OnShowInactiveChanged(bool value)       => ApplyFilter();
    partial void OnFilterCategoryIdChanged(int? value)   => ApplyFilter();

    // ── Commands ──────────────────────────────────────────────────────────
    [RelayCommand]
    public void ClearFilters()
    {
        SearchTerm       = string.Empty;
        ShowLowStockOnly = false;
        ShowInactive     = false;
        FilterCategoryId = null;
    }

    [RelayCommand]
    public async Task NewProduct()
    {
        var vm = App.GetService<ProductEditViewModel>();
        await vm.InitializeAsync(null);
        var dialog = new Views.ProductEditDialog
        {
            DataContext = vm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }

    [RelayCommand]
    public async Task EditProduct(ProductDto? product)
    {
        if (product == null) return;
        var vm = App.GetService<ProductEditViewModel>();
        await vm.InitializeAsync(product);
        var dialog = new Views.ProductEditDialog
        {
            DataContext = vm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true)
            await LoadAsync();
    }

    [RelayCommand]
    public async Task ToggleActive(ProductDto? product)
    {
        if (product == null) return;

        var action  = product.IsActive ? "desativar" : "ativar";
        var icon    = product.IsActive ? System.Windows.MessageBoxImage.Warning
                                       : System.Windows.MessageBoxImage.Question;
        var confirm = System.Windows.MessageBox.Show(
            $"Deseja {action} o produto \"{product.Name}\"?",
            "Confirmar",
            System.Windows.MessageBoxButton.YesNo,
            icon);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        await _productService.SetActiveAsync(product.Id, !product.IsActive);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteProduct(ProductDto? product)
    {
        if (product == null) return;
        var result = System.Windows.MessageBox.Show(
            $"Excluir permanentemente o produto '{product.Name}'?\n\nEsta ação não pode ser desfeita.",
            "Confirmar exclusão",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            await _productService.DeleteAsync(product.Id);
            await LoadAsync();
        }
    }
}
