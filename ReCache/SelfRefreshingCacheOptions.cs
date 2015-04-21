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
			StandardCacheOptions = new CacheOptions();
		}
	}
}
