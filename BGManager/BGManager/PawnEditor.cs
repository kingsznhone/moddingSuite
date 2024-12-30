using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BGManager;

public class PawnEditor : Form
{
	public string OriginalPawn = "";

	public string NewPawn = "";

	private IContainer components = null;

	private Label label1;

	private Label label2;

	private Button _executeButton;

	private ListBox _localListBox;

	private ListBox _globalListBox;

	private TextBox _localPawnsFilterTextBox;

	private TextBox _globalPawnsFilterTextBox;

	public HashSet<string> GlobalSet { get; protected set; }

	public HashSet<string> CampaignSet { get; protected set; }

	public PawnEditor(HashSet<string> campaignSet, string selectedPawn, HashSet<string> globalSet)
	{
		InitializeComponent();
		GlobalSet = globalSet;
		CampaignSet = campaignSet;
		foreach (string s in campaignSet)
		{
			_localListBox.Items.Add(s);
		}
		_localListBox.SelectedItem = selectedPawn;
		foreach (string s in globalSet)
		{
			if (!campaignSet.Contains(s))
			{
				_globalListBox.Items.Add(s);
			}
		}
		_localPawnsFilterTextBox.Text = selectedPawn;
	}

	private void _executeButton_Click(object sender, EventArgs e)
	{
		OriginalPawn = _localListBox.Text;
		NewPawn = _globalListBox.Text;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private void _globalPawnsFilterTextBox_TextChanged(object sender, EventArgs e)
	{
		_globalListBox.Items.Clear();
		foreach (string s in GlobalSet)
		{
			if (s.ToUpper().IndexOf(_globalPawnsFilterTextBox.Text.ToUpper()) >= 0 && !CampaignSet.Contains(s))
			{
				_globalListBox.Items.Add(s);
			}
		}
	}

	private void _globalListBox_MouseDoubleClick(object sender, MouseEventArgs e)
	{
		OriginalPawn = _localListBox.Text;
		NewPawn = _globalListBox.Text;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && components != null)
		{
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		this.label1 = new System.Windows.Forms.Label();
		this.label2 = new System.Windows.Forms.Label();
		this._executeButton = new System.Windows.Forms.Button();
		this._localListBox = new System.Windows.Forms.ListBox();
		this._globalListBox = new System.Windows.Forms.ListBox();
		this._localPawnsFilterTextBox = new System.Windows.Forms.TextBox();
		this._globalPawnsFilterTextBox = new System.Windows.Forms.TextBox();
		base.SuspendLayout();
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(13, 19);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(558, 13);
		this.label1.TabIndex = 2;
		this.label1.Text = "Change Pawn in left combo box from selected campaign with value from right combo box from global pawn models list";
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(259, 76);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(55, 13);
		this.label2.TabIndex = 3;
		this.label2.Text = "=====>>>";
		this._executeButton.Location = new System.Drawing.Point(332, 333);
		this._executeButton.Name = "_executeButton";
		this._executeButton.Size = new System.Drawing.Size(239, 23);
		this._executeButton.TabIndex = 4;
		this._executeButton.Text = "Change Pawn in Campaign";
		this._executeButton.UseVisualStyleBackColor = true;
		this._executeButton.Click += new System.EventHandler(_executeButton_Click);
		this._localListBox.FormattingEnabled = true;
		this._localListBox.Location = new System.Drawing.Point(16, 73);
		this._localListBox.Name = "_localListBox";
		this._localListBox.Size = new System.Drawing.Size(237, 251);
		this._localListBox.TabIndex = 5;
		this._globalListBox.FormattingEnabled = true;
		this._globalListBox.Location = new System.Drawing.Point(332, 76);
		this._globalListBox.Name = "_globalListBox";
		this._globalListBox.Size = new System.Drawing.Size(239, 251);
		this._globalListBox.TabIndex = 6;
		this._globalListBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(_globalListBox_MouseDoubleClick);
		this._localPawnsFilterTextBox.Location = new System.Drawing.Point(16, 44);
		this._localPawnsFilterTextBox.Name = "_localPawnsFilterTextBox";
		this._localPawnsFilterTextBox.ReadOnly = true;
		this._localPawnsFilterTextBox.Size = new System.Drawing.Size(237, 20);
		this._localPawnsFilterTextBox.TabIndex = 7;
		this._globalPawnsFilterTextBox.Location = new System.Drawing.Point(332, 46);
		this._globalPawnsFilterTextBox.Name = "_globalPawnsFilterTextBox";
		this._globalPawnsFilterTextBox.Size = new System.Drawing.Size(239, 20);
		this._globalPawnsFilterTextBox.TabIndex = 8;
		this._globalPawnsFilterTextBox.TextChanged += new System.EventHandler(_globalPawnsFilterTextBox_TextChanged);
		base.AcceptButton = this._executeButton;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(581, 364);
		base.Controls.Add(this._globalPawnsFilterTextBox);
		base.Controls.Add(this._localPawnsFilterTextBox);
		base.Controls.Add(this._globalListBox);
		base.Controls.Add(this._localListBox);
		base.Controls.Add(this._executeButton);
		base.Controls.Add(this.label2);
		base.Controls.Add(this.label1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
		base.Name = "PawnEditor";
		this.Text = "PawnEditor";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
