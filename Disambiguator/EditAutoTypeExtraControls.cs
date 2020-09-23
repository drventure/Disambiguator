using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KeePass.UI;
using KeePass.Forms;
using KeePass.App;


namespace Disambiguator
{
	public class EditAutoTypeExtraControls : UserControl
	{
		private System.Windows.Forms.Label mURLLabel;
		private System.Windows.Forms.RadioButton mMatchURL;
		private System.Windows.Forms.RadioButton mMatchTitle;
		private System.Windows.Forms.ComboBox mURL;
		private System.Windows.Forms.Label mURLHint;
		private System.Windows.Forms.LinkLabel mURLWildcardLink;


		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.mURLLabel = new System.Windows.Forms.Label();
            this.mMatchURL = new System.Windows.Forms.RadioButton();
            this.mMatchTitle = new System.Windows.Forms.RadioButton();
            this.mURL = new System.Windows.Forms.ComboBox();
            this.mURLHint = new System.Windows.Forms.Label();
            this.mURLWildcardLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // mURLLabel
            // 
            this.mURLLabel.AutoSize = true;
            this.mURLLabel.Enabled = false;
            this.mURLLabel.Location = new System.Drawing.Point(46, 56);
            this.mURLLabel.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.mURLLabel.Name = "mURLLabel";
            this.mURLLabel.Size = new System.Drawing.Size(128, 25);
            this.mURLLabel.TabIndex = 2;
            this.mURLLabel.Text = "Target URL:";
            // 
            // mMatchURL
            // 
            this.mMatchURL.AutoSize = true;
            this.mMatchURL.Location = new System.Drawing.Point(24, 8);
            this.mMatchURL.Margin = new System.Windows.Forms.Padding(6);
            this.mMatchURL.Name = "mMatchURL";
            this.mMatchURL.Size = new System.Drawing.Size(594, 29);
            this.mMatchURL.TabIndex = 0;
            this.mMatchURL.Text = "Match against the URL of a page shown in a web browser";
            this.mMatchURL.UseVisualStyleBackColor = true;
            this.mMatchURL.CheckedChanged += new System.EventHandler(this.OnMatchOptionChanged);
            // 
            // mMatchTitle
            // 
            this.mMatchTitle.AutoSize = true;
            this.mMatchTitle.Checked = true;
            this.mMatchTitle.Location = new System.Drawing.Point(24, 173);
            this.mMatchTitle.Margin = new System.Windows.Forms.Padding(6, 6, 6, 0);
            this.mMatchTitle.Name = "mMatchTitle";
            this.mMatchTitle.Size = new System.Drawing.Size(373, 29);
            this.mMatchTitle.TabIndex = 3;
            this.mMatchTitle.TabStop = true;
            this.mMatchTitle.Text = "Match against the title of a window";
            this.mMatchTitle.UseVisualStyleBackColor = true;
            this.mMatchTitle.CheckedChanged += new System.EventHandler(this.OnMatchOptionChanged);
            // 
            // mURL
            // 
            this.mURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mURL.Enabled = false;
            this.mURL.FormattingEnabled = true;
            this.mURL.Location = new System.Drawing.Point(190, 50);
            this.mURL.Margin = new System.Windows.Forms.Padding(6);
            this.mURL.Name = "mURL";
            this.mURL.Size = new System.Drawing.Size(15914, 33);
            this.mURL.TabIndex = 1;
            this.mURL.DropDown += new System.EventHandler(this.mURL_DropDown);
            this.mURL.TextChanged += new System.EventHandler(this.mURL_TextChanged);
            // 
            // mURLHint
            // 
            this.mURLHint.AutoSize = true;
            this.mURLHint.Enabled = false;
            this.mURLHint.Location = new System.Drawing.Point(184, 102);
            this.mURLHint.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.mURLHint.Name = "mURLHint";
            this.mURLHint.Size = new System.Drawing.Size(679, 25);
            this.mURLHint.TabIndex = 6;
            this.mURLHint.Text = "Click the drop-down button on the right to see currently opened URLs.";
            // 
            // mURLWildcardLink
            // 
            this.mURLWildcardLink.AutoSize = true;
            this.mURLWildcardLink.Enabled = false;
            this.mURLWildcardLink.Location = new System.Drawing.Point(184, 135);
            this.mURLWildcardLink.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.mURLWildcardLink.Name = "mURLWildcardLink";
            this.mURLWildcardLink.Size = new System.Drawing.Size(555, 25);
            this.mURLWildcardLink.TabIndex = 2;
            this.mURLWildcardLink.TabStop = true;
            this.mURLWildcardLink.Text = "Simple wildcards and regular expressions are supported.";
            this.mURLWildcardLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.mURLWildcardLink_LinkClicked);
            // 
            // EditAutoTypeExtraControls
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.mURLWildcardLink);
            this.Controls.Add(this.mURLHint);
            this.Controls.Add(this.mURL);
            this.Controls.Add(this.mMatchTitle);
            this.Controls.Add(this.mMatchURL);
            this.Controls.Add(this.mURLLabel);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "EditAutoTypeExtraControls";
            this.Size = new System.Drawing.Size(15958, 202);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

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
				m_cmbWindow.Text = DisambiguatorExt.UrlAutoTypeWindowTitlePrefix + mURL.Text;
			}
		}


		private void m_cmbWindow_TextChanged(object sender, EventArgs e)
		{
			if (m_cmbWindow.Text.StartsWith(DisambiguatorExt.UrlAutoTypeWindowTitlePrefix))
			{
				MatchURL = true;

				mURL.Text = m_cmbWindow.Text.Substring(DisambiguatorExt.UrlAutoTypeWindowTitlePrefix.Length);
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
		}
	}
}
