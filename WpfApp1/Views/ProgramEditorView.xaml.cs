using BackyardBoss.ViewModels;
using System.Windows;
using System.Windows.Controls;
using BackyardBoss.Views; // Needed to open PickTimeWindow


namespace BackyardBoss.Views
{
    public partial class ProgramEditorView : UserControl
    {
        public ProgramEditorView()
        {
            InitializeComponent();
        }


        private void OpenTimePickerDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RadialTimePickerDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (DataContext is ProgramEditorViewModel vm)
            {
                dialog.SetTime(vm.StartTime1.ParsedTime); // pass current time
            }

            if (dialog.ShowDialog() == true)
            {
                var selectedTime = dialog.SelectedTime;
                if (DataContext is ProgramEditorViewModel vm2)
                {
                    vm2.StartTime1.Time = selectedTime.ToString(@"hh\:mm");
                }
            }
        }


        private void StartTimesListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ProgramEditorViewModel viewModel && !string.IsNullOrEmpty(viewModel.SelectedStartTime))
            {
                var window = new PickTimeWindow
                {
                    Owner = Application.Current.MainWindow,
                    ViewModel = viewModel
                };

                // Preload current time
                var parts = viewModel.SelectedStartTime.Split(':');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int hour) &&
                    int.TryParse(parts[1], out int minute))
                {
                    window.SetInitialTime(hour, minute);
                }

                if (window.ShowDialog() == true && !string.IsNullOrEmpty(window.SelectedTime))
                {
                    int index = viewModel.StartTimes.IndexOf(viewModel.SelectedStartTime);
                    if (index >= 0)
                    {
                        viewModel.StartTimes[index] = window.SelectedTime;

                        viewModel.SortStartTimes();
                        viewModel.SelectedStartTime = window.SelectedTime;
                        viewModel.OnPropertyChanged(nameof(viewModel.StartTimes));
                        viewModel.MarkDirty();
                    }
                }
            }
        }

    }
}
