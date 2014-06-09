using System;
using System.Collections.Generic;

namespace Inivit.SuperCache
{
	public class TupleComparer<TItem1, TItem2>
		: IEqualityComparer<Tuple<TItem1, TItem2>>
	{
		public bool Compare<T>(T x, T y)
		{
			return EqualityComparer<T>.Default.Equals(x, y);
		}

		public bool Equals(Tuple<TItem1, TItem2> x, Tuple<TItem1, TItem2> y)
		{
			if (object.ReferenceEquals(null, x) ^ object.ReferenceEquals(null, y))
				return false;
			if (object.ReferenceEquals(null, x) && object.ReferenceEquals(null, y))
				return true;

			if (this.Compare(x.Item1, y.Item1) && this.Compare(x.Item2, y.Item2))
				return true;
			else
				return false;
		}

		public int GetHashCode(Tuple<TItem1, TItem2> obj)
		{
			if (obj == null)
				return -1;
			int hash = 0;
			hash += obj.Item1.GetHashCode();
			hash += obj.Item2.GetHashCode();
			return hash;
		}
	}
}
