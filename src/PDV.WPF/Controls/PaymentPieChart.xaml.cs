using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PDV.WPF.Controls;

/// <summary>
/// Gráfico de pizza desenhado em WPF puro (sem dependências externas).
/// Recebe um Dictionary&lt;string, decimal&gt; em <see cref="Source"/> — chave = forma de
/// pagamento (nome do enum), valor = total.
/// </summary>
public partial class PaymentPieChart : UserControl
{
    private static readonly Color[] Palette =
    {
        Color.FromRgb(0x42, 0x85, 0xF4), // azul
        Color.FromRgb(0x34, 0xA8, 0x53), // verde
        Color.FromRgb(0xFB, 0xBC, 0x05), // amarelo
        Color.FromRgb(0xEA, 0x43, 0x35), // vermelho
        Color.FromRgb(0x9C, 0x27, 0xB0), // roxo
        Color.FromRgb(0x00, 0xAC, 0xC1), // ciano
        Color.FromRgb(0xFF, 0x70, 0x43), // laranja
    };

    private static readonly CultureInfo PtBR = new("pt-BR");

    public PaymentPieChart()
    {
        InitializeComponent();
        Loaded += (_, _) => Redraw();
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(object), typeof(PaymentPieChart),
            new PropertyMetadata(null, OnSourceChanged));

    public object? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PaymentPieChart)d).Redraw();

    private sealed class LegendItem
    {
        public Brush Brush { get; init; } = Brushes.Gray;
        public string Label { get; init; } = string.Empty;
        public string ValueText { get; init; } = string.Empty;
        public string PercentText { get; init; } = string.Empty;
    }

    private void Redraw()
    {
        if (ChartCanvas == null) return;

        ChartCanvas.Children.Clear();
        Legend.ItemsSource = null;

        var data = (Source as IDictionary<string, decimal>)?
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .ToList();

        var total = data?.Sum(kv => kv.Value) ?? 0m;

        if (data == null || data.Count == 0 || total <= 0)
        {
            EmptyLabel.Visibility = Visibility.Visible;
            return;
        }
        EmptyLabel.Visibility = Visibility.Collapsed;

        const double size = 190, r = 92, cx = 95, cy = 95;
        var legend = new List<LegendItem>();

        // Caso especial: uma única fatia = círculo completo (ArcSegment não fecha 360°).
        if (data.Count == 1)
        {
            var color = Palette[0];
            ChartCanvas.Children.Add(new Ellipse
            {
                Width = size, Height = size, Fill = new SolidColorBrush(color)
            });
            legend.Add(BuildLegend(color, data[0].Key, data[0].Value, 1d));
            Legend.ItemsSource = legend;
            return;
        }

        double startAngle = 0;
        for (int i = 0; i < data.Count; i++)
        {
            var fraction = (double)(data[i].Value / total);
            var sweep = fraction * 360.0;
            var endAngle = startAngle + sweep;
            var color = Palette[i % Palette.Length];

            var p1 = PointOnCircle(cx, cy, r, startAngle);
            var p2 = PointOnCircle(cx, cy, r, endAngle);

            var figure = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
            figure.Segments.Add(new LineSegment(p1, true));
            figure.Segments.Add(new ArcSegment(p2, new Size(r, r), 0, sweep > 180,
                SweepDirection.Clockwise, true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            ChartCanvas.Children.Add(new Path
            {
                Data = geometry,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1
            });

            legend.Add(BuildLegend(color, data[i].Key, data[i].Value, fraction));
            startAngle = endAngle;
        }

        Legend.ItemsSource = legend;
    }

    private static LegendItem BuildLegend(Color color, string key, decimal value, double fraction) => new()
    {
        Brush = new SolidColorBrush(color),
        Label = FriendlyName(key),
        ValueText = value.ToString("C2", PtBR),
        PercentText = $"  ({fraction:P0})"
    };

    private static Point PointOnCircle(double cx, double cy, double r, double angleDegrees)
    {
        var rad = (angleDegrees - 90) * Math.PI / 180.0;
        return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    private static string FriendlyName(string enumName) => enumName switch
    {
        "Dinheiro"      => "Dinheiro",
        "CartaoDebito"  => "Cartão Débito",
        "CartaoCredito" => "Cartão Crédito",
        "Pix"           => "Pix",
        "Boleto"        => "Boleto",
        "Cheque"        => "Cheque",
        "Crediario"     => "Crediário",
        _               => enumName
    };
}
