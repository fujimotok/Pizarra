using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using HidLibrary;

namespace Pizarra
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private HotKeyHelper _hotkey;
        private HidDevice _device;
        private bool isRotate = false;

        public MainWindow()
        {
            InitializeComponent();

            // HotKeyの登録
            this._hotkey = new HotKeyHelper(this);
            this._hotkey.Register(ModifierKeys.Control | ModifierKeys.Shift,
                                  Key.Z,
                                  (_, __) => { this.OnHotKeyPressed(); });
            TabletModeController.SetTabletMode(false);
            Display.Rotate(1, Display.Orientations.DEGREES_CW_90); // 90：横 0：縦

            var deviceList = HidDevices.Enumerate().ToArray();
            _device = Array.Find(deviceList, dev => dev.DevicePath == "xxx");

            if (_device != null)
            {
                if (_device.IsConnected)
                {
                    _device.OpenDevice();
                }

                _device.MonitorDeviceEvents = true;
                _device.ReadReport(OnReport);
            }
        }

        private void OnHotKeyPressed()
        {
            if (isRotate)
            {
                TabletModeController.SetTabletMode(false);
                Display.Rotate(1, Display.Orientations.DEGREES_CW_90); // 90：横 0：縦
            }
            else
            {
                TabletModeController.SetTabletMode(true);
                Display.Rotate(1, Display.Orientations.DEGREES_CW_0); // 90：横 0：縦
            }

            isRotate = !isRotate;
        }

        private void OnReport(HidReport report)
        {
            TabletModeController.SetTabletMode(true);
            Display.Rotate(1, Display.Orientations.DEGREES_CW_0); // 90：横 0：縦

            isRotate = true;

            // we need to start listening again for more data
            _device.ReadReport(OnReport);
        }

        protected override void OnClosed(EventArgs e)
        {
            TabletModeController.SetTabletMode(false);
            Display.Rotate(1, Display.Orientations.DEGREES_CW_90); // 90：横 0：縦

            // HotKeyの登録解除
            this._hotkey.Dispose();

            base.OnClosed(e);
        }

        private void HorizontalMode_Click(object sender, RoutedEventArgs e)
        {
            this.isRotate = false;
            TabletModeController.SetTabletMode(false);
            Display.Rotate(1, Display.Orientations.DEGREES_CW_90); // 90：横 0：縦
        }

        private void VerticallMode_Click(object sender, RoutedEventArgs e)
        {
            this.isRotate = true;
            TabletModeController.SetTabletMode(true);
            Display.Rotate(1, Display.Orientations.DEGREES_CW_0); // 90：横 0：縦
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
