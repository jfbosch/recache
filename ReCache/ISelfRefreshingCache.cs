using System;
using System.Threading.Tasks;

namespace ReCache
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
	public interface ISelfRefreshingCache<TKey, TValue> : ICache<TKey, TValue>
	{
		/// <summary>
		/// Parameters are:
		/// CurrentGeneration
		/// DurationMilliseconds
		/// </summary>
		Action<int, long> RefreshCacheCallback { get; set; }
		/// <summary>
		/// Parameters are:
		/// CurrentGeneration
		/// DurationMilliseconds
		/// The reason for the failure.
		/// </summary>
		Action<int, long, Exception> RefreshCacheFailedCallback { get; set; }
	}
}