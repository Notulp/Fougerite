using System.Collections.Generic;

namespace Fougerite.Events
{
    using System;
    using System.Timers;

    public class TimedEvent
    {
        private Dictionary<string, object> _args;
        private string _name;
        private Timer _timer;
        private long lastTick;
        private int _elapsedCount;

        public delegate void TimedEventFireDelegate(TimedEvent te);
        public event TimedEventFireDelegate OnFire;
        
        public TimedEvent(string name, double interval, bool autoreset = false)
        {
            _name = name;
            _timer = new Timer();
            _timer.Interval = interval;
            _timer.Elapsed += new ElapsedEventHandler(_timer_Elapsed);
            _elapsedCount = 0;
            _timer.AutoReset = autoreset;
        }

        public TimedEvent(string name, double interval, bool autoreset, Dictionary<string, object> args)
            : this(name, interval)
        {
            _timer.AutoReset = autoreset;
            _args = args;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (OnFire != null)
                {
                    OnFire(this);
                }
                lastTick = DateTime.UtcNow.Ticks;
                _elapsedCount += 1;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error occured at timer: {Name} Error: {ex}");
                Stop();
                Logger.LogDebug("Trying to restart timer.");
                Start();
                Logger.LogDebug("Restarted!");
            }
        }

        public void Start()
        {
            _timer.Start();
            lastTick = DateTime.UtcNow.Ticks;
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Kill()
        {
            Stop();
            _timer.Dispose();
        }

        public bool AutoReset
        {
            get { return _timer.AutoReset; }
            set { _timer.AutoReset = value; }
        }

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

        
        public double Interval
        {
            get
            {
                return _timer.Interval;
            }
            set
            {
                _timer.Interval = value;
            }
        }

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

        public double TimeLeft
        {
            get
            {
                return (Interval - ((DateTime.UtcNow.Ticks - lastTick) / 0x2710L));
            }
        }
        
        public int ElapsedCount 
        {
            get
            {
                return _elapsedCount;
            }
        }
    }
}