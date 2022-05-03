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
            this._lock = new ReaderWriterLock();
            this._list = new List<T>();
        }

        public ConcurrentList(int capacity)
        {
            this._lock = new ReaderWriterLock();
            this._list = new List<T>(capacity);
        }

        public ConcurrentList(IEnumerable<T> items)
        {
            this._lock = new ReaderWriterLock();
            this._list = new List<T>(items);
        }

        public List<T> GetShallowCopy()
        {
            List<T> temp;
            try
            {
                this._lock.AcquireReaderLock(Timeout.Infinite);
                temp = new List<T>(_list);
            }
            finally
            {
                this._lock.ReleaseReaderLock();
            }

            return temp;
        }

        public void Add(T item)
        {
            try
            {
                this._lock.AcquireWriterLock(Timeout.Infinite);
                this._list.Add(item);
            }
            finally
            {
                this._lock.ReleaseWriterLock();
            }
        }

        public void Insert(int index, T item)
        {
            try
            {
                this._lock.AcquireWriterLock(Timeout.Infinite);
                this._list.Insert(index, item);
            }
            finally
            {
                this._lock.ReleaseWriterLock();
            }
        }

        public bool Remove(T item)
        {
            try
            {
                this._lock.AcquireWriterLock(Timeout.Infinite);
                return this._list.Remove(item);
            }
            finally
            {
                this._lock.ReleaseWriterLock();
            }
        }

        public void RemoveAt(int index)
        {
            try
            {
                this._lock.AcquireWriterLock(Timeout.Infinite);
                this._list.RemoveAt(index);
            }
            finally
            {
                this._lock.ReleaseWriterLock();
            }
        }

        public int IndexOf(T item)
        {
            try
            {
                this._lock.AcquireReaderLock(Timeout.Infinite);
                return this._list.IndexOf(item);
            }
            finally
            {
                this._lock.ReleaseReaderLock();
            }
        }

        public void Clear()
        {
            try
            {
                this._lock.AcquireReaderLock(Timeout.Infinite);
                this._list.Clear();
            }
            finally
            {
                this._lock.ReleaseReaderLock();
            }
        }

        public bool Contains(T item)
        {
            try
            {
                this._lock.AcquireReaderLock(Timeout.Infinite);
                return this._list.Contains(item);
            }
            finally
            {
                this._lock.ReleaseReaderLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                this._lock.AcquireReaderLock(Timeout.Infinite);
                this._list.CopyTo(array, arrayIndex);
            }
            finally
            {
                this._lock.ReleaseReaderLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(this._list, this._lock);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(this._list, this._lock);
        }

        ~ConcurrentList()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
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
                    this._lock.AcquireReaderLock(Timeout.Infinite);
                    return this._list[index];
                }
                finally
                {
                    this._lock.ReleaseReaderLock();
                }
            }
            set
            {
                try
                {
                    this._lock.AcquireWriterLock(Timeout.Infinite);
                    this._list[index] = value;
                }
                finally
                {
                    this._lock.ReleaseWriterLock();
                }
            }
        }

        public int Count
        {
            get
            {
                try
                {
                    this._lock.AcquireReaderLock(Timeout.Infinite);
                    return this._list.Count;
                }
                finally
                {
                    this._lock.ReleaseReaderLock();
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
    }
}