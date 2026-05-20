using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;

namespace PDV.WPF.ViewModels;

public partial class SetupViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string _fullName     = string.Empty;
    [ObservableProperty] private string _username     = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>Populado pelo code-behind via PasswordBox.PasswordChanged.</summary>
    public string Password        { get; set; } = string.Empty;
    public string PasswordConfirm { get; set; } = string.Empty;

    /// <summary>
    /// Disparado quando o admin foi criado com sucesso.
    /// O code-behind abre o LoginWindow e fecha esta janela.
    /// </summary>
    public event Action? SetupCompleted;

    public SetupViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Informe seu nome completo.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Escolha um nome de usuário.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Defina uma senha.";
            return;
        }
        if (Password.Length < 4)
        {
            ErrorMessage = "A senha deve ter pelo menos 4 caracteres.";
            return;
        }
        if (Password != PasswordConfirm)
        {
            ErrorMessage = "As senhas não conferem.";
            return;
        }

        IsBusy = true;
        try
        {
            await _authService.CreateUserAsync(new CreateUserDto
            {
                Username = Username.Trim(),
                FullName = FullName.Trim(),
                Password = Password,
                Role     = UserRole.Administrador
            });

            SetupCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            ErrorMessage = msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                ? $"O usuário \"{Username}\" já existe. Escolha outro nome."
                : $"Erro ao criar conta: {msg}";
        }
        finally { IsBusy = false; }
    }
}
