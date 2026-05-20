using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Application.DTOs;
using PDV.Domain.Enums;

namespace PDV.WPF.ViewModels;

public partial class PaymentDialogViewModel : ObservableObject
{
    public decimal TotalDue { get; }

    public ObservableCollection<PaymentLine> Payments { get; } = new();

    public decimal TotalPaid => Payments.Sum(p => p.Amount);
    public decimal Change => TotalPaid - TotalDue;
    public bool CanConfirm => TotalPaid >= TotalDue;

    public IEnumerable<PaymentMethod> Methods => Enum.GetValues<PaymentMethod>();

    public PaymentDialogViewModel(decimal totalDue)
    {
        TotalDue = totalDue;
        AddLine(totalDue);
    }

    private void AddLine(decimal amount)
    {
        var line = new PaymentLine { Amount = amount };
        line.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(TotalPaid));
            OnPropertyChanged(nameof(Change));
            OnPropertyChanged(nameof(CanConfirm));
            AddPaymentLineCommand.NotifyCanExecuteChanged();
            ConfirmCommand.NotifyCanExecuteChanged();
        };
        Payments.Add(line);
    }

    [RelayCommand]
    public void AddPaymentLine()
    {
        var remaining = TotalDue - TotalPaid;
        AddLine(remaining > 0 ? remaining : 0);
    }

    [RelayCommand]
    public void RemoveLine(PaymentLine line)
    {
        if (Payments.Count <= 1) return;
        Payments.Remove(line);
        OnPropertyChanged(nameof(TotalPaid));
        OnPropertyChanged(nameof(Change));
        OnPropertyChanged(nameof(CanConfirm));
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    public void Confirm(System.Windows.Window window) => window.DialogResult = true;

    [RelayCommand]
    public void Cancel(System.Windows.Window window) => window.DialogResult = false;

    public List<PaymentDto> GetPayments() =>
        Payments.Select(p => new PaymentDto { Method = p.Method, Amount = p.Amount }).ToList();
}
