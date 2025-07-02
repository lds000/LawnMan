using System;
using System.Windows;
using System.Windows.Controls;

namespace BackyardBoss.Views
{
    public class SectionTemplateSelector : DataTemplateSelector
    {
        public DataTemplate OverviewTemplate { get; set; }

        public DataTemplate GaugesTemplate { get; set; }
        public DataTemplate SetsTemplate { get; set; }
        public DataTemplate ScheduleTemplate { get; set; }
        public DataTemplate HistoryTemplate { get; set; }
        public DataTemplate SettingsTemplate { get; set; }
        public DataTemplate DebugTemplate { get; set; } // Renamed from ErrorTemplate for debug info
        public DataTemplate SoilDataTemplate { get; set; }
        public DataTemplate SensorDataTemplate { get; set; } // <-- Add this property
        public DataTemplate MapTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            string section = item as string;
            return section switch
            {
                "Overview" =>   OverviewTemplate,
                "Gauges" => GaugesTemplate,
                "Sets" => SetsTemplate,
                "Schedule" => ScheduleTemplate,
                "History" => HistoryTemplate,
                "Settings" => SettingsTemplate,
                "Soil Data" => SoilDataTemplate,
                "Sensor Data" => SensorDataTemplate, // <-- Add this case
                "Debug" => DebugTemplate, // Renamed from Error
                "Map" => MapTemplate,
                _ => OverviewTemplate
            };
        }
    }
}
