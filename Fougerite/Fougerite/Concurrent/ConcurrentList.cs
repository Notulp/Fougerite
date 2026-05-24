using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// A thread-safe implementation of a List for .NET 3.5.
    /// Utilizes a <see cref="ReaderWriterLock"/> to allow multiple concurrent readers,
    /// or a single exclusive writer.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class ConcurrentList<T> : IList<T>, IDisposable
    {
        private readonly List<T> _list;
        private readonly ReaderWriterLock _lock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class that is empty.
        /// </summary>
        public ConcurrentList()
        {
            _lock = new ReaderWriterLock();
            _list = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements the list can store.</param>
        public ConcurrentList(int capacity)
        {
            _lock = new ReaderWriterLock();
            _list = new List<T>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentList{T}"/> class that contains elements copied from the specified collection.
        /// </summary>
        /// <param name="items">The collection whose elements are copied to the new list.</param>
        public ConcurrentList(IEnumerable<T> items)
        {
            _lock = new ReaderWriterLock();
            _list = new List<T>(items);
        }

        /// <summary>
        /// Returns a new <see cref="List{T}"/> containing a snapshot of all current elements.
        /// </summary>
        /// <returns>A shallow copy of the internal list.</returns>
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

        /// <summary>
        /// Adds an object to the end of the list. Requires an exclusive writer lock.
        /// </summary>
        /// <param name="item">The object to be added to the end of the list.</param>
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

        /// <summary>
        /// Inserts an element into the list at the specified index. Requires an exclusive writer lock.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert.</param>
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

        /// <summary>
        /// Removes the first occurrence of a specific object from the list. Requires an exclusive writer lock.
        /// </summary>
        /// <param name="item">The object to remove from the list.</param>
        /// <returns>true if item is successfully removed, otherwise, false.</returns>
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

        /// <summary>
        /// Removes the element at the specified index of the list. Requires an exclusive writer lock.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
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

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>The zero-based index of the first occurrence if found, otherwise, –1.</returns>
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

        /// <summary>
        /// Removes all elements from the list. Requires an exclusive writer lock.
        /// </summary>
        public void Clear()
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _list.Clear();
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>true if item is found in the list, otherwise, false.</returns>
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

        /// <summary>
        /// Copies the entire list to a compatible one-dimensional array, starting at the specified index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
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

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>A <see cref="ConcurrentEnumerator{T}"/> for the list.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(_list, _lock);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the list.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(_list, _lock);
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up.
        /// </summary>
        ~ConcurrentList()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the <see cref="ReaderWriterLock"/> and suppresses finalization.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
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

        /// <summary>
        /// Gets the number of elements actually contained in the list.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the list is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }
        
        /// <summary>
        /// Adds the elements of the specified collection to the end of the list. Requires an exclusive writer lock.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the list.</param>
        public void AddRange(IEnumerable<T> collection)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _list.AddRange(collection);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }
        
        /// <summary>
        /// Removes a range of elements from the list. Requires an exclusive writer lock.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        public void RemoveRange(int index, int count)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                _list.RemoveRange(index, count);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }
    }
}