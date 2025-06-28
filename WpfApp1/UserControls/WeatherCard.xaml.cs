using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Data;
using BackyardBoss.Views;
using System.Linq;

namespace BackyardBoss.UserControls
{
    public partial class WeatherCard : UserControl
    {
        // Animation tuning variables
        public double SpringConstant { get; set; } = 8; // Higher = stiffer spring
        public double Damping { get; set; } = 2; // Higher = more damping
        public double PhysicsUpdateHz { get; set; } = 60.0; // Simulation frequency

        public static readonly DependencyProperty WindDirectionProperty =
            DependencyProperty.Register(
                nameof(WindDirection),
                typeof(double),
                typeof(WeatherCard),
                new PropertyMetadata(0.0, OnWindDirectionChanged));

        public double WindDirection
        {
            get => (double)GetValue(WindDirectionProperty);
            set => SetValue(WindDirectionProperty, value);
        }

        private double _physicsAngle = 0.0;
        private double _physicsVelocity = 0.0;
        private bool _physicsActive = false;
        private double _lastTarget = 0.0;
        private DateTime _lastPhysicsUpdate = DateTime.MinValue;

        public WeatherCard()
        {
            InitializeComponent();
            this.Loaded += WeatherCard_Loaded;
            var binding = new Binding("EnvWindDirDeg") { Mode = BindingMode.OneWay };
            this.SetBinding(WindDirectionProperty, binding);
        }

        private void WeatherCard_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize physics state to current direction
            _physicsAngle = WindDirection;
            _lastTarget = WindDirection;
            if (CompassImg?.RenderTransform is RotateTransform rotate)
                rotate.Angle = _physicsAngle;
            StartPhysics();
        }

        private void WeatherPanel_Click(object sender, MouseButtonEventArgs e)
        {
            var window = new WeatherPanelView();
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        private static void OnWindDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WeatherCard card)
            {
                card._lastTarget = (double)e.NewValue;
                card.StartPhysics();
            }
        }

        private void StartPhysics()
        {
            if (!_physicsActive)
            {
                _physicsActive = true;
                _lastPhysicsUpdate = DateTime.Now;
                CompositionTarget.Rendering += PhysicsUpdate;
            }
        }

        private void StopPhysics()
        {
            if (_physicsActive)
            {
                _physicsActive = false;
                CompositionTarget.Rendering -= PhysicsUpdate;
            }
        }

        private void PhysicsUpdate(object? sender, EventArgs e)
        {
            if (CompassImg?.RenderTransform is not RotateTransform rotate)
                return;

            // Calculate time delta
            DateTime now = DateTime.Now;
            double dt = 1.0 / PhysicsUpdateHz;
            if (_lastPhysicsUpdate != DateTime.MinValue)
                dt = Math.Min((now - _lastPhysicsUpdate).TotalSeconds, 0.1); // Clamp to avoid large jumps
            _lastPhysicsUpdate = now;

            // Find shortest path (circular)
            double delta = _lastTarget - _physicsAngle;
            if (delta > 180) delta -= 360;
            if (delta < -180) delta += 360;

            // Spring force: F = -k * x
            double force = SpringConstant * delta;
            // Damping force: Fd = -c * v
            double dampingForce = -Damping * _physicsVelocity;
            // Net force
            double netForce = force + dampingForce;

            // Integrate velocity and position
            _physicsVelocity += netForce * dt;
            _physicsAngle += _physicsVelocity * dt;

            // Clamp angle to [0,360)
            if (_physicsAngle < 0) _physicsAngle += 360;
            if (_physicsAngle >= 360) _physicsAngle -= 360;

            rotate.Angle = _physicsAngle;

            // Stop if close enough and slow
            if (Math.Abs(delta) < 0.1 && Math.Abs(_physicsVelocity) < 0.05)
            {
                rotate.Angle = _lastTarget;
                _physicsAngle = _lastTarget;
                _physicsVelocity = 0;
                StopPhysics();
            }
        }
    }
}