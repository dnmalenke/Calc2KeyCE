using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Calc2KeyCE.Core.Usb;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Reflection;

namespace Calc2KeyCE.Core.ScreenMirroring
{
    public class ScreenMirror
    {
        private bool _connected = false;
        private UsbEndpointWriter _calcWriter;
        private Thread _sendThread;
        private Thread _screenThread;

        private byte[] _compressedImage;
        private byte[] _uncompressedImage;

        private Func<byte[]> _captureFunc;

        public EventHandler<ErrorCode> OnUsbError { get; set; }

        /// <summary>
        /// ScreenMirror Class
        /// </summary>
        /// <param name="calculator"></param>
        /// <param name="captureFunc"> 
        /// Byte array of the screen in rgb565 format
        /// </param>
        public ScreenMirror(ref UsbCalculator calculator, Func<byte[]> captureFunc)
        {
            _calcWriter = calculator.CalcWriter;
            _captureFunc = captureFunc;
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

        private unsafe void GetScreenArray()
        {
            while (_connected)
            {
                _uncompressedImage = _captureFunc.Invoke();

                long d = 0;
                long opZ = 0;

                // for some reason when the window maximizes the optimize funtion takes a LONG time to run. This cancels it in that situation.
                CancellationTokenSource ctSource = new();
                ctSource.CancelAfter(5000);

                Optimal[] op = Optimize.optimize(_uncompressedImage, (uint)_uncompressedImage.Length, 0, ctSource.Token);

                if (ctSource.IsCancellationRequested)
                {
                    ctSource.Dispose();
                    _compressedImage = Array.Empty<byte>();

                    if (!_sendThread.IsAlive && _connected)
                    {
                        _sendThread.Start();
                    }

                    continue;
                }

                _compressedImage = Compress.compress(op, _uncompressedImage, (uint)_uncompressedImage.Length, 0, ref d, ref opZ);

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

                    if (compImage.Length >= 51200 || compImage.Length == 0)
                    {
                        _calcWriter.Write(153600, 1000, out _);
                        c = _calcWriter.Write(_uncompressedImage, 0, 153600, 10000, out _);
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
