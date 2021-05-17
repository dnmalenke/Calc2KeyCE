using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Calc2KeyCE.KeyHandling;
using Calc2KeyCE.ScreenMirroring;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Newtonsoft.Json;

namespace Calc2KeyCE
{
    public partial class Calc2KeyCE : Form
    {      
        private bool _connected = false;

        private UsbDevice _calculator;
        private UsbEndpointWriter _calcWriter;
        private UsbEndpointReader _calcReader;

        private ScreenMirror _screenMirror;

        delegate void SetPropCallback();

        public Calc2KeyCE()
        {
            InitializeComponent();
            KeyPreview = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateBoundKeyList();
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                UsbRegDeviceList allDevices = UsbDevice.AllDevices;

                foreach (UsbRegistry usbRegistry in allDevices)
                {
                    if (usbRegistry.Open(out _calculator))
                    {
                        if (_calculator.Info.Descriptor.VendorID == 0x0451 && _calculator.Info.Descriptor.ProductID == -8183)
                        {
                            _calcWriter = _calculator.OpenEndpointWriter(WriteEndpointID.Ep02);
                            _calcReader = _calculator.OpenEndpointReader(ReadEndpointID.Ep01);
                            _calcReader.DataReceivedEnabled = true;
                            _calcReader.DataReceived += new EventHandler<EndpointDataEventArgs>(DataReceivedHandler);
                        }
                    }
                }

                if (_calcWriter != null && _calcReader != null)
                {
                    _connected = true;
                    ConnectBtn.Text = "Disconnect";
                    BindBtn.Visible = true;
                    SaveBtn.Visible = true;
                    LoadOverButton.Visible = true;
                    LoadAddBtn.Visible = true;
                    CastScreenCheckBox.Visible = false;

                    if (CastScreenCheckBox.Checked)
                    {
                        _screenMirror = new(ref _calcWriter);
                        _screenMirror.StartMirroring();
                        _screenMirror.OnUsbError += UsbErrorHandler;

                    }
                    else
                    {
                        SendConnectDisconnectMessage();
                    }
                }
            }
            else
            {
                DisconnectUsb();

                DisconnectUI();
            }

            UpdateBoundKeyList();
        }
        
        private void SendConnectDisconnectMessage()
        {
            if (_calcWriter != null)
            {
                _calcWriter.Write(new byte[] { 0x00, 0x00, 0x00 }, 1000, out _);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectUsb();
        }

        private void UsbErrorHandler(object sender, ErrorCode errorCode)
        {
            _screenMirror.StopMirroring();
            DisconnectUsb();
            DisconnectUI();
            MessageBox.Show(errorCode.ToString());
        }

        private void DisconnectUI()
        {
            if (ConnectBtn.InvokeRequired)
            {
                SetPropCallback c = new(DisconnectUI);
                Invoke(c);
            }
            else
            {
                ConnectBtn.Text = "Connect";
                BindBtn.Visible = false;
                KeyBindingBox.Visible = false;
                SaveBtn.Visible = false;
                LoadOverButton.Visible = false;
                LoadAddBtn.Visible = false;
                CastScreenCheckBox.Visible = true;
            }
        }

        private void DisconnectUsb()
        {
            try
            {
                _connected = false;
                if (_screenMirror != null)
                {
                    _screenMirror.StopMirroring();
                    _screenMirror = null;
                }
                SendConnectDisconnectMessage();

                if (_calcWriter != null)
                {
                    _calcWriter.Dispose();
                    _calcWriter = null;
                    _calcReader.Dispose();
                    _calcReader = null;
                }
                _calculator = null;

                UsbDevice.Exit();

            }
            catch { }
        }       
    }
}
