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
        }


        private void RtfHelp_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(e.LinkText);
            }
            catch { }
        }
    }
}
