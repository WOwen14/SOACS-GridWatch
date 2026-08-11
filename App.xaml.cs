using System;
using System.Windows;
using System.Windows.Threading;

namespace SOACS.GridWatch
{
    public partial class App : Application
    {
        private SplashWindow _splash;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            _splash = new SplashWindow();
            _splash.Show();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(4100);
            timer.Tick += delegate
            {
                timer.Stop();
                var main = new MainWindow();
                MainWindow = main;
                main.Show();
                _splash.Close();
            };
            timer.Start();
        }
    }
}
