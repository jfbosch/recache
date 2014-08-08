using Inivit.SuperCache;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Tests
{
	[TestClass]
	public class SampleUsage
	{
		[TestMethod]
		public void Sample1()
		{
			// This func is to fetch the value from the data source. In this case it 
			// is just a random generator, but this could be anything like a web service or database.
			// The parameter should be the same type as your cache key type, and the return value
			// should be the same type as your cache's value type.
			Func<int, string> loaderFunction = (key) =>
			{
				int rnd = new Random().Next();
				return string.Format("value{0}", rnd);
			};

			var cacheOptions = new CacheOptions
			{
				CacheItemExpiry = TimeSpan.FromSeconds(1),
				FlushInterval = TimeSpan.FromSeconds(5),
				MaximumCacheSizeIndicator = 100
			};
			var cache = new Cache<int, string>(
				cacheOptions, loaderFunction);


			// Record the first hit values for 100 keys
			var firstHitValues = new List<string>();
			for (int key = 0; key < 100; key++)
			{
				var value = cache.GetOrLoad(key);
				firstHitValues.Add(value);
			}

			// Assert that 50 subsequent hits return the same values for all 100 keys.
			for (int i = 0; i < 50; i++)
			{
				for (int key = 0; key < 100; key++)
				{
					var value = cache.GetOrLoad(key);
					Assert.AreEqual(firstHitValues[key], value);
				}
			}

			// Wait a while so that all cache entries expire as per the configured options
			Thread.Sleep(1000);

			// Assert that the next hit returns fresh values. i.e. not equal to the first hit values. 
			for (int key = 0; key < 100; key++)
			{
				var value = cache.GetOrLoad(key);
				Assert.AreNotEqual(firstHitValues[key], value);
			}


		}

		[TestMethod]
		public void Sample2WithFluentApi()
		{
			// This func is to fetch the value from the data source. In this case it 
			// is just a random generator, but this could be anything like a web service or database.
			// The parameter should be the same type as your cache key type, and the return value
			// should be the same type as your cache's value type.
			Func<int, string> loaderFunction = (key) =>
			{
				int rnd = new Random().Next();
				return string.Format("value{0}", rnd);
			};

			var cache = CacheBuilder.Build<int, string>()
				.CacheItemExpiry(TimeSpan.FromSeconds(1))
				.FlushInterval(TimeSpan.FromSeconds(5))
				.MaximumCacheSizeIndicator (100)
				.LoaderFunction(loaderFunction)
				.Create();

			// Record the first hit values for 100 keys
			var firstHitValues = new List<string>();
			for (int key = 0; key < 100; key++)
			{
				var value = cache.GetOrLoad(key);
				firstHitValues.Add(value);
			}

			// Assert that 50 subsequent hits return the same values for all 100 keys.
			for (int i = 0; i < 50; i++)
			{
				for (int key = 0; key < 100; key++)
				{
					var value = cache.GetOrLoad(key);
					Assert.AreEqual(firstHitValues[key], value);
				}
			}

			// Wait a while so that all cache entries expire as per the configured options
			Thread.Sleep(1000);

			// Assert that the next hit returns fresh values. i.e. not equal to the first hit values. 
			for (int key = 0; key < 100; key++)
			{
				var value = cache.GetOrLoad(key);
				Assert.AreNotEqual(firstHitValues[key], value);
			}
		}


	}
}
