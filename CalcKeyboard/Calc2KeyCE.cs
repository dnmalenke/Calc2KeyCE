using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using WindowsInput;

namespace CalcKeyboard
{
    public partial class Calc2KeyCE : Form
    {
        private static SerialPort _serialPort;
        private List<BoundKey> _boundKeys = new List<BoundKey>();
        private bool _connected = false;
        private Dictionary<string, Type> _groupTypes;
        private Dictionary<string, int> _currentKeys = new Dictionary<string, int>();
        private List<string> _previousKeys = new List<string>();
        private List<string> _addedKeys = new List<string>();
        private List<string> _currentMouseActions = new List<string>();
        private double _mouseMoveX = 0;
        private double _mouseMoveY = 0;
        private bool _binding = false;
        private InputSimulator _inputSimulator;

        public Calc2KeyCE()
        {
            InitializeComponent();
            KeyPreview = true;
            _inputSimulator = new InputSimulator();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DeviceSelector.Items.AddRange(SerialPort.GetPortNames().Distinct().ToArray());
            UpdateBoundKeyList();
        }

        private void RefreshBtnClick(object sender, EventArgs e)
        {
            DeviceSelector.Items.Clear();
            DeviceSelector.Items.AddRange(SerialPort.GetPortNames().Distinct().ToArray());
        }

        private void ConnectBtnClick(object sender, EventArgs e)
        {
            if (DeviceSelector.SelectedItem != null && !_connected)
            {
                _serialPort = new SerialPort();

                _serialPort.PortName = DeviceSelector.SelectedItem.ToString();

                _serialPort.DataReceived += DataReceivedHandler;
                _serialPort.ErrorReceived += SerialErrorReceived;

                try
                {
                    _serialPort.Open();
                    _serialPort.Write("c");

                    _connected = true;
                    ConnectBtn.Text = "Disconnect";
                    DeviceSelector.Enabled = false;
                    button3.Visible = true;
                    SaveBtn.Visible = true;
                    button1.Visible = true;
                    button2.Visible = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                DisconnectSerial();

                _connected = false;
                ConnectBtn.Text = "Connect";
                DeviceSelector.Enabled = true;
                button3.Visible = false;
                KeyBindingBox.Visible = false;
                SaveBtn.Visible = false;
                button1.Visible = false;
                button2.Visible = false;
            }

            UpdateBoundKeyList();
        }

        private Task DisconnectSerial()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Write("d");
                    _serialPort.DataReceived -= DataReceivedHandler;
                    _serialPort.Close();
                }
                catch { }
            }

            return Task.CompletedTask;
        }

        private void SerialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            MessageBox.Show(e.EventType.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string receivedLine = _serialPort.ReadLine();

                receivedLine = receivedLine.Replace("\0", string.Empty);

                if (receivedLine.StartsWith('s'))
                {
                    receivedLine = receivedLine.Trim('s', ',');

                    int[] rawKeyboardData = Array.ConvertAll(receivedLine.Split(','), int.Parse);

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
                                        _currentKeys.Add(keyName,1);
                                        _addedKeys.Add(keyName);
                                    }
                                }
                            }
                        }
                    }

                    //foreach (var key in _currentKeys)
                    //{
                    //    Console.WriteLine($"{key.Key}:{key.Value}");
                    //}

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
                        HandleBoundKeys();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void HandleBoundKeys()
        {
            foreach (var boundKey in _boundKeys.Where(bk => _previousKeys.Except(_addedKeys).Contains(Enum.GetName(typeof(CalculatorKeyboard.AllKeys), bk.CalcKey))))
            {
                //keyup

                if (boundKey.KeyboardAction != null)
                {
                    Keyboard.SendKey(GetDirectXKeyStroke(boundKey.KeyboardAction.Value), true, Keyboard.InputType.Keyboard);
                }

                if (boundKey.MouseButtonAction != null)
                {
                    MouseOperations.MouseEvent(GetMouseEventFlag(boundKey.MouseButtonAction.Value, true));

                    _currentMouseActions.Remove(boundKey.MouseButtonAction.ToString());
                }

                if (boundKey.MouseMoveAction != null)
                {
                    switch (boundKey.MouseMoveAction.Value)
                    {
                        case MouseOperations.MouseMoveActions.MoveDown:
                            _mouseMoveY = 0;
                            break;
                        case MouseOperations.MouseMoveActions.MoveUp:
                            _mouseMoveY = 0;
                            break;
                        case MouseOperations.MouseMoveActions.MoveLeft:
                            _mouseMoveX = 0;
                            break;
                        case MouseOperations.MouseMoveActions.MoveRight:
                            _mouseMoveX = 0;
                            break;
                        default:
                            break;
                    }
                }

                _currentKeys.Remove(boundKey.CalcKey.ToString());
            }

            foreach (var boundKey in _boundKeys.Where(bk => _currentKeys.ContainsKey(Enum.GetName(typeof(CalculatorKeyboard.AllKeys), bk.CalcKey))))
            {
                //keydown

                if (boundKey.KeyboardAction != null)
                {
                    if(_currentKeys[boundKey.CalcKey.ToString()] == 1 || _currentKeys[boundKey.CalcKey.ToString()] > 50)
                    {
                        Keyboard.SendKey(GetDirectXKeyStroke(boundKey.KeyboardAction.Value), false, Keyboard.InputType.Keyboard);
                    }
                }

                if (boundKey.MouseButtonAction != null && !_currentMouseActions.Contains(boundKey.MouseButtonAction.ToString()))
                {
                    MouseOperations.MouseEvent(GetMouseEventFlag(boundKey.MouseButtonAction.Value, false));

                    _currentMouseActions.Add(boundKey.MouseButtonAction.ToString());
                }

                if (boundKey.MouseMoveAction != null)
                {
                    switch (boundKey.MouseMoveAction.Value)
                    {
                        case MouseOperations.MouseMoveActions.MoveDown:
                            if (_mouseMoveY < double.MaxValue)
                            {
                                _mouseMoveY += 0.1;
                            }
                            break;
                        case MouseOperations.MouseMoveActions.MoveUp:
                            if (_mouseMoveY > double.MinValue)
                            {
                                _mouseMoveY -= 0.1;
                            }
                            break;
                        case MouseOperations.MouseMoveActions.MoveLeft:
                            if (_mouseMoveX > double.MinValue)
                            {
                                _mouseMoveX -= 0.1;
                            }
                            break;
                        case MouseOperations.MouseMoveActions.MoveRight:
                            if (_mouseMoveX < double.MaxValue)
                            {
                                _mouseMoveX += 0.1;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            if (Math.Round(_mouseMoveX) != 0 || Math.Round(_mouseMoveY) != 0)
            {
                var mousePosition = MouseOperations.GetCursorPosition();
                _inputSimulator.Mouse.MoveMouseBy((int)Math.Round(_mouseMoveX), (int)Math.Round(_mouseMoveY));
               // MouseOperations.SetCursorPosition(mousePosition.X + (int)Math.Round(_mouseMoveX), mousePosition.Y + (int)Math.Round(_mouseMoveY));
            }
        }

        private Keyboard.DirectXKeyStrokes GetDirectXKeyStroke(Keys keyboardKey)
        {
            return (Keyboard.DirectXKeyStrokes)Enum.Parse(typeof(Keyboard.DirectXKeyStrokes), keyboardKey.ToString(), true);
        }

        private MouseOperations.MouseEventFlags GetMouseEventFlag(MouseButtons mouseAction, bool mouseUp = false)
        {
            string mouseActionString = mouseAction.ToString();

            if (mouseUp)
            {
                mouseActionString += "Up";
            }
            else
            {
                mouseActionString += "Down";
            }

            return (MouseOperations.MouseEventFlags)Enum.Parse(typeof(MouseOperations.MouseEventFlags), mouseActionString, true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            KeyBindingBox.Visible = true;
            _binding = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectSerial();
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
