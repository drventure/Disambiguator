using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using KeePass.UI;
using KeePass.Forms;
using KeePass.App;
using System.Reflection;
using System.Threading;

namespace WebAutoType
{
	public partial class EditAutoTypeExtraControls : UserControl
	{
		private const int SeparatorIndent = 6;
		private EditAutoTypeItemForm mParent;
		private ImageComboBoxEx m_cmbWindow;
		private ImageComboBoxEx mWindowTitleComboImposter;
		private Label mSeparator;
		private List<Control> mWindowTitleControls;

		public EditAutoTypeExtraControls()
		{
			InitializeComponent();
		}

		public EditAutoTypeExtraControls(EditAutoTypeItemForm parent)
			: this()
		{
			mParent = parent;

			mWindowTitleControls = (from controlName in new[] {	"m_lblOpenHint",
																"m_lblOpenHint",
																"m_lblTargetWindow",
																"m_lnkWildcardRegexHint" }
													let control = FindControl(controlName)
													where control != null
													select control).ToList();


			m_cmbWindow = FindControl("m_cmbWindow") as ImageComboBoxEx;
			if (m_cmbWindow == null)
			{
				System.Diagnostics.Debug.Fail("Could not find combo window");
				m_cmbWindow = new ImageComboBoxEx(); // Create null stub to avoid crashing out
			}

			m_cmbWindow.TextChanged += m_cmbWindow_TextChanged;

			// Create an Imposter combo box over it
			mWindowTitleComboImposter = new ImageComboBoxEx
			{
				IntegralHeight = false,
				Name = "mWindowTitleComboImposter",
				TabIndex = m_cmbWindow.TabIndex,
			};

			mWindowTitleComboImposter.TextChanged += mWindowTitleComboImposter_TextChanged;
			mWindowTitleComboImposter.DropDown += mWindowTitleComboImposter_DropDown;

			// Track changes to size and position and items
			m_cmbWindow.LocationChanged += SyncComboImposter;
			m_cmbWindow.SizeChanged += SyncComboImposter;

			// Ignore any direct changes itself
			mWindowTitleComboImposter.LocationChanged += SyncComboImposter;

			// Take initial values
			SyncComboImposter(null, EventArgs.Empty);

			// Replace the combo with its imposter
			m_cmbWindow.Parent.Controls.Add(mWindowTitleComboImposter);
			m_cmbWindow.Visible = false;
			
			mWindowTitleControls.Add(mWindowTitleComboImposter);

			// Add a separator between the two groups of radio buttons
			mSeparator = new Label
			{
				BorderStyle = BorderStyle.Fixed3D,
				Location = new Point(SeparatorIndent, mWindowTitleControls.Max(c => c.Bottom) + 7),
				Size = new Size(mParent.ClientSize.Width - SeparatorIndent * 2, 2),
				Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
				TabStop = false,
			};

			m_cmbWindow.Parent.Controls.Add(mSeparator);
			
		}

		private Control FindControl(string name)
		{
			return mParent.Controls.Find(name, false).FirstOrDefault();
		}

		#region Disposal
		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Unhook any hooked events from parent controls
				if (m_cmbWindow != null)
				{
					m_cmbWindow.TextChanged -= m_cmbWindow_TextChanged;
					m_cmbWindow.LocationChanged -= SyncComboImposter;
					m_cmbWindow.SizeChanged -= SyncComboImposter;
				}

				if (mWindowTitleComboImposter != null)
				{
					mWindowTitleComboImposter.Dispose();
				}

				if (mSeparator != null)
				{
					mSeparator.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		#endregion

		private void SyncComboImposter(object sender, EventArgs e)
		{
			// These setters automatically check for equality and don't re-apply equal values
			mWindowTitleComboImposter.SetBounds(m_cmbWindow.Left, m_cmbWindow.Top, m_cmbWindow.Width, m_cmbWindow.Height); 
		}

		private void mWindowTitleComboImposter_TextChanged(object sender, EventArgs e)
		{
			if (!MatchURL)
			{
				m_cmbWindow.Text = mWindowTitleComboImposter.Text;
			}
		}

		private void mWindowTitleComboImposter_DropDown(object sender, EventArgs e)
		{
			// Sync available items
			mWindowTitleComboImposter.Items.Clear();
			mWindowTitleComboImposter.OrderedImageList = m_cmbWindow.OrderedImageList;
			mWindowTitleComboImposter.Items.AddRange(m_cmbWindow.Items.Cast<Object>().ToArray());
		}

		private void mURL_TextChanged(object sender, EventArgs e)
		{
			if (MatchURL)
			{
				m_cmbWindow.Text = WebAutoTypeExt.UrlAutoTypeWindowTitlePrefix + mURL.Text;
			}
		}

		private void m_cmbWindow_TextChanged(object sender, EventArgs e)
		{
			if (m_cmbWindow.Text.StartsWith(WebAutoTypeExt.UrlAutoTypeWindowTitlePrefix))
			{
				MatchURL = true;

				mURL.Text = m_cmbWindow.Text.Substring(WebAutoTypeExt.UrlAutoTypeWindowTitlePrefix.Length);
				mWindowTitleComboImposter.Text = String.Empty;
			}
			else
			{
				MatchURL = false;

				mWindowTitleComboImposter.Text = m_cmbWindow.Text;
				mURL.Text = String.Empty;
			}
		}

		private void mURLWildcardLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			AppHelp.ShowHelp(AppDefs.HelpTopics.AutoType, AppDefs.HelpTopics.AutoTypeWindowFilters);
		}

		private bool MatchURL
		{
			get 
			{
				return mMatchURL.Checked;
			}
			set
			{
				mMatchURL.Checked = value;
			}
		}

		private void OnMatchOptionChanged(object sender, EventArgs e)
		{
			var urlControlsEnabled = MatchURL;
			var titleControlsEnabled = !urlControlsEnabled;
			
			mURL.Enabled = urlControlsEnabled;
			mURLLabel.Enabled = urlControlsEnabled;
			mURLHint.Enabled = urlControlsEnabled;
			mURLWildcardLink.Enabled = urlControlsEnabled;

			foreach (var control in mWindowTitleControls)
			{
				control.Enabled = titleControlsEnabled;
			}
			
			if (!urlControlsEnabled)
			{
				mURL.Text = String.Empty;
			}
			
			if (!titleControlsEnabled)
			{
				mWindowTitleComboImposter.Text = String.Empty;
			}
		}

		private void mURL_DropDown(object sender, EventArgs e)
		{
			// Get all the currently open browser window URLs
			mURL.Items.Clear();

			ThreadPool.QueueUserWorkItem(PopulateUrlDropDown);
			//mURL.Items.AddRange((from browserWindow in WebBrowserUrl.GetTopLevelBrowserWindows() select WebBrowserUrl.GetBrowserUrl(browserWindow)).ToArray());
		}

		private void PopulateUrlDropDown(object state)
		{
			foreach (var browserWindowUrl in WebBrowserUrl.GetTopLevelBrowserWindowUrls())
			{
				mURL.BeginInvoke(new Action(() => mURL.Items.Add(browserWindowUrl)));
			}
		}

	}
}
