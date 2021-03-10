using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Pizarra
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex = new Mutex(false, "Pizzara");

        public App()
        {
            var mw = new MainWindow();
            mw.Hide();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(
                (s1, e1) =>
                {
                    System.IO.File.AppendAllText(DateTime.Now.ToString("yyyyMMdd") + "_log.txt", "[" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "] message: ${e1}");
                });
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(
                (s2, e2) =>
                {
                    System.IO.File.AppendAllText(DateTime.Now.ToString("yyyyMMdd") + "_log.txt", "[" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "] message: ${e2}");
                });

            if (!App.mutex.WaitOne(0, false))
            {
                App.mutex.Close();
                App.mutex = null;
                this.Shutdown();
                return;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (App.mutex != null)
            {
                App.mutex.ReleaseMutex();
                App.mutex.Close();
                App.mutex = null;
            }
        }
    }
}
