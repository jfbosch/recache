using System;

namespace Inivit.SuperCache
{
	public class CacheOptions
	{
		public enum ThroughputExpectations : byte
		{
			Level1Under50PerSec = 1,
			Level2Under500PerSec = 4,
			Level3Under1000PerSec = 10,
			Level4Under5000PerSec = 20,
			Level5Over5000PerSec = 50,
		}

		/// <summary>
		/// A number that indicates the maximum number of items that should be stored in the cache.
		/// The actual number of items in the cache might temporarily exceed this amount depending on the frequency at which new items are added to the cache, and the value of the FlushInterval option. 
		/// The default is 10000.
		/// </summary>
		public int MaximumCacheSizeIndicator { get; set; }

		/// <summary>
		/// The time cached items will be valid for before they expire.
		/// The default is 1 minute.
		/// </summary>
		public TimeSpan CacheItemExpiry { get; set; }
		
		/// <summary>
		/// The interval at which FlushInvalidatedEntries() will be invoked.
		/// The default is every 10 seconds.
		/// </summary>
		public TimeSpan FlushInterval { get; set; }
		public ThroughputExpectations ExpectedThroughputLevel { get; set; }

		public CacheOptions()
		{
			// Set some logical defaults.
			this.MaximumCacheSizeIndicator = 10000;
			this.CacheItemExpiry = TimeSpan.FromMinutes(1);
			this.FlushInterval = TimeSpan.FromSeconds(10);
			this.ExpectedThroughputLevel = ThroughputExpectations.Level2Under500PerSec;
		}
	}
}
