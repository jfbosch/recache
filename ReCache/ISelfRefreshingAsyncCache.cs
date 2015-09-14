using System;
using System.Threading.Tasks;

namespace ReCache
{
	public interface ISelfRefreshingAsyncCache<TKey, TValue> : IAsyncCache<TKey, TValue>
	{
		/// <summary>
		/// Parameters are:
		/// CurrentGeneration
		/// DurationMilliseconds
		/// ClientContext
		/// </summary>
		Action<int, int, string> RefreshCacheCallback { get; set; }
	}
}