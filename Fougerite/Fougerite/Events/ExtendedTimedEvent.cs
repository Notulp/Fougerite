using System;
using System.Collections.Generic;
using System.Timers;

namespace Fougerite.Events
{
    public class ExtendedTimedEvent
    {
        private Dictionary<string, object> _args;
        private readonly Timer _timer;
        private long _lastTick;
        private int _elapsedCount;

        public delegate void TimedEventFireDelegate(ExtendedTimedEvent evt);

        public event TimedEventFireDelegate OnFire;

        public ExtendedTimedEvent(double interval)
        {
            _timer = new Timer();
            _timer.Interval = interval;
            _timer.Elapsed += _timer_Elapsed;
            _elapsedCount = 0;
        }

        public ExtendedTimedEvent(double interval, Dictionary<string, object> args)
            : this(interval)
        {
            Args = args;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (OnFire != null)
            {
                OnFire(this);
            }

            _elapsedCount += 1;
            _lastTick = DateTime.UtcNow.Ticks;
        }

        public void Start()
        {
            _timer.Start();
            _lastTick = DateTime.UtcNow.Ticks;
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Kill()
        {
            _timer.Stop();
            _timer.Dispose();
        }

        public Dictionary<string, object> Args
        {
            get { return _args; }
            set { _args = value; }
        }

        public double Interval
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }

        public double TimeLeft
        {
            get { return (Interval - ((DateTime.UtcNow.Ticks - _lastTick) / 0x2710L)); }
        }

        public int ElapsedCount
        {
            get { return _elapsedCount; }
        }
    }
}