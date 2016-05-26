using ReCache;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace ReCache
{
	/* Read the following link and understand how ConcurrentDictionary works before modifying this class.
	 * http://arbel.net/2013/02/03/best-practices-for-using-concurrentdictionary/
	 */

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public class Cache<TKey, TValue> : ICache<TKey, TValue>
	{
		private bool _isDisposed = false;
		private readonly object _disposeLock = new object();

		private readonly ConcurrentDictionary<TKey, KeyGate<TKey>> _keyGates;
		private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cachedEntries;
		private CacheOptions _options;
		private Timer _flushTimer;

		/// <summary>
		/// The function to use for retreaving the entry if it is not yet in the cache.
		/// </summary>
		public Func<TKey, Task<TValue>> LoaderFunction { get; set; }

		/// <summary>
		/// Returns the number of items in the cache by enumerating them (non-locking).
		/// </summary>
		public int Count { get { return this.Items.Count(); } }

		public IEnumerable<KeyValuePair<TKey, TValue>> Items { get { return _cachedEntries.Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value.CachedValue)); } }

		public Cache(
			CacheOptions options)
			: this(options, null)
		{
		}

		public Cache(
			CacheOptions options,
			Func<TKey, Task<TValue>> loaderFunction)
		{
			this.SetOptions(options);

			LoaderFunction = loaderFunction;
			_keyGates = new ConcurrentDictionary<TKey, KeyGate<TKey>>();
			_cachedEntries = new ConcurrentDictionary<TKey, CacheEntry<TValue>>();
			this.InitializeFlushTimer();
		}

		public Cache(
			IEqualityComparer<TKey> comparer,
			CacheOptions options)
			: this(comparer, options, null)
		{
		}

		public Cache(
			IEqualityComparer<TKey> comparer,
			CacheOptions options,
			Func<TKey, Task<TValue>> loaderFunction)
			: this(options, loaderFunction)
		{
			if (comparer == null)
				throw new ArgumentNullException(nameof(comparer));

			_keyGates = new ConcurrentDictionary<TKey, KeyGate<TKey>>();
			_cachedEntries = new ConcurrentDictionary<TKey, CacheEntry<TValue>>(comparer);
			this.InitializeFlushTimer();
		}

		private void InitializeFlushTimer()
		{
			if (_flushTimer == null)
			{
				_flushTimer = new Timer(_options.FlushInterval.TotalMilliseconds);
				_flushTimer.Elapsed += (sender, eventArgs) =>
				{
					_flushTimer.Stop();
					try
					{
						this.FlushInvalidatedEntries();
					}
					finally
					{
						_flushTimer.Start();
					}
				};
				_flushTimer.Start();
			}
			else
			{
				// The timer was already started by the first constructor, so just stop and restart it,
				// as we have instantiated the dictionary again in the second constructor.
				_flushTimer.Stop();
				_flushTimer.Start();
			}
		}

		private void SetOptions(CacheOptions options)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (options.MaximumCacheSizeIndicator < 1)
				throw new ArgumentException("MaximumCacheSizeIndicator cannot be less than 1.");
			if (options.CacheItemExpiry.TotalMilliseconds < 10)
				throw new ArgumentException("CacheExpiry cannot be less than 10 ms.");
			if (options.FlushInterval.TotalMilliseconds < 50)
				throw new ArgumentException("FlushInterval cannot be less than 50 ms.");
			_options = options;
		}

		public async Task<TValue> GetOrLoadAsync(
			TKey key)
		{
			return await GetOrLoadAsync(key, false, this.LoaderFunction).ConfigureAwait(false);
		}

		public async Task<TValue> GetOrLoadAsync(
			TKey key,
			Func<TKey, Task<TValue>> loaderFunction)
		{
			return await GetOrLoadAsync(key, false, loaderFunction).ConfigureAwait(false);
		}

		public async Task<TValue> GetOrLoadAsync(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached)
		{
			return await GetOrLoadAsync(key, resetExpiryTimeoutIfAlreadyCached, this.LoaderFunction).ConfigureAwait(false);
		}

		public async Task<TValue> GetOrLoadAsync(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached,
			Func<TKey, Task<TValue>> loaderFunction)
		{
			TValue v;
			if (this.TryGet(key, resetExpiryTimeoutIfAlreadyCached, out v))
				return v;

			var keyGate = this.GetKeyGate(key);
			bool gotKeyLockBeforeTimeout = await keyGate.Lock.WaitAsync(_options.CircuitBreakerTimeoutForAdditionalThreadsPerKey).ConfigureAwait(false);
			if (!gotKeyLockBeforeTimeout)
			{
				throw new CircuitBreakerTimeoutException("The key's value is already busy loading, but the CircuitBreakerTimeoutForAdditionalThreadsPerKey of {1} ms has been reached. Hitting the cache again with the same key after a short while might work. Key: {0}".FormatWith(key.ToString(), _options.CircuitBreakerTimeoutForAdditionalThreadsPerKey.TotalMilliseconds));
			}
			else // Got the key gate lock.
			{
				try
				{
					return await GetIfCachedAndNotExpiredElseLoad(key, resetExpiryTimeoutIfAlreadyCached, loaderFunction);
				}
				finally
				{
					keyGate.Lock.Release();
				}
			}
		}

		private async Task<TValue> GetIfCachedAndNotExpiredElseLoad(TKey key, bool resetExpiryTimeoutIfAlreadyCached, Func<TKey, Task<TValue>> loaderFunction)
		{
			CacheEntry<TValue> entry;
			if (_cachedEntries.TryGetValue(key, out entry))
			{
				//TODO: make faster.
				var someTimeAgo = DateTime.Now.AddMilliseconds(-_options.CacheItemExpiry.TotalMilliseconds);
				if (entry.TimeLoaded < someTimeAgo)
				{
					// Entry is stale, reload.
					var newEntry = await this.LoadAndCacheEntryAsync(key, loaderFunction).ConfigureAwait(false);
					if (!object.ReferenceEquals(newEntry.CachedValue, entry.CachedValue))
						DisposeEntry(entry);

					return newEntry.CachedValue;
				}
				else // Cached entry is still good.
				{
					if (resetExpiryTimeoutIfAlreadyCached)
						entry.TimeLoaded = DateTime.Now;

					if (this.HitCallback != null)
						this.HitCallback(key, entry);

					return entry.CachedValue;
				}
			}

			// not in cache at all.
			return (await this.LoadAndCacheEntryAsync(key, loaderFunction).ConfigureAwait(false)).CachedValue;
		}

		public TValue Get(TKey key)
		{
			return this.Get(key, false);
		}

		public TValue Get(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached)
		{
			CacheEntry<TValue> entry;
			if (_cachedEntries.TryGetValue(key, out entry))
			{
				//TODO: make faster.
				var someTimeAgo = DateTime.Now.AddMilliseconds(-_options.CacheItemExpiry.TotalMilliseconds);
				if (entry.TimeLoaded < someTimeAgo)
				{
					// Expired
					return default(TValue);
				}

				if (this.HitCallback != null)
					this.HitCallback(key, entry);

				if (resetExpiryTimeoutIfAlreadyCached)
					entry.TimeLoaded = DateTime.Now;
				return entry.CachedValue;
			}
			else // not in cache at all.
				return default(TValue);
		}

		public bool TryGet(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached,
			out TValue value)
		{
			CacheEntry<TValue> entry;
			if (_cachedEntries.TryGetValue(key, out entry))
			{
				//TODO: make faster.
				var someTimeAgo = DateTime.Now.AddMilliseconds(-_options.CacheItemExpiry.TotalMilliseconds);
				if (entry.TimeLoaded < someTimeAgo)
				{
					// Expired
					value = default(TValue);
					return false;
				}

				if (this.HitCallback != null)
					this.HitCallback(key, entry);

				if (resetExpiryTimeoutIfAlreadyCached)
					entry.TimeLoaded = DateTime.Now;

				value = entry.CachedValue;
				return true;
			}
			else // not in cache at all.
			{
				value = default(TValue);
				return false;
			}
		}

		private async Task<CacheEntry<TValue>> LoadAndCacheEntryAsync(TKey key, Func<TKey, Task<TValue>> loaderFunction)
		{
			if (loaderFunction == null)
				throw new ArgumentNullException(nameof(loaderFunction));

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			CacheEntry<TValue> entry = new CacheEntry<TValue>();

			//HACK: execute sync to prevent deadlock.
			await Task.FromResult(0);
			entry.CachedValue = loaderFunction(key).Result;
			//entry.CachedValue = await loaderFunction(key).ConfigureAwait(false);

			entry.TimeLoaded = DateTime.Now;
			_cachedEntries.AddOrUpdate(key, entry, (k, v) => entry);

			stopwatch.Stop();

			if (this.MissedCallback != null)
				this.MissedCallback(key, entry, (int)stopwatch.ElapsedMilliseconds);

			return entry;
		}

		private KeyGate<TKey> GetKeyGate(TKey key)
		{
			//TODO: make lazy.
			var tempKeyGate = new KeyGate<TKey>(key);
			var keyGate = _keyGates.GetOrAdd(key, (k) => tempKeyGate);
			if (tempKeyGate != keyGate)
				tempKeyGate.Dispose();

			return keyGate;
		}

		public bool Invalidate(TKey key)
		{
			CacheEntry<TValue> tmp;
			bool removed = _cachedEntries.TryRemove(key, out tmp);
			if (removed)
				DisposeEntry(tmp);
			return removed;
		}

		public void InvalidateAll()
		{
			// Clear() acquires all internal locks simultaneously, so will cause more contention.
			//_cachedEntries.Clear();

			foreach (var pair in _cachedEntries)
				Invalidate(pair.Key);
		}

		public bool HasKey(TKey key)
		{
			CacheEntry<TValue> tmp;
			return _cachedEntries.TryGetValue(key, out tmp);
		}

		public void FlushInvalidatedEntries()
		{
			var entriesBeforeFlush = _cachedEntries.ToList();
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// Firsh flush stale entries.
			var someTimeAgo = DateTime.Now.AddMilliseconds(-_options.CacheItemExpiry.TotalMilliseconds);
			var remainingEntries = new List<KeyValuePair<TKey, CacheEntry<TValue>>>();
			// Enumerating over the ConcurrentDictionary is thread safe and lock free.
			foreach (var pair in _cachedEntries)
			{
				var key = pair.Key;
				var entry = pair.Value;
				if (entry.TimeLoaded < someTimeAgo)
				{
					// Entry is stale, remove it.
					if (!this.Invalidate(key))
						remainingEntries.Add(pair);
				}
				else
					remainingEntries.Add(pair);
			}

			// Now flush anything exceeding the max size, starting with the oldest entries first.
			if (remainingEntries.Count > _options.MaximumCacheSizeIndicator)
			{
				int numberOfEntriesToTrim = remainingEntries.Count - _options.MaximumCacheSizeIndicator;
				var keysToRemove = remainingEntries
					.OrderBy(p => p.Value.TimeLoaded)
					.Take(numberOfEntriesToTrim)
					.ToList();

				foreach (var entry in keysToRemove)
				{
					this.Invalidate(entry.Key);
					remainingEntries.Remove(entry);
				}
			}

			stopwatch.Stop();
			ExecuteFlushCallback(entriesBeforeFlush, remainingEntries, stopwatch.ElapsedMilliseconds);
		}

		private void ExecuteFlushCallback(List<KeyValuePair<TKey, CacheEntry<TValue>>> entriesBeforeFlush, List<KeyValuePair<TKey, CacheEntry<TValue>>> remainingEntries, long elapsedMilliseconds)
		{
			if (FlushCallback != null)
			{
				var clientContexts = entriesBeforeFlush
					.Union(remainingEntries)
					.Select(entry => entry.Value.ClientContext)
					.Distinct()
					.ToList();

				long elapsedMillisecondsPart = (long)elapsedMilliseconds.SafeDivideBy(clientContexts.Count, elapsedMilliseconds);
				foreach (var clientContext in clientContexts)
				{
					var beforeFlushCount = entriesBeforeFlush.Count(entry => entry.Value.ClientContext == clientContext);
					var remainingCount = remainingEntries.Count(entry => entry.Value.ClientContext == clientContext);
					var itemsFlushed = beforeFlushCount - remainingCount;
					FlushCallback(remainingCount, itemsFlushed, clientContext, elapsedMillisecondsPart);
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public IEnumerator<KeyValuePair<TKey, CacheEntry<TValue>>> GetEnumerator()
		{
			return _cachedEntries.GetEnumerator();
		}

		public bool TryAdd(TKey key, TValue value)
		{
			var entry = new CacheEntry<TValue>();
			entry.CachedValue = value;
			entry.TimeLoaded = DateTime.Now;
			return _cachedEntries.TryAdd(key, entry);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Cache()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			lock (this._disposeLock)
			{
				if (!_isDisposed)
				{
					if (disposing)
					{
						// free managed resources
						this.InvalidateAll();

						foreach (KeyGate<TKey> keyInfo in _keyGates.Values.Select(ki => ki).ToList())
						{
							keyInfo.Dispose();
							KeyGate<TKey> throwAway;
							_keyGates.TryRemove(keyInfo.Key, out throwAway);
						}

						if (this._flushTimer != null)
						{
							this._flushTimer.Stop();
							this._flushTimer.Dispose();
							this._flushTimer = null;
						}
					}
				}
			}

			// free native resources if there are any.
		}

		private void DisposeEntry(CacheEntry<TValue> entry)
		{
			if (_options.DisposeExpiredValuesIfDisposable)
			{
				if (entry.CachedValue is IDisposable)
				{
					var val = (IDisposable)entry.CachedValue;
					val.Dispose();
				}
			}
		}

		public Action<TKey, CacheEntry<TValue>> HitCallback { get; set; }

		public Action<TKey, CacheEntry<TValue>, int> MissedCallback { get; set; }

		public Action<long, long, string, long> FlushCallback { get; set; }
	}
}