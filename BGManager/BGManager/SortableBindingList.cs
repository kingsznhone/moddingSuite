using System.Collections.Generic;
using System.ComponentModel;

namespace BGManager;

public class SortableBindingList<T> : BindingList<T>
{
	private PropertyDescriptor _sortingField = null;

	private ListSortDirection _lsd = ListSortDirection.Ascending;

	private bool _isSorted = false;

	protected override bool SupportsSortingCore => true;

	protected override PropertyDescriptor SortPropertyCore => _sortingField;

	protected override ListSortDirection SortDirectionCore => _lsd;

	protected override bool IsSortedCore => _isSorted;

	public PropertyDescriptor SortingField
	{
		get
		{
			return _sortingField;
		}
		set
		{
			_sortingField = value;
		}
	}

	public ListSortDirection LSD
	{
		get
		{
			return _lsd;
		}
		set
		{
			_lsd = value;
		}
	}

	protected override void ApplySortCore(PropertyDescriptor property, ListSortDirection direction)
	{
		if (base.Items is List<T> items)
		{
			PropertyComparer<T> pc = new PropertyComparer<T>(property, direction);
			LSD = direction;
			SortingField = property;
			_isSorted = true;
			items.Sort(pc);
		}
		OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
	}

	protected override void RemoveSortCore()
	{
		_isSorted = false;
		SortingField = base.SortPropertyCore;
		LSD = base.SortDirectionCore;
		OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
	}
}
