﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
       // private Thread _captureThread;

        private byte[] _compressedImage;
        private byte[] _uncompressedImage;

        private Func<byte[]> _captureFunc;

        public EventHandler<ErrorCode> OnUsbError { get; set; }

        /// <summary>
        /// ScreenMirror Class
        /// </summary>
        /// <param name="calculator"></param>
        /// <param name="captureFunc"> 
        /// Function that returns a 320x240 byte array of the screen in rgb565 format
        /// </param>
        public ScreenMirror(ref UsbCalculator calculator, Func<byte[]> captureFunc)
        {
            _calcWriter = calculator.CalcWriter;
            _captureFunc = captureFunc;
        }

        public void StartMirroring()
        {
            _connected = true;

          

           // _captureThread = new Thread(new ThreadStart(CaptureScreen));
            _screenThread = new Thread(new ThreadStart(CompressScreenArray));
            _sendThread = new Thread(new ThreadStart(SendScreenToCalc));

            //_captureThread.Start();
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

        private void CaptureScreen()
        {
            while (_connected)
            {
                _uncompressedImage = _captureFunc.Invoke();
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

#if DEBUG
                Debug.WriteLine($"Capturing at {1.0 / (frameTimer.ElapsedMilliseconds / 1000.0)} fps");
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
                        _calcWriter.Write(153600, 1000, out _);
                        c = _calcWriter.Write(_uncompressedImage, 66, 153600, 10000, out _);
                    }
                    else
                    {
                        _calcWriter.Write(BitConverter.GetBytes(compImage.Length).ToArray(), 1000, out _);
                        c = _calcWriter.Write(compImage, 0, compImage.Length, 1000, out _);
                    }
#if DEBUG
                    Debug.WriteLine($"Sending at {1.0 / (frameTimer.ElapsedMilliseconds/1000.0)} fps");
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