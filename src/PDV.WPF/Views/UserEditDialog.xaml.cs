using System.Windows;
using System.Windows.Input;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class UserEditDialog : Window
{
    private UserEditViewModel? _vm;

    public UserEditDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
    }

    private void BindViewModel()
    {
        if (_vm != null)
            _vm.RequestClose -= OnRequestClose;

        _vm = DataContext as UserEditViewModel;

        if (_vm != null)
            _vm.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(bool result)
    {
        DialogResult = result;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BindViewModel();
        // Auto-focus the username field on open
        Dispatcher.BeginInvoke(() => TxtUsername.Focus());
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _vm?.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }

    // Bridge PasswordBox values to ViewModel properties (PasswordBox can't bind directly)
    private void PwdPassword_Changed(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.Password = PwdPassword.Password;
    }

    private void PwdConfirm_Changed(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.PasswordConfirm = PwdConfirm.Password;
    }
}
