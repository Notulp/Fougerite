using System;
using System.Threading;

namespace Fougerite.Concurrent
{
    /// <summary>
    /// Provides support for lazy initialization.
    /// </summary>
    /// <typeparam name="T">Specifies the type of object that is being lazily initialized.</typeparam>
    public sealed class Lazy<T>
    {
        private readonly ReaderWriterLock _lock = new ReaderWriterLock();
        private readonly Func<T> _createValue;
        private bool _isValueCreated;
        private T _value;

        /// <summary>
        /// Gets the lazily initialized value of the current Lazy{T} instance.
        /// </summary>
        public T Value
        {
            get
            {
                if (!IsValueCreated)
                {
                    _lock.AcquireWriterLock(-1);
                    try
                    {
                        if (!_isValueCreated)
                        {
                            _value = _createValue();
                            _isValueCreated = true;
                        }
                    }
                    finally
                    {
                        _lock.ReleaseWriterLock();
                    }
                }

                return _value;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether a value has been created for this Lazy{T} instance.
        /// </summary>
        public bool IsValueCreated
        {
            get
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return _isValueCreated;
                }
                finally
                {
                    _lock.ReleaseReaderLock();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/> class, 
        /// using the specified value factory for lazy initialization.
        /// </summary>
        /// <param name="createValue">
        /// A delegate that is invoked to produce the lazily initialized value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="createValue"/> is <c>null</c>.
        /// </exception>
        /// <typeparam name="T">
        /// Specifies the type of object that is being lazily initialized.
        /// </typeparam>
        public Lazy(Func<T> createValue)
        {
            if (createValue == null)
                throw new ArgumentNullException(nameof(createValue));

            _createValue = createValue;
        }

        /// <summary>
        /// Creates and returns a string representation of the Lazy{T}.Value.
        /// </summary>
        /// <returns>The string representation of the Lazy{T}.Value property.</returns>
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}