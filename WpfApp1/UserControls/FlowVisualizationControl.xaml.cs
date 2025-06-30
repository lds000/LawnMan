using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Generic;

namespace BackyardBoss.UserControls
{
    public partial class FlowVisualizationControl : UserControl
    {
        private readonly DispatcherTimer _timer;
        private double _phase;
        private readonly Random _rand = new();
        private readonly List<Ellipse> _bubbles = new();
        public static readonly DependencyProperty FlowRateProperty = DependencyProperty.Register(
            nameof(FlowRate), typeof(double), typeof(FlowVisualizationControl),
            new PropertyMetadata(0.0, OnFlowRateChanged));

        public double FlowRate
        {
            get => (double)GetValue(FlowRateProperty);
            set => SetValue(FlowRateProperty, value);
        }

        public static readonly DependencyProperty IsFlowVisibleProperty = DependencyProperty.Register(
            nameof(IsFlowVisible), typeof(Visibility), typeof(FlowVisualizationControl),
            new PropertyMetadata(Visibility.Visible));

        public Visibility IsFlowVisible
        {
            get => (Visibility)GetValue(IsFlowVisibleProperty);
            set => SetValue(IsFlowVisibleProperty, value);
        }

        private static void OnFlowRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowVisualizationControl control)
            {
                double flow = (double)e.NewValue;
                control.IsFlowVisible = flow > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public FlowVisualizationControl()
        {
            InitializeComponent();
            Loaded += (s, e) => StartAnimation();
            Unloaded += (s, e) => _timer.Stop();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            _timer.Tick += (s, e) => Animate();
        }

        private void StartAnimation()
        {
            _timer.Start();
        }

        private void Animate()
        {
            DrawWaterWave();
            AnimateBubbles();
        }

        private void DrawWaterWave()
        {
            WaterCanvas.Children.Clear();
            double width = 60; // Fixed width for compact display
            double height = WaterCanvas.ActualHeight > 0 ? WaterCanvas.ActualHeight : 40;
            double amplitude = Math.Min(10, Math.Max(3, FlowRate / 5));
            double frequency = 2 * Math.PI / width * (1 + FlowRate / 20);
            double baseY = height / 2;
            var path = new Path { Stroke = Brushes.DeepSkyBlue, StrokeThickness = 8, Opacity = 0.7 };
            var geo = new PathGeometry();
            var fig = new PathFigure { StartPoint = new Point(0, baseY) };
            for (double x = 0; x <= width; x += 2)
            {
                double y = baseY + amplitude * Math.Sin(frequency * x + _phase);
                fig.Segments.Add(new LineSegment(new Point(x, y), true));
            }
            geo.Figures.Add(fig);
            path.Data = geo;
            WaterCanvas.Width = width;
            WaterCanvas.Children.Add(path);
            if (FlowRate > 0)
                _phase += 0.15 + FlowRate / 100.0;
        }

        private void AnimateBubbles()
        {
            // Add new bubbles based on flow rate
            if (_rand.NextDouble() < Math.Min(0.2, FlowRate / 100.0))
            {
                var bubble = new Ellipse
                {
                    Width = _rand.Next(6, 12),
                    Height = _rand.Next(6, 12),
                    Fill = Brushes.AliceBlue,
                    Opacity = 0.5 + _rand.NextDouble() * 0.3
                };
                Canvas.SetLeft(bubble, _rand.Next(0, (int)(WaterCanvas.ActualWidth - 12)));
                Canvas.SetTop(bubble, WaterCanvas.ActualHeight - 10);
                _bubbles.Add(bubble);
                BubbleCanvas.Children.Add(bubble);
            }
            // Move bubbles
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                var bubble = _bubbles[i];
                double top = Canvas.GetTop(bubble);
                top -= 1.5 + FlowRate / 30.0;
                Canvas.SetTop(bubble, top);
                if (top < -12)
                {
                    BubbleCanvas.Children.Remove(bubble);
                    _bubbles.RemoveAt(i);
                }
            }
        }
    }
}
