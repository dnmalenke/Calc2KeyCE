using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Calc2KeyCE.Core.ScreenMirroring;
using Calc2KeyCE.Core.Usb;
using LibUsbDotNet.Main;
using Python.Included;
using Python.Runtime;

namespace Calc2KeyCE.Universal
{
    class Program
    {
        // Linux
        // wget https://packages.microsoft.com/config/ubuntu/21.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        // sudo dpkg -i packages-microsoft-prod.deb
        // sudo apt update
        // sudo apt install dotnet-runtime-5.0
        // sudo apt install libusb-1.0-0 // set symlink for libusb https://github.com/LibUsbDotNet/LibUsbDotNet
        // cd /lib/x86_64-linux-gnu
        // sudo ln -s libusb-1.0.so.0 libusb-1.0.so
        // sudo apt install driverctl
        // 
        //

        // Mac
        // http://macappstore.org/libusb/
        // net 5

        // use serial for backend?

        private static UsbCalculator _calculator = new();
        private static ScreenMirror _screenMirror;
        private static bool _connected = false;

        static async Task Main(string[] args)
        {
            Python.Deployment.Installer.SetupPython().Wait();
            //Installer.SetupPython().Wait();
            PythonEngine.Initialize();
            dynamic sys = PythonEngine.ImportModule("sys");
            Console.WriteLine("Python version: " + sys.version);
            //_calculator = new();

            //if (!_calculator.Initialize())
            //{
            //    Console.WriteLine("Calculator not found.");
            //    return;
            //}
            //Console.WriteLine("Calculator Found");
            //// _calculator.DataReceived += new EventHandler<EndpointDataEventArgs>(DataReceivedHandler);

            //_connected = true;

            //if (true) // if cast screen
            //{
            //    _screenMirror = new(ref _calculator, () => Capture());
            //    _screenMirror.StartMirroring();
            //    _screenMirror.OnUsbError += UsbErrorHandler;
            //}
            //else
            //{
            //    _calculator.SendConnectDisconnectMessage();
            //}
        }

        //private static void UsbErrorHandler(object sender, ErrorCode errorCode)
        //{
        //    _screenMirror.StopMirroring();
        //    DisconnectUsb();
        //    Console.WriteLine(errorCode.ToString());
        //}

        //private static void DisconnectUsb()
        //{
        //    try
        //    {
        //        _connected = false;
        //        if (_screenMirror != null)
        //        {
        //            _screenMirror.StopMirroring();
        //            _screenMirror = null;
        //        }

        //        if (_calculator != null)
        //        {
        //            _calculator.DisconnectUsb();

        //            _calculator = null;
        //        }
        //    }
        //    catch { }
        //}

        //public static byte[] Capture()
        //{
        //    using (Process screenCap = new())
        //    {
        //        if (OperatingSystem.IsWindows())
        //        {
        //            screenCap.StartInfo.FileName = "./PyScreenCaptureWindows.exe";
        //        }
        //        else if (OperatingSystem.IsLinux())
        //        {
        //            screenCap.StartInfo.FileName = "./PyScreenCaptureLinux";
        //        }
        //        else if (OperatingSystem.IsMacOS())
        //        {
        //            screenCap.StartInfo.FileName = "./PyScreenCaptureMacOS";
        //        }
        //        else
        //        {
        //            throw new PlatformNotSupportedException();
        //        }

        //        screenCap.StartInfo.RedirectStandardOutput = true;
        //        screenCap.Start();

        //        var screenStrs = screenCap.StandardOutput.ReadToEnd().Trim('[', ']', '\r', '\n').Split(',');

        //        return screenStrs.ToList().ConvertAll(s => Convert.ToByte(s)).ToArray();
        //    }
        //}
    }
}
