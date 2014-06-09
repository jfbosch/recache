using System;

namespace Inivit.SuperCache
{
	public class CacheEntry<TValue>
	{
		internal TValue CachedValue { get; set; }
		internal DateTime TimeLoaded { get; set; }
	}
}
