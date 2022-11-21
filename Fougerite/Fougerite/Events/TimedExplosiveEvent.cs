using UnityEngine;

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a C4 is placed somewhere.
    /// </summary>
    public class TimedExplosiveEvent
    {
        private readonly TimedExplosive _timedExplosive;
        private readonly Vector3 _location;
        private bool _cancelled;
        
        public TimedExplosiveEvent(TimedExplosive timedExplosive)
        {
            _timedExplosive = timedExplosive;
            _location = timedExplosive.gameObject.transform.position;
        }

        /// <summary>
        /// Gets the TimedExplosive class.
        /// </summary>
        public TimedExplosive TimedExplosive
        {
            get
            {
                return _timedExplosive;
            }
        }

        /// <summary>
        /// Returns the location.
        /// </summary>
        public Vector3 Location
        {
            get
            {
                return _location;
            }
        }

        /// <summary>
        /// Returns the gameobject.
        /// Null if destroyed.
        /// </summary>
        public GameObject GameObject
        {
            get
            {
                return _timedExplosive.gameObject;
            }
        }

        /// <summary>
        /// Gets if the event was cancelled.
        /// </summary>
        public bool Cancelled
        {
            get
            {
                return _cancelled;
            }
        }
        

        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            if (_cancelled)
                return;

            _cancelled = true;
        }
    }
}