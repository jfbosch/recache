using System.Collections.Generic;
using System.Linq;

namespace Inivit.SuperCache
{
	public class EnumerableStringComparer : IEqualityComparer<IEnumerable<string>>
	{
		public bool Equals(IEnumerable<string> x, IEnumerable<string> y)
		{
			if (object.ReferenceEquals(null, x) ^ object.ReferenceEquals(null, y))
				return false;
			if (object.ReferenceEquals(null, x) && object.ReferenceEquals(null, y))
				return true;

			string xString = string.Concat(x.ToArray());
			string yString = string.Concat(y.ToArray());
			return xString == yString;
		}

		public int GetHashCode(IEnumerable<string> obj)
		{
			if (obj == null)
				return -1;
			if (obj.Count() == 0)
				return 0;

			int hash = 0;
			foreach (var entry in obj)
				hash += entry.GetHashCode();
			return hash;
		}

	}
}
