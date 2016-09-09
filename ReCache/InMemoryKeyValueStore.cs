using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReCache
{
	public class InMemoryKeyValueStore<TKey, TValue> : IKeyValueStore<TKey, TValue>
	{
		private readonly ConcurrentDictionary<TKey, TValue> _entries;

		public InMemoryKeyValueStore()
		{
			_entries = new ConcurrentDictionary<TKey, TValue>();
		}

		public InMemoryKeyValueStore(IEqualityComparer<TKey> comparer)
		{
			if (comparer == null)
				throw new ArgumentNullException(nameof(comparer));

			_entries = new ConcurrentDictionary<TKey, TValue>(comparer);
		}

		// Summary:
		//     Attempts to get the value associated with the specified key.
		//
		// Parameters:
		//   key:
		//     The key of the value to get.
		//
		//   value:
		//     When this method returns, contains the object 
		//     that has the specified key, or the default value of the type if the operation
		//     failed.
		//
		// Returns:
		//     true if the key was found.
		//     otherwise, false.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     key is null.
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return _entries.TryGetValue(key, out value);
		}

		// Summary:
		//     Adds a key/value pair.
		//     if the key does not already exist, or updates a key/value pair 
		//     by using the specified function if the key already exists.
		//
		// Parameters:
		//   key:
		//     The key to be added or whose value should be updated
		//
		//   addValue:
		//     The value to be added for an absent key
		//
		//   updateValueFactory:
		//     The function used to generate a new value for an existing key based on the key's
		//     existing value
		//
		// Returns:
		//     The new value for the key. This will be either be addValue (if the key was absent)
		//     or the result of updateValueFactory (if the key was present).
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     key or updateValueFactory is null.
		//
		//   T:System.OverflowException:
		//     The dictionary already contains the maximum number of elements (System.Int32.MaxValue).
		public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (updateValueFactory == null)
				throw new ArgumentNullException(nameof(updateValueFactory));

			return _entries.AddOrUpdate(key, addValue, updateValueFactory);
		}

		//
		// Summary:
		//     Attempts to remove and return the value that has the specified key.
		//
		// Parameters:
		//   key:
		//     The key of the element to remove and return.
		//
		//   value:
		//     When this method returns, contains the object removed.
		//     or the default value of the TValue type if key does not exist.
		//
		// Returns:
		//     true if the object was removed successfully; otherwise, false.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     key is null.
		public bool TryRemove(TKey key, out TValue value)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return _entries.TryRemove(key, out value);
		}

		//
		// Summary:
		//     Attempts to add the specified key and value.
		//
		// Parameters:
		//   key:
		//     The key of the element to add.
		//
		//   value:
		//     The value of the element to add. The value can be null for reference types.
		//
		// Returns:
		//     true if the key/value pair was added
		//     successfully; false if the key already exists.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     key is null.
		//
		//   T:System.OverflowException:
		//     The dictionary already contains the maximum number of elements (System.Int32.MaxValue).
		public bool TryAdd(TKey key, TValue value)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			return _entries.TryAdd(key, value);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _entries.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
	}
}
