using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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


namespace Disambiguator
{
    /// <summary>
    /// Main Disambiguator plugin
    /// 
    /// NOTE: do NOT use string interpolation anywhere, use string.Format()
    /// PLGX format won't work with string interpolation
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
        /// is reporting on for this invocation
        /// In other words, reporting MIGHT have been turned on by the {REPORT} tag
        /// </summary>
        private bool _report = false;

        /// <summary>
        /// used to turn logging on or off dynamically
        /// </summary>
        private static bool _loggingOn = false;

        /// <summary>
        /// used to control the depth of control tree traversal.
        /// Anything more than 2 or 3 is not recommended.
        /// </summary>
        private static int _depth = 3;

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

            // Initialize native DLL loader for Tesseract
            NativeDllLoader.Initialize();

            AutoType.SequenceQuery += AutoType_SequenceQuery;
            AutoType.SequenceQueriesBegin += AutoType_SequenceQueriesBegin;
            AutoType.SequenceQueriesEnd += AutoType_SequenceQueriesEnd;

            Debug("Initialization done");
            return true; // Initialization successful
        }


        /// <summary>
        /// Provide our menu item to KeePass
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
                //a list of UIElements that we can use to disambiguate
                _currentUIElements = TraverseControlTree(e.TargetWindowHandle, _depth);

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
        /// This version traverses the ENTIRE control tree from the given window on down.
        /// Depending on depth, this could take significant time
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="ctlParam"></param>
        /// <returns></returns>
        private List<UIElement> TraverseControlTree(IntPtr targetWindowHandle, int depth)
        {
            Debug("Traversing Control Tree...");
            var uiElements = new List<UIElement>();

            //guard condition, not target handle means nothing we can do
            //so just return an empty element list
            if (targetWindowHandle.Equals(IntPtr.Zero)) return uiElements;

            try
            {
                ReportWrite("   Application of current target window: \"{0}\"", _exePath);

                var uiElement = UIElementFromWindowHandle(targetWindowHandle);
                uiElements.Add(uiElement);
                var indent = "   ";
                ReportWrite(indent, uiElement);

                //Doesn't appear to be of much value
                //var parentID = uiaObject.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty);
                //ReportWrite("{0}ParentID: {0}", parentID);

                //add children down to requested depth
                uiElements.AddRange(TraverseControlChildren(uiElement.uiaObject, depth, indent));
            }
            catch (Exception ex)
            {
                Debug("Error while traversing Target App: " + ex.ToString());
                MessageBox.Show("An error was encountered:\r\n" + ex.ToString());
            }
            return uiElements;
        }


        /// <summary>
        /// Traverse the given controls  children, recursively down to the given depth
        /// NOTE: depth of -1 means traverse the entire tree. Not recommended.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="ctlParam"></param>
        /// <returns></returns>
        private List<UIElement> TraverseControlChildren(AutomationElement uiaObject, int depth, string indent)
        {
            var uiElements = new List<UIElement>();

            //once we've drilled down to the required depth, don't go any further
            depth--;
            if (depth == 0) return uiElements;
            indent = indent + "   ";

            try
            {
                // Find all children of this element
                AutomationElementCollection elementCollection = uiaObject.FindAll(TreeScope.Children, Condition.TrueCondition);
                foreach (AutomationElement child in elementCollection)
                {
                    var uiElement = UIElementFromAutomationElement(child);
                    uiElements.Add(uiElement);
                    ReportWrite(indent, uiElement);
                    
                    uiElements.AddRange(TraverseControlChildren(child, depth, indent));
                }
            }
            catch (Exception ex)
            {
                Debug("Error while traversing Target App: " + ex.ToString());
                MessageBox.Show("An error was encountered:\r\n" + ex.ToString());
            }
            return uiElements;
        }


        private UIElement UIElementFromWindowHandle(IntPtr hWnd)
        {
            UIElement uiElement = null;
            var uiaObject = AutomationElement.FromHandle(hWnd);
            if (uiaObject == null) return uiElement;

            uiElement = new UIElement()
            {
                hWnd = hWnd,
                uiaObject = uiaObject,
            };

            return ResolveUIElement(uiElement);
        }


        private UIElement UIElementFromAutomationElement(AutomationElement uiaObject)
        {
            UIElement uiElement = null;
            if (uiaObject == null) return uiElement;
            var hWnd = new IntPtr((int)uiaObject.GetCurrentPropertyValue(AutomationElement.NativeWindowHandleProperty));
            uiElement = new UIElement()
            {
                hWnd = hWnd,
                uiaObject = uiaObject,
            };
            return ResolveUIElement(uiElement);
        }


        private UIElement ResolveUIElement(UIElement uiElement)
        {
            //add the root control element to the list
            string ID = string.Empty;
            string Name = ID;
            string Class = ID;
            string TheControlType = string.Empty;
            string Text = string.Empty;
            Rectangle Bounds = new Rectangle();

            //attempt each of these resolutions, but if they fail, just ignore
            const string FAILED = "!Failed to resolve!";
            var msg = FAILED;
            try
            {
                ID = uiElement.uiaObject.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty) as string;
            }
            catch (Exception ex)
            {
                Debug("Unable to resolve AutomationID: " + ex.ToString());
                ID = msg;
            };
            try
            {
                Name = uiElement.uiaObject.GetCurrentPropertyValue(AutomationElement.NameProperty) as string;
            }
            catch (Exception ex)
            {
                Debug("Unable to resolve Name: " + ex.ToString());
                Name = msg;
            };
            try
            {
                Class = uiElement.uiaObject.GetCurrentPropertyValue(AutomationElement.ClassNameProperty) as string;
            }
            catch (Exception ex)
            {
                Debug("Unable to resolve ClassName: " + ex.ToString());
                Class = msg;
            };
            try
            {
                TheControlType = uiElement.uiaObject.GetCurrentPropertyValue(AutomationElement.ControlTypeProperty) as string;
            }
            catch (Exception ex)
            {
                Debug("Unable to resolve ControlType: " + ex.ToString());
                TheControlType = msg;
            };
            try
            {
                Bounds = uiElement.uiaObject.GetBounds();
            }
            catch (Exception ex)
            {
                Debug("Unable to resolve BoundingRectangle: " + ex.ToString());
            };

            //if we've got bounds, try several approaches to get the control text
            if (!Bounds.IsEmpty)
            {
                try
                {
                    Text = TextExtractor.ExtractText(uiElement);
                }
                catch (Exception ex)
                {
                    Debug("Unable to extract raw control text: " + ex.ToString());
                }
            }

            uiElement.ID = ID;
            uiElement.Name = Name;
            uiElement.Class = Class;
            uiElement.Text = Text;
            uiElement.Type = TheControlType;
            uiElement.Bounds = Bounds;

            return uiElement;
        }

        /// <summary>
        /// Event fired once when Autotype sequence processes is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoType_SequenceQueriesEnd(object sender, SequenceQueriesEventArgs e)
        {
            var msg = string.Format("Sequence Queries Ended with {0} Matched Sequence{1}. ", _matchCount, _matchCount > 1 ? "s" : "");
            if (_report)
            {
                msg = msg + "Reporting has been turned on, so actual matching is disabled. ";
            }
            Debug(msg);

            //free the memory
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
                    MatchSequence(autoTypeSequenceTitle, entryAutoTypeSequence, e);
                }
                catch (Exception ex)
                {
                    Debug("Error In AutoType_SequenceQuery matching title. Error: {0}", ex);
                }

                //run through the target window associations looking for match elements
                foreach (AutoTypeAssociation association in e.Entry.AutoType.Associations)
                {
                    //get the window name (this would usually contain the TITLE of the window
                    //that would match
                    try
                    {
                        var winName = association.WindowName;
                        MatchSequence(winName, association.Sequence, e);
                    }
                    catch (Exception ex)
                    {
                        Debug("Error In AutoType_SequenceQuery matching AutoType Association, Error: {0}", ex);
                    }
                }

                Debug("Finished AutoType_SequenceQuery");
            }
            catch (Exception ex)
            {
                Debug("Error In AutoType_SequenceQuery: {0}", ex);
            }
        }


        /// <summary>
        /// Given a windowName and a sequence, determine whether there's match and if so
        /// add it to the list of matches
        /// </summary>
        /// <param name="winName"></param>
        /// <param name="sequence"></param>
        /// <param name="e"></param>
        public void MatchSequence(string winName, string sequence, SequenceQueryEventArgs e)
        {
            //clear all parameters and flags
            string exeParam = string.Empty;
            string ctlParam = string.Empty;
            var match = false;

            Debug("MatchSequence winName='{0}'  sequence='{1}'  eventId='{2}'  TargetTitle='{3}'  TargetHandle='{4}'", winName, sequence, e.EventID, e.TargetWindowTitle, e.TargetWindowHandle);

            //get the window name (this would usually contain the TITLE of the window
            //that would match
            if (string.IsNullOrWhiteSpace(winName))
            {
                Debug("   winName should not be empty. Skipping...");
                return;
            }

            //first compile the window name to replace all KeePass elements
            var matchTemplate = SprEngine.Compile(winName, new SprContext(e.Entry, e.Database, SprCompileFlags.All));

            //pull out any exe or ctl parameters
            exeParam = getParam(ref matchTemplate, "exe");
            ctlParam = getParam(ref matchTemplate, "ctl");

            //First, the winName must match to be considered
            var title = e.TargetWindowTitle ?? string.Empty;
            match = IsAMatch(title, matchTemplate);
            if (!match)
            {
                Debug("   Window Title '{0}' did not match", title);
                return;
            }

            //now check if any exeParam matches
            //reset match
            match = false;
            if (!string.IsNullOrEmpty(exeParam))
            {
                Debug("   Searching for EXE tag '{0}'", exeParam);
                if (exeParam.Contains(@"\"))
                {
                    //parameter looks like it's got a path element, so compare to the whole exeName
                    match = (IsAMatch(_exePath, exeParam));
                    if (!match) Debug("   Executable path '{0}' did not match '{1}'", _exePath, exeParam);
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
                    if (!match) Debug("   Executable path '{0}' did not match '{1}'", _exePath, exeParam);
                }
            }

            //no EXE match, so check for any child control matches
            if (!match && !string.IsNullOrEmpty(ctlParam))
            {
                match = MatchOnControlTree(_currentUIElements, ctlParam);
            }

            //if reporting is on we DO NOT want to match
            //!!!!NOTE!!!! Other keePass key entries +may still match+ if reporting is ON and could thus perform autotype!
            if (match)
            {
                Debug("   Sequence matched.");
                if (!_report)
                {
                    Debug("   Adding Sequence to found list");
                    e.AddSequence(string.IsNullOrEmpty(sequence) ? e.Entry.GetAutoTypeSequence() : sequence);
                    _matchCount++;
                }
                else
                {
                    Debug("   Reporting on, so actual matching is disabled.");
                }
            }
            else
            {
                Debug("   Sequence did not match.");
            }
        }


        /// <summary>
        /// Scan the already generated list of UIElements for the current target of this Autotype
        /// invocation, looking for any control matches
        /// </summary>
        /// <param name="uiElements"></param>
        /// <param name="ctlParam"></param>
        /// <returns>true if a match is detected</returns>
        private bool MatchOnControlTree(List<UIElement> uiElements, string ctlParam)
        {
            Debug("   Scanning control tree using ctlParam '{0}'", ctlParam);

            var matches = uiElements.Where(u => 
                IsAMatch(u.ID, ctlParam) || 
                IsAMatch(u.Name, ctlParam) ||
                IsAMatch(u.Class, ctlParam) ||
                IsAMatch(u.Type, ctlParam) ||
                IsAMatch(u.Text, ctlParam) 
                ).ToList();
            if (matches.Any())
            {
                matches.ForEach(m => Debug("      Matched on ID:'{0}' Name:'{1}'  Class:'{2}'  Type:'{3}'  Text: '{4}'", m.ID, m.Name, m.Class, m.Type, m.Text));
                return true;
            }

            Debug("      No Match Found");
            return false;
        }


        /// <summary>
        /// used to cache and precompute regex's
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
                //attempt to compile the regex
                try
                {
                    objRegex = new Regex(matchPattern.Substring(2, matchPattern.Length - 4), RegexOptions.IgnoreCase);
                }
                catch (Exception)
                {
                    //if it fails to compile, assume it's NOT a regex
                    bRegex = false;
                }
            }

            var match = false;
            //if we've got a regex
            if (bRegex)
            {
                //try to match as a regex
                match = (objRegex.IsMatch(value));
                Debug("   Matching '{0}' to '{1}' as regex resulted in {2}", value, matchPattern, match ? "!!!MATCH!!!" : "no match");
            }
            else
            {
                //otherwise just use simple matching
                match = StrUtil.SimplePatternMatch(matchPattern, value, StrUtil.CaseIgnoreCmp);
                Debug("   Matching '{0}' to '{1}' as pattern resulted in {2}", value, matchPattern, match ? "!!!MATCH!!!" : "no match");
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
            Debug("KeePass terminating");

            AutoType.SequenceQuery -= AutoType_SequenceQuery;
            AutoType.SequenceQueriesBegin -= AutoType_SequenceQueriesBegin;
            AutoType.SequenceQueriesEnd -= AutoType_SequenceQueriesEnd;

            //make sure this form (if it exists) is closed.
            Debug("Releasing Report form");
            TestOutput.Release();

            // Clean up native DLL loader
            NativeDllLoader.Cleanup();
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


        /// <summary>
        /// Simple logging function
        /// </summary>
        /// <param name="template"></param>
        /// <param name="args"></param>
        internal static void Debug(string template, params object[] args)
        {
            if (_loggingOn)
            {
                try
                {
                    var buf = template;
                    if (args != null && args.Length > 0)
                    {
                        try
                        {
                            buf = string.Format(buf, args);
                        }
                        catch { }
                    }
                    File.AppendAllText(LogFile, string.Format("{0} {1}\r\n", DateTime.Now, buf));
                }
                catch { }
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
            if (!string.IsNullOrEmpty(template)) Debug("REPORT: " + template, args);

            if (_report)
            {
                TestOutput.WriteLine(template, args);
            }
        }

        private void ReportWrite(string indent, UIElement uiElement)
        {
            if (_report && uiElement != null)
            {
                ReportWrite("{0}WindowID: {1}", indent, uiElement.ID);
                ReportWrite("{0}  Bounds: {1},{2},{3},{4}", indent, uiElement.Bounds.X, uiElement.Bounds.Y, uiElement.Bounds.Width, uiElement.Bounds.Height);
                ReportWrite("{0}  Name  : {1}", indent, uiElement.Name);
                ReportWrite("{0}  Class : {1}", indent, uiElement.Class);
                ReportWrite("{0}  Text  : {1}", indent, uiElement.Text);
                ReportWrite("{0}  Type  : {1}", indent, uiElement.Type);
            }
        }
    }


    internal class UIElement
    {
        public IntPtr hWnd { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public Rectangle Bounds { get; set; }
        public AutomationElement uiaObject { get; set; }
        }
}