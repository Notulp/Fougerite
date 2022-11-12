namespace Fougerite.Events
{
    using UnityEngine;

    /// <summary>
    /// This class is created when a player is spawning or just spawned.
    /// </summary>
    public class SpawnEvent
    {
        private readonly bool _atCamp;
        private float _x;
        private float _y;
        private float _z;
        private Vector3 _orig;

        public SpawnEvent(Vector3 pos, bool camp)
        {
            _atCamp = camp;
            _x = pos.x;
            _y = pos.y;
            _z = pos.z;
            _orig = pos;
        }

        /// <summary>
        /// Did the player use the campused button?
        /// </summary>
        public bool CampUsed
        {
            get
            {
                return _atCamp;
            }
        }

        /// <summary>
        /// Location where the player is spawning or spawned. Can change at Spawning.
        /// </summary>
        public Vector3 Location
        {
            get
            {
                return _orig;
            }
            set
            {
                _x = value.x;
                _y = value.y;
                _z = value.z;
            }
        }

        /// <summary>
        /// X of the spawn coordinates. Can change at Spawning.
        /// </summary>
        public float X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        /// <summary>
        /// Y of the spawn coordinates. Can change at Spawning.
        /// </summary>
        public float Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }
        
        /// <summary>
        /// Z of the spawn coordinates. Can change at Spawning.
        /// </summary>
        public float Z
        {
            get
            {
                return _z;
            }
            set
            {
                _z = value;
            }
        }
    }
}