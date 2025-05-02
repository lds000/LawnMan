using System;
using System.Windows;

namespace BackyardBoss.Views
{
    public partial class RadialTimePickerDialog : Window
    {
        public RadialTimePickerDialog()
        {
            InitializeComponent();
        }

        public TimeSpan SelectedTime => TimePicker.SelectedTime;

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public void SetTime(TimeSpan initialTime)
        {
            TimePicker.SelectedTime = initialTime;
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
