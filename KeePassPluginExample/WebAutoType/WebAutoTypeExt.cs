using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
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

namespace WebAutoType
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class WebAutoTypeExt : Plugin
	{
		private IPluginHost m_host;

		private const string IpcCreateEntryEventName = "WebAutoType.CreateEntry";

		internal const string UrlAutoTypeWindowTitlePrefix = "??:URL:";
		private const string OptionsConfigRoot = "WebAutoType.";
		private const string UserNameAutoTypeSequenceStart = "{USERNAME}{TAB}";
		private const string ExtraControlsName = "WebAutoTypeControls";
		private const string ExistingControlsContainerName = "WebAutoTypeOriginalControls";

		private Dictionary<int, string> mUrlForAutoTypeEvent = new Dictionary<int, string>();
		private Dictionary<int, bool> mSkipUserNameForSequence = new Dictionary<int, bool>();
		private ToolStripMenuItem mOptionsMenu;
		private int mCreateEntryHotkeyId;

		private readonly HashSet<int> mFoundSequence = new HashSet<int>();
		private readonly HashSet<string> mUnfoundUrls = new HashSet<string>();
		private FieldInfo mSearchTextBoxField;

		private ChromeAccessibilityWinEventHook mChromeAccessibility;

		public override string UpdateUrl
		{
			get { return "sourceforge-version://WebAutoType/webautotype?-v(%5B%5Cd.%5D%2B)%5C.zip"; }
		}

		public override bool Initialize(IPluginHost host)
		{
			Debug.Assert(host != null);
			if (host == null)
				return false;

			m_host = host;

			GlobalWindowManager.WindowAdded += GlobalWindowManager_WindowAdded;
			AutoType.SequenceQuery += AutoType_SequenceQuery;
			AutoType.SequenceQueriesBegin += AutoType_SequenceQueriesBegin;
			AutoType.SequenceQueriesEnd += AutoType_SequenceQueriesEnd;

			mOptionsMenu = new ToolStripMenuItem
			{
				Text = Properties.Resources.OptionsMenuItemText,
			};
			mOptionsMenu.Click += mOptionsMenu_Click;

			m_host.MainWindow.ToolsMenu.DropDownItems.Add(mOptionsMenu);

			IpcUtilEx.IpcEvent += OnIpcEvent;
			HotKeyManager.HotKeyPressed += HotKeyManager_HotKeyPressed;

			if (CreateEntryHotKey != Keys.None)
			{
				mCreateEntryHotkeyId = HotKeyManager.RegisterHotKey(CreateEntryHotKey);
			}

			mSearchTextBoxField = typeof(SearchForm).GetField("m_tbSearch", BindingFlags.Instance | BindingFlags.NonPublic);

			mChromeAccessibility = new ChromeAccessibilityWinEventHook();

			return true; // Initialization successful
		}

		private void mOptionsMenu_Click(object sender, EventArgs e)
		{
			using (var options = new Options(m_host))
			{
				options.MatchUrlField = MatchUrlField;
				options.CreateEntryHotKey = CreateEntryHotKey;
				options.CreateEntryTargetGroup = CreateEntryTargetGroup;
				options.AutoSkipUserName = AutoSkipUserName;
				options.ShowRepeatedSearch = ShowRepeatedSearch;

				if (options.ShowDialog(m_host.MainWindow) == DialogResult.OK)
				{
					MatchUrlField = options.MatchUrlField;
					CreateEntryHotKey = options.CreateEntryHotKey;
					CreateEntryTargetGroup = options.CreateEntryTargetGroup;
					AutoSkipUserName = options.AutoSkipUserName;
					ShowRepeatedSearch = options.ShowRepeatedSearch;

					// Unregister the old hotkey, and register the new
					if (mCreateEntryHotkeyId != 0)
					{
						HotKeyManager.UnregisterHotKey(mCreateEntryHotkeyId);
						mCreateEntryHotkeyId = 0;
					}
					if (CreateEntryHotKey != Keys.None)
					{
						mCreateEntryHotkeyId = HotKeyManager.RegisterHotKey(CreateEntryHotKey);
					}
				}
			}
		}

		#region Options
		private bool MatchUrlField
		{
			get { return m_host.CustomConfig.GetBool(OptionsConfigRoot + "MatchUrlField", true); }
			set { m_host.CustomConfig.SetBool(OptionsConfigRoot + "MatchUrlField", value); }
		}

		private Keys CreateEntryHotKey
		{
			get { return (Keys)m_host.CustomConfig.GetULong(OptionsConfigRoot + "CreateEntryHotKey", (ulong)Keys.None); }
			set { m_host.CustomConfig.SetULong(OptionsConfigRoot + "CreateEntryHotKey", (ulong)value); }
		}

		private PwUuid CreateEntryTargetGroup
		{
			get
			{
				var hexString = m_host.CustomConfig.GetString(OptionsConfigRoot + "CreateEntryTargetGroup", null);
				if (String.IsNullOrEmpty(hexString))
				{
					return null;
				}
				return new PwUuid(MemUtil.HexStringToByteArray(hexString));
			}
			set
			{
				m_host.CustomConfig.SetString(OptionsConfigRoot + "CreateEntryTargetGroup", value == null ? "" : value.ToHexString());
			}
		}

		private bool AutoSkipUserName
		{
			get { return m_host.CustomConfig.GetBool(OptionsConfigRoot + "AutoSkipUserName", false); }
			set { m_host.CustomConfig.SetBool(OptionsConfigRoot + "AutoSkipUserName", value); }
		}

		private bool ShowRepeatedSearch
		{
			get { return m_host.CustomConfig.GetBool(OptionsConfigRoot + "ShowRepeatedSearch", false); }
			set { m_host.CustomConfig.SetBool(OptionsConfigRoot + "ShowRepeatedSearch", value); }
		}

		#endregion

		private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
		{
			CreateEntry();
		}

		private void OnIpcEvent(object sender, IpcEventArgs ipcEventArgs)
		{
			if (ipcEventArgs.Name.Equals(IpcCreateEntryEventName, StringComparison.InvariantCultureIgnoreCase))
			{
				m_host.MainWindow.BeginInvoke(new Action(CreateEntry));
			}
		}

		private bool mCreatingEntry = false;

		private void CreateEntry()
		{
			if (mCreatingEntry) return;
			mCreatingEntry = true;
			try
			{

				// Unlock, if required
				m_host.MainWindow.ProcessAppMessage((IntPtr)Program.AppMessage.Unlock, IntPtr.Zero);

				if (m_host.MainWindow.IsAtLeastOneFileOpen())
				{
					string selectedText, url, title;
					WebBrowserUrl.GetFocusedBrowserInfo(mChromeAccessibility, out selectedText, out url, out title);

					var urlSuggestions = new List<String>();
					if (!String.IsNullOrEmpty(url))
					{
						// Use only the root part of the URL
						try
						{
							var uri = new Uri(url);
							urlSuggestions.Add(uri.GetLeftPart(UriPartial.Authority) + "/");
							urlSuggestions.Add(uri.GetLeftPart(UriPartial.Path));
							urlSuggestions.Add(uri.GetLeftPart(UriPartial.Query));
						}
						catch (UriFormatException)
						{
						}
						// Finally, the url exactly as given
						urlSuggestions.Add(url);
					}

					// Logic adapted from EntryTemplates.CreateEntry
					var database = m_host.Database;
					var entry = new PwEntry(true, true);
					if (!String.IsNullOrEmpty(title)) entry.Strings.Set(PwDefs.TitleField, new ProtectedString(database.MemoryProtection.ProtectTitle, title));
					if (urlSuggestions.Any()) entry.Strings.Set(PwDefs.UrlField, new ProtectedString(database.MemoryProtection.ProtectUrl, urlSuggestions[0]));
					if (!String.IsNullOrEmpty(selectedText)) entry.Strings.Set(PwDefs.UserNameField, new ProtectedString(database.MemoryProtection.ProtectUserName, selectedText));

					// Generate a default password, the same as in MainForm.OnEntryAdd
					ProtectedString psAutoGen;
					PwGenerator.Generate(out psAutoGen, Program.Config.PasswordGenerator.AutoGeneratedPasswordsProfile, null, Program.PwGeneratorPool);
					psAutoGen = psAutoGen.WithProtection(database.MemoryProtection.ProtectPassword);
					entry.Strings.Set(PwDefs.PasswordField, psAutoGen);


					PwGroup group = database.RootGroup;
					if (CreateEntryTargetGroup != null)
					{
						group = database.RootGroup.FindGroup(CreateEntryTargetGroup, true) ?? database.RootGroup;
					}

					// Set parent group temporarily, so that the AutoType tab, and other plugins such as PEDCalc, can obtain it in the PwEntryForm.
					//entry.ParentGroup = group;
					var parentGroupProperty = typeof(PwEntry).GetProperty("ParentGroup", BindingFlags.Instance | BindingFlags.Public);
					if (parentGroupProperty != null) parentGroupProperty.SetValue(entry, @group);

					using (var entryForm = new PwEntryForm())
					{
						entryForm.InitEx(entry, PwEditMode.AddNewEntry, database, m_host.MainWindow.ClientIcons, false, true);

						// Customise entry form to show drop-down for selecting URL
						var urlBox = entryForm.Controls.Find("m_tbUrl", true).FirstOrDefault();
						if (urlBox != null)
						{
							var urlCombo = new ComboBox
							{
								DropDownStyle = ComboBoxStyle.DropDown,
								TabIndex = urlBox.TabIndex,
								Text = urlBox.Text,
							};
							foreach (var urlSuggestion in urlSuggestions.Distinct())
							{
								urlCombo.Items.Add(urlSuggestion);
							}
							var syncPos = new EventHandler(delegate { urlCombo.SetBounds(urlBox.Left, urlBox.Top, urlBox.Width, urlBox.Height); });
							urlBox.Resize += syncPos;
							syncPos(null, EventArgs.Empty); // Initial sizing
							urlBox.Parent.Controls.Add(urlCombo);
							urlBox.Visible = false;

							// Sync text
							urlCombo.TextChanged += delegate { urlBox.Text = urlCombo.Text; };
							urlBox.TextChanged += delegate { urlCombo.Text = urlBox.Text; };
						}

						if (ShowForegroundDialog(entryForm) == DialogResult.OK)
						{
							group.AddEntry(entry, true, true);
							m_host.MainWindow.UpdateUI(false, null, database.UINeedsIconUpdate, null, true, null, true);
						}
						else
						{
							m_host.MainWindow.UpdateUI(false, null, database.UINeedsIconUpdate, null, database.UINeedsIconUpdate, null, false);
						}
					}
				}
			}
			finally
			{
				mCreatingEntry = false;
			}
		}

		private void ShowSearchDialog(string searchText)
		{
			using (var searchForm = new SearchForm())
			{
				searchForm.InitEx(m_host.Database, m_host.Database.RootGroup);
				if (mSearchTextBoxField != null)
				{
					var searchTextBox = mSearchTextBoxField.GetValue(searchForm) as TextBox;
					if (searchTextBox != null)
					{
						searchTextBox.Text = searchText;
					}
				}

				if (ShowForegroundDialog(searchForm) == DialogResult.OK)
				{
					m_host.MainWindow.UpdateUI(false, null, false, null, true, searchForm.SearchResultsGroup, false);

					// Things that can't be done without reflection:
					//m_host.MainWindow.ShowSearchResultsStatusMessage();
					//m_host.MainWindow.SelectFirstEntryIfNoneSelected();
					//m_host.MainWindow.ResetDefaultFocus(m_host.MainWindow.m_lvEntries);

					m_host.MainWindow.EnsureVisibleForegroundWindow(true, true);
				}
			}
		}

		private DialogResult ShowForegroundDialog(Form form)
		{
			m_host.MainWindow.EnsureVisibleForegroundWindow(false, false);
			form.StartPosition = FormStartPosition.CenterScreen;
			if (m_host.MainWindow.IsTrayed())
			{
				form.ShowInTaskbar = true;
			}

			form.Shown += FormOnShown;
			return form.ShowDialog(m_host.MainWindow);
		}

		private void FormOnShown(object sender, EventArgs eventArgs)
		{
			var form = (Form)sender;
			form.Shown -= FormOnShown;
			form.Activate();
		}

		private void AutoType_SequenceQueriesBegin(object sender, SequenceQueriesEventArgs e)
		{
			if (!BrowserUrlReader.IsWindowHandleSupportedBrowser(e.TargetWindowHandle))
			{
				return;
			}

			bool passwordFieldFocussed = false;

			string sUrl = WebBrowserUrl.GetFocusedBrowserUrl(mChromeAccessibility, e.TargetWindowHandle, out passwordFieldFocussed);

			if (!string.IsNullOrEmpty(sUrl))
			{
				lock (mUrlForAutoTypeEvent)
				{
					mUrlForAutoTypeEvent[e.EventID] = sUrl;
					mSkipUserNameForSequence[e.EventID] = passwordFieldFocussed && AutoSkipUserName;
				}

				// Ensure starting un-found.
				mFoundSequence.Remove(e.EventID);
			}
		}

		private void AutoType_SequenceQueriesEnd(object sender, SequenceQueriesEventArgs e)
		{
			lock (mUrlForAutoTypeEvent)
			{
				if (ShowRepeatedSearch)
				{
					string url;
					if (mUrlForAutoTypeEvent.TryGetValue(e.EventID, out url))
					{
						if (!mFoundSequence.Remove(e.EventID))
						{
							// Unsuccessful autotype
							if (mUnfoundUrls.Remove(url))
							{
								// Second unsuccessful auto-type for the same URL, show the search window
								m_host.MainWindow.BeginInvoke(new Action(() => ShowSearchDialog(url)));
							}
							else
							{
								// First unsuccessful auto-type, record the URL
								mUnfoundUrls.Add(url);
							}
						}
						else
						{
							// Successful autotype
							mUnfoundUrls.Remove(url);
						}
					}
				}

				mUrlForAutoTypeEvent.Remove(e.EventID);
			}
		}

		private void AutoType_SequenceQuery(object sender, SequenceQueryEventArgs e)
		{
			string entryAutoTypeSequence = e.Entry.GetAutoTypeSequence();

			string url;
			lock (mUrlForAutoTypeEvent)
			{
				if (!mUrlForAutoTypeEvent.TryGetValue(e.EventID, out url))
				{
					return;
				}

				bool skipUserName = false;
				mSkipUserNameForSequence.TryGetValue(e.EventID, out skipUserName);

				if (skipUserName && entryAutoTypeSequence.StartsWith(UserNameAutoTypeSequenceStart, StrUtil.CaseIgnoreCmp))
				{
					entryAutoTypeSequence = entryAutoTypeSequence.Substring(UserNameAutoTypeSequenceStart.Length);
				}
			}

			var matchFound = false;
			foreach (AutoTypeAssociation association in e.Entry.AutoType.Associations)
			{
				string strUrlSpec = association.WindowName;
				if (strUrlSpec == null)
				{
					continue;
				}

				strUrlSpec = strUrlSpec.Trim();

				if (!strUrlSpec.StartsWith(UrlAutoTypeWindowTitlePrefix) || strUrlSpec.Length <= UrlAutoTypeWindowTitlePrefix.Length)
				{
					continue;
				}

				strUrlSpec = strUrlSpec.Substring(7);

				if (strUrlSpec.Length > 0)
				{
					strUrlSpec = SprEngine.Compile(strUrlSpec, new SprContext(e.Entry, e.Database, SprCompileFlags.All));
				}

				bool bRegex = strUrlSpec.StartsWith(@"//") && strUrlSpec.EndsWith(@"//") && (strUrlSpec.Length > 4);
				Regex objRegex = null;

				if (bRegex)
				{
					try
					{
						objRegex = new Regex(strUrlSpec.Substring(2, strUrlSpec.Length - 4), RegexOptions.IgnoreCase);
					}
					catch (Exception)
					{
						bRegex = false;
					}
				}

				if (bRegex)
				{
					if (objRegex.IsMatch(url))
					{
						e.AddSequence(string.IsNullOrEmpty(association.Sequence) ? entryAutoTypeSequence : association.Sequence);
						matchFound = true;
					}
				}
				else if (StrUtil.SimplePatternMatch(strUrlSpec, url, StrUtil.CaseIgnoreCmp))
				{
					e.AddSequence(string.IsNullOrEmpty(association.Sequence) ? entryAutoTypeSequence : association.Sequence);
					matchFound = true;
				}
			}

			if (MatchUrlField)
			{
				var urlFieldValue = e.Entry.Strings.GetSafe(KeePassLib.PwDefs.UrlField).ReadString();

				var match = Regex.Match(urlFieldValue, @"^(?<scheme>\w+://)?(?<credentials>[^@/]+@)?(?<host>[^/]+?)(?<port>:\d+)?(?<path>/.*)?$");
				if (match.Success)
				{
					// Convert URL into regex to match subdomains and sub-paths
					var urlRegex = "^" + // Must be start of string
					               GetValueOrDefault(match, "scheme", "https?://") + // Scheme or assume http/s
					               Regex.Escape(match.Groups["credentials"].Value) + // Credentials if present, otherwise assert none
					               @"(\w+\.)*" + // Allow any number of subdomains
					               Regex.Escape(match.Groups["host"].Value) + // Host part
					               GetValueOrDefault(match, "port", @"(:\d+)?") + // Exact port if specified, otherwise any or no port.
					               GetValueOrDefault(match, "path", "(?:/|$)") + // Path part as specified, or ensure host ends with / or end of url
					               ".*$"; // Allow anything at the end of the url

					matchFound = Regex.IsMatch(url, urlRegex);
				}
				else
				{
					// Can't parse URL field value as URL, so fall back on plain equals
					matchFound = urlFieldValue.Equals(url, StrUtil.CaseIgnoreCmp);
				}
				
				if (matchFound)
				{
					e.AddSequence(entryAutoTypeSequence);
				}
			}

			if (matchFound && ShowRepeatedSearch)
			{
				lock (mUrlForAutoTypeEvent)
				{
					mFoundSequence.Add(e.EventID);
				}
			}
		}
		
		private static string GetValueOrDefault(Match match, string groupName, string defaultRegex)
		{
			var matchGroup = match.Groups[groupName];
			if (matchGroup.Success)
			{
				return Regex.Escape(matchGroup.Value);
			}

			return defaultRegex;
		}

		#region Edit AutoType Window Customisation
		private void GlobalWindowManager_WindowAdded(object p_sender, GwmWindowEventArgs p_e)
		{
			var editAutoTypeItemForm = p_e.Form as EditAutoTypeItemForm;
			if (editAutoTypeItemForm != null)
			{
				CustomiseEditFormUI(editAutoTypeItemForm);
			}
		}

		private void CustomiseEditFormUI(EditAutoTypeItemForm editForm)
		{
			if (editForm.Controls.Find(ExtraControlsName, false).Any())
			{
				Debug.Fail("Form already customised");
				return;
			}

			var editSequenceOnlyField = typeof(EditAutoTypeItemForm).GetField("m_bEditSequenceOnly", BindingFlags.Instance | BindingFlags.NonPublic);
			if (editSequenceOnlyField != null)
			{
				if (editSequenceOnlyField.GetValue(editForm) as bool? == true)
				{
					// This is a sequence-only edit window, so don't customise it.
					return;
				}
			}

			editForm.SuspendLayout();
			try
			{
				var banner = editForm.Controls.Find("m_bannerImage", false).FirstOrDefault();
				var placeholders = editForm.Controls.Find("m_rtbPlaceholders", false).FirstOrDefault();


				// Add the new control, docked just below the banner
				var extraControls = new EditAutoTypeExtraControls(editForm)
				{
					Name = ExtraControlsName,
					Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
					AutoSizeMode = AutoSizeMode.GrowOnly,
				};

				editForm.Controls.Add(extraControls);
				editForm.Controls.SetChildIndex(extraControls, 0);

				if (banner != null)
				{
					extraControls.Top = banner.Bottom;
					extraControls.MinimumSize = new Size(banner.Width, 0);
				}

				var shiftAmount = extraControls.Height;

				// Move all existing controls, except for the banner and the extra controls, into a container
				var container = new Panel
				{
					Name = ExistingControlsContainerName,
					Size = editForm.ClientSize,
					Location = Point.Empty,
					Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
				};

				var acceptButton = editForm.AcceptButton;
				var cancelButton = editForm.CancelButton;

				foreach (var control in editForm.Controls.Cast<Control>().ToArray())
				{
					if (control != banner &&
						control != extraControls)
					{
						container.Controls.Add(control);
					}
				}

				editForm.AcceptButton = acceptButton;
				editForm.CancelButton = cancelButton;

				if (editForm.FormBorderStyle == FormBorderStyle.Sizable)
				{
					ApplyKeeResizeHack(editForm, shiftAmount);
				}

				// Resize the form
				editForm.Height += shiftAmount;
				if (editForm.MinimumSize.Height != 0)
				{
					editForm.MinimumSize = new Size(editForm.MinimumSize.Width, editForm.MinimumSize.Height + shiftAmount);
				}
				

				// Then put the container panel back on the form, shifted downwards
				container.Top = shiftAmount;
				editForm.Controls.Add(container);
			}
			finally
			{
				editForm.ResumeLayout();
			}

			editForm.FormClosing += OnEditFormClosing;
		}

		private static void ApplyKeeResizeHack(EditAutoTypeItemForm editForm, int shiftAmount)
		{
			// KeeResize hack - KeeResize will automatically grow the items on the form when it resizes, which doesn't play nicely with having them moved from their original locations.
			// To fix this, intercept the resize event for KeeResize and lie about the size of the form.
			var eventsProperty = editForm.GetType().GetProperty("Events", BindingFlags.NonPublic | BindingFlags.Instance);
			if (eventsProperty != null)
			{
				var events = eventsProperty.GetValue(editForm, null) as EventHandlerList;

				var resizeEventKeyField = typeof(Control).GetField("EventResize", BindingFlags.NonPublic | BindingFlags.Static);
				if (resizeEventKeyField != null)
				{
					var resizeEventKey = resizeEventKeyField.GetValue(editForm);

					var resizeEvent = events[resizeEventKey];

					if (resizeEvent != null)
					{
						var keeResizeEventHandler = resizeEvent.GetInvocationList().FirstOrDefault(eventHandler => eventHandler.Method.DeclaringType.FullName == "KeeResizeLib.FormResizer");
						if (keeResizeEventHandler != null)
						{
							events.RemoveHandler(resizeEventKey, keeResizeEventHandler);

							var heightField = typeof(Control).GetField("height", BindingFlags.NonPublic | BindingFlags.Instance);

							if (heightField != null)
							{
								// Discover minimum height
								var controlInfoField = keeResizeEventHandler.Target.GetType().GetField("ControlInfo", BindingFlags.Public | BindingFlags.Instance);
								if (controlInfoField != null)
								{
									var controlInfo = controlInfoField.GetValue(keeResizeEventHandler.Target);
									if (controlInfo != null)
									{
										var orgHField = controlInfo.GetType().GetField("OrgH", BindingFlags.Public | BindingFlags.Instance);
										if (orgHField != null)
										{
											var orgH = orgHField.GetValue(controlInfo) as int?;
											if (orgH != null)
											{
												// Enforce this as a minimum size
												editForm.MinimumSize = new Size(Math.Max(editForm.MinimumSize.Height, orgH.Value), editForm.MinimumSize.Width);
											}
										}
									}
								}

								// Intercept all future resizes to lie about the height to KeeResize
								editForm.Resize += (o, e) =>
								{
									var realHeight = (int)heightField.GetValue(editForm);
									heightField.SetValue(editForm, realHeight - shiftAmount);

									try
									{
										keeResizeEventHandler.DynamicInvoke(o, e);
									}
									finally
									{
										heightField.SetValue(editForm, realHeight);
									}
								};
							}
						}
					}
				}
			}
		}

		private void OnEditFormClosing(object sender, FormClosingEventArgs e)
		{
			var editForm = sender as EditAutoTypeItemForm;

			if (editForm != null)
			{
				RemoveEditFormUICustomisations(editForm);
			}
		}

		private void RemoveEditFormUICustomisations(EditAutoTypeItemForm editForm)
		{
			var extraControls = editForm.Controls.Find(ExtraControlsName, false).FirstOrDefault();
			var container = editForm.Controls.Find(ExistingControlsContainerName, false).FirstOrDefault();
			if (extraControls == null || container == null)
			{
				Debug.Fail("Form not customised");
				return;
			}
			editForm.SuspendLayout();
			try
			{
				var acceptButton = editForm.AcceptButton;
				var cancelButton = editForm.CancelButton;

				var shiftAmount = extraControls.Height;
				editForm.Controls.Remove(container);
				editForm.Controls.Remove(extraControls);

				if (editForm.MinimumSize.Height != 0)
				{
					editForm.MinimumSize = new Size(editForm.MinimumSize.Width, editForm.MinimumSize.Height - shiftAmount);
				}
				editForm.Height -= shiftAmount;

				foreach (var control in container.Controls.Cast<Control>().ToArray())
				{
					editForm.Controls.Add(control);
				}
				
				container.Dispose();
				extraControls.Dispose();

				editForm.AcceptButton = acceptButton;
				editForm.CancelButton = cancelButton;
			}
			finally
			{
				editForm.ResumeLayout();
			}
		}
		#endregion

		/// <summary>
		///
		/// </summary>
		public override void Terminate()
		{
			GlobalWindowManager.WindowAdded -= GlobalWindowManager_WindowAdded;
			AutoType.SequenceQuery -= AutoType_SequenceQuery;
			AutoType.SequenceQueriesBegin -= AutoType_SequenceQueriesBegin;
			AutoType.SequenceQueriesEnd -= AutoType_SequenceQueriesEnd;

			// Edit form customisations will be removed automatically when the form is closed.

			if (mOptionsMenu != null)
			{
				m_host.MainWindow.ToolsMenu.DropDownItems.Remove(mOptionsMenu);

				mOptionsMenu = null;
			}

			if (mCreateEntryHotkeyId != 0)
			{
				var result = HotKeyManager.UnregisterHotKey(mCreateEntryHotkeyId);
				Debug.Assert(result);
				mCreateEntryHotkeyId = 0;
			}

			if (mChromeAccessibility != null)
			{
				mChromeAccessibility.Dispose();
				mChromeAccessibility = null;
			}
		}
	}
}