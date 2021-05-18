using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Calc2KeyCE.Usb;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Vanara.PInvoke;

namespace Calc2KeyCE.ScreenMirroring
{
    public class ScreenMirror
    {
        private bool _connected = false;
        private UsbEndpointWriter _calcWriter;
        private Thread _sendThread;
        private Thread _screenThread;

        byte[] _compressedImage;
        byte[] _uncompressedImage;

        public EventHandler<ErrorCode> OnUsbError { get; set; }

        public ScreenMirror(ref UsbCalculator calculator)
        {
            _calcWriter = calculator.CalcWriter;
        }

        public void StartMirroring()
        {
            _connected = true;
            _screenThread = new Thread(new ThreadStart(GetScreenArray));
            _sendThread = new Thread(new ThreadStart(SendScreenToCalc));

            _screenThread.Start();
        }

        public void StopMirroring()
        {
            if (_connected)
            {
                _connected = false;
                _sendThread.Join();
            }
        }

        private Bitmap CaptureMonitor(Screen monitor)
        {
            Rectangle monitorRect = monitor.Bounds;
            Bitmap resultBmp = null;
            HWND desktopWindow = User32.GetDesktopWindow();
            HDC windowDc = User32.GetWindowDC(desktopWindow);
            HDC memDc = Gdi32.CreateCompatibleDC(windowDc);
            var bitmap = Gdi32.CreateCompatibleBitmap(windowDc, monitorRect.Width, monitorRect.Height);
            var oldBitmap = Gdi32.SelectObject(memDc, bitmap);

            bool result = Gdi32.BitBlt(memDc, 0, 0, monitorRect.Width, monitorRect.Height, windowDc, 0, 0, Gdi32.RasterOperationMode.SRCCOPY);

            if (result)
            {
                resultBmp = bitmap.ToBitmap();
            }

            Gdi32.SelectObject(memDc, oldBitmap);
            Gdi32.DeleteObject(bitmap);
            Gdi32.DeleteDC(memDc);
            User32.ReleaseDC(desktopWindow, windowDc);

            return resultBmp;
        }

        private unsafe void GetScreenArray()
        {
            while (_connected)
            {
                var result = CaptureMonitor(Screen.AllScreens.First());
                var shrunkImage = result.GetThumbnailImage(320, 240, null, IntPtr.Zero);
                shrunkImage.RotateFlip(RotateFlipType.Rotate180FlipX);

                Bitmap clone = new Bitmap(shrunkImage.Width, shrunkImage.Height, PixelFormat.Format16bppRgb565);

                using (Graphics gr = Graphics.FromImage(clone))
                {
                    gr.DrawImage(shrunkImage, new Rectangle(0, 0, clone.Width, clone.Height));
                }

                _uncompressedImage = clone.ToByteArray(ImageFormat.Bmp);

                clone.Dispose();
                result.Dispose();
                shrunkImage.Dispose();

                long d = 0;
                long opZ = 0;

                // for some reason when the window maximizes the optimize funtion takes a LONG time to run. This cancels it in that situation.
                CancellationTokenSource ctSource = new();
                ctSource.CancelAfter(5000);

                Optimal[] op = Optimize.optimize(_uncompressedImage, (uint)_uncompressedImage.Length, 66, ctSource.Token);

                if (ctSource.IsCancellationRequested)
                {
                    ctSource.Dispose();
                    continue;
                }

                _compressedImage = Compress.compress(op, _uncompressedImage, (uint)_uncompressedImage.Length, 66, ref d, ref opZ);

                ctSource.Dispose();

                if (!_sendThread.IsAlive && _connected)
                {
                   _sendThread.Start();
                }
            }
        }

        private void SendScreenToCalc()
        {
            ErrorCode c;
            while (_connected)
            {
                if (_compressedImage != null)
                {
                    byte[] compImage = _compressedImage.ToArray();

                    if (compImage.Length >= 51200)
                    {
                        _calcWriter.Write(153600, 1000, out _);
                        c = _calcWriter.Write(_uncompressedImage, 66, 153600, 10000, out _);
                    }
                    else
                    {
                        _calcWriter.Write(BitConverter.GetBytes(compImage.Length).ToArray(), 1000, out _);
                        c = _calcWriter.Write(compImage, 0, compImage.Length, 1000, out _);
                    }

                    if (c != ErrorCode.Success)
                    {
                        _connected = false;
                        OnUsbError.Invoke(this, c);
                    }
                }
            }
        }
    }
}
