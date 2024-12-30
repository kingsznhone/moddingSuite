using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using moddingSuite.BL;
using moddingSuite.BL.Ndf;
using moddingSuite.Model.Edata;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types;

namespace BGManager;

public class BGManagerForm : Form
{
	internal LimitationPerUnitForm _unitsView = null;

	private bool _ndf_changed = false;

	private bool _hash_changed = false;

	private BGSettings _settings = BGSettingsManager.Load();

	private Deck _exportDeck = null;

	private Progress _progressBox = new Progress();

	private bool _initDone = false;

	private uint _selectedBgId = 0u;

	private ToolTip _bgmToolTip = new ToolTip();

	private ContextMenuStrip menu = null;

	private BG _rightClickedBG = null;

	private IContainer components = null;

	private ToolStrip bgToolStrip;

	private DataGridView deckInstancesGrid;

	private DataGridView deckGrid;

	private DataGridView bgGrid;

	private StatusStrip bgStatusStrip;

	private CheckBox _BGOnlyCheckBox;

	private Button _cfgButton;

	private OpenFileDialog openDatFileDialog;

	private OpenFileDialog openHashFileDialog;

	private ComboBox _campaignPickComboBox;

	private Label _deckDescriptionLabel;

	private Label campaignLabel;

	private Button _unitsViewButton;

	private ToolStripStatusLabel _redSideCampaignSummaryLabel;

	private ToolStripStatusLabel _blueSideCampaignSummary;

	private Button saveNdfButton;

	private ToolStripStatusLabel _campaignRatioToolStrip;

	private ComboBox _countryComboBox;

	private CheckBox _countryCheckBox;

	private CheckBox _categoryCheckBox;

	private ComboBox _categoryComboBox;

	private CheckBox _divisionCheckBox;

	private ComboBox _divisionComboBox;

	private CheckBox _typeCheckBox;

	private ComboBox _typeComboBox;

	private Button _thinkButton;

	private Button _showChangesButton;

	private CheckBox _sideCheckBox;

	private ComboBox _sideComboBox;

	private Button _donateButton;

	private Button _exportCampaignButton;

	private Button _exportEverythingButton;

	public DataBase DB { get; protected set; }

	public BGSettings Settings => _settings;

	public Unit SelectedUnit { get; protected set; }

	public BGManagerForm()
	{
		this.WindowState = FormWindowState.Maximized;

        InitializeComponent();
		string errorText = "";
		if (!DataBase.Validate(_settings, out errorText))
		{
			ConfigurationForm cf = new ConfigurationForm(_settings);
			DialogResult dr = cf.ShowDialog();
			if (dr != DialogResult.OK)
			{
				return;
			}
			_settings = cf.Settings;
		}
		_BGOnlyCheckBox.Checked = _settings.ShowAllBG;
		Collection<string> campaigns = new Collection<string>();
		_settings.LoadCampaignList(ref campaigns);
		_campaignPickComboBox.Items.Clear();
		foreach (string c in campaigns)
		{
			_campaignPickComboBox.Items.Add(c);
		}
		_campaignPickComboBox.SelectedItem = _settings.SelectedCampaign;
		while (true)
		{
			bool flag = true;
			if (LoadDataBase())
			{
				break;
			}
			MessageBox.Show("Wrong configuration. Please re-configure");
		}
		SetupBGFilters();
		SetupInterface();
		Gauges.InitLogFile($"BGM_{DateTime.Now:MMddyy_HHmm}.log");
		_initDone = true;
	}

	internal bool LoadDataBase()
	{
		DB = new DataBase(this, _settings, _progressBox);
		return DB.Valid;
	}

	internal void InvalidateGrids()
	{
		deckGrid.ClearSelection();
		deckInstancesGrid.ClearSelection();
		bgGrid.ClearSelection();
		deckGrid.Invalidate();
		deckInstancesGrid.Invalidate();
		bgGrid.Invalidate();
	}

	internal void SetupBGFilters()
	{
		_countryComboBox.Items.Clear();
		foreach (string c in DB.CountryList)
		{
			_countryComboBox.Items.Add(c);
		}
		_categoryComboBox.Items.Clear();
		foreach (string c in DB.CampaignMap[_settings.SelectedCampaign].CategoryList)
		{
			_categoryComboBox.Items.Add(c);
		}
		_divisionComboBox.Items.Clear();
		foreach (string c in DB.CampaignMap[_settings.SelectedCampaign].DivisionList)
		{
			_divisionComboBox.Items.Add(c);
		}
		_typeComboBox.Items.Clear();
		foreach (string c in DB.TypeList)
		{
			_typeComboBox.Items.Add(c);
		}
		UpdateCampaignStats();
	}

	internal void LoadDeck(Deck deck, string deckName)
	{
		_deckDescriptionLabel.Text = deck.Description;
		deckGrid.DataSource = deck;
		if (_initDone)
		{
			deckGrid.Sort(deckGrid.Columns["Type"], ListSortDirection.Ascending);
			string bgName = "";
			if (_initDone && bgGrid.SelectedCells.Count > 0)
			{
				BG bg = (BG)bgGrid.CurrentRow.DataBoundItem;
				if (bg.GetDeckIndex() == deck.Id)
				{
					bgName = bg.Name;
				}
			}
			Text = ((bgName.Length > 0) ? (bgName + ":") : "");
			object obj = Text;
			Text = string.Concat(obj, deck.Id, ":", deck.Export);
		}
		Invalidate();
	}

	internal void UpdateCampaignStats()
	{
		DB.CampaignMap[Settings.SelectedCampaign].Statistics.ComputeSides();
		_redSideCampaignSummaryLabel.Text = DB.CampaignMap[Settings.SelectedCampaign].Statistics.Computer.ShortDescription;
		_blueSideCampaignSummary.Text = DB.CampaignMap[Settings.SelectedCampaign].Statistics.Player.ShortDescription;
		_campaignRatioToolStrip.Text = DB.CampaignMap[Settings.SelectedCampaign].Statistics.Ratio;
	}

	internal void SetupInterface()
	{
		_bgmToolTip.SetToolTip(_cfgButton, _settings.DataFilePath + " and " + _settings.DictionaryFilePath);
		SelectedUnit = null;
		Gauges.configureDataGridView(ref deckInstancesGrid, autoGenerateColumns: false, readOnly: true);
		Gauges.configureDataGridView(ref bgGrid, autoGenerateColumns: false, readOnly: false);
		Gauges.configureDataGridView(ref deckGrid, autoGenerateColumns: false, readOnly: false);
		deckInstancesGrid.Columns.Clear();
		foreach (DataGridViewColumn c in Deck.GenerateDGVColumns())
		{
			deckInstancesGrid.Columns.Add(c);
		}
		bgGrid.Columns.Clear();
		foreach (DataGridViewColumn c in BG.GenerateDGVColumns(DB.CampaignMap[_settings.SelectedCampaign]))
		{
			bgGrid.Columns.Add(c);
		}
		deckGrid.Columns.Clear();
		foreach (DataGridViewColumn c in DeckUnit.GenerateDGVColumns())
		{
			deckGrid.Columns.Add(c);
		}
		ApplyBGFilter(invalidateGrids: false);
		if (DB.DeckMap.Count > 0)
		{
			deckInstancesGrid.DataSource = DB.AllDeckList;
			deckInstancesGrid.ClearSelection();
			deckInstancesGrid.Columns[0].ToolTipText = "Instance ID in TWargameNationDeck";
			deckInstancesGrid.Sort(deckInstancesGrid.Columns[0], ListSortDirection.Ascending);
		}
		if (DB.CampaignMap[_settings.SelectedCampaign].BGL.Count > 0)
		{
			bgGrid.Sort(bgGrid.Columns[0], ListSortDirection.Ascending);
		}
	}

	internal void ConfigureDatFile()
	{
		DialogResult dr = openDatFileDialog.ShowDialog();
		if (dr == DialogResult.OK)
		{
			_settings.DataFilePath = openDatFileDialog.FileName;
		}
	}

	internal void ConfigureHashPath()
	{
		DialogResult dr = openHashFileDialog.ShowDialog();
		if (dr == DialogResult.OK)
		{
			_settings.DictionaryFilePath = openHashFileDialog.FileName;
		}
	}

	internal bool ConfigureDatabasePath(BGSettings settings, string prompt)
	{
		FolderBrowserDialog fbd = new FolderBrowserDialog();
		fbd.Description = prompt;
		fbd.ShowNewFolderButton = false;
		fbd.SelectedPath = Directory.GetCurrentDirectory();
		if (fbd.ShowDialog() != DialogResult.OK)
		{
			return false;
		}
		settings.DataFilePath = fbd.SelectedPath + "\\NDF_Win.dat";
		settings.DictionaryFilePath = fbd.SelectedPath + "\\ZZ_Win.dat";
		return true;
	}

	internal void SaveChanges()
	{
		if (_ndf_changed)
		{
			DB.SaveNDF();
			_ndf_changed = false;
		}
		if (_hash_changed)
		{
			DB.SaveHash();
			_hash_changed = false;
		}
	}

	private bool FilterBattleGroup(BG bg)
	{
		if (_BGOnlyCheckBox.Checked && (!bg.IsBattleGroup || bg.InitId == 0))
		{
			return false;
		}
		if (_sideCheckBox.Checked && (!bg.IsBattleGroup || bg.Side.ToString() != (string)_sideComboBox.SelectedItem))
		{
			return false;
		}
		if (_countryCheckBox.Checked && bg.Country != (string)_countryComboBox.SelectedItem)
		{
			return false;
		}
		if (_categoryCheckBox.Checked && bg.Category != (string)_categoryComboBox.SelectedItem)
		{
			return false;
		}
		if (_divisionCheckBox.Checked && bg.Division != (string)_divisionComboBox.SelectedItem)
		{
			return false;
		}
		if (_typeCheckBox.Checked && bg.MimeticType != (string)_typeComboBox.SelectedItem)
		{
			return false;
		}
		return true;
	}

	private void ApplyBGFilter(bool invalidateGrids = true)
	{
		_settings.ShowAllBG = _BGOnlyCheckBox.Checked;
		if (_BGOnlyCheckBox.Checked || _sideCheckBox.Checked || _countryCheckBox.Checked || _categoryCheckBox.Checked || _divisionCheckBox.Checked || _typeCheckBox.Checked)
		{
			BGList filteredList = new BGList(DB.CampaignMap[_settings.SelectedCampaign].BGL, FilterBattleGroup);
			bgGrid.DataSource = filteredList;
			bgGrid.Sort(bgGrid.Columns["Name"], ListSortDirection.Descending);
		}
		else
		{
			foreach (BG bg in DB.CampaignMap[_settings.SelectedCampaign].BGL)
			{
				bg.SortOwner = DB.CampaignMap[_settings.SelectedCampaign].BGL;
			}
			bgGrid.DataSource = DB.CampaignMap[_settings.SelectedCampaign].BGL;
		}
		if (invalidateGrids)
		{
			InvalidateGrids();
		}
	}

	private void DisplayUnitView(bool show)
	{
		if (_unitsView == null)
		{
			_unitsView = new LimitationPerUnitForm(this, DB);
		}
		if (show)
		{
			_unitsView.Show();
		}
		else
		{
			_unitsView.Hide();
		}
	}

	private void ExportCampaign(DataBase exportDB, string name)
	{
		int i = 0;
		int bgCount = DB.CampaignMap[name].BGL.Count;
		_progressBox.SetJobTitle("Exporting strings and trans table references...");
		exportDB.CampaignMap[name].Upgrade(DB.CampaignMap[name]);
		_progressBox.SetJobTitle($"Exporting {bgCount} battle groups from '{name}'");
		_progressBox.Show();
		foreach (BG bg in exportDB.CampaignMap[name].BGL)
		{
			BG bG = bg;
			BGList bGL = DB.CampaignMap[name].BGL;
			Func<BG, bool> predicate = (BG fId) => fId.Id == bg.Id;
			if (bG.Upgrade(bGL.FirstOrDefault(predicate)))
			{
				_progressBox.UpdateProgress(i, bgCount);
			}
			i++;
		}
		exportDB.CampaignMap[name].DataChanged(DataChangeType.BattleGroup);
		exportDB.CampaignMap[name].DataChanged(DataChangeType.Deck);
		exportDB.CampaignMap[name].DataChanged(DataChangeType.Hash);
		_progressBox.Hide();
	}

	private void ReportChanges(DataBase originalDB, string campaign, TextViewForm tvf)
	{
		tvf.AddLine("Summary of mod : " + campaign);
		tvf.AddLine(DB.CampaignMap[campaign].Statistics.Ratio);
		tvf.AddLine("=================================================", 1);
		tvf.AddLine("Conflict side : Computer");
		tvf.AddLine("-------------------------------------------------");
		tvf.AddLine(DB.CampaignMap[campaign].Statistics.Computer.UnitsSummary);
		if (Settings.FullSummary)
		{
			foreach (BG bg2 in originalDB.CampaignMap[campaign].BGL)
			{
				string bgDiff = "";
				BGList bGL = DB.CampaignMap[Settings.SelectedCampaign].BGL;
				Func<BG, bool> predicate = (BG fId) => fId.Id == bg2.Id;
				BG sameIdBG = bGL.FirstOrDefault(predicate);
				if (sameIdBG != null && bg2.Side == ConflictSide.Computer && bg2.IsBattleGroup && bg2.InitId != 0 && !bg2.Equal(sameIdBG, ref bgDiff, complete: true))
				{
					tvf.AddLine(bgDiff);
				}
			}
		}
		tvf.AddLine("Conflict side : Player");
		tvf.AddLine("-------------------------------------------------");
		tvf.AddLine(DB.CampaignMap[campaign].Statistics.Player.UnitsSummary);
		if (Settings.FullSummary)
		{
			foreach (BG bg in originalDB.CampaignMap[campaign].BGL)
			{
				string bgDiff = "";
				if (bg.Side == ConflictSide.Player && bg.IsBattleGroup && bg.InitId != 0 && !bg.Equal(DB.CampaignMap[campaign].BGL.FirstOrDefault((BG fId) => fId.Id == bg.Id), ref bgDiff, complete: true))
				{
					tvf.AddLine(bgDiff);
				}
			}
		}
		tvf.Text = "Summary of mod : " + campaign;
	}

	private Deck SelectedDeck()
	{
		if (deckGrid.Rows.Count > 0)
		{
			DeckUnit du = (DeckUnit)deckGrid.Rows[0].DataBoundItem;
			if (du != null)
			{
				return du.HostDeck;
			}
		}
		return null;
	}

	public void DataChanged(DataChangeType dct = DataChangeType.Deck)
	{
		if (_initDone)
		{
			if (SelectedDeck() != null)
			{
				SelectedDeck().UpdateDeckInfo();
				deckGrid.InvalidateRow(deckGrid.SelectedRows[0].Index);
				_deckDescriptionLabel.Text = SelectedDeck().Description;
			}
			if (dct == DataChangeType.BattleGroup)
			{
			}
			if (dct == DataChangeType.Hash)
			{
				_hash_changed = true;
			}
			UpdateCampaignStats();
			saveNdfButton.Enabled = true;
			_ndf_changed = true;
		}
	}

	public void SelectBG(uint bgId)
	{
		BG bg = DB.CampaignMap[_settings.SelectedCampaign].BGL.FirstOrDefault((BG Id) => Id.Id == bgId);
		if (bg == null || bg.Import.Length <= 0 || !DB.DeckMap.ContainsKey(bg.Import))
		{
			return;
		}
		_selectedBgId = bg.Id;
		Deck deck = (_exportDeck = DB.DeckMap[bg.Import]);
		deckInstancesGrid.ClearSelection();
		deckGrid.ClearSelection();
		int deckIdx = -1;
		int i = 0;
		foreach (DataGridViewRow row in (IEnumerable)deckInstancesGrid.Rows)
		{
			if (deck.Id == (uint)row.Cells["Id"].Value)
			{
				deckIdx = i;
				break;
			}
			i++;
		}
		if (deckIdx >= 0)
		{
			deckInstancesGrid.CurrentCell = deckInstancesGrid.Rows[deckIdx].Cells[0];
		}
		if (_exportDeck != null)
		{
			LoadDeck(_exportDeck, _exportDeck.Export);
			_exportDeck = null;
		}
	}

	public void Update(LimitationPerUnit lpu)
	{
		if (SelectedDeck() != null && lpu != null)
		{
			DeckUnit du = SelectedDeck().FirstOrDefault((DeckUnit fLPU) => fLPU.LimitationId == lpu.Id);
			if (du != null)
			{
				du.Update(lpu);
				deckGrid.Invalidate();
			}
		}
	}

	public void ChangeLPU(uint limitationId)
	{
		if (DB.LPUMap.ContainsKey(limitationId) && SelectedDeck() != null)
		{
			LimitationPerUnit lpu = DB.LPUMap[limitationId];
			DeckUnit du = (DeckUnit)deckGrid.SelectedRows[0].DataBoundItem;
			du.LimitationId = limitationId;
			DB.CampaignMap[_settings.SelectedCampaign].DataChanged(DataChangeType.Deck);
			BringToFront();
			SelectedUnit = null;
		}
	}

	private void deckInstancesGrid_SelectionChanged(object sender, EventArgs e)
	{
		if (deckInstancesGrid.SelectedCells.Count > 0 && _initDone)
		{
			Deck obj = ((_exportDeck == null) ? ((Deck)deckInstancesGrid.CurrentRow.DataBoundItem) : _exportDeck);
			LoadDeck(obj, obj.Export);
			_exportDeck = null;
		}
	}

	private void _BGManagerForm_FormClosing(object sender, FormClosingEventArgs e)
	{
		if (_ndf_changed || _hash_changed)
		{
			DialogResult confirmResult = MessageBox.Show("Do you want to save changes?", "Exit program requested", MessageBoxButtons.YesNo);
			if (confirmResult == DialogResult.Yes)
			{
				SaveChanges();
			}
		}
		BGSettingsManager.Save(_settings);
		Gauges.CloseLogFile(_settings.KeepLogFiles);
	}

	private void _cfgButton_Click(object sender, EventArgs e)
	{
		_initDone = false;
		ConfigurationForm cf = new ConfigurationForm(_settings);
		DialogResult dr = cf.ShowDialog();
		if (dr == DialogResult.OK)
		{
			bool skipReload = _settings.DataFilePath == cf.Settings.DataFilePath && _settings.DictionaryFilePath == cf.Settings.DictionaryFilePath && _settings.EverythingNDFPattern == cf.Settings.EverythingNDFPattern && _settings.UnitHashPattern == cf.Settings.UnitHashPattern && _settings.BGHashPattern == cf.Settings.BGHashPattern && _settings.InterfaceHashPattern == cf.Settings.InterfaceHashPattern;
			_settings = cf.Settings;
			if (!skipReload && LoadDataBase())
			{
				SetupBGFilters();
				SetupInterface();
			}
		}
		_initDone = true;
		
	}

	private void _campaignPickComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		_settings.SelectedCampaign = (string)_campaignPickComboBox.SelectedItem;
		if (_initDone && DB != null && DB.CampaignMap.Count > 0)
		{
			bgGrid.DataSource = null;
			InvalidateGrids();
			_initDone = false;
			SetupBGFilters();
			SetupInterface();
			UpdateCampaignStats();
			_initDone = true;
		}
	}

	private void _unitsViewButton_Click(object sender, EventArgs e)
	{
		bool shown = _unitsView != null && _unitsView.Visible;
		DisplayUnitView(!shown);
	}

	private void _thinkButton_Click(object sender, EventArgs e)
	{
		FolderBrowserDialog fbd = new FolderBrowserDialog();
		fbd.SelectedPath = Directory.GetCurrentDirectory();
		if (fbd.ShowDialog() != DialogResult.OK)
		{
			return;
		}
		List<string> fileList = new List<string>();
		Gauges.BuildFileList(ref fileList, fbd.SelectedPath, "*.dat");
		TextViewForm tvf = new TextViewForm();
		foreach (string fileName in fileList)
		{
			tvf.AddLine(fileName);
			EdataManager eDataManager = new EdataManager(fileName);
			eDataManager.ParseEdataFile();
			Dictionary<string, Dictionary<string, HashSet<string>>> data = new Dictionary<string, Dictionary<string, HashSet<string>>>();
			_progressBox.SetJobTitle($"Searching {eDataManager.Files.Count} .ndf files");
			_progressBox.Show();
			_progressBox.UpdateProgress(0, eDataManager.Files.Count);
			int i = 0;
			foreach (EdataContentFile ndfFile in eDataManager.Files)
			{
				_progressBox.UpdateProgress(i++, eDataManager.Files.Count);
				NdfbinReader ndfbinReader = new NdfbinReader();
				NdfBinary ndfBin = null;
				try
				{
					ndfBin = ndfbinReader.Read(eDataManager.GetRawData(ndfFile));
				}
				catch (Exception)
				{
					ndfBin = null;
				}
				if (ndfBin == null)
				{
					continue;
				}
				Dictionary<string, HashSet<string>> classData = new Dictionary<string, HashSet<string>>();
				foreach (NdfClass cls in ndfBin.Classes)
				{
					HashSet<string> propSet = new HashSet<string>();
					foreach (NdfObject inst in cls.Instances)
					{
						foreach (NdfPropertyValue prop in inst.PropertyValues)
						{
							if (prop.Type == NdfType.TableString && (NDFWrappers.ndfGetString(prop.Value).Contains("MI_8_CHI") || NDFWrappers.ndfGetString(prop.Value).Contains("StrategicTexture_") || NDFWrappers.ndfGetString(prop.Value).Contains("_CampagneDynamique") || NDFWrappers.ndfGetString(prop.Value).Contains("Modele_")))
							{
								propSet.Add($"{prop.Property.Name} ({prop.Value})");
							}
						}
					}
					if (propSet.Count > 0)
					{
						classData.Add(cls.Name, propSet);
					}
				}
				if (classData.Count > 0)
				{
					data.Add(ndfFile.ToString(), classData);
				}
			}
			if (data.Count <= 0)
			{
				continue;
			}
			foreach (KeyValuePair<string, Dictionary<string, HashSet<string>>> ndfInfo in data)
			{
				tvf.AddLine("    " + ndfInfo.Key);
				foreach (KeyValuePair<string, HashSet<string>> clsInfo in ndfInfo.Value)
				{
					foreach (string propInfo in clsInfo.Value)
					{
						tvf.AddLine($"\tCLS {clsInfo.Key,-40}:{propInfo}");
					}
				}
			}
		}
		_progressBox.Hide();
		tvf.ShowDialog();
	}

	private void _showChangesButton_Click(object sender, EventArgs e)
	{
		BGSettings settings = new BGSettings(Settings);
		DialogResult dr = openDatFileDialog.ShowDialog();
		if (dr != DialogResult.OK)
		{
			return;
		}
		settings.DataFilePath = openDatFileDialog.FileName;
		dr = openHashFileDialog.ShowDialog();
		if (dr != DialogResult.OK)
		{
			return;
		}
		settings.DictionaryFilePath = openHashFileDialog.FileName;
		DataBase originalDB = new DataBase(this, settings, _progressBox);
		TextViewForm tvf = new TextViewForm();
		foreach (string campaign in Settings.Campaigns.Keys)
		{
			ReportChanges(originalDB, campaign, tvf);
		}
		tvf.ShowDialog();
	}

	private void _saveNdfButton_Click(object sender, EventArgs e)
	{
		SaveChanges();
		saveNdfButton.Enabled = false;
	}

	private void _donateButton_Click(object sender, EventArgs e)
	{
		string url = "";
		string business = "rogers@qmg.org";
		string description = "Donation%20for%20BGManager";
		string country = "US";
		string currency = "USD";
		string text = url;
		url = text + "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=" + business + "&lc=" + country + "&item_name=" + description + "&currency_code=" + currency + "&bn=PP%2dDonationsBF";
		Process.Start(url);
	}

	private void _exportCampaignButton_Click(object sender, EventArgs e)
	{
		BGSettings settings = new BGSettings(Settings);
		if (ConfigureDatabasePath(settings, "Path to NDF_Win.dat and ZZ_Win.dat files to export selected campaign"))
		{
			_initDone = false;
			DataBase exportDB = new DataBase(this, settings, _progressBox);
			ExportCampaign(exportDB, Settings.SelectedCampaign);
			_progressBox.Show();
			_progressBox.SetJobTitle("Saving .dat-files...");
			_progressBox.UpdateProgress(1, 2);
			exportDB.SaveHash();
			_progressBox.UpdateProgress(2, 2);
			exportDB.SaveNDF();
			_progressBox.Hide();
			_initDone = true;
		}
	}

	private void _exportEverythingButton_Click(object sender, EventArgs e)
	{
		BGSettings settings = new BGSettings(Settings);
		if (!ConfigureDatabasePath(settings, "Path to NDF_Win.dat and ZZ_Win.dat files to export"))
		{
			return;
		}
		_initDone = false;
		DataBase exportDB = new DataBase(this, settings, _progressBox);
		foreach (KeyValuePair<string, DataBase.CampaignData> item in DB.CampaignMap)
		{
			ExportCampaign(exportDB, item.Key);
		}
		_progressBox.Show();
		_progressBox.SetJobTitle("Saving .dat-files...");
		_progressBox.UpdateProgress(1, 2);
		exportDB.SaveHash();
		_progressBox.UpdateProgress(2, 2);
		exportDB.SaveNDF();
		_progressBox.Hide();
		_initDone = true;
	}

	private void deckGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
	{
		DB.CampaignMap[_settings.SelectedCampaign].DataChanged(DataChangeType.Deck);
	}

	private void deckGrid_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
	{
	}

	private void deckGrid_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
	{
		if (SelectedDeck() != null)
		{
			if (SelectedDeck().Count == 1)
			{
				MessageBox.Show("Cannot erase Unit Limitation", "Error", MessageBoxButtons.OK);
				e.Cancel = true;
			}
			DeckUnit du = (DeckUnit)deckGrid.SelectedRows[0].DataBoundItem;
			du.LPU.UnregisterDeck(SelectedDeck());
			if (!du.NewDeckUnit)
			{
				DataChanged();
			}
		}
	}

	private void deckGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
	{
		if (_initDone && deckGrid.SelectedCells.Count > 0 && SelectedDeck() != null)
		{
			DisplayUnitView(show: true);
			uint lpuId = (uint)deckGrid.SelectedRows[0].Cells["Id"].Value;
			SelectedUnit = DB.UnitMap[DB.LPUMap[lpuId].UnitId];
			_unitsView.SelectUnit(DB.LPUMap[lpuId].UnitId, lpuId);
			_unitsView.BringToFront();
		}
	}

	private void deckGrid_SelectionChanged(object sender, EventArgs e)
	{
		SelectedUnit = null;
	}

	private void menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
	{
		if (_rightClickedBG != null)
		{
			_rightClickedBG.DivisionCountry = e.ClickedItem.Text;
			_rightClickedBG = null;
		}
	}

	private void bgGrid_SelectionChanged(object sender, EventArgs e)
	{
		if (_initDone && bgGrid.SelectedCells.Count > 0 && bgGrid.CurrentRow != null)
		{
			BG bg = (BG)bgGrid.CurrentRow.DataBoundItem;
			SelectBG(bg.Id);
		}
		else
		{
			deckInstancesGrid.ClearSelection();
			deckGrid.DataSource = null;
		}
	}

	private void bgGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
	{
		BG bg = null;
		if (bgGrid.SelectedCells.Count <= 0)
		{
			return;
		}
		uint bgId = (uint)bgGrid.SelectedRows[0].Cells["Id"].Value;
		bg = DB.CampaignMap[_settings.SelectedCampaign].BGL.FirstOrDefault((BG fId) => fId.Id == bgId);
		if (!(bgGrid.Columns[e.ColumnIndex].Name == "Pawn") || !bg._sharedData.ContainsKey("DepictionTemplate"))
		{
			return;
		}
		string sharedList = "";
		int i = 0;
		foreach (BG otherBG in bg._sharedData["DepictionTemplate"])
		{
			if (++i > 10)
			{
				sharedList += "and others\n";
				break;
			}
			object obj = sharedList;
			sharedList = string.Concat(obj, otherBG.Name, "(id:", otherBG.Id, ")");
			sharedList += "\n";
		}
		if (MessageBox.Show("This battlegroup shares Pawn icon with\n" + sharedList + "You are about to change them all", "Warning", MessageBoxButtons.OKCancel) != DialogResult.OK)
		{
			e.Cancel = true;
		}
	}

	private void bgGrid_CellClick(object sender, DataGridViewCellEventArgs e)
	{
		if (e.RowIndex < 0 || bgGrid.SelectedCells.Count < 1)
		{
			return;
		}
		uint bgId = (uint)bgGrid.SelectedRows[0].Cells["Id"].Value;
		Rectangle location = bgGrid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, cutOverflow: true);
		BG bg = DB.CampaignMap[_settings.SelectedCampaign].BGL.FirstOrDefault((BG fId) => fId.Id == bgId);
		if (bgGrid.Columns[e.ColumnIndex].DataPropertyName == "Pawn")
		{
			PickPawnForm ppForm = new PickPawnForm(bg.CD.PawnModelList, bg.Pawn);
			ppForm.SetDesktopLocation(location.Left, location.Bottom);
			if (ppForm.ShowDialog() == DialogResult.OK)
			{
				bg.Pawn = ppForm.Pawn;
			}
		}
		else if (bgGrid.Columns[e.ColumnIndex].DataPropertyName == "BGB")
		{
			BGBForm bgbForm = new BGBForm(bg);
			if (bgbForm.ShowDialog() != DialogResult.OK)
			{
				return;
			}
			uint selectedId = bgbForm.SelectedId;
			if (bg.CD.BGBMap.FirstOrDefault((KeyValuePair<string, BattleGroupBattle> fId) => fId.Value.id == selectedId).Value != null)
			{
				bg.BGB = bg.CD.BGBMap.FirstOrDefault((KeyValuePair<string, BattleGroupBattle> fId) => fId.Value.id == selectedId).Value.view;
			}
		}
		else if (bgGrid.Columns[e.ColumnIndex].Name == "Objectives" && bg._sharedData.ContainsKey("ObjectivesList"))
		{
			ObjectivesForm of = new ObjectivesForm(bg);
			if (of.ShowDialog() == DialogResult.OK)
			{
				bg.ReplaceObjectives(of.CheckedBG, replacingZones: false);
				bg.ReplaceObjectives(of.CheckedZones, replacingZones: true);
				bg.CD.DataChanged(DataChangeType.BattleGroup);
				bg.Objectives = bg.Objectives;
			}
		}
	}

	private void bgGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
	{
		BG bg = (BG)bgGrid.Rows[e.RowIndex].DataBoundItem;
		if (!_settings.IgnoreBgGridErrors)
		{
			switch (MessageBox.Show(e.Exception.Message + Environment.NewLine + "Change value to NULL (press OK)," + Environment.NewLine + "Ignore those errors from now (Cancel)" + Environment.NewLine + "Battle Groups Grid : " + bgGrid.Columns[e.ColumnIndex].Name + Environment.NewLine + "value  : " + bgGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), "Invalid Battlegroup value", MessageBoxButtons.OKCancel))
			{
			case DialogResult.OK:
				bgGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = null;
				break;
			case DialogResult.Cancel:
				_settings.IgnoreBgGridErrors = true;
				break;
			}
		}
		if (sender == null)
		{
		}
	}

	private void bgGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
	{
		if (bgGrid.SelectedCells.Count > 0)
		{
			uint bgId = (uint)bgGrid.SelectedRows[0].Cells["Id"].Value;
			BG bg = DB.CampaignMap[_settings.SelectedCampaign].BGL.FirstOrDefault((BG fId) => fId.Id == bgId);
			if (bg != null)
			{
				DB.CampaignMap[_settings.SelectedCampaign].DataChanged(DataChangeType.BattleGroup);
			}
		}
	}

	private void bgGrid_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
	{
		if (e.RowIndex < 0 || bgGrid.SelectedCells.Count < 1 || e.Button != MouseButtons.Right)
		{
			return;
		}
		BG bg = (BG)bgGrid.Rows[e.RowIndex].DataBoundItem;
		if (bgGrid.Columns[e.ColumnIndex].DataPropertyName == "Division" && bg.Division != "")
		{
			menu = new ContextMenuStrip();
			foreach (string country in bg.CD.DB.CountryList)
			{
				ToolStripMenuItem item = new ToolStripMenuItem(country);
				item.Checked = bg.DivisionCountry == country;
				menu.Items.Add(item);
			}
			_rightClickedBG = bg;
			menu.ItemClicked += menu_ItemClicked;
			menu.Show(new Point(Cursor.Position.X, Cursor.Position.Y));
		}
		if (bgGrid.Columns[e.ColumnIndex].DataPropertyName == "Pawn" && bg != null)
		{
			PawnEditor pe = new PawnEditor(bg.CD.PawnModelList, bg.Pawn, bg.CD.DB.PawnModelList);
			if (pe.ShowDialog() == DialogResult.OK && !bg.SubstitutePawn(pe.NewPawn))
			{
				bgGrid.Invalidate();
			}
		}
	}

	private void bgGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
	{
		if (e.ListChangedType == ListChangedType.Reset && _selectedBgId != 0)
		{
			_initDone = false;
			BG bg = DB.CampaignMap[_settings.SelectedCampaign].BGL.FirstOrDefault((BG fId) => fId.Id == _selectedBgId);
			BGList bgl = (BGList)bgGrid.DataSource;
			int idx = bgl.IndexOf(bg);
			_selectedBgId = 0u;
			if (idx >= 0)
			{
				bgGrid.CurrentCell = bgGrid.Rows[idx].Cells[0];
				SelectBG(bg.Id);
			}
			_initDone = true;
		}
	}

	private void bgGrid_CellValidated(object sender, DataGridViewCellEventArgs e)
	{
		if (bgGrid.SelectedCells.Count > 0 && (bgGrid.Columns[e.ColumnIndex].DataPropertyName == "Division" || bgGrid.Columns[e.ColumnIndex].DataPropertyName == "Pawn"))
		{
			bgGrid.Invalidate();
		}
	}

	private void BGOnlyCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		if (_initDone)
		{
			ApplyBGFilter();
		}
	}

	private void _countryCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _countryComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _categotyCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _divisionCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _divisionComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _typeCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _typeComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _sideCheckBox_CheckedChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
	}

	private void _sideComboBox_SelectedIndexChanged(object sender, EventArgs e)
	{
		ApplyBGFilter();
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
		this.bgToolStrip = new System.Windows.Forms.ToolStrip();
		this.deckInstancesGrid = new System.Windows.Forms.DataGridView();
		this.deckGrid = new System.Windows.Forms.DataGridView();
		this.bgGrid = new System.Windows.Forms.DataGridView();
		this.bgStatusStrip = new System.Windows.Forms.StatusStrip();
		this._redSideCampaignSummaryLabel = new System.Windows.Forms.ToolStripStatusLabel();
		this._blueSideCampaignSummary = new System.Windows.Forms.ToolStripStatusLabel();
		this._campaignRatioToolStrip = new System.Windows.Forms.ToolStripStatusLabel();
		this._BGOnlyCheckBox = new System.Windows.Forms.CheckBox();
		this._cfgButton = new System.Windows.Forms.Button();
		this.openDatFileDialog = new System.Windows.Forms.OpenFileDialog();
		this.openHashFileDialog = new System.Windows.Forms.OpenFileDialog();
		this._campaignPickComboBox = new System.Windows.Forms.ComboBox();
		this._deckDescriptionLabel = new System.Windows.Forms.Label();
		this.campaignLabel = new System.Windows.Forms.Label();
		this._unitsViewButton = new System.Windows.Forms.Button();
		this.saveNdfButton = new System.Windows.Forms.Button();
		this._countryComboBox = new System.Windows.Forms.ComboBox();
		this._countryCheckBox = new System.Windows.Forms.CheckBox();
		this._categoryCheckBox = new System.Windows.Forms.CheckBox();
		this._categoryComboBox = new System.Windows.Forms.ComboBox();
		this._divisionCheckBox = new System.Windows.Forms.CheckBox();
		this._divisionComboBox = new System.Windows.Forms.ComboBox();
		this._typeCheckBox = new System.Windows.Forms.CheckBox();
		this._typeComboBox = new System.Windows.Forms.ComboBox();
		this._thinkButton = new System.Windows.Forms.Button();
		this._showChangesButton = new System.Windows.Forms.Button();
		this._sideCheckBox = new System.Windows.Forms.CheckBox();
		this._sideComboBox = new System.Windows.Forms.ComboBox();
		this._donateButton = new System.Windows.Forms.Button();
		this._exportCampaignButton = new System.Windows.Forms.Button();
		this._exportEverythingButton = new System.Windows.Forms.Button();
		((System.ComponentModel.ISupportInitialize)this.deckInstancesGrid).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.deckGrid).BeginInit();
		((System.ComponentModel.ISupportInitialize)this.bgGrid).BeginInit();
		this.bgStatusStrip.SuspendLayout();
		base.SuspendLayout();
		this.bgToolStrip.Location = new System.Drawing.Point(0, 0);
		this.bgToolStrip.Name = "bgToolStrip";
		this.bgToolStrip.Size = new System.Drawing.Size(1264, 25);
		this.bgToolStrip.TabIndex = 0;
		this.bgToolStrip.Text = "toolStrip1";
		this.deckInstancesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.deckInstancesGrid.Location = new System.Drawing.Point(0, 34);
		this.deckInstancesGrid.Name = "deckInstancesGrid";
		this.deckInstancesGrid.Size = new System.Drawing.Size(277, 302);
		this.deckInstancesGrid.TabIndex = 7;
		this.deckInstancesGrid.SelectionChanged += new System.EventHandler(deckInstancesGrid_SelectionChanged);
		this.deckGrid.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.deckGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.deckGrid.Location = new System.Drawing.Point(283, 34);
		this.deckGrid.Name = "deckGrid";
		this.deckGrid.Size = new System.Drawing.Size(978, 286);
		this.deckGrid.TabIndex = 8;
		this.deckGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(deckGrid_CellDoubleClick);
		this.deckGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(deckGrid_CellValueChanged);
		this.deckGrid.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(deckGrid_RowsAdded);
		this.deckGrid.SelectionChanged += new System.EventHandler(deckGrid_SelectionChanged);
		this.deckGrid.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(deckGrid_UserDeletingRow);
		this.bgGrid.AllowUserToAddRows = false;
		this.bgGrid.AllowUserToDeleteRows = false;
		this.bgGrid.AllowUserToResizeRows = false;
		this.bgGrid.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		this.bgGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		this.bgGrid.Location = new System.Drawing.Point(0, 369);
		this.bgGrid.MultiSelect = false;
		this.bgGrid.Name = "bgGrid";
		this.bgGrid.Size = new System.Drawing.Size(1261, 338);
		this.bgGrid.TabIndex = 12;
		this.bgGrid.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(bgGrid_CellBeginEdit);
		this.bgGrid.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(bgGrid_CellClick);
		this.bgGrid.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(bgGrid_CellMouseClick);
		this.bgGrid.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(bgGrid_CellValidated);
		this.bgGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(bgGrid_CellValueChanged);
		this.bgGrid.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(bgGrid_DataBindingComplete);
		this.bgGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(bgGrid_DataError);
		this.bgGrid.SelectionChanged += new System.EventHandler(bgGrid_SelectionChanged);
		this.bgStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[3] { this._redSideCampaignSummaryLabel, this._blueSideCampaignSummary, this._campaignRatioToolStrip });
		this.bgStatusStrip.Location = new System.Drawing.Point(0, 710);
		this.bgStatusStrip.Name = "bgStatusStrip";
		this.bgStatusStrip.Size = new System.Drawing.Size(1264, 22);
		this.bgStatusStrip.TabIndex = 13;
		this.bgStatusStrip.Text = "statusStrip1";
		this._redSideCampaignSummaryLabel.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this._redSideCampaignSummaryLabel.ForeColor = System.Drawing.Color.DarkRed;
		this._redSideCampaignSummaryLabel.Name = "_redSideCampaignSummaryLabel";
		this._redSideCampaignSummaryLabel.Size = new System.Drawing.Size(169, 17);
		this._redSideCampaignSummaryLabel.Text = "Red Side Campaign Summary";
		this._blueSideCampaignSummary.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this._blueSideCampaignSummary.ForeColor = System.Drawing.Color.Navy;
		this._blueSideCampaignSummary.Name = "_blueSideCampaignSummary";
		this._blueSideCampaignSummary.Size = new System.Drawing.Size(172, 17);
		this._blueSideCampaignSummary.Text = "Blue Side Campaign Summary";
		this._campaignRatioToolStrip.Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
		this._campaignRatioToolStrip.ForeColor = System.Drawing.Color.Indigo;
		this._campaignRatioToolStrip.Name = "_campaignRatioToolStrip";
		this._campaignRatioToolStrip.Size = new System.Drawing.Size(93, 17);
		this._campaignRatioToolStrip.Text = "Campaign Ratio";
		this._BGOnlyCheckBox.AutoSize = true;
		this._BGOnlyCheckBox.Location = new System.Drawing.Point(2, 346);
		this._BGOnlyCheckBox.Name = "_BGOnlyCheckBox";
		this._BGOnlyCheckBox.Size = new System.Drawing.Size(110, 17);
		this._BGOnlyCheckBox.TabIndex = 9;
		this._BGOnlyCheckBox.Text = "Battle groups only";
		this._BGOnlyCheckBox.UseVisualStyleBackColor = true;
		this._BGOnlyCheckBox.CheckedChanged += new System.EventHandler(BGOnlyCheckBox_CheckedChanged);
		this._cfgButton.Location = new System.Drawing.Point(12, 7);
		this._cfgButton.Name = "_cfgButton";
		this._cfgButton.Size = new System.Drawing.Size(74, 23);
		this._cfgButton.TabIndex = 1;
		this._cfgButton.Text = "Configure...";
		this._cfgButton.UseVisualStyleBackColor = true;
		this._cfgButton.Click += new System.EventHandler(_cfgButton_Click);
		this.openDatFileDialog.Title = "Select NDF_Win.dat file";
		this.openHashFileDialog.Title = "Select ZZ_Win.dat file";
		this._campaignPickComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._campaignPickComboBox.FormattingEnabled = true;
		this._campaignPickComboBox.Items.AddRange(new object[4] { "Busan Pocket", "Pearl of the Orient", "2nd Korean War", "Climb Narodnaya" });
		this._campaignPickComboBox.Location = new System.Drawing.Point(750, 7);
		this._campaignPickComboBox.Name = "_campaignPickComboBox";
		this._campaignPickComboBox.Size = new System.Drawing.Size(219, 21);
		this._campaignPickComboBox.TabIndex = 4;
		this._campaignPickComboBox.SelectedIndexChanged += new System.EventHandler(_campaignPickComboBox_SelectedIndexChanged);
		this._deckDescriptionLabel.AutoSize = true;
		this._deckDescriptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25f, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
		this._deckDescriptionLabel.Location = new System.Drawing.Point(280, 323);
		this._deckDescriptionLabel.Name = "_deckDescriptionLabel";
		this._deckDescriptionLabel.Size = new System.Drawing.Size(153, 13);
		this._deckDescriptionLabel.TabIndex = 9;
		this._deckDescriptionLabel.Text = "selected deck description";
		this.campaignLabel.AutoSize = true;
		this.campaignLabel.Location = new System.Drawing.Point(657, 12);
		this.campaignLabel.Name = "campaignLabel";
		this.campaignLabel.Size = new System.Drawing.Size(87, 13);
		this.campaignLabel.TabIndex = 3;
		this.campaignLabel.Text = "Select Campaign";
		this._unitsViewButton.Location = new System.Drawing.Point(975, 5);
		this._unitsViewButton.Name = "_unitsViewButton";
		this._unitsViewButton.Size = new System.Drawing.Size(114, 23);
		this._unitsViewButton.TabIndex = 5;
		this._unitsViewButton.Text = "Units and Limitations";
		this._unitsViewButton.UseVisualStyleBackColor = true;
		this._unitsViewButton.Click += new System.EventHandler(_unitsViewButton_Click);
		this.saveNdfButton.Enabled = false;
		this.saveNdfButton.Location = new System.Drawing.Point(1095, 5);
		this.saveNdfButton.Name = "saveNdfButton";
		this.saveNdfButton.Size = new System.Drawing.Size(85, 23);
		this.saveNdfButton.TabIndex = 6;
		this.saveNdfButton.Text = "Save Changes";
		this.saveNdfButton.UseVisualStyleBackColor = true;
		this.saveNdfButton.Click += new System.EventHandler(_saveNdfButton_Click);
		this._countryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._countryComboBox.FormattingEnabled = true;
		this._countryComboBox.Location = new System.Drawing.Point(351, 342);
		this._countryComboBox.Name = "_countryComboBox";
		this._countryComboBox.Size = new System.Drawing.Size(121, 21);
		this._countryComboBox.TabIndex = 11;
		this._countryComboBox.SelectedIndexChanged += new System.EventHandler(_countryComboBox_SelectedIndexChanged);
		this._countryCheckBox.AutoSize = true;
		this._countryCheckBox.Location = new System.Drawing.Point(283, 346);
		this._countryCheckBox.Name = "_countryCheckBox";
		this._countryCheckBox.Size = new System.Drawing.Size(62, 17);
		this._countryCheckBox.TabIndex = 10;
		this._countryCheckBox.Text = "Country";
		this._countryCheckBox.UseVisualStyleBackColor = true;
		this._countryCheckBox.CheckedChanged += new System.EventHandler(_countryCheckBox_CheckedChanged);
		this._categoryCheckBox.AutoSize = true;
		this._categoryCheckBox.Location = new System.Drawing.Point(478, 346);
		this._categoryCheckBox.Name = "_categoryCheckBox";
		this._categoryCheckBox.Size = new System.Drawing.Size(68, 17);
		this._categoryCheckBox.TabIndex = 14;
		this._categoryCheckBox.Text = "Category";
		this._categoryCheckBox.UseVisualStyleBackColor = true;
		this._categoryCheckBox.CheckedChanged += new System.EventHandler(_categotyCheckBox_CheckedChanged);
		this._categoryComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._categoryComboBox.FormattingEnabled = true;
		this._categoryComboBox.Location = new System.Drawing.Point(552, 342);
		this._categoryComboBox.Name = "_categoryComboBox";
		this._categoryComboBox.Size = new System.Drawing.Size(121, 21);
		this._categoryComboBox.TabIndex = 15;
		this._categoryComboBox.SelectedIndexChanged += new System.EventHandler(_categoryComboBox_SelectedIndexChanged);
		this._divisionCheckBox.AutoSize = true;
		this._divisionCheckBox.Location = new System.Drawing.Point(679, 346);
		this._divisionCheckBox.Name = "_divisionCheckBox";
		this._divisionCheckBox.Size = new System.Drawing.Size(63, 17);
		this._divisionCheckBox.TabIndex = 16;
		this._divisionCheckBox.Text = "Division";
		this._divisionCheckBox.UseVisualStyleBackColor = true;
		this._divisionCheckBox.CheckedChanged += new System.EventHandler(_divisionCheckBox_CheckedChanged);
		this._divisionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._divisionComboBox.FormattingEnabled = true;
		this._divisionComboBox.Location = new System.Drawing.Point(748, 342);
		this._divisionComboBox.Name = "_divisionComboBox";
		this._divisionComboBox.Size = new System.Drawing.Size(146, 21);
		this._divisionComboBox.TabIndex = 17;
		this._divisionComboBox.SelectedIndexChanged += new System.EventHandler(_divisionComboBox_SelectedIndexChanged);
		this._typeCheckBox.AutoSize = true;
		this._typeCheckBox.Location = new System.Drawing.Point(900, 346);
		this._typeCheckBox.Name = "_typeCheckBox";
		this._typeCheckBox.Size = new System.Drawing.Size(50, 17);
		this._typeCheckBox.TabIndex = 18;
		this._typeCheckBox.Text = "Type";
		this._typeCheckBox.UseVisualStyleBackColor = true;
		this._typeCheckBox.CheckedChanged += new System.EventHandler(_typeCheckBox_CheckedChanged);
		this._typeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._typeComboBox.FormattingEnabled = true;
		this._typeComboBox.Location = new System.Drawing.Point(956, 342);
		this._typeComboBox.Name = "_typeComboBox";
		this._typeComboBox.Size = new System.Drawing.Size(121, 21);
		this._typeComboBox.TabIndex = 19;
		this._typeComboBox.SelectedIndexChanged += new System.EventHandler(_typeComboBox_SelectedIndexChanged);
		this._thinkButton.Enabled = false;
		this._thinkButton.Location = new System.Drawing.Point(92, 7);
		this._thinkButton.Name = "_thinkButton";
		this._thinkButton.Size = new System.Drawing.Size(68, 23);
		this._thinkButton.TabIndex = 20;
		this._thinkButton.Text = "Analyze";
		this._thinkButton.UseVisualStyleBackColor = true;
		this._thinkButton.Click += new System.EventHandler(_thinkButton_Click);
		this._showChangesButton.Location = new System.Drawing.Point(284, 5);
		this._showChangesButton.Name = "_showChangesButton";
		this._showChangesButton.Size = new System.Drawing.Size(93, 23);
		this._showChangesButton.TabIndex = 21;
		this._showChangesButton.Text = "Show changes";
		this._showChangesButton.UseVisualStyleBackColor = true;
		this._showChangesButton.Click += new System.EventHandler(_showChangesButton_Click);
		this._sideCheckBox.AutoSize = true;
		this._sideCheckBox.Location = new System.Drawing.Point(118, 346);
		this._sideCheckBox.Name = "_sideCheckBox";
		this._sideCheckBox.Size = new System.Drawing.Size(47, 17);
		this._sideCheckBox.TabIndex = 22;
		this._sideCheckBox.Text = "Side";
		this._sideCheckBox.UseVisualStyleBackColor = true;
		this._sideCheckBox.CheckedChanged += new System.EventHandler(_sideCheckBox_CheckedChanged);
		this._sideComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		this._sideComboBox.FormattingEnabled = true;
		this._sideComboBox.Items.AddRange(new object[2] { "Computer", "Player" });
		this._sideComboBox.Location = new System.Drawing.Point(171, 342);
		this._sideComboBox.Name = "_sideComboBox";
		this._sideComboBox.Size = new System.Drawing.Size(106, 21);
		this._sideComboBox.TabIndex = 23;
		this._sideComboBox.SelectedIndexChanged += new System.EventHandler(_sideComboBox_SelectedIndexChanged);
		this._donateButton.Location = new System.Drawing.Point(1186, 5);
		this._donateButton.Name = "_donateButton";
		this._donateButton.Size = new System.Drawing.Size(75, 23);
		this._donateButton.TabIndex = 24;
		this._donateButton.Text = "Donate";
		this._donateButton.UseVisualStyleBackColor = true;
		this._donateButton.Click += new System.EventHandler(_donateButton_Click);
		this._exportCampaignButton.Location = new System.Drawing.Point(515, 5);
		this._exportCampaignButton.Name = "_exportCampaignButton";
		this._exportCampaignButton.Size = new System.Drawing.Size(107, 23);
		this._exportCampaignButton.TabIndex = 25;
		this._exportCampaignButton.Text = "Export Campaign...";
		this._exportCampaignButton.UseVisualStyleBackColor = true;
		this._exportCampaignButton.Click += new System.EventHandler(_exportCampaignButton_Click);
		this._exportEverythingButton.Location = new System.Drawing.Point(383, 5);
		this._exportEverythingButton.Name = "_exportEverythingButton";
		this._exportEverythingButton.Size = new System.Drawing.Size(126, 23);
		this._exportEverythingButton.TabIndex = 26;
		this._exportEverythingButton.Text = "Export Everything...";
		this._exportEverythingButton.UseVisualStyleBackColor = true;
		this._exportEverythingButton.Click += new System.EventHandler(_exportEverythingButton_Click);
		base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new System.Drawing.Size(1264, 732);
		base.Controls.Add(this._exportEverythingButton);
		base.Controls.Add(this._exportCampaignButton);
		base.Controls.Add(this._donateButton);
		base.Controls.Add(this._sideComboBox);
		base.Controls.Add(this._sideCheckBox);
		base.Controls.Add(this._showChangesButton);
		base.Controls.Add(this._thinkButton);
		base.Controls.Add(this._typeComboBox);
		base.Controls.Add(this._typeCheckBox);
		base.Controls.Add(this._divisionComboBox);
		base.Controls.Add(this._divisionCheckBox);
		base.Controls.Add(this._categoryComboBox);
		base.Controls.Add(this._categoryCheckBox);
		base.Controls.Add(this._countryCheckBox);
		base.Controls.Add(this._countryComboBox);
		base.Controls.Add(this.saveNdfButton);
		base.Controls.Add(this._unitsViewButton);
		base.Controls.Add(this.campaignLabel);
		base.Controls.Add(this._deckDescriptionLabel);
		base.Controls.Add(this._campaignPickComboBox);
		base.Controls.Add(this._cfgButton);
		base.Controls.Add(this._BGOnlyCheckBox);
		base.Controls.Add(this.bgStatusStrip);
		base.Controls.Add(this.bgGrid);
		base.Controls.Add(this.deckGrid);
		base.Controls.Add(this.deckInstancesGrid);
		base.Controls.Add(this.bgToolStrip);
		base.Name = "BGManagerForm";
		this.Text = "BG Manager";
		base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(_BGManagerForm_FormClosing);
		((System.ComponentModel.ISupportInitialize)this.deckInstancesGrid).EndInit();
		((System.ComponentModel.ISupportInitialize)this.deckGrid).EndInit();
		((System.ComponentModel.ISupportInitialize)this.bgGrid).EndInit();
		this.bgStatusStrip.ResumeLayout(false);
		this.bgStatusStrip.PerformLayout();
		base.ResumeLayout(false);
		base.PerformLayout();
	}
}
