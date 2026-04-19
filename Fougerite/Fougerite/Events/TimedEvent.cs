using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fougerite.Events
{
    public class TimedEvent : MonoBehaviour
    {
        private Dictionary<string, object> _args;
        private string _name;
        private long _lastTick;
        private DateTime _lastTickDate;
        private double _interval;
        private bool _autoReset;
        private int _elapsedCount;
        private int _maxElapsedCount;
        private bool _running;
        private bool _killed;
        private string _pluginName;
        
        /// <summary>
        /// This event is fired when the timer is killed/disposed.
        /// </summary>
        public event Action<string> OnKilled;
        
        /// <summary>
        /// The delegate type of the timer.
        /// </summary>
        public delegate void TimedEventFireDelegate(TimedEvent te);
        
        /// <summary>
        /// This event is fired when the timer elapses.
        /// </summary>
        public event TimedEventFireDelegate OnFire;

        /// <summary>
        /// Creates an empty timer.
        /// </summary>
        public TimedEvent()
        {
            
        }
        
        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <param name="name">The name of the timer.</param>
        /// <param name="interval">Interval in milliseconds.</param>
        /// <param name="autoreset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="maxElapsedCount">Optional: The maximum number of times the timer should fire before killing itself. 0 = infinite. Only considered if autoreset is true.</param>
        public TimedEvent(string name, double interval, bool autoreset = false, int maxElapsedCount = 0)
        {
            _name = name;
            _elapsedCount = 0;
            _autoReset = autoreset;
            _interval = interval;
            _running = false;
            _maxElapsedCount = maxElapsedCount;
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <param name="name">The name of the timer.</param>
        /// <param name="interval">Interval in milliseconds.</param>
        /// <param name="autoreset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="args">The Dictionary that will hold additional data for you.</param>
        /// <param name="maxElapsedCount">Optional: The maximum number of times the timer should fire before killing itself. 0 = infinite. Only considered if autoreset is true.</param>
        public TimedEvent(string name, double interval, bool autoreset, Dictionary<string, object> args, int maxElapsedCount = 0)
            : this(name, interval, autoreset, maxElapsedCount)
        {
            _args = args;
        }

        /// <summary>
        /// Fires the timer event internally.
        /// </summary>
        private void InternalFire()
        {
            // Call the event
            using (new Stopper(nameof(TimedEvent), $"{PluginName}.{_name}"))
            {
                try
                {
                    if (OnFire != null)
                    {
                        OnFire(this);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error occured at timer: {PluginName}.{_name} Error: {ex}");
                }
            }

            // Set some infos
            _lastTick = DateTime.UtcNow.Ticks;
            _lastTickDate = DateTime.Now;
            _elapsedCount += 1;
            
            // Check if we hit the limit
            bool hitLimit = _maxElapsedCount > 0 && _elapsedCount >= _maxElapsedCount;

            // Auto reset is false, we stop here
            if (!_autoReset || hitLimit)
            {
                // Dispose the timer
                Kill();
            }
        }

        /// <summary>
        /// Do not call this method directly. This is used by Unity when the object is disabled.
        /// </summary>
        public void OnDisable()
        {
            _running = false;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            if (_running) return;
            
            _running = true;
            _lastTick = DateTime.UtcNow.Ticks;
            _lastTickDate = DateTime.Now;
            
            StartCoroutine(TimerLoop());
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            _running = false;
            StopAllCoroutines();
        }

        /// <summary>
        /// Stops and Disposes the timer.
        /// </summary>
        public void Kill()
        {
            if (_killed) return;
            Util.GetUtil().ThreadTimerCheck();
            _killed = true;
            Stop();
            OnKilled?.Invoke(_name);
            DestroyImmediate(gameObject);
        }

        /// <summary>
        /// The internal loop that runs the timer.
        /// </summary>
        /// <returns></returns>
        private IEnumerator TimerLoop()
        {
            while (_running)
            {
                float waitTime = (float)(_interval / 1000f);
                yield return new WaitForSeconds(waitTime);
        
                if (_running)
                {
                    InternalFire();
                }
            }
        }

        /// <summary>
        /// True if the timer should raise the elapsed event each time it elapses, false if only once.
        /// Basically true means the timer will keep running forever, false means only once.
        /// </summary>
        public bool AutoReset
        {
            get
            {
                return _autoReset;
            }
            set
            {
                _autoReset = value;
            }
        }
        
        /// <summary>
        /// The maximum number of times the timer should fire before killing itself. 
        /// 0 = infinite. Only considered if autoreset is true.
        /// </summary>
        public int MaxElapsedCount
        {
            get
            {
                return _maxElapsedCount;
            }
            set
            {
                _maxElapsedCount = value;
            }
        }

        /// <summary>
        /// The custom arguments to store in the timer
        /// </summary>
        public Dictionary<string, object> Args
        {
            get
            {
                return _args;
            }
            set
            {
                _args = value;
            }
        }
        
        /// <summary>
        /// The interval to run in milliseconds
        /// 1000 = 1 second
        /// </summary>
        public double Interval
        {
            get
            {
                return _interval;
            }
            set
            {
                _interval = value;
            }
        }

        /// <summary>
        /// The name of the Timer for easier identification
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        /// <summary>
        /// The name of the plugin that created this timer
        /// This is optionally set by the plugin system
        /// </summary>
        public string PluginName
        {
            get
            {
                return _pluginName;
            }
            set
            {
                _pluginName = value;
            }
        }

        /// <summary>
        /// The time left for the next tick
        /// </summary>
        public double TimeLeft
        {
            get
            {
                return Interval - (DateTime.UtcNow.Ticks - _lastTick) / 0x2710L;
            }
        }
        
        /// <summary>
        /// The last tick time.
        /// </summary>
        public long LastTick
        {
            get
            {
                return _lastTick;
            }
        }

        /// <summary>
        /// The last tick, but DateTime for easier calculations and more reliability
        /// </summary>
        public DateTime LastTickDate
        {
            get
            {
                return _lastTickDate;
            }
        }
        
        /// <summary>
        /// The amount of time the timer has elapsed so far
        /// </summary>
        public int ElapsedCount 
        {
            get
            {
                return _elapsedCount;
            }
        }

        /// <summary>
        /// Tells whether the timer is currently running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _running;
            }
        }

        /// <summary>
        /// Tells whether the timer has been killed/disposed.
        /// </summary>
        public bool IsKilled
        {
            get
            {
                return _killed;
            }
        }
    }
}