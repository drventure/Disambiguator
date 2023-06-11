using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Disambiguator
{
    internal partial class TestOutput : Form
    {
        private static TestOutput _window = null;
        private Button btnCopyToClipboard;


        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnClose = new System.Windows.Forms.Button();
            this.tbxOutput = new System.Windows.Forms.TextBox();
            this.btnCopyToClipboard = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(684, 390);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(104, 48);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "&Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // tbxOutput
            // 
            this.tbxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbxOutput.Location = new System.Drawing.Point(13, 13);
            this.tbxOutput.Multiline = true;
            this.tbxOutput.Name = "tbxOutput";
            this.tbxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbxOutput.Size = new System.Drawing.Size(775, 359);
            this.tbxOutput.TabIndex = 1;
            this.tbxOutput.WordWrap = false;
            // 
            // btnCopyToClipboard
            // 
            this.btnCopyToClipboard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCopyToClipboard.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCopyToClipboard.Location = new System.Drawing.Point(13, 390);
            this.btnCopyToClipboard.Name = "btnCopyToClipboard";
            this.btnCopyToClipboard.Size = new System.Drawing.Size(197, 48);
            this.btnCopyToClipboard.TabIndex = 2;
            this.btnCopyToClipboard.Text = "Copy to Clipboard";
            this.btnCopyToClipboard.UseVisualStyleBackColor = true;
            this.btnCopyToClipboard.Click += new System.EventHandler(this.btnCopyToClipboard_Click);
            // 
            // TestOutput
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnCopyToClipboard);
            this.Controls.Add(this.tbxOutput);
            this.Controls.Add(this.btnClose);
            this.Name = "TestOutput";
            this.Text = "Disambiguation Details Report";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TestOutput_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.TextBox tbxOutput;


        internal TestOutput()
        {
            InitializeComponent();
        }


        internal static void Release()
        {
            if (_window != null) _window.InvokeIfRequired(o => o.Close());
        }


        internal static void WriteLine(string template, params object[] args)
        {
            var buf = string.Format(template, args);
            WriteLine(buf); 
        }


        internal static void WriteLine(string output)
        {
            if (_window == null)
            {
                _window = new TestOutput();
            }

            _window.WriteBuffer(output);
        }


        private void WriteBuffer(string output)
        {
            this.InvokeIfRequired(o =>
            {
                //if (!_window.Visible) TestOutput.ShowOnTop();
                this.tbxOutput.AppendText(output + "\r\n");
                this.tbxOutput.Refresh();
            });
        }


        internal static void ShowOnTop()
        {
            if (_window != null)
            {
                _window.InvokeIfRequired(o =>
                {
                    _window.Show();
                    _window.BringToFront();
                    _window.tbxOutput.SelectionStart = _window.tbxOutput.Text.Length;
                    _window.tbxOutput.ScrollToCaret();
                });
            }
        }


        private void TestOutput_FormClosed(object sender, FormClosedEventArgs e)
        {
            _window = null;
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            try
            {
                new SetClipboardHelper(DataFormats.Text, tbxOutput.Text).Go();
                System.Media.SystemSounds.Beep.Play();
            }
            catch 
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }
    }


    public static class SynchronizeInvokeExtensions
    {
        public static void InvokeIfRequired<T>(this T obj, Action<T> action)
            where T : ISynchronizeInvoke
        {
            if (obj.InvokeRequired)
            {
                obj.Invoke(action, new object[] { obj });
            }
            else
            {
                action(obj);
            }
        }

        public static TOut InvokeIfRequired<TIn, TOut>(this TIn obj, Func<TIn, TOut> func)
            where TIn : ISynchronizeInvoke
        {
            return obj.InvokeRequired
                ? (TOut)obj.Invoke(func, new object[] { obj })
                : func(obj);
        }
    }
}

