using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BackyardBoss.UserControls
{
    public enum PointType { Normal, Dripper, Sprinkler, TConnection, Mister }

    public class SprinklerLinePoint : INotifyPropertyChanged
    {
        private Guid _id = Guid.NewGuid();
        private double _x;
        private double _y;
        private PointType _type = PointType.Normal;

        public Guid Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
        public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }
        public PointType Type { get => _type; set { _type = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SprinklerLineConnection
    {
        public Guid FromPointId { get; set; }
        public Guid ToPointId { get; set; }
    }

    public class SprinklerLineModel : INotifyPropertyChanged
    {
        private bool _isWatering;
        private Color _defaultColor = Colors.Blue;
        private string _sprinklerLineTitle = string.Empty;

        public ObservableCollection<SprinklerLinePoint> Points { get; set; } = new();
        public ObservableCollection<SprinklerLineConnection> Connections { get; set; } = new();

        public Color DefaultColor
        {
            get => _defaultColor;
            set { _defaultColor = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineColor)); }
        }

        public bool IsWatering
        {
            get => _isWatering;
            set { _isWatering = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineColor)); }
        }

        public string SprinklerLineTitle
        {
            get => _sprinklerLineTitle;
            set { _sprinklerLineTitle = value; OnPropertyChanged(); }
        }

        public Color LineColor => IsWatering ? Colors.Green : DefaultColor;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MyMapDisplayControl : UserControl
    {
        public static readonly DependencyProperty MapImageSourceProperty =
            DependencyProperty.Register(nameof(MapImageSource), typeof(ImageSource), typeof(MyMapDisplayControl), new PropertyMetadata(null, OnVisualPropertyChanged));

        public static readonly DependencyProperty SprinklerLinesProperty =
            DependencyProperty.Register(nameof(SprinklerLines), typeof(ObservableCollection<SprinklerLineModel>), typeof(MyMapDisplayControl), new PropertyMetadata(null, OnVisualPropertyChanged));

        public static readonly DependencyProperty SelectedLineProperty =
            DependencyProperty.Register(nameof(SelectedLine), typeof(SprinklerLineModel), typeof(MyMapDisplayControl), new PropertyMetadata(null, OnSelectedLineChanged));

        public ImageSource MapImageSource
        {
            get => (ImageSource)GetValue(MapImageSourceProperty);
            set => SetValue(MapImageSourceProperty, value);
        }

        public ObservableCollection<SprinklerLineModel> SprinklerLines
        {
            get => (ObservableCollection<SprinklerLineModel>)GetValue(SprinklerLinesProperty);
            set => SetValue(SprinklerLinesProperty, value);
        }

        public SprinklerLineModel SelectedLine
        {
            get => (SprinklerLineModel)GetValue(SelectedLineProperty);
            set => SetValue(SelectedLineProperty, value);
        }

        private static void OnSelectedLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MyMapDisplayControl control)
            {
                control.RedrawOverlay();
            }
        }

        private DispatcherTimer _animationTimer;
        private Color _orange = (Color)ColorConverter.ConvertFromString("#FF6200");
        private Color _blue = (Color)ColorConverter.ConvertFromString("#0033A0");
        private bool _fadeToBlue = true;
        private SprinklerLineModel _activeLine;

        public MyMapDisplayControl()
        {
            InitializeComponent();
            Loaded += (s, e) => RedrawOverlay();
        }

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MyMapDisplayControl control)
            {
                control.RedrawOverlay();
            }
        }

        private void StartActiveLineFade(SprinklerLineModel line)
        {
            if (line == null)
                return;

            if (_animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer = null;
            }

            line.DefaultColor = _orange;
            _fadeToBlue = true;

            _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _animationTimer.Tick += (s, e) =>
            {
                if (_fadeToBlue)
                {
                    line.DefaultColor = _blue;
                }
                else
                {
                    line.DefaultColor = _orange;
                }
                _fadeToBlue = !_fadeToBlue;
                RedrawOverlay();
            };
            _animationTimer.Start();
        }

        private void RedrawOverlay()
        {
            OverlayCanvas.Children.Clear();
            var line = SelectedLine;
            if (line == null) return;

            double ellipseSize = 24;
            var brush = new SolidColorBrush(line.LineColor);
            foreach (var conn in line.Connections)
            {
                var from = line.Points.FirstOrDefault(p => p.Id == conn.FromPointId);
                var to = line.Points.FirstOrDefault(p => p.Id == conn.ToPointId);
                if (from != null && to != null)
                {
                    var l = new Line
                    {
                        X1 = from.X,
                        Y1 = from.Y,
                        X2 = to.X,
                        Y2 = to.Y,
                        Stroke = brush,
                        StrokeThickness = 3,
                        ToolTip = $"{line.SprinklerLineTitle}: ({from.X:0},{from.Y:0}) → ({to.X:0},{to.Y:0})"
                    };
                    OverlayCanvas.Children.Add(l);
                }
            }
            foreach (var pt in line.Points)
            {
                double cx = pt.X, cy = pt.Y;
                FrameworkElement icon = null;
                switch (pt.Type)
                {
                    case PointType.Dripper:
                        var drop = new Canvas { Width = ellipseSize, Height = ellipseSize, Background = Brushes.Transparent };
                        var dropEllipse = new Ellipse
                        {
                            Width = ellipseSize * 0.8,
                            Height = ellipseSize * 0.8,
                            Fill = Brushes.DeepSkyBlue,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 2
                        };
                        Canvas.SetLeft(dropEllipse, ellipseSize * 0.1);
                        Canvas.SetTop(dropEllipse, ellipseSize * 0.15);
                        var dropTriangle = new Polygon
                        {
                            Points = new PointCollection {
                                new Point(ellipseSize/2, ellipseSize*0.05),
                                new Point(ellipseSize*0.35, ellipseSize*0.35),
                                new Point(ellipseSize*0.65, ellipseSize*0.35)
                            },
                            Fill = Brushes.DeepSkyBlue,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 2
                        };
                        drop.Children.Add(dropEllipse);
                        drop.Children.Add(dropTriangle);
                        icon = drop;
                        break;
                    case PointType.Sprinkler:
                        var sprinkler = new Canvas { Width = ellipseSize, Height = ellipseSize, Background = Brushes.Transparent };
                        var greenCircle = new Ellipse
                        {
                            Width = ellipseSize * 0.8,
                            Height = ellipseSize * 0.8,
                            Fill = Brushes.LightGreen,
                            Stroke = Brushes.Green,
                            StrokeThickness = 2
                        };
                        Canvas.SetLeft(greenCircle, ellipseSize * 0.1);
                        Canvas.SetTop(greenCircle, ellipseSize * 0.1);
                        sprinkler.Children.Add(greenCircle);
                        for (int i = 0; i < 8; i++)
                        {
                            double angle = Math.PI * 2 * i / 8;
                            double x1 = ellipseSize / 2 + Math.Cos(angle) * ellipseSize * 0.25;
                            double y1 = ellipseSize / 2 + Math.Sin(angle) * ellipseSize * 0.25;
                            double x2 = ellipseSize / 2 + Math.Cos(angle) * ellipseSize * 0.48;
                            double y2 = ellipseSize / 2 + Math.Sin(angle) * ellipseSize * 0.48;
                            var ray = new Line
                            {
                                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                                Stroke = Brushes.Green,
                                StrokeThickness = 2
                            };
                            sprinkler.Children.Add(ray);
                        }
                        icon = sprinkler;
                        break;
                    case PointType.TConnection:
                        var tconn = new Canvas { Width = ellipseSize, Height = ellipseSize, Background = Brushes.Transparent };
                        var vline = new Line
                        {
                            X1 = ellipseSize / 2, Y1 = ellipseSize * 0.15,
                            X2 = ellipseSize / 2, Y2 = ellipseSize * 0.85,
                            Stroke = Brushes.SaddleBrown,
                            StrokeThickness = 5
                        };
                        var hline = new Line
                        {
                            X1 = ellipseSize * 0.2, Y1 = ellipseSize * 0.5,
                            X2 = ellipseSize * 0.8, Y2 = ellipseSize * 0.5,
                            Stroke = Brushes.SaddleBrown,
                            StrokeThickness = 5
                        };
                        tconn.Children.Add(vline);
                        tconn.Children.Add(hline);
                        icon = tconn;
                        break;
                    case PointType.Mister:
                        var mister = new Canvas { Width = ellipseSize, Height = ellipseSize, Background = Brushes.Transparent };
                        var mistCircle = new Ellipse
                        {
                            Width = ellipseSize * 0.8,
                            Height = ellipseSize * 0.8,
                            Fill = Brushes.AliceBlue,
                            Stroke = Brushes.SkyBlue,
                            StrokeThickness = 2
                        };
                        Canvas.SetLeft(mistCircle, ellipseSize * 0.1);
                        Canvas.SetTop(mistCircle, ellipseSize * 0.1);
                        mister.Children.Add(mistCircle);
                        for (int i = 0; i < 3; i++)
                        {
                            var path = new System.Windows.Shapes.Path
                            {
                                Stroke = Brushes.SkyBlue,
                                StrokeThickness = 2,
                                Data = Geometry.Parse($"M {ellipseSize * 0.2} {ellipseSize * (0.3 + i * 0.15)} Q {ellipseSize * 0.5} {ellipseSize * (0.2 + i * 0.15)}, {ellipseSize * 0.8} {ellipseSize * (0.3 + i * 0.15)}")
                            };
                            mister.Children.Add(path);
                        }
                        icon = mister;
                        break;
                    default:
                        var ellipse = new Ellipse
                        {
                            Width = ellipseSize,
                            Height = ellipseSize,
                            Fill = Brushes.White,
                            Stroke = Brushes.Blue,
                            StrokeThickness = 2,
                            Tag = pt,
                            ToolTip = $"({pt.X:0},{pt.Y:0})"
                        };
                        icon = ellipse;
                        break;
                }
                if (icon != null)
                {
                    icon.Tag = pt;
                    icon.ToolTip = $"({pt.X:0},{pt.Y:0})";
                    Canvas.SetLeft(icon, cx - ellipseSize / 2);
                    Canvas.SetTop(icon, cy - ellipseSize / 2);
                    OverlayCanvas.Children.Add(icon);
                }
            }
        }
    }
}