using CommunityToolkit.Mvvm.ComponentModel;
using PDV.Domain.Enums;

namespace PDV.WPF.ViewModels;

public partial class PaymentLine : ObservableObject
{
    [ObservableProperty] private PaymentMethod _method = PaymentMethod.Dinheiro;
    [ObservableProperty] private decimal _amount;
}
