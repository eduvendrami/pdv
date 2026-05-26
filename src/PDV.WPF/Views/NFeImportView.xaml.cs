using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class NFeImportView : UserControl
{
    private NFeImportViewModel? _vm;

    public NFeImportView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.ItemsLoaded -= FocusFirstItem;
        _vm = e.NewValue as NFeImportViewModel;
        if (_vm != null) _vm.ItemsLoaded += FocusFirstItem;
    }

    // Seleciona e dá foco ao primeiro item da lista, para começar pelo teclado sem mouse.
    private void FocusFirstItem()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (ItemsGrid.Items.Count == 0) return;

            ItemsGrid.SelectedIndex = 0;
            ItemsGrid.ScrollIntoView(ItemsGrid.Items[0]);

            // Posiciona a célula atual na coluna de preço de venda da primeira linha.
            var priceColumn = ItemsGrid.Columns.FirstOrDefault(c => (c.Header as string) == "Venda (R$)")
                              ?? ItemsGrid.Columns.LastOrDefault(c => !c.IsReadOnly);
            if (priceColumn != null)
                ItemsGrid.CurrentCell = new DataGridCellInfo(ItemsGrid.Items[0], priceColumn);

            ItemsGrid.Focus();
            ItemsGrid.BeginEdit();
        }));
    }

    private void EditingText_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            tb.SelectAll();
    }
}
