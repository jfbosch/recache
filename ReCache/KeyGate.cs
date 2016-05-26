using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReCache
{
	internal sealed class KeyGate<TKey> : IDisposable
	{
		private bool _isDisposed = false;
		private readonly object _disposeLock = new object();

		internal TKey Key { get; private set; }
		internal SemaphoreSlim Lock { get; private set; }

		internal KeyGate(TKey key)
		{
			if (ReferenceEquals(null, key))
				throw new ArgumentNullException(nameof(key));

			this.Key = key;
			this.Lock = new SemaphoreSlim(1, 1);
		}

		public void Dispose()
		{
			lock (this._disposeLock)
			{
				if (!_isDisposed)
				{
					this.Lock.Dispose();
					GC.SuppressFinalize(this);
					_isDisposed = true;
				}
			}
		}

	}
}
