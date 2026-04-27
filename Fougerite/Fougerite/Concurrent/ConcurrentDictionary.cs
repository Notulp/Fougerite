using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// A .NET 3.5 implementation of ConcurrentDictionary, using ReaderWriterLock.
    /// You should read the microsoft docs on how a ConcurrentDictionary works.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly ReaderWriterLock _padlock = new ReaderWriterLock();
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
        
        /// <summary>
        /// Initializes a new, empty instance of the ConcurrentDictionary class.
        /// </summary>
        public ConcurrentDictionary()
        {
            
        }
        
        /// <summary>
        /// Initializes a new instance of the ConcurrentDictionary class containing elements from the specified dictionary.
        /// </summary>
        /// <param name="originalDict">The dictionary whose elements are copied to the new ConcurrentDictionary.</param>
        public ConcurrentDictionary(Dictionary<TKey, TValue> originalDict)
        {
            _dictionary = originalDict;
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
                lock (_padlock)
                {
                    return _dictionary[key];
                }
            }

            set
            {
                lock (_padlock)
                {
                    _dictionary[key] = value;
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
            lock (_padlock)
            {
                // ToList() creates a snapshot to prevent "Collection Modified" exceptions during iteration
                return ((IEnumerable)_dictionary.ToList()).GetEnumerator();
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
            lock (_padlock)
            {
                return _dictionary.ContainsKey(key) ? _dictionary[key] : default(TValue);
            }
        }

        /// <summary>
        /// Returns a standard, non-thread-safe Dictionary containing the current elements.
        /// </summary>
        /// <returns>A shallow copy of the internal dictionary.</returns>
        public Dictionary<TKey, TValue> GetShallowCopy()
        {
            lock (_padlock)
            {
                return new Dictionary<TKey, TValue>(_dictionary);
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
            lock (_padlock)
                return _dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>true if the key/value pair was added, false if the key already exists.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            lock (_padlock)
            {
                if (!_dictionary.ContainsKey(key))
                {
                    _dictionary.Add(key, value);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>true if the object was removed successfully, otherwise, false.</returns>
        public bool TryRemove(TKey key)
        {
            lock (_padlock)
            {
                if (_dictionary.ContainsKey(key))
                    return _dictionary.Remove(key);
            }

            return false;
        }

        /// <summary>
        /// Removes the element with the specified key from the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        public void Remove(TKey key)
        {
            lock (_padlock)
            {
                if (_dictionary.ContainsKey(key))
                    _dictionary.Remove(key);
            }
        }

        /// <summary>
        /// Forcibly adds a key/value pair. Throws exception if key exists.
        /// </summary>
        public void Add(TKey key, TValue val)
        {
            lock (_padlock)
            {
                _dictionary.Add(key, val);
            }
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="id">The key to locate.</param>
        /// <returns>true if the key is found, otherwise, false.</returns>
        public bool ContainsKey(TKey id)
        {
            lock (_padlock)
            {
                return _dictionary.ContainsKey(id);
            }
        }
        
        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            lock (_padlock)
            {
                _dictionary.Clear();
            }
        }

        /// <summary>
        /// Returns a sorted list of the dictionary elements based on a key.
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> OrderBy(Func<KeyValuePair<TKey, TValue>, TKey> func)
        {
            lock (_padlock)
                return _dictionary.OrderBy(func).ToList();
        }
        
        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// Exposed as a property for Jint compatibility.
        /// </summary>
        public int Count
        {
            get 
            {
                lock (_padlock) 
                {
                    return _dictionary.Count;
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
                lock (_padlock)
                {
                    return new List<TValue>(_dictionary.Values);
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
                lock (_padlock)
                {
                    return new List<TKey>(_dictionary.Keys);
                }
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                lock (_padlock)
                {
                    return _dictionary.Values;
                }
            }
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                lock (_padlock)
                {
                    return _dictionary.Keys;
                }
            }
        }

        /// <summary>
        /// Explicit implementation of the generic enumerator.
        /// </summary>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            lock (_padlock)
            {
                return _dictionary.GetEnumerator();
            }
        }
        
        /// <summary>
        /// Explicit implementation of the untyped enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_padlock)
            {
                return _dictionary.GetEnumerator();
            }
        }
    }
}