using System;
using System.Runtime.Serialization;

namespace ReCache
{
	[Serializable]
	public class CircuitBreakerTimeoutException : Exception
	{
		public CircuitBreakerTimeoutException() { }

		public CircuitBreakerTimeoutException(string message)
			: base(message)
		{
		}

		public CircuitBreakerTimeoutException(string message, string fullyQualifiedName, Exception inner) : base(message, inner)
		{
		}

		protected CircuitBreakerTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }

	}
}
