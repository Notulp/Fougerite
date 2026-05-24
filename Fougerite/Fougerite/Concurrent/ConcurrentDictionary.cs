using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// A thread-safe implementation of a Dictionary for .NET 3.5 using ReaderWriterLock.
    /// Allows multiple concurrent readers or a single exclusive writer.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class ConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly ReaderWriterLock _padlock = new ReaderWriterLock();
        private readonly Dictionary<TKey, TValue> _dictionary;
        
        /// <summary>
        /// Initializes a new, empty instance of the ConcurrentDictionary class.
        /// </summary>
        public ConcurrentDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes a new, empty instance of the ConcurrentDictionary class that has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
        public ConcurrentDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        /// <summary>
        /// Initializes a new, empty instance of the ConcurrentDictionary class that uses the specified IEqualityComparer.
        /// </summary>
        /// <param name="comparer">The IEqualityComparer implementation to use when comparing keys.</param>
        public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Initializes a new, empty instance of the ConcurrentDictionary class with specified capacity and IEqualityComparer.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
        /// <param name="comparer">The IEqualityComparer implementation to use when comparing keys.</param>
        public ConcurrentDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentDictionary class containing elements copied from the specified dictionary.
        /// </summary>
        /// <param name="originalDict">The dictionary whose elements are copied to the new ConcurrentDictionary.</param>
        public ConcurrentDictionary(Dictionary<TKey, TValue> originalDict)
        {
            if (originalDict == null) throw new ArgumentNullException(nameof(originalDict));
            _dictionary = new Dictionary<TKey, TValue>(originalDict);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the specified key.</returns>
        public TValue this[TKey key]
        {
            get
            {
                _padlock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _dictionary[key];
                }
                finally
                {
                    _padlock.ReleaseReaderLock();
                }
            }
            set
            {
                _padlock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _dictionary[key] = value;
                }
                finally
                {
                    _padlock.ReleaseWriterLock();
                }
            }
        }
        
        /// <summary>
        /// Returns an enumerator that iterates through a snapshot of the dictionary.
        /// This public method is required for Jint 'for...in' loops to function correctly.
        /// </summary>
        /// <returns>An enumerator for the dictionary contents.</returns>
        public IEnumerator GetEnumerator()
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                // ToList() creates a snapshot to prevent "Collection Modified" exceptions during iteration
                return ((IEnumerable)_dictionary.ToList()).GetEnumerator();
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }
        
        /// <summary>
        /// Safely retrieves an item by key. Returns default(TValue) if the key is not found.
        /// Useful for Jint scripts to avoid KeyNotFound exceptions.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The value if found, otherwise null (or default).</returns>
        public TValue GetItem(TKey key)
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _dictionary.ContainsKey(key) ? _dictionary[key] : default(TValue);
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Returns a standard, non-thread-safe Dictionary containing the current elements.
        /// </summary>
        /// <returns>A shallow copy of the internal dictionary.</returns>
        public Dictionary<TKey, TValue> GetShallowCopy()
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return new Dictionary<TKey, TValue>(_dictionary);
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value, otherwise, the default value.</param>
        /// <returns>true if the key was found, otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _dictionary.TryGetValue(key, out value);
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>true if the key/value pair was added, false if the key already exists.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            _padlock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (!_dictionary.ContainsKey(key))
                {
                    _dictionary.Add(key, value);
                    return true;
                }
                return false;
            }
            finally
            {
                _padlock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the object was removed successfully, otherwise, false.</returns>
        public bool TryRemove(TKey key)
        {
            _padlock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_dictionary.ContainsKey(key))
                {
                    return _dictionary.Remove(key);
                }
                return false;
            }
            finally
            {
                _padlock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        public void Remove(TKey key)
        {
            _padlock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary.Remove(key);
                }
            }
            finally
            {
                _padlock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Forcibly adds a key/value pair. Throws exception if key exists.
        /// </summary>
        public void Add(TKey key, TValue val)
        {
            _padlock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _dictionary.Add(key, val);
            }
            finally
            {
                _padlock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="id">The key to locate.</param>
        /// <returns>true if the key is found, otherwise, false.</returns>
        public bool ContainsKey(TKey id)
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _dictionary.ContainsKey(id);
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }
        
        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            _padlock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                _dictionary.Clear();
            }
            finally
            {
                _padlock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Returns a sorted list of the dictionary elements based on a key.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> OrderBy(Func<KeyValuePair<TKey, TValue>, TKey> func)
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _dictionary.OrderBy(func).ToList();
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }
        
        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// Exposed as a property for Jint compatibility.
        /// </summary>
        public int Count
        {
            get 
            {
                _padlock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _dictionary.Count;
                }
                finally
                {
                    _padlock.ReleaseReaderLock();
                }
            }
        }
        
        /// <summary>
        /// Returns a new List containing all the values in the dictionary.
        /// </summary>
        public List<TValue> ValuesCopy
        {
            get
            {
                _padlock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return new List<TValue>(_dictionary.Values);
                }
                finally
                {
                    _padlock.ReleaseReaderLock();
                }
            }
        }
        
        /// <summary>
        /// Returns a new List containing all the keys in the dictionary.
        /// </summary>
        public List<TKey> KeysCopy
        {
            get
            {
                _padlock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return new List<TKey>(_dictionary.Keys);
                }
                finally
                {
                    _padlock.ReleaseReaderLock();
                }
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// NOTE: Iterating this outside a lock is thread-unsafe. Use ValuesCopy for safe iteration.
        /// </summary>
        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                _padlock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _dictionary.Values;
                }
                finally
                {
                    _padlock.ReleaseReaderLock();
                }
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// NOTE: Iterating this outside a lock is thread-unsafe. Use KeysCopy for safe iteration.
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                _padlock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _dictionary.Keys;
                }
                finally
                {
                    _padlock.ReleaseReaderLock();
                }
            }
        }

        /// <summary>
        /// Explicit implementation of the generic enumerator.
        /// </summary>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                // ToList() prevents "Collection was modified" exceptions
                return _dictionary.ToList().GetEnumerator();
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }
        
        /// <summary>
        /// Explicit implementation of the untyped enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            _padlock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _dictionary.ToList().GetEnumerator();
            }
            finally
            {
                _padlock.ReleaseReaderLock();
            }
        }
    }
}