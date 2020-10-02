using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
	/// 
	/// </summary>
	public sealed class DisambiguatorExt : Plugin
	{
		private IPluginHost _keePassHost;

		internal const string UrlAutoTypeWindowTitlePrefix = "??:URL:";
		private const string OptionsConfigRoot = "Disambiguator.";
		private const string ExtraControlsName = "DisambiguatorControls";
		private const string ExistingControlsContainerName = "DisambiguatorOriginalControls";

		private Dictionary<int, string> mUrlForAutoTypeEvent = new Dictionary<int, string>();
		private ToolStripMenuItem _optionsMenu;
		private int _createEntryHotkeyId;

		private readonly HashSet<int> _foundSequence = new HashSet<int>();
		private FieldInfo _searchTextBoxField;

		public override string UpdateUrl
		{
			get { return "sourceforge-version://Disambiguator/appmatch?-v(%5B%5Cd.%5D%2B)%5C.zip"; }
		}


		public override bool Initialize(IPluginHost host)
		{
			Debug.Assert(host != null);
			if (host == null) return false;

			_keePassHost = host;

			GlobalWindowManager.WindowAdded += GlobalWindowManager_WindowAdded;
			AutoType.SequenceQuery += AutoType_SequenceQuery;
			AutoType.SequenceQueriesBegin += AutoType_SequenceQueriesBegin;
			AutoType.SequenceQueriesEnd += AutoType_SequenceQueriesEnd;

			_searchTextBoxField = typeof(SearchForm).GetField("m_tbSearch", BindingFlags.Instance | BindingFlags.NonPublic);

			return true; // Initialization successful
		}


		#region Options
		private bool MatchUrlField
		{
			get { return _keePassHost.CustomConfig.GetBool(OptionsConfigRoot + "MatchUrlField", true); }
			set { _keePassHost.CustomConfig.SetBool(OptionsConfigRoot + "MatchUrlField", value); }
		}
		#endregion


		/// <summary>
		/// Not used at this point, because this plugin just adds additional
		/// filtering based on Window Class and hosting app
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AutoType_SequenceQueriesBegin(object sender, SequenceQueriesEventArgs e)
		{
		}


		/// <summary>
		/// Not used at this point, because this plugin just adds additional
		/// filtering based on Window Class and hosting app
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AutoType_SequenceQueriesEnd(object sender, SequenceQueriesEventArgs e)
		{
		}


		private void AutoType_SequenceQuery(object sender, SequenceQueryEventArgs e)
		{
			//main win title and Autotype sequence for this entry
			//we have to check this separately from the custom associations
			var targetWindowTitle = e.Entry.Strings.ReadSafe("Title");
			string entryAutoTypeSequence = e.Entry.GetAutoTypeSequence();

			ResolveSequence(targetWindowTitle, entryAutoTypeSequence, e);

			//run through the target window associations looking for match elements
			foreach (AutoTypeAssociation association in e.Entry.AutoType.Associations)
			{
				//get the window name (this would usually contain the TITLE of the window
				//that would match
				var winName = association.WindowName;
				ResolveSequence(winName, association.Sequence, e);
			}
		}


		public void ResolveSequence(string winName, string sequence, SequenceQueryEventArgs e)
		{
			var exePath = getExecutableFromHwnd(e.TargetWindowHandle).ToLower();

			//get the window name (this would usually contain the TITLE of the window
			//that would match
			if (winName == null) return;

			var match = false;
			if (!match)
			{
				//next remove any app out of the window name
				//and try it

				//first compile the window name to replace all KeePass elements
				winName = SprEngine.Compile(winName, new SprContext(e.Entry, e.Database, SprCompileFlags.All));

				var exeParam = getParam(ref winName, "exe");
				if (!string.IsNullOrEmpty(exeParam))
				{
					if (exeParam.Contains(@"\"))
					{
						//param looks like it's got a path element, so compare to the whole exename
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
			}

			//and lastly, the winName must match as well to be considered
			var title = e.TargetWindowTitle ?? string.Empty;
			match = match && IsAMatch(title, winName);

			if (match)
			{
				e.AddSequence(string.IsNullOrEmpty(sequence) ? e.Entry.GetAutoTypeSequence() : sequence);
			}
		}


		/// <summary>
		/// Parse a {} delimited named param from a string
		/// Only honor the first instance
		/// </summary>
		/// <param name="value"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		public string getParam(ref string value, string key)
		{
			string param = string.Empty;
			var rx = new Regex(string.Format("\\{{{0}:(?<paramValue>.*)\\}}", key), RegexOptions.IgnoreCase);
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


		public bool IsAMatch(string value, string matchPattern)
		{
			//check if the targetname is actually a regex
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
		private static extern bool QueryFullProcessImageName([In]UIntPtr hProcess, [In]int dwFlags, [Out]StringBuilder lpExeName, out int lpdwSize);
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


		#region Edit AutoType Window Customisation
		private void GlobalWindowManager_WindowAdded(object p_sender, GwmWindowEventArgs p_e)
		{
			//var editAutoTypeItemForm = p_e.Form as EditAutoTypeItemForm;
			//if (editAutoTypeItemForm != null)
			//{
			//	CustomizeEditFormUI(editAutoTypeItemForm);
			//}
		}

		private void CustomizeEditFormUI(EditAutoTypeItemForm editForm)
		{
			if (editForm.Controls.Find(ExtraControlsName, false).Any())
			{
				Debug.Fail("Form already customized");
				return;
			}

			var editSequenceOnlyField = typeof(EditAutoTypeItemForm).GetField("m_bEditSequenceOnly", BindingFlags.Instance | BindingFlags.NonPublic);
			if (editSequenceOnlyField != null)
			{
				if (editSequenceOnlyField.GetValue(editForm) as bool? == true)
				{
					// This is a sequence-only edit window, so don't customize it.
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
				Debug.Fail("Form not customized");
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
		/// called when KeePass is closing
		/// </summary>
		public override void Terminate()
		{
			GlobalWindowManager.WindowAdded -= GlobalWindowManager_WindowAdded;
			AutoType.SequenceQuery -= AutoType_SequenceQuery;
			AutoType.SequenceQueriesBegin -= AutoType_SequenceQueriesBegin;
			AutoType.SequenceQueriesEnd -= AutoType_SequenceQueriesEnd;

			// Edit form customizations will be removed automatically when the form is closed.

			if (_optionsMenu != null)
			{
				_keePassHost.MainWindow.ToolsMenu.DropDownItems.Remove(_optionsMenu);

				_optionsMenu = null;
			}

			if (_createEntryHotkeyId != 0)
			{
				var result = HotKeyManager.UnregisterHotKey(_createEntryHotkeyId);
				Debug.Assert(result);
				_createEntryHotkeyId = 0;
			}
		}
	}
}