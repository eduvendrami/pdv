using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;

namespace PDV.WPF.ViewModels;

public partial class QuantityDialogViewModel : ObservableObject
{
    public string  ProductName { get; }
    public decimal SalePrice   { get; }
    public string  Unit        { get; }

    [ObservableProperty] private decimal _quantity = 1;
    [ObservableProperty] private string  _errorMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public QuantityDialogViewModel(ProductDto product, decimal defaultQty = 1)
    {
        ProductName = product.Name;
        SalePrice   = product.SalePrice;
        Unit        = product.UnitOfMeasure.ToString();
        Quantity    = defaultQty > 0 ? defaultQty : 1;
    }

    [RelayCommand]
    public void Confirm()
    {
        if (Quantity <= 0)
        {
            ErrorMessage = "Informe uma quantidade maior que zero.";
            return;
        }
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    public void Cancel() => RequestClose?.Invoke(false);
}
