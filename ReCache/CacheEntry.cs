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

		public string ClientContext;
	}
}