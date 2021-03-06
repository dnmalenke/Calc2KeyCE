﻿namespace Calc2KeyCE
{
    partial class Calc2KeyCE
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ConnectBtn = new System.Windows.Forms.Button();
            this.BindBtn = new System.Windows.Forms.Button();
            this.KeyBindingBox = new System.Windows.Forms.GroupBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.AddBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.KeyboardKeyBindingBox = new System.Windows.Forms.TextBox();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.CalcKeyBindBox = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ClearBindBtn = new System.Windows.Forms.Button();
            this.RemoveBindBtn = new System.Windows.Forms.Button();
            this.BoundKeyList = new System.Windows.Forms.ListBox();
            this.SaveBtn = new System.Windows.Forms.Button();
            this.LoadOverButton = new System.Windows.Forms.Button();
            this.LoadAddBtn = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.CastScreenCheckBox = new System.Windows.Forms.CheckBox();
            this.KeyBindingBox.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // ConnectBtn
            // 
            this.ConnectBtn.Location = new System.Drawing.Point(18, 12);
            this.ConnectBtn.Name = "ConnectBtn";
            this.ConnectBtn.Size = new System.Drawing.Size(75, 23);
            this.ConnectBtn.TabIndex = 2;
            this.ConnectBtn.Text = "Connect";
            this.ConnectBtn.UseVisualStyleBackColor = true;
            this.ConnectBtn.Click += new System.EventHandler(this.ConnectBtn_Click);
            // 
            // BindBtn
            // 
            this.BindBtn.Location = new System.Drawing.Point(18, 73);
            this.BindBtn.Name = "BindBtn";
            this.BindBtn.Size = new System.Drawing.Size(75, 23);
            this.BindBtn.TabIndex = 4;
            this.BindBtn.Text = "Bind";
            this.BindBtn.UseVisualStyleBackColor = true;
            this.BindBtn.Visible = false;
            this.BindBtn.Click += new System.EventHandler(this.BindBtn_Click);
            // 
            // KeyBindingBox
            // 
            this.KeyBindingBox.Controls.Add(this.comboBox1);
            this.KeyBindingBox.Controls.Add(this.CancelBtn);
            this.KeyBindingBox.Controls.Add(this.AddBtn);
            this.KeyBindingBox.Controls.Add(this.label3);
            this.KeyBindingBox.Controls.Add(this.KeyboardKeyBindingBox);
            this.KeyBindingBox.Controls.Add(this.radioButton3);
            this.KeyBindingBox.Controls.Add(this.radioButton2);
            this.KeyBindingBox.Controls.Add(this.radioButton1);
            this.KeyBindingBox.Controls.Add(this.label2);
            this.KeyBindingBox.Controls.Add(this.label1);
            this.KeyBindingBox.Controls.Add(this.CalcKeyBindBox);
            this.KeyBindingBox.Location = new System.Drawing.Point(18, 102);
            this.KeyBindingBox.Name = "KeyBindingBox";
            this.KeyBindingBox.Size = new System.Drawing.Size(187, 213);
            this.KeyBindingBox.TabIndex = 7;
            this.KeyBindingBox.TabStop = false;
            this.KeyBindingBox.Text = "Bind a key";
            this.KeyBindingBox.Visible = false;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(7, 125);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 23);
            this.comboBox1.TabIndex = 8;
            this.comboBox1.Visible = false;
            this.comboBox1.SelectedValueChanged += new System.EventHandler(this.comboBox1_SelectedValueChanged);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Location = new System.Drawing.Point(7, 184);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 8;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // AddBtn
            // 
            this.AddBtn.Location = new System.Drawing.Point(88, 184);
            this.AddBtn.Name = "AddBtn";
            this.AddBtn.Size = new System.Drawing.Size(92, 23);
            this.AddBtn.TabIndex = 11;
            this.AddBtn.Text = "Add Binding";
            this.AddBtn.UseVisualStyleBackColor = true;
            this.AddBtn.Visible = false;
            this.AddBtn.Click += new System.EventHandler(this.AddBtn_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 107);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(118, 15);
            this.label3.TabIndex = 8;
            this.label3.Text = "Press a Keyboard Key";
            this.label3.Visible = false;
            // 
            // KeyboardKeyBindingBox
            // 
            this.KeyboardKeyBindingBox.Location = new System.Drawing.Point(6, 125);
            this.KeyboardKeyBindingBox.Name = "KeyboardKeyBindingBox";
            this.KeyboardKeyBindingBox.Size = new System.Drawing.Size(100, 23);
            this.KeyboardKeyBindingBox.TabIndex = 0;
            this.KeyboardKeyBindingBox.Visible = false;
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(6, 135);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(122, 19);
            this.radioButton3.TabIndex = 10;
            this.radioButton3.TabStop = true;
            this.radioButton3.Text = "Mouse Movement";
            this.radioButton3.UseVisualStyleBackColor = true;
            this.radioButton3.Visible = false;
            this.radioButton3.CheckedChanged += new System.EventHandler(this.radioButton3_CheckedChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(6, 110);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(100, 19);
            this.radioButton2.TabIndex = 10;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Mouse Button";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.Visible = false;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(6, 85);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(97, 19);
            this.radioButton1.TabIndex = 10;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "Keyboard Key";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.Visible = false;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 15);
            this.label2.TabIndex = 9;
            this.label2.Text = "Select an Option";
            this.label2.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 15);
            this.label1.TabIndex = 8;
            this.label1.Text = "Press a Calculator Key";
            // 
            // CalcKeyBindBox
            // 
            this.CalcKeyBindBox.Enabled = false;
            this.CalcKeyBindBox.Location = new System.Drawing.Point(6, 37);
            this.CalcKeyBindBox.Name = "CalcKeyBindBox";
            this.CalcKeyBindBox.Size = new System.Drawing.Size(100, 23);
            this.CalcKeyBindBox.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.ClearBindBtn);
            this.groupBox2.Controls.Add(this.RemoveBindBtn);
            this.groupBox2.Controls.Add(this.BoundKeyList);
            this.groupBox2.Location = new System.Drawing.Point(211, 102);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(389, 213);
            this.groupBox2.TabIndex = 8;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Currently Bound Keys";
            // 
            // ClearBindBtn
            // 
            this.ClearBindBtn.Location = new System.Drawing.Point(320, 52);
            this.ClearBindBtn.Name = "ClearBindBtn";
            this.ClearBindBtn.Size = new System.Drawing.Size(63, 23);
            this.ClearBindBtn.TabIndex = 2;
            this.ClearBindBtn.Text = "Clear";
            this.ClearBindBtn.UseVisualStyleBackColor = true;
            this.ClearBindBtn.Click += new System.EventHandler(this.ClearBindBtn_Click);
            // 
            // RemoveBindBtn
            // 
            this.RemoveBindBtn.Location = new System.Drawing.Point(320, 22);
            this.RemoveBindBtn.Name = "RemoveBindBtn";
            this.RemoveBindBtn.Size = new System.Drawing.Size(63, 23);
            this.RemoveBindBtn.TabIndex = 1;
            this.RemoveBindBtn.Text = "Remove";
            this.RemoveBindBtn.UseVisualStyleBackColor = true;
            this.RemoveBindBtn.Click += new System.EventHandler(this.RemoveBindBtn_Click);
            // 
            // BoundKeyList
            // 
            this.BoundKeyList.FormattingEnabled = true;
            this.BoundKeyList.ItemHeight = 15;
            this.BoundKeyList.Location = new System.Drawing.Point(6, 22);
            this.BoundKeyList.Name = "BoundKeyList";
            this.BoundKeyList.Size = new System.Drawing.Size(307, 184);
            this.BoundKeyList.TabIndex = 0;
            // 
            // SaveBtn
            // 
            this.SaveBtn.Location = new System.Drawing.Point(316, 12);
            this.SaveBtn.Name = "SaveBtn";
            this.SaveBtn.Size = new System.Drawing.Size(284, 23);
            this.SaveBtn.TabIndex = 9;
            this.SaveBtn.Text = "Save Preset";
            this.SaveBtn.UseVisualStyleBackColor = true;
            this.SaveBtn.Visible = false;
            this.SaveBtn.Click += new System.EventHandler(this.SaveBtn_Click);
            // 
            // LoadOverButton
            // 
            this.LoadOverButton.Location = new System.Drawing.Point(316, 41);
            this.LoadOverButton.Name = "LoadOverButton";
            this.LoadOverButton.Size = new System.Drawing.Size(284, 23);
            this.LoadOverButton.TabIndex = 10;
            this.LoadOverButton.Text = "Load Preset and Overwrite Currently Bound Keys";
            this.LoadOverButton.UseVisualStyleBackColor = true;
            this.LoadOverButton.Visible = false;
            this.LoadOverButton.Click += new System.EventHandler(this.LoadOverButton_Click);
            // 
            // LoadAddBtn
            // 
            this.LoadAddBtn.Location = new System.Drawing.Point(316, 70);
            this.LoadAddBtn.Name = "LoadAddBtn";
            this.LoadAddBtn.Size = new System.Drawing.Size(284, 23);
            this.LoadAddBtn.TabIndex = 11;
            this.LoadAddBtn.Text = "Load Preset and Add to Currently Bound Keys";
            this.LoadAddBtn.UseVisualStyleBackColor = true;
            this.LoadAddBtn.Visible = false;
            this.LoadAddBtn.Click += new System.EventHandler(this.LoadAddBtn_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "json";
            this.saveFileDialog1.Filter = "Calculator Key Presets|*.json";
            this.saveFileDialog1.Title = "Save Calculator Key Preset";
            this.saveFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.saveFileDialog1_FileOk);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "json";
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "Calculator Key Presets|*.json";
            this.openFileDialog1.Title = "Open Calculator Key Preset";
            // 
            // CastScreenCheckBox
            // 
            this.CastScreenCheckBox.AutoSize = true;
            this.CastScreenCheckBox.Checked = true;
            this.CastScreenCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CastScreenCheckBox.Location = new System.Drawing.Point(99, 15);
            this.CastScreenCheckBox.Name = "CastScreenCheckBox";
            this.CastScreenCheckBox.Size = new System.Drawing.Size(87, 19);
            this.CastScreenCheckBox.TabIndex = 12;
            this.CastScreenCheckBox.Text = "Cast Screen";
            this.CastScreenCheckBox.UseVisualStyleBackColor = true;
            // 
            // Calc2KeyCE
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(615, 330);
            this.Controls.Add(this.CastScreenCheckBox);
            this.Controls.Add(this.LoadAddBtn);
            this.Controls.Add(this.LoadOverButton);
            this.Controls.Add(this.ConnectBtn);
            this.Controls.Add(this.SaveBtn);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.KeyBindingBox);
            this.Controls.Add(this.BindBtn);
            this.Name = "Calc2KeyCE";
            this.Text = "Calc2KeyCE";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form1_PreviewKeyDown);
            this.KeyBindingBox.ResumeLayout(false);
            this.KeyBindingBox.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button ConnectBtn;
        private System.Windows.Forms.Button BindBtn;
        private System.Windows.Forms.GroupBox KeyBindingBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox CalcKeyBindBox;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox KeyboardKeyBindingBox;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button AddBtn;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListBox BoundKeyList;
        private System.Windows.Forms.Button SaveBtn;
        private System.Windows.Forms.Button LoadOverButton;
        private System.Windows.Forms.Button LoadAddBtn;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button RemoveBindBtn;
        private System.Windows.Forms.Button ClearBindBtn;
        private System.Windows.Forms.CheckBox CastScreenCheckBox;
    }
}

