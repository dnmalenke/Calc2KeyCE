using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Calc2KeyCE.Core.Usb
{
    public class UsbCalculator
    {
        private UsbDevice _calculator;
        public UsbEndpointWriter CalcWriter { get; private set; }
        public UsbEndpointReader CalcReader { get; private set; }

        public event EventHandler<EndpointDataEventArgs> DataReceived;

        public bool Initialize()
        {
            _calculator = GetCalculator();

            if (_calculator != null)
            {
                CalcWriter = _calculator.OpenEndpointWriter(WriteEndpointID.Ep02);
                CalcReader = _calculator.OpenEndpointReader(ReadEndpointID.Ep01);
                CalcReader.DataReceivedEnabled = true;
                CalcReader.DataReceived += new EventHandler<EndpointDataEventArgs>(DataReceivedHandler);

                return true;
            }

            return false;
        }

        public void SendConnectDisconnectMessage()
        {
            if (CalcWriter != null)
            {
                CalcWriter.Write(new byte[] { 0x00, 0x00, 0x00 }, 1000, out _);
            }
        }

        public void DisconnectUsb()
        {
            try
            {
                SendConnectDisconnectMessage();

                if (CalcWriter != null)
                {
                    CalcWriter.Dispose();
                    CalcWriter = null;
                    CalcReader.Dispose();
                    CalcReader = null;
                }
                _calculator = null;

                UsbDevice.Exit();
            }
            catch { }
        }

        private void DataReceivedHandler(object sender, EndpointDataEventArgs e)
        {
            if (DataReceived != null)
            {
                DataReceived.Invoke(sender, e);
            }
        }

        private UsbDevice GetCalculator()
        {
            UsbRegDeviceList allDevices = UsbDevice.AllDevices;
            foreach (UsbRegistry usbRegistry in allDevices)
            {
                if (usbRegistry.Open(out UsbDevice dev))
                {
                    if (dev.Info.Descriptor.VendorID == 0x0451 && dev.Info.Descriptor.ProductID == -8183)
                    {
                        return dev;
                    }
                }
            }

            return null;
        }
    }
}
