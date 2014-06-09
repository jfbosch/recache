using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;

namespace Inivit.SuperCache
{
	/// <summary>
	/// This cache will automatically refresh all existing entries on the specified
	/// interval. Whenever an entry is requested for a given key, the latest available
	/// entry for that key will be returned if it is already cached. If the entry does
	/// not yet exist in the cache, it will be loaded and cached, and will from that
	/// point on automatically be refreshed on the specified interval. As such, this
	/// cache will periodically and asynchronously keep it's entries up to date with
	/// values from the backend source, while still serving up zero latency hits of the
	/// latest values.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class SelfRefreshingCache<TKey, TValue> : ICache<TKey, TValue>
	{
		private readonly String _cacheRefreshedMetric;
		private readonly String _cacheRefreshedGenerationMetric;

		private int _currentGeneration = 0;
		private System.Timers.Timer _refresherTimer;
		private readonly SelfRefreshingCacheOptions _options;

		public string Name { get { return _generationCache.Name; } }

		// The backing, generation cache, with the int part of the key being the generation of the cache entry.
		private ICache<Tuple<TKey, int>, TValue> _generationCache;

		/// <summary>
		/// The function to use for retreaving the entry if it is not yet in the cache.
		/// </summary>
		public Func<TKey, TValue> LoaderFunction { get; set; }

		/// <summary>
		/// Returns the number of items in the cache at the moment this property was invoked. It delegates
		/// to the internal ConcurrentDictionary<TKey, TValue>.Count which acquires all internal locks.
		/// As such, it causes contention and should be used sparingly.
		/// </summary>
		public int Count { get { return this._generationCache.Count; } }

		public SelfRefreshingCache(
			string cacheName,
			SelfRefreshingCacheOptions options,
			Func<TKey, TValue> loaderFunction)
		{
			if (options == null)
				throw new ArgumentNullException("options");
			if ((options.RefreshInterval.TotalMilliseconds / options.StandardCacheOptions.CacheItemExpiry.TotalMilliseconds * 100) > 50)
				throw new ArgumentException("The RefreshInterval may at most be 50% of the length of the CacheItemExpiry to allow for ample reload time, else the cache will experience unnecessary, possibly concurrent, misses. Either decrease the refresh interval, or increase the expiry timeout.");
			if (loaderFunction == null)
				throw new ArgumentNullException("loaderFunction");

			_options = options;

			this.LoaderFunction = loaderFunction;
			// Wrap the incoming loader function into a function that can be passed to the generation cache.
			Func<Tuple<TKey, int>, TValue> versionLoderFunction = (generationKey) =>
			{
				return this.LoaderFunction(generationKey.Item1);
			};

			_generationCache = new Cache<Tuple<TKey, int>, TValue>(
				cacheName,
				new TupleComparer<TKey, int>(),
				options.StandardCacheOptions,
				versionLoderFunction);

			_cacheRefreshedMetric = string.Format("cache.{0}.self_refreshing.refreshed", cacheName);
			_cacheRefreshedGenerationMetric = string.Format("cache.{0}.self_refreshing.current_generation", cacheName);

			_refresherTimer = new System.Timers.Timer(options.RefreshInterval.TotalMilliseconds);
			_refresherTimer.Elapsed += RefreshCache;
			_refresherTimer.Start();
		}

		private void RefreshCache(object sender, ElapsedEventArgs e)
		{
			try
			{
				_refresherTimer.Stop();
				int nextGeneration = _currentGeneration + 1;
				if (nextGeneration == int.MaxValue)
					// If this cache refreshes every 1 sec, and it's been running for 68.1 years, we'll have to loop back to start at zero again.
					nextGeneration = 0;

				// Populate the next generation cache before we switch it to be the active, current generation.
				var currentGenerationEntries = _generationCache
					.Where(entry => entry.Key.Item2 == _currentGeneration)
					// Start with the freshist entries.
					.OrderByDescending(entry => entry.Value.TimeLoaded);
				int itemCount = 0;
				foreach (var entry in currentGenerationEntries)
				{
					_generationCache.GetOrLoad(GenerationKey(entry.Key.Item1, nextGeneration));

					if (++itemCount > _options.StandardCacheOptions.MaximumCacheSizeIndicator)
						// Stop migrating entries to the next generation if we have reached the max.
						return;
				}

				// The next generation's cache entries have been loaded, so let's switch to it.
				int previousGeneration = Interlocked.Exchange(ref _currentGeneration, nextGeneration);

				// Invalidate the previous generation entries, as we no longer need them.
				var previousGenerationEntries = _generationCache.Where(entry => entry.Key.Item2 == previousGeneration);
				foreach (var oldEntry in previousGenerationEntries)
					_generationCache.Invalidate(oldEntry.Key);
			}
			finally
			{
				_refresherTimer.Start();
			}
		}

		public TValue GetOrLoad(
			TKey key)
		{
			return GetOrLoad(key, false);
		}

		public TValue GetOrLoad(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached)
		{
			// If the next generation for this key is already loaded, we can go ahead and return it.
			var nextGenKey = GenerationKey(key, _currentGeneration + 1);
			var val = _generationCache.Get(nextGenKey, resetExpiryTimeoutIfAlreadyCached);
			// We include the HasKey check here because if TValue is a primative type, the != null check will always return true, even if there was no cache entry.
			if (val != null && _generationCache.HasKey(nextGenKey))
				return val;

			// Else fall back to standard behavior for GetOrLoad current generation
			return this._generationCache.GetOrLoad(GenerationKey(key), resetExpiryTimeoutIfAlreadyCached);
		}

		public TValue Get(TKey key)
		{
			return this.Get(key, false);
		}

		public TValue Get(
			TKey key,
			bool resetExpiryTimeoutIfAlreadyCached)
		{
			// If the next generation for this key is already loaded, we can go ahead and return it.
			var nextGenKey = GenerationKey(key, _currentGeneration + 1);
			var val = _generationCache.Get(nextGenKey, resetExpiryTimeoutIfAlreadyCached);
			// We include the HasKey check here because if TValue is a primative type, the != null check will always return true, even if there was no cache entry.
			if (val != null && _generationCache.HasKey(nextGenKey))
				return val;

			// Else fall back to standard behavior for Get current generation
			return this._generationCache.Get(GenerationKey(key), resetExpiryTimeoutIfAlreadyCached);
		}

		public bool Invalidate(TKey key)
		{
			return _generationCache.Invalidate(GenerationKey(key));
		}

		public void InvalidateAll()
		{
			_generationCache.InvalidateAll();
		}

		public bool HasKey(TKey key)
		{
			return _generationCache.HasKey(GenerationKey(key));
		}

		/// <summary>
		/// Returns the compound generation key for the combination of the provided key and current generation.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private Tuple<TKey, int> GenerationKey(TKey key)
		{
			return GenerationKey(key, _currentGeneration);
		}

		/// <summary>
		/// Returns the compound generation key for the provided key and generation.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="generation"></param>
		/// <returns></returns>
		private Tuple<TKey, int> GenerationKey(TKey key, int generation)
		{
			var generationKey = new Tuple<TKey, int>(key, generation);
			return generationKey;
		}

		public void FlushInvalidatedEntries()
		{
			_generationCache.FlushInvalidatedEntries();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public IEnumerator<KeyValuePair<TKey, CacheEntry<TValue>>> GetEnumerator()
		{
			int gen = _currentGeneration;
			var currentGenerationEntries = _generationCache
				.Where(e => e.Key.Item2 == gen)
				.Select(e => new KeyValuePair<TKey, CacheEntry<TValue>>(e.Key.Item1, e.Value));

			foreach (var entry in currentGenerationEntries)
				yield return entry;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~SelfRefreshingCache()
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
			if (this._refresherTimer != null)
			{
				this._refresherTimer.Stop();
				this._refresherTimer.Dispose();
				this._refresherTimer = null;
			}

			if (_generationCache != null)
			{
				_generationCache.Dispose();
				_generationCache = null;
			}
		}

		public TValue GetOrLoad(TKey key, Func<TKey, TValue> loaderFunction)
		{
			throw new NotImplementedException("Custom loaders do not make sense in a SelfRefreshingCache");
		}

		public TValue GetOrLoad(TKey key, bool resetExpiryTimeoutIfAlreadyCached, Func<TKey, TValue> loaderFunction)
		{
			throw new NotImplementedException("Custom loaders do not make sense in a SelfRefreshingCache");
		}
	}
}
