using System.Windows;
using System.Windows.Controls;
using BackyardBoss.ViewModels;

namespace BackyardBoss.Views
{
    public partial class DebugView : UserControl
    {
        public DebugView()
        {
            InitializeComponent();
            this.DataContext = DebugViewModel.Current; // Use singleton instance
        }

        private void DebugListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DebugListView.View is GridView gridView && gridView.Columns.Count == 4)
            {
                double totalWidth = DebugListView.ActualWidth;
                double otherColumnsWidth = gridView.Columns[0].Width + gridView.Columns[1].Width + gridView.Columns[2].Width;
                double detailsWidth = totalWidth - otherColumnsWidth - 35; // 35 for scrollbar/margin fudge factor
                if (detailsWidth > 100)
                    gridView.Columns[3].Width = detailsWidth;
            }
        }
    }
}
