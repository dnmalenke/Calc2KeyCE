using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Calc2KeyCE.Core.KeyHandling;
using LibUsbDotNet.Main;
using Newtonsoft.Json;

namespace Calc2KeyCE
{
    public partial class Calc2KeyCE : Form
    {
        private Dictionary<string, Type> _groupTypes;
        private Dictionary<string, int> _currentKeys = new();

        private List<BoundKey> _boundKeys = new();
        private List<string> _previousKeys = new();
        private List<string> _addedKeys = new();

        private System.Timers.Timer _keyHandleTimer = new(1);

        private bool _binding = false;

        private void DataReceivedHandler(object sender, EndpointDataEventArgs e)
        {
            byte[] rawKeyboardData = e.Buffer.Take(7).ToArray();

            _previousKeys = _currentKeys.Keys.ToList();

            _addedKeys = new List<string>();

            if (_groupTypes == null)
            {
                _groupTypes = new Dictionary<string, Type>();

                foreach (dynamic member in typeof(CalculatorKeyboard).GetMembers())
                {
                    if (member.Name.StartsWith("Group"))
                    {
                        _groupTypes.Add(member.Name, Type.GetType(member.AssemblyQualifiedName));
                    }
                }
            }

            for (int i = 0; i < rawKeyboardData.Length; i++)
            {
                if(rawKeyboardData[i] == 0)
                {
                    continue;
                }

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
            else if(!_keyHandleTimer.Enabled)
            {
                _keyHandleTimer.Elapsed += KeyHandleTimerTick;
                _keyHandleTimer.AutoReset = true;
                _keyHandleTimer.Enabled = true;

                KeyHandler.HandleBoundKeys(_boundKeys, _currentKeys, _previousKeys, _addedKeys, true, false);
            }
            else
            {
                KeyHandler.HandleBoundKeys(_boundKeys, _currentKeys, _previousKeys, _addedKeys, true, false);
            }
        }

        private void KeyHandleTimerTick(object sender, EventArgs e)
        {
            KeyHandler.HandleBoundKeys(_boundKeys.ToList(), _currentKeys, _previousKeys.ToList(), _addedKeys.ToList(), false,true);
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
        }

        protected override bool ProcessCmdKey(ref Message msg, System.Windows.Forms.Keys keyData)
        {
            object sender = FromHandle(msg.HWnd);
            KeyEventArgs e = new KeyEventArgs(keyData);
            Form1_KeyDown(sender, e);
            return true;
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

        private void HideRadioButtons()
        {
            radioButton3.Visible = false;
            radioButton2.Visible = false;
            radioButton1.Visible = false;

            radioButton1.Checked = false;
            radioButton2.Checked = false;
            radioButton3.Checked = false;
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

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            AddBtn.Visible = true;
        }

        private void BindBtn_Click(object sender, EventArgs e)
        {
            KeyBindingBox.Visible = true;
            _binding = true;
        }

        private void AddBtn_Click(object sender, EventArgs e)
        {
            var newBinding = new BoundKey { CalcKey = (CalculatorKeyboard.AllKeys)Enum.Parse(typeof(CalculatorKeyboard.AllKeys), CalcKeyBindBox.Text, true) };

            if (KeyboardKeyBindingBox.Visible)
            {
                newBinding.KeyboardAction = (Core.KeyHandling.Keys)Enum.Parse(typeof(Core.KeyHandling.Keys), KeyboardKeyBindingBox.Text, true);
            }
            else if (label3.Text == "Select a Mouse Button" && !string.IsNullOrEmpty(comboBox1.SelectedItem.ToString()))
            {
                newBinding.MouseButtonAction = (Core.KeyHandling.MouseButtons)Enum.Parse(typeof(Core.KeyHandling.MouseButtons), comboBox1.SelectedItem.ToString(), true);
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

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void LoadOverButton_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                _boundKeys = JsonConvert.DeserializeObject<List<BoundKey>>(File.ReadAllText(openFileDialog1.FileName));
                UpdateBoundKeyList();
            }
        }

        private void LoadAddBtn_Click(object sender, EventArgs e)
        {
            var result = openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                _boundKeys.AddRange(JsonConvert.DeserializeObject<List<BoundKey>>(File.ReadAllText(openFileDialog1.FileName)));
                UpdateBoundKeyList();
            }
        }

        private void RemoveBindBtn_Click(object sender, EventArgs e)
        {
            if (BoundKeyList.SelectedItem != null)
            {
                _boundKeys.RemoveAt(BoundKeyList.SelectedIndex);
                UpdateBoundKeyList();
            }
        }

        private void ClearBindBtn_Click(object sender, EventArgs e)
        {
            _boundKeys.Clear();
            UpdateBoundKeyList();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            File.WriteAllText(saveFileDialog1.FileName, JsonConvert.SerializeObject(_boundKeys));
        }
    }
}
