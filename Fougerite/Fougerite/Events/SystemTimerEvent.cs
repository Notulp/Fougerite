using System;
using System.Collections.Generic;
using System.Timers;

namespace Fougerite.Events
{
    /// <summary>
    /// A thread-safe wrapper for System.Timers.Timer, designed to be used identically to the Unity-based TimedEvent.
    /// Use this if you need to create a timer from the thread, without accessing the gameobject and other unity class
    /// provided in the TimedEvent class, which would result your server into crashing.
    /// </summary>
    public class SystemTimerEvent : IDisposable
    {
        /// <summary>
        /// Delegate for the OnFire event.
        /// </summary>
        /// <param name="timer">The instance of the timer firing the event.</param>
        public delegate void SystemTimerFireDelegate(SystemTimerEvent timer);

        /// <summary>
        /// Triggered when the timer interval elapses.
        /// </summary>
        public event SystemTimerFireDelegate OnFire;

        /// <summary>
        /// Triggered when the timer is killed. Passes the timer name to handle dictionary cleanup.
        /// </summary>
        public event Action<string> OnKilled;

        private Timer _timer;
        private int _elapsedCount;

        /// <summary>
        /// Gets or sets the name of the timer.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the plugin that created the timer.
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// Gets or sets the interval in milliseconds.
        /// </summary>
        public double Interval { get; set; }

        /// <summary>
        /// Dictionary of custom arguments passed to parallel timers.
        /// </summary>
        public Dictionary<string, object> Args { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the timer should repeat.
        /// </summary>
        public bool AutoReset { get; set; }

        /// <summary>
        /// Gets or sets the maximum amount of times this timer can fire before it kills itself. 0 is infinite.
        /// </summary>
        public int MaxElapsedCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the SystemTimerEvent class.
        /// </summary>
        public SystemTimerEvent(string name, string pluginName, double interval, bool autoReset, int maxElapsedCount)
        {
            Name = name;
            PluginName = pluginName;
            Interval = interval;
            AutoReset = autoReset;
            MaxElapsedCount = maxElapsedCount;
            _elapsedCount = 0;

            _timer = new Timer(Interval);
            _timer.AutoReset = AutoReset;
            _timer.Elapsed += Timer_Elapsed;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            if (_timer != null)
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Stops the timer from firing, but does not destroy it.
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
        }

        /// <summary>
        /// Stops and destroys the timer, triggering the OnKilled event.
        /// </summary>
        public void Kill()
        {
            Stop();
            Dispose();
            OnKilled?.Invoke(Name);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                OnFire?.Invoke(this);

                if (MaxElapsedCount > 0)
                {
                    _elapsedCount++;
                    if (_elapsedCount >= MaxElapsedCount)
                    {
                        Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[SystemTimerEvent] Error in timer {Name}: {ex}");
            }
        }

        /// <summary>
        /// Clears the underlying timer resources.
        /// </summary>
        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}