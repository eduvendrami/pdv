using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Application.Services;
using PDV.Domain.Enums;

namespace PDV.WPF.ViewModels;

public partial class UserEditViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private int? _editingId;

    [ObservableProperty] private string _username  = string.Empty;
    [ObservableProperty] private string _fullName  = string.Empty;
    [ObservableProperty] private UserRole _role    = UserRole.Operador;
    [ObservableProperty] private string _dialogTitle = "Novo Usuário";
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isNewUser   = true;

    /// <summary>
    /// Populated by the View via PasswordBox.PasswordChanged — never bound directly.
    /// </summary>
    public string Password        { get; set; } = string.Empty;
    public string PasswordConfirm { get; set; } = string.Empty;

    public IEnumerable<UserRole> Roles => Enum.GetValues<UserRole>();

    /// <summary>
    /// View subscribes: sets DialogResult = result on the dialog window.
    /// </summary>
    public event Action<bool>? RequestClose;

    public UserEditViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    public void Initialize(UserDto? user)
    {
        if (user != null)
        {
            _editingId  = user.Id;
            IsNewUser   = false;
            DialogTitle = "Editar Usuário";
            Username    = user.Username;
            FullName    = user.FullName;
            Role        = user.Role;
        }
        else
        {
            _editingId  = null;
            IsNewUser   = true;
            DialogTitle = "Novo Usuário";
            Username    = string.Empty;
            FullName    = string.Empty;
            Role        = UserRole.Operador;
        }
        Password        = string.Empty;
        PasswordConfirm = string.Empty;
        ErrorMessage    = string.Empty;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        // ── Basic field validation ──────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "O campo Usuário é obrigatório.";
            return;
        }
        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "O campo Nome Completo é obrigatório.";
            return;
        }

        // ── Password validation ─────────────────────────────────────────────
        if (IsNewUser && string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "A senha é obrigatória para novos usuários.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(Password) && Password != PasswordConfirm)
        {
            ErrorMessage = "As senhas não conferem.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(Password) && Password.Length < 4)
        {
            ErrorMessage = "A senha deve ter pelo menos 4 caracteres.";
            return;
        }

        IsBusy = true;
        try
        {
            var dto = new CreateUserDto
            {
                Username = Username.Trim(),
                FullName = FullName.Trim(),
                Role     = Role,
                Password = Password
            };

            if (_editingId.HasValue)
                await _authService.UpdateUserAsync(_editingId.Value, dto);
            else
                await _authService.CreateUserAsync(dto);

            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            // Friendly message for common constraint violations
            var msg = ex.InnerException?.Message ?? ex.Message;
            ErrorMessage = msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                ? $"O nome de usuário \"{Username}\" já está em uso."
                : $"Erro ao salvar: {msg}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public void Cancel() => RequestClose?.Invoke(false);
}
