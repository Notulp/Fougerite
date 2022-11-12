using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// A .NET 3.5 implementation of ConcurrentList, using ReaderWriterLock.
    /// You should read the microsoft docs on how a ConcurrentList works.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentList<T> : IList<T>, IDisposable
    {
        private readonly List<T> _list;
        private readonly ReaderWriterLock _lock;

        public ConcurrentList()
        {
            _lock = new ReaderWriterLock();
            _list = new List<T>();
        }

        public ConcurrentList(int capacity)
        {
            _lock = new ReaderWriterLock();
            _list = new List<T>(capacity);
        }

        public ConcurrentList(IEnumerable<T> items)
        {
            _lock = new ReaderWriterLock();
            _list = new List<T>(items);
        }

        public List<T> GetShallowCopy()
        {
            List<T> temp;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                temp = new List<T>(_list);
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return temp;
        }

        public void Add(T item)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _list.Add(item);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public void Insert(int index, T item)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _list.Insert(index, item);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public bool Remove(T item)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                return _list.Remove(item);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public void RemoveAt(int index)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _list.RemoveAt(index);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public int IndexOf(T item)
        {
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                return _list.IndexOf(item);
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public void Clear()
        {
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                _list.Clear();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                return _list.Contains(item);
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                _list.CopyTo(array, arrayIndex);
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(_list, _lock);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(_list, _lock);
        }

        ~ConcurrentList()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);
        }

        public T this[int index]
        {
            get
            {
                try
                {
                    _lock.AcquireReaderLock(Timeout.Infinite);
                    return _list[index];
                }
                finally
                {
                    _lock.ReleaseReaderLock();
                }
            }
            set
            {
                try
                {
                    _lock.AcquireWriterLock(Timeout.Infinite);
                    _list[index] = value;
                }
                finally
                {
                    _lock.ReleaseWriterLock();
                }
            }
        }

        public int Count
        {
            get
            {
                try
                {
                    _lock.AcquireReaderLock(Timeout.Infinite);
                    return _list.Count;
                }
                finally
                {
                    _lock.ReleaseReaderLock();
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
    }
}