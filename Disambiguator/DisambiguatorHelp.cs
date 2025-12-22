using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Disambiguator
{
    public partial class DisambiguatorHelp : Form
    {
        public DisambiguatorHelp()
        {
            InitializeComponent();
            this.Resize += DisambiguatorHelp_Resize;
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void DisambiguatorHelp_Load(object sender, EventArgs e)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream("Disambiguator.DisambiguatorHelp.rtf");

            this.rtfHelp.LoadFile(stream, RichTextBoxStreamType.RichText);
            this.rtfHelp.ReadOnly = true;
            this.rtfHelp.LinkClicked += RtfHelp_LinkClicked;

            this.Text = this.Text + " v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }


        private void DisambiguatorHelp_Resize(object sender, EventArgs e)
        {
            // Resize the RTF control to fill the form's client area
            // Leave some margin/padding if needed
            if (this.rtfHelp != null)
            {
                // Get the current position and calculate new size
                int margin = 10;
                
                // Adjust for the close button if it exists at the bottom
                int bottomReserved = 0;
                if (this.btnClose != null)
                {
                    bottomReserved = this.btnClose.Height + (margin * 2);
                }
                
                // Set the RTF control size
                this.rtfHelp.Location = new Point(margin, margin);
                this.rtfHelp.Width = this.ClientSize.Width - (margin * 2);
                this.rtfHelp.Height = this.ClientSize.Height - (margin * 2) - bottomReserved;
                
                // Position the close button at the bottom if it exists
                if (this.btnClose != null)
                {
                    this.btnClose.Top = this.ClientSize.Height - this.btnClose.Height - margin;
                    this.btnClose.Left = (this.ClientSize.Width - this.btnClose.Width) / 2;
                }
            }
        }


        private void RtfHelp_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
            catch { }
        }

        private void rtfHelp_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
