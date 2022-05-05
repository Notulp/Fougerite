using System.Threading;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// Self implementation of this class.
    /// This class resides in the mscorlib under namespace System.Threading by having an internal flag.
    /// This is not the place where we want to patch, so here is our own version instead.
    /// </summary>
    public class LockQueue
    {
        private ReaderWriterLock rwlock;
        private int _lockCount = 0;

        public LockQueue(ReaderWriterLock rwlock)
        {
            this.rwlock = rwlock;
        }

        public bool Wait(int timeout)
        {
            bool @lock = false;

            try
            {
                lock (this)
                {
                    _lockCount++;
                    Monitor.Exit(rwlock);
                    @lock = true;
                    return Monitor.Wait(this, timeout);
                }
            }
            finally
            {
                if (@lock)
                {
                    Monitor.Enter(rwlock);
                    _lockCount--;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (this) return (_lockCount == 0);
            }
        }

        public void Pulse()
        {
            lock (this) Monitor.Pulse(this);
        }
    }
}