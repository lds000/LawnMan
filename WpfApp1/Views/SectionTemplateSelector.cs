using System;
using System.Windows;
using System.Windows.Controls;

namespace BackyardBoss.Views
{
    public class SectionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OverviewTemplate { get; set; }
        public DataTemplate SetsTemplate { get; set; }
        public DataTemplate ScheduleTemplate { get; set; }
        public DataTemplate HistoryTemplate { get; set; }
        public DataTemplate SettingsTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string section = item as string;
            return section switch
            {
                "Overview" => OverviewTemplate,
                "Sets" => SetsTemplate,
                "Schedule" => ScheduleTemplate,
                "History" => HistoryTemplate,
                "Settings" => SettingsTemplate,
                _ => OverviewTemplate
            };
        }
    }
}
