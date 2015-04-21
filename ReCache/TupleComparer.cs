using System;
using System.Collections.Generic;

namespace ReCache
{
	public class TupleComparer<TItem1, TItem2>
		: IEqualityComparer<Tuple<TItem1, TItem2>>
	{
		private IEqualityComparer<TItem1> _item1Comparer;
		private IEqualityComparer<TItem2> _item2Comparer;

		public TupleComparer()
		{
		}

		public TupleComparer(
			IEqualityComparer<TItem1> item1Comparer,
			IEqualityComparer<TItem2> item2Comparer)
		{
			_item1Comparer = item1Comparer;
			_item2Comparer = item2Comparer;
		}

		public bool DefaultCompareEquals<T>(T x, T y)
		{
			return EqualityComparer<T>.Default.Equals(x, y);
		}

		public bool Equals(Tuple<TItem1, TItem2> x, Tuple<TItem1, TItem2> y)
		{
			if (ReferenceEquals(null, x) ^ ReferenceEquals(null, y))
				return false;
			if (ReferenceEquals(null, x) && ReferenceEquals(null, y))
				return true;

			Func<TItem1, TItem1, bool> item1sAreEqual = DefaultCompareEquals;
			if (_item1Comparer != null)
				item1sAreEqual = _item1Comparer.Equals;

			Func<TItem2, TItem2, bool> item2sAreEqual = DefaultCompareEquals;
			if (_item2Comparer != null)
				item2sAreEqual = _item2Comparer.Equals;

			return item1sAreEqual(x.Item1, y.Item1) && item2sAreEqual(x.Item2, y.Item2);
		}

		public int GetHashCode(Tuple<TItem1, TItem2> obj)
		{
			if (obj == null)
				return -1;
			int hash = 0;

			if (_item1Comparer != null)
				hash += _item1Comparer.GetHashCode(obj.Item1);
			else
				hash += obj.Item1.GetHashCode();

			if (_item2Comparer != null)
				hash += _item2Comparer.GetHashCode(obj.Item2);
			else
				hash += obj.Item2.GetHashCode();

			return hash;
		}
	}
}