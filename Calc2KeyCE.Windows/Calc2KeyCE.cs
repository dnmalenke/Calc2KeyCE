using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Calc2KeyCE.Core.ScreenMirroring;
using Calc2KeyCE.Core.Usb;
using Calc2KeyCE.ScreenMirroring;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Newtonsoft.Json;
using Vanara.PInvoke;

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

            foreach (var screen in Screen.AllScreens)
            {
                MonitorSelectBox.Items.Add(new KeyValuePair<Screen, string>(screen, $"{screen.DeviceName} : {screen.Bounds.Width}x{screen.Bounds.Height}"));
            }
            MonitorSelectBox.DisplayMember = "Value";

            if (MonitorSelectBox.Items.Count > 0)
            {
                MonitorSelectBox.SelectedIndex = 0;
            }
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            if (!_connected)
            {
                _calculator = new();

                if (!_calculator.Initialize())
                {
                    MessageBox.Show("Calculator not found.");
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
                MonitorSelectBox.Visible = false;

                if (CastScreenCheckBox.Checked)
                {
                    Screen selectedScreen = ((KeyValuePair<Screen, string>)MonitorSelectBox.SelectedItem).Key;
                    _screenMirror = new(ref _calculator, () => CaptureMonitor.Capture(selectedScreen));
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

            if (errorCode != ErrorCode.IoTimedOut)
            {
                MessageBox.Show(errorCode.ToString());
            }
            else
            {
                if (ConnectBtn.InvokeRequired)
                {
                    SetPropCallback c = new(() => ConnectBtn_Click(null, EventArgs.Empty));
                    Invoke(c);
                }
                else
                {
                    ConnectBtn_Click(null, EventArgs.Empty);
                }
            }

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
                MonitorSelectBox.Visible = true;
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

                if (_calculator != null)
                {
                    _calculator.DisconnectUsb();

                    _calculator = null;
                }
            }
            catch { }
        }
    }
}
