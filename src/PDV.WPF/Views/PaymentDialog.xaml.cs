using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PDV.WPF.Views;

public partial class PaymentDialog : Window
{
    public PaymentDialog()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Foco direto no primeiro campo de forma de pagamento ao abrir o diálogo.
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            var combo = FindFirstDescendant<ComboBox>(this);
            combo?.Focus();
        }));
    }

    private static T? FindFirstDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var found = FindFirstDescendant<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
    }

    private void Amount_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.SelectAll();
    }
}
