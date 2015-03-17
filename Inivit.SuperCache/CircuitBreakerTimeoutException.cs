using System;

namespace Inivit.SuperCache
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