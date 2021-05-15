using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Newtonsoft.Json;
using Vanara.PInvoke;
using WindowsInput;

namespace Calc2KeyCE
{
    public partial class Calc2KeyCE : Form
    {
        private bool _connected = false;
        private List<BoundKey> _boundKeys = new();
        private Dictionary<string, Type> _groupTypes;
        private Dictionary<string, int> _currentKeys = new();
        private List<string> _previousKeys = new();
        private List<string> _addedKeys = new();
        private bool _binding = false;
        private UsbDevice _calculator;
        private UsbEndpointWriter _calcWriter;
        private UsbEndpointReader _calcReader;
        private ScreenMirror _screenMirror;

        public Calc2KeyCE()
        {
            InitializeComponent();
            KeyPreview = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateBoundKeyList();
        }

        private void ConnectBtnClick(object sender, EventArgs e)
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
                    button3.Visible = true;
                    SaveBtn.Visible = true;
                    button1.Visible = true;
                    button2.Visible = true;
                    checkBox1.Visible = false;

                    if (checkBox1.Checked)
                    {
                        _screenMirror = new(ref _calcWriter);
                        _screenMirror.StartMirroring();
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

                // UI Stuff
                ConnectBtn.Text = "Connect";
                button3.Visible = false;
                KeyBindingBox.Visible = false;
                SaveBtn.Visible = false;
                button1.Visible = false;
                button2.Visible = false;
                checkBox1.Visible = true;
            }

            UpdateBoundKeyList();
        }

        private void DataReceivedHandler(object sender, EndpointDataEventArgs e)
        {
            byte[] rawKeyboardData = e.Buffer.Take(7).ToArray();

            _previousKeys = _currentKeys.Keys.ToList();

            _addedKeys = new List<string>();

            if (_groupTypes == null)
            {
                _groupTypes = new Dictionary<string, Type>();

                foreach (var member in typeof(CalculatorKeyboard).GetMembers())
                {
                    if (member.Name.StartsWith("Group"))
                    {
                        _groupTypes.Add(member.Name, Type.GetType($"{typeof(CalculatorKeyboard).FullName}+{member.Name}"));
                    }
                }
            }

            for (int i = 0; i < rawKeyboardData.Length; i++)
            {
                Type currentGroup = _groupTypes[$"Group{i + 1}"];

                if (currentGroup != null)
                {
                    foreach (var value in Enum.GetValues(currentGroup))
                    {
                        if ((rawKeyboardData[i] & (int)value) != 0)
                        {
                            var keyName = Enum.GetName(currentGroup, value);

                            if (_currentKeys.ContainsKey(keyName))
                            {
                                _currentKeys[keyName]++;
                                _addedKeys.Add(keyName);
                            }
                            else
                            {
                                _currentKeys.Add(keyName, 1);
                                _addedKeys.Add(keyName);
                            }
                        }
                    }
                }
            }

            if (_binding && !string.IsNullOrEmpty(_addedKeys.FirstOrDefault()))
            {
                CalcKeyBindBox.Invoke((MethodInvoker)(() => CalcKeyBindBox.Text = _addedKeys.FirstOrDefault()));
                label2.Invoke((MethodInvoker)(() => label2.Visible = true));
                radioButton1.Invoke((MethodInvoker)(() => radioButton1.Visible = true));
                radioButton2.Invoke((MethodInvoker)(() => radioButton2.Visible = true));
                radioButton3.Invoke((MethodInvoker)(() => radioButton3.Visible = true));
                _binding = false;
            }
            else
            {
                KeyHandler.HandleBoundKeys(_boundKeys, _currentKeys, _previousKeys, _addedKeys);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            KeyBindingBox.Visible = true;
            _binding = true;
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                HideRadioButtons();
                label3.Text = "Press a Keyboard Key";
                label3.Visible = true;
                KeyboardKeyBindingBox.Visible = true;
            }
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            var newBinding = new BoundKey { CalcKey = (CalculatorKeyboard.AllKeys)Enum.Parse(typeof(CalculatorKeyboard.AllKeys), CalcKeyBindBox.Text, true) };

            if (KeyboardKeyBindingBox.Visible)
            {
                newBinding.KeyboardAction = (Keys)Enum.Parse(typeof(Keys), KeyboardKeyBindingBox.Text, true);
            }
            else if (label3.Text == "Select a Mouse Button" && !string.IsNullOrEmpty(comboBox1.SelectedItem.ToString()))
            {
                newBinding.MouseButtonAction = (MouseButtons)Enum.Parse(typeof(MouseButtons), comboBox1.SelectedItem.ToString(), true);
            }
            else
            {
                newBinding.MouseMoveAction = (MouseOperations.MouseMoveActions)Enum.Parse(typeof(MouseOperations.MouseMoveActions), comboBox1.SelectedItem.ToString(), true);
            }

            _boundKeys.Add(newBinding);

            ClearBindingBox();
            UpdateBoundKeyList();
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            ClearBindingBox();
        }

        private void ClearBindingBox()
        {
            CalcKeyBindBox.Text = "";
            KeyboardKeyBindingBox.Text = "";
            KeyboardKeyBindingBox.Enabled = true;
            KeyboardKeyBindingBox.Visible = false;
            comboBox1.Visible = false;
            comboBox1.SelectedText = string.Empty;
            label2.Visible = false;
            label3.Visible = false;
            HideRadioButtons();
            KeyBindingBox.Visible = false;
            AddBtn.Visible = false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            object sender = FromHandle(msg.HWnd);
            KeyEventArgs e = new KeyEventArgs(keyData);
            Form1_KeyDown(sender, e);
            return true;
        }

        private void Form1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            e.IsInputKey = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (KeyboardKeyBindingBox.Visible)
            {
                KeyboardKeyBindingBox.Text = e.KeyCode.ToString();
                KeyboardKeyBindingBox.Enabled = false;
                AddBtn.Visible = true;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                HideRadioButtons();
                label3.Text = "Select a Mouse Button";
                label3.Visible = true;
                comboBox1.DataSource = null;
                comboBox1.Items.Clear();
                comboBox1.SelectedItem = null;
                comboBox1.Text = "";
                comboBox1.Items.Add("Left");
                comboBox1.Items.Add("Right");
                comboBox1.Items.Add("Middle");
                comboBox1.Visible = true;
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                HideRadioButtons();
                label3.Text = "Select a Mouse Direction";
                label3.Visible = true;
                comboBox1.DataSource = null;
                comboBox1.Items.Clear();
                comboBox1.DataSource = Enum.GetValues(typeof(MouseOperations.MouseMoveActions));
                comboBox1.Visible = true;
            }
        }

        private void HideRadioButtons()
        {
            radioButton3.Visible = false;
            radioButton2.Visible = false;
            radioButton1.Visible = false;

            radioButton1.Checked = false;
            radioButton2.Checked = false;
            radioButton3.Checked = false;
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            AddBtn.Visible = true;
        }

        private void UpdateBoundKeyList()
        {
            BoundKeyList.Items.Clear();

            if (_boundKeys.Any() && _connected)
            {
                groupBox2.Visible = true;

                foreach (var boundKey in _boundKeys)
                {
                    BoundKeyList.Items.Add($"{boundKey.CalcKey}\t{boundKey.KeyboardAction}{(boundKey.MouseButtonAction.HasValue ? $"Mouse {boundKey.MouseButtonAction}" : "")}{(boundKey.MouseMoveAction.HasValue ? $"Mouse {boundKey.MouseMoveAction}" : "")}");
                }
            }
            else
            {
                groupBox2.Visible = false;
            }
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            File.WriteAllText(saveFileDialog1.FileName, JsonConvert.SerializeObject(_boundKeys));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                _boundKeys = JsonConvert.DeserializeObject<List<BoundKey>>(File.ReadAllText(openFileDialog1.FileName));
                UpdateBoundKeyList();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                _boundKeys.AddRange(JsonConvert.DeserializeObject<List<BoundKey>>(File.ReadAllText(openFileDialog1.FileName)));
                UpdateBoundKeyList();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (BoundKeyList.SelectedItem != null)
            {
                _boundKeys.RemoveAt(BoundKeyList.SelectedIndex);
                UpdateBoundKeyList();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _boundKeys.Clear();
            UpdateBoundKeyList();
        }
    }
}
