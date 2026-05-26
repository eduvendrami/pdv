using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.WPF.Helpers;
using PDV.WPF.Services;

namespace PDV.WPF.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private bool    _isMenuOpen;

    // ── User info (shown in top bar) ─────────────────────────────────────
    public string CurrentUserName => SessionManager.CurrentUser?.FullName ?? "Usuário";
    public string CurrentUserRole => SessionManager.RoleLabel;

    // ── Navigation visibility (bound in XAML) ───────────────────────────
    // Computed once per session — MainViewModel is created after login.
    public bool ShowManagement => SessionManager.CanAccessManagement;
    public bool ShowUsers      => SessionManager.CanAccessUsers;

    public MainViewModel()
    {
        NavigateToSale();
    }

    // ── Sales — all roles ────────────────────────────────────────────────
    [RelayCommand]
    public void NavigateToSale()
    {
        CurrentView = App.GetService<SaleViewModel>();
        IsMenuOpen  = false;
    }

    // ── Management — Gerente+ ────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(UserIsManager))]
    public void NavigateToProducts()
    {
        CurrentView = App.GetService<ProductListViewModel>();
        IsMenuOpen  = false;
    }

    [RelayCommand(CanExecute = nameof(UserIsManager))]
    public void NavigateToCustomers()
    {
        CurrentView = App.GetService<CustomerListViewModel>();
        IsMenuOpen  = false;
    }

    [RelayCommand(CanExecute = nameof(UserIsManager))]
    public void NavigateToSuppliers()
    {
        CurrentView = App.GetService<SupplierListViewModel>();
        IsMenuOpen  = false;
    }

    [RelayCommand(CanExecute = nameof(UserIsManager))]
    public void NavigateToStock()
    {
        CurrentView = App.GetService<StockViewModel>();
        IsMenuOpen  = false;
    }

    [RelayCommand(CanExecute = nameof(UserIsManager))]
    public void NavigateToCash()
    {
        CurrentView = App.GetService<CashViewModel>();
        IsMenuOpen  = false;
    }

    [RelayCommand(CanExecute = nameof(UserIsManager))]
    public void NavigateToReports()
    {
        CurrentView = App.GetService<ReportsViewModel>();
        IsMenuOpen  = false;
    }

    [RelayCommand(CanExecute = nameof(UserIsManager))]
    public void NavigateToNFeImport()
    {
        CurrentView = App.GetService<NFeImportViewModel>();
        IsMenuOpen  = false;
    }

    // ── Admin only ───────────────────────────────────────────────────────
    [RelayCommand(CanExecute = nameof(UserIsAdmin))]
    public void NavigateToUsers()
    {
        CurrentView = App.GetService<UserListViewModel>();
        IsMenuOpen  = false;
    }

    [RelayCommand(CanExecute = nameof(UserIsAdmin))]
    public void NavigateToBackup()
    {
        CurrentView = App.GetService<BackupViewModel>();
        IsMenuOpen  = false;
    }

    // ── CanExecute guards ────────────────────────────────────────────────
    private bool UserIsManager() => SessionManager.CanAccessManagement;
    private bool UserIsAdmin()   => SessionManager.CanAccessUsers;

    // ── Verificar atualizações ────────────────────────────────────────────
    [ObservableProperty] private bool _checkingUpdate = false;

    [RelayCommand]
    public async Task CheckUpdates()
    {
        CheckingUpdate = true;
        IsMenuOpen     = false;
        try
        {
            var svc     = new UpdateService();
            var release = await svc.CheckForUpdateAsync();

            if (release == null)
            {
                System.Windows.MessageBox.Show(
                    $"O sistema já está na versão mais recente ({UpdateService.CurrentVersion.ToString(3)}).",
                    "Sem atualizações",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            var win = new Views.UpdateProgressWindow(release)
            {
                Owner = App.GetMainWindow()
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Não foi possível verificar atualizações:\n{ex.Message}",
                "Erro",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        finally { CheckingUpdate = false; }
    }

    // ── Logout ───────────────────────────────────────────────────────────
    [RelayCommand]
    public void Logout()
    {
        SessionManager.Logout();
        var login = new Views.LoginWindow();
        login.Show();
        System.Windows.Application.Current.Windows.OfType<Views.MainWindow>().FirstOrDefault()?.Close();
    }
}
