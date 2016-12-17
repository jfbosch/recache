using System;
using System.Threading;

namespace ReCache
{
	public class CacheOptions
	{
		public string CacheName { get; set; }

		/// <summary>
		/// This option determines how the cache behaves when new requests come in on threads for a key that is already being fetched by the loader func.
		/// When specified, an exception is thrown on the secondary threads requesting the same key when the wait period times out, letting the client deal with the fact that the cache is not yet warm.
		/// When set to TimeSpan.MaxValue, it will block the new thread indefinitely, while waiting for the active loader func call for the key to either return or fault.
		/// A value of TimeSpan.Zero will cause the exception to be thrown immediately.
		/// It defaults to 200 ms.
		/// </summary>
		private TimeSpan _circuitBreakerTimeoutForAdditionalThreadsPerKey;
		public TimeSpan CircuitBreakerTimeoutForAdditionalThreadsPerKey
		{
			get
			{
				return _circuitBreakerTimeoutForAdditionalThreadsPerKey;
			}
			set
			{
				// Map TimeSpan.MaxValue to -1 ms because that is what Task.WaitAll expects for infinite timeout.
				if (value == TimeSpan.MaxValue)
					_circuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.FromMilliseconds(Timeout.Infinite);
				else
					_circuitBreakerTimeoutForAdditionalThreadsPerKey = value;
			}
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
		/// If set to true, will check if the cached value implements IDisposable, and if so,
		/// will call .Dispose() on values that are invalidated or flushed.  The default is true.
		/// </summary>
		public bool DisposeExpiredValuesIfDisposable { get; set; }

		/// <summary>
		/// The interval at which FlushInvalidatedEntries() will be invoked.
		/// The default is every 10 seconds.
		/// </summary>
		public TimeSpan FlushInterval { get; set; }

		/// <summary>
		/// The time after which a loader func that has not yet
		/// been completed will be aborted. If the loader func is aborted,
		/// an exception will be thrown.
		/// </summary>
		public TimeSpan LoaderFuncTimeout { get; set; }

		public CacheOptions()
		{
			// Set some logical defaults.
			this.CacheName = "(NotSet)";
			this.MaximumCacheSizeIndicator = 10000;
			this.CacheItemExpiry = TimeSpan.FromSeconds(60);
			this.FlushInterval = TimeSpan.FromSeconds(120);
			this.LoaderFuncTimeout = TimeSpan.FromSeconds(60);
			this.CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.FromMilliseconds(2000);
		}
	}
}
