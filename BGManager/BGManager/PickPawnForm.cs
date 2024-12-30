using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BGManager;

public class PickPawnForm : Form
{
	private IContainer components = null;

	private TextBox _filterTextBox;

	private Label label1;

	private ListBox _pawnListBox;

	public string Pawn { get; protected set; }

	public HashSet<string> GlobalSet { get; protected set; }

	public PickPawnForm(HashSet<string> campaignSet, string selectedPawn)
	{
		InitializeComponent();
		GlobalSet = campaignSet;
		foreach (string s in campaignSet)
		{
			_pawnListBox.Items.Add(s);
		}
		_pawnListBox.SelectedItem = selectedPawn;
		Pawn = selectedPawn;
		Text = Pawn;
	}

	private void _filterTextBox_TextChanged(object sender, EventArgs e)
	{
		_pawnListBox.Items.Clear();
		foreach (string s in GlobalSet)
		{
			if (s.ToUpper().IndexOf(_filterTextBox.Text.ToUpper()) >= 0)
			{
				_pawnListBox.Items.Add(s);
			}
		}
	}

	private void _pawnListBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		Pawn = _pawnListBox.SelectedItem.ToString();
	}

	private void _pawnListBox_DoubleClick(object sender, EventArgs e)
	{
		Pawn = _pawnListBox.SelectedItem.ToString();
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
		this._filterTextBox = new System.Windows.Forms.TextBox();
		this.label1 = new System.Windows.Forms.Label();
		this._pawnListBox = new System.Windows.Forms.ListBox();
		base.SuspendLayout();
		this._filterTextBox.Location = new System.Drawing.Point(46, 9);
		this._filterTextBox.Name = "_filterTextBox";
		this._filterTextBox.Size = new System.Drawing.Size(131, 20);
		this._filterTextBox.TabIndex = 0;
		this._filterTextBox.TextChanged += new System.EventHandler(_filterTextBox_TextChanged);
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(1, 12);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(44, 13);
		this.label1.TabIndex = 1;
		this.label1.Text = "Filter list";
		this._pawnListBox.FormattingEnabled = true;
		this._pawnListBox.Location = new System.Drawing.Point(4, 35);
		this._pawnListBox.Name = "_pawnListBox";
		this._pawnListBox.Size = new System.Drawing.Size(173, 225);
		this._pawnListBox.TabIndex = 2;
		this._pawnListBox.SelectedIndexChanged += new System.EventHandler(_pawnListBox_SelectedIndexChanged);
		this._pawnListBox.DoubleClick += new System.EventHandler(_pawnListBox_DoubleClick);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(178, 259);
		base.Controls.Add(this._pawnListBox);
		base.Controls.Add(this.label1);
		base.Controls.Add(this._filterTextBox);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "PickPawnForm";
		base.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
