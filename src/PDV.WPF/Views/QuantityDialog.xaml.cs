using System.Windows;
using System.Windows.Input;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class QuantityDialog : Window
{
    private QuantityDialogViewModel? _vm;

    public QuantityDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
    }

    private void BindViewModel()
    {
        if (_vm != null) _vm.RequestClose -= OnRequestClose;
        _vm = DataContext as QuantityDialogViewModel;
        if (_vm != null) _vm.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(bool result) => DialogResult = result;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BindViewModel();
        Dispatcher.BeginInvoke(() =>
        {
            TxtQty.Focus();
            TxtQty.SelectAll();
        });
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { _vm?.CancelCommand.Execute(null); e.Handled = true; }
    }
}
