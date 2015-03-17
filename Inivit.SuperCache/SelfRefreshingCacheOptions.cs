using System;

namespace Inivit.SuperCache
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
