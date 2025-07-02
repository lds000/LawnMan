using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BackyardBoss.UserControls
{
    public partial class PressureGaugeControl : UserControl
    {
        public static readonly DependencyProperty PressureValueProperty =
            DependencyProperty.Register("PressureValue", typeof(double), typeof(PressureGaugeControl),
                new PropertyMetadata(0.0, OnPressureValueChanged));

        public double PressureValue
        {
            get => (double)GetValue(PressureValueProperty);
            set => SetValue(PressureValueProperty, value);
        }

        // Angle for the needle (calibration: 0 psi = -120 deg, 120 psi = 120 deg)
        public static readonly DependencyProperty NeedleAngleProperty =
            DependencyProperty.Register("NeedleAngle", typeof(double), typeof(PressureGaugeControl),
                new PropertyMetadata(-120.0));

        public double NeedleAngle
        {
            get => (double)GetValue(NeedleAngleProperty);
            set => SetValue(NeedleAngleProperty, value);
        }

        private static void OnPressureValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
                
            if (d is PressureGaugeControl ctrl)
            {
                // Map 0-120 psi to -120 to 120 degrees

                double psi = (double)e.NewValue;
                double angle = -124 + (psi / 80.0) * 248.0; ctrl.NeedleAngle = angle;
            }
        }

        public PressureGaugeControl()
        {
            InitializeComponent();
        }
    }
}
