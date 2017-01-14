using System;
using System.Threading.Tasks;

namespace ReCache
{
	public static class Fluent
	{

		public static CacheBuilder<TKey, TValue> WithName<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			string name)
		{
			builder.CacheOptions.CacheName = name;
			return builder;
		}

		public static CacheBuilder<TKey, TValue> CacheItemExpiryFromTimeSpan<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			TimeSpan cacheItemExpiry)
		{
			builder.CacheOptions.CacheItemExpiry = cacheItemExpiry;
			return builder;
		}


		public static CacheBuilder<TKey, TValue> CacheItemExpiryFromMilliseconds<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int milliseconds)
		{
			builder.CacheOptions.CacheItemExpiry = TimeSpan.FromMilliseconds(milliseconds);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> CacheItemExpiryFromSeconds<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int seconds)
		{
			builder.CacheOptions.CacheItemExpiry = TimeSpan.FromSeconds(seconds);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> CacheItemExpiryFromMinutes<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int minutes)
		{
			builder.CacheOptions.CacheItemExpiry = TimeSpan.FromMinutes(minutes);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> CircuitBreakerTimeoutForAdditionalThreadsPerKeyFromMilliseconds<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int milliseconds)
		{
			builder.CacheOptions.CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.FromMilliseconds(milliseconds);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> CircuitBreakerTimeoutForAdditionalThreadsPerKeyFromSeconds<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int seconds)
		{
			builder.CacheOptions.CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.FromSeconds(seconds);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> CircuitBreakerTimeoutForAdditionalThreadsPerKeyFromMinutes<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int minutes)
		{
			builder.CacheOptions.CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.FromMinutes(minutes);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> CircuitBreakerTimeoutForAdditionalThreadsPerKeyFromTimeSpan<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			TimeSpan timeout)
		{
			builder.CacheOptions.CircuitBreakerTimeoutForAdditionalThreadsPerKey = timeout;
			return builder;
		}


		public static CacheBuilder<TKey, TValue> CacheItemExpiryPercentageRandomization<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int cacheItemExpiryPercentageRandomization)
		{
			builder.CacheOptions.CacheItemExpiryPercentageRandomization = cacheItemExpiryPercentageRandomization;
			return builder;
		}

		public static CacheBuilder<TKey, TValue> FlushIntervalFromTimeSpan<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			TimeSpan flushInterval)
		{
			builder.CacheOptions.FlushInterval = flushInterval;
			return builder;
		}

		public static CacheBuilder<TKey, TValue> FlushIntervalFromMilliseconds<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int milliseconds)
		{
			builder.CacheOptions.FlushInterval = TimeSpan.FromMilliseconds(milliseconds);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> FlushIntervalFromSeconds<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int seconds)
		{
			builder.CacheOptions.FlushInterval = TimeSpan.FromSeconds(seconds);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> FlushIntervalFromMinutes<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int minutes)
		{
			builder.CacheOptions.FlushInterval = TimeSpan.FromMinutes(minutes);
			return builder;
		}

		public static CacheBuilder<TKey, TValue> MaximumCacheSizeIndicator<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			int maximumCacheSizeIndicator)
		{
			builder.CacheOptions.MaximumCacheSizeIndicator = maximumCacheSizeIndicator;
			return builder;
		}

		public static CacheBuilder<TKey, TValue> DisposeExpiredValuesIfDisposable<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder)
		{
			builder.CacheOptions.DisposeExpiredValuesIfDisposable = true;
			return builder;
		}


		public static CacheBuilder<TKey, TValue> LoaderFunction<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder,
			Func<TKey, Task<TValue>> loaderFunction)
		{
			builder.LoaderFunc = loaderFunction;
			return builder;
		}

		public static Cache<TKey, TValue> Create<TKey, TValue>(
			this CacheBuilder<TKey, TValue> builder)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			var cache = new Cache<TKey, TValue>(builder.CacheOptions);
			cache.LoaderFunction = builder.LoaderFunc;
			return cache;
		}

	}
}
