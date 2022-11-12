namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a door is opened or closed.
    /// </summary>
    public class DoorEvent
    {
        private Entity _ent;
        private bool _open;

        public DoorEvent(Entity e)
        {
            Open = false;
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
                _ent = value;
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
                _open = value;
            }
        }
    }
}