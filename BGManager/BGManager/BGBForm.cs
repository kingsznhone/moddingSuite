using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace BGManager;

public class BGBForm : Form
{
	private IContainer components = null;

	private DataGridView _bgbDataGridView;

	private DataGridView _bgbHdrDataGridView;

	public uint SelectedId { get; protected set; }

	public BGBForm(BG bg)
	{
		InitializeComponent();
		_bgbHdrDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_bgbDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
		PropertyInfo[] pi = typeof(BattleGroupBattle).GetProperties();
		DataGridViewColumn column = Gauges.CreateColumn("Type", 50f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: false, "");
		_bgbHdrDataGridView.Columns.Add(column);
		column = Gauges.CreateColumn("Type", 50f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: false, "");
		_bgbDataGridView.Columns.Add(column);
		column = Gauges.CreateColumn("Data", 50f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: true, "");
		_bgbHdrDataGridView.Columns.Add(column);
		for (int i = 0; i < pi.Length; i++)
		{
			_bgbHdrDataGridView.Rows.Add();
			_bgbDataGridView.Rows.Add();
			_bgbHdrDataGridView[0, i].Value = pi.GetValue(i).ToString();
			_bgbHdrDataGridView[1, i].Value = pi[i].Name;
		}
		DataGridViewCell bgCell = null;
		SelectedId = bg.BGBId;
		foreach (BattleGroupBattle bgb in bg.CD.BGBMap.Values)
		{
			column = Gauges.CreateColumn(bgb.id.ToString(), 10f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: true, "");
			_bgbDataGridView.Columns.Add(column);
			if (bgb.id == bg.BGBId)
			{
				bgCell = _bgbDataGridView[_bgbDataGridView.Columns.Count - 1, 0];
			}
			for (int i = 0; i < pi.Length; i++)
			{
				_bgbDataGridView[_bgbDataGridView.Columns.Count - 1, i].Value = ((pi[i].Name == "view") ? ".." : pi[i].GetValue(bgb).ToString());
			}
		}
		if (bgCell != null)
		{
			_bgbDataGridView.CurrentCell = bgCell;
			_bgbDataGridView.FirstDisplayedScrollingColumnIndex = bgCell.ColumnIndex;
		}
	}

	private void _bgbDataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		SelectedId = uint.Parse(_bgbDataGridView.Columns[_bgbDataGridView.CurrentCell.ColumnIndex].Name);
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
		this._bgbDataGridView = new System.Windows.Forms.DataGridView();
		this._bgbHdrDataGridView = new System.Windows.Forms.DataGridView();
		((System.ComponentModel.ISupportInitialize)this._bgbDataGridView).BeginInit();
		((System.ComponentModel.ISupportInitialize)this._bgbHdrDataGridView).BeginInit();
		base.SuspendLayout();
		this._bgbDataGridView.AllowUserToAddRows = false;
		this._bgbDataGridView.AllowUserToDeleteRows = false;
		this._bgbDataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
		this._bgbDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
		this._bgbDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this._bgbDataGridView.ColumnHeadersVisible = false;
		this._bgbDataGridView.Location = new System.Drawing.Point(128, 2);
		this._bgbDataGridView.Name = "_bgbDataGridView";
		this._bgbDataGridView.ReadOnly = true;
		this._bgbDataGridView.RowHeadersVisible = false;
		this._bgbDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullColumnSelect;
		this._bgbDataGridView.Size = new System.Drawing.Size(446, 337);
		this._bgbDataGridView.TabIndex = 0;
		this._bgbDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(_bgbDataGridView_CellDoubleClick);
		this._bgbHdrDataGridView.AllowUserToAddRows = false;
		this._bgbHdrDataGridView.AllowUserToDeleteRows = false;
		this._bgbHdrDataGridView.AllowUserToResizeColumns = false;
		this._bgbHdrDataGridView.AllowUserToResizeRows = false;
		this._bgbHdrDataGridView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
		this._bgbHdrDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this._bgbHdrDataGridView.ColumnHeadersVisible = false;
		this._bgbHdrDataGridView.Location = new System.Drawing.Point(1, 2);
		this._bgbHdrDataGridView.Name = "_bgbHdrDataGridView";
		this._bgbHdrDataGridView.ReadOnly = true;
		this._bgbHdrDataGridView.RowHeadersVisible = false;
		this._bgbHdrDataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullColumnSelect;
		this._bgbHdrDataGridView.Size = new System.Drawing.Size(126, 337);
		this._bgbHdrDataGridView.TabIndex = 1;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(575, 340);
		base.Controls.Add(this._bgbHdrDataGridView);
		base.Controls.Add(this._bgbDataGridView);
		base.Name = "BGBForm";
		this.Text = "TBattleGroupBattleModuleDescriptor";
		((System.ComponentModel.ISupportInitialize)this._bgbDataGridView).EndInit();
		((System.ComponentModel.ISupportInitialize)this._bgbHdrDataGridView).EndInit();
		base.ResumeLayout(false);
	}
}
