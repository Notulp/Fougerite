using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// A .NET 3.5 implementation of ConcurrentEnumerator, using ReaderWriterLock.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        private readonly ReaderWriterLock _lock;

        public ConcurrentEnumerator(IEnumerable<T> inner, ReaderWriterLock @lock)
        {
            this._lock = @lock;
            this._lock.AcquireReaderLock(Timeout.Infinite);
            this._inner = inner.GetEnumerator();
        }


        public bool MoveNext()
        {
            return _inner.MoveNext();
        }

        public void Reset()
        {
            _inner.Reset();
        }

        public void Dispose()
        {
            this._lock.ReleaseReaderLock();
        }

        public T Current
        {
            get { return _inner.Current; }
        }

        object IEnumerator.Current
        {
            get { return _inner.Current; }
        }
    }
}