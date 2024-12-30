using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types.AllTypes;

namespace BGManager;

public class LimitationPerUnit
{
	private string _veterancy = "";

	private uint _id = 0u;

	private uint _unitId = 0u;

	private uint _trnId = 0u;

	private uint _strnId = 0u;

	private uint _count = 0u;

	private NdfCollection _vetRef;

	private List<int> _vet = new List<int>(5);

	private List<Deck> _regDecks = new List<Deck>();

	public NdfObject LimitRef { get; protected set; }

	public bool NewObject { get; protected set; }

	public DataBase DB { get; protected set; }

	public List<Deck> RegisteredDecks => _regDecks;

	public uint Id => _id;

	public string UnitName => DB.UnitMap[UnitId].Name;

	public Unit TheUnit => DB.UnitMap[UnitId];

	public uint UnitId
	{
		get
		{
			return _unitId;
		}
		set
		{
			_unitId = value;
			if (value == 0)
			{
				LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "Descriptor").Value = null;
				return;
			}
			LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "Descriptor").Value = new NdfObjectReference(DB.Everything.Classes.First((NdfClass fName) => fName.Name == "TUniteAuSolDescriptor"), value);
		}
	}

	public string Description
	{
		get
		{
			string s = UnitName;
			if (TrnId != 0)
			{
				s = s + " in " + Transport;
			}
			if (STrnId != 0)
			{
				s = s + " on " + SuperTransport;
			}
			return s + $" ({DB.UnitMap[UnitId].Country}),{Veterancy}";
		}
	}

	public uint TrnId
	{
		get
		{
			return _trnId;
		}
		set
		{
			_trnId = value;
			if (value == 0)
			{
				LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "Transporter").Value = null;
				return;
			}
			LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "Transporter").Value = new NdfObjectReference(DB.Everything.Classes.First((NdfClass fName) => fName.Name == "TUniteAuSolDescriptor"), value);
		}
	}

	public uint STrnId
	{
		get
		{
			return _strnId;
		}
		set
		{
			_strnId = value;
			if (value == 0)
			{
				LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "SuperTransporter").Value = null;
				return;
			}
			LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "SuperTransporter").Value = new NdfObjectReference(DB.Everything.Classes.First((NdfClass fName) => fName.Name == "TUniteAuSolDescriptor"), value);
		}
	}

	public string Transport
	{
		get
		{
			return (_trnId != 0) ? DB.UnitMap[_trnId].Name : " ";
		}
		set
		{
			KeyValuePair<uint, Unit> u = DB.UnitMap.FirstOrDefault((KeyValuePair<uint, Unit> fNameAndCountry) => fNameAndCountry.Value.Name == value && fNameAndCountry.Value.Country == TheUnit.Country);
			if (u.Value == null)
			{
				u = DB.UnitMap.FirstOrDefault((KeyValuePair<uint, Unit> fName) => fName.Value.Name == value);
			}
			if (u.Key >= 0 && TrnId != u.Key)
			{
				TrnId = u.Key;
				DB.DataChanged();
			}
		}
	}

	public string SuperTransport
	{
		get
		{
			return (_strnId != 0) ? DB.UnitMap[_strnId].Name : " ";
		}
		set
		{
			uint id = DB.UnitMap.FirstOrDefault((KeyValuePair<uint, Unit> fName) => fName.Value.Name == value).Key;
			if (id != 0)
			{
				STrnId = id;
			}
		}
	}

	public string Veterancy
	{
		get
		{
			return (_vet.Count == 5) ? $"{_vet[0],-2}/{_vet[1],-2}/{_vet[2],-2}/{_vet[3],-2}/{_vet[4],-2}" : "";
		}
		set
		{
			int i = 0;
			foreach (Match j in Regex.Matches(value, "\\d+"))
			{
				_vet[i] = int.Parse(j.Value);
				_vetRef[i].Value = new NdfInt32(_vet[i]);
				i++;
				if (i > 4)
				{
					break;
				}
			}
			_count = 0u;
			foreach (int u in _vet)
			{
				_count += (uint)u;
			}
			if (NewObject)
			{
				NewObject = false;
			}
			DB.DataChanged();
		}
	}

	public uint Count => _count;

	public static List<DataGridViewColumn> GenerateDGVColumns(DataBase db, string country)
	{
		List<DataGridViewColumn> cList = new List<DataGridViewColumn>();
		DataGridViewColumn column = new DataGridViewColumn();
		cList.Add(Gauges.CreateColumn("Id", 30f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: true, "Id", "Instance in TLimitationPerUnit class"));
		cList.Add(Gauges.CreateColumn("Trn Id", 30f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: true, "TrnId"));
		cList.Add(Gauges.CreateColumn("Transport", 70f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "Transport", (country != null) ? db.TransportSet[country] : db.TransportList, "Choose transport"));
		cList.Add(Gauges.CreateColumn("STrn Id", 30f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: true, "STrnId"));
		cList.Add(Gauges.CreateColumn("SuperTransport", 70f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "SuperTransport", db.SuperTransportList, "Choose barque"));
		cList.Add(Gauges.CreateColumn("Veterancy", 60f, DataGridViewColumnSortMode.NotSortable, readOnly: false, visible: true, "Veterancy"));
		cList.Add(Gauges.CreateColumn("Count", 30f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Count"));
		return cList;
	}

	public static LimitationPerUnit FindOrCreateSimilar(DataBase db, LimitationPerUnit prototype)
	{
		LimitationPerUnit similarLPU = db.LPUMap.Values.FirstOrDefault((LimitationPerUnit fEqual) => fEqual.Description == prototype.Description);
		while (similarLPU == null)
		{
			if ((similarLPU = db.LPUMap.Values.FirstOrDefault((LimitationPerUnit fEqual) => fEqual.UnitName == prototype.UnitName && fEqual.Transport == prototype.Transport && fEqual.SuperTransport == prototype.SuperTransport && fEqual.RegisteredDecks.Count == 0)) != null)
			{
				Gauges.Log("LimitationPerUnit::FindOrCreateSimilar() - found same name, trn and strn LPU {0}:{1}", similarLPU.Id, similarLPU.Description);
				break;
			}
			if ((similarLPU = db.LPUMap.Values.FirstOrDefault((LimitationPerUnit fEqual) => fEqual.UnitName == prototype.UnitName && fEqual.Transport == prototype.Transport && fEqual.RegisteredDecks.Count == 0)) != null)
			{
				Gauges.Log("LimitationPerUnit::FindOrCreateSimilar() - found same name and transport LPU {0}:{1}", similarLPU.Id, similarLPU.Description);
				break;
			}
			if ((similarLPU = db.LPUMap.Values.FirstOrDefault((LimitationPerUnit fEqual) => fEqual.UnitName == prototype.UnitName && fEqual.RegisteredDecks.Count == 0)) != null)
			{
				Gauges.Log("LimitationPerUnit::FindOrCreateSimilar() - found LPU with the same unit name LPU {0}:{1}", similarLPU.Id, similarLPU.Description);
				break;
			}
			if ((similarLPU = db.LPUMap.Values.FirstOrDefault((LimitationPerUnit fEqual) => fEqual.RegisteredDecks.Count == 0)) != null)
			{
				Gauges.Log("LimitationPerUnit::FindOrCreateSimilar() - found unused LPU {0}:{1}", similarLPU.Id, similarLPU.Description);
				break;
			}
			similarLPU = new LimitationPerUnit(db);
			Gauges.Log("LimitationPerUnit::FindOrCreateSimilar() - creating new LPU with Id:{0}", similarLPU.Id);
		}
		similarLPU.Upgrade(prototype);
		if (!db.LPUMap.ContainsValue(similarLPU))
		{
			db.LPUMap.Add(similarLPU.Id, similarLPU);
		}
		return similarLPU;
	}

	public LimitationPerUnit()
	{
		DB = null;
		NewObject = true;
		_id = 0u;
	}

	public LimitationPerUnit(DataBase db)
	{
		DB = db;
		NdfClass lpuClass = DB.Everything.Classes.First((NdfClass name) => name.Name == "TLimitationPerUnit");
		LimitRef = db.Everything.CreateInstanceOf(lpuClass, isTopLevelInstance: false);
		if (_vetRef == null)
		{
			_vetRef = new NdfCollection();
			LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "NbUnitByLevel").Value = _vetRef;
		}
		if (_vetRef.Count != 5)
		{
			_vetRef.Clear();
			for (int i = 0; i < 5; i++)
			{
				_vet.Add(0);
				_vetRef.Add(new CollectionItemValueHolder(new NdfInt32(0), DB.Everything));
			}
		}
		_id = LimitRef.Id;
		NewObject = true;
	}

	public LimitationPerUnit(DataBase db, NdfObject limitRef)
	{
		DB = db;
		SetupData(limitRef);
		NewObject = false;
	}

	public void SetupData(NdfObject limitRef)
	{
		LimitRef = limitRef;
		_id = LimitRef.Id;
		NdfObjectReference unitRef = (NdfObjectReference)NDFWrappers.ndfGetValueByKey("Descriptor", limitRef.PropertyValues);
		NdfObjectReference trnRef = (NdfObjectReference)NDFWrappers.ndfGetValueByKey("Transporter", limitRef.PropertyValues);
		NdfObjectReference strnRef = (NdfObjectReference)NDFWrappers.ndfGetValueByKey("SuperTransporter", limitRef.PropertyValues);
		_vetRef = (NdfCollection)NDFWrappers.ndfGetValueByKey("NbUnitByLevel", limitRef.PropertyValues);
		if (_vetRef == null)
		{
			_vetRef = new NdfCollection();
			LimitRef.PropertyValues.First((NdfPropertyValue fName) => fName.Property.Name == "NbUnitByLevel").Value = _vetRef;
		}
		if (_vetRef.Count != 5)
		{
			_vetRef.Clear();
			for (int j = 0; j < 5; j++)
			{
				_vet.Add(0);
				_vetRef.Add(new CollectionItemValueHolder(new NdfInt32(0), DB.Everything));
			}
		}
		_unitId = unitRef?.InstanceId ?? 0;
		_trnId = trnRef?.InstanceId ?? 0;
		_strnId = strnRef?.InstanceId ?? 0;
		_vet = NDFWrappers.ndfGetIntList(_vetRef);
		if (_vet.Count != 5)
		{
			return;
		}
		_veterancy = $"{_vet[0],-2}/{_vet[1],-2}/{_vet[2],-2}/{_vet[3],-2}/{_vet[4],-2}";
		foreach (int i in _vet)
		{
			_count += (uint)i;
		}
	}

	public void RegisterDeck(Deck deck)
	{
		if (!_regDecks.Contains(deck))
		{
			_regDecks.Add(deck);
		}
	}

	public void UnregisterDeck(Deck deck)
	{
		if (_regDecks.Contains(deck))
		{
			_regDecks.Remove(deck);
		}
	}

	public bool Equal(LimitationPerUnit lpu, ref string diff)
	{
		if (Description == lpu.Description && Veterancy == lpu.Veterancy)
		{
			return true;
		}
		diff = $"    {Description} ===> {lpu.Description}{Environment.NewLine}";
		return false;
	}

	public void Upgrade(LimitationPerUnit prototypeLPU)
	{
		Unit u = DB.FindSimilarUnit(prototypeLPU.DB.UnitMap[prototypeLPU.UnitId]);
		if (u == null)
		{
			Gauges.Log("LimitationPerUnit::Upgrade({0}) ERROR - can't find unit {1}", Id, prototypeLPU.UnitName);
			UnitId = prototypeLPU.UnitId;
		}
		else
		{
			UnitId = u.Id;
		}
		TrnId = ((prototypeLPU.TrnId == 0) ? null : DB.FindSimilarUnit(prototypeLPU.DB.UnitMap[prototypeLPU.TrnId]))?.Id ?? 0;
		STrnId = ((prototypeLPU.STrnId == 0) ? null : DB.FindSimilarUnit(prototypeLPU.DB.UnitMap[prototypeLPU.STrnId]))?.Id ?? 0;
		if (UnitId != prototypeLPU.UnitId || TrnId != prototypeLPU.TrnId || STrnId != prototypeLPU.STrnId)
		{
			Gauges.Log($"LimitationPerUnit::Upgrade(prototype:{prototypeLPU.Description}) WARNING:possible id's change - LPU:{Description}");
		}
		Veterancy = prototypeLPU.Veterancy;
		Gauges.Log("LimitationPerUnit::Upgrade() [Id:{0}] ===> [Id:{1}]", Id, prototypeLPU.Id);
	}
}
