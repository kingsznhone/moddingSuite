using System.ComponentModel;
using System.Linq;
using moddingSuite.Model.Ndfbin;

namespace BGManager;

public class LimitationPerUnitList : SortableBindingList<LimitationPerUnit>
{
	public LimitationPerUnitForm Owner { get; set; }

	public DataBase DB { get; protected set; }

	public LimitationPerUnitList(LimitationPerUnitForm owner, DataBase db, uint unitId)
	{
		DB = db;
		Owner = owner;
		if (DB.LPUList == null || DB.LPUList.Count == 0)
		{
			return;
		}
		foreach (LimitationPerUnit lpu in DB.LPUMap.Values)
		{
			if (lpu.UnitId == unitId)
			{
				base.Items.Add(lpu);
			}
		}
	}

	protected override void RemoveItem(int index)
	{
		LimitationPerUnit lpu = base.Items[index];
		if (DB.LPUMap.ContainsValue(lpu))
		{
			DB.LPUMap.Remove(lpu.Id);
		}
		if (DB.LPUList.Contains(lpu.LimitRef))
		{
			DB.Everything.DeleteInstance(lpu.LimitRef);
		}
		DB.DataChanged();
		base.RemoveItem(index);
	}

	protected override void OnAddingNew(AddingNewEventArgs e)
	{
		if (Owner != null)
		{
			LimitationPerUnit lpu = new LimitationPerUnit(DB);
			lpu.UnitId = Owner.UnitId;
			lpu.TrnId = Owner.TrnId;
			lpu.SetupData(lpu.LimitRef);
			DB.LPUMap.Add(lpu.Id, lpu);
			DB.LPUList.Add(lpu.LimitRef);
			e.NewObject = lpu;
		}
		else
		{
			e.NewObject = null;
		}
		base.OnAddingNew(e);
	}

	public override void CancelNew(int itemIndex)
	{
		NdfClass lpuClass = DB.Everything.Classes.First((NdfClass name) => name.Name == "TLimitationPerUnit");
		LimitationPerUnit lpu = base.Items[itemIndex];
		DB.LPUMap.Remove(lpu.Id);
		DB.LPUList.Remove(lpu.LimitRef);
		base.CancelNew(itemIndex);
	}
}
