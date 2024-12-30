using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using moddingSuite.Model.Ndfbin;

namespace BGManager;

public class ObjectivesForm : Form
{
	private List<string> unavailBG = new List<string>();

	public List<string> CheckedBG = new List<string>();

	public List<string> CheckedZones = new List<string>();

	private IContainer components = null;

	private Label bgLabel;

	private CheckedListBox _bgCheckedListBox;

	private Button buttonSave;

	private Button buttonCancel;

	private CheckedListBox _zonesCheckedListBox;

	private Label zonesLabel;

	public BG BattleGroup { get; protected set; }

	public ObjectivesForm(BG battlegroup)
	{
		InitializeComponent();
		BattleGroup = battlegroup;
		foreach (BG bg in BattleGroup.Owner)
		{
			if (bg.IsBattleGroup && bg.CD.NDFAlternative.Trans.FirstOrDefault((NdfTranReference fExport) => fExport.Value == bg.Export) == null)
			{
				unavailBG.Add(bg.Name);
			}
			_bgCheckedListBox.Items.Add(string.Format("{0,-3}:{1} ({2})", bg.Id, bg.Name, (bg.Country.Length > 0) ? bg.Country : "Neutral"), BattleGroup.ObjectiveBGs.Contains(bg) ? CheckState.Checked : (unavailBG.Contains(bg.Name) ? CheckState.Indeterminate : CheckState.Unchecked));
		}
		_bgCheckedListBox.ItemCheck += delegate(object s, ItemCheckEventArgs e)
		{
			if (e.CurrentValue == CheckState.Indeterminate)
			{
				e.NewValue = CheckState.Indeterminate;
			}
		};
		foreach (string zone in battlegroup.CD.DeparturesList)
		{
			_zonesCheckedListBox.Items.Add(zone, BattleGroup.ObjectiveZones.Contains(zone));
		}
	}

	private void buttonSave_Click(object sender, EventArgs e)
	{
		foreach (string item in _bgCheckedListBox.CheckedItems)
		{
			string sId = item.Substring(0, item.IndexOf(':')).Trim();
			uint id = uint.Parse(sId);
			BG bg = BattleGroup.Owner.First((BG fId) => fId.Id == id);
			if (!unavailBG.Contains(bg.Name))
			{
				CheckedBG.Add(bg.ShortDataBaseName);
			}
		}
		List<string> checkedZones = new List<string>();
		foreach (string zone in _zonesCheckedListBox.CheckedItems)
		{
			CheckedZones.Add(zone);
		}
		base.DialogResult = DialogResult.OK;
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
		this.bgLabel = new System.Windows.Forms.Label();
		this._bgCheckedListBox = new System.Windows.Forms.CheckedListBox();
		this.buttonSave = new System.Windows.Forms.Button();
		this.buttonCancel = new System.Windows.Forms.Button();
		this._zonesCheckedListBox = new System.Windows.Forms.CheckedListBox();
		this.zonesLabel = new System.Windows.Forms.Label();
		base.SuspendLayout();
		this.bgLabel.AutoSize = true;
		this.bgLabel.Location = new System.Drawing.Point(3, 8);
		this.bgLabel.Name = "bgLabel";
		this.bgLabel.Size = new System.Drawing.Size(105, 13);
		this.bgLabel.TabIndex = 0;
		this.bgLabel.Text = "Target Battle Groups";
		this._bgCheckedListBox.FormattingEnabled = true;
		this._bgCheckedListBox.Location = new System.Drawing.Point(7, 26);
		this._bgCheckedListBox.Name = "_bgCheckedListBox";
		this._bgCheckedListBox.Size = new System.Drawing.Size(269, 454);
		this._bgCheckedListBox.TabIndex = 2;
		this.buttonSave.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.buttonSave.Location = new System.Drawing.Point(342, 491);
		this.buttonSave.Name = "buttonSave";
		this.buttonSave.Size = new System.Drawing.Size(75, 23);
		this.buttonSave.TabIndex = 3;
		this.buttonSave.Text = "Save";
		this.buttonSave.UseVisualStyleBackColor = true;
		this.buttonSave.Click += new System.EventHandler(buttonSave_Click);
		this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.buttonCancel.Location = new System.Drawing.Point(423, 491);
		this.buttonCancel.Name = "buttonCancel";
		this.buttonCancel.Size = new System.Drawing.Size(75, 23);
		this.buttonCancel.TabIndex = 4;
		this.buttonCancel.Text = "Cancel";
		this.buttonCancel.UseVisualStyleBackColor = true;
		this._zonesCheckedListBox.FormattingEnabled = true;
		this._zonesCheckedListBox.Location = new System.Drawing.Point(282, 26);
		this._zonesCheckedListBox.Name = "_zonesCheckedListBox";
		this._zonesCheckedListBox.Size = new System.Drawing.Size(216, 454);
		this._zonesCheckedListBox.TabIndex = 5;
		this.zonesLabel.AutoSize = true;
		this.zonesLabel.Location = new System.Drawing.Point(277, 8);
		this.zonesLabel.Name = "zonesLabel";
		this.zonesLabel.Size = new System.Drawing.Size(71, 13);
		this.zonesLabel.TabIndex = 6;
		this.zonesLabel.Text = "Target Zones";
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.CancelButton = this.buttonCancel;
		base.ClientSize = new System.Drawing.Size(510, 521);
		base.ControlBox = false;
		base.Controls.Add(this.zonesLabel);
		base.Controls.Add(this._zonesCheckedListBox);
		base.Controls.Add(this.buttonCancel);
		base.Controls.Add(this.buttonSave);
		base.Controls.Add(this._bgCheckedListBox);
		base.Controls.Add(this.bgLabel);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "ObjectivesForm";
		this.Text = "Objectives";
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
