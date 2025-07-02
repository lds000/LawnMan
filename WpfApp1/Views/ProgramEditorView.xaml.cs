using BackyardBoss.ViewModels;
using System.Windows;
using System.Windows.Controls;
using BackyardBoss.Services;

namespace BackyardBoss.Views
{
    public partial class ProgramEditorView : UserControl
    {
        public ProgramEditorView()
        {
            InitializeComponent();
            this.DataContext = new ProgramEditorViewModel();
            DebugLogger.LogFileIO("Test FileIO message from ProgramEditorView");


        }

        private void PlantMoistureControl_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {

        }
    }
}
