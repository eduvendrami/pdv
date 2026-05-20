using System.Windows;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        DataContext = App.GetService<LoginViewModel>();
    }
}
