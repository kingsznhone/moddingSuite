using System.Collections.Generic;
using System.ComponentModel;

namespace BGManager;

public class PropertyComparer<T> : IComparer<T>
{
	private PropertyDescriptor property;

	private ListSortDirection sortDirection;

	public PropertyComparer(PropertyDescriptor property, ListSortDirection sortDirection)
	{
		this.property = property;
		this.sortDirection = sortDirection;
	}

	public int Compare(T x, T y)
	{
		if (sortDirection == ListSortDirection.Ascending)
		{
			return Comparer<T>.Default.Compare(x, y);
		}
		return Comparer<T>.Default.Compare(y, x);
	}
}
