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
		/// ClientContext
		/// </summary>
		Action<int, int, string> RefreshCacheCallback { get; set; }
		Action<int, int, Exception> RefreshCacheFailedCallback { get; set; }
	}
}