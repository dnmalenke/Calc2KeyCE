using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Calc2KeyCE.Compression;
using Calc2KeyCE.Core.Usb;
using LibUsbDotNet;
using LibUsbDotNet.Main;

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
        /// Function that returns a 320x240 byte array of the screen in 8bpp indexed color mode. First 512 bytes is the palette
        /// </param>
        public ScreenMirror(ref UsbCalculator calculator, Func<byte[]> captureFunc)
        {
            _calcWriter = calculator.CalcWriter;
            _captureFunc = captureFunc;
        }

        public void StartMirroring()
        {
            _connected = true;

            _screenThread = new Thread(new ThreadStart(CompressScreenArray));
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

        private unsafe void CompressScreenArray()
        {
#if DEBUG
            Stopwatch frameTimer = Stopwatch.StartNew();
#endif
            while (_connected)
            {
                _uncompressedImage = _captureFunc.Invoke();

                long d = 0;
                long opZ = 0;

                CancellationTokenSource ctSource = new();
                ctSource.CancelAfter(5000);

                Optimal[] op = Optimize.optimize(_uncompressedImage, (uint)_uncompressedImage.Length, 512, ctSource.Token);

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

                _compressedImage = _uncompressedImage.Take(512).Concat(Compress.compress(op, _uncompressedImage, (uint)_uncompressedImage.Length, 512, ref d, ref opZ)).ToArray();

                ctSource.Dispose();

                if (!_sendThread.IsAlive && _connected)
                {
                    _sendThread.Start();
                }

#if DEBUG
                // Debug.WriteLine($"Capturing at {1.0 / (frameTimer.ElapsedMilliseconds / 1000.0)} fps");
                frameTimer.Restart();
#endif
            }
        }

        private void SendScreenToCalc()
        {
            ErrorCode c;
#if DEBUG
            Stopwatch frameTimer = Stopwatch.StartNew();
#endif
            while (_connected)
            {
                if (_compressedImage != null)
                {
                    byte[] compImage = _compressedImage.ToArray();
                    if (compImage.Length >= 51200 || compImage.Length == 0)
                    {
                        _calcWriter.Write(_uncompressedImage.Length, 1000, out _);
                        c = _calcWriter.Write(_uncompressedImage, 0, _uncompressedImage.Length, 10000, out _);
                    }
                    else
                    {
                        _calcWriter.Write(compImage.Length, 1000, out _);
                        c = _calcWriter.Write(compImage, 0, compImage.Length, 1000, out _);
                    }
#if DEBUG
                    // Debug.WriteLine($"Size: {compImage.Length}");
                    // Debug.WriteLine($"Sending at {1.0 / (frameTimer.ElapsedMilliseconds/1000.0)} fps");
                    frameTimer.Restart();
#endif
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