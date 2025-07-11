using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BackyardBoss.UserControls
{
    public partial class CompassControl : UserControl
    {
        public static readonly DependencyProperty WindDirectionProperty =
            DependencyProperty.Register(nameof(WindDirection), typeof(double), typeof(CompassControl),
                new PropertyMetadata(0.0, OnWindDirectionChanged));

        public double WindDirection
        {
            get => (double)GetValue(WindDirectionProperty);
            set => SetValue(WindDirectionProperty, value);
        }

        private double _physicsAngle = 0.0;
        private double _physicsVelocity = 0.0;
        private double _lastTarget = 0.0;
        private bool _physicsActive = false;
        private DateTime _lastPhysicsUpdate = DateTime.MinValue;

        public double SpringConstant { get; set; } = 8;
        public double Damping { get; set; } = 2;
        public double PhysicsUpdateHz { get; set; } = 60.0;

        public CompassControl()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                _physicsAngle = WindDirection;
                _lastTarget = WindDirection;
                if (NeedleRotate != null)
                    NeedleRotate.Angle = _physicsAngle;
                StartPhysics();
            };
        }

        private static void OnWindDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CompassControl ctrl)
            {
                ctrl._lastTarget = (double)e.NewValue;
                ctrl.StartPhysics();
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
            if (NeedleRotate == null) return;

            DateTime now = DateTime.Now;
            double dt = 1.0 / PhysicsUpdateHz;
            if (_lastPhysicsUpdate != DateTime.MinValue)
                dt = Math.Min((now - _lastPhysicsUpdate).TotalSeconds, 0.1);
            _lastPhysicsUpdate = now;

            double delta = _lastTarget - _physicsAngle;
            if (delta > 180) delta -= 360;
            if (delta < -180) delta += 360;

            double force = SpringConstant * delta;
            double dampingForce = -Damping * _physicsVelocity;
            double netForce = force + dampingForce;

            _physicsVelocity += netForce * dt;
            _physicsAngle += _physicsVelocity * dt;

            if (_physicsAngle < 0) _physicsAngle += 360;
            if (_physicsAngle >= 360) _physicsAngle -= 360;

            NeedleRotate.Angle = _physicsAngle;

            if (Math.Abs(delta) < 0.1 && Math.Abs(_physicsVelocity) < 0.05)
            {
                NeedleRotate.Angle = _lastTarget;
                _physicsAngle = _lastTarget;
                _physicsVelocity = 0;
                StopPhysics();
            }
        }
    }
}
