using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BackyardBoss.Views
{
    public partial class NumericKeypadWindow : Window
    {
        public string Result
        {
            get; private set;
        }
        public event EventHandler Cancelled;

        public NumericKeypadWindow(string initialValue = "")
        {
            InitializeComponent();
            Display.Text = initialValue;
            FocusManager.SetFocusedElement(this, this);

            foreach (var button in FindVisualChildren<Button>(this))
            {
                button.PreviewTouchDown += Button_TouchDown;
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    var child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                    {
                        yield return t;
                    }

                    foreach (var childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public event Action<string> ValueSubmitted;


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string value)
            {
                Display.Text += value;
            }
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (Display.Text.Length > 0)
                Display.Text = Display.Text[..^1];
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Display.Text = "";
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ValueSubmitted?.Invoke(Display.Text);
            Close(); // No need for DialogResult
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            Close(); // ✅ needed here too
        }


        private void Button_TouchDown(object sender, TouchEventArgs e)
        {
            (sender as Button)?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
        }

        private void Ok_TouchDown(object sender, TouchEventArgs e)
        {
            Ok_Click(sender, e);
            e.Handled = true;
        }

        private void Cancel_TouchDown(object sender, TouchEventArgs e)
        {
            Cancel_Click(sender, e);
            e.Handled = true;
        }

        private void Clear_TouchDown(object sender, TouchEventArgs e)
        {
            Clear_Click(sender, e);
            e.Handled = true;
        }

        private void Backspace_TouchDown(object sender, TouchEventArgs e)
        {
            Backspace_Click(sender, e);
            e.Handled = true;
        }

    }
}
