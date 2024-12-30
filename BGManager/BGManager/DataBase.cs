using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using moddingSuite.BL;
using moddingSuite.BL.Ndf;
using moddingSuite.Model.Edata;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types;
using moddingSuite.Model.Trad;

namespace BGManager;

public class DataBase
{
	public class CampaignData
	{
		private DataBase _owner = null;

		private EdataContentFile _ndfscriptingContent = null;

		private EdataContentFile _ndfalternativeContent = null;

		private bool _dataChanged = false;

		private Dictionary<string, PawnModule> _pawnModuleMap = new Dictionary<string, PawnModule>();

		private Dictionary<string, BattleGroupBattle> _bgbMap = new Dictionary<string, BattleGroupBattle>();

		private HashSet<string> _departuresList = new HashSet<string>();

		private HashSet<string> _categoryList = new HashSet<string>();

		private HashSet<string> _divisionList = new HashSet<string>();

		private HashSet<string> _bgIconList = new HashSet<string>();

		private HashSet<string> _pawnModelList = new HashSet<string>();

		private HashSet<string> _divisionIconList = new HashSet<string>();

		public List<string> PlayerCountries = new List<string>();

		public List<string> ComputerCountries = new List<string>();

		public string Name;

		public BGList BGL = null;

		public CampaignStat Statistics { get; protected set; }

		public HashSet<string> DeparturesList => _departuresList;

		public HashSet<string> CategoryList => _categoryList;

		public HashSet<string> DivisionList => _divisionList;

		public HashSet<string> BGIconsList => _bgIconList;

		public HashSet<string> PawnModelList => _pawnModelList;

		public HashSet<string> DivisionIconsList => _divisionIconList;

		public Dictionary<string, PawnModule> PawnModuleMap => _pawnModuleMap;

		public Dictionary<string, BattleGroupBattle> BGBMap => _bgbMap;

		public DataBase DB => _owner;

		public NdfBinary NDFScripting { get; protected set; }

		public NdfBinary NDFAlternative { get; protected set; }

		public CampaignData(string campaignName, BGSettings.CampaignSettings p, DataBase db, Progress progress)
		{
			Name = campaignName;
			_owner = db;
			NdfBinary ndfBin = null;
			NDFWrappers.ndfLoadBinary(db.NDFEdataManager, p.NdfScriptingPath, ref ndfBin, ref _ndfscriptingContent);
			NDFScripting = ndfBin;
			NDFWrappers.ndfLoadBinary(db.NDFEdataManager, p.NdfScriptingForAlternativePath, ref ndfBin, ref _ndfalternativeContent);
			NDFAlternative = ndfBin;
			if (NDFScripting == null || NDFAlternative == null)
			{
				MessageBox.Show($"Cannot open ndf-file {((NDFScripting == null) ? p.NdfScriptingPath : p.NdfScriptingForAlternativePath)} for campaign {campaignName}", "Error");
				return;
			}
			_departuresList.Clear();
			_categoryList.Clear();
			_divisionList.Clear();
			foreach (NdfTranReference line in NDFScripting.Trans)
			{
				if (line.Value.IndexOf("StrategicTexture_BattleGroupLabel_") == 0)
				{
					_bgIconList.Add(line.Value.Substring("StrategicTexture_BattleGroupLabel_".Length));
				}
				else if (line.Value.IndexOf("StrategicTexture_Division_") == 0)
				{
					_divisionIconList.Add(line.Value.Substring("StrategicTexture_Division_".Length));
				}
				if (line.Value.IndexOf("Modele_") == 0 && line.Value.IndexOf("_CampagneDynamique") > 0)
				{
					_pawnModelList.Add(line.Value.Substring("Modele_".Length, line.Value.Length - "Modele_".Length - "_CampagneDynamique".Length));
				}
			}
			BGL = new BGList(this, progress);
			foreach (BG bg in BGL)
			{
				_departuresList.Add(bg.Departure);
				_categoryList.Add(bg.Category);
				_divisionList.Add(bg.Division);
				_owner.TypeList.Add(bg.MimeticType);
				if (bg.Side == ConflictSide.Computer)
				{
					ComputerCountries.Add(bg.Country);
				}
				foreach (BG otherBG in BGL)
				{
					if (otherBG != bg)
					{
						bg.CrossLinkBG(otherBG);
					}
				}
			}
			foreach (BG bg in BGL)
			{
				if (bg.Side == ConflictSide.Unknown)
				{
					if (ComputerCountries.Contains(bg.Country) && !PlayerCountries.Contains(bg.Country))
					{
						bg.Side = ConflictSide.Computer;
						continue;
					}
					PlayerCountries.Add(bg.Country);
					bg.Side = ConflictSide.Player;
				}
			}
			foreach (NdfStringReference s in NDFAlternative.Strings)
			{
				if (s.Value != null && s.Value.Length > 1 && (s.Value.Contains("Naval Sector") || !s.Value.Contains('_')))
				{
					_departuresList.Add(s.Value);
				}
			}
			NdfClass cls = NDFAlternative.Classes.FirstOrDefault((NdfClass name) => name.Name == "TBattleGroupTargetDescriptor");
			if (cls != null)
			{
				foreach (NdfObject instance in cls.Instances)
				{
					NdfPropertyValue propValue = instance.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "Zone");
					if (propValue != null && propValue.Value != null && propValue.Value.Type == NdfType.TableString)
					{
						_departuresList.Add(propValue.Value.ToString());
					}
				}
			}
			Statistics = new CampaignStat(this);
			Statistics.ComputeSides();
		}

		public void DataChanged(DataChangeType dct)
		{
			_dataChanged = true;
			_owner.DataChanged(dct);
		}

		public bool DataChanged()
		{
			return _dataChanged;
		}

		public void Save(NdfbinWriter writer, EdataManager dm)
		{
			if (DataChanged())
			{
				byte[] newFile = writer.Write(NDFScripting, NDFScripting.Header.IsCompressedBody);
				dm.ReplaceFile(_ndfscriptingContent, newFile);
				newFile = writer.Write(NDFAlternative, NDFAlternative.Header.IsCompressedBody);
				dm.ReplaceFile(_ndfalternativeContent, newFile);
				_dataChanged = false;
			}
		}

		public void Upgrade(CampaignData campaignData)
		{
		}
	}

	public Dictionary<string, CampaignData> CampaignMap = new Dictionary<string, CampaignData>();

	private EdataContentFile _everythingContent = null;

	private EdataContentFile _unitHash = null;

	private EdataContentFile _bgHash = null;

	private EdataContentFile _intHash = null;

	private Dictionary<uint, Unit> _unitMap = new Dictionary<uint, Unit>();

	private Dictionary<uint, Unit> _transportMap = new Dictionary<uint, Unit>();

	private Dictionary<uint, Unit> _superTransportMap = new Dictionary<uint, Unit>();

	private Dictionary<string, HashSet<string>> _transportSet = new Dictionary<string, HashSet<string>>();

	private HashSet<string> _countryList = new HashSet<string>();

	private HashSet<string> _transportList = new HashSet<string>();

	private HashSet<string> _superTransportList = new HashSet<string>();

	private HashSet<string> _typeList = new HashSet<string>();

	private HashSet<string> _pawnModelList = new HashSet<string>();

	private HashSet<string> _divisionIconList = new HashSet<string>();

	private HashSet<string> _bgIconList = new HashSet<string>();

	private Dictionary<string, Deck> _deckMap = new Dictionary<string, Deck>();

	private ObservableCollection<NdfObject> _LPUList = null;

	private Dictionary<uint, LimitationPerUnit> _limitMap = new Dictionary<uint, LimitationPerUnit>();

	public NdfBinary Everything { get; protected set; }

	public ObservableCollection<TradEntry> UnitHash => UnitHashManager.Entries;

	public ObservableCollection<TradEntry> BGHash => BGHashManager.Entries;

	public ObservableCollection<TradEntry> InterfaceHash => InterfaceHashManager.Entries;

	public bool Valid { get; protected set; }

	public EdataManager NDFEdataManager { get; protected set; }

	private EdataManager HashEdataManager { get; set; }

    private EdataManager HashEdataManager_BG { get; set; }
    private EdataManager HashEdataManager_I { get; set; }

    private TradManager UnitHashManager { get; set; }

	private TradManager BGHashManager { get; set; }

	private TradManager InterfaceHashManager { get; set; }

	private BGManagerForm Owner { get; set; }

	public uint MinYear { get; protected set; }

	public uint MaxYear { get; protected set; }

	public HashSet<string> CountryList => _countryList;

	public HashSet<string> TransportList => _transportList;

	public HashSet<string> SuperTransportList => _superTransportList;

	public Dictionary<string, HashSet<string>> TransportSet => _transportSet;

	public Dictionary<uint, Unit> UnitMap => _unitMap;

	public HashSet<string> TypeList => _typeList;

	public HashSet<string> PawnModelList => _pawnModelList;

	public HashSet<string> DivisionIconList => _divisionIconList;

	public HashSet<string> BGIconList => _bgIconList;

	public DeckList AllDeckList { get; protected set; }

	public Dictionary<string, Deck> DeckMap => _deckMap;

	public Dictionary<uint, LimitationPerUnit> LPUMap => _limitMap;

	public ObservableCollection<NdfObject> LPUList => _LPUList;

	public BGSettings Settings { get; protected set; }

	public Unit FindSimilarUnit(Unit prototype)
	{
		Unit u = UnitMap.Values.FirstOrDefault((Unit fSame) => fSame.Id == prototype.Id && fSame.Name == prototype.Name);
		if (u == null)
		{
			u = UnitMap.Values.FirstOrDefault((Unit fSame) => fSame.Name == prototype.Name && fSame.Country == prototype.Country);
			if (u == null || u.Role != prototype.Role || u.MoveType != prototype.MoveType || u.UType != prototype.UType || u.UnitAllegiance != prototype.UnitAllegiance)
			{
				Gauges.Log("DataBase::FindSimilarUnit(prototype: {0}) => {1} FAILED", prototype.Description, (u == null) ? "" : u.Description);
				u = UnitMap.Values.FirstOrDefault((Unit fSame) => fSame.USubType == prototype.USubType && fSame.Country == prototype.Country && fSame.MoveType == prototype.MoveType && fSame.BattleRole == prototype.BattleRole);
			}
			Gauges.Log("DataBase::FindSimilarUnit(prototype: {0}:{1}) => {2}:{3}", prototype.Id, prototype.Name, u?.Id ?? 0, (u == null) ? "" : u.Name);
		}
		return u;
	}

	public void RegisterUnitRole(Unit unit, Unit.TransportRole role)
	{
		switch (role)
		{
		case Unit.TransportRole.Transport:
			if (!_transportMap.ContainsKey(unit.Id))
			{
				if (!_transportSet.ContainsKey(unit.Country))
				{
					_transportSet[unit.Country] = new HashSet<string>();
				}
				_transportSet[unit.Country].Add(unit.Name);
				_transportMap.Add(unit.Id, unit);
				_transportList.Add(unit.Name);
			}
			break;
		case Unit.TransportRole.Barque:
			_transportMap.Add(unit.Id, unit);
			_transportList.Add(unit.Name);
			_superTransportList.Add(unit.Name);
			_superTransportMap.Add(unit.Id, unit);
			break;
		}
	}

	public DataBase(BGManagerForm owner, BGSettings settings, Progress progress)
	{
		Cursor.Current = Cursors.WaitCursor;
		Owner = owner;
		NDFEdataManager = new EdataManager(settings.DataFilePath);
		NDFEdataManager.ParseEdataFile();
		HashEdataManager = new EdataManager(settings.DictionaryFilePath);
		HashEdataManager.ParseEdataFile();
        HashEdataManager_BG = new EdataManager(settings.DictionaryFile_BG_Path);
        HashEdataManager_BG.ParseEdataFile();
        HashEdataManager_I = new EdataManager(settings.DictionaryFile_I_Path);
        HashEdataManager_I.ParseEdataFile();
        NdfBinary ndfBin = null;
		NDFWrappers.ndfLoadBinary(NDFEdataManager, settings.EverythingNDFPattern, ref ndfBin, ref _everythingContent);
		Everything = ndfBin;
		Settings = settings;
		LoadTables(settings, progress);
		Valid = Everything != null && UnitHashManager != null && BGHashManager != null && InterfaceHashManager != null && UnitHash != null && BGHash != null && InterfaceHash != null;
		Cursor.Current = Cursors.Default;
	}

	public static bool Validate(BGSettings settings, out string errorText)
	{
		errorText = "";
		if (settings.DataFilePath.Length < 1 || !File.Exists(settings.DataFilePath))
		{
			errorText = "Invalid NDF_Win.dat path";
			return false;
		}
		if (settings.DictionaryFilePath.Length < 1 || !File.Exists(settings.DictionaryFilePath))
		{
			errorText = "Invalid ZZ_Win.dat path";
			return false;
		}
		EdataManager dm = null;
		dm = new EdataManager(settings.DataFilePath);
		dm.ParseEdataFile();
		if (!CheckNDF(dm, settings.EverythingNDFPattern))
		{
			errorText = $"Cannot open {settings.EverythingNDFPattern} in {settings.DataFilePath}";
			return false;
		}
		foreach (KeyValuePair<string, BGSettings.CampaignSettings> campaign in settings.Campaigns)
		{
			if (!CheckNDF(dm, campaign.Value.NdfScriptingPath))
			{
				errorText = $"Cannot find {campaign.Value.NdfScriptingPath} in {settings.DataFilePath} for campaign {campaign.Key}";
				return false;
			}
			if (!CheckNDF(dm, campaign.Value.NdfScriptingForAlternativePath))
			{
				errorText = $"Cannot find {campaign.Value.NdfScriptingForAlternativePath} in {settings.DataFilePath} for campaign {campaign.Key}";
				return false;
			}
		}
		dm = new EdataManager(settings.DictionaryFilePath);
		dm.ParseEdataFile();
		if (!CheckHash(dm, settings.UnitHashPattern))
		{
			errorText = $"Cannot open {settings.UnitHashPattern} in {settings.DictionaryFilePath}";
			return false;
		}
        dm = new EdataManager(settings.DictionaryFile_BG_Path);
        dm.ParseEdataFile();
        if (!CheckHash(dm, settings.BGHashPattern))
		{
			errorText = $"Cannot open {settings.BGHashPattern} in {settings.DictionaryFilePath}";
			return false;
		}
        dm = new EdataManager(settings.DictionaryFile_I_Path);
        dm.ParseEdataFile();
        if (!CheckHash(dm, settings.InterfaceHashPattern))
		{
			errorText = $"Cannot open {settings.InterfaceHashPattern} in {settings.DictionaryFilePath}";
			return false;
		}
		return true;
	}

	public static bool CheckNDF(EdataManager dm, string ndfFilePath)
	{
		EdataContentFile content = null;
		NdfBinary ndfBin = null;
		NDFWrappers.ndfLoadBinary(dm, ndfFilePath, ref ndfBin, ref content);
		return ndfBin != null && content != null;
	}

	public static bool CheckHash(EdataManager dm, string hashPath)
	{
		if (dm.Files.FirstOrDefault((EdataContentFile fPath) => fPath.Path.IndexOf(hashPath) >= 0) == null)
		{
			return false;
		}
		EdataContentFile f = dm.Files.FirstOrDefault((EdataContentFile fPath) => fPath.Path.IndexOf(hashPath) >= 0);
		if (f != null)
		{
			TradManager tm = new TradManager(dm.GetRawData(f));
			if (tm != null)
			{
				return true;
			}
		}
		return false;
	}

	public void DataChanged(DataChangeType dct = DataChangeType.Deck)
	{
		Owner.DataChanged(dct);
	}

	public void SaveNDF()
	{
		Cursor.Current = Cursors.WaitCursor;
		NdfbinWriter writer = new NdfbinWriter();
		byte[] newFile = writer.Write(Everything, Everything.Header.IsCompressedBody);
		NDFEdataManager.ReplaceFile(_everythingContent, newFile);
		foreach (CampaignData cd in CampaignMap.Values)
		{
			cd.Save(writer, NDFEdataManager);
		}
		Cursor.Current = Cursors.Default;
	}

	public void SaveHash()
	{
		Cursor.Current = Cursors.WaitCursor;
		byte[] newFile = UnitHashManager.BuildTradFile();
		HashEdataManager.ReplaceFile(_unitHash, newFile);
		newFile = BGHashManager.BuildTradFile();
		HashEdataManager_BG.ReplaceFile(_bgHash, newFile);
		Cursor.Current = Cursors.Default;
	}

	private void LoadTables(BGSettings settings, Progress progress)
	{
		_unitHash = HashEdataManager.Files.FirstOrDefault((EdataContentFile fPath) => fPath.Path.IndexOf(settings.UnitHashPattern) >= 0);
		UnitHashManager = new TradManager(HashEdataManager.GetRawData(_unitHash));
		_bgHash = HashEdataManager_BG.Files.FirstOrDefault((EdataContentFile fPath) => fPath.Path.IndexOf(settings.BGHashPattern) >= 0);
		if (_bgHash == null)
		{
			_bgHash = HashEdataManager_BG.Files.FirstOrDefault((EdataContentFile fPath) => fPath.Path.IndexOf(settings.FallbackBGHashPattern) >= 0);
		}
		BGHashManager = new TradManager(HashEdataManager_BG.GetRawData(_bgHash));
		_intHash = HashEdataManager_I.Files.FirstOrDefault((EdataContentFile fPath) => fPath.Path.IndexOf(settings.InterfaceHashPattern) >= 0);
		InterfaceHashManager = new TradManager(HashEdataManager_I.GetRawData(_intHash));
		if (UnitHashManager == null || BGHashManager == null || InterfaceHashManager == null)
		{
			MessageBox.Show(string.Format("Cannot open dictionary path {0}", (UnitHashManager == null) ? settings.UnitHashPattern : ((BGHashManager == null) ? (settings.BGHashPattern + " or " + settings.FallbackBGHashPattern) : settings.InterfaceHashPattern)), "Error");
		}
		LoadAllLimitations(progress);
		LoadAllUnits(progress);
		LoadAllDecks(progress);
		_typeList.Clear();
		CampaignMap.Clear();
		progress.Show();
		foreach (KeyValuePair<string, BGSettings.CampaignSettings> campaign in settings.Campaigns)
		{
			CampaignMap.Add(campaign.Key, new CampaignData(campaign.Key, campaign.Value, this, progress));
			foreach (string v in CampaignMap[campaign.Key].BGIconsList)
			{
				_bgIconList.Add(v);
			}
			foreach (string v in CampaignMap[campaign.Key].DivisionIconsList)
			{
				_divisionIconList.Add(v);
			}
			foreach (string pm in CampaignMap[campaign.Key].PawnModelList)
			{
				_pawnModelList.Add(pm);
			}
		}
		_pawnModelList.Add(" ");
		progress.Hide();
	}

	private void LoadAllUnits(Progress progress)
	{
		_unitMap.Clear();
		MinYear = 3000u;
		MaxYear = 0u;
		ObservableCollection<NdfObject> ua = Everything.Classes.First((NdfClass name) => name.Name == "TUniteAuSolDescriptor").Instances;
		ObservableCollection<NdfObject> ud = Everything.Classes.First((NdfClass name) => name.Name == "TUniteDescriptor").Instances;
		List<NdfObject> uAll = new List<NdfObject>();
		foreach (NdfObject unit in ud)
		{
			uAll.Add(unit);
		}
		foreach (NdfObject unit in ua)
		{
			uAll.Add(unit);
		}
		progress.SetJobTitle($"Loading {uAll.Count} units...");
		progress.Show();
		progress.UpdateProgress(0, uAll.Count);
		_transportList.Clear();
		_transportMap.Clear();
		_superTransportList.Clear();
		_superTransportMap.Clear();
		_transportList.Add(" ");
		_superTransportList.Add(" ");
		int i = 0;
		foreach (NdfObject unit in uAll)
		{
			progress.UpdateProgress(i++, uAll.Count);
			Unit u = new Unit(this, unit);
			_unitMap.Add(unit.Id, u);
			if (u.Country.Length > 0)
			{
				_countryList.Add(u.Country);
			}
			if (u.Year != 0 && MinYear > u.Year)
			{
				MinYear = u.Year;
			}
			if (MaxYear < u.Year)
			{
				MaxYear = u.Year;
			}
		}
		foreach (Unit u in _transportMap.Values)
		{
			if (!TransportSet.Keys.Contains(u.Country))
			{
				TransportSet.Add(u.Country, new HashSet<string>());
				TransportSet[u.Country].Add(" ");
			}
			TransportSet[u.Country].Add(u.Name);
		}
		progress.Hide();
	}

	private void LoadAllDecks(Progress progress)
	{
		_deckMap.Clear();
		AllDeckList = new DeckList();
		ObservableCollection<NdfObject> deckInstances = Everything.Classes.First((NdfClass name) => name.Name == "TWargameNationDeck").Instances;
		progress.SetJobTitle($"Loading {deckInstances.Count} decks...");
		progress.Show();
		progress.UpdateProgress(0, deckInstances.Count);
		int i = 0;
		foreach (NdfObject deck in deckInstances)
		{
			progress.UpdateProgress(i, deckInstances.Count);
			string deckName = NDFWrappers.ndfResolveExport(Everything, deck.Id);
			Deck d = new Deck(deck, deckName, this);
			_deckMap.Add(deckName, d);
			i++;
			AllDeckList.Add(d);
		}
		progress.Hide();
	}

	private void LoadAllLimitations(Progress progress)
	{
		_limitMap.Clear();
		_LPUList = Everything.Classes.First((NdfClass name) => name.Name == "TLimitationPerUnit").Instances;
		progress.SetJobTitle($"Loading {_LPUList.Count} limitations...");
		progress.Show();
		progress.UpdateProgress(0, _LPUList.Count);
		int i = 0;
		foreach (NdfObject limit in _LPUList)
		{
			progress.UpdateProgress(i, _LPUList.Count);
			LimitationPerUnit lpu = new LimitationPerUnit(this, limit);
			LPUMap.Add(lpu.Id, lpu);
			i++;
		}
		progress.Hide();
	}
}
