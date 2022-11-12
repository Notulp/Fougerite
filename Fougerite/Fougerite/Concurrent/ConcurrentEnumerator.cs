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
            _lock = @lock;
            _lock.AcquireReaderLock(Timeout.Infinite);
            _inner = inner.GetEnumerator();
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
            _lock.ReleaseReaderLock();
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