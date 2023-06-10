using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.Security;

using System.Windows.Automation;

using KeePass;
using KeePass.Plugins;
using KeePass.Forms;
using KeePass.UI;
using KeePass.Util;
using KeePass.Util.Spr;
using KeePassLib.Collections;
using KeePassLib.Utility;
using KeePassLib;
using KeePassLib.Security;
using KeePassLib.Cryptography.PasswordGenerator;
using System.Net.NetworkInformation;

namespace Disambiguator
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class DisambiguatorExt : Plugin
    {
        private IPluginHost _keePassHost;
        private bool _reportFlag = false;

        public override string UpdateUrl
        {
            get { return "https://raw.githubusercontent.com/drventure/Disambiguator/master/VERSION"; }
        }


        public override bool Initialize(IPluginHost host)
        {
            Debug("Starting...");

            if (host == null) return false;

            _keePassHost = host;

            AutoType.SequenceQuery += AutoType_SequenceQuery;
            AutoType.SequenceQueriesBegin += AutoType_SequenceQueriesBegin;
            AutoType.SequenceQueriesEnd += AutoType_SequenceQueriesEnd;

            Debug("Initialization done");
            return true; // Initialization successful
        }


        /// <summary>
        /// Provide our menu item to Keepass
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            // Provide a menu item for the main location(s)
            ToolStripMenuItem menuItem = new ToolStripMenuItem();
            switch (t)
            {
                case PluginMenuType.Main:
                    menuItem.Text = "The Disambiguator Options";
                    menuItem.Click += this.OnMainOptionsClicked;
                    var subMenu = new ToolStripMenuItem()
                    {
                        Text = "Report",
                        CheckOnClick = true,
                        Checked = false,
                    };
                    subMenu.Click += this.OnSetReportClicked;
                    menuItem.DropDownItems.Add(subMenu);
                    break;

                //case PluginMenuType.Group:
                //    menuItem.Text = "The Disambiguator Options";
                //    menuItem.Click += this.OnGroupOptionsClicked;
                //    break;

                //case PluginMenuType.Tray:
                //    menuItem.Text = "The Disambiguator Options";
                //    menuItem.Click += this.OnTrayOptionsClicked;
                //    break;

                //case PluginMenuType.Entry:
                //    menuItem.Text = "The Disambiguator Options";
                //    menuItem.Click += this.OnEntryOptionsClicked;
                //    break;

                default:
                    //no menus anywhere else
                    menuItem = null;
                    break;
            }

            return menuItem;
        }


        private void OnSetReportClicked(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;

            _reportFlag = menuItem.Checked;
            MessageBox.Show(string.Format("Disambiguator Reporting is {0}", _reportFlag ? "Enabled" : "Disabled"));
        }


        private void OnEntryOptionsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Only for test right now");
        }


        private void OnTrayOptionsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Only for test right now");
        }


        private void OnGroupOptionsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Only for test right now");
        }


        private void OnMainOptionsClicked(object sender, EventArgs e)
        {
            MessageBox.Show("Only for test right now");
        }


        /// <summary>
        /// Not used at this point, because this plugin just adds additional
        /// filtering based on Window Class and hosting app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoType_SequenceQueriesBegin(object sender, SequenceQueriesEventArgs e)
        {
            Debug("Sequence Queries Begin");
        }


        /// <summary>
        /// Not used at this point, because this plugin just adds additional
        /// filtering based on Window Class and hosting app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoType_SequenceQueriesEnd(object sender, SequenceQueriesEventArgs e)
        {
            Debug("Sequence Queries End");
        }


        private void AutoType_SequenceQuery(object sender, SequenceQueryEventArgs e)
        {
            //main win title and AutoType sequence for this entry
            //we have to check this separately from the custom associations
            var targetWindowTitle = e.Entry.Strings.ReadSafe("Title");
            string entryAutoTypeSequence = e.Entry.GetAutoTypeSequence();

            Debug("ResolveSequence for TargetWindowTitle {0}", targetWindowTitle);
            ResolveSequence(targetWindowTitle, entryAutoTypeSequence, e);

            //run through the target window associations looking for match elements
            foreach (AutoTypeAssociation association in e.Entry.AutoType.Associations)
            {
                //get the window name (this would usually contain the TITLE of the window
                //that would match
                var winName = association.WindowName;
                Debug("ResolveSequence for AutoTypeAssoc {0}", winName);
                ResolveSequence(winName, association.Sequence, e);
            }

            Debug("Finished AutoType_SequenceQuery");
        }


        public void ResolveSequence(string winName, string sequence, SequenceQueryEventArgs e)
        {
            Debug("ResolveSequence winName={0}  sequence={1}  eventId={2}  DBName={3}  TargetTitle={4}  TargetHandle={5}", winName, sequence, e.EventID, e.Database.Name, e.TargetWindowTitle, e.TargetWindowHandle);

            //clear all parameters and flags
            string exeParam = string.Empty;
            string ctlParam = string.Empty;

            var exePath = getExecutableFromHwnd(e.TargetWindowHandle).ToLower();

            //get the window name (this would usually contain the TITLE of the window
            //that would match
            if (winName == null)
            {
                Debug("Empty WindowName detected");
                return;
            }
            var matchTemplate = winName;

            var match = false;
            //next remove any app out of the window name
            //and try it

            //first compile the window name to replace all KeePass elements
            matchTemplate = SprEngine.Compile(matchTemplate, new SprContext(e.Entry, e.Database, SprCompileFlags.All));

            exeParam = getParam(ref matchTemplate, "exe");
            ctlParam = getParam(ref matchTemplate, "ctl");
            _reportFlag = _reportFlag || getFlag(ref matchTemplate, "report");

            if (_reportFlag)
            {
                if (!string.IsNullOrEmpty(exeParam))
                {
                    ReportLine("EXE tag detected. Searching for \"{0}\"", exeParam);
                }
                else
                {
                    ReportLine("No EXE tag detected.");
                }
                if (!string.IsNullOrEmpty(ctlParam))
                {
                    ReportLine("CTL tag detected. Searching for \"{0}\"", ctlParam);
                }
                else
                {
                    ReportLine("No CTL tag detected.");
                }

                ReportLine("Application of current target window: \"{0}\"", exePath);
                ReportLine("");
            }

            if (!string.IsNullOrEmpty(exeParam))
            {
                if (exeParam.Contains(@"\"))
                {
                    //parameter looks like it's got a path element, so compare to the whole exeName
                    match = (IsAMatch(exePath, exeParam));
                }
                else
                {
                    //no path element, so just compare to the exe filename
                    //check if there's an ext specified
                    if (!exeParam.Contains("."))
                    {
                        //add an exe extension by default if none specified
                        exeParam += ".exe";
                    }
                    match = (IsAMatch(Path.GetFileName(exePath), exeParam));
                }
            }

            //We always want to enumerate child controls when reporting
            if (_reportFlag || (!match && !string.IsNullOrEmpty(ctlParam)))
            {
                //no match yet, check for any child controls
                ReportLine("Scanning Descendant Controls...");

                //attempt to retrieve an accessible object from the target window handle
                var uiaObject = AutomationElement.FromHandle(e.TargetWindowHandle);

                ReportLine("TargetWindow: {0} Resolved: {1}", e.TargetWindowHandle, uiaObject != null);

                //and scan through it's child objects
                match = ScanControlTree(uiaObject, ctlParam);
            }

            //and lastly, the winName must match as well to be considered
            var title = e.TargetWindowTitle ?? string.Empty;
            match = match && IsAMatch(title, matchTemplate);

            //if reporting is on we DO NOT want to match
            //NOTE that other entries with reporting OFF +may still match+
            if (match && !_reportFlag)
            {
                e.AddSequence(string.IsNullOrEmpty(sequence) ? e.Entry.GetAutoTypeSequence() : sequence);
            }
        }


        /// <summary>
        /// Write a line to the report output window with formatting
        /// </summary>
        /// <param name="template"></param>
        /// <param name="args"></param>
        private void ReportLine(string template, params object[] args)
        {
            if (_reportFlag)
            {
                Debug(template, args);
                TestOutput.WriteLine(template, args);
            }
        }


        private bool ScanControlTree(AutomationElement parent, string ctlParam)
        {
            Debug("Scanning Control Tree");
            if (parent != null)
            {
                var parentID = parent.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty);
                ReportLine("Parent ID: {0}", parentID);

                //use an always true OR condition
                //probably a better way to do this, but this'll work for now.
                //var condition = new OrCondition(
                //	new PropertyCondition(AutomationElement.IsEnabledProperty, true),
                //	new PropertyCondition(AutomationElement.IsEnabledProperty, false)
                //);

                // Find all children of this parent
                AutomationElementCollection elementCollection = parent.FindAll(TreeScope.Subtree, Condition.TrueCondition);
                var match = false;
                foreach (AutomationElement child in elementCollection)
                {
                    var childID = child.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty) as string;
                    var childName = child.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
                    var childClass = child.GetCurrentPropertyValue(AutomationElement.ClassNameProperty) as string;
                    ReportLine("  Child ID: {0}", childID);
                    ReportLine("          Name: {0}", childName);
                    ReportLine("          ClassName: {0}", childClass);
                    if (IsAMatch(childID, ctlParam) ||
                        IsAMatch(childName, ctlParam) ||
                        IsAMatch(childClass, ctlParam))
                    {
                        ReportLine("!!!! ENTRY MATCHED !!!!\r\n");
                        match = true;
                        if (!_reportFlag)
                        {
                            Debug("ScanControlTree returning true");
                            return true;
                        }
                    }
                }
                if (!match) ReportLine("\r\nNo Match Found");
            }
            Debug("ScanControlTree returning false");
            return false;
        }


        /// <summary>
        /// Parse a {} delimited named parameter from a string
        /// Only honor the first instance
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string getParam(ref string value, string key)
        {
            string param = string.Empty;
            var rx = new Regex(string.Format("\\{{{0}:(?<paramValue>.*?)\\}}", key), RegexOptions.IgnoreCase);
            var rxmatches = rx.Matches(value);
            //there really should only be one match in the string
            if (rxmatches.Count == 1)
            {
                Match rxmatch = rxmatches[0];
                var rxgroup = rxmatch.Groups["paramValue"];
                param = rxgroup.Value;
                value = value.Substring(0, rxmatch.Index) + value.Substring(rxmatch.Index + rxmatch.Length);
            }
            return param;
        }


        /// <summary>
        /// Parse a {} delimited named flag from a string
        /// Only honor the first instance.
        /// A flag is just a name in {}
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool getFlag(ref string value, string key)
        {
            string param = string.Empty;
            var rx = new Regex(string.Format("\\{{{0}\\}}", key), RegexOptions.IgnoreCase);
            var rxmatches = rx.Matches(value);
            //there really should only be one match in the string
            if (rxmatches.Count == 1)
            {
                Match rxmatch = rxmatches[0];
                //remove the flag from the input string
                value = value.Substring(0, rxmatch.Index) + value.Substring(rxmatch.Index + rxmatch.Length);
                return true;
            }
            return false;
        }


        public bool IsAMatch(string value, string matchPattern)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (string.IsNullOrEmpty(matchPattern)) return false;

            //check if the targetName is actually a regex
            // it'll be "//regex here//" if it is
            bool bRegex = matchPattern.StartsWith(@"//") && matchPattern.EndsWith(@"//") && (matchPattern.Length > 4);
            Regex objRegex = null;
            if (bRegex)
            {
                try
                {
                    objRegex = new Regex(matchPattern.Substring(2, matchPattern.Length - 4), RegexOptions.IgnoreCase);
                }
                catch (Exception)
                {
                    bRegex = false;
                }
            }

            //if we've got a regex
            var match = false;
            if (bRegex)
            {
                //check it as a regex
                match = (objRegex.IsMatch(value));
            }
            else
            {
                //otherwise just use simple matching
                match = StrUtil.SimplePatternMatch(matchPattern, value, StrUtil.CaseIgnoreCmp);
            }

            return match;
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(UIntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName([In] UIntPtr hProcess, [In] int dwFlags, [Out] StringBuilder lpExeName, out int lpdwSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, UIntPtr dwProcessId);

        private static string GetExecutablePath(UIntPtr dwProcessId)
        {
            StringBuilder buffer = new StringBuilder(1024);

            const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
            UIntPtr hprocess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, dwProcessId);
            if (hprocess != UIntPtr.Zero)
            {
                try
                {
                    int size = buffer.Capacity;
                    if (QueryFullProcessImageName(hprocess, 0, buffer, out size))
                    {
                        return buffer.ToString();
                    }
                }
                finally
                {
                    CloseHandle(hprocess);
                }
            }
            return string.Empty;
        }


        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out UIntPtr lpdwProcessId);
        private string getExecutableFromHwnd(IntPtr hWnd)
        {
            var lpdwProcessId = new UIntPtr();
            var procId = GetWindowThreadProcessId(hWnd, out lpdwProcessId);
            return GetExecutablePath(lpdwProcessId);
        }


        /// <summary>
        /// called when KeePass is closing
        /// </summary>
        public override void Terminate()
        {
            Debug("Keepass terminating");

            AutoType.SequenceQuery -= AutoType_SequenceQuery;
            AutoType.SequenceQueriesBegin -= AutoType_SequenceQueriesBegin;
            AutoType.SequenceQueriesEnd -= AutoType_SequenceQueriesEnd;

            //make sure this form (if it exists) is closed.
            Debug("Releasing Report form");
            TestOutput.Release();
        }


        private static string _logFile;
        private static string LogFile
        {
            get
            {
                if (_logFile != null) return _logFile;

                _logFile = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Disambiguator.log");
                return _logFile;
            }
        }


        internal static void Debug(string template, params object[] args)
        {
            try
            {
                var buf = string.Format(template, args);
                File.AppendAllText(LogFile, string.Format("{0} {1}\r\n", DateTime.Now, buf));
                //System.Diagnostics.Debug.WriteLine(string.Format("Disambiguator: {0}", buf));
            }
            catch 
            {
            }
        }
    }
}