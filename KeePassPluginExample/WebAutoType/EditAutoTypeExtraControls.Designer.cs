namespace WebAutoType
{
	partial class EditAutoTypeExtraControls
	{
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
			this.mURLLabel.Location = new System.Drawing.Point(23, 29);
			this.mURLLabel.Name = "mURLLabel";
			this.mURLLabel.Size = new System.Drawing.Size(66, 13);
			this.mURLLabel.TabIndex = 2;
			this.mURLLabel.Text = "Target URL:";
			// 
			// mMatchURL
			// 
			this.mMatchURL.AutoSize = true;
			this.mMatchURL.Location = new System.Drawing.Point(12, 4);
			this.mMatchURL.Name = "mMatchURL";
			this.mMatchURL.Size = new System.Drawing.Size(300, 17);
			this.mMatchURL.TabIndex = 0;
			this.mMatchURL.Text = "Match against the URL of a page shown in a web browser";
			this.mMatchURL.UseVisualStyleBackColor = true;
			this.mMatchURL.CheckedChanged += new System.EventHandler(this.OnMatchOptionChanged);
			// 
			// mMatchTitle
			// 
			this.mMatchTitle.AutoSize = true;
			this.mMatchTitle.Checked = true;
			this.mMatchTitle.Location = new System.Drawing.Point(12, 90);
			this.mMatchTitle.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.mMatchTitle.Name = "mMatchTitle";
			this.mMatchTitle.Size = new System.Drawing.Size(189, 17);
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
			this.mURL.Location = new System.Drawing.Point(95, 26);
			this.mURL.Name = "mURL";
			this.mURL.Size = new System.Drawing.Size(367, 21);
			this.mURL.TabIndex = 1;
			this.mURL.DropDown += new System.EventHandler(this.mURL_DropDown);
			this.mURL.TextChanged += new System.EventHandler(this.mURL_TextChanged);
			// 
			// mURLHint
			// 
			this.mURLHint.AutoSize = true;
			this.mURLHint.Enabled = false;
			this.mURLHint.Location = new System.Drawing.Point(92, 53);
			this.mURLHint.Name = "mURLHint";
			this.mURLHint.Size = new System.Drawing.Size(337, 13);
			this.mURLHint.TabIndex = 6;
			this.mURLHint.Text = "Click the drop-down button on the right to see currently opened URLs.";
			// 
			// mURLWildcardLink
			// 
			this.mURLWildcardLink.AutoSize = true;
			this.mURLWildcardLink.Enabled = false;
			this.mURLWildcardLink.Location = new System.Drawing.Point(92, 70);
			this.mURLWildcardLink.Name = "mURLWildcardLink";
			this.mURLWildcardLink.Size = new System.Drawing.Size(270, 13);
			this.mURLWildcardLink.TabIndex = 2;
			this.mURLWildcardLink.TabStop = true;
			this.mURLWildcardLink.Text = "Simple wildcards and regular expressions are supported.";
			this.mURLWildcardLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.mURLWildcardLink_LinkClicked);
			// 
			// EditAutoTypeExtraControls
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.Controls.Add(this.mURLWildcardLink);
			this.Controls.Add(this.mURLHint);
			this.Controls.Add(this.mURL);
			this.Controls.Add(this.mMatchTitle);
			this.Controls.Add(this.mMatchURL);
			this.Controls.Add(this.mURLLabel);
			this.Name = "EditAutoTypeExtraControls";
			this.Size = new System.Drawing.Size(475, 107);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label mURLLabel;
		private System.Windows.Forms.RadioButton mMatchURL;
		private System.Windows.Forms.RadioButton mMatchTitle;
		private System.Windows.Forms.ComboBox mURL;
		private System.Windows.Forms.Label mURLHint;
		private System.Windows.Forms.LinkLabel mURLWildcardLink;
	}
}
