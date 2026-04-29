using System;
using System.Diagnostics;
using Fougerite.PluginLoaders;

namespace Fougerite
{
    /// <summary>
    /// A performance monitoring utility used to track the execution time of code blocks.
    /// It logs a warning if the elapsed time exceeds a specified threshold upon disposal.
    /// </summary>
    public class Stopper : CountedInstance, IDisposable
    {
        private readonly string _type;
        private readonly string _method;
        private readonly long _warnTimeMS;
        private readonly Stopwatch _stopper;

        /// <summary>
        /// Initializes a new instance of the Stopper class and starts the high-resolution timer.
        /// </summary>
        /// <param name="type">The name of the class or category being monitored.</param>
        /// <param name="method">The name of the method or specific logic block being monitored.</param>
        /// <param name="warnSecs">The threshold in seconds before a warning is logged. Defaults to 0.1s (100ms).</param>
        public Stopper(string type, string method, float warnSecs = 0.1f)
        {
            _type = type;
            _method = method;
            _warnTimeMS = (long)(warnSecs * 1000);
            _stopper = Stopwatch.StartNew();
        }

        /// <summary>
        /// Stops the timer and compares the elapsed time against the warning threshold.
        /// If the execution took too long, a warning is sent to the Logger.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (_stopper.ElapsedMilliseconds > _warnTimeMS) 
            {
                Logger.LogWarning($"[Stopper.{_type}.{_method}] Took: {_stopper.Elapsed.Seconds}s ({_stopper.ElapsedMilliseconds}ms)");
            }
        }
    }
}