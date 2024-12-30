using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using moddingSuite.Model.Ndfbin;
using moddingSuite.Model.Ndfbin.Types;
using moddingSuite.Model.Ndfbin.Types.AllTypes;

namespace BGManager;

public class Deck : SortableBindingList<DeckUnit>, IComparable
{
	private NdfObject _deck = null;

	private NdfCollection _unitLimits = null;

	private string _name = "";

	private string _description = "";

	public DataBase DB { get; protected set; }

	public NdfCollection UnitLimits => _unitLimits;

	public uint Id => _deck.Id;

	public Allegiance DeckAllegiance { get; protected set; }

	public string Export => _name;

	public uint Price { get; protected set; }

	public string Description => _description;

	public Deck(NdfObject deck, string name, DataBase db)
	{
		_deck = deck;
		_name = name;
		DB = db;
		_unitLimits = (NdfCollection)NDFWrappers.ndfGetWrapperByKey(_deck, "UnitLimitations", NdfType.List);
		if (_unitLimits == null)
		{
			return;
		}
		DeckAllegiance = Allegiance.Unknown;
		base.Items.Clear();
		foreach (CollectionItemValueHolder limitation in _unitLimits)
		{
			NdfObjectReference r = (NdfObjectReference)limitation.Value;
			DeckUnit deckUnit = new DeckUnit(limitation, this, DB);
			base.Items.Add(deckUnit);
		}
		UpdateDeckInfo();
	}

	public void UpdateDeckInfo()
	{
		uint unitsCost = 0u;
		uint transportCost = 0u;
		uint unitsCount = 0u;
		uint transportsCount = 0u;
		uint maxYear = 0u;
		foreach (DeckUnit deckUnit in base.Items)
		{
			if (DeckAllegiance == Allegiance.Unknown)
			{
				DeckAllegiance = deckUnit.UnitAllegiance;
			}
			else if (DeckAllegiance != deckUnit.UnitAllegiance)
			{
				DeckAllegiance = Allegiance.Mixed;
			}
			unitsCost += deckUnit.Price * deckUnit.Count;
			unitsCount += deckUnit.Count;
			if (maxYear < deckUnit.Year)
			{
				maxYear = deckUnit.Year;
			}
			if (deckUnit.Transport != null && maxYear < deckUnit.Transport.Year)
			{
				maxYear = deckUnit.Transport.Year;
			}
			transportCost += deckUnit.TransportPrice * deckUnit.Count;
			if (deckUnit.Transport != null)
			{
				transportsCount += deckUnit.Count;
			}
		}
		Price = unitsCost + transportCost;
		_description = $"{unitsCount} units, {transportsCount} transports, {Price} total price, {maxYear} max year";
	}

	public static List<DataGridViewColumn> GenerateDGVColumns()
	{
		List<DataGridViewColumn> cList = new List<DataGridViewColumn>();
		DataGridViewColumn column = new DataGridViewColumn();
		cList.Add(Gauges.CreateColumn("Id", 90f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Id", "Instance ID in TWargameNationDeck"));
		cList.Add(Gauges.CreateColumn("Name", 300f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Export"));
		cList.Add(Gauges.CreateColumn("Price", 90f, DataGridViewColumnSortMode.Automatic, readOnly: true, visible: true, "Price"));
		return cList;
	}

	protected override void RemoveItem(int index)
	{
		DeckUnit du = base.Items[index];
		_unitLimits.Remove(du.LimitationRef);
		base.RemoveItem(index);
	}

	protected override void OnAddingNew(AddingNewEventArgs e)
	{
		NdfObjectReference objRef = new NdfObjectReference(DB.Everything.Classes.First((NdfClass name) => name.Name == "TLimitationPerUnit"), DB.LPUMap.Keys.ElementAt(0));
		CollectionItemValueHolder item = new CollectionItemValueHolder(objRef, DB.Everything);
		_unitLimits.Add(item);
		e.NewObject = new DeckUnit(item, this, DB, newDeckUnit: true);
		base.OnAddingNew(e);
	}

	public int CompareTo(object obj)
	{
		Deck a = (Deck)obj;
		return DB.AllDeckList.SortingField.Name switch
		{
			"Id" => Gauges.NumericCompare(Id, a.Id), 
			"Name" => Export.CompareTo(a.Export), 
			"Price" => Gauges.NumericCompare(Price, a.Price), 
			_ => Gauges.NumericCompare(Id, a.Id), 
		};
	}

	public bool Equal(Deck deck, ref string diff)
	{
		int oldLength = diff.Length;
		List<DeckUnit> newDeck = new List<DeckUnit>();
		List<DeckUnit> oldDeck = new List<DeckUnit>();
		foreach (DeckUnit du2 in deck)
		{
			newDeck.Add(du2);
		}
		foreach (DeckUnit du2 in base.Items)
		{
			oldDeck.Add(du2);
		}
		while (oldDeck.Count > 0)
		{
			DeckUnit du = oldDeck[0];
			DeckUnit sameUnitNewDeck = newDeck.FirstOrDefault((DeckUnit fLPU) => fLPU.LimitationId == du.LimitationId);
			if (sameUnitNewDeck != null)
			{
				newDeck.Remove(sameUnitNewDeck);
				string lpuDiff = "";
				if (!du.LPU.Equal(sameUnitNewDeck.LPU, ref lpuDiff))
				{
					diff += lpuDiff;
				}
			}
			else if (newDeck.FirstOrDefault((DeckUnit fUnitName) => fUnitName.LPU.Description == du.LPU.Description) != null)
			{
				sameUnitNewDeck = newDeck.FirstOrDefault((DeckUnit fUnitName) => fUnitName.LPU.Description == du.LPU.Description);
				newDeck.Remove(sameUnitNewDeck);
				string lpuDiff = "";
				if (!du.LPU.Equal(sameUnitNewDeck.LPU, ref lpuDiff))
				{
					diff += lpuDiff;
				}
			}
			else
			{
				diff = diff + " -  " + du.LPU.Description + Environment.NewLine;
			}
			oldDeck.Remove(du);
		}
		foreach (DeckUnit du2 in newDeck)
		{
			diff = diff + " +  " + du2.LPU.Description + Environment.NewLine;
		}
		return oldLength == diff.Length;
	}

	public void Upgrade(Deck deck)
	{
		List<DeckUnit> newDeck = new List<DeckUnit>();
		List<DeckUnit> oldDeck = new List<DeckUnit>();
		foreach (DeckUnit du2 in deck)
		{
			newDeck.Add(du2);
		}
		foreach (DeckUnit du2 in base.Items)
		{
			oldDeck.Add(du2);
		}
		while (oldDeck.Count > 0)
		{
			DeckUnit du = oldDeck[0];
			DeckUnit sameUnitNewDeck = newDeck.FirstOrDefault((DeckUnit fLPU) => fLPU.LimitationId == du.LimitationId);
			if (sameUnitNewDeck != null)
			{
				newDeck.Remove(sameUnitNewDeck);
				du.Upgrade(sameUnitNewDeck);
			}
			else if (newDeck.FirstOrDefault((DeckUnit fUnitName) => fUnitName.LPU.Description == du.LPU.Description) != null)
			{
				sameUnitNewDeck = newDeck.FirstOrDefault((DeckUnit fUnitName) => fUnitName.LPU.Description == du.LPU.Description);
				du.Upgrade(sameUnitNewDeck);
				newDeck.Remove(sameUnitNewDeck);
			}
			else
			{
				Gauges.Log("Deck::Upgrade({0}) removing DeckUnit({1}:{2})", Id, du.LPU.Id, du.LPU.Description);
				du.LPU.UnregisterDeck(this);
				base.Items.Remove(du);
				UnitLimits.Remove(du.LimitationRef);
			}
			oldDeck.Remove(du);
		}
		foreach (DeckUnit du2 in newDeck)
		{
			Gauges.Log("Deck::Upgrade({0}) adding DeckUnit({1}:{2})", Id, du2.LPU.Id, du2.LPU.Description);
			DeckUnit newDU = new DeckUnit(this, DB, du2);
			UnitLimits.Add(newDU.LimitationRef);
			base.Items.Add(newDU);
		}
	}
}
