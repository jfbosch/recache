using System;
using System.Runtime.Serialization;

namespace ReCache
{
	[Serializable]
	public class CacheOptionsException : ArgumentException
	{
		public CacheOptionsException() { }

		public CacheOptionsException(string message)
			: base(message)
		{
		}

		public CacheOptionsException(string message, string fullyQualifiedName, Exception inner) : base(message, inner)
		{
		}

		protected CacheOptionsException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }

	}
}
