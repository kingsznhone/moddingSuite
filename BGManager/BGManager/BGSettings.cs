using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace BGManager;

public class BGSettings
{
	public class CampaignSettings
	{
		public string NdfScriptingPath;

		public string NdfScriptingForAlternativePath;

		public uint PlayerTeamPointsIndex;

		public CampaignSettings(string ndfScriptingPath, string ndfScriptingForAlternativePath, uint playerTeamPointsIndex)
		{
			NdfScriptingPath = ndfScriptingPath;
			NdfScriptingForAlternativePath = ndfScriptingForAlternativePath;
			PlayerTeamPointsIndex = playerTeamPointsIndex;
		}
	}

	private string _NDF_WinFilePath = "";

	private string _DictionaryFilePath = "";

	private string _DictionaryFile_BG_Path = "";

	private string _DictionaryFile_I_Path = "";

	private string _EverythingNDFPattern = "everything";

	private string _UnitHashPattern = "us\\localisation\\unit";

	private string _IntHashPattern = "us\\localisation\\interface_outgame";

	private string _BGHashPattern = "us\\localisation\\battlegroup";

	private string _FallbackBGHashPattern = "dev\\localisation\\battlegroup";

	private bool _showAllBG = true;

	private bool _keepLogFiles = false;

	private bool _fullSummary = false;

	private bool _exportAllPawns = true;

	private string _SelectedCampaign = "Climb Narodnaya";

	private Dictionary<string, CampaignSettings> _campaigns = new Dictionary<string, CampaignSettings>();

	[XmlIgnore]
	public Dictionary<string, CampaignSettings> Campaigns => _campaigns;

	public string DataFilePath
	{
		get
		{
			return _NDF_WinFilePath;
		}
		set
		{
			_NDF_WinFilePath = value;
		}
	}

	public string DictionaryFilePath
	{
		get
		{
			return _DictionaryFilePath;
		}
		set
		{
			_DictionaryFilePath = value;
		}
	}

    public string DictionaryFile_BG_Path 
	{ 
		get => _DictionaryFile_BG_Path; 
		set => _DictionaryFile_BG_Path = value; 
	}
    public string DictionaryFile_I_Path
    {
        get => _DictionaryFile_I_Path;
        set => _DictionaryFile_I_Path = value;
    }

    public string EverythingNDFPattern
	{
		get
		{
			return _EverythingNDFPattern;
		}
		set
		{
			_EverythingNDFPattern = value;
		}
	}

	public string UnitHashPattern
	{
		get
		{
			return _UnitHashPattern;
		}
		set
		{
			_UnitHashPattern = value;
		}
	}

	public string BGHashPattern
	{
		get
		{
			return _BGHashPattern;
		}
		set
		{
			_BGHashPattern = value;
		}
	}

	public string FallbackBGHashPattern
	{
		get
		{
			return _FallbackBGHashPattern;
		}
		set
		{
			_FallbackBGHashPattern = value;
		}
	}

	public string InterfaceHashPattern
	{
		get
		{
			return _IntHashPattern;
		}
		set
		{
			_IntHashPattern = value;
		}
	}

	public bool ShowAllBG
	{
		get
		{
			return _showAllBG;
		}
		set
		{
			_showAllBG = value;
		}
	}

	public bool ExportAllPawns
	{
		get
		{
			return _exportAllPawns;
		}
		set
		{
			_exportAllPawns = value;
		}
	}

	public string SelectedCampaign
	{
		get
		{
			return _SelectedCampaign;
		}
		set
		{
			_SelectedCampaign = value;
		}
	}

	public bool IgnoreBgGridErrors { get; set; }

	public bool FullSummary
	{
		get
		{
			return _fullSummary;
		}
		set
		{
			_fullSummary = value;
		}
	}

	public bool KeepLogFiles
	{
		get
		{
			return _keepLogFiles;
		}
		set
		{
			_keepLogFiles = value;
		}
	}


    public BGSettings()
	{
		_campaigns.Add("Busan Pocket", new CampaignSettings("patchable\\scenario\\campdyn_pusan_pocket\\ndfscripting", "patchable\\scenario\\campdyn_pusan_pocket\\ndfscriptingforalter", 1u));
		_campaigns.Add("Pearl of the Orient", new CampaignSettings("patchable\\scenario\\campdyn_crown_jewel\\ndfscripting", "patchable\\scenario\\campdyn_crown_jewel\\ndfscriptingforalter", 1u));
		_campaigns.Add("2nd Korean War", new CampaignSettings("patchable\\scenario\\campdyn_2nd_korean_war\\ndfscripting", "patchable\\scenario\\campdyn_2nd_korean_war\\ndfscriptingforalter", 0u));
		_campaigns.Add("Climb Narodnaya", new CampaignSettings("patchable\\scenario\\campdyn_climbnarodnaia\\ndfscripting", "patchable\\scenario\\campdyn_climbnarodnaia\\ndfscriptingforalter", 1u));
		_campaigns.Add("Bear vs Dragon", new CampaignSettings("patchable\\scenario\\campdyn_proto1\\ndfscripting", "patchable\\scenario\\campdyn_proto1\\ndfscriptingforalter", 1u));
		_campaigns.Add("Debug", new CampaignSettings("patchable\\scenario\\campdyn_debug\\ndfscripting", "patchable\\scenario\\campdyn_debug\\ndfscriptingforalter", 1u));
	}

	public BGSettings(BGSettings settings)
	{
		SelectedCampaign = settings.SelectedCampaign;
		UnitHashPattern = settings.UnitHashPattern;
		BGHashPattern = settings.BGHashPattern;
		EverythingNDFPattern = settings.EverythingNDFPattern;
		InterfaceHashPattern = settings.InterfaceHashPattern;
		_campaigns = settings.Campaigns;
		IgnoreBgGridErrors = settings.IgnoreBgGridErrors;
	}

	public void LoadCampaignList(ref Collection<string> items)
	{
		items.Clear();
		foreach (string key in _campaigns.Keys)
		{
			items.Add(key);
		}
	}
}
