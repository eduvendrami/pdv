using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PDV.Application.DTOs;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class ProductListView : UserControl
{
    public ProductListView()
    {
        InitializeComponent();
    }

    // ── Clique simples faz toggle (sem apagar seleção anterior) ──────────
    private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Só intercepta se o clique veio de dentro de uma célula de dados
        var cell = FindVisualParent<DataGridCell>((DependencyObject)e.OriginalSource);
        if (cell == null) return;                           // header, borda, botão — deixa passar

        // Botões dentro da linha (Editar / Toggle / Excluir) não devem mudar seleção
        if (FindVisualParent<Button>((DependencyObject)e.OriginalSource) != null) return;

        var row = FindVisualParent<DataGridRow>(cell);
        if (row == null) return;

        row.IsSelected = !row.IsSelected;   // toggle sem limpar os outros
        e.Handled = true;                   // impede o DataGrid de "desmarcar tudo"
    }

    // ── Sincroniza SelectedItems com o ViewModel ─────────────────────────
    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        var grid = (DataGrid)sender;
        vm.SelectedProducts = grid.SelectedItems.OfType<ProductDto>().ToList();
        vm.SelectedCount    = vm.SelectedProducts.Count;
    }

    // ── Selecionar / desmarcar todos ─────────────────────────────────────
    private void SelectAll_Click(object sender, RoutedEventArgs e)    => ProductGrid.SelectAll();
    private void DeselectAll_Click(object sender, RoutedEventArgs e)  => ProductGrid.UnselectAll();

    // ── Helper: sobe a árvore visual até encontrar T ──────────────────────
    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T t) return t;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
