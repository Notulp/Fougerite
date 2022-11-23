namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a door is opened or closed.
    /// </summary>
    public class DoorEvent
    {
        private Entity _ent;
        private bool _open;
        private BasicDoor _basicDoor;
        private bool _cancelled;
        private BasicDoor.State _basicDoorState;

        public DoorEvent(Entity e)
        {
            _open = false;
            Entity = e;
        }

        /// <summary>
        /// Gets the door's entity.
        /// </summary>
        public Entity Entity
        {
            get
            {
                return _ent;
            }
            set
            {
                // Ehh, this shouldn't have been implemented
                _ent = value;
            }
        }

        /// <summary>
        /// Gets the BasicDoor class.
        /// </summary>
        public BasicDoor BasicDoor
        {
            get
            {
                return _basicDoor;
            }
            internal set
            {
                _basicDoor = value;
            }
        }

        /// <summary>
        /// Gets or Sets wheather we should open the door if the player is not authorized to do it.
        /// </summary>
        public bool Open
        {
            get
            {
                return _open;
            }
            set
            {
                if (_cancelled)
                    return;
                
                _open = value;
                if (_open == false)
                    _cancelled = true;
            }
        }

        /// <summary>
        /// Gets if the event was cancelled using the Open property.
        /// </summary>
        public bool Cancelled
        {
            get
            {
                return _cancelled;
            }
        }

        /// <summary>
        /// Gets the current state of the door
        /// </summary>
        public BasicDoor.State State
        {
            get
            {
                return _basicDoorState;
            }
            internal set
            {
                _basicDoorState = value;
            }
        }
    }
}