using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;

namespace PDV.WPF.ViewModels;

public partial class UserListViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty] private ObservableCollection<UserDto> _users = new();

    public UserListViewModel(IAuthService authService)
    {
        _authService = authService;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _authService.GetUsersAsync();
            Users = new ObservableCollection<UserDto>(list);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void NewUser()
    {
        var vm = App.GetService<UserEditViewModel>();
        vm.Initialize(null);
        var dialog = new Views.UserEditDialog
        {
            DataContext = vm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    public void EditUser(UserDto? user)
    {
        if (user == null) return;
        var vm = App.GetService<UserEditViewModel>();
        vm.Initialize(user);
        var dialog = new Views.UserEditDialog
        {
            DataContext = vm,
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() == true) _ = LoadAsync();
    }

    [RelayCommand]
    public async Task DeleteUserAsync(UserDto? user)
    {
        if (user == null) return;
        var r = System.Windows.MessageBox.Show($"Excluir usuário '{user.Username}'?", "Confirmar",
            System.Windows.MessageBoxButton.YesNo);
        if (r == System.Windows.MessageBoxResult.Yes)
        {
            await _authService.DeleteUserAsync(user.Id);
            await LoadAsync();
        }
    }
}
