using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BGManager;

public class LimitationPerUnitForm : Form
{
	private DataTable _unitsTable = new DataTable();

	private IContainer components = null;

	private CheckBox _countryCheckBox;

	private ComboBox _countryComboBox;

	private CheckBox _yearCheckBox;

	private ComboBox _yearConditionComboBox;

	private ComboBox _yearComboBox;

	private CheckBox _nameFilterCheckBox;

	private TextBox _nameFilterTextBox;

	private DataGridView _unitGrid;

	private GroupBox groupBox1;

	private GroupBox groupBox2;

	private DataGridView _limitsGrid;

	private ComboBox _unitTypeComboBox;

	private CheckBox _unitTypeCheckBox;

	private DataGridView _bgGrid;

	private Label label1;

	private TextBox weaponTextBoxA;

	private TextBox weaponTextBoxB;

	private TextBox weaponTextBoxC;

	private TextBox unitGeneralData;

	private CheckBox _nativeTransportCheckBox;

	private BGManagerForm OwnerForm { get; set; }

	public uint UnitId { get; protected set; }

	public uint TrnId { get; protected set; }

	public DataBase DB { get; protected set; }

	public LimitationPerUnitForm(BGManagerForm owner, DataBase db)
	{
		OwnerForm = owner;
		DB = db;
		InitializeComponent();
		UnitId = 0u;
		TrnId = 0u;
		_unitsTable.Columns.Add("Id", typeof(uint));
		_unitsTable.Columns.Add("Name", typeof(string));
		_unitsTable.Columns.Add("Price", typeof(uint));
		_unitsTable.Columns.Add("Type", typeof(string));
		_unitsTable.Columns.Add("Role", typeof(string));
		_unitsTable.Columns.Add("Country", typeof(string));
		_unitsTable.Columns.Add("Year", typeof(string));
		Gauges.configureDataGridView(ref _unitGrid, autoGenerateColumns: true, readOnly: true);
		Gauges.configureDataGridView(ref _bgGrid, autoGenerateColumns: true, readOnly: true);
		SetupLimitsGrid(null);
		foreach (Unit unit in DB.UnitMap.Values)
		{
			if (((unit.Year == 0 && unit.Name == "FOB") || unit.Year != 0) && unit.Price != 0)
			{
				_unitsTable.Rows.Add(unit.Id, unit.Name, unit.Price, unit.UType, unit.BattleRole, unit.Country, unit.Year);
			}
		}
		_unitGrid.DataSource = _unitsTable;
		_unitGrid.Columns["Id"].FillWeight = 30f;
		_unitGrid.Columns["Name"].FillWeight = 100f;
		_unitGrid.Columns["Price"].FillWeight = 30f;
		_unitGrid.Columns["Type"].FillWeight = 40f;
		_unitGrid.Columns["Role"].FillWeight = 70f;
		_unitGrid.Columns["Country"].FillWeight = 30f;
		_unitGrid.Columns["Year"].FillWeight = 30f;
		foreach (string s in DB.CountryList)
		{
			_countryComboBox.Items.Add(s);
		}
		_yearConditionComboBox.Items.Add(" > ");
		_yearConditionComboBox.Items.Add(" < ");
		_yearConditionComboBox.Items.Add(" = ");
		_yearConditionComboBox.SelectedIndex = 0;
		for (uint year = DB.MinYear; year < DB.MaxYear; year++)
		{
			_yearComboBox.Items.Add(year);
		}
		Array unitTypes = Enum.GetValues(typeof(Unit.UnitType));
		foreach (Unit.UnitType ut in unitTypes)
		{
			if (ut != 0)
			{
				_unitTypeComboBox.Items.Add(ut);
			}
		}
		_yearComboBox.SelectedIndex = 0;
	}

	private void SetupLimitsGrid(string country)
	{
		Gauges.configureDataGridView(ref _limitsGrid, autoGenerateColumns: false, readOnly: false);
		foreach (DataGridViewColumn c in LimitationPerUnit.GenerateDGVColumns(DB, country))
		{
			_limitsGrid.Columns.Add(c);
		}
	}

	private void ApplyFilter()
	{
		List<string> filter = new List<string>();
		if (_nameFilterCheckBox.Checked && _nameFilterTextBox.Text.Length > 0)
		{
			filter.Add($"Name  LIKE '%{_nameFilterTextBox.Text}%'");
		}
		if (_countryCheckBox.Checked)
		{
			filter.Add($"Country LIKE '%{_countryComboBox.Text}%'");
		}
		if (_yearCheckBox.Checked)
		{
			filter.Add($"Year {_yearConditionComboBox.Text} {_yearComboBox.SelectedItem}");
		}
		if (_unitTypeCheckBox.Checked)
		{
			filter.Add($"Type LIKE '%{_unitTypeComboBox.SelectedItem}%'");
		}
		string filterStr = "";
		for (int i = 0; i < filter.Count; i++)
		{
			if (i > 0)
			{
				filterStr += " AND ";
			}
			filterStr += filter[i];
		}
		_unitsTable.DefaultView.RowFilter = filterStr;
	}

	internal void SelectUnit(uint unitId, uint lpuId)
	{
		_nameFilterCheckBox.Checked = false;
		_countryCheckBox.Checked = false;
		_unitTypeCheckBox.Checked = false;
		_yearCheckBox.Checked = false;
		_unitGrid.ClearSelection();
		int i = 0;
		foreach (DataGridViewRow dgvr in (IEnumerable)_unitGrid.Rows)
		{
			if ((uint)dgvr.Cells["Id"].Value == unitId)
			{
				_unitGrid.CurrentCell = _unitGrid.Rows[i].Cells[0];
				UnitId = unitId;
				UpdateUnitInfo();
				int j = 0;
				{
					foreach (DataGridViewRow lpuDgvr in (IEnumerable)_limitsGrid.Rows)
					{
						if ((uint)lpuDgvr.Cells["Id"].Value == lpuId)
						{
							_limitsGrid.CurrentCell = _limitsGrid.Rows[j].Cells[0];
							SelectLPU(lpuId);
							break;
						}
						j++;
					}
					break;
				}
			}
			i++;
		}
	}

	private void SelectLPU(uint lpuId)
	{
		List<SimpleBG> bgList = BuildLinkedBGList(lpuId);
		if (bgList.Count > 0)
		{
			_bgGrid.DataSource = bgList;
		}
	}

	private void UpdateUnitInfo()
	{
		LimitationPerUnitList filteredList = new LimitationPerUnitList(this, DB, UnitId);
		_limitsGrid.DataSource = filteredList;
		Gauges.configureDataGridView(ref _limitsGrid, Gauges.DGVConfigType.setEditableColumns, new List<string>(new string[3] { "Veterancy", "Transport", "SuperTransport" }));
		Unit u = DB.UnitMap[UnitId];
		SetupLimitsGrid(_nativeTransportCheckBox.Checked ? u.Country : null);
		weaponTextBoxA.Text = ((u.Turrets.Count > 0) ? u.Turrets[0].Description : "");
		weaponTextBoxB.Text = ((u.Turrets.Count > 1) ? u.Turrets[1].Description : "");
		weaponTextBoxC.Text = ((u.Turrets.Count > 2) ? u.Turrets[2].Description : "");
		unitGeneralData.Text = string.Format("Deck types : {0}{1}Movement type: {2}{1}Damage: {3}{1}Prototype: {4}{1}{5}{6}{7}ECM {8}%", u.DeckTypesString, Environment.NewLine, Unit.FormatMovingType(u.MoveType), u.Damage, u.IsPrototype ? "Yes" : "No", u.RoleString, u.MovementString, u.ArmorString, u.ECM);
	}

	private List<Deck> BuildLinkedDeckList(uint limitationId)
	{
		List<Deck> deckList = new List<Deck>();
		if (DB.LPUMap.Keys.Contains(limitationId))
		{
			foreach (Deck deck in DB.AllDeckList)
			{
				if (deck.FirstOrDefault((DeckUnit fLPU) => fLPU.LimitationId == limitationId) != null)
				{
					deckList.Add(deck);
				}
			}
		}
		return deckList;
	}

	private List<SimpleBG> BuildLinkedBGList(uint limitationId)
	{
		List<SimpleBG> bgList = new List<SimpleBG>();
		if (DB.LPUMap.Keys.Contains(limitationId))
		{
			foreach (KeyValuePair<string, DataBase.CampaignData> item in DB.CampaignMap)
			{
				foreach (BG bg in item.Value.BGL)
				{
					foreach (Deck deck in DB.LPUMap[limitationId].RegisteredDecks)
					{
						if (deck.Export == bg.Import)
						{
							bgList.Add(new SimpleBG(bg));
						}
					}
				}
			}
		}
		return bgList;
	}

	private void LimitationPerUnitForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		OwnerForm._unitsView = null;
	}

	private void filterChanged(object sender, EventArgs e)
	{
		ApplyFilter();
	}

	private void _nativeTransportCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		Unit u = null;
		if (_unitGrid.SelectedCells.Count > 0)
		{
			List<LimitationPerUnit> limits = new List<LimitationPerUnit>();
			UnitId = (uint)_unitGrid.CurrentRow.Cells["Id"].Value;
			u = DB.UnitMap[UnitId];
		}
		SetupLimitsGrid((_nativeTransportCheckBox.Checked && u != null) ? u.Country : null);
	}

	private void _unitGrid_SelectionChanged(object sender, EventArgs e)
	{
		TextBox textBox = weaponTextBoxA;
		TextBox textBox2 = weaponTextBoxB;
		string text2 = (weaponTextBoxC.Text = "");
		text2 = (textBox2.Text = text2);
		textBox.Text = text2;
		if (_unitGrid.SelectedCells.Count > 0)
		{
			List<LimitationPerUnit> limits = new List<LimitationPerUnit>();
			UnitId = (uint)_unitGrid.CurrentRow.Cells["Id"].Value;
			UpdateUnitInfo();
		}
		else
		{
			_limitsGrid.DataSource = null;
		}
	}

	private void _limitsGrid_SelectionChanged(object sender, EventArgs e)
	{
		if (_limitsGrid.SelectedCells.Count > 0)
		{
			uint limitationId = (uint)_limitsGrid.CurrentRow.Cells["Id"].Value;
			SelectLPU(limitationId);
		}
		_bgGrid.DataSource = null;
	}

	private void _limitsGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
	{
		LimitationPerUnit lpu = (LimitationPerUnit)_limitsGrid.Rows[e.RowIndex].DataBoundItem;
		if (lpu != null && lpu.UnitId == 0)
		{
			lpu.UnitId = (uint)_unitGrid.SelectedRows[0].Cells[0].Value;
		}
		if (_limitsGrid.Columns[e.ColumnIndex].Name == "Veterancy")
		{
			string s = e.FormattedValue.ToString().Trim();
			s = s.Replace(" ", string.Empty);
			Regex rgx = new Regex("^(\\d){1,2}\\/(\\d){1,2}\\/(\\d){1,2}\\/(\\d){1,2}\\/(\\d){1,2}$");
			bool isMatch = rgx.IsMatch(s);
			e.Cancel = !isMatch;
			if (!e.Cancel)
			{
				OwnerForm.Update(lpu);
			}
		}
	}

	private void _limitsGrid_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
	{
		LimitationPerUnit lpu = (LimitationPerUnit)e.Row.DataBoundItem;
		List<SimpleBG> bgList = BuildLinkedBGList(lpu.Id);
		List<Deck> deckList = BuildLinkedDeckList(lpu.Id);
		if (bgList.Count > 0)
		{
			string msg = "Those battle groups are referenced by limitation you deleting:\n";
			foreach (SimpleBG bg in bgList)
			{
				msg += $"Id:{bg.Id} {bg.Name} ({bg.Country})\n";
			}
			MessageBox.Show(msg, "Warning : cannot delete");
			e.Cancel = true;
		}
		else
		{
			if (deckList.Count <= 0)
			{
				return;
			}
			string msg = "Those decks are referenced by limitation you deleting:\n";
			foreach (Deck deck in deckList)
			{
				msg += $"Id:{deck.Id} {deck.Export} ({deck.Description})\n";
			}
			MessageBox.Show(msg, "Warning : cannot delete");
			e.Cancel = true;
		}
	}

	private void _limitsGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
	{
	}

	private void _limitsGrid_DockChanged(object sender, EventArgs e)
	{
		DB.DataChanged();
	}

	private void _limitsGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
	{
		LimitationPerUnit lpu = (LimitationPerUnit)_limitsGrid.Rows[e.RowIndex].DataBoundItem;
		if (lpu != null && lpu.UnitId == 0)
		{
			lpu.UnitId = (uint)_unitGrid.SelectedRows[0].Cells[0].Value;
		}
		if (_limitsGrid.Columns[e.ColumnIndex].Name == "Transport" || _limitsGrid.Columns[e.ColumnIndex].Name == "SuperTransport")
		{
			OwnerForm.Update(lpu);
		}
	}

	private void _limitsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		if (OwnerForm.SelectedUnit != null && _limitsGrid.SelectedCells.Count > 0)
		{
			uint limitationId = (uint)_limitsGrid.CurrentRow.Cells["Id"].Value;
			OwnerForm.ChangeLPU(limitationId);
		}
	}

	private void _bgGrid_DoubleClick(object sender, EventArgs e)
	{
		if (_bgGrid.SelectedCells.Count > 0)
		{
			uint bgId = (uint)_bgGrid.CurrentRow.Cells["Id"].Value;
		}
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
		this._countryCheckBox = new System.Windows.Forms.CheckBox();
		this._countryComboBox = new System.Windows.Forms.ComboBox();
		this._yearCheckBox = new System.Windows.Forms.CheckBox();
		this._yearConditionComboBox = new System.Windows.Forms.ComboBox();
		this._yearComboBox = new System.Windows.Forms.ComboBox();
		this._nameFilterCheckBox = new System.Windows.Forms.CheckBox();
		this._nameFilterTextBox = new System.Windows.Forms.TextBox();
		this._unitGrid = new System.Windows.Forms.DataGridView();
		this.groupBox1 = new System.Windows.Forms.GroupBox();
		this._unitTypeComboBox = new System.Windows.Forms.ComboBox();
		this._unitTypeCheckBox = new System.Windows.Forms.CheckBox();
		this.groupBox2 = new System.Windows.Forms.GroupBox();
		this._nativeTransportCheckBox = new System.Windows.Forms.CheckBox();
		this.label1 = new System.Windows.Forms.Label();
		this._bgGrid = new System.Windows.Forms.DataGridView();
		this._limitsGrid = new System.Windows.Forms.DataGridView();
		this.weaponTextBoxA = new System.Windows.Forms.TextBox();
		this.weaponTextBoxB = new System.Windows.Forms.TextBox();
		this.weaponTextBoxC = new System.Windows.Forms.TextBox();
		this.unitGeneralData = new System.Windows.Forms.TextBox();
		((System.ComponentModel.ISupportInitialize)this._unitGrid).BeginInit();
		this.groupBox1.SuspendLayout();
		this.groupBox2.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)this._bgGrid).BeginInit();
		((System.ComponentModel.ISupportInitialize)this._limitsGrid).BeginInit();
		base.SuspendLayout();
		this._countryCheckBox.AutoSize = true;
		this._countryCheckBox.Location = new System.Drawing.Point(11, 17);
		this._countryCheckBox.Name = "_countryCheckBox";
		this._countryCheckBox.Size = new System.Drawing.Size(62, 17);
		this._countryCheckBox.TabIndex = 0;
		this._countryCheckBox.Text = "Country";
		this._countryCheckBox.UseVisualStyleBackColor = true;
		this._countryCheckBox.CheckedChanged += new System.EventHandler(filterChanged);
		this._countryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._countryComboBox.FormattingEnabled = true;
		this._countryComboBox.Location = new System.Drawing.Point(9, 38);
		this._countryComboBox.Name = "_countryComboBox";
		this._countryComboBox.Size = new System.Drawing.Size(121, 21);
		this._countryComboBox.TabIndex = 1;
		this._countryComboBox.SelectedIndexChanged += new System.EventHandler(filterChanged);
		this._yearCheckBox.AutoSize = true;
		this._yearCheckBox.Location = new System.Drawing.Point(140, 17);
		this._yearCheckBox.Name = "_yearCheckBox";
		this._yearCheckBox.Size = new System.Drawing.Size(48, 17);
		this._yearCheckBox.TabIndex = 2;
		this._yearCheckBox.Text = "Year";
		this._yearCheckBox.UseVisualStyleBackColor = true;
		this._yearCheckBox.CheckedChanged += new System.EventHandler(filterChanged);
		this._yearConditionComboBox.FormattingEnabled = true;
		this._yearConditionComboBox.Location = new System.Drawing.Point(140, 38);
		this._yearConditionComboBox.Name = "_yearConditionComboBox";
		this._yearConditionComboBox.Size = new System.Drawing.Size(62, 21);
		this._yearConditionComboBox.TabIndex = 3;
		this._yearConditionComboBox.SelectedIndexChanged += new System.EventHandler(filterChanged);
		this._yearComboBox.FormattingEnabled = true;
		this._yearComboBox.Location = new System.Drawing.Point(208, 38);
		this._yearComboBox.Name = "_yearComboBox";
		this._yearComboBox.Size = new System.Drawing.Size(63, 21);
		this._yearComboBox.TabIndex = 4;
		this._yearComboBox.SelectedIndexChanged += new System.EventHandler(filterChanged);
		this._nameFilterCheckBox.AutoSize = true;
		this._nameFilterCheckBox.Location = new System.Drawing.Point(286, 17);
		this._nameFilterCheckBox.Name = "_nameFilterCheckBox";
		this._nameFilterCheckBox.Size = new System.Drawing.Size(52, 17);
		this._nameFilterCheckBox.TabIndex = 5;
		this._nameFilterCheckBox.Text = "name";
		this._nameFilterCheckBox.UseVisualStyleBackColor = true;
		this._nameFilterCheckBox.CheckedChanged += new System.EventHandler(filterChanged);
		this._nameFilterTextBox.Location = new System.Drawing.Point(286, 38);
		this._nameFilterTextBox.Name = "_nameFilterTextBox";
		this._nameFilterTextBox.Size = new System.Drawing.Size(91, 20);
		this._nameFilterTextBox.TabIndex = 6;
		this._nameFilterTextBox.TextChanged += new System.EventHandler(filterChanged);
		this._unitGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this._unitGrid.Location = new System.Drawing.Point(6, 66);
		this._unitGrid.Name = "_unitGrid";
		this._unitGrid.Size = new System.Drawing.Size(499, 199);
		this._unitGrid.TabIndex = 7;
		this._unitGrid.SelectionChanged += new System.EventHandler(_unitGrid_SelectionChanged);
		this.groupBox1.Controls.Add(this._unitTypeComboBox);
		this.groupBox1.Controls.Add(this._unitTypeCheckBox);
		this.groupBox1.Controls.Add(this._unitGrid);
		this.groupBox1.Controls.Add(this._nameFilterTextBox);
		this.groupBox1.Controls.Add(this._nameFilterCheckBox);
		this.groupBox1.Controls.Add(this._yearComboBox);
		this.groupBox1.Controls.Add(this._yearConditionComboBox);
		this.groupBox1.Controls.Add(this._yearCheckBox);
		this.groupBox1.Controls.Add(this._countryComboBox);
		this.groupBox1.Controls.Add(this._countryCheckBox);
		this.groupBox1.Location = new System.Drawing.Point(5, 3);
		this.groupBox1.Name = "groupBox1";
		this.groupBox1.Size = new System.Drawing.Size(511, 274);
		this.groupBox1.TabIndex = 0;
		this.groupBox1.TabStop = false;
		this.groupBox1.Text = "TUniteAuSolDescriptor filetered list";
		this._unitTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._unitTypeComboBox.FormattingEnabled = true;
		this._unitTypeComboBox.Location = new System.Drawing.Point(391, 37);
		this._unitTypeComboBox.Name = "_unitTypeComboBox";
		this._unitTypeComboBox.Size = new System.Drawing.Size(114, 21);
		this._unitTypeComboBox.TabIndex = 9;
		this._unitTypeComboBox.SelectedIndexChanged += new System.EventHandler(filterChanged);
		this._unitTypeCheckBox.AutoSize = true;
		this._unitTypeCheckBox.Location = new System.Drawing.Point(391, 17);
		this._unitTypeCheckBox.Name = "_unitTypeCheckBox";
		this._unitTypeCheckBox.Size = new System.Drawing.Size(68, 17);
		this._unitTypeCheckBox.TabIndex = 8;
		this._unitTypeCheckBox.Text = "Unit type";
		this._unitTypeCheckBox.UseVisualStyleBackColor = true;
		this._unitTypeCheckBox.CheckedChanged += new System.EventHandler(filterChanged);
		this.groupBox2.Controls.Add(this._nativeTransportCheckBox);
		this.groupBox2.Controls.Add(this.label1);
		this.groupBox2.Controls.Add(this._bgGrid);
		this.groupBox2.Controls.Add(this._limitsGrid);
		this.groupBox2.Location = new System.Drawing.Point(5, 283);
		this.groupBox2.Name = "groupBox2";
		this.groupBox2.Size = new System.Drawing.Size(511, 340);
		this.groupBox2.TabIndex = 1;
		this.groupBox2.TabStop = false;
		this.groupBox2.Text = "TLimitationPerUnit filtered list";
		this._nativeTransportCheckBox.AutoSize = true;
		this._nativeTransportCheckBox.Location = new System.Drawing.Point(7, 179);
		this._nativeTransportCheckBox.Name = "_nativeTransportCheckBox";
		this._nativeTransportCheckBox.Size = new System.Drawing.Size(145, 17);
		this._nativeTransportCheckBox.TabIndex = 3;
		this._nativeTransportCheckBox.Text = "List native transports only";
		this._nativeTransportCheckBox.UseVisualStyleBackColor = true;
		this._nativeTransportCheckBox.CheckedChanged += new System.EventHandler(_nativeTransportCheckBox_CheckedChanged);
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(6, 200);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(151, 13);
		this.label1.TabIndex = 2;
		this.label1.Text = "Battle groups this limitation use";
		this._bgGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this._bgGrid.Location = new System.Drawing.Point(6, 216);
		this._bgGrid.Name = "_bgGrid";
		this._bgGrid.Size = new System.Drawing.Size(499, 118);
		this._bgGrid.TabIndex = 1;
		this._bgGrid.DoubleClick += new System.EventHandler(_bgGrid_DoubleClick);
		this._limitsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this._limitsGrid.Location = new System.Drawing.Point(6, 19);
		this._limitsGrid.Name = "_limitsGrid";
		this._limitsGrid.Size = new System.Drawing.Size(499, 154);
		this._limitsGrid.TabIndex = 0;
		this._limitsGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(_limitsGrid_CellDoubleClick);
		this._limitsGrid.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(_limitsGrid_CellEndEdit);
		this._limitsGrid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(_limitsGrid_CellValidating);
		this._limitsGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(_limitsGrid_DataError);
		this._limitsGrid.SelectionChanged += new System.EventHandler(_limitsGrid_SelectionChanged);
		this._limitsGrid.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(_limitsGrid_UserDeletingRow);
		this._limitsGrid.DockChanged += new System.EventHandler(_limitsGrid_DockChanged);
		this.weaponTextBoxA.Location = new System.Drawing.Point(525, 13);
		this.weaponTextBoxA.Multiline = true;
		this.weaponTextBoxA.Name = "weaponTextBoxA";
		this.weaponTextBoxA.ReadOnly = true;
		this.weaponTextBoxA.Size = new System.Drawing.Size(396, 171);
		this.weaponTextBoxA.TabIndex = 2;
		this.weaponTextBoxB.Location = new System.Drawing.Point(525, 190);
		this.weaponTextBoxB.Multiline = true;
		this.weaponTextBoxB.Name = "weaponTextBoxB";
		this.weaponTextBoxB.ReadOnly = true;
		this.weaponTextBoxB.Size = new System.Drawing.Size(396, 128);
		this.weaponTextBoxB.TabIndex = 3;
		this.weaponTextBoxC.Location = new System.Drawing.Point(525, 324);
		this.weaponTextBoxC.Multiline = true;
		this.weaponTextBoxC.Name = "weaponTextBoxC";
		this.weaponTextBoxC.ReadOnly = true;
		this.weaponTextBoxC.Size = new System.Drawing.Size(396, 128);
		this.weaponTextBoxC.TabIndex = 4;
		this.unitGeneralData.Location = new System.Drawing.Point(525, 458);
		this.unitGeneralData.Multiline = true;
		this.unitGeneralData.Name = "unitGeneralData";
		this.unitGeneralData.ReadOnly = true;
		this.unitGeneralData.Size = new System.Drawing.Size(396, 165);
		this.unitGeneralData.TabIndex = 5;
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(933, 635);
		base.Controls.Add(this.unitGeneralData);
		base.Controls.Add(this.weaponTextBoxC);
		base.Controls.Add(this.weaponTextBoxB);
		base.Controls.Add(this.weaponTextBoxA);
		base.Controls.Add(this.groupBox2);
		base.Controls.Add(this.groupBox1);
		base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.Name = "LimitationPerUnitForm";
		base.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
		this.Text = "Units & Limitations";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(LimitationPerUnitForm_FormClosing);
		((System.ComponentModel.ISupportInitialize)this._unitGrid).EndInit();
		this.groupBox1.ResumeLayout(false);
		this.groupBox1.PerformLayout();
		this.groupBox2.ResumeLayout(false);
		this.groupBox2.PerformLayout();
		((System.ComponentModel.ISupportInitialize)this._bgGrid).EndInit();
		((System.ComponentModel.ISupportInitialize)this._limitsGrid).EndInit();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
