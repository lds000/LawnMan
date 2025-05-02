using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BackyardBoss.UserControls
{
    public partial class RadialTimePicker : UserControl
    {
        private const double Radius = 140;
        private const double Center = 150;
        private bool isDragging = false;

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(nameof(SelectedTime), typeof(TimeSpan), typeof(RadialTimePicker),
                new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeChanged));

        public TimeSpan SelectedTime
        {
            get => (TimeSpan)GetValue(SelectedTimeProperty);
            set => SetValue(SelectedTimeProperty, value);
        }

        public RadialTimePicker()
        {
            InitializeComponent();
        }

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadialTimePicker picker)
                picker.DrawClock();
        }

        private void ClockCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            DrawClock();
        }

        private void ClockCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            UpdateTimeFromPoint(e.GetPosition(ClockCanvas));
        }

        private void ClockCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateTimeFromPoint(e.GetPosition(ClockCanvas));
            }
        }



        private void ClockCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }

        private void UpdateTimeFromPoint(Point point)
        {
            double dx = point.X - Center;
            double dy = point.Y - Center;

            double angle = Math.Atan2(dy, dx);
            if (angle < -Math.PI / 2)
                angle += 2 * Math.PI;

            double normalizedAngle = (angle + Math.PI / 2) % (2 * Math.PI);
            double minutes = normalizedAngle / (2 * Math.PI) * 1440;

            minutes = Math.Round(minutes / 5) * 5;
            if (minutes >= 1440)
                minutes = 0;

            SelectedTime = TimeSpan.FromMinutes(minutes);
        }

        private void DrawClock()
        {
            if (ClockCanvas == null)
                return;
            ClockCanvas.Children.Clear();

            // Ticks
            for (int i = 0; i < 1440; i += 5)
            {
                double angle = (Math.PI * 2 * i / 1440) - (Math.PI / 2);
                double x1 = Center + Math.Cos(angle) * (Radius - 5);
                double y1 = Center + Math.Sin(angle) * (Radius - 5);
                double x2 = Center + Math.Cos(angle) * Radius;
                double y2 = Center + Math.Sin(angle) * Radius;

                var tick = new Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Gray,
                    StrokeThickness = (i % 60 == 0) ? 2 : 1
                };
                ClockCanvas.Children.Add(tick);
            }

            // Hour Labels
            for (int hour = 0; hour <= 23; hour += 6)
            {
                double angle = (Math.PI * 2 * (hour * 60) / 1440) - (Math.PI / 2);
                double x = Center + Math.Cos(angle) * (Radius - 25);
                double y = Center + Math.Sin(angle) * (Radius - 25);

                var label = new TextBlock
                {
                    Text = hour.ToString(),
                    FontSize = 14,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(label, x - 10);
                Canvas.SetTop(label, y - 10);
                ClockCanvas.Children.Add(label);
            }

            // Selected time arm and knob
            double selectedMinutes = SelectedTime.TotalMinutes;
            double selectedAngle = (Math.PI * 2 * selectedMinutes / 1440) - (Math.PI / 2);
            double xTip = Center + Math.Cos(selectedAngle) * (Radius - 40);
            double yTip = Center + Math.Sin(selectedAngle) * (Radius - 40);

            var arm = new Line
            {
                X1 = Center,
                Y1 = Center,
                X2 = xTip,
                Y2 = yTip,
                Stroke = Brushes.Blue,
                StrokeThickness = 3
            };
            ClockCanvas.Children.Add(arm);

            var knob = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.DodgerBlue,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(knob, xTip - 8);
            Canvas.SetTop(knob, yTip - 8);
            ClockCanvas.Children.Add(knob);

            var centerDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = Brushes.Black
            };
            Canvas.SetLeft(centerDot, Center - 5);
            Canvas.SetTop(centerDot, Center - 5);

            ClockCanvas.Children.Add(centerDot);

            DigitalTimeText.Text = DateTime.Today.Add(SelectedTime).ToString("hh:mm tt");

        }
    }
}
