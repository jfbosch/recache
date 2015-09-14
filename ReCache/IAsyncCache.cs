using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReCache
{
	public interface IAsyncCache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, CacheEntry<TValue>>>, IDisposable
	{
		TValue Get(TKey key);

		TValue Get(TKey key, bool resetExpiryTimeoutIfAlreadyCached);

		Task<TValue> GetOrLoadAsync(TKey key);

		Task<TValue> GetOrLoadAsync(TKey key, Func<TKey, Task<TValue>> loaderFunction);

		Task<TValue> GetOrLoadAsync(TKey key, bool resetExpiryTimeoutIfAlreadyCached);

		Task<TValue> GetOrLoadAsync(TKey key, bool resetExpiryTimeoutIfAlreadyCached, Func<TKey, Task<TValue>> loaderFunction);

		bool HasKey(TKey key);

		bool Invalidate(TKey key);

		void InvalidateAll();

		bool TryAdd(TKey key, TValue value);

		Func<TKey, Task<TValue>> LoaderFunction { get; set; }

		void FlushInvalidatedEntries();

		int Count { get; }

		IEnumerable<KeyValuePair<TKey, TValue>> Items { get; }

		Action<TKey, CacheEntry<TValue>> HitCallback { get; set; }

		Action<TKey, CacheEntry<TValue>, int> MissedCallback { get; set; }

		Action<long, long, string, long> FlushCallback { get; set; }
	}
}