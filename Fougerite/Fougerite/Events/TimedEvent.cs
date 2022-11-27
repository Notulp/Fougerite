using System.Collections.Generic;
using System;
using System.Timers;

namespace Fougerite.Events
{
    public class TimedEvent
    {
        private Dictionary<string, object> _args;
        private string _name;
        private Timer _timer;
        private long _lastTick;
        private DateTime _lastTickDate;
        private double _interval;
        private bool _autoReset;
        private int _elapsedCount;

        /// <summary>
        /// The delegate type of the timer.
        /// </summary>
        public delegate void TimedEventFireDelegate(TimedEvent te);
        /// <summary>
        /// This is the event you must subscribe to.
        /// </summary>
        public event TimedEventFireDelegate OnFire;
        
        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <param name="name">The name of the timer.</param>
        /// <param name="interval">Interval in milliseconds.</param>
        /// <param name="autoreset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        public TimedEvent(string name, double interval, bool autoreset = false)
        {
            _name = name;
            _elapsedCount = 0;
            _autoReset = autoreset;
            _interval = interval;
            
            _timer = new Timer();
            _timer.Interval = interval;
            _timer.Elapsed += TimerElapsed;
            _timer.AutoReset = autoreset;
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <param name="name">The name of the timer.</param>
        /// <param name="interval">Interval in milliseconds.</param>
        /// <param name="autoreset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="args">The Dictionary that will hold additional data for you.</param>
        public TimedEvent(string name, double interval, bool autoreset, Dictionary<string, object> args)
            : this(name, interval, autoreset)
        {
            _args = args;
        }

        /// <summary>
        /// Gets called when the timer is elapsed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Dispose the timer first, as It has always been unreliable on long run in mono
            if (_timer != null)
                _timer.Dispose();

            // Call the event
            try
            {
                if (OnFire != null)
                {
                    OnFire(this);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error occured at timer: {Name} Error: {ex}");
            }

            // Set some infos
            _lastTick = DateTime.UtcNow.Ticks;
            _lastTickDate = DateTime.Now;
            _elapsedCount += 1;

            // Re-Create
            ReCreate();

            // Start
            Start();
        }

        /// <summary>
        /// Re-creates the whole timer object.
        /// </summary>
        private void ReCreate()
        {
            _timer = new Timer();
            _timer.Interval = _interval;
            _timer.Elapsed += TimerElapsed;
            _timer.AutoReset = _autoReset;
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            if (_timer == null)
            {
                ReCreate();
            }
            
            // Should have a value by this time.
            _timer?.Start();
            
            _lastTick = DateTime.UtcNow.Ticks;
            _lastTickDate = DateTime.Now;
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
                _timer.Stop();
        }

        /// <summary>
        /// Stops and Disposes the timer.
        /// </summary>
        public void Kill()
        {
            Stop();
            if (_timer != null)
                _timer.Dispose();
        }

        /// <summary>
        /// True if the timer should raise the elapsed event each time it elapses, false if only once.
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
        /// The time left for the next tick
        /// </summary>
        public double TimeLeft
        {
            get
            {
                return Interval - ((DateTime.UtcNow.Ticks - _lastTick) / 0x2710L);
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
    }
}