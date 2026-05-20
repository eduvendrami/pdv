using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.WPF.Helpers;

namespace PDV.WPF.ViewModels;

public partial class SupplierEditViewModel : BaseViewModel
{
    private readonly ISupplierService _supplierService;
    private int? _editingId;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _tradeName;
    [ObservableProperty] private string? _cnpj;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _city;
    [ObservableProperty] private string? _state;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string _dialogTitle = "Novo Fornecedor";
    [ObservableProperty] private string? _errorMessage;

    public SupplierEditViewModel(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    public void Initialize(SupplierDto? supplier)
    {
        if (supplier != null)
        {
            _editingId = supplier.Id;
            DialogTitle = "Editar Fornecedor";
            Name = supplier.Name;
            TradeName = supplier.TradeName;
            Cnpj = supplier.Cnpj;
            Phone = supplier.Phone;
            Email = supplier.Email;
            Address = supplier.Address;
            City = supplier.City;
            State = supplier.State;
            Notes = supplier.Notes;
        }
        else
        {
            _editingId = null;
            DialogTitle = "Novo Fornecedor";
        }
    }

    [RelayCommand]
    public async Task SaveAsync(System.Windows.Window window)
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Nome é obrigatório."; return; }
        IsBusy = true;
        try
        {
            var dto = new CreateSupplierDto
            {
                Name = Name,
                TradeName = TradeName.NullIfEmpty(),
                Cnpj = Cnpj.NullIfEmpty(),
                Phone = Phone.NullIfEmpty(),
                Email = Email.NullIfEmpty(),
                Address = Address.NullIfEmpty(),
                City = City.NullIfEmpty(),
                State = State.NullIfEmpty(),
                Notes = Notes.NullIfEmpty()
            };
            if (_editingId.HasValue)
                await _supplierService.UpdateAsync(_editingId.Value, dto);
            else
                await _supplierService.CreateAsync(dto);
            window.DialogResult = true;
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void Cancel(System.Windows.Window window) => window.DialogResult = false;
}
