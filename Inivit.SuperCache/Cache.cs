﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace Inivit.SuperCache
{
	/* Read the following link and understand how ConcurrentDictionary works before modifying this class.
	 * http://arbel.net/2013/02/03/best-practices-for-using-concurrentdictionary/
	 */
	public class Cache<TKey, TValue> : ICache<TKey, TValue>
	{
		private ConcurrentDictionary<TKey, CacheEntry<TValue>> _cachedEntries;
		private CacheOptions _options;
		private Timer _flushTimer;

		private readonly String _cacheName;
		private readonly String _cacheHitMetric;
		private readonly String _cacheMissedMetric;
		private readonly String _cacheStaleHitMetric;
		private readonly String _cacheItemCountMetric;

		public string Name { get { return _cacheName; } }

		/// <summary>
		/// The function to use for retreaving the entry if it is not yet in the cache.
		/// </summary>
		public Func<TKey, TValue> LoaderFunction { get; set; }

		/// <summary>
		/// Returns the number of items in the cache at the moment this property was invoked. It delegates
		/// to the internal ConcurrentDictionary<TKey, TValue>.Count which acquires all internal locks.
		/// As such, it causes contention and should be used sparingly.
		/// </summary>
		public int Count { get { return this._cachedEntries.Count; } }

		public Cache(
			string cacheName,
			CacheOptions options)
			: this(cacheName, options, null)
		{
		}

		public Cache(
			string cacheName,
			CacheOptions options,
			Func<TKey, TValue> loaderFunction)
		{
			if (string.IsNullOrWhiteSpace(cacheName))
				throw new ArgumentException("cacheName cannot be null or a blank string.");
			if (cacheName.Contains(" ") || cacheName.Contains("."))
				throw new ArgumentException("cacheName may not contain a . or a space.");
			this.SetOptions(options);

			_cacheName = cacheName;
			_cacheHitMetric = string.Format("cache.{0}.hit", cacheName);
			_cacheMissedMetric = string.Format("cache.{0}.missed", _cacheName);
			_cacheStaleHitMetric = string.Format("cache.{0}.stale_hit", _cacheName);
			_cacheItemCountMetric = string.Format("cache.{0}.item_count", _cacheName);

			LoaderFunction = loaderFunction;
			_cachedEntries = new ConcurrentDictionary<TKey, CacheEntry<TValue>>();
			this.InitializeFlushTimer();
		}

		public Cache(
			string cacheName,
			IEqualityComparer<TKey> comparer,
			CacheOptions options)
			: this(cacheName, comparer, options, null)
		{

		}

		public Cache(
			string cacheName,
			IEqualityComparer<TKey> comparer,
			CacheOptions options,
			Func<TKey, TValue> loaderFunction)
			: this(cacheName, options, loaderFunction)
		{
			if (comparer == null)
				throw new ArgumentNullException("comparer");

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
					this.FlushInvalidatedEntries();
					_flushTimer.Start();
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

			var entry = new CacheEntry<TValue>();
			entry.CachedValue = loaderFunction(key);

			entry.TimeLoaded = DateTime.Now;
			_cachedEntries.AddOrUpdate(key, entry, (k, v) => entry);
			return entry;
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
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public IEnumerator<KeyValuePair<TKey, CacheEntry<TValue>>> GetEnumerator()
		{
			return _cachedEntries.GetEnumerator();
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

	}
}