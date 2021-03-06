﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ReCache
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
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public class SelfRefreshingCache<TKey, TValue> : ISelfRefreshingCache<TKey, TValue>
	{
		private volatile int _currentGeneration = 0;
		private System.Timers.Timer _refresherTimer;
		private readonly SelfRefreshingCacheOptions _options;

		// The backing, generation cache, with the int part of the key being the generation of the cache entry.
		private ICache<Tuple<TKey, int>, TValue> _generationCache;

		private Action<TKey, CacheEntry<TValue>> _generationCacheHitCallback;
		private Action<TKey, CacheEntry<TValue>, long> _generationCacheMissedCallback;

		public string CacheName
		{
			get { return _generationCache.CacheName; }
			set { _generationCache.CacheName = value; }
		}

		/// <summary>
		/// The function to use for retreaving the entry if it is not yet in the cache.
		/// </summary>
		public Func<TKey, Task<TValue>> LoaderFunction { get; set; }

		/// <summary>
		/// Returns the number of items in the cache by enumerating them (non-locking).
		/// </summary>
		public int Count => this.Items.Count();

		public IEnumerable<KeyValuePair<TKey, TValue>> Items => _generationCache
			.Where(x => x.Key.Item2 == _currentGeneration)
			.Select(x => new KeyValuePair<TKey, TValue>(x.Key.Item1, x.Value.CachedValue));

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public SelfRefreshingCache(
			SelfRefreshingCacheOptions options,
			Func<TKey, Task<TValue>> loaderFunction)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if ((options.RefreshInterval.TotalMilliseconds / options.StandardCacheOptions.CacheItemExpiry.TotalMilliseconds * 100) > 50)
				throw new ArgumentException("The RefreshInterval may at most be 50% of the length of the CacheItemExpiry to allow for ample reload time, else the cache will experience unnecessary, possibly concurrent, misses. Either decrease the refresh interval, or increase the expiry timeout. CacheName: " + options.StandardCacheOptions ?? string.Empty);
			if (loaderFunction == null)
				throw new ArgumentNullException(nameof(loaderFunction) + ";  CacheName: " + options.StandardCacheOptions ?? string.Empty);

			_options = options;

			this.LoaderFunction = loaderFunction;
			// Wrap the incoming loader function into a function that can be passed to the generation cache.
			Func<Tuple<TKey, int>, Task<TValue>> versionLoderFunction = (generationKey) =>
			{
				return this.LoaderFunction(generationKey.Item1);
			};

			_generationCache = new Cache<Tuple<TKey, int>, TValue>(
				new TupleComparer<TKey, int>(),
				options.StandardCacheOptions,
				versionLoderFunction);

			_refresherTimer = new System.Timers.Timer(options.RefreshInterval.TotalMilliseconds);
			_refresherTimer.Elapsed += RefreshCache;
			_refresherTimer.Start();
		}

		// Suppress warning of volatile not being treated as volatile
		// http://msdn.microsoft.com/en-us/library/4bw5ewxy.aspx
#pragma warning disable 420

		private async void RefreshCache(object sender, ElapsedEventArgs e)
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			try
			{
				_refresherTimer.Stop();

				int nextGeneration = _currentGeneration + 1;
				if (nextGeneration == int.MaxValue)
					// If this cache refreshes every 1 sec, and it's been running for 68.1 years, we'll have to loop back to start at zero again.
					nextGeneration = 0;

				// Populate the next generation cache before we switch it to be the active, current generation.
				var currentGenerationEntriesToRefresh = await LoadNextGenerationAsync(nextGeneration).ConfigureAwait(false);

				// The next generation's cache entries have been loaded, so let's switch to it.
				int previousGeneration = Interlocked.Exchange(ref _currentGeneration, nextGeneration);

				// Invalidate the previous generation entries, as we no longer need them.
				var previousGenerationEntries = _generationCache.Where(entry => entry.Key.Item2 == previousGeneration);
				foreach (var oldEntry in previousGenerationEntries)
					_generationCache.Invalidate(oldEntry.Key);

				stopwatch.Stop();

				TryRefreshCacheCallback(_currentGeneration, stopwatch.ElapsedMilliseconds);
			}
			catch (Exception ex)
			{
				stopwatch.Stop();
				// If the refresh failed, we want to pass the info back to the app using the cache.
				TryRefreshCacheFailedCallback(stopwatch, ex);
			}
			finally
			{
				_refresherTimer.Start();
			}
		}

		private void TryRefreshCacheFailedCallback(System.Diagnostics.Stopwatch stopwatch, Exception ex)
		{
			try
			{
				this.RefreshCacheFailedCallback?.Invoke(_currentGeneration, stopwatch.ElapsedMilliseconds, ex);
			}
			finally { } // suppress client code exceptions
		}

		private void TryRefreshCacheCallback(
			int currentGeneration,
			long elapsedMilliseconds)
		{
			try
			{
				this.RefreshCacheCallback?.Invoke(currentGeneration, elapsedMilliseconds);
			}
			finally { } // suppress client code exceptions
		}

		private async Task<IOrderedEnumerable<KeyValuePair<Tuple<TKey, int>, CacheEntry<TValue>>>> LoadNextGenerationAsync(int nextGeneration)
		{
			var currentGenerationEntriesToRefresh = _generationCache
				.Where(entry => entry.Key.Item2 == _currentGeneration)
				// Start with the freshist accessed entries.
				.OrderByDescending(entry => entry.Value.TimeLastAccessed);

			int itemCount = 0;
			foreach (var entry in currentGenerationEntriesToRefresh)
			{
				await _generationCache.GetOrLoadAsync(GenerationKey(entry.Key.Item1, nextGeneration)).ConfigureAwait(false);

				itemCount++;
				if (itemCount >= _options.StandardCacheOptions.MaximumCacheSizeIndicator)
					// Stop migrating entries to the next generation if we have reached the max.
					break;
			}

			return currentGenerationEntriesToRefresh;
		}

		public async Task<TValue> GetOrLoadAsync(
			TKey key)
		{
			return await GetOrLoadAsync(key, false).ConfigureAwait(false);
		}

		public async Task<TValue> GetOrLoadAsync(
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
			return await this._generationCache.GetOrLoadAsync(GenerationKey(key), resetExpiryTimeoutIfAlreadyCached).ConfigureAwait(false);
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
		private static Tuple<TKey, int> GenerationKey(TKey key, int generation)
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

		public bool TryAdd(TKey key, TValue value)
		{
			var entry = new CacheEntry<TValue>();
			entry.CachedValue = value;
			entry.TimeLoaded = DateTime.UtcNow;
			return _generationCache.TryAdd(GenerationKey(key), value);
		}

		~SelfRefreshingCache()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "SelfRefreshingCache")]
		public Task<TValue> GetOrLoadAsync(TKey key, Func<TKey, Task<TValue>> loaderFunction)
		{
			throw new NotImplementedException("Custom loaders do not make sense in a SelfRefreshingCache. CacheName: " + this.CacheName);
		}

		public Task<TValue> GetOrLoadAsync(TKey key, bool resetExpiryTimeoutIfAlreadyCached, Func<TKey, Task<TValue>> loaderFunction)
		{
			throw new NotImplementedException("Custom loaders do not make sense in a SelfRefreshingCache. CacheName: " + this.CacheName);
		}

		public Action<TKey, CacheEntry<TValue>> HitCallback
		{
			get
			{
				return _generationCacheHitCallback;
			}
			set
			{
				_generationCacheHitCallback = value;
				if (_generationCacheHitCallback != null)
				{
					_generationCache.HitCallback = (genKey, cacheEntry) =>
					{
						_generationCacheHitCallback(genKey.Item1, cacheEntry);
					};
				}
				else
					_generationCache.HitCallback = null;
			}
		}

		public Action<TKey, CacheEntry<TValue>, long> MissedCallback
		{
			get
			{
				return _generationCacheMissedCallback;
			}
			set
			{
				_generationCacheMissedCallback = value;
				if (_generationCacheMissedCallback != null)
				{
					_generationCache.MissedCallback = (genKey, cacheEntry, durationMilliseconds) =>
					{
						_generationCacheMissedCallback(genKey.Item1, cacheEntry, durationMilliseconds);
					};
				}
				else
					_generationCache.MissedCallback = null;
			}
		}

		public Action<int, int, long> FlushCallback
		{
			get
			{
				return _generationCache.FlushCallback;
			}
			set
			{
				_generationCache.FlushCallback = value;
			}
		}

		public Action<int, long> RefreshCacheCallback { get; set; }
		public Action<int, long, Exception> RefreshCacheFailedCallback { get; set; }
	}
}