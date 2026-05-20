using System.Windows;
using System.Windows.Input;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel? _vm;

    public SetupWindow()
    {
        InitializeComponent();
        DataContext = App.GetService<SetupViewModel>();
        DataContextChanged += (_, _) => BindViewModel();
    }

    private void BindViewModel()
    {
        if (_vm != null) _vm.SetupCompleted -= OnSetupCompleted;
        _vm = DataContext as SetupViewModel;
        if (_vm != null) _vm.SetupCompleted += OnSetupCompleted;
    }

    private void OnSetupCompleted()
    {
        // Abre o login antes de fechar para não disparar shutdown
        new LoginWindow().Show();
        Close();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BindViewModel();
        Dispatcher.BeginInvoke(() => TxtFullName.Focus());
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Esc não fecha — usuário obrigatoriamente deve fazer o setup
        e.Handled = e.Key == Key.Escape;
    }

    private void PwdPassword_Changed(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.Password = PwdPassword.Password;
    }

    private void PwdConfirm_Changed(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.PasswordConfirm = PwdConfirm.Password;
    }
}
