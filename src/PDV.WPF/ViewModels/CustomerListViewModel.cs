using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;

namespace PDV.WPF.ViewModels;

public partial class CustomerListViewModel : BaseViewModel
{
    private readonly ICustomerService _customerService;

    [ObservableProperty] private ObservableCollection<CustomerDto> _customers = new();
    [ObservableProperty] private string _searchTerm = string.Empty;

    public CustomerListViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = string.IsNullOrWhiteSpace(SearchTerm)
                ? await _customerService.GetAllAsync()
                : await _customerService.SearchAsync(SearchTerm);
            Customers = new ObservableCollection<CustomerDto>(list);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void NewCustomer()
    {
        var vm = App.GetService<CustomerEditViewModel>();
        vm.Initialize(null);
        var dialog = new Views.CustomerEditDialog { DataContext = vm };
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    public void EditCustomer(CustomerDto? customer)
    {
        if (customer == null) return;
        var vm = App.GetService<CustomerEditViewModel>();
        vm.Initialize(customer);
        var dialog = new Views.CustomerEditDialog { DataContext = vm };
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteCustomerAsync(CustomerDto? customer)
    {
        if (customer == null) return;
        var r = System.Windows.MessageBox.Show($"Excluir cliente '{customer.Name}'?", "Confirmar",
            System.Windows.MessageBoxButton.YesNo);
        if (r == System.Windows.MessageBoxResult.Yes)
        {
            await _customerService.DeleteAsync(customer.Id);
            await LoadAsync();
        }
    }
}
