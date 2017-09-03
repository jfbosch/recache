using System;
using System.Collections.Generic;

namespace ReCache
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public interface IKeyValueStore<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
	{
		TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory);
		bool TryAdd(TKey key, TValue value);
		bool TryGetValue(TKey key, out TValue value);
		bool TryRemove(TKey key, out TValue value);
	}
}