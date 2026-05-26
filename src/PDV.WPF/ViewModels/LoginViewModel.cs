using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.WPF.Helpers;
using System.Windows.Controls;

namespace PDV.WPF.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoginAsync(object parameter)
    {
        ErrorMessage = string.Empty;

        var passwordBox = parameter as PasswordBox;
        var password = passwordBox?.Password;

        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Informe usuário e senha.";
            return;
        }

        IsBusy = true;
        try
        {
            var user = await _authService.LoginAsync(new LoginDto { Username = Username, Password = password });
            if (user == null)
            {
                ErrorMessage = "Usuário ou senha inválidos.";
                return;
            }
            SessionManager.Login(user);
            var main = new Views.MainWindow();
            main.Show();
            System.Windows.Application.Current.Windows.OfType<Views.LoginWindow>().FirstOrDefault()?.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
