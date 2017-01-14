using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using ReCache;
using System.Threading.Tasks;
using FluentAssertions;

namespace Tests
{
	[TestClass]
	public class SampleUsage
	{
		[TestMethod]
		public async Task Sample1()
		{
			// This func is to fetch the value from the data source. In this case it 
			// is just a random generator, but this could be anything like a web service or database.
			// The parameter should be the same type as your cache key type, and the return value
			// should be the same type as your cache's value type.
			Func<int, Task<string>> loaderFunction = async (key) =>
			{
				int rnd = new Random().Next();
				return await Task.FromResult(string.Format("value{0}", rnd));
			};

			var cacheOptions = new CacheOptions
			{
				CacheName = nameof(Sample1),
				CacheItemExpiry = TimeSpan.FromSeconds(1),
				CacheItemExpiryPercentageRandomization = 0,
				FlushInterval = TimeSpan.FromSeconds(5),
				MaximumCacheSizeIndicator = 100
			};
			var cache = new Cache<int, string>(
				cacheOptions, loaderFunction);

			// Record the first hit values for 100 keys
			var firstHitValues = new List<string>();
			for (int key = 0; key < 100; key++)
			{
				var value = await cache.GetOrLoadAsync(key);
				firstHitValues.Add(value);
			}

			// Assert that 50 subsequent hits return the same values for all 100 keys.
			for (int i = 0; i < 50; i++)
			{
				for (int key = 0; key < 100; key++)
				{
					var value = await cache.GetOrLoadAsync(key);
					Assert.AreEqual(firstHitValues[key], value);
				}
			}

			// Wait a while so that all cache entries expire as per the configured options
			Thread.Sleep(1000);

			// Assert that the next hit returns fresh values. i.e. not equal to the first hit values. 
			for (int key = 0; key < 100; key++)
			{
				var value = await cache.GetOrLoadAsync(key);
				Assert.AreNotEqual(firstHitValues[key], value);
			}
		}

		[TestMethod]
		public async Task Sample2WithFluentApi()
		{
			// This func is to fetch the value from the data source. In this case it 
			// is just a random generator, but this could be anything like a web service or database.
			// The parameter should be the same type as your cache key type, and the return value
			// should be the same type as your cache's value type.
			Func<int, Task<string>> loaderFunction = async (key) =>
			{
				int rnd = new Random().Next();
				return await Task.FromResult(string.Format("value{0}", rnd));
			};

			var cache = CacheBuilder.Build<int, string>()
				.WithName(nameof(Sample2WithFluentApi))
				.CacheItemExpiryFromSeconds(1)
				.CacheItemExpiryPercentageRandomization(0)
				.FlushIntervalFromSeconds(5)
				.MaximumCacheSizeIndicator(100)
				.CircuitBreakerTimeoutForAdditionalThreadsPerKeyFromSeconds(10)
				.DisposeExpiredValuesIfDisposable()
				.LoaderFunction(loaderFunction)
				.Create();

			// Record the first hit values for 100 keys
			var firstHitValues = new List<string>();
			for (int key = 0; key < 100; key++)
			{
				var value = await cache.GetOrLoadAsync(key);
				firstHitValues.Add(value);
			}

			// Assert that 50 subsequent hits return the same values for all 100 keys.
			for (int i = 0; i < 50; i++)
			{
				for (int key = 0; key < 100; key++)
				{
					var value = await cache.GetOrLoadAsync(key);
					Assert.AreEqual(firstHitValues[key], value);
				}
			}

			// Wait a while so that all cache entries expire as per the configured options
			Thread.Sleep(1000);

			// Assert that the next hit returns fresh values. i.e. not equal to the first hit values. 
			for (int key = 0; key < 100; key++)
			{
				var value = await cache.GetOrLoadAsync(key);
				Assert.AreNotEqual(firstHitValues[key], value);
			}
		}

		[TestMethod]
		public async Task Sample3WithFluentApiAndRandomItemExpiry()
		{
			// This func is to fetch the value from the data source. In this case it 
			// is just a random generator, but this could be anything like a web service or database.
			// The parameter should be the same type as your cache key type, and the return value
			// should be the same type as your cache's value type.
			Func<int, Task<string>> loaderFunction = async (key) =>
			{
				int rnd = new Random().Next();
				return await Task.FromResult(string.Format("value{0}", rnd));
			};

			int expiryMs = 1000;
			var cache = CacheBuilder.Build<int, string>()
				.WithName(nameof(Sample3WithFluentApiAndRandomItemExpiry))
				.CacheItemExpiryFromMilliseconds(expiryMs)
				.CacheItemExpiryPercentageRandomization(100)
				.FlushIntervalFromSeconds(5)
				.MaximumCacheSizeIndicator(100)
				.CircuitBreakerTimeoutForAdditionalThreadsPerKeyFromSeconds(10)
				.DisposeExpiredValuesIfDisposable()
				.LoaderFunction(loaderFunction)
				.Create();

			// Record the first hit values for 100 keys
			var firstHitValues = new List<string>();
			for (int key = 0; key < 100; key++)
			{
				var value = await cache.GetOrLoadAsync(key);
				firstHitValues.Add(value);
			}

			// Assert that 10 subsequent hits return the same values for all 100 keys.
			for (int i = 0; i < 10; i++)
			{
				for (int key = 0; key < 100; key++)
				{
					var value = await cache.GetOrLoadAsync(key);
					value.Should().Be(firstHitValues[key]);
				}
			}

			// Wait the expiry time so that about half the cache entries randomly expire as per the configured options
			await Task.Delay(expiryMs);

			// Assert that the next hit returns fresh values. i.e. not equal to the first hit values. 
			int oldValueCount = 0;
			int newValueCount = 0;
			for (int key = 0; key < 100; key++)
			{
				var value = await cache.GetOrLoadAsync(key);
				if (value == firstHitValues[key])
					oldValueCount++;
				else
					newValueCount++;
			}

			// Statistically, it should be about 50/50 split, but for this test to pass reliably, we considder 30% on either side success.
			oldValueCount.Should().BeGreaterOrEqualTo(30, "about half should have not yet expired");
			newValueCount.Should().BeGreaterOrEqualTo(30, "about half should have expired already");
		}


	}
}
