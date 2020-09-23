using KeePass.UI;

namespace WebAutoType
{
	partial class Options
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Label hotkeyLabel;
			this.mCreateEntryGroupBox = new System.Windows.Forms.GroupBox();
			this.mTargetGroup = new System.Windows.Forms.ComboBox();
			this.mTargetGroupLabel = new System.Windows.Forms.Label();
			this.mCreateEntryShortcutKey = new KeePass.UI.HotKeyControlEx();
			this.mMatchURLField = new System.Windows.Forms.CheckBox();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.mAutoSkipUsername = new System.Windows.Forms.CheckBox();
			this.mShowRepeatedSearch = new System.Windows.Forms.CheckBox();
			hotkeyLabel = new System.Windows.Forms.Label();
			this.mCreateEntryGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// hotkeyLabel
			// 
			hotkeyLabel.AutoSize = true;
			hotkeyLabel.Location = new System.Drawing.Point(7, 25);
			hotkeyLabel.Name = "hotkeyLabel";
			hotkeyLabel.Size = new System.Drawing.Size(78, 13);
			hotkeyLabel.TabIndex = 3;
			hotkeyLabel.Text = "Global hot key:";
			// 
			// mCreateEntryGroupBox
			// 
			this.mCreateEntryGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mCreateEntryGroupBox.Controls.Add(this.mTargetGroup);
			this.mCreateEntryGroupBox.Controls.Add(this.mTargetGroupLabel);
			this.mCreateEntryGroupBox.Controls.Add(hotkeyLabel);
			this.mCreateEntryGroupBox.Controls.Add(this.mCreateEntryShortcutKey);
			this.mCreateEntryGroupBox.Location = new System.Drawing.Point(12, 81);
			this.mCreateEntryGroupBox.Name = "mCreateEntryGroupBox";
			this.mCreateEntryGroupBox.Size = new System.Drawing.Size(228, 105);
			this.mCreateEntryGroupBox.TabIndex = 3;
			this.mCreateEntryGroupBox.TabStop = false;
			this.mCreateEntryGroupBox.Text = "Create Entry from Web Page";
			// 
			// mTargetGroup
			// 
			this.mTargetGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mTargetGroup.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.mTargetGroup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.mTargetGroup.FormattingEnabled = true;
			this.mTargetGroup.Location = new System.Drawing.Point(10, 71);
			this.mTargetGroup.Name = "mTargetGroup";
			this.mTargetGroup.Size = new System.Drawing.Size(207, 21);
			this.mTargetGroup.TabIndex = 5;
			this.mTargetGroup.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.mTargetGroup_DrawItem);
			// 
			// mTargetGroupLabel
			// 
			this.mTargetGroupLabel.AutoSize = true;
			this.mTargetGroupLabel.Location = new System.Drawing.Point(7, 54);
			this.mTargetGroupLabel.Name = "mTargetGroupLabel";
			this.mTargetGroupLabel.Size = new System.Drawing.Size(82, 13);
			this.mTargetGroupLabel.TabIndex = 4;
			this.mTargetGroupLabel.Text = "Create in group:";
			// 
			// mCreateEntryShortcutKey
			// 
			this.mCreateEntryShortcutKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mCreateEntryShortcutKey.Location = new System.Drawing.Point(91, 22);
			this.mCreateEntryShortcutKey.Name = "mCreateEntryShortcutKey";
			this.mCreateEntryShortcutKey.Size = new System.Drawing.Size(126, 20);
			this.mCreateEntryShortcutKey.TabIndex = 2;
			// 
			// mMatchURLField
			// 
			this.mMatchURLField.AutoSize = true;
			this.mMatchURLField.Location = new System.Drawing.Point(12, 12);
			this.mMatchURLField.Name = "mMatchURLField";
			this.mMatchURLField.Size = new System.Drawing.Size(200, 17);
			this.mMatchURLField.TabIndex = 0;
			this.mMatchURLField.Text = "&Use the URL field value for matching";
			this.mMatchURLField.UseVisualStyleBackColor = true;
			// 
			// m_btnCancel
			// 
			this.m_btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Location = new System.Drawing.Point(165, 195);
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.Size = new System.Drawing.Size(75, 23);
			this.m_btnCancel.TabIndex = 5;
			this.m_btnCancel.Text = "&Cancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			// 
			// m_btnOK
			// 
			this.m_btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Location = new System.Drawing.Point(84, 195);
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.Size = new System.Drawing.Size(75, 23);
			this.m_btnOK.TabIndex = 4;
			this.m_btnOK.Text = "&OK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			// 
			// mAutoSkipUsername
			// 
			this.mAutoSkipUsername.AutoSize = true;
			this.mAutoSkipUsername.Location = new System.Drawing.Point(12, 35);
			this.mAutoSkipUsername.Name = "mAutoSkipUsername";
			this.mAutoSkipUsername.Size = new System.Drawing.Size(230, 17);
			this.mAutoSkipUsername.TabIndex = 6;
			this.mAutoSkipUsername.Text = "&Automatically skip user name for passwords";
			this.mAutoSkipUsername.UseVisualStyleBackColor = true;
			// 
			// mShowRepeatedSearch
			// 
			this.mShowRepeatedSearch.AutoSize = true;
			this.mShowRepeatedSearch.Location = new System.Drawing.Point(12, 58);
			this.mShowRepeatedSearch.Name = "mShowRepeatedSearch";
			this.mShowRepeatedSearch.Size = new System.Drawing.Size(192, 17);
			this.mShowRepeatedSearch.TabIndex = 7;
			this.mShowRepeatedSearch.Text = "&Show search for repeated autotype";
			this.mShowRepeatedSearch.UseVisualStyleBackColor = true;
			// 
			// Options
			// 
			this.AcceptButton = this.m_btnOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ClientSize = new System.Drawing.Size(252, 229);
			this.Controls.Add(this.mShowRepeatedSearch);
			this.Controls.Add(this.mAutoSkipUsername);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.mCreateEntryGroupBox);
			this.Controls.Add(this.mMatchURLField);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Options";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "WebAutoType Options";
			this.mCreateEntryGroupBox.ResumeLayout(false);
			this.mCreateEntryGroupBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox mMatchURLField;
		private KeePass.UI.HotKeyControlEx mCreateEntryShortcutKey;
		private System.Windows.Forms.ComboBox mTargetGroup;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.GroupBox mCreateEntryGroupBox;
		private System.Windows.Forms.Label mTargetGroupLabel;
		private System.Windows.Forms.CheckBox mAutoSkipUsername;
		private System.Windows.Forms.CheckBox mShowRepeatedSearch;
	}
}