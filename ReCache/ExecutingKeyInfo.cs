using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReCache
{
	internal sealed class ExecutingKeyInfo<TKey> : IDisposable
	{
		public TKey Key { get; private set; }
		public SemaphoreSlim Gate { get; private set; }

		public ExecutingKeyInfo(TKey key)
		{
			this.Key = key;
			this.Gate = new SemaphoreSlim(1, 1);
		}

		public void Dispose()
		{
			this.Gate.Dispose();
			GC.SuppressFinalize(this);
		}

	}
}
