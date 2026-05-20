using System.Windows;
using System.Windows.Media.Animation;

namespace PDV.WPF.Helpers;

/// <summary>
/// Animates a GridLength value (used to slide sidebar columns in/out).
/// </summary>
public class GridLengthAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public IEasingFunction? EasingFunction { get; set; }

    public override Type TargetPropertyType => typeof(GridLength);

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();

    public override object GetCurrentValue(object defaultOriginValue,
                                           object defaultDestinationValue,
                                           AnimationClock animationClock)
    {
        if (animationClock.CurrentProgress is null) return To;

        double progress = animationClock.CurrentProgress.Value;
        if (EasingFunction != null)
            progress = EasingFunction.Ease(progress);

        double from = From.IsAuto ? 0 : From.Value;
        double to   = To.IsAuto   ? 0 : To.Value;
        double current = from + (to - from) * progress;

        return new GridLength(current, To.IsAuto ? GridUnitType.Auto : To.GridUnitType);
    }
}
