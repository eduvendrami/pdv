using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;

namespace PDV.WPF.ViewModels;

public partial class SupplierListViewModel : BaseViewModel
{
    private readonly ISupplierService _supplierService;

    [ObservableProperty] private ObservableCollection<SupplierDto> _suppliers = new();
    [ObservableProperty] private string _searchTerm = string.Empty;

    public SupplierListViewModel(ISupplierService supplierService)
    {
        _supplierService = supplierService;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _supplierService.GetAllAsync();
            Suppliers = new ObservableCollection<SupplierDto>(list);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void NewSupplier()
    {
        var vm = App.GetService<SupplierEditViewModel>();
        vm.Initialize(null);
        var dialog = new Views.SupplierEditDialog { DataContext = vm };
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    public void EditSupplier(SupplierDto? supplier)
    {
        if (supplier == null) return;
        var vm = App.GetService<SupplierEditViewModel>();
        vm.Initialize(supplier);
        var dialog = new Views.SupplierEditDialog { DataContext = vm };
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteSupplierAsync(SupplierDto? supplier)
    {
        if (supplier == null) return;
        var r = System.Windows.MessageBox.Show($"Excluir fornecedor '{supplier.Name}'?", "Confirmar",
            System.Windows.MessageBoxButton.YesNo);
        if (r == System.Windows.MessageBoxResult.Yes)
        {
            await _supplierService.DeleteAsync(supplier.Id);
            await LoadAsync();
        }
    }
}
