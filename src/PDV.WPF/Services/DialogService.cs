using System.Collections.Generic;
using PDV.Application.DTOs;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Services;

/// <summary>
/// Implementação WPF de <see cref="IDialogService"/>. Centraliza a criação das
/// janelas modais e a definição do Owner (sempre a MainWindow ativa).
/// </summary>
public sealed class DialogService : IDialogService
{
    public decimal? AskQuantity(ProductDto product, decimal defaultQty = 1)
    {
        var vm = new QuantityDialogViewModel(product, defaultQty);
        var dialog = new Views.QuantityDialog
        {
            DataContext = vm,
            Owner = App.GetMainWindow()
        };
        return dialog.ShowDialog() == true ? vm.Quantity : null;
    }

    public IReadOnlyList<PaymentDto>? RequestPayment(decimal total)
    {
        var vm = new PaymentDialogViewModel(total);
        var dialog = new Views.PaymentDialog
        {
            DataContext = vm,
            Owner = App.GetMainWindow()
        };
        return dialog.ShowDialog() == true ? vm.GetPayments() : null;
    }

    public void ShowReceipt(SaleDto sale)
    {
        var receipt = new Views.SaleReceiptWindow(sale) { Owner = App.GetMainWindow() };
        receipt.ShowDialog();
    }
}
