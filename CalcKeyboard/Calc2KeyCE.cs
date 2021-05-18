using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Calc2KeyCE.KeyHandling;
using Calc2KeyCE.ScreenMirroring;
using Calc2KeyCE.Usb;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Newtonsoft.Json;

namespace Calc2KeyCE
{
    public partial class Calc2KeyCE : Form
    {
        private bool _connected = false;

        private UsbCalculator _calculator = new();

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
                if (!_calculator.Initialize())
                {
                    return;
                }

                _calculator.DataReceived += new EventHandler<EndpointDataEventArgs>(DataReceivedHandler);

                _connected = true;
                ConnectBtn.Text = "Disconnect";
                BindBtn.Visible = true;
                SaveBtn.Visible = true;
                LoadOverButton.Visible = true;
                LoadAddBtn.Visible = true;
                CastScreenCheckBox.Visible = false;

                if (CastScreenCheckBox.Checked)
                {
                    _screenMirror = new(ref _calculator);
                    _screenMirror.StartMirroring();
                    _screenMirror.OnUsbError += UsbErrorHandler;
                }
                else
                {
                    _calculator.SendConnectDisconnectMessage();
                }
            }
            else
            {
                DisconnectUsb();
                DisconnectUI();
            }

            UpdateBoundKeyList();
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

                _calculator.DisconnectUsb();
               
                _calculator = null;
            }
            catch { }
        }
    }
}
