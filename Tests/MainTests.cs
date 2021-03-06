﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Tests
{
	[TestClass]
	public class MainTests
	{
		[TestInitialize()]
		public void Initialize()
		{
		}

		protected async Task<string> IntLoaderFunc(int key)
		{
			return await Task.FromResult(key.ToString());
		}

		[TestMethod]
		public virtual async Task TestStringAsKey()
		{
			var _backendStore = new Dictionary<string, int>();
			_backendStore.Add("", 0);
			_backendStore.Add("a", 1);
			_backendStore.Add("b", 2);
			_backendStore.Add("abc", 3);

			Func<string, Task<int>> loaderFunc = async (key) =>
			{
				var val = _backendStore[key];
				// Remove it, so that an exception is thrown if the backend store gets accessed more than once.
				_backendStore.Remove(key);
				return await Task.FromResult(val);
			};

			var _cache = new Cache<string, int>(
				new CacheOptions
				{
					CacheItemExpiry = TimeSpan.FromSeconds(1),
					FlushInterval = TimeSpan.FromMinutes(1),
					MaximumCacheSizeIndicator = 10000
				},
				loaderFunc);

			(await _cache.GetOrLoadAsync("", false)).Should().Be(0);
			(await _cache.GetOrLoadAsync("", false)).Should().Be(0);
			(await _cache.GetOrLoadAsync("a", false)).Should().Be(1);
			(await _cache.GetOrLoadAsync("a", false)).Should().Be(1);
			(await _cache.GetOrLoadAsync("abc", false)).Should().Be(3);
		}

		[TestMethod]
		public virtual async Task TestEnumerableStringAsKeyAndThatLoaderFuncOnlyGetsHitOnce()
		{
			var _backendStore = new Dictionary<IEnumerable<string>, int>(new EnumerableStringComparer());
			_backendStore.Add(Enumerable.Empty<string>(), 0);
			_backendStore.Add(new string[] { "" }, 1);
			_backendStore.Add(new string[] { "a" }, 2);
			_backendStore.Add(new string[] { "a", "b" }, 3);

			Func<IEnumerable<string>, Task<int>> loaderFunc = async (key) =>
			{
				var val = _backendStore[key];
				// Remove it, so that an exception is thrown if the backend store gets accessed more than once.
				_backendStore.Remove(key);
				return await Task.FromResult(val);
			};

			var _cache = new Cache<IEnumerable<string>, int>(
				new EnumerableStringComparer(),
				new CacheOptions
				{
					CacheItemExpiry = TimeSpan.FromSeconds(1),
					FlushInterval = TimeSpan.FromMinutes(1),
					MaximumCacheSizeIndicator = 10000
				},
				loaderFunc);

			(await _cache.GetOrLoadAsync(Enumerable.Empty<string>(), false)).Should().Be(0);
			(await _cache.GetOrLoadAsync(new string[] { }, false)).Should().Be(0);
			(await _cache.GetOrLoadAsync(new string[] { "" }, false)).Should().Be(1);
			(await _cache.GetOrLoadAsync(new string[] { "a" }, false)).Should().Be(2);
			(await _cache.GetOrLoadAsync(new string[] { "a", "b" }, false)).Should().Be(3);
		}

		[TestMethod]
		public async Task StaleEntriesShouldBeAutoFlushed()
		{
			var _cache = new Cache<int, string>(
				new CacheOptions
				{
					CacheItemExpiry = TimeSpan.FromSeconds(1),
					FlushInterval = TimeSpan.FromMilliseconds(100),
					MaximumCacheSizeIndicator = 1000
				},
				IntLoaderFunc);

			Parallel.For(0, 1000, async (i) => await _cache.GetOrLoadAsync(i));
			_cache.Count.Should().Be(1000);
			Thread.Sleep(500);
			_cache.Count.Should().Be(1000);
			Thread.Sleep(700);
			_cache.Count.Should().Be(0);
			await Task.Yield();
		}

		[TestMethod]
		public async Task EntriesShouldNotBeFlushedIfExtendWasUsed()
		{
			var _cache = new Cache<int, string>(
				new CacheOptions
				{
					CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.FromSeconds(1),
					CacheItemExpiry = TimeSpan.FromMilliseconds(200),
					FlushInterval = TimeSpan.FromMilliseconds(50),
					MaximumCacheSizeIndicator = 1000
				},
				IntLoaderFunc);

			Parallel.For(0, 100, async (i) => await _cache.GetOrLoadAsync(i));
			_cache.Count.Should().Be(100);
			await Task.Delay(100);
			Parallel.For(0, 50, async (i) => await _cache.GetOrLoadAsync(i));
			_cache.Count.Should().Be(100);
			await Task.Delay(150);
			Parallel.For(0, 50, async (i) => await _cache.GetOrLoadAsync(i));
			_cache.Count.Should().Be(50);
		}

		[TestMethod]
		public async Task StaleEntriesShouldNotBeFlushedIfFlushIntervalIsNotReached()
		{
			var _cache = new Cache<int, string>(
				new CacheOptions
				{
					CacheItemExpiry = TimeSpan.FromMilliseconds(10),
					FlushInterval = TimeSpan.FromMilliseconds(500),
					MaximumCacheSizeIndicator = 1000
				},
				IntLoaderFunc);


			for (int i = 0; i < 100; i++)
				await _cache.GetOrLoadAsync(i);

			_cache.Count.Should().Be(100);
			Thread.Sleep(600);
			_cache.Count.Should().Be(0);
		}

		//[TestMethod]
		public async Task ShouldTimeoutAndAbortFirstCallPerKey()
		{
			//TODO: do we need this test?
			await Task.Delay(1);
			throw new NotImplementedException();
			//int numberOfLoaderCalls = await Task.FromResult(0);
			//var cache = new Cache<int, string>(
			//	new CacheOptions
			//	{
			//		LoaderFuncTimeout = TimeSpan.FromMilliseconds(500),
			//		CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.MaxValue,
			//		CacheItemExpiry = TimeSpan.FromMinutes(1),
			//		FlushInterval = TimeSpan.FromMilliseconds(5000),
			//		MaximumCacheSizeIndicator = 1000
			//	},
			//	async (key) =>
			//	{
			//		if (key == 1)
			//			Thread.Sleep(50);
			//		else
			//			//Thread.Sleep(10000);
			//			await Task.Delay(10000);

			//		Interlocked.Increment(ref numberOfLoaderCalls);
			//		return await Task.FromResult(key.ToString());
			//	});

			//int exceptionCount = 0;
			//var options = new ParallelOptions() { MaxDegreeOfParallelism = 15 };
			//Parallel.For(1, 3, options, async (i) =>
			//{
			//	try
			//	{
			//		(await cache.GetOrLoadAsync(i)).Should().Be(i.ToString());
			//	}
			//	catch ()
			//	{
			//		Interlocked.Increment(ref exceptionCount);
			//	}
			//});

			//numberOfLoaderCalls.Should().Be(1, "loader func should only have completed once by the time the assertion is hit.");
			//exceptionCount.Should().Be(1, "the second loader func should time out.");
		}

		[TestMethod]
		public async Task CircuitBreakerShouldOnlyPassThroughFirstThreadRequestAndShouldBlockOtherThreadsAndShareResult()
		{
			var random = new Random();
			int numberOfLoaderCalls = await Task.FromResult(0);
			var cache = new Cache<int, string>(
				new CacheOptions
				{
					CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.MaxValue,
					CacheItemExpiry = TimeSpan.FromMinutes(1),
					FlushInterval = TimeSpan.FromMilliseconds(5000),
					MaximumCacheSizeIndicator = 1000
				},
				async (key) =>
				{
					Thread.Sleep(random.Next(50));
					Interlocked.Increment(ref numberOfLoaderCalls);
					return await Task.FromResult(key.ToString());
				});

			int testKey = 7;
			string testValue = testKey.ToString();

			var options = new ParallelOptions() { MaxDegreeOfParallelism = 15 };
			Parallel.For(0, 500, options, async (i) =>
			{
				switch (i)
				{
					case 100:
					case 200:
					case 300:
					case 400:
						// Fetch i as the key (we only ask for each of these keys once).
						(await cache.GetOrLoadAsync(i)).Should().Be(i.ToString());
						break;
					default: // For all others, fetch the same test key.
						(await cache.GetOrLoadAsync(testKey)).Should().Be(testValue);
						break;
				}
			});

			numberOfLoaderCalls.Should().Be(5, "1 for each of the hundreds (100 to 400), and 1 for the test key");
		}

		[TestMethod]
		public async Task CircuitBreakerShouldOnlyPassThroughFirstThreadRequestAndShouldThrowForOtherThreadsAfterTimeout_Loop()
		{
			for (int i = 0; i < 150; i++)
				await CircuitBreakerShouldOnlyPassThroughFirstThreadRequestAndShouldThrowForOtherThreadsAfterTimeout();
		}

		[TestMethod]
		public async Task CircuitBreakerShouldOnlyPassThroughFirstThreadRequestAndShouldThrowForOtherThreadsAfterTimeout()
		{
			var random = new Random();
			await CircuitBreakerShouldOnlyPassThroughFirstThreadRequestAndShouldThrowForOtherThreadsAfterTimeout(random, TimeSpan.Zero);
			await CircuitBreakerShouldOnlyPassThroughFirstThreadRequestAndShouldThrowForOtherThreadsAfterTimeout(random, TimeSpan.FromMilliseconds(5));
		}

		private static async Task CircuitBreakerShouldOnlyPassThroughFirstThreadRequestAndShouldThrowForOtherThreadsAfterTimeout(Random random, TimeSpan timeout)
		{
			int numberOfLoaderCallInitiations = await Task.FromResult(0);
			int numberOfLoaderCalls = await Task.FromResult(0);

			using (var cache = new Cache<int, string>(
				new CacheOptions
				{
					CircuitBreakerTimeoutForAdditionalThreadsPerKey = timeout,
					CacheItemExpiry = TimeSpan.FromSeconds(120),
					FlushInterval = TimeSpan.FromSeconds(5),
					MaximumCacheSizeIndicator = 1000
				},
				async (key) =>
				{
					Interlocked.Increment(ref numberOfLoaderCallInitiations);
					await Task.Delay(random.Next(30, 50)).ConfigureAwait(false);
					Interlocked.Increment(ref numberOfLoaderCalls);
					return await Task.FromResult(key.ToString()).ConfigureAwait(false);
				}))
			{
				int numberOfCacheRequestsShortCircuited = 0;
				int testKey = 7;
				string testValue = testKey.ToString();

				ActionBlock<int> block = new ActionBlock<int>(async (i) =>
				{
					try
					{
						switch (i)
						{
							case 100:
							case 200:
							case 300:
							case 400:
								// Fetch i as the key
								var v = await cache.GetOrLoadAsync(i);
								v.Should().Be(i.ToString());
								break;
							default: // For all others, fetch the same test key.
								var v2 = await cache.GetOrLoadAsync(testKey);
								v2.Should().Be(testKey.ToString());
								break;
						}
					}
					catch (CircuitBreakerTimeoutException)
					{
						Interlocked.Increment(ref numberOfCacheRequestsShortCircuited);
					}
				}, new ExecutionDataflowBlockOptions
				{
					MaxDegreeOfParallelism = 15
				});

				var options = new ParallelOptions() { MaxDegreeOfParallelism = 15 };
				Parallel.For(0, 500, options, (i) =>
				{
					block.Post(i);
				});
				block.Complete();
				await block.Completion.ConfigureAwait(false);

				int num = 5;
				numberOfLoaderCallInitiations.Should().Be(num, "1 initiation for each of the hundreds (100 to 400), and 1 for the test key");
				numberOfLoaderCalls.Should().Be(num, "1 for each of the hundreds (100 to 400), and 1 for the test key");
				numberOfCacheRequestsShortCircuited.Should().BeGreaterThan(4, "At least some should be too fast (i.e. hit while the key's value is still busy caching), and because the timeout is set very short, it should fail");
			}
		}

		[TestMethod]
		public async Task SecondColdHitOnSameKeyShouldBubbleLoaderExceptionAfterFirstLoaderFailure()
		{
			// We are basically testing that the second call to the same key also bubbles the loader func's
			// exception if the first hit on the key also got a loader func exception. We do not want the
			// second hit to show up as a circuite breaker exception.

			int numberOfLoaderCallInitiations = await Task.FromResult(0);

			using (var cache = new Cache<int, string>(
				new CacheOptions
				{
					CacheName = "MyCache",
					CircuitBreakerTimeoutForAdditionalThreadsPerKey = TimeSpan.FromMilliseconds(100),
					CacheItemExpiry = TimeSpan.FromSeconds(120),
					FlushInterval = TimeSpan.FromSeconds(5),
					MaximumCacheSizeIndicator = 1000
				},
				async (key) =>
				{
					Interlocked.Increment(ref numberOfLoaderCallInitiations);
					await Task.Delay(150).ConfigureAwait(false);
					throw new Exception("Loader Func Failure");
				}))
			{
				cache.CacheName = "NewCacheName";
				int testKey = 7;

				var t1 = cache.GetOrLoadAsync(testKey);
				await Task.Delay(80);
				var t2 = cache.GetOrLoadAsync(testKey);

				bool hitFuncException = false;
				try
				{
					await t2.ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					if (ex.Message == "Loader Func Failure")
						hitFuncException = true;
				}
				hitFuncException.Should().BeTrue();

				numberOfLoaderCallInitiations.Should().Be(2);
			}
		}



		[TestMethod]
		public async Task ShouldNotDeadlock_Loop()
		{
			for (int i = 0; i < 150; i++)
				await FuncShouldNotDeadlock();
		}

		[TestMethod]
		public async Task FuncShouldNotDeadlock()
		{
			var random = new Random();
			int numberOfLoaderCallInitiations = await Task.FromResult(0);
			int numberOfLoaderCalls = await Task.FromResult(0);

			Func<int, Task<string>> loaderFunc = async (key) =>
			{
				Interlocked.Increment(ref numberOfLoaderCallInitiations);
				//Thread.Sleep(random.Next(30, 50));
				await Task.Delay(random.Next(30, 50)).ConfigureAwait(false);
				Interlocked.Increment(ref numberOfLoaderCalls);
				return await Task.FromResult(key.ToString()).ConfigureAwait(false);
			};

			Func<int, Task<string>> cacheFunc = async (key) =>
			{
				await Task.FromResult(0);
				//Does not deadlock
				//return loaderFunc(key).Result;
				//Deadlocks
				return await loaderFunc(key).ConfigureAwait(false);
			};

			int testKey = 7;

			int num = 1;
			var options = new ParallelOptions() { MaxDegreeOfParallelism = 15 };
			Parallel.For(0, num, options, (i) =>
			{
				var v = cacheFunc(testKey).Result;
				v.Should().Be(testKey.ToString());
			});

			numberOfLoaderCallInitiations.Should().Be(num, "initiations should match");
			numberOfLoaderCalls.Should().Be(num, "completions should match");
		}

		[TestMethod]
		public async Task EntriesExceedingMaxShouldBeAutoFlushedEvenIfNotStale()
		{
			var cache = new Cache<int, string>(
				new CacheOptions
				{
					CacheItemExpiry = TimeSpan.FromMinutes(1),
					FlushInterval = TimeSpan.FromMilliseconds(500),
					MaximumCacheSizeIndicator = 99
				},
				IntLoaderFunc);

			for (int i = 0; i < 200; i++)
				await cache.GetOrLoadAsync(i);

			cache.Count.Should().Be(200);
			Thread.Sleep(1500);
			cache.Count.Should().Be(99);
		}

		[TestMethod]
		public async Task TestCacheFlushCallback()
		{
			var _cache = new Cache<int, string>(
				new CacheOptions
				{
					CacheName = nameof(TestCacheFlushCallback),
					CacheItemExpiry = TimeSpan.FromSeconds(1),
					FlushInterval = TimeSpan.FromMilliseconds(500),
					MaximumCacheSizeIndicator = 1000
				},
				IntLoaderFunc);

			int flushCallbackRaised = 0;

			_cache.MissedCallback = (key, cacheEntry, durationMilliseconds) =>
			{
			};

			_cache.FlushCallback = (count, itemsRemoved, millisecondElapsed) =>
			{
				//Console.WriteLine("flushCallBackRaised...count :{0}, itemsRemoved: {1}, clientContext : {2}, millisecondElapsed: {3} ", count, itemsRemoved, clientContext, millisecondElapsed);
				Interlocked.Increment(ref flushCallbackRaised);
			};

			Parallel.For(0, 1000, (i) => _cache.GetOrLoadAsync(i).Wait());
			_cache.Count.Should().Be(1000);
			await Task.Delay(700);
			_cache.Count.Should().Be(1000);
			await Task.Delay(1500);
			_cache.Count.Should().Be(0);
			flushCallbackRaised.Should().BeGreaterOrEqualTo(4);
		}

		[TestMethod]
		public async Task SelfRefreshingCacheEntriesExceedingMaxShouldBeAutoFlushedOnRefreshEvenIfNotStale()
		{
			var _cache = new SelfRefreshingCache<int, string>(
				new SelfRefreshingCacheOptions
				{
					RefreshInterval = TimeSpan.FromMilliseconds(600),
					StandardCacheOptions = new CacheOptions
					{
						CacheItemExpiry = TimeSpan.FromMinutes(1),
						FlushInterval = TimeSpan.FromMilliseconds(50000),
						MaximumCacheSizeIndicator = 99
					}
				},
				IntLoaderFunc);

			for (int i = 0; i < 200; i++)
				await _cache.GetOrLoadAsync(i);

			_cache.Count.Should().Be(200);
			Thread.Sleep(700);
			_cache.Count.Should().Be(99);
		}

		[TestMethod]
		public async Task SelfrefreshingCacheShouldPurgedMoreRecentlyAccessedEntriesLastWhileRefreshing()
		{
			int refreshMs = 50;
			var _cache = new SelfRefreshingCache<int, string>(
				new SelfRefreshingCacheOptions
				{
					RefreshInterval = TimeSpan.FromMilliseconds(refreshMs),
					StandardCacheOptions = new CacheOptions
					{
						CacheItemExpiry = TimeSpan.FromMinutes(1),
						FlushInterval = TimeSpan.FromHours(1),
						MaximumCacheSizeIndicator = 90
					}
				},
				IntLoaderFunc);

			for (int i = 1; i <= 199; i++)
				await _cache.GetOrLoadAsync(i);

			_cache.Count.Should().Be(199);

			Thread.Sleep(10);
			// Refresh the access of key 100 to 190. (these should be the surviving 90 keys)
			for (int i = 100; i <= 190; i++)
				await _cache.GetOrLoadAsync(i);

			// Wait for refresh timer to refresh keys to the new generation
			Thread.Sleep(refreshMs + 20);
			_cache.Count.Should().Be(90);
			foreach (var item in _cache.Items.Reverse())
			{
				item.Key.Should().BeGreaterOrEqualTo(100);
				item.Key.Should().BeLessOrEqualTo(190);
			}
		}

		[TestMethod]
		public async Task TryAddToCacheShouldIncreaseItemsInList()
		{
			var _cache = new Cache<int, string>(
				new CacheOptions
				{
					CacheItemExpiry = TimeSpan.FromSeconds(1),
					FlushInterval = TimeSpan.FromMilliseconds(100),
					MaximumCacheSizeIndicator = 1000
				},
				IntLoaderFunc);

			Parallel.For(0, 5, async (i) => await _cache.GetOrLoadAsync(i));
			_cache.Count.Should().Be(5);

			_cache.TryAdd(6, "6");

			_cache.Count.Should().Be(6);
			await Task.Yield();
		}

		[TestMethod]
		public async Task ItemsPropertyShouldReturnListOfItemsInCache()
		{
			var _cache = new Cache<int, string>(
				new CacheOptions
				{
					CacheItemExpiry = TimeSpan.FromSeconds(1),
					FlushInterval = TimeSpan.FromMilliseconds(100),
					MaximumCacheSizeIndicator = 1000
				},
				IntLoaderFunc);

			Parallel.For(0, 5, async (i) => await _cache.GetOrLoadAsync(i));
			_cache.Count.Should().Be(5);

			_cache.Items.Should().NotBeNull();
			_cache.Items.Count().Should().Be(5);

			_cache.TryAdd(6, "6");

			_cache.Items.Should().NotBeNull();
			_cache.Items.Count().Should().Be(6);
			await Task.Yield();
		}

		[TestMethod]
		public async Task SelfRefreshingCacheEntriesTryAddToCacheShouldIncreaseItemsInList()
		{
			var _cache = new SelfRefreshingCache<int, string>(
				new SelfRefreshingCacheOptions
				{
					RefreshInterval = TimeSpan.FromMilliseconds(5),
					StandardCacheOptions = new CacheOptions
					{
						CacheItemExpiry = TimeSpan.FromMinutes(1),
						FlushInterval = TimeSpan.FromMilliseconds(50),
						MaximumCacheSizeIndicator = 99
					}
				},
				IntLoaderFunc);

			for (int i = 0; i < 5; i++)
				_cache.GetOrLoadAsync(i).Wait();

			_cache.Count.Should().Be(5);

			_cache.TryAdd(6, "6");

			_cache.Count.Should().Be(6);
			await Task.Yield();
		}

		[TestMethod]
		public async Task SelfRefreshingCacheItemsPropertyShouldReturnListOfItemsInCache()
		{
			var _cache = new SelfRefreshingCache<int, string>(
				new SelfRefreshingCacheOptions
				{
					RefreshInterval = TimeSpan.FromMilliseconds(50),
					StandardCacheOptions = new CacheOptions
					{
						CacheItemExpiry = TimeSpan.FromMinutes(1),
						FlushInterval = TimeSpan.FromMilliseconds(50),
						MaximumCacheSizeIndicator = 99
					}
				},
				IntLoaderFunc);

			Parallel.For(0, 5, (i) =>
			{
				_cache.GetOrLoadAsync(i).Wait();
				Thread.Sleep(1);
			});
			_cache.Count.Should().Be(5);

			_cache.Items.Should().NotBeNull();
			_cache.Items.Count().Should().Be(5);

			_cache.TryAdd(6, "6");

			_cache.Items.Should().NotBeNull();
			_cache.Items.Count().Should().Be(6);
			await Task.Yield();
		}


	}
}
