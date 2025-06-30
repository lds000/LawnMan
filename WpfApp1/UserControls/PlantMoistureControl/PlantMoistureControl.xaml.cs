using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BackyardBoss.UserControls
{
    public partial class PlantMoistureControl : UserControl
    {
        public PlantMoistureControl()
        {
            InitializeComponent();
            // Do NOT set DataContext = this; so parent bindings work
            UpdateClip();
            this.Loaded += UserControl_Loaded;
        }

        public static readonly DependencyProperty MoisturePercentProperty =
            DependencyProperty.Register("MoisturePercent", typeof(double), typeof(PlantMoistureControl),
                new PropertyMetadata(100.0, OnMoistureChanged));

        public double MoisturePercent
        {
            get => (double)(GetValue(MoisturePercentProperty) ?? 0.0);
            set => SetValue(MoisturePercentProperty, value);
        }

        // Blue fill (moist) - bottom up
        public double BlueClipHeight => 64 * Math.Max(0, Math.Min(100, MoisturePercent)) / 100.0;
        public double BlueClipTop => 64 - BlueClipHeight;
        // Green fill (dry) - top down
        public double GreenClipHeight => 64 - BlueClipHeight;

        private static void OnMoistureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlantMoistureControl ctrl)
            {
                ctrl.OnPropertyChanged(nameof(BlueClipHeight));
                ctrl.OnPropertyChanged(nameof(BlueClipTop));
                ctrl.OnPropertyChanged(nameof(GreenClipHeight));
                ctrl.UpdateClip();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateClip();
        }

        private void UpdateClip()
        {
            if (BlueClipRect != null)
                BlueClipRect.Rect = new Rect(0, BlueClipTop, 64, BlueClipHeight);
            if (GreenClipRect != null)
                GreenClipRect.Rect = new Rect(0, 0, 64, GreenClipHeight);
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }
}
