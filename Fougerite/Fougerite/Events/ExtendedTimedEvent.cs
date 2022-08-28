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
            this._timer = new Timer();
            this._timer.Interval = interval;
            this._timer.Elapsed += this._timer_Elapsed;
            this._elapsedCount = 0;
        }

        public ExtendedTimedEvent(double interval, Dictionary<string, object> args)
            : this(interval)
        {
            this.Args = args;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.OnFire != null)
            {
                this.OnFire(this);
            }

            this._elapsedCount += 1;
            this._lastTick = DateTime.UtcNow.Ticks;
        }

        public void Start()
        {
            this._timer.Start();
            this._lastTick = DateTime.UtcNow.Ticks;
        }

        public void Stop()
        {
            this._timer.Stop();
        }

        public void Kill()
        {
            this._timer.Stop();
            this._timer.Dispose();
        }

        public Dictionary<string, object> Args
        {
            get { return this._args; }
            set { this._args = value; }
        }

        public double Interval
        {
            get { return this._timer.Interval; }
            set { this._timer.Interval = value; }
        }

        public double TimeLeft
        {
            get { return (this.Interval - ((DateTime.UtcNow.Ticks - this._lastTick) / 0x2710L)); }
        }

        public int ElapsedCount
        {
            get { return this._elapsedCount; }
        }
    }
}