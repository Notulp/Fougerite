using System;
using System.Collections;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// Our own implementation of ReaderWriterLock.
    /// This class resides in the mscorlib.dll, but LockQueue is internal, and we aren't going to patch that.
    /// You should read the microsoft docs on how a ReaderWriterLock works.
    /// </summary>
    public sealed class ReaderWriterLock : CriticalFinalizerObject
    {
        private int _seqNum = 1;
        private int _state;
        private int _readers;
        private int _writerLockOwner;
        private readonly LockQueue _writerQueue;
        private readonly Hashtable _readerLocks;

        public ReaderWriterLock()
        {
            _writerQueue = new LockQueue(this);
            _readerLocks = new Hashtable();

            GC.SuppressFinalize(this);
        }

        ~ReaderWriterLock()
        {
        }

        public bool IsReaderLockHeld
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                lock (this) return _readerLocks.ContainsKey(Thread.CurrentThread.ManagedThreadId);
            }
        }

        public bool IsWriterLockHeld
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                lock (this) return (_state < 0 && Thread.CurrentThread.ManagedThreadId == _writerLockOwner);
            }
        }

        public int WriterSeqNum
        {
            get
            {
                lock (this) return _seqNum;
            }
        }

        public void AcquireReaderLock(int millisecondsTimeout)
        {
            AcquireReaderLock(millisecondsTimeout, 1);
        }

        void AcquireReaderLock(int millisecondsTimeout, int initialLockCount)
        {
            lock (this)
            {
                if (HasWriterLock())
                {
                    AcquireWriterLock(millisecondsTimeout, initialLockCount);
                    return;
                }

                object nlocks = _readerLocks[Thread.CurrentThread.ManagedThreadId];
                if (nlocks == null)
                {
                    // Not currently holding a reader lock
                    // Wait if there is a write lock
                    _readers++;
                    try
                    {
                        if (_state < 0 || !_writerQueue.IsEmpty)
                        {
                            do
                            {
                                if (!Monitor.Wait(this, millisecondsTimeout))
                                    throw new ApplicationException("Timeout expired");
                            } while (_state < 0);
                        }
                    }
                    finally
                    {
                        _readers--;
                    }

                    _readerLocks[Thread.CurrentThread.ManagedThreadId] = initialLockCount;
                    _state += initialLockCount;
                }
                else
                {
                    _readerLocks[Thread.CurrentThread.ManagedThreadId] = ((int)nlocks) + 1;
                    _state++;
                }
            }
        }

        public void AcquireReaderLock(TimeSpan timeout)
        {
            int ms = CheckTimeout(timeout);
            AcquireReaderLock(ms, 1);
        }

        public void AcquireWriterLock(int millisecondsTimeout)
        {
            AcquireWriterLock(millisecondsTimeout, 1);
        }

        void AcquireWriterLock(int millisecondsTimeout, int initialLockCount)
        {
            lock (this)
            {
                if (HasWriterLock())
                {
                    _state--;
                    return;
                }

                // wait while there are reader locks or another writer lock, or
                // other threads waiting for the writer lock
                if (_state != 0 || !_writerQueue.IsEmpty)
                {
                    do
                    {
                        if (!_writerQueue.Wait(millisecondsTimeout))
                            throw new ApplicationException("Timeout expired");
                    } while (_state != 0);
                }

                _state = -initialLockCount;
                _writerLockOwner = Thread.CurrentThread.ManagedThreadId;
                _seqNum++;
            }
        }

        public void AcquireWriterLock(TimeSpan timeout)
        {
            int ms = CheckTimeout(timeout);
            AcquireWriterLock(ms, 1);
        }

        public bool AnyWritersSince(int seqNum)
        {
            lock (this)
            {
                return (this._seqNum > seqNum);
            }
        }

        public void DowngradeFromWriterLock(ref LockCookie lockCookie)
        {
            lock (this)
            {
                if (!HasWriterLock())
                    throw new ApplicationException("The thread does not have the writer lock.");

                if (lockCookie.WriterLocks != 0)
                    _state++;
                else
                {
                    _state = lockCookie.ReaderLocks;
                    _readerLocks[Thread.CurrentThread.ManagedThreadId] = _state;
                    if (_readers > 0)
                    {
                        Monitor.PulseAll(this);
                    }
                }

                // MSDN: A thread does not block when downgrading from the writer lock, 
                // even if other threads are waiting for the writer lock
            }
        }

        public LockCookie ReleaseLock()
        {
            LockCookie cookie;
            lock (this)
            {
                cookie = GetLockCookie();
                if (cookie.WriterLocks != 0)
                    ReleaseWriterLock(cookie.WriterLocks);
                else if (cookie.ReaderLocks != 0)
                {
                    ReleaseReaderLock(cookie.ReaderLocks, cookie.ReaderLocks);
                }
            }

            return cookie;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void ReleaseReaderLock()
        {
            lock (this)
            {
                if (HasWriterLock())
                {
                    ReleaseWriterLock();
                    return;
                }
                else if (_state > 0)
                {
                    object readLockCount = _readerLocks[Thread.CurrentThread.ManagedThreadId];
                    if (readLockCount != null)
                    {
                        ReleaseReaderLock((int)readLockCount, 1);
                        return;
                    }
                }

                throw new ApplicationException("The thread does not have any reader or writer locks.");
            }
        }

        void ReleaseReaderLock(int currentCount, int releaseCount)
        {
            int newCount = currentCount - releaseCount;

            if (newCount == 0)
                _readerLocks.Remove(Thread.CurrentThread.ManagedThreadId);
            else
                _readerLocks[Thread.CurrentThread.ManagedThreadId] = newCount;

            _state -= releaseCount;
            if (_state == 0 && !_writerQueue.IsEmpty)
                _writerQueue.Pulse();
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public void ReleaseWriterLock()
        {
            lock (this)
            {
                if (!HasWriterLock())
                    throw new ApplicationException("The thread does not have the writer lock.");

                ReleaseWriterLock(1);
            }
        }

        void ReleaseWriterLock(int releaseCount)
        {
            _state += releaseCount;
            if (_state == 0)
            {
                if (_readers > 0)
                {
                    Monitor.PulseAll(this);
                }
                else if (!_writerQueue.IsEmpty)
                    _writerQueue.Pulse();
            }
        }

        public void RestoreLock(ref LockCookie lockCookie)
        {
            lock (this)
            {
                if (lockCookie.WriterLocks != 0)
                    AcquireWriterLock(-1, lockCookie.WriterLocks);
                else if (lockCookie.ReaderLocks != 0)
                    AcquireReaderLock(-1, lockCookie.ReaderLocks);
            }
        }

        public LockCookie UpgradeToWriterLock(int millisecondsTimeout)
        {
            LockCookie cookie;
            lock (this)
            {
                cookie = GetLockCookie();
                if (cookie.WriterLocks != 0)
                {
                    _state--;
                    return cookie;
                }

                if (cookie.ReaderLocks != 0)
                    ReleaseReaderLock(cookie.ReaderLocks, cookie.ReaderLocks);
            }

            // Don't do this inside the lock, since it can cause a deadlock.
            AcquireWriterLock(millisecondsTimeout);
            return cookie;
        }

        public LockCookie UpgradeToWriterLock(TimeSpan timeout)
        {
            int ms = CheckTimeout(timeout);
            return UpgradeToWriterLock(ms);
        }

        LockCookie GetLockCookie()
        {
            LockCookie cookie = new LockCookie(Thread.CurrentThread.ManagedThreadId);

            if (HasWriterLock())
                cookie.WriterLocks = -_state;
            else
            {
                object locks = _readerLocks[Thread.CurrentThread.ManagedThreadId];
                if (locks != null) cookie.ReaderLocks = (int)locks;
            }

            return cookie;
        }

        bool HasWriterLock()
        {
            return (_state < 0 && Thread.CurrentThread.ManagedThreadId == _writerLockOwner);
        }

        private int CheckTimeout(TimeSpan timeout)
        {
            int ms = (int)timeout.TotalMilliseconds;

            if (ms < -1)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Number must be either non-negative or -1");
            return ms;
        }
    }
}