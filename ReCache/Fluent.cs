using System;

namespace ReCache
{
	public static class Fluent
	{
		public static CacheBuilder<TKey, TValue> CacheItemExpiry<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			TimeSpan cacheItemExpiry)
		{
			builder.CacheOptions.CacheItemExpiry = cacheItemExpiry;
			return builder;
		}

		public static CacheBuilder<TKey, TValue> FlushInterval<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			TimeSpan flushInterval)
		{
			builder.CacheOptions.FlushInterval = flushInterval;
			return builder;
		}

		public static CacheBuilder<TKey, TValue> MaximumCacheSizeIndicator<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int maximumCacheSizeIndicator)
		{
			builder.CacheOptions.MaximumCacheSizeIndicator = maximumCacheSizeIndicator;
			return builder;
		}

		public static CacheBuilder<TKey, TValue> LoaderFunction<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			Func<TKey, TValue>  loaderFunction)
		{
			builder.LoaderFunc = loaderFunction;
			return builder;
		}

		public static Cache<TKey, TValue> Create<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder)
		{
			var cache = new Cache<TKey, TValue>(builder.CacheOptions);
			cache.LoaderFunction = builder.LoaderFunc;
			return cache;
		}

	}
}
