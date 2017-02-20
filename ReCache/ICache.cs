using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReCache
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public interface ICache<TKey, TValue> : IEnumerable<KeyValuePair<TKey, CacheEntry<TValue>>>, IDisposable
	{
		TValue Get(TKey key);

		TValue Get(TKey key, bool resetExpiryTimeoutIfAlreadyCached);

		Task<TValue> GetOrLoadAsync(TKey key);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		Task<TValue> GetOrLoadAsync(TKey key, Func<TKey, Task<TValue>> loaderFunction);

		Task<TValue> GetOrLoadAsync(TKey key, bool resetExpiryTimeoutIfAlreadyCached);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		Task<TValue> GetOrLoadAsync(TKey key, bool resetExpiryTimeoutIfAlreadyCached, Func<TKey, Task<TValue>> loaderFunction);

		bool HasKey(TKey key);

		bool Invalidate(TKey key);

		void InvalidateAll();

		bool TryAdd(TKey key, TValue value);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		Func<TKey, Task<TValue>> LoaderFunction { get; set; }

		void FlushInvalidatedEntries();

		int Count { get; }
		string CacheName { get; set; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		IEnumerable<KeyValuePair<TKey, TValue>> Items { get; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		Action<TKey, CacheEntry<TValue>> HitCallback { get; set; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		Action<TKey, CacheEntry<TValue>, long> MissedCallback { get; set; }

		Action<int, int, long> FlushCallback { get; set; }
	}
}