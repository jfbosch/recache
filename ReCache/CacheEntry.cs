using System;

namespace ReCache
{
	public class CacheEntry<TValue>
	{
		internal TValue CachedValue { get; set; }

		internal DateTime TimeLoaded { get; set; }

		/// <summary>
		/// An arbitrary context (meta data) for use by the client of the cache.
		/// </summary>
		public volatile string ClientContext;
	}
}