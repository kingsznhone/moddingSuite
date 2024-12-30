using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types.AllTypes;

namespace BGManager;

public class DeckUnit : IComparable
{
	public Deck HostDeck { get; protected set; }

	public CollectionItemValueHolder LimitationRef { get; protected set; }

	public DataBase DB { get; set; }

	protected NdfCollection VetRef { get; set; }

	public Unit Transport { get; protected set; }

	public Unit SuperTransport { get; protected set; }

	public Unit HostUnit { get; protected set; }

	public LimitationPerUnit LPU { get; protected set; }

	public bool NewDeckUnit { get; protected set; }

	public uint LimitationId
	{
		get
		{
			return LPU.Id;
		}
		set
		{
			if (DB.LPUMap.Keys.Contains(value))
			{
				NewDeckUnit = false;
				if (LPU != null && LPU != DB.LPUMap[value])
				{
					LPU.UnregisterDeck(HostDeck);
				}
				LPU = DB.LPUMap[value];
				LPU.RegisterDeck(HostDeck);
				HostUnit = DB.UnitMap[LPU.UnitId];
				NdfObjectReference newLPURef = new NdfObjectReference(DB.Everything.Classes.First((NdfClass fName) => fName.Name == "TLimitationPerUnit"), value);
				LimitationRef.Value = newLPURef;
				if (LPU.TrnId != 0)
				{
					Transport = DB.UnitMap[LPU.TrnId];
				}
				else
				{
					Transport = null;
				}
				if (LPU.STrnId != 0)
				{
					SuperTransport = DB.UnitMap[LPU.STrnId];
				}
				else
				{
					SuperTransport = null;
				}
			}
		}
	}

	public uint Count => LPU.Count;

	public string Name => NewDeckUnit ? "" : HostUnit.Name;

	public string BattleRole => NewDeckUnit ? "" : HostUnit.BattleRole;

	public uint Price => (!NewDeckUnit) ? HostUnit.Price : 0u;

	public string TransportName => NewDeckUnit ? "" : ((Transport != null) ? (Transport.Name + ((SuperTransportName.Length > 0) ? (" + " + SuperTransportName) : "")) : "");

	public string SuperTransportName => NewDeckUnit ? "" : ((SuperTransport != null) ? SuperTransport.Name : "");

	public uint TransportPrice => (!NewDeckUnit) ? ((Transport != null) ? Transport.Price : 0u) : 0u;

	public uint TotalCost
	{
		get
		{
			return (!NewDeckUnit) ? (Count * (Price + TransportPrice)) : 0u;
		}
		protected set
		{
		}
	}

	public Unit.UnitType UType => NewDeckUnit ? Unit.UnitType.Logistics : HostUnit.UType;

	public string Country => NewDeckUnit ? "" : HostUnit.Country;

	public uint Year => (!NewDeckUnit) ? HostUnit.Year : 0u;

	public Allegiance UnitAllegiance => NewDeckUnit ? Allegiance.Unknown : HostUnit.UnitAllegiance;

	public string Veterancy
	{
		get
		{
			return NewDeckUnit ? "" : LPU.Veterancy;
		}
		set
		{
			LPU.Veterancy = value;
		}
	}

	public static List<DataGridViewColumn> GenerateDGVColumns()
	{
		List<DataGridViewColumn> cList = new List<DataGridViewColumn>();
		DataGridViewColumn column = new DataGridViewColumn();
		cList.Add(Gauges.CreateColumn("Id", 70f, DataGridViewColumnSortMode.Automatic, readOnly: false, visible: true, "LimitationId", "Instance ID in TLimitationPerUnit"));
		cList.Add(Gauges.CreateColumn("Unit", 290f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Name"));
		cList.Add(Gauges.CreateColumn("Role", 90f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "BattleRole"));
		cList.Add(Gauges.CreateColumn("Price", 80f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Price"));
		cList.Add(Gauges.CreateColumn("Transport", 170f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "TransportName"));
		cList.Add(Gauges.CreateColumn("Transport Price", 90f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: true, "TransportPrice"));
		cList.Add(Gauges.CreateColumn("Veterancy", 100f, DataGridViewColumnSortMode.NotSortable, readOnly: true, visible: true, "Veterancy"));
		cList.Add(Gauges.CreateColumn("Total Cost", 60f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "TotalCost"));
		cList.Add(Gauges.CreateColumn("Type", 80f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "UType"));
		cList.Add(Gauges.CreateColumn("Country", 60f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Country"));
		cList.Add(Gauges.CreateColumn("Year", 50f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Year"));
		return cList;
	}

	public DeckUnit()
	{
		LimitationId = 0u;
		LimitationRef = null;
		DB = null;
	}

	public DeckUnit(CollectionItemValueHolder limitationRef, Deck hostDeck, DataBase db, bool newDeckUnit = false)
	{
		LimitationRef = limitationRef;
		HostDeck = hostDeck;
		DB = db;
		NdfObjectReference r = (NdfObjectReference)limitationRef.Value;
		LimitationId = r.InstanceId;
		LPU = DB.LPUMap[r.InstanceId];
		LPU.RegisterDeck(HostDeck);
		NewDeckUnit = newDeckUnit;
	}

	public DeckUnit(Deck hostDeck, DataBase db, DeckUnit prototype)
	{
		HostDeck = hostDeck;
		DB = db;
		LimitationPerUnit similarLPU = LimitationPerUnit.FindOrCreateSimilar(DB, prototype.LPU);
		LimitationRef = new CollectionItemValueHolder(null, DB.Everything);
		LimitationId = similarLPU.Id;
		NewDeckUnit = false;
	}

	public int CompareTo(object obj)
	{
		DeckUnit a = (DeckUnit)obj;
		if (HostDeck == null)
		{
			return 0;
		}
		return HostDeck.SortingField.Name switch
		{
			"LimitationId" => Gauges.NumericCompare(LimitationId, a.LimitationId), 
			"Name" => Name.CompareTo(a.Name), 
			"TransportName" => TransportName.CompareTo(a.TransportName), 
			"Country" => Country.CompareTo(a.Country), 
			"Price" => Gauges.NumericCompare(Price, a.Price), 
			"Year" => Gauges.NumericCompare(Year, a.Year), 
			"UType" => Gauges.NumericCompare((uint)UType, (uint)a.UType), 
			"TotalCost" => Gauges.NumericCompare(TotalCost, a.TotalCost), 
			_ => Gauges.NumericCompare(LimitationId, a.LimitationId), 
		};
	}

	public void Update(LimitationPerUnit lpu)
	{
		LPU = lpu;
		HostUnit = DB.UnitMap[LPU.UnitId];
		if (LPU.TrnId != 0)
		{
			Transport = DB.UnitMap[LPU.TrnId];
		}
		if (LPU.STrnId != 0)
		{
			SuperTransport = DB.UnitMap[LPU.STrnId];
		}
	}

	public void Upgrade(DeckUnit du)
	{
		if (LPU.Description != du.LPU.Description)
		{
			Gauges.Log("DeckUnit::Upgrade() [{0}:{1}] ===> [{2}:{3}]", LPU.Id, LPU.Description, du.LPU.Id, du.LPU.Description);
			if (LPU.RegisteredDecks.Count == 1)
			{
				LPU.Upgrade(du.LPU);
				return;
			}
			LimitationPerUnit newLPU = LimitationPerUnit.FindOrCreateSimilar(DB, du.LPU);
			LimitationId = newLPU.Id;
		}
	}
}
