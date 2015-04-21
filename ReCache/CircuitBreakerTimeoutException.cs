using System;

namespace ReCache
{
	[Serializable]
	public class CircuitBreakerTimeoutException : Exception
	{
		public CircuitBreakerTimeoutException(string message)
			: base(message)
		{
		}
	}

}