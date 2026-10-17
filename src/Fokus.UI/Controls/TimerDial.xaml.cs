using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace Fokus.UI.Controls;

public partial class TimerDial : UserControl
{
    public static readonly DependencyProperty TimeTextProperty =
        DependencyProperty.Register(nameof(TimeText), typeof(string), typeof(TimerDial), new PropertyMetadata("25:00"));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(TimerDial), new PropertyMetadata(1.0, OnProgressChanged));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(TimerDial), new PropertyMetadata("Concentración", OnStatusChanged));

    public static readonly DependencyProperty TaskNameProperty =
        DependencyProperty.Register(nameof(TaskName), typeof(string), typeof(TimerDial), new PropertyMetadata(string.Empty, OnTaskChanged));

    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(TimerDial), new PropertyMetadata(false, OnRunningChanged));

    public static readonly DependencyProperty IsBreakProperty =
        DependencyProperty.Register(nameof(IsBreak), typeof(bool), typeof(TimerDial), new PropertyMetadata(false, OnBreakChanged));

    public static readonly DependencyProperty SessionCountProperty =
        DependencyProperty.Register(nameof(SessionCount), typeof(int), typeof(TimerDial), new PropertyMetadata(4, OnSessionsChanged));

    public static readonly DependencyProperty SessionIndexProperty =
        DependencyProperty.Register(nameof(SessionIndex), typeof(int), typeof(TimerDial), new PropertyMetadata(0, OnSessionsChanged));

    public static readonly DependencyProperty IsFlowmodoroProperty =
        DependencyProperty.Register(nameof(IsFlowmodoro), typeof(bool), typeof(TimerDial), new PropertyMetadata(false, OnSessionsChanged));

    public static readonly DependencyProperty TimeFontWeightProperty =
        DependencyProperty.Register(nameof(TimeFontWeight), typeof(FontWeight), typeof(TimerDial), new PropertyMetadata(FontWeights.SemiBold));

    public static readonly DependencyProperty TimeFontFamilyProperty =
        DependencyProperty.Register(nameof(TimeFontFamily), typeof(FontFamily), typeof(TimerDial), new PropertyMetadata(null));

    public static readonly DependencyProperty DialColorProperty =
        DependencyProperty.Register(nameof(DialColor), typeof(Color), typeof(TimerDial), new PropertyMetadata(Color.FromRgb(235, 75, 85)));

    public static readonly DependencyProperty DialBrushProperty =
        DependencyProperty.Register(nameof(DialBrush), typeof(Brush), typeof(TimerDial), new PropertyMetadata(null));

    public static readonly DependencyProperty PlayPauseIconProperty =
        DependencyProperty.Register(nameof(PlayPauseIcon), typeof(IconElement), typeof(TimerDial), new PropertyMetadata(null));

    public static readonly DependencyProperty TaskVisibilityProperty =
        DependencyProperty.Register(nameof(TaskVisibility), typeof(Visibility), typeof(TimerDial), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty DotsVisibilityProperty =
        DependencyProperty.Register(nameof(DotsVisibility), typeof(Visibility), typeof(TimerDial), new PropertyMetadata(Visibility.Visible));

    public event RoutedEventHandler? PlayPauseClick;
    public event RoutedEventHandler? StopClick;
    public event RoutedEventHandler? SkipClick;

    public TimerDial()
    {
        InitializeComponent();
        TimeFontFamily = new FontFamily("Segoe UI");
        PlayPauseIcon = new SymbolIcon(SymbolRegular.Play24);
        UpdateColors();
        Loaded += (_, _) =>
        {
            UpdateArc();
            UpdateDots();
        };
    }

    public string TimeText { get => (string)GetValue(TimeTextProperty); set => SetValue(TimeTextProperty, value); }
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public string StatusText { get => (string)GetValue(StatusTextProperty); set => SetValue(StatusTextProperty, value); }
    public string TaskName { get => (string)GetValue(TaskNameProperty); set => SetValue(TaskNameProperty, value); }
    public bool IsRunning { get => (bool)GetValue(IsRunningProperty); set => SetValue(IsRunningProperty, value); }
    public bool IsBreak { get => (bool)GetValue(IsBreakProperty); set => SetValue(IsBreakProperty, value); }
    public int SessionCount { get => (int)GetValue(SessionCountProperty); set => SetValue(SessionCountProperty, value); }
    public int SessionIndex { get => (int)GetValue(SessionIndexProperty); set => SetValue(SessionIndexProperty, value); }
    public bool IsFlowmodoro { get => (bool)GetValue(IsFlowmodoroProperty); set => SetValue(IsFlowmodoroProperty, value); }
    public FontWeight TimeFontWeight { get => (FontWeight)GetValue(TimeFontWeightProperty); set => SetValue(TimeFontWeightProperty, value); }
    public FontFamily TimeFontFamily { get => (FontFamily)GetValue(TimeFontFamilyProperty); set => SetValue(TimeFontFamilyProperty, value); }
    public Color DialColor { get => (Color)GetValue(DialColorProperty); set => SetValue(DialColorProperty, value); }
    public Brush DialBrush { get => (Brush)GetValue(DialBrushProperty); set => SetValue(DialBrushProperty, value); }
    public IconElement PlayPauseIcon { get => (IconElement)GetValue(PlayPauseIconProperty); set => SetValue(PlayPauseIconProperty, value); }
    public Visibility TaskVisibility { get => (Visibility)GetValue(TaskVisibilityProperty); set => SetValue(TaskVisibilityProperty, value); }
    public Visibility DotsVisibility { get => (Visibility)GetValue(DotsVisibilityProperty); set => SetValue(DotsVisibilityProperty, value); }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimerDial)d).UpdateArc();
    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimerDial)d).UpdateColors();
    private static void OnTaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dial = (TimerDial)d;
        dial.TaskVisibility = string.IsNullOrWhiteSpace(dial.TaskName) ? Visibility.Collapsed : Visibility.Visible;
    }
    private static void OnRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dial = (TimerDial)d;
        dial.PlayPauseIcon = dial.IsRunning ? new SymbolIcon(SymbolRegular.Pause24) : new SymbolIcon(SymbolRegular.Play24);
    }
    private static void OnBreakChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimerDial)d).UpdateColors();
    private static void OnSessionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TimerDial)d).UpdateDots();

    private void UpdateColors()
    {
        Color color = IsBreak ? Color.FromRgb(46, 179, 143) : Color.FromRgb(235, 75, 85);
        DialColor = color;
        DialBrush = new SolidColorBrush(color);
        UpdateDots();
    }

    private void UpdateArc()
    {
        double radius = 130;
        double centerX = 160;
        double centerY = 160;

        double progress = Math.Clamp(Progress, 0.0, 1.0);
        double angle = progress * 359.99; // Avoid full 360 loop singularity in ArcSegment

        double startAngleRad = -Math.PI / 2; // -90 deg (top)
        double endAngleRad = startAngleRad + (angle * Math.PI / 180.0);

        Point startPoint = new Point(
            centerX + radius * Math.Cos(startAngleRad),
            centerY + radius * Math.Sin(startAngleRad)
        );

        Point endPoint = new Point(
            centerX + radius * Math.Cos(endAngleRad),
            centerY + radius * Math.Sin(endAngleRad)
        );

        bool isLargeArc = angle > 180.0;

        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false
        };

        figure.Segments.Add(new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            IsLargeArc = isLargeArc,
            SweepDirection = SweepDirection.Clockwise
        });

        geometry.Figures.Add(figure);
        ProgressPath.Data = geometry;
    }

    private void UpdateDots()
    {
        if (SessionDotsPanel == null)
            return;

        SessionDotsPanel.Children.Clear();
        if (IsFlowmodoro || SessionCount <= 1)
        {
            DotsVisibility = Visibility.Collapsed;
            return;
        }

        DotsVisibility = Visibility.Visible;
        for (int i = 0; i < SessionCount; i++)
        {
            bool isCurrent = i == SessionIndex;
            var dot = new Ellipse
            {
                Width = isCurrent ? 8 : 6,
                Height = isCurrent ? 8 : 6,
                Margin = new Thickness(3, 0, 3, 0),
                Fill = isCurrent ? DialBrush : (Brush)Application.Current.FindResource("TextFillColorTertiaryBrush"),
                Opacity = isCurrent ? 1.0 : 0.5
            };
            SessionDotsPanel.Children.Add(dot);
        }
    }

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e) => PlayPauseClick?.Invoke(this, e);
    private void BtnStop_Click(object sender, RoutedEventArgs e) => StopClick?.Invoke(this, e);
    private void BtnSkip_Click(object sender, RoutedEventArgs e) => SkipClick?.Invoke(this, e);
}
