using System;
using System.Threading.Tasks;

namespace ReCache
{
	public interface ISelfRefreshingCache<TKey, TValue> : ICache<TKey, TValue>
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