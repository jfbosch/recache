using System;
using System.Collections.Generic;

namespace Inivit.SuperCache
{
	public interface ICache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, CacheEntry<TValue>>>, IDisposable
	{
		TValue Get(TKey key);

		TValue Get(TKey key, bool resetExpiryTimeoutIfAlreadyCached);

		TValue GetOrLoad(TKey key);

		TValue GetOrLoad(TKey key, Func<TKey, TValue> loaderFunction);

		TValue GetOrLoad(TKey key, bool resetExpiryTimeoutIfAlreadyCached);

		TValue GetOrLoad(TKey key, bool resetExpiryTimeoutIfAlreadyCached, Func<TKey, TValue> loaderFunction);

		bool HasKey(TKey key);

		bool Invalidate(TKey key);

		void InvalidateAll();

		bool TryAdd(TKey key, TValue value);

		Func<TKey, TValue> LoaderFunction { get; set; }

		void FlushInvalidatedEntries();

		int Count { get; }

		IEnumerable<KeyValuePair<TKey, TValue>> Items { get; }

		Action<TKey, CacheEntry<TValue>> HitCallback { get; set; }

		Action<TKey, CacheEntry<TValue>, int> MissedCallback { get; set; }

		Action<long, long, string, long> FlushCallback { get; set; }
	}
}