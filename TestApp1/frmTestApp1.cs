using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;


namespace TestApp1
{
    public partial class frmTestApp1 : Form
    {
        public frmTestApp1()
        {
            InitializeComponent();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            var root = AutomationElement.RootElement;

            Recurse(root);
        }

        private void Recurse(AutomationElement element)
        {
            int ec = 0;
            var elems = element.FindAll(TreeScope.Children, Condition.TrueCondition).Cast<AutomationElement>();

            var e1 = elems.Where(e => Prop(e, "Name") == "Certify-Client Repos").FirstOrDefault();

            elems = e1.FindAll(TreeScope.Children, Condition.TrueCondition).Cast<AutomationElement>();

            e1 = elems.Where(e => Prop(e, "Name") == "Windows Security").FirstOrDefault();

            elems = e1.FindAll(TreeScope.Subtree, Condition.TrueCondition).Cast<AutomationElement>();

            var tc = elems.Count();
            foreach (AutomationElement e in elems)
            {
                ec++;
                try
                {
                    var props = e.GetSupportedProperties();
                    tbxOutput.Text += "\r\n";
                    int pc = 0;
                    foreach (var p in props)
                    {
                        pc++;
                        tbxOutput.Text += $"   {p.ProgrammaticName}-{e.GetCurrentPropertyValue(p)}\r\n";
                        lblStatus.Text = $"{pc} prop in {ec} of {tc}";
                        lblStatus.Refresh();
                    }
                }
                catch 
                { }
            }
        }


        private string Prop(AutomationElement e, string propName)
        {
            try
            {
                var props = e.GetSupportedProperties();
                foreach (var p in props)
                {
                    if (p.ProgrammaticName.Contains("." + propName + "Property"))
                    {
                        return $"{e.GetCurrentPropertyValue(p)}";
                    }
                }
            }
            catch { }
            return "$$$UNKNOWN$$$";
        }
    }
}
