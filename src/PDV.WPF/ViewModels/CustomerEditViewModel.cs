using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.WPF.Helpers;

namespace PDV.WPF.ViewModels;

public partial class CustomerEditViewModel : BaseViewModel
{
    private readonly ICustomerService _customerService;
    private int? _editingId;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _cpfCnpj;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _state;
    [ObservableProperty] private decimal _creditLimit;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string _dialogTitle = "Novo Cliente";
    [ObservableProperty] private string? _errorMessage;

    public CustomerEditViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public void Initialize(CustomerDto? customer)
    {
        if (customer != null)
        {
            _editingId = customer.Id;
            DialogTitle = "Editar Cliente";
            Name = customer.Name;
            CpfCnpj = customer.CpfCnpj;
            Phone = customer.Phone;
            Email = customer.Email;
            Address = customer.Address;
            City = customer.City;
            State = customer.State;
            CreditLimit = customer.CreditLimit;
            Notes = customer.Notes;
        }
        else
        {
            _editingId = null;
            DialogTitle = "Novo Cliente";
        }
    }

    [RelayCommand]
    public async Task SaveAsync(System.Windows.Window window)
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Nome é obrigatório.";
            return;
        }
        IsBusy = true;
        try
        {
            var dto = new CreateCustomerDto
            {
                Name = Name,
                CpfCnpj = CpfCnpj.NullIfEmpty(),
                Phone = Phone.NullIfEmpty(),
                Email = Email.NullIfEmpty(),
                Address = Address.NullIfEmpty(),
                City = City.NullIfEmpty(),
                State = State.NullIfEmpty(),
                CreditLimit = CreditLimit,
                Notes = Notes.NullIfEmpty()
            };
            if (_editingId.HasValue)
                await _customerService.UpdateAsync(_editingId.Value, dto);
            else
                await _customerService.CreateAsync(dto);
            window.DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void Cancel(System.Windows.Window window) => window.DialogResult = false;
}
