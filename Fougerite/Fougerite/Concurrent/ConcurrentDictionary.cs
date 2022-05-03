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

        public Dictionary<TKey, TValue> GetShallowCopy()
        {
            lock (_padlock)
            {
                return new Dictionary<TKey, TValue>(_dictionary);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_padlock)
                return _dictionary.TryGetValue(key, out value);
        }

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

        public bool TryRemove(TKey key)
        {
            lock (_padlock)
            {
                return _dictionary.Remove(key);
            }
        }

        internal void Add(TKey key, TValue val)
        {
            lock (_padlock)
            {
                _dictionary.Add(key, val);
            }
        }

        public bool ContainsKey(TKey id)
        {
            lock (_padlock)
                return _dictionary.ContainsKey(id);
        }

        public List<KeyValuePair<TKey, TValue>> OrderBy(Func<KeyValuePair<TKey, TValue>, TKey> func)
        {
            lock (_padlock)
                return _dictionary.OrderBy(func).ToList();
        }

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


        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            lock (_padlock)
            {
                return _dictionary.GetEnumerator();
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_padlock)
            {
                return _dictionary.GetEnumerator();
            }
        }
    }
}