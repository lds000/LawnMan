using BackyardBoss.ViewModels;
using System.Windows;
using System.Windows.Controls;
using BackyardBoss.Views;
using System.Windows.Input; // Needed to open PickTimeWindow
using BackyardBoss.Services; // For DebugLogger

namespace BackyardBoss.Views
{
    public partial class ProgramEditorView : UserControl
    {
        public ProgramEditorView()
        {
            InitializeComponent();
            this.DataContext = new ProgramEditorViewModel(); // Set DataContext here for classic WPF pattern

            // Add a test debug log entry
            DebugLogger.LogFileIO("Test FileIO message from ProgramEditorView");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void WeatherPanel_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var weatherWindow = new WeatherPanelView();
            weatherWindow.Owner = Window.GetWindow(this); // ✅ Gets the parent window
            weatherWindow.ShowDialog();
        }

        private void TextBox_TouchNumericEntry(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is TextBox tb)
            {
                var window = new NumericKeypadWindow(tb.Text)
                {
                    Owner = Window.GetWindow(this)
                };

                if (window.ShowDialog() == true)
                {
                    tb.Text = window.Result;
                }

                // ✅ Release all input captures (mouse/touch/stylus)
                Mouse.Capture(null);
                Stylus.Capture(null);
                Keyboard.ClearFocus();

                // ✅ Manually shift focus to a fallback UI element (like root Grid)
                var parent = Window.GetWindow(this);
                if (parent != null)
                {
                    var scope = FocusManager.GetFocusScope(parent);
                    FocusManager.SetFocusedElement(scope, parent);
                }
            }
        }
    }
}
