using CommunityToolkit.Mvvm.ComponentModel;

namespace PDV.WPF.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    protected void SetStatus(string message) => StatusMessage = message;
}
