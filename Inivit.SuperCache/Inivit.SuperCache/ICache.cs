﻿using System;
using System.Collections.Generic;

namespace Inivit.SuperCache
{
	interface ICache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, CacheEntry<TValue>>>, IDisposable
	{
		string Name { get; }
		TValue Get(TKey key);
		TValue Get(TKey key, bool resetExpiryTimeoutIfAlreadyCached);
		TValue GetOrLoad(TKey key);
		TValue GetOrLoad(TKey key, Func<TKey, TValue> loaderFunction);
		TValue GetOrLoad(TKey key, bool resetExpiryTimeoutIfAlreadyCached);
		TValue GetOrLoad(TKey key, bool resetExpiryTimeoutIfAlreadyCached, Func<TKey, TValue> loaderFunction);
		bool HasKey(TKey key);
		bool Invalidate(TKey key);
		void InvalidateAll();
		Func<TKey, TValue> LoaderFunction { get; set; }
		void FlushInvalidatedEntries();
		int Count { get; }
	}
}