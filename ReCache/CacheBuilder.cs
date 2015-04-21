using System;

namespace ReCache
{
	public class CacheBuilder
	{
		public static CacheBuilder<TKey, TValue> Build<TKey, TValue>()
		{
			return new CacheBuilder<TKey, TValue>();
		}
	}

	public class CacheBuilder<TKey, TValue>
	{
		internal CacheOptions CacheOptions { get; set; }
		internal Func<TKey, TValue> LoaderFunc { get; set; }

		public CacheBuilder()
		{
			this.CacheOptions = new CacheOptions();
		}
	}
}
