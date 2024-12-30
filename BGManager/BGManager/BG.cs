using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types;
using moddingSuite.Model.Ndfbin.Types.AllTypes;
using moddingSuite.Model.Trad;

namespace BGManager;

public class BG : IComparable, INotifyPropertyChanged
{
	private enum ObjectiveOperationType
	{
		AddZone,
		RemoveZone,
		AddBattleGroup,
		RemoveBattleGroup
	}

	public const string ModelPrefix = "Modele_";

	public const string ModelSuffix = "_CampagneDynamique";

	private BGList _sortOwner = null;

	private NdfObject _bg = null;

	private string _name = "";

	private string _debugName = "";

	private string _bgHint = "";

	private string _country = "";

	private NdfValueWrapper _bgNameHash = null;

	private NdfValueWrapper _bgHintHash = null;

	private string _departureZone = "";

	private string _mimeticType = "";

	private string _bgIcon = null;

	private string _divisionIcon = null;

	private uint _apparenceModel = 0u;

	private string _pm = "";

	private string _bgb = "";

	private string _division = "";

	private int _price = 0;

	private NdfObjectReference _initParams = null;

	private NdfCollection _modules = null;

	private string _objectives = "";

	internal Dictionary<string, List<BG>> _sharedData = new Dictionary<string, List<BG>>();

	internal Dictionary<string, NdfObjectReference> _ndfRefMap = new Dictionary<string, NdfObjectReference>();

	public BGList Owner { get; protected set; }

	public BGList SortOwner
	{
		get
		{
			return (_sortOwner == null) ? Owner : _sortOwner;
		}
		set
		{
			_sortOwner = ((value == null) ? Owner : value);
		}
	}

	public Collection<string> ObjectiveZones { get; protected set; }

	public Collection<BG> ObjectiveBGs { get; protected set; }

	public Collection<string> ObjectiveBGdbNames { get; protected set; }

	public Collection<uint> UsedObjectiveIds { get; protected set; }

	public DataBase.CampaignData CD { get; protected set; }

	public string FullName => $"{Id}:{Name}:{DeckPrice} pts";

	public uint Id => _bg.Id;

	public uint InitId { get; protected set; }

	public string ShortDataBaseName { get; protected set; }

	public bool IsBattleGroup { get; protected set; }

	public bool PresentOnStart { get; protected set; }

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (IsBattleGroup && value != _name && value != null && value.Length > 0 && _bgNameHash != null)
			{
				NDFWrappers.ndfSetHashString(CD.DB.BGHash, _bgNameHash, value);
				CD.DB.DataChanged(DataChangeType.Hash);
				_name = value;
			}
		}
	}

	public string Country
	{
		get
		{
			return _country;
		}
		set
		{
			if (!(_country == value))
			{
				Gauges.Log("BG::Country({0}) {1} ===> {2}", FullName, _country, value);
				_country = value;
				if (!CD.DB.CountryList.Contains(_country))
				{
					_country = "";
					NDFWrappers.ndfSetPropertyValue(_ndfRefMap["BattleGroup"].Instance, "MotherCountry", null);
				}
				else
				{
					NDFWrappers.ndfSetString(CD.NDFScripting.Strings, _ndfRefMap["BattleGroup"].Instance, "MotherCountry", _country);
				}
				CD.DataChanged(DataChangeType.BattleGroup);
			}
		}
	}

	public ConflictSide Side { get; set; }

	public int Price
	{
		get
		{
			return _price;
		}
		set
		{
			if (value != _price)
			{
				if (_sharedData.ContainsKey("DynamicCampaignProducerModule"))
				{
					NdfObject newDCPM = NDFWrappers.ndfCopyInstance(_ndfRefMap["DynamicCampaignProducerModule"].Instance);
					NdfObjectReference newDCPMor = new NdfObjectReference(newDCPM.Class, newDCPM.Id);
					NDFWrappers.ndfSetMapValue(CD.NDFScripting, _modules, "DynamicCampaignProducerModule", newDCPMor);
					_ndfRefMap["DynamicCampaignProducerModule"] = newDCPMor;
					_sharedData.Remove("DynamicCampaignProducerModule");
				}
				_price = value;
				NdfInt32 v = new NdfInt32(_price);
				NDFWrappers.ndfSetPropertyValue(_ndfRefMap["DynamicCampaignProducerModule"].Instance, "Price", v);
				CD.DataChanged(DataChangeType.BattleGroup);
			}
		}
	}

	public string Depiction { get; set; }

	public uint BGBId { get; protected set; }

	public string BGB
	{
		get
		{
			return _bgb;
		}
		set
		{
			if (_bgb == value)
			{
				return;
			}
			if (value != null && CD.BGBMap.Keys.Contains(value) && _ndfRefMap.ContainsKey("BattleGroupBattle") && _ndfRefMap["BattleGroupBattle"] != null)
			{
				_bgb = value;
				BGBId = CD.BGBMap[_bgb].id;
				NdfObjectReference objRef = NDFWrappers.ndfCreateObjectReference(CD.NDFScripting, "TBattleGroupBattleModuleDescriptor", BGBId);
				if (objRef != null)
				{
					_ndfRefMap["BattleGroupBattle"] = objRef;
					NDFWrappers.ndfSetMapValue(CD.NDFScripting, _modules, "BattleGroupBattle", objRef);
					CD.DataChanged(DataChangeType.BattleGroup);
				}
			}
		}
	}

	public string PM
	{
		get
		{
			return _pm;
		}
		set
		{
			if (_pm == value)
			{
				return;
			}
			if (value != null && CD.PawnModuleMap.Keys.Contains(value) && _ndfRefMap.ContainsKey("DynamicCampaignPawnModule") && _ndfRefMap["DynamicCampaignPawnModule"] != null)
			{
				_pm = value;
				NdfObjectReference objRef = NDFWrappers.ndfCreateObjectReference(CD.NDFScripting, "TDynamicCampaignPawnModuleDescriptor", CD.PawnModuleMap[_pm].id);
				if (objRef != null)
				{
					_ndfRefMap["DynamicCampaignPawnModule"] = objRef;
					NDFWrappers.ndfSetMapValue(CD.NDFScripting, _modules, "DynamicCampaignPawnModule", objRef);
					CD.DataChanged(DataChangeType.BattleGroup);
				}
			}
			else
			{
				Gauges.Log("BG::Upgrade() Error - cannot upgrade PM for BG {0}. new PM:{1}", Name, value);
			}
		}
	}

	public Deck LinkedDeck
	{
		get
		{
			if (Import.Length > 0 && CD.DB.DeckMap.ContainsKey(Import))
			{
				return CD.DB.DeckMap[Import];
			}
			return null;
		}
	}

	public uint DeckPrice => (LinkedDeck != null) ? LinkedDeck.Price : 0u;

	public uint ApparenceModel
	{
		get
		{
			return _apparenceModel;
		}
		set
		{
			if (_ndfRefMap.ContainsKey("ApparenceModel") && _ndfRefMap["ApparenceModel"] != null)
			{
				NdfObjectReference objRef = NDFWrappers.ndfCreateObjectReference(CD.NDFScripting, "TBattleGroupApparenceModelModuleDescriptor", value);
				if (objRef != null)
				{
					_apparenceModel = value;
					_ndfRefMap["ApparenceModel"] = objRef;
					NDFWrappers.ndfSetMapValue(CD.NDFScripting, _modules, "ApparenceModel", objRef);
				}
			}
		}
	}

	public string Pawn
	{
		get
		{
			if (!IsBattleGroup)
			{
				return " ";
			}
			string pawnInfo = $"ApparenceModel ID:{_apparenceModel}";
			if (!NDFWrappers.ndfCheckChain(_ndfRefMap["ApparenceModel"].Instance, new List<string> { "Depiction", "DepictionTemplate", "SubDepictions", "0", "Depiction", "DepictionTemplate", "DepictionAlternatives", "0", "MeshDescriptor" }))
			{
				return pawnInfo;
			}
			NdfValueWrapper w = NDFWrappers.ndfGetValueByChain(_ndfRefMap["ApparenceModel"].Instance, new List<string>(new string[8] { "Depiction", "DepictionTemplate", "SubDepictions", "0", "Depiction", "DepictionTemplate", "DepictionAlternatives", "0" }));
			if (w != null && w.Type == NdfType.ObjectReference)
			{
				NdfObjectReference x = (NdfObjectReference)w;
				pawnInfo = NDFWrappers.ndfGetTransTable(x.Instance, CD.NDFScripting, "MeshDescriptor");
				pawnInfo = ((pawnInfo.IndexOf("Modele_") != 0 || pawnInfo.IndexOf("_CampagneDynamique") <= 0) ? " Invalid pawn data" : pawnInfo.Substring("Modele_".Length, pawnInfo.Length - "Modele_".Length - "_CampagneDynamique".Length));
			}
			return pawnInfo;
		}
		set
		{
			if (!IsBattleGroup || _ndfRefMap["DepictionTemplate"] == null || _ndfRefMap["DepictionTemplate"].Instance == null)
			{
				return;
			}
			string errorText = "";
			string targetModel = "Modele_" + value + "_CampagneDynamique";
			NdfObject depiction = _ndfRefMap["DepictionTemplate"].Instance;
			if (_sharedData.ContainsKey("DepictionTemplate"))
			{
				NdfObjectReference objRef = (NdfObjectReference)NDFWrappers.ndfGetValueByChain(_ndfRefMap["ApparenceModel"].Instance, new List<string> { "Depiction", "DepictionTemplate", "SubDepictions", "0", "Depiction" });
				NdfObject newDepiction = NDFWrappers.ndfCopyInstance(objRef.Instance);
				objRef = (NdfObjectReference)NDFWrappers.ndfGetValueByChain(_ndfRefMap["ApparenceModel"].Instance, new List<string> { "Depiction", "DepictionTemplate", "SubDepictions", "0" });
				NDFWrappers.ndfSetPropertyValue(objRef.Instance, "Depiction", new NdfObjectReference(newDepiction.Class, newDepiction.Id));
				NdfObjectReference oldDARef = (NdfObjectReference)NDFWrappers.ndfGetValueByChain(_ndfRefMap["ApparenceModel"].Instance, new List<string> { "Depiction", "DepictionTemplate", "SubDepictions", "0", "Depiction", "DepictionAlternatives", "0" });
				NdfObject newDA = NDFWrappers.ndfCopyInstance(oldDARef.Instance);
				NdfTrans newMesh = NDFWrappers.ndfCreateTransReference(CD.NDFScripting, targetModel);
				NDFWrappers.ndfSetPropertyValue(newDA, "MeshDescriptor", newMesh);
				NdfPropertyValue v2 = newDepiction.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "DepictionAlternatives");
				if (v2 == null || v2.Type != NdfType.List)
				{
					return;
				}
				NdfCollection daList = (NdfCollection)v2.Value;
				foreach (CollectionItemValueHolder item in daList)
				{
					item.Value = new NdfObjectReference(newDA.Class, newDA.Id);
				}
				CD.DataChanged(DataChangeType.BattleGroup);
				return;
			}
			NdfValueWrapper v = NDFWrappers.ndfGetValueByChain(_ndfRefMap["ApparenceModel"].Instance, new List<string> { "Depiction", "DepictionTemplate", "SubDepictions" });
			if (v != null && v.Type == NdfType.List)
			{
				NdfCollection list = (NdfCollection)v;
				if (list.Count > 0)
				{
					v = list[0].Value;
					v = NDFWrappers.ndfGetValueByChain(((NdfObjectReference)v).Instance, new List<string> { "Depiction", "DepictionAlternatives" });
					if (v != null && v.Type == NdfType.List)
					{
						list = (NdfCollection)v;
						foreach (CollectionItemValueHolder item in list)
						{
							NdfObjectReference objRef = (NdfObjectReference)item.Value;
							if (objRef != null && objRef.Instance != null && objRef.Instance.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "MeshDescriptor") != null)
							{
								string extraModel = "";
								foreach (string s in CD.DB.PawnModelList)
								{
									ObservableCollection<NdfTranReference> trans = CD.NDFScripting.Trans;
									Func<NdfTranReference, bool> predicate = (NdfTranReference fValue) => fValue.Value == "Modele_" + s + "_CampagneDynamique";
									if (trans.FirstOrDefault(predicate) == null)
									{
										extraModel = "Modele_" + s + "_CampagneDynamique";
										break;
									}
								}
								NdfTrans transRef = NDFWrappers.ndfCreateTransReference(CD.NDFScripting, targetModel, extraModel);
								if (transRef != null)
								{
									NDFWrappers.ndfSetPropertyValue(objRef.Instance, "MeshDescriptor", transRef);
									CD.DataChanged(DataChangeType.BattleGroup);
									continue;
								}
								errorText = "Failed to create trans reference for " + targetModel;
								break;
							}
							errorText = "MeshDescriptor not found";
							break;
						}
					}
					else
					{
						errorText = "DepictionAlternatives not found";
					}
				}
				else
				{
					errorText = "SubDepictions not found";
				}
			}
			if (errorText.Length > 0)
			{
				MessageBox.Show(errorText, "Error setting Pawn");
			}
		}
	}

	public string Objectives
	{
		get
		{
			if ((ObjectiveZones.Count > 0 || ObjectiveBGs.Count > 0) && _objectives.Length < 1)
			{
				_objectives = $"{ObjectiveZones.Count} zones and {ObjectiveBGs.Count} battlegroups\t\n";
				foreach (string s in ObjectiveZones)
				{
					_objectives += $"Zone: {s},\n";
				}
				foreach (BG bg in ObjectiveBGs)
				{
					_objectives += $"BG: {bg.Country} {bg.Name} \n";
				}
			}
			return _objectives;
		}
		set
		{
			_objectives = "";
			UpdateObjectives();
		}
	}

	public string Export { get; protected set; }

	public string Import { get; protected set; }

	public string DivisionIcon
	{
		get
		{
			return _divisionIcon;
		}
		set
		{
			if (value == _divisionIcon)
			{
				return;
			}
			string v = ((value != null) ? value : "");
			if (_divisionIcon == null)
			{
				if (v.IndexOf("StrategicTexture_Division_") == 0)
				{
					_divisionIcon = v.Substring("StrategicTexture_Division_".Length);
				}
				else
				{
					_divisionIcon = "";
				}
			}
			else if (CD.DivisionIconsList.Contains(v))
			{
				NdfValueWrapper newRef = NDFWrappers.ndfCreateTransReference(CD.NDFScripting, "StrategicTexture_Division_" + v);
				if (newRef != null)
				{
					_divisionIcon = ((v != null) ? v : "");
					NDFWrappers.ndfSetPropertyValue(_ndfRefMap["DynamicCampaignProducerModule"].Instance, "DivisionTexture", newRef);
					CD.DataChanged(DataChangeType.BattleGroup);
				}
			}
			else if (CD.DB.DivisionIconList.Contains(v))
			{
				if (MessageBox.Show($"Replace {_divisionIcon} with {v} ?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
				{
					NdfTranReference tref = CD.NDFScripting.Trans.FirstOrDefault((NdfTranReference fValue) => fValue.Value == "StrategicTexture_Division_" + _divisionIcon);
					if (tref != null)
					{
						tref.Value = "StrategicTexture_Division_" + v;
						CD.DataChanged(DataChangeType.BattleGroup);
						CD.DivisionIconsList.Remove(_divisionIcon);
						CD.DivisionIconsList.Add(v);
						_divisionIcon = v;
					}
				}
			}
			else
			{
				MessageBox.Show("Cannot set this icon - don't have trans table reference");
			}
		}
	}

	public string BGIcon
	{
		get
		{
			return _bgIcon;
		}
		set
		{
			if (value == _bgIcon)
			{
				return;
			}
			string v = ((value != null) ? value : "");
			if (_bgIcon == null)
			{
				if (v.IndexOf("StrategicTexture_BattleGroupLabel_") == 0)
				{
					_bgIcon = v.Substring("StrategicTexture_BattleGroupLabel_".Length);
				}
				else
				{
					_bgIcon = "";
				}
			}
			else if (CD.BGIconsList.Contains(v))
			{
				NdfValueWrapper newRef = NDFWrappers.ndfCreateTransReference(CD.NDFScripting, (v != null && v.Length > 0) ? ("StrategicTexture_BattleGroupLabel_" + v) : null);
				if (newRef != null)
				{
					_bgIcon = ((v == null) ? "" : v);
					NDFWrappers.ndfSetPropertyValue(_ndfRefMap["BattleGroup"].Instance, "TextureIconInfo", newRef);
					CD.DataChanged(DataChangeType.BattleGroup);
				}
			}
			else if (CD.DB.BGIconList.Contains(v))
			{
				if (MessageBox.Show($"Replace {_bgIcon} with {v} ?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
				{
					NdfTranReference tref = CD.NDFScripting.Trans.FirstOrDefault((NdfTranReference fValue) => fValue.Value == "StrategicTexture_BattleGroupLabel_" + _bgIcon);
					if (tref != null)
					{
						tref.Value = "StrategicTexture_BattleGroupLabel_" + v;
						CD.DataChanged(DataChangeType.BattleGroup);
						CD.BGIconsList.Remove(_bgIcon);
						CD.BGIconsList.Add(v);
						_bgIcon = v;
					}
				}
			}
			else
			{
				MessageBox.Show("Cannot set this icon - don't have trans table reference");
			}
		}
	}

	public string Category { get; protected set; }

	public string DivisionCountry
	{
		get
		{
			return NDFWrappers.ndfGetStringByKey("DivisionNationality", _ndfRefMap["DynamicCampaignProducerModule"].Instance);
		}
		set
		{
			string v = value;
			if (!CD.DB.CountryList.Contains(_country))
			{
				v = "";
				NDFWrappers.ndfSetPropertyValue(_ndfRefMap["DynamicCampaignProducerModule"].Instance, "DivisionNationality", null);
			}
			else
			{
				NDFWrappers.ndfSetString(CD.NDFScripting.Strings, _ndfRefMap["DynamicCampaignProducerModule"].Instance, "DivisionNationality", v);
			}
			CD.DataChanged(DataChangeType.BattleGroup);
		}
	}

	public string Division
	{
		get
		{
			return _division;
		}
		set
		{
			string v = ((value == null) ? "" : value);
			if (v == _division)
			{
				return;
			}
			string oldHashValue = NDFWrappers.ndfGetHashStringByKey("DivisionName", _ndfRefMap["DynamicCampaignProducerModule"].Instance, CD.DB.BGHash);
			if (oldHashValue != v)
			{
				NDFWrappers.ndfSetHashString(CD.DB.BGHash, NDFWrappers.ndfGetValueByKey("DivisionName", _ndfRefMap["DynamicCampaignProducerModule"].Instance.PropertyValues), v);
				foreach (BG bg in CD.BGL)
				{
					if (bg.Id != Id && _division == bg.Division)
					{
						bg.Division = v;
					}
				}
				CD.DataChanged(DataChangeType.Hash);
			}
			_division = v;
			OnPropertyChanged("Division");
		}
	}

	public string MimeticType
	{
		get
		{
			return _mimeticType;
		}
		set
		{
			if (_mimeticType == value)
			{
				return;
			}
			_mimeticType = value;
			if (!CD.DB.TypeList.Contains(_mimeticType) || !IsBattleGroup)
			{
				_mimeticType = null;
				NDFWrappers.ndfSetPropertyValue(_ndfRefMap["BattleGroup"].Instance, "MimeticType", null);
				return;
			}
			TradEntry trad = CD.DB.BGHash.FirstOrDefault((TradEntry fName) => fName.Content == _mimeticType);
			NdfLocalisationHash lh = new NdfLocalisationHash(trad.Hash);
			NDFWrappers.ndfSetPropertyValue(_ndfRefMap["BattleGroup"].Instance, "MimeticType", lh);
		}
	}

	public string Departure
	{
		get
		{
			return _departureZone;
		}
		set
		{
			if (_departureZone == value)
			{
				return;
			}
			string s = value;
			if (CD.NDFAlternative.Strings.FirstOrDefault((NdfStringReference fValue) => fValue.Value.Equals(s)) == null || !(_departureZone != s))
			{
				return;
			}
			_departureZone = s;
			NdfPropertyValue v = _initParams.Instance.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "DepartureZone");
			if (v != null && v.Type == NdfType.TableString)
			{
				NdfStringReference ndfSR = CD.NDFAlternative.Strings.FirstOrDefault((NdfStringReference fValue) => fValue.Value == s);
				if (ndfSR != null)
				{
					NdfString ndfString = new NdfString(ndfSR);
					v.Value = ndfString;
					CD.DataChanged(DataChangeType.BattleGroup);
				}
			}
		}
	}

	public string Hint
	{
		get
		{
			return _bgHint;
		}
		set
		{
			if (value != null && _bgHint != value && value.Length > 0 && _bgHintHash != null)
			{
				NDFWrappers.ndfSetHashString(CD.DB.BGHash, _bgHintHash, value);
				CD.DB.DataChanged(DataChangeType.Hash);
				CD.DataChanged(DataChangeType.BattleGroup);
				_bgHint = value;
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public static List<DataGridViewColumn> GenerateDGVColumns(DataBase.CampaignData cd)
	{
		List<DataGridViewColumn> cList = new List<DataGridViewColumn>();
		DataGridViewColumn column = new DataGridViewColumn();
		HashSet<string> cl = cd.DB.CountryList;
		cl.Add("");
		HashSet<string> t = cd.DB.TypeList;
		t.Add("");
		HashSet<string> pm = new HashSet<string>();
		foreach (string s in cd.PawnModuleMap.Keys)
		{
			pm.Add(s);
		}
		HashSet<string> bgb = new HashSet<string>();
		foreach (string s in cd.BGBMap.Keys)
		{
			bgb.Add(s);
		}
		cList.Add(Gauges.CreateColumn("Id", 30f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Id", "Instance ID in TModularUnitDescriptor"));
		cList.Add(Gauges.CreateColumn("Init Id", 30f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "InitId"));
		cList.Add(Gauges.CreateCheckBoxColumn("On map", 30f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "PresentOnStart", "If battle group placed on map at turn 0"));
		cList.Add(Gauges.CreateColumn("Name", 100f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "Name"));
		cList.Add(Gauges.CreateColumn("Ctry", 45f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "Country", cl));
		cList.Add(Gauges.CreateColumn("Side", 45f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Side"));
		cList.Add(Gauges.CreateButtonColumn("Moral Cohesion", 80f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "BGB", "TBattleGroupBattleModuleDescriptor"));
		cList.Add(Gauges.CreateColumn("Health AP", 50f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "PM", pm));
		cList.Add(Gauges.CreateColumn("Price", 30f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "Price"));
		cList.Add(Gauges.CreateColumn("Deck Price", 40f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "DeckPrice"));
		cList.Add(Gauges.CreateButtonColumn("Pawn", 90f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "Pawn", "ApparenceModel chain"));
		cList.Add(Gauges.CreateColumn("Division", 80f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "Division"));
		cList.Add(Gauges.CreateColumn("Type", 80f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "MimeticType", t));
		cList.Add(Gauges.CreateColumn("Division Icon", 90f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "DivisionIcon", cd.DB.DivisionIconList, "DynamicCampaignProducerModule:DivisionTexture"));
		cList.Add(Gauges.CreateColumn("Battle Group Icon", 90f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "BGIcon", cd.DB.BGIconList, "BattleGroup:TextureIconInfo"));
		cList.Add(Gauges.CreateButtonColumn("Objectives", 65f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "Objectives"));
		cList.Add(Gauges.CreateColumn("Departure", 65f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "Departure", cd.DeparturesList));
		cList.Add(Gauges.CreateColumn("Description", 70f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "Hint"));
		return cList;
	}

	public BG(BGList bgList, NdfObject bg, DataBase.CampaignData cd)
	{
		CD = cd;
		InitData(bgList, bg);
		if (_ndfRefMap.Count < 1)
		{
			return;
		}
		IsBattleGroup = _ndfRefMap["BattleGroupUnitList"] != null;
		if (_ndfRefMap.ContainsKey("BattleGroup") && _ndfRefMap["BattleGroup"] != null)
		{
			_bgHintHash = null;
			_bgNameHash = NDFWrappers.ndfGetWrapperByKey(_ndfRefMap["BattleGroup"].Instance, "BattleGroupName", NdfType.LocalisationHash);
			_name = NDFWrappers.ndfGetHashString(_bgNameHash, cd.DB.BGHash);
			if (!IsBattleGroup)
			{
				Name = Name + "(" + ShortDataBaseName + ")";
			}
			_country = NDFWrappers.ndfGetStringByKey("MotherCountry", _ndfRefMap["BattleGroup"].Instance);
			_mimeticType = NDFWrappers.ndfGetHashStringByKey("MimeticType", _ndfRefMap["BattleGroup"].Instance, cd.DB.BGHash);
			_bgHintHash = NDFWrappers.ndfGetWrapperByKey(_ndfRefMap["BattleGroup"].Instance, "TexteHint", NdfType.LocalisationHash);
			if (_bgHintHash == null)
			{
				_bgHintHash = NDFWrappers.ndfGetWrapperByKey(_ndfRefMap["BattleGroup"].Instance, "TitleHint", NdfType.LocalisationHash);
			}
			_bgHint = ((_bgHintHash != null) ? NDFWrappers.ndfGetHashString(_bgHintHash, cd.DB.BGHash) : "");
			BGIcon = NDFWrappers.ndfGetTransTable(_ndfRefMap["BattleGroup"].Instance, cd.NDFScripting, "TextureIconInfo");
		}
		if (_ndfRefMap.ContainsKey("BattleGroupBattle") && _ndfRefMap["BattleGroupBattle"] != null)
		{
			BattleGroupBattle bgb = new BattleGroupBattle(_ndfRefMap["BattleGroupBattle"].Instance);
			cd.BGBMap[bgb.view] = bgb;
			BGBId = bgb.id;
			_bgb = bgb.view;
		}
		if (_ndfRefMap.ContainsKey("BattleGroupUnitList") && _ndfRefMap["BattleGroupUnitList"] != null)
		{
			Import = NDFWrappers.ndfGetTransTable(_ndfRefMap["BattleGroupUnitList"].Instance, cd.NDFScripting, "Deck");
		}
		if (_ndfRefMap.ContainsKey("DynamicCampaignProducerModule") && _ndfRefMap["DynamicCampaignProducerModule"] != null)
		{
			_price = NDFWrappers.ndfGetIntByKey("Price", _ndfRefMap["DynamicCampaignProducerModule"].Instance);
			Category = NDFWrappers.ndfGetHashStringByKey("Category", _ndfRefMap["DynamicCampaignProducerModule"].Instance, cd.DB.BGHash);
			_division = NDFWrappers.ndfGetHashStringByKey("DivisionName", _ndfRefMap["DynamicCampaignProducerModule"].Instance, cd.DB.BGHash);
			DivisionIcon = NDFWrappers.ndfGetTransTable(_ndfRefMap["DynamicCampaignProducerModule"].Instance, cd.NDFScripting, "DivisionTexture");
		}
		if (_ndfRefMap.ContainsKey("DynamicCampaignPawnModule") && _ndfRefMap["DynamicCampaignPawnModule"] != null)
		{
			PawnModule pm = new PawnModule(_ndfRefMap["DynamicCampaignPawnModule"].Instance);
			CD.PawnModuleMap[pm.view] = pm;
			_pm = pm.view;
		}
		if (_ndfRefMap.ContainsKey("ApparenceModel") && _ndfRefMap["ApparenceModel"] != null)
		{
			_apparenceModel = _ndfRefMap["ApparenceModel"].Instance.Id;
			NdfObjectReference objRef = (NdfObjectReference)NDFWrappers.ndfGetWrapperByKey(_ndfRefMap["ApparenceModel"].Instance, "Depiction", NdfType.ObjectReference);
			if (objRef != null)
			{
				objRef = (NdfObjectReference)NDFWrappers.ndfGetWrapperByKey(objRef.Instance, "DepictionTemplate", NdfType.ObjectReference);
				NdfCollection ndfList = (NdfCollection)NDFWrappers.ndfGetWrapperByKey(objRef.Instance, "DepictionAlternatives", NdfType.List);
				if (ndfList != null && ndfList.Count > 0)
				{
					objRef = (NdfObjectReference)ndfList[0].Value;
					if (objRef != null)
					{
						Depiction += NDFWrappers.ndfGetTransTable(objRef.Instance, cd.NDFScripting, "MeshDescriptor");
					}
				}
			}
		}
		Export = ShortDataBaseName;
	}

	internal void ProcessInitData(NdfObject initData)
	{
		_objectives = "";
		ObjectiveZones.Clear();
		ObjectiveBGs.Clear();
		ObjectiveBGdbNames.Clear();
		UsedObjectiveIds.Clear();
		NdfPropertyValue v = initData.PropertyValues.First((NdfPropertyValue Value) => Value.Property.Name == "InitialisationParameters");
		if (v.Type == NdfType.ObjectReference)
		{
			_initParams = (NdfObjectReference)v.Value;
			InitId = _initParams.Instance.Id;
			_departureZone = NDFWrappers.ndfGetStringByKey("DepartureZone", _initParams.Instance);
			PresentOnStart = NDFWrappers.ndfGetStringByKey("ProductionSlot", _initParams.Instance) == "";
			UpdateObjectives();
		}
	}

	internal void CrossLinkBG(BG otherBG)
	{
		if (CompareNdfMap(otherBG, "DynamicCampaignProducerModule"))
		{
			UpdateSharedData(otherBG, "DynamicCampaignProducerModule");
		}
		if (CompareNdfMap(otherBG, "DepictionTemplate"))
		{
			UpdateSharedData(otherBG, "DepictionTemplate");
		}
		foreach (uint i in UsedObjectiveIds)
		{
			if (otherBG.UsedObjectiveIds.Contains(i))
			{
				UpdateSharedData(otherBG, "ObjectivesList");
			}
		}
	}

	private void UpdateSharedData(BG otherBG, string key)
	{
		if (!_sharedData.ContainsKey(key))
		{
			_sharedData.Add(key, new List<BG>());
		}
		if (!_sharedData[key].Contains(otherBG))
		{
			_sharedData[key].Add(otherBG);
		}
	}

	private bool CompareNdfMap(BG otherBG, string key)
	{
		if (_ndfRefMap != null && otherBG._ndfRefMap != null && otherBG._ndfRefMap.ContainsKey(key) && _ndfRefMap.ContainsKey(key) && otherBG._ndfRefMap[key] != null && _ndfRefMap[key] != null && otherBG._ndfRefMap[key].InstanceId == _ndfRefMap[key].InstanceId)
		{
			return true;
		}
		return false;
	}

	private void InitData(BGList bgList, NdfObject bg)
	{
		_bg = bg;
		Side = ConflictSide.Unknown;
		Owner = bgList;
		SortOwner = bgList;
		Export = "";
		Import = "";
		Name = "";
		ObjectiveBGs = new Collection<BG>();
		ObjectiveZones = new Collection<string>();
		UsedObjectiveIds = new Collection<uint>();
		ObjectiveBGdbNames = new Collection<string>();
		_debugName = NDFWrappers.ndfGetStringByKey("ClassNameForDebug", bg);
		ShortDataBaseName = NDFWrappers.ndfGetStringByKey("_ShortDatabaseName", bg);
		NdfPropertyValue pv = bg.PropertyValues.First((NdfPropertyValue Value) => Value.Property.Name == "Modules");
		if (pv != null && pv.Type == NdfType.List && pv.Value != null)
		{
			_modules = (NdfCollection)pv.Value;
			_ndfRefMap["BattleGroup"] = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("BattleGroup", _modules);
			_ndfRefMap["BattleGroupUnitList"] = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("BattleGroupUnitList", _modules);
			_ndfRefMap["BattleGroupBattle"] = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("BattleGroupBattle", _modules);
			_ndfRefMap["DynamicCampaignProducerModule"] = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("DynamicCampaignProducerModule", _modules);
			_ndfRefMap["DynamicCampaignPawnModule"] = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("DynamicCampaignPawnModule", _modules);
			_ndfRefMap["ApparenceModel"] = (NdfObjectReference)NDFWrappers.ndfGetValueFromMap("ApparenceModel", _modules);
			_ndfRefMap["DepictionTemplate"] = (NdfObjectReference)NDFWrappers.ndfGetValueByChain(bg, new List<string> { "Modules", "ApparenceModel", "Depiction", "DepictionTemplate", "SubDepictions", "0", "Depiction" });
		}
	}

	public uint GetDeckIndex()
	{
		if (Import.Length > 0 && CD.DB.DeckMap.ContainsKey(Import))
		{
			Deck deck = CD.DB.DeckMap[Import];
			return deck.Id;
		}
		return 0u;
	}

	public bool Upgrade(BG bg)
	{
		if (bg == null)
		{
			Gauges.Log($"BG [{Name}] cannot be upgraded - not found");
			return false;
		}
		if (LinkedDeck == null || bg.LinkedDeck == null)
		{
			Gauges.Log($"Will not uprgade non-battlegroup object : {Name},{bg.Name}");
			return false;
		}
		if (Id != bg.Id || InitId != bg.InitId || Import != bg.Import || Export != bg.Export)
		{
			Gauges.Log(string.Format("Cannot updgrade BG:{4}\t{0}:{1}{4}\t-->{4}{2}:{3}", Name, LinkedDeck.Description, bg.Name, bg.LinkedDeck.Description, Environment.NewLine));
			return false;
		}
		Gauges.Log("BG::Upgrade() [{0}]===>[{1}]", FullName, bg.FullName);
		Name = bg.Name;
		Hint = bg.Hint;
		DivisionIcon = bg.DivisionIcon;
		BGIcon = bg.BGIcon;
		MimeticType = bg.MimeticType;
		Departure = bg.Departure;
		PM = bg.PM;
		KeyValuePair<string, BattleGroupBattle> bgbPair = CD.BGBMap.FirstOrDefault((KeyValuePair<string, BattleGroupBattle> fCompare) => fCompare.Value.Compare(bg.CD.BGBMap[bg.BGB]));
		if (bgbPair.Value != null && bgbPair.Key != null)
		{
			BGB = bgbPair.Key;
		}
		Country = bg.Country;
		DivisionCountry = bg.DivisionCountry;
		Division = bg.Division;
		LinkedDeck.Upgrade(bg.LinkedDeck);
		return true;
	}

	public void ReplaceObjectives(List<string> checkedObjects, bool replacingZones)
	{
		List<string> newZones = new List<string>();
		IEnumerable<string> remainedZones = checkedObjects.Intersect(replacingZones ? ObjectiveZones : ObjectiveBGdbNames);
		IEnumerable<string> addedZones = checkedObjects.Except(replacingZones ? ObjectiveZones : ObjectiveBGdbNames);
		IEnumerable<string> removedZones = (replacingZones ? ObjectiveZones : ObjectiveBGdbNames).Except(checkedObjects);
		string what = (replacingZones ? "zones" : "battlegroups");
		string message = "Changes\n";
		if (remainedZones.Count() > 0)
		{
			message = message + "Remained " + what + ": ";
		}
		BG bg = null;
		foreach (string s3 in remainedZones)
		{
			bg = (replacingZones ? null : Owner.First((BG fDBName) => fDBName.ShortDataBaseName == s3));
			message += ((bg == null) ? s3 : (bg.Name + "(" + bg.Country + ")"));
			message += " ";
		}
		if (addedZones.Count() > 0)
		{
			message = message + "\nAdded " + what + ": ";
		}
		foreach (string s2 in addedZones)
		{
			bg = (replacingZones ? null : Owner.First((BG fDBName) => fDBName.ShortDataBaseName == s2));
			message += ((bg == null) ? s2 : (bg.Name + "(" + bg.Country + ")"));
			message += " ";
			ChangeObjective(s2, (!replacingZones) ? ObjectiveOperationType.AddBattleGroup : ObjectiveOperationType.AddZone);
		}
		if (removedZones.Count() > 0)
		{
			message = message + "\nRemoved " + what + ": ";
		}
		foreach (string s in removedZones)
		{
			bg = (replacingZones ? null : Owner.First((BG fDBName) => fDBName.ShortDataBaseName == s));
			message += ((bg == null) ? s : (bg.Name + "(" + bg.Country + ")"));
			message += " ";
			ChangeObjective(s, replacingZones ? ObjectiveOperationType.RemoveZone : ObjectiveOperationType.RemoveBattleGroup);
		}
		if (removedZones.Count() > 0 || addedZones.Count() > 0)
		{
			MessageBox.Show(message);
		}
	}

	private void ChangeObjective(string token, ObjectiveOperationType action)
	{
		_objectives = "";
		NdfClass targets = CD.NDFAlternative.Classes.FirstOrDefault((NdfClass name) => name.Name == "TBattleGroupTargetDescriptor");
		NdfPropertyValue propValue = null;
		uint targetId = 0u;
		BG targetBG = null;
		foreach (NdfObject obj2 in targets.Instances)
		{
			if (action == ObjectiveOperationType.AddBattleGroup || action == ObjectiveOperationType.RemoveBattleGroup)
			{
				propValue = obj2.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "BgToFollow");
				if (propValue != null && propValue.Value != null && propValue.Type == NdfType.TransTableReference)
				{
					NdfTrans bgTrans = (NdfTrans)propValue.Value;
					string bgName = NDFWrappers.ndfResolveImport(CD.NDFAlternative, bgTrans.Value.ToString());
					targetBG = Owner.FirstOrDefault((BG fExport) => fExport.Export == bgName);
					if (targetBG != null && token == targetBG.ShortDataBaseName)
					{
						targetId = obj2.Id;
						break;
					}
				}
			}
			else
			{
				propValue = obj2.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "Zone");
				if (propValue != null && propValue.Value != null && propValue.Value.Type == NdfType.TableString && token == propValue.Value.ToString())
				{
					targetId = obj2.Id;
					break;
				}
			}
		}
		if (targetId == 0)
		{
			if (action == ObjectiveOperationType.RemoveBattleGroup || action == ObjectiveOperationType.RemoveZone)
			{
				MessageBox.Show("Invalid remove attempt for [" + token + "]");
			}
			else
			{
				targetBG = Owner.FirstOrDefault((BG fShortDataBaseName) => fShortDataBaseName.ShortDataBaseName == token);
				NdfObject newTarget = CD.NDFAlternative.CreateInstanceOf(targets, isTopLevelInstance: false);
				targetId = newTarget.Id;
				if (action == ObjectiveOperationType.AddBattleGroup && targetBG != null)
				{
					NdfPropertyValue nZone = newTarget.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "Zone");
					NdfValueWrapper v = NDFWrappers.ndfCreateTransReference(CD.NDFAlternative, targetBG.Export);
					if (v == null)
					{
						MessageBox.Show($"Object {targetBG.Name}:{targetBG.Id} cannot be set as objective. Don't have reference in TRANS table", "Error");
					}
					else
					{
						newTarget.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "BgToFollow").Value = v;
						targets.Instances.Add(newTarget);
					}
				}
				else if (action == ObjectiveOperationType.AddZone && token.Length > 0)
				{
					NdfStringReference ndfSR = CD.NDFAlternative.Strings.FirstOrDefault((NdfStringReference fValue) => fValue.Value == token);
					newTarget.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "Zone").Value = new NdfString(ndfSR);
					targets.Instances.Add(newTarget);
				}
			}
		}
		if (targetId == 0)
		{
			return;
		}
		NdfCollection listOfObjList = NDFWrappers.ndfGetCollectionByKey("ObjectivesList", _initParams.Instance.PropertyValues);
		foreach (CollectionItemValueHolder objList in listOfObjList)
		{
			if (objList.Value.Type != NdfType.List)
			{
				continue;
			}
			NdfCollection targetsList = (NdfCollection)objList.Value;
			foreach (CollectionItemValueHolder obj in targetsList)
			{
				if (obj.Value.Type == NdfType.ObjectReference)
				{
					if (action == ObjectiveOperationType.AddZone || action == ObjectiveOperationType.AddBattleGroup)
					{
						NdfObjectReference newRef = new NdfObjectReference(targets, targetId);
						targetsList.Add(new CollectionItemValueHolder(newRef, CD.NDFAlternative));
						return;
					}
					NdfObjectReference objRef = (NdfObjectReference)obj.Value;
					if (objRef.InstanceId == targetId)
					{
						targetsList.Remove(obj);
						return;
					}
				}
			}
		}
	}

	private void UpdateObjectives()
	{
		ObjectiveZones.Clear();
		ObjectiveBGs.Clear();
		ObjectiveBGdbNames.Clear();
		NdfCollection listOfObjList = NDFWrappers.ndfGetCollectionByKey("ObjectivesList", _initParams.Instance.PropertyValues);
		if (listOfObjList != null)
		{
			foreach (CollectionItemValueHolder objList in listOfObjList)
			{
				bool haveObjectives = false;
				if (objList.Value.Type == NdfType.List)
				{
					NdfCollection objectives = (NdfCollection)objList.Value;
					foreach (CollectionItemValueHolder obj in objectives)
					{
						if (obj.Value.Type == NdfType.ObjectReference)
						{
							haveObjectives = true;
							NdfObjectReference targetDescriptorRef = (NdfObjectReference)obj.Value;
							UsedObjectiveIds.Add(targetDescriptorRef.InstanceId);
							NdfPropertyValue propValue = targetDescriptorRef.Instance.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "Zone");
							if (propValue != null && propValue.Value != null && propValue.Value.Type == NdfType.TableString && ObjectiveZones.IndexOf(propValue.Value.ToString()) < 0)
							{
								ObjectiveZones.Add(propValue.Value.ToString());
							}
							propValue = targetDescriptorRef.Instance.PropertyValues.FirstOrDefault((NdfPropertyValue fName) => fName.Property.Name == "BgToFollow");
							if (propValue != null && propValue.Value != null && propValue.Type == NdfType.TransTableReference)
							{
								NdfTrans bgTrans = (NdfTrans)propValue.Value;
								string bgName = NDFWrappers.ndfResolveImport(CD.NDFAlternative, bgTrans.Value.ToString());
								BG targetBG = Owner.FirstOrDefault((BG fExport) => fExport.Export == bgName);
								if (targetBG != null && ObjectiveBGs.IndexOf(targetBG) < 0)
								{
									ObjectiveBGs.Add(targetBG);
									ObjectiveBGdbNames.Add(targetBG.ShortDataBaseName);
								}
							}
						}
					}
				}
				if (haveObjectives)
				{
					Side = ConflictSide.Computer;
				}
			}
			return;
		}
		Side = ConflictSide.Unknown;
	}

	public bool SubstitutePawn(string newPawn)
	{
		bool uniquePawn = true;
		foreach (BG bg in CD.BGL)
		{
			if (bg.Id != Id && bg.Pawn == Pawn)
			{
				uniquePawn = false;
				break;
			}
		}
		if (!uniquePawn)
		{
			string unusedPawn = "";
			foreach (string pawn in CD.PawnModelList)
			{
				bool pawnIsUnused = true;
				foreach (BG bg in CD.BGL)
				{
					if (unusedPawn == bg.Pawn)
					{
						pawnIsUnused = false;
						break;
					}
				}
				if (pawnIsUnused)
				{
					unusedPawn = pawn;
					break;
				}
			}
			if (unusedPawn.Length > 0)
			{
				Pawn = unusedPawn;
				uniquePawn = true;
			}
		}
		NdfTranReference tref = CD.NDFScripting.Trans.FirstOrDefault((NdfTranReference fValue) => fValue.Value == "Modele_" + Pawn + "_CampagneDynamique");
		if (tref != null)
		{
			tref.Value = "Modele_" + newPawn + "_CampagneDynamique";
			CD.DataChanged(DataChangeType.BattleGroup);
			CD.PawnModelList.Remove(Pawn);
			CD.PawnModelList.Add(newPawn);
		}
		return uniquePawn;
	}

	int IComparable.CompareTo(object obj)
	{
		BG bgA = (BG)obj;
		return SortOwner.SortingField.Name switch
		{
			"Id" => Gauges.NumericCompare(Id, bgA.Id), 
			"InitId" => Gauges.NumericCompare(InitId, bgA.InitId), 
			"PresentOnStart" => (PresentOnStart && bgA.PresentOnStart == bgA.PresentOnStart) ? 1 : ((bgA.PresentOnStart != PresentOnStart) ? (-1) : 0), 
			"Name" => Name.CompareTo(bgA.Name), 
			"Country" => Country.CompareTo(bgA.Country), 
			"Price" => Gauges.NumericCompare(Price, bgA.Price), 
			"DeckPrice" => Gauges.NumericCompare(DeckPrice, bgA.DeckPrice), 
			"ApparenceModel" => Gauges.NumericCompare(ApparenceModel, bgA.ApparenceModel), 
			"Side" => (Side > bgA.Side) ? 1 : ((Side < bgA.Side) ? (-1) : bgA.Country.CompareTo(Country)), 
			"Departure" => Departure.CompareTo(bgA.Departure), 
			"Category" => Category.CompareTo(bgA.Category), 
			"Pawn" => Pawn.CompareTo(bgA.Pawn), 
			"Division" => Division.CompareTo(bgA.Division), 
			"MimeticType" => MimeticType.CompareTo(bgA.MimeticType), 
			_ => Gauges.NumericCompare(Id, bgA.Id), 
		};
	}

	public bool Equal(BG bg, ref string diff, bool complete = false)
	{
		string s = "";
		string bgDiff = "";
		int oldLength = diff.Length;
		if (Name != bg.Name)
		{
			diff = diff + $"Name: {Name,-30} ===> {bg.Name}" + Environment.NewLine;
		}
		if (DeckPrice != bg.DeckPrice)
		{
		}
		if ((bg.LinkedDeck == null && bg.LinkedDeck != null) || (bg.LinkedDeck != null && bg.LinkedDeck == null))
		{
			bgDiff = bgDiff + string.Format("Deck: {0,-30} ===> {1}", (LinkedDeck == null) ? "empty" : LinkedDeck.Export, (bg.LinkedDeck == null) ? "empty" : bg.LinkedDeck.Export) + Environment.NewLine;
		}
		if (MimeticType != bg.MimeticType)
		{
			bgDiff = bgDiff + $"Type: {MimeticType,-30} ===> {bg.MimeticType}" + Environment.NewLine;
		}
		if (bg.LinkedDeck != null && bg.LinkedDeck != null && !LinkedDeck.Equal(bg.LinkedDeck, ref s))
		{
			bgDiff += s;
		}
		if (Hint != bg.Hint)
		{
			bgDiff = bgDiff + $"Description:{Environment.NewLine} '{Hint}'{Environment.NewLine}    ===>{Environment.NewLine}{bg.Hint}" + Environment.NewLine;
		}
		if (!complete)
		{
			if (bgDiff.Length > 0 || oldLength != diff.Length)
			{
				if (oldLength == diff.Length)
				{
					bgDiff = $"{Name}{Environment.NewLine}{bgDiff}";
				}
				diff += bgDiff;
				return false;
			}
			return true;
		}
		if (BGIcon != bg.BGIcon)
		{
			bgDiff = bgDiff + $"Icon: {BGIcon,-25} ===> {bg.BGIcon}" + Environment.NewLine;
		}
		if (Division != bg.Division)
		{
			bgDiff = bgDiff + $"Division: {Division,-25} ===> {bg.Division}" + Environment.NewLine;
		}
		if (ApparenceModel != bg.ApparenceModel)
		{
			bgDiff = bgDiff + $"ApparenceModel: {ApparenceModel} ===> {bg.ApparenceModel}" + Environment.NewLine;
		}
		if (DivisionIcon != bg.DivisionIcon)
		{
			bgDiff = bgDiff + $"Dvision Texture: {DivisionIcon} ===> {bg.DivisionIcon}" + Environment.NewLine;
		}
		if (bgDiff.Length > 0 || oldLength != diff.Length)
		{
			if (oldLength == diff.Length)
			{
				bgDiff = $"{Name}{Environment.NewLine}{bgDiff}";
			}
			diff += bgDiff;
			return false;
		}
		return oldLength == diff.Length;
	}

	protected virtual void OnPropertyChanged(string propertyName)
	{
		if (null != this.PropertyChanged)
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
