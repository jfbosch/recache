using System;
using System.Globalization;

namespace ReCache
{
	internal static class Extensions
	{
		public static decimal SafeDivideBy(this decimal value, decimal divideBy, decimal valueIfZero = default(decimal))
		{
			if (divideBy == 0) return valueIfZero;

			return value / divideBy;
		}

		public static float SafeDivideBy(this float value, float divideBy, float valueIfZero = default(float))
		{
			if (divideBy == 0) return valueIfZero;

			return value / divideBy;
		}

		public static decimal SafeDivideBy(this long value, decimal divideBy, decimal valueIfZero = default(decimal))
		{
			if (divideBy == 0) return valueIfZero;

			return value / divideBy;
		}

		/// <summary>
		/// Formats the string by delegating to <see cref="String.Format"/> .
		/// It is called FormatI (I = instance) because there is already a static Format
		/// method with the same parameters on the string type (<see cref="string.Format"/> ),
		/// and the compiler gets confused when you call Format on an instance of a string when the
		/// first params parameter is a string.
		/// </summary>
		public static string FormatWith(
			this string value,
			params object[] args)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			return string.Format(CultureInfo.InvariantCulture, value, args);
		}

	}
}
