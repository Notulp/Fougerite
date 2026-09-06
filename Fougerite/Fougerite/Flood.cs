using Fougerite.Events;

namespace Fougerite
{
    /// <summary>
    /// This class is used by Fougerite to filter any flood connections to the server.
    /// </summary>
    public class Flood
    {
        private TimedEvent _te;
        private int _count = 1;
        private readonly string _ip;
            
        /// <summary>
        /// Initializes a new instance of the <see cref="Flood"/> class for the specified IP address.
        /// Starts a timer to track and eventually clear flood data for this IP.
        /// </summary>
        /// <param name="ip">The IP address to monitor for flooding.</param>
        public Flood(string ip)
        {
            _ip = ip;
            _te = Util.GetUtil().CreateTimer($"Flood.{ip}", 3000, Check, false, $"{nameof(Fougerite)}.{nameof(Flood)}");
            _te.Start();
        }

        /// <summary>
        /// Increments the connection attempt count for this IP address.
        /// </summary>
        public void Increase()
        {
            _count = _count + 1;
        }

        /// <summary>
        /// Gets the current number of connection attempts recorded for this IP address.
        /// </summary>
        public int Amount
        {
            get { return _count; }
        }

        /// <summary>
        /// Resets the flood tracking timer for this IP address, restarting the 3-second window.
        /// </summary>
        public void Reset()
        {
            _te.Kill();
            _te = Util.GetUtil().CreateTimer($"Flood.{_ip}", 3000, Check, false, $"{nameof(Fougerite)}.{nameof(Flood)}");
            _te.Start();
        }

        /// <summary>
        /// Stops the flood tracking timer and ceases monitoring for this IP address.
        /// </summary>
        public void Stop()
        {
            _te.Kill();
        }

        private void Check(TimedEvent evt)
        {
            evt.Kill();
            if (Hooks.FloodChecks.ContainsKey(_ip))
            {
                Hooks.FloodChecks.Remove(_ip);
            }
        }
    }
}