using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReCache;
using System;

namespace Tests
{
	[TestClass]
	public class OptionsTests
	{
		[TestInitialize()]
		public void Initialize()
		{
		}

		[TestMethod]
		public void TestCacheItemExpiryPercentageRandomizationWithDefault()
		{
			WithDefault(TimeSpan.FromSeconds(1)).Should().Be(100);
			WithDefault(TimeSpan.FromSeconds(60)).Should().Be(6000);
			WithDefault(TimeSpan.FromSeconds(59)).Should().Be(5900);
			WithDefault(TimeSpan.FromMilliseconds(50)).Should().Be(5);
			WithDefault(TimeSpan.FromMilliseconds(10)).Should().Be(1);
		}

		private static int WithDefault(TimeSpan span)
		{
			var op = new CacheOptions
			{
				CacheItemExpiry = span,
			};
			op.Initialize();
			return op.CacheItemExpiryPercentageRandomizationMilliseconds;
		}

		[TestMethod]
		public void TestCacheItemExpiryPercentageRandomizationWithSpecifiedPercentage()
		{
			WithSpecific(TimeSpan.FromMilliseconds(10), 0).Should().Be(0);
			WithSpecific(TimeSpan.FromSeconds(10), 0).Should().Be(0);
			WithSpecific(TimeSpan.FromMinutes(10), 0).Should().Be(0);
			WithSpecific(TimeSpan.FromHours(10), 0).Should().Be(0);
			WithSpecific(TimeSpan.FromDays(10), 0).Should().Be(0);

			WithSpecific(TimeSpan.FromMilliseconds(10), 50).Should().Be(5);
			WithSpecific(TimeSpan.FromSeconds(60), 50).Should().Be(30000);
			WithSpecific(TimeSpan.FromSeconds(60), 100).Should().Be(60000);
			WithSpecific(TimeSpan.FromSeconds(100), 99).Should().Be(99000);
			WithSpecific(TimeSpan.FromDays(10), 2).Should().Be(17280000);
		}

		private static int WithSpecific(TimeSpan span, int percentage)
		{
			var op = new CacheOptions
			{
				CacheItemExpiry = span,
				CacheItemExpiryPercentageRandomization = percentage,
			};
			op.Initialize();
			return op.CacheItemExpiryPercentageRandomizationMilliseconds;
		}

		[TestMethod]
		public void TestCacheItemExpiryPercentageRandomizationWithSpecifiedBadPercentage()
		{
			Action act = () => WithSpecific(TimeSpan.FromMilliseconds(10), -1);
			act.ShouldThrow<CacheOptionsException>().Where((x) => x.Message.Contains("It is currently set to the unsupported value of -1."));
			act = () => WithSpecific(TimeSpan.FromMilliseconds(10), 101);
			act.ShouldThrow<CacheOptionsException>().Where((x) => x.Message.Contains("It is currently set to the unsupported value of 101."));
		}

	}
}