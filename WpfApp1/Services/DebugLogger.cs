using System;
using System.Diagnostics;
using BackyardBoss.Models;
using BackyardBoss.ViewModels;

namespace BackyardBoss.Services
{
    public static class DebugLogger
    {
        public static DebugSettings Settings { get; set; } = new DebugSettings();

        public static void LogFileIO(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "FileIO");
            if (Settings.FileIO)
                Debug.WriteLine($"[FileIO] {message}");
        }
        public static void LogPropertyChange(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "PropertyChange");
            if (Settings.PropertyChanges)
                Debug.WriteLine($"[PropertyChange] {message}");
        }
        public static void LogVariableStatus(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "VariableStatus");
            if (Settings.VariableStatus)
                Debug.WriteLine($"[VariableStatus] {message}");
        }
        public static void LogNetwork(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "Network");
            if (Settings.Network)
                Debug.WriteLine($"[Network] {message}");
        }
        public static void LogAutoSave(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "AutoSave");
            if (Settings.AutoSave)
                Debug.WriteLine($"[AutoSave] {message}");
        }
        public static void LogPiCommunication(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "PiCommunication");
            if (Settings.PiCommunication)
                Debug.WriteLine($"[PiCommunication] {message}");
        }
        public static void LogError(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "Error");
            if (Settings.Error)
                Debug.WriteLine($"[Error] {message}");
        }
        public static void LogCurrentIssue(string message)
        {
            DebugViewModel.Current?.AddDebug(message, "CurrentIssue");
            if (Settings.CurrentIssue)
                Debug.WriteLine($"[CurrentIssue] {message}");
        }
    }
}
