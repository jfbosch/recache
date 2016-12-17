using System;

namespace ReCache
{
	public class SelfRefreshingCacheOptions
	{
		public CacheOptions StandardCacheOptions { get; set; }
		public TimeSpan RefreshInterval { get; set; }

		public SelfRefreshingCacheOptions()
		{
			// Set some logical defaults.
			this.StandardCacheOptions = new CacheOptions();
			this.RefreshInterval = TimeSpan.FromMinutes(5);
		}
	}
}
