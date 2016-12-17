using System;

namespace ReCache
{
	public class CacheEntry<TValue>
	{
		private TValue _cachedValue;
		internal TValue CachedValue
		{
			get
			{
				this.TimeLastAccessed = DateTime.UtcNow;
				return _cachedValue;
			}
			set
			{
				_cachedValue = value;
			}
		}
		internal DateTime TimeLoaded { get; set; }
		internal DateTime TimeLastAccessed { get; set; }

		/// <summary>
		/// An arbitrary context (meta data) for use by the client of the cache.
		/// </summary>
		public string ClientContext;
	}
}