using System;
using System.Diagnostics;
using Fougerite.PluginLoaders;

namespace Fougerite
{
    public class Stopper : CountedInstance, IDisposable
    {
        private readonly string _type;
        private readonly string _method;
        private readonly long _warnTimeMS;
        private readonly Stopwatch _stopper;

        public Stopper(string type, string method, float warnSecs = 0.1f)
        {
            _type = type;
            _method = method;
            _warnTimeMS = (long)(warnSecs * 1000);
            _stopper = Stopwatch.StartNew();
        }

        void IDisposable.Dispose()
        {
            if (_stopper.ElapsedMilliseconds > _warnTimeMS) 
            {
                Logger.LogWarning(string.Format("[Stopper.{0}.{1}] Took: {2}s ({3}ms)",
                    _type,
                    _method,
                    _stopper.Elapsed.Seconds,
                    _stopper.ElapsedMilliseconds
                ));
            }
        }
    }
}