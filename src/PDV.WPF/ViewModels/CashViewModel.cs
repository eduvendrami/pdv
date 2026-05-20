using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;
using PDV.WPF.Helpers;

namespace PDV.WPF.ViewModels;

public partial class CashViewModel : BaseViewModel
{
    private readonly ICashService _cashService;

    [ObservableProperty] private CashSessionDto? _currentSession;
    [ObservableProperty] private decimal _openingBalance;
    [ObservableProperty] private decimal _closingBalance;
    [ObservableProperty] private string? _sessionNotes;
    [ObservableProperty] private decimal _movementAmount;
    [ObservableProperty] private CashMovementType _movementType = CashMovementType.Suprimento;
    [ObservableProperty] private string? _movementDescription;

    public bool IsOpen => CurrentSession != null;
    public bool IsClosed => CurrentSession == null;
    public IEnumerable<CashMovementType> MovementTypes => new[] { CashMovementType.Suprimento, CashMovementType.Sangria };

    public CashViewModel(ICashService cashService)
    {
        _cashService = cashService;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            CurrentSession = await _cashService.GetOpenSessionAsync();
            OnPropertyChanged(nameof(IsOpen));
            OnPropertyChanged(nameof(IsClosed));
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task OpenCashAsync()
    {
        IsBusy = true;
        try
        {
            CurrentSession = await _cashService.OpenSessionAsync(
                new OpenCashSessionDto { OpeningBalance = OpeningBalance },
                SessionManager.CurrentUser!.Id);
            OnPropertyChanged(nameof(IsOpen));
            OnPropertyChanged(nameof(IsClosed));
            SetStatus("Caixa aberto com sucesso!");
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task CloseCashAsync()
    {
        IsBusy = true;
        try
        {
            CurrentSession = await _cashService.CloseSessionAsync(
                new CloseCashSessionDto { ClosingBalance = ClosingBalance, Notes = SessionNotes },
                SessionManager.CurrentUser!.Id);
            OnPropertyChanged(nameof(IsOpen));
            OnPropertyChanged(nameof(IsClosed));
            SetStatus("Caixa fechado com sucesso!");
            await LoadAsync();
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task AddMovementAsync()
    {
        if (CurrentSession == null) { SetStatus("Nenhum caixa aberto."); return; }
        if (MovementAmount <= 0) { SetStatus("Informe um valor válido."); return; }

        IsBusy = true;
        try
        {
            await _cashService.AddMovementAsync(new CashSupplyDto
            {
                Amount = MovementAmount,
                Type = MovementType,
                Description = MovementDescription
            }, CurrentSession.Id);
            SetStatus("Movimento registrado.");
            MovementAmount = 0;
            MovementDescription = null;
            await LoadAsync();
        }
        catch (Exception ex) { SetStatus($"Erro: {ex.Message}"); }
        finally { IsBusy = false; }
    }
}
