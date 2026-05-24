using System;
using System.Collections.Generic;
using System.Linq;
using Fougerite.Concurrent;
using Fougerite.Events;

namespace Fougerite
{
    /// <summary>
    /// Represents a Fougerite C# plugin.
    /// </summary>
    public abstract class Module : IDisposable
    {
        public readonly ConcurrentDictionary<string, TimedEvent> Timers = new ConcurrentDictionary<string, TimedEvent>();
        public readonly ConcurrentList<TimedEvent> ParallelTimers = new ConcurrentList<TimedEvent>();
        
        public virtual string ModuleFolder { get; set; }

        public virtual string Name
        {
            get { return "None"; }
        }

        public virtual Version Version
        {
            get { return new Version(1, 0); }
        }

        public virtual string Author
        {
            get { return "None"; }
        }

        public virtual string Description
        {
            get { return "None"; }
        }

        public virtual bool Enabled { get; set; }

        /// <summary>
        /// Priority of the plugin's loading.
        /// </summary>
        public virtual uint Order
        {
            get { return uint.MaxValue; }
        }

        public virtual string UpdateURL
        {
            get { return ""; }
        }

        ~Module()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract void DeInitialize();

        public abstract void Initialize();
        
        
        /// <summary>
        /// Creates a timer with a callback.
        /// </summary>
        /// <param name="name">Name of the timer.</param>
        /// <param name="timeoutDelay">Interval in milliseconds.</param>
        /// <param name="callback">The callback function to execute when timer fires.</param>
        /// <param name="autoReset">True if the timer should repeat, false for single execution.</param>
        /// <param name="maxElapsedCount">Optional: Max fires before killing. 0 = infinite.</param>
        /// <returns>The created TimedEvent instance.</returns>
        public TimedEvent CreateTimer(string name, int timeoutDelay, Action<TimedEvent> callback, bool autoReset = false, int maxElapsedCount = 0)
        {
            Util.GetUtil().ThreadTimerCheck();
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent != null)
            {
                return timedEvent;
            }

            UnityEngine.GameObject go = new UnityEngine.GameObject($"{Name}_{name}_{UnityEngine.Random.Range(1, 999999)}");
            UnityEngine.Object.DontDestroyOnLoad(go);
            timedEvent = go.AddComponent<TimedEvent>();
            timedEvent.Name = name;
            timedEvent.PluginName = Name;
            timedEvent.Interval = timeoutDelay;
            timedEvent.AutoReset = autoReset;
            timedEvent.MaxElapsedCount = maxElapsedCount;
            timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
            timedEvent.OnKilled += (cbName) => Timers.TryRemove(cbName);
            Timers.Add(name, timedEvent);

            return timedEvent;
        }

        /// <summary>
        /// Creates a parallel timer with arguments and a callback. Multiple timers with the same name can exist.
        /// </summary>
        /// <param name="name">Name of the timer.</param>
        /// <param name="timeoutDelay">Interval in milliseconds.</param>
        /// <param name="args">Dictionary of custom arguments to pass to the timer.</param>
        /// <param name="callback">The callback function to execute when timer fires.</param>
        /// <param name="autoReset">True if the timer should repeat, false for single execution.</param>
        /// <param name="maxElapsedCount">Optional: Max fires before killing. 0 = infinite.</param>
        /// <returns>The created TimedEvent instance.</returns>
        public TimedEvent CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args, Action<TimedEvent> callback, bool autoReset = false, int maxElapsedCount = 0)
        {
            Util.GetUtil().ThreadTimerCheck();
            UnityEngine.GameObject go = new UnityEngine.GameObject($"{Name}_Parallel_{name}_{UnityEngine.Random.Range(1, 999999)}");
            UnityEngine.Object.DontDestroyOnLoad(go);
            TimedEvent timedEvent = go.AddComponent<TimedEvent>();
            timedEvent.Name = name;
            timedEvent.PluginName = Name;
            timedEvent.Interval = timeoutDelay;
            timedEvent.Args = args;
            timedEvent.AutoReset = autoReset;
            timedEvent.MaxElapsedCount = maxElapsedCount;
            timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
            timedEvent.OnKilled += (cbName) => ParallelTimers.Remove(timedEvent);
            ParallelTimers.Add(timedEvent);

            return timedEvent;
        }

        /// <summary>
        /// Gets the timer.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public TimedEvent GetTimer(string name)
        {
            TimedEvent result = Timers.ContainsKey(name) ? Timers[name] : null;
            return result;
        }

        /// <summary>
        /// Kills the timer.
        /// </summary>
        /// <param name="name">Name.</param>
        public void KillTimer(string name)
        {
            TimedEvent timer = GetTimer(name);
            if (timer == null)
                return;
            timer.Kill();
        }
        
        /// <summary>
        /// Gets the parallel timer.
        /// </summary>
        /// <returns>The parallel timer.</returns>
        /// <param name="name">Name.</param>
        public List<TimedEvent> GetParallelTimer(string name)
        {
            return ParallelTimers.Where(timer => timer.Name == name).ToList();
        }

        /// <summary>
        /// Kills the parallel timer.
        /// </summary>
        /// <param name="name">Name.</param>
        public void KillParallelTimer(string name)
        {
            foreach (TimedEvent timer in GetParallelTimer(name))
            {
                timer.Kill();
                ParallelTimers.Remove(timer);
            }
        }

        /// <summary>
        /// Kills the timers.
        /// </summary>
        public void KillTimers()
        {
            foreach (TimedEvent current in Timers.Values)
            {
                current.Kill();
            }

            foreach (TimedEvent timer in ParallelTimers)
            {
                timer.Kill();
            }

            Timers.Clear();
            ParallelTimers.Clear();
        }
    }
}