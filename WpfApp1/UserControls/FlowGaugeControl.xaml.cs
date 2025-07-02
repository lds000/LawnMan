using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BackyardBoss.UserControls
{
    public partial class FlowGaugeControl : UserControl
    {
        public static readonly DependencyProperty FlowValueProperty =
            DependencyProperty.Register("FlowValue", typeof(double), typeof(FlowGaugeControl),
                new PropertyMetadata(0.0, OnFlowValueChanged));

        public double FlowValue
        {
            get => (double)GetValue(FlowValueProperty);
            set => SetValue(FlowValueProperty, value);
        }

        // Angle for the needle (calibration: 0 psi = -120 deg, 120 psi = 120 deg)
        public static readonly DependencyProperty NeedleAngleFlowProperty =
            DependencyProperty.Register("NeedleAngleFlow", typeof(double), typeof(FlowGaugeControl),
                new PropertyMetadata(-120.0));

        public double NeedleAngleFlow
        {
            get => (double)GetValue(NeedleAngleFlowProperty);
            set => SetValue(NeedleAngleFlowProperty, value);
        }

        private static void OnFlowValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (d is FlowGaugeControl ctrl)
            {
                // Map 0-120 psi to -120 to 120 degrees

                double gph = (double)e.NewValue;
                double angle = -124 + (gph / 60.0) * 248.0; ctrl.NeedleAngleFlow = angle;
            }
        }

        public FlowGaugeControl()
        {
            InitializeComponent();
        }
    }
}
