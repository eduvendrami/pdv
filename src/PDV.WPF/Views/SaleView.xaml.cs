using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class SaleView : UserControl
{
    private SaleViewModel? _vm;

    public SaleView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
    }

    private void BindViewModel()
    {
        if (_vm != null)
        {
            _vm.FocusSearchRequested -= FocusSearch;
            _vm.FocusListRequested   -= FocusListAndScroll;
        }

        _vm = DataContext as SaleViewModel;

        if (_vm != null)
        {
            _vm.FocusSearchRequested += FocusSearch;
            _vm.FocusListRequested   += FocusListAndScroll;
        }
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        BindViewModel();
        FocusSearch();
        ApplyColumnPermissions();
    }

    /// <summary>
    /// Hides the per-item discount column for users without discount permission.
    /// DataGridColumn.Visibility has no DataContext so we set it from code-behind.
    /// </summary>
    private void ApplyColumnPermissions()
    {
        if (_vm == null) return;
        var discCol = GridItems.Columns
            .FirstOrDefault(c => c.Header?.ToString() == "Desc.");
        if (discCol != null)
            discCol.Visibility = _vm.CanEditItemDiscount
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
    }

    private void FocusSearch()
    {
        Dispatcher.BeginInvoke(() =>
        {
            TxtSearch.Focus();
            TxtSearch.SelectAll();
        });
    }

    private void FocusList()
    {
        Dispatcher.BeginInvoke(() => LstResults.Focus());
    }

    private void FocusListAndScroll()
    {
        Dispatcher.BeginInvoke(() =>
        {
            LstResults.Focus();
            ScrollResultIntoView();
        });
    }

    // ── Campo de busca ──────────────────────────────────────────────────────

    private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        switch (e.Key)
        {
            case Key.Enter:
                if (_vm.ShowSearchResults)
                    _vm.ConfirmSelectedResult();
                else
                    _vm.SearchCommand.Execute(null);
                e.Handled = true;
                break;

            // Seta para baixo: move foco direto para a lista e navega
            case Key.Down when _vm.ShowSearchResults:
                _vm.MoveSelection(+1);
                FocusList();
                ScrollResultIntoView();
                e.Handled = true;
                break;

            case Key.Up when _vm.ShowSearchResults:
                _vm.MoveSelection(-1);
                FocusList();
                ScrollResultIntoView();
                e.Handled = true;
                break;

            case Key.Escape when _vm.ShowSearchResults:
                _vm.ClearSearchResultsCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F9:
                _vm.FinalizeSaleCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F4:
                _vm.ClearSaleCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    // ── Lista de resultados ─────────────────────────────────────────────────

    // Clique com o mouse: a lista já recebe foco naturalmente ao ser clicada.
    // Garantimos foco explícito para que as setas funcionem imediatamente.
    private void LstResults_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        LstResults.Focus();
    }

    // Duplo clique num item: adiciona ao carrinho
    private void LstResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm == null) return;
        _vm.ConfirmSelectedResult();
        e.Handled = true;
    }

    private void LstResults_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        switch (e.Key)
        {
            case Key.Enter:
                _vm.ConfirmSelectedResult();
                e.Handled = true;
                break;

            case Key.Escape:
                _vm.ClearSearchResultsCommand.Execute(null);
                FocusSearch();
                e.Handled = true;
                break;

            // Seta chega ao topo da lista: devolve foco ao campo de busca
            case Key.Up when LstResults.SelectedIndex <= 0:
                _vm.MoveSelection(-1);
                FocusSearch();
                e.Handled = true;
                break;
        }
    }

    private void ScrollResultIntoView()
    {
        if (LstResults.SelectedItem != null)
            LstResults.ScrollIntoView(LstResults.SelectedItem);
    }

    // ── Carrinho ────────────────────────────────────────────────────────────

    private void GridItems_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm == null) return;

        switch (e.Key)
        {
            case Key.Delete:
                if (GridItems.SelectedItem is SaleItemLine item)
                    _vm.RemoveItemCommand.Execute(item);
                e.Handled = true;
                break;

            case Key.Escape:
                FocusSearch();
                e.Handled = true;
                break;

            case Key.F9:
                _vm.FinalizeSaleCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F4:
                _vm.ClearSaleCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
