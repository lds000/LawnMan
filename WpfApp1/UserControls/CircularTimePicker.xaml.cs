using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BackyardBoss.UserControls
{
    public partial class CircularTimePicker : UserControl
    {
        private const double OuterRadius = 120;
        private const double InnerRadius = 90;
        private const double Center = 150;

        private double minuteAngle = 90;
        private double hourAngle = 0;
        private bool isPM = false; // Keep AM/PM internally
        
        public event EventHandler SelectedTimeChanged;

        public CircularTimePicker()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MinuteThumb.DragDelta += MinuteThumb_DragDelta;
            HourThumb.DragDelta += HourThumb_DragDelta;
            RootGrid.MouseDown += RootGrid_MouseDown;
            UpdateVisuals();
        }

        public static readonly DependencyProperty SelectedTimeProperty =
            DependencyProperty.Register(
                nameof(SelectedTime),
                typeof(TimeSpan),
                typeof(CircularTimePicker),
                new PropertyMetadata(TimeSpan.Zero, OnSelectedTimeChanged));

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CircularTimePicker)d;
            control.SyncVisualsToSelectedTime();
        }

        private void SyncVisualsToSelectedTime()
        {
            int totalMinutes = (int)SelectedTime.TotalMinutes;
            int hour = SelectedTime.Hours % 12;
            if (hour == 0)
                hour = 12;

            // Calculate angles
            hourAngle = ((double)hour / 12.0) * 360.0;
            minuteAngle = ((double)SelectedTime.Minutes / 60.0) * 360.0;

            // Set AM/PM state
            isPM = SelectedTime.Hours >= 12;

            UpdateVisuals();
        }


        public TimeSpan SelectedTime
        {
            get
            {
                return (TimeSpan)GetValue(SelectedTimeProperty);
            }
            set
            {
                SetValue(SelectedTimeProperty, value);
            }
        }

        private void MinuteThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point position = Mouse.GetPosition(RootGrid);
            minuteAngle = PointToAngle(position);
            UpdateVisuals();
        }

        private void HourThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Point position = Mouse.GetPosition(RootGrid);
            hourAngle = PointToAngle(position);
            UpdateVisuals();
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPosition = e.GetPosition(RootGrid);

            double dx = clickPosition.X - Center;
            double dy = clickPosition.Y - Center;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= InnerRadius)
            {
                ToggleAmPm();
            }
        }

        private double PointToAngle(Point p)
        {
            double dx = p.X - Center;
            double dy = p.Y - Center;
            double angle = Math.Atan2(dy, dx) * (180 / Math.PI);
            angle = (angle + 90) % 360;
            if (angle < 0)
                angle += 360;
            return angle;
        }

        private void UpdateVisuals()
        {
            SetThumbPosition(MinuteThumb, minuteAngle, outerRadius: true);
            SetThumbPosition(HourThumb, hourAngle, outerRadius: false);

            UpdateClockHands();
            DrawSelectionArcs();
            UpdateSelectedTime();
            PositionDigitalTimeLabel();
        }

        private void SetThumbPosition(Thumb thumb, double angle, bool outerRadius)
        {
            double radius = outerRadius ? OuterRadius : InnerRadius;
            double radians = (angle - 90) * (Math.PI / 180);

            double x = Center + radius * Math.Cos(radians) - thumb.Width / 2;
            double y = Center + radius * Math.Sin(radians) - thumb.Height / 2;

            Canvas.SetLeft(thumb, x);
            Canvas.SetTop(thumb, y);
        }

        private void UpdateClockHands()
        {
            double minuteRadians = (minuteAngle - 90) * (Math.PI / 180);
            MinuteHand.X1 = Center;
            MinuteHand.Y1 = Center;
            MinuteHand.X2 = Center + OuterRadius * 0.8 * Math.Cos(minuteRadians);
            MinuteHand.Y2 = Center + OuterRadius * 0.8 * Math.Sin(minuteRadians);

            double hourRadians = (hourAngle - 90) * (Math.PI / 180);
            HourHand.X1 = Center;
            HourHand.Y1 = Center;
            HourHand.X2 = Center + InnerRadius * 0.5 * Math.Cos(hourRadians);
            HourHand.Y2 = Center + InnerRadius * 0.5 * Math.Sin(hourRadians);
        }

        private void DrawSelectionArcs()
        {
            DrawArc(MinuteArcGeometry, OuterRadius, 0, minuteAngle);
            DrawArc(HourArcGeometry, InnerRadius, 0, hourAngle);
        }

        private void DrawArc(PathGeometry geometry, double radius, double startAngle, double endAngle)
        {
            double startRadians = (startAngle - 90) * (Math.PI / 180);
            double endRadians = (endAngle - 90) * (Math.PI / 180);

            Point startPoint = new Point(
                Center + radius * Math.Cos(startRadians),
                Center + radius * Math.Sin(startRadians));

            Point endPoint = new Point(
                Center + radius * Math.Cos(endRadians),
                Center + radius * Math.Sin(endRadians));

            bool isLargeArc = Math.Abs(endAngle - startAngle) > 180;

            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false
            };

            var arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };

            figure.Segments.Clear();
            figure.Segments.Add(arcSegment);

            geometry.Figures.Clear();
            geometry.Figures.Add(figure);
        }

        private void UpdateSelectedTime()
        {
            int hour = (int)((hourAngle / 360.0) * 12);
            hour = (hour == 0) ? 12 : hour;

            if (isPM && hour != 12)
                hour += 12;
            if (!isPM && hour == 12)
                hour = 0;

            int minute = (int)((minuteAngle / 360.0) * 60);
            minute = minute % 60;

            SelectedTime = new TimeSpan(hour, minute, 0);

            UpdateDigitalTimeLabel();
            UpdateBackground();

            // 🔥 Fire event!
            SelectedTimeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleAmPm()
        {
            isPM = !isPM;
            UpdateSelectedTime();
        }

        private void UpdateDigitalTimeLabel()
        {
            DigitalTimeLabel.Text = $"{SelectedTime.Hours:D2}:{SelectedTime.Minutes:D2}";
        }

        private void UpdateBackground()
        {
            InnerCircleBackground.Fill = isPM
                ? new SolidColorBrush(Color.FromRgb(230, 230, 250)) // Light Lavender PM
                : new SolidColorBrush(Colors.White); // White AM
        }

        private void PositionDigitalTimeLabel()
        {
            double labelRadius = InnerRadius * 0.5;
            double radians = 90 * (Math.PI / 180);

            double x = Center + labelRadius * Math.Cos(radians) - DigitalTimeLabel.Width / 2;
            double y = Center + labelRadius * Math.Sin(radians) - DigitalTimeLabel.FontSize / 2;

            Canvas.SetLeft(DigitalTimeLabel, x);
            Canvas.SetTop(DigitalTimeLabel, y);
        }
    }
}
