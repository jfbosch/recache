using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;

namespace Inivit.SuperCache
{
	/* Read the following link and understand how ConcurrentDictionary works before modifying this class.
	 * http://arbel.net/2013/02/03/best-practices-for-using-concurrentdictionary/
	 */

	public class Cache<TKey, TValue> : ICache<TKey, TValue>
	{
		private ConcurrentDictionary<TKey, TKey> _keysBusyLoading;
		private ConcurrentDictionary<TKey, CacheEntry<TValue>> _cachedEntries;
		private CacheOptions _options;
		private Timer _flushTimer;

		/// <summary>
		/// The function to use for retreaving the entry if it is not yet in the cache.
		/// </summary>
		public Func<TKey, TValue> LoaderFunction { get; set; }

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
			Func<TKey, TValue> loaderFunction)
		{
			this.SetOptions(options);

			LoaderFunction = loaderFunction;
			_keysBusyLoading = new ConcurrentDictionary<TKey, TKey>();
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
			Func<TKey, TValue> loaderFunction)
			: this(options, loaderFunction)
		{
			if (comparer == null)
				throw new ArgumentNullException("comparer");

			_keysBusyLoading = new ConcurrentDictionary<TKey, TKey>();
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
					catch (Exception)
					{
						throw;
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
				throw new ArgumentNullException("options");
			if (options.MaximumCacheSizeIndicator < 1)
				throw new ArgumentException("MaximumCacheSizeIndicator cannot be less than 1.");
			if (options.CacheItemExpiry.TotalMilliseconds < 10)
				throw new ArgumentException("CacheExpiry cannot be less than 10 ms.");
			if (options.FlushInterval.TotalMilliseconds < 50)
				throw new ArgumentException("FlushInterval cannot be less than 50 ms.");
			_options = options;
		}

		public TValue GetOrLoad(
			TKey key)
		{
			return GetOrLoad(key, false, this.LoaderFunction);
		}

		public TValue GetOrLoad(
			TKey key,
			Func<TKey, TValue> loaderFunction)
		{
			return GetOrLoad(key, false, loaderFunction);
		}

		public TValue GetOrLoad(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached)
		{
			return GetOrLoad(key, resetExpiryTimeoutIfAlreadyCached, this.LoaderFunction);
		}

		public TValue GetOrLoad(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached,
			Func<TKey, TValue> loaderFunction)
		{
			CacheEntry<TValue> entry;
			if (_cachedEntries.TryGetValue(key, out entry))
			{
				var someTimeAgo = DateTime.Now.AddMilliseconds(-_options.CacheItemExpiry.TotalMilliseconds);
				if (entry.TimeLoaded < someTimeAgo)
				{
					// Entry is stale, reload.
					return LoadAndCacheEntry(key, loaderFunction).CachedValue;
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
			return LoadAndCacheEntry(key, loaderFunction).CachedValue;
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
				if (this.HitCallback != null)
					this.HitCallback(key, entry);

				if (resetExpiryTimeoutIfAlreadyCached)
					entry.TimeLoaded = DateTime.Now;
				return entry.CachedValue;
			}
			else // not in cache at all.
				return default(TValue);
		}

		private CacheEntry<TValue> LoadAndCacheEntry(TKey key, Func<TKey, TValue> loaderFunction)
		{
			if (loaderFunction == null)
				throw new ArgumentNullException("loaderFunction");

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			CacheEntry<TValue> entry;
			bool isNewKey = _keysBusyLoading.TryAdd(key, key);
			if (isNewKey)
			{
				try
				{
					entry = new CacheEntry<TValue>();
					entry.CachedValue = loaderFunction(key);
					entry.TimeLoaded = DateTime.Now;
					_cachedEntries.AddOrUpdate(key, entry, (k, v) => entry);
				}
				finally
				{
					TKey throwaway;
					_keysBusyLoading.TryRemove(key, out throwaway);
				}
			}
			else // Key is already busy loading
			{
				// Wait for the key busy being cached to complete so that we can return it's value for this thread too. But stop waiting when the timeout is reached.
				var cancellation = new System.Threading.CancellationTokenSource();
				var pollingTask = PollForEntry(key, cancellation.Token);
				if (Task.WaitAll(new Task[] { pollingTask }, _options.CircuitBreakerTimeoutForAdditionalThreadsPerKey))
					entry = pollingTask.Result;
				else
				{
					cancellation.Cancel(false);
					throw new CircuitBreakerTimeoutException("The key's value is already busy loading, but the CircuitBreakerTimeoutForAdditionalThreadsPerKey of {1} ms has been reached. Hitting the cache again with the same key after a short while might work. Key: {0}".FormatWith(key.ToString(), _options.CircuitBreakerTimeoutForAdditionalThreadsPerKey.TotalMilliseconds));
				}
			}

			stopwatch.Stop();

			if (this.MissedCallback != null)
				this.MissedCallback(key, entry, (int)stopwatch.ElapsedMilliseconds);

			return entry;
		}

		private Task<CacheEntry<TValue>> PollForEntry(TKey key, System.Threading.CancellationToken cancellationToken)
		{
			Func<CacheEntry<TValue>> poller = () =>
			{
				while (true)
				{
					if (cancellationToken.IsCancellationRequested)
						return null;

					CacheEntry<TValue> e;
					if (_cachedEntries.TryGetValue(key, out e))
						return e;
					else
						System.Threading.Thread.Sleep(1);
				}
			};
			return Task.Run<CacheEntry<TValue>>(poller, cancellationToken);
		}

		public bool Invalidate(TKey key)
		{
			CacheEntry<TValue> tmp;
			return _cachedEntries.TryRemove(key, out tmp);
		}

		public void InvalidateAll()
		{
			// Clear() acquires all internal locks simultaneously, so will cause more contention.
			_cachedEntries.Clear();
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
			if (disposing)
			{
				// free managed resources
				this.InvalidateAll();
			}

			// free native resources if there are any.
			if (this._flushTimer != null)
			{
				this._flushTimer.Stop();
				this._flushTimer.Dispose();
				this._flushTimer = null;
			}
		}

		public Action<TKey, CacheEntry<TValue>> HitCallback { get; set; }

		public Action<TKey, CacheEntry<TValue>, int> MissedCallback { get; set; }

		public Action<long, long, string, long> FlushCallback { get; set; }
	}
}