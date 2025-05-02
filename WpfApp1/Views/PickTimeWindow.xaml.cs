using BackyardBoss.ViewModels;
using System;
using System.Windows;

namespace BackyardBoss.Views
{
    public partial class PickTimeWindow : Window
    {
        public string SelectedTime
        {
            get; private set;
        }
        public ProgramEditorViewModel ViewModel
        {
            get; set;
        }

        public PickTimeWindow()
        {
            InitializeComponent();
            MyTimePicker.SelectedTimeChanged += MyTimePicker_SelectedTimeChanged;
        }

        private void MyTimePicker_SelectedTimeChanged(object sender, EventArgs e)
        {
            // 🔥 This fires when the clock is changed (drag or click)

            // Example:
            // You could call a method on your ViewModel here,
            // or update some other part of your app.

            // If you have a ViewModel:



            if (ViewModel != null)
            {
                ViewModel.UpdateUpcomingRunsPreview();
            }

            // Or if you want to close window with selected time:
            // this.DialogResult = true;
            // this.Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var time = MyTimePicker.SelectedTime; // ✅ No HasValue needed
            SelectedTime = $"{time.Hours:D2}:{time.Minutes:D2}";
            DialogResult = true;
            Close();
        }
        public void SetInitialTime(int hour, int minute)
        {
            MyTimePicker.SelectedTime = new TimeSpan(hour, minute, 0);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
