using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PDV.WPF.Helpers;
using PDV.WPF.ViewModels;

namespace PDV.WPF.Views;

public partial class MainWindow : Window
{
    private MainViewModel _vm = null!;

    public MainWindow()
    {
        InitializeComponent();
        _vm = App.GetService<MainViewModel>();
        DataContext = _vm;

        // Watch IsMenuOpen to animate the sidebar column width
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsMenuOpen))
                AnimateSidebar(_vm.IsMenuOpen);
        };
    }

    private void AnimateSidebar(bool open)
    {
        double targetWidth = open ? 242 : 0;
        var anim = new GridLengthAnimation
        {
            From = SidebarColumn.Width,
            To   = new GridLength(targetWidth),
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, anim);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.F2)      _vm.NavigateToSaleCommand.Execute(null);
        if (e.Key == Key.Escape && _vm.IsMenuOpen) _vm.IsMenuOpen = false;
    }
}
