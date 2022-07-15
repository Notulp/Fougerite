namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a door is opened or closed.
    /// </summary>
    public class DoorEvent
    {
        private Fougerite.Entity _ent;
        private bool _open;

        public DoorEvent(Fougerite.Entity e)
        {
            this.Open = false;
            this.Entity = e;
        }

        /// <summary>
        /// Gets the door's entity.
        /// </summary>
        public Fougerite.Entity Entity
        {
            get
            {
                return this._ent;
            }
            set
            {
                this._ent = value;
            }
        }

        /// <summary>
        /// Gets or Sets wheather we should open the door if the player is not authorized to do it.
        /// </summary>
        public bool Open
        {
            get
            {
                return this._open;
            }
            set
            {
                this._open = value;
            }
        }
    }
}