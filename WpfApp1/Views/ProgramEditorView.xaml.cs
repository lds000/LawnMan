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



    }
}
