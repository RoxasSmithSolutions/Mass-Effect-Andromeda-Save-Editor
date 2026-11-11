using System;
using System.IO;
using System.Windows;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: SuppressIldasm]

namespace MEABasicSaveEditor
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                MessageBox.Show("An unexpected error occurred:\n" + e.ExceptionObject?.ToString(), "Save Editor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show("A UI error occurred:\n" + e.Exception?.Message, "Save Editor Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var win = new MainWindow();
                win.Show();
                if (e.Args.Length > 0 && File.Exists(e.Args[0]))
                {
                    win.LoadSaveFile(e.Args[0]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fatal Error launching MainWindow:\n" + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
