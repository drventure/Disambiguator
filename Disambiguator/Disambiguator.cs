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

using Accessibility;
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

			AutoType.SequenceQuery += AutoType_SequenceQuery;
			AutoType.SequenceQueriesBegin += AutoType_SequenceQueriesBegin;
			AutoType.SequenceQueriesEnd += AutoType_SequenceQueriesEnd;

			return true; // Initialization successful
		}


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
			string exeParam = string.Empty;
			string ctlParam = string.Empty;

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

			    exeParam = getParam(ref winName, "exe");
				ctlParam = getParam(ref winName, "ctl");
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

			if (!match)
            {
				//no match yet, check for any child controls

				//attempt to retrieve an accessible object from the target window handle
				var accObject = Accessible.ObjectFromWindow(e.TargetWindowHandle);

				//and scan through it's child objects
				match = RecurseChildObjects(accObject, ctlParam);
			}

			//and lastly, the winName must match as well to be considered
			var title = e.TargetWindowTitle ?? string.Empty;
			match = match && IsAMatch(title, winName);

			if (match)
			{
				e.AddSequence(string.IsNullOrEmpty(sequence) ? e.Entry.GetAutoTypeSequence() : sequence);
			}
		}


		private bool RecurseChildObjects(IAccessible parent, string ctlParam)
        {
			if (parent != null)
			{
				foreach (var child in parent.Children())
				{
					if (IsAMatch(child.SafeGetName(), ctlParam))
					{
						return true;
					}
				}
			}
			return false;
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


		/// <summary>
		/// called when KeePass is closing
		/// </summary>
		public override void Terminate()
		{
			AutoType.SequenceQuery -= AutoType_SequenceQuery;
			AutoType.SequenceQueriesBegin -= AutoType_SequenceQueriesBegin;
			AutoType.SequenceQueriesEnd -= AutoType_SequenceQueriesEnd;
		}
	}
}