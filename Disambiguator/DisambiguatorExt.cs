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

        /// <summary>
        /// has reports been turned on in UI
        /// </summary>
        private bool _reportOn = false;

        /// <summary>
        /// used to turn logging on or off dynamically
        /// </summary>
        private static bool _loggingOn = false;

        /// <summary>
        /// is reporting on for this invocation
        /// </summary>
        private bool _report = false;
        
        private string _exePath = null;
        private string _exeFile = null;
        private int _matchCount = 0;
        private List<UIElement> _currentUIElements = null;


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

                    subMenu = new ToolStripMenuItem()
                    {
                        Text = "Logging",
                        CheckOnClick = true,
                        Checked = false,
                    };
                    subMenu.Click += this.OnSetLoggingClicked;
                    menuItem.DropDownItems.Add(subMenu);

                    subMenu = new ToolStripMenuItem()
                    {
                        Text = "Help",
                    };
                    subMenu.Click += this.OnHelpClicked;
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

        private void OnSetLoggingClicked(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            _loggingOn = menuItem.Checked;

            if (_loggingOn)
            {
                MessageBox.Show("Disambiguator Logging is now enabled\r\n\r\n" +
                    "With logging on, a Disambiguator.log file will be written to\r\n" +
                    "the current user's Desktop.\r\n\r\n" +
                    "It is recommended to only turn on logging when asked to by\r\n" +
                    "The Disambiguator development team."
                    , "The Disambiguator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnHelpClicked(object sender, EventArgs e)
        {
            new DisambiguatorHelp().ShowDialog();
        }


        private void OnSetReportClicked(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;

            _reportOn = menuItem.Checked;
            if (_reportOn)
            {
                MessageBox.Show("Disambiguator Reporting is now enabled\r\n\r\n" +
                    "With reporting on, pressing the AutoType hotkey will instead\r\n" +
                    "evaluate the target application and display a report of the\r\n" +
                    "executable name and various control details you can use\r\n" +
                    "to pinpoint the specific autotype sequence to use."
                    , "The Disambiguator", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                MessageBox.Show("Disambiguator Reporting is now disabled\r\n\r\n" +
                    "Standard autotype functionality will now resume."
                    , "The Disambiguator", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void OnEntryOptionsClicked(object sender, EventArgs e)
        {
            //MessageBox.Show("Only for test right now");
        }


        private void OnTrayOptionsClicked(object sender, EventArgs e)
        {
            //MessageBox.Show("Only for test right now");
        }


        private void OnGroupOptionsClicked(object sender, EventArgs e)
        {
            //MessageBox.Show("Only for test right now");
        }


        private void OnMainOptionsClicked(object sender, EventArgs e)
        {
            //MessageBox.Show("Only for test right now");
        }


        /// <summary>
        /// Event fired once when AutoType is invoked for a target window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoType_SequenceQueriesBegin(object sender, SequenceQueriesEventArgs e)
        {
            try
            {
                Debug("Sequence Queries Begin");

                _exePath = getExecutableFromHwnd(e.TargetWindowHandle).ToLower();
                _exeFile = Path.GetFileName(_exePath);
                _report = _reportOn;
                _matchCount = 0;

                //traverse the control tree for the target window to collect
                //a list of UIelements that we can use to disambiguate
                _currentUIElements = TraverseControlTree(e.TargetWindowHandle);

                //once the target app is analyzed, show any report window (if applicable)
                TestOutput.ShowOnTop();
            }
            catch (Exception ex)
            {
                Debug("Error In AutoType_SequenceQueriesBegin: " + ex.ToString());
            }
        }


        /// <summary>
        /// Traverse the control tree from the parent element and build of a list of control elements to be tested
        /// Do this only once per invocation because it could be expensive
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="ctlParam"></param>
        /// <returns></returns>
        private List<UIElement> TraverseControlTree(IntPtr targetWindowHandle)
        {
            Debug("Traversing Control Tree...");
            var uiElements = new List<UIElement>();

            try
            {
                ReportWrite("   Application of current target window: \"{0}\"", _exePath);

                //attempt to retrieve an accessible object from the target window handle
                var uiaObject = AutomationElement.FromHandle(targetWindowHandle);

                ReportWrite("   TargetWindow: {0} Resolved: {1}", targetWindowHandle, uiaObject != null);

                if (uiaObject != null)
                {
                    var parentID = uiaObject.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty);
                    ReportWrite("   Parent ID: {0}", parentID);

                    //use an always true OR condition
                    //probably a better way to do this, but this'll work for now.
                    //var condition = new OrCondition(
                    //	new PropertyCondition(AutomationElement.IsEnabledProperty, true),
                    //	new PropertyCondition(AutomationElement.IsEnabledProperty, false)
                    //);

                    // Find all children of this parent
                    AutomationElementCollection elementCollection = uiaObject.FindAll(TreeScope.Subtree, Condition.TrueCondition);
                    foreach (AutomationElement child in elementCollection)
                    {
                        var uiElement = new UIElement()
                        {
                            ID = child.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty) as string,
                            Name = child.GetCurrentPropertyValue(AutomationElement.NameProperty) as string,
                            Class = child.GetCurrentPropertyValue(AutomationElement.ClassNameProperty) as string,
                        };
                        uiElements.Add(uiElement);
                        ReportWrite("   Child ID: {0}", uiElement.ID);
                        ReportWrite("      Name : {0}", uiElement.Name);
                        ReportWrite("      Class: {0}", uiElement.Class);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug("Error while traversing Target App: " + ex.ToString());
                MessageBox.Show("An error was encountered:\r\n" + ex.ToString());
            }
            return uiElements;
        }


        /// <summary>
        /// Event fired once when Autotype sequence processes is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoType_SequenceQueriesEnd(object sender, SequenceQueriesEventArgs e)
        {
            Debug("Sequence Queries End. {0} Matched Sequences", _matchCount);
            _currentUIElements = null;
        }


        /// <summary>
        /// Event fired for each possible sequence to allow us to determine
        /// whether the target window matches the provided sequence
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoType_SequenceQuery(object sender, SequenceQueryEventArgs e)
        {
            try
            {
                //if reporting is on, we don't actually try to match anything
                if (_report) return;

                //main win title and AutoType sequence for this entry
                //we have to check this separately from the custom associations
                var autoTypeSequenceTitle = e.Entry.Strings.ReadSafe("Title");
                string entryAutoTypeSequence = e.Entry.GetAutoTypeSequence();

                try
                {
                    Debug("ResolveSequence for AutoType Sequence Title");
                    ResolveSequence(autoTypeSequenceTitle, entryAutoTypeSequence, e);
                }
                catch (Exception ex)
                {
                    Debug("Error In AutoType_SequenceQuery.Resolving Title: " + ex.ToString());
                }

                //run through the target window associations looking for match elements
                foreach (AutoTypeAssociation association in e.Entry.AutoType.Associations)
                {
                    //get the window name (this would usually contain the TITLE of the window
                    //that would match
                    try
                    { 
                    var winName = association.WindowName;
                    Debug("ResolveSequence for AutoType Association Name");
                    ResolveSequence(winName, association.Sequence, e);
                    }
                    catch (Exception ex)
                    {
                        Debug("Error In AutoType_SequenceQuery.Resolving AutoType Associations: " + ex.ToString());
                    }
                }

                Debug("Finished AutoType_SequenceQuery");
            }
            catch (Exception ex)
            {
                Debug("Error In AutoType_SequenceQuery: " + ex.ToString());
            }
        }


        public void ResolveSequence(string winName, string sequence, SequenceQueryEventArgs e)
        {
            //clear all parameters and flags
            string exeParam = string.Empty;
            string ctlParam = string.Empty;
            var match = false;

            //get the window name (this would usually contain the TITLE of the window
            //that would match
            if (winName == null)
            {
                Debug("Empty WindowName detected");
                return;
            }

            Debug("ResolveSequence winName={0}  sequence={1}  eventId={2}  TargetTitle={3}  TargetHandle={4}", winName, sequence, e.EventID, e.TargetWindowTitle, e.TargetWindowHandle);

            //next remove any app out of the window name
            //and try it

            //first compile the window name to replace all KeePass elements
            var matchTemplate = SprEngine.Compile(winName, new SprContext(e.Entry, e.Database, SprCompileFlags.All));

            exeParam = getParam(ref matchTemplate, "exe");
            ctlParam = getParam(ref matchTemplate, "ctl");
 
            if (!string.IsNullOrEmpty(exeParam))
            {
                Debug("Searching for EXE tag \"{0}\"", exeParam);
                if (exeParam.Contains(@"\"))
                {
                    //parameter looks like it's got a path element, so compare to the whole exeName
                    match = (IsAMatch(_exePath, exeParam));
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
                    match = (IsAMatch(_exeFile, exeParam));
                }
            }

            //Always enumerate child controls when reporting
            if (!match && !string.IsNullOrEmpty(ctlParam))
            {
                Debug("Searching for CTL tag \"{0}\"", ctlParam);
                //no match yet, check for any child controls
                Debug("Scanning Descendant Controls...");
                match = ScanControlTree(_currentUIElements, ctlParam);
            }

            //Lastly, the winName must match as well to be considered
            var title = e.TargetWindowTitle ?? string.Empty;
            var titleMatch = IsAMatch(title, matchTemplate);
            Debug("Checking for Window Title Match, title={0}, match={1}", title, titleMatch);
            match = match && titleMatch;

            //if reporting is on we DO NOT want to match
            //NOTE that other entries with reporting OFF +may still match+
            if (match)
            {
                Debug("Adding Sequence to found list");
                e.AddSequence(string.IsNullOrEmpty(sequence) ? e.Entry.GetAutoTypeSequence() : sequence);
                _matchCount++;
            }
        }


        /// <summary>
        /// Scan the already generated list of UIElements for the current target of this Autotype
        /// invocation, looking for any control matches
        /// </summary>
        /// <param name="uiElements"></param>
        /// <param name="ctlParam"></param>
        /// <returns></returns>
        private bool ScanControlTree(List<UIElement> uiElements, string ctlParam)
        {
            Debug("Testing Target UI Elements using ctlParam {0}", ctlParam);

            var matches = uiElements.Where(u => IsAMatch(u.ID, ctlParam) || IsAMatch(u.Name, ctlParam) || IsAMatch(u.Class, ctlParam)).ToList();
            if (matches.Any())
            {
                Debug("!!!! MATCHED {0} ENTRIES !!!!", matches.Count);
                matches.ForEach(m => Debug("  Matched on ID:{0} Name{1} Class{2}", m.ID, m.Name, m.Class));
                if (!_report)
                {
                    return true;
                }
            }
            else
            {
                Debug("No Match Found");
            }

            Debug("ScanControlTree returning false");
            return false;
        }


        /// <summary>
        /// used to cache and precompute regexes
        /// </summary>
        private static Dictionary<string, Regex> _regexes = new Dictionary<string, Regex>();

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
            Regex rx;
            _regexes.TryGetValue(key, out rx);
            if (rx == null) rx = new Regex(string.Format("\\{{{0}:(?<paramValue>.*?)\\}}", key), RegexOptions.IgnoreCase);
            var rxMatches = rx.Matches(value);
            //there really should only be one match in the string
            if (rxMatches.Count == 1)
            {
                Match rxMatch = rxMatches[0];
                var rxGroup = rxMatch.Groups["paramValue"];
                param = rxGroup.Value;
                value = value.Substring(0, rxMatch.Index) + value.Substring(rxMatch.Index + rxMatch.Length);
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


        /// <summary>
        /// Checks a string value for a match against the matchPattern
        /// </summary>
        /// <param name="value"></param>
        /// <param name="matchPattern"></param>
        /// <returns></returns>
        private bool IsAMatch(string value, string matchPattern)
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
            if (!_loggingOn) return;
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


        /// <summary>
        /// Write an empty line to the report
        /// </summary>
        private void ReportWrite() { ReportWrite(""); }


        /// <summary>
        /// Write a line to the report output window with formatting
        /// </summary>
        /// <param name="template"></param>
        /// <param name="args"></param>
        private void ReportWrite(string template, params object[] args)
        {
            if (_report)
            {
                if (!string.IsNullOrEmpty(template)) Debug("REPORT: " + template, args);
                TestOutput.WriteLine(template, args);
            }
        }
    }


    internal class UIElement
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
    }
}