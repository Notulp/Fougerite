namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a player's metabolism is updated.
    /// </summary>
    public class MetabolismEvent
    {
        private readonly Metabolism _m;
        private readonly Player _player;
        private readonly float _delta;
        private bool _cancelled;

        /// <summary>
        /// Initializes a new instance of the MetabolismEvent class.
        /// </summary>
        /// <param name="m">The metabolism component involved.</param>
        /// <param name="delta">The time elapsed since the last metabolic tick.</param>
        public MetabolismEvent(Metabolism m, float delta)
        {
            _m = m;
            _delta = delta;
            var character = m.GetComponent<Character>();
            if (character != null && character.netUser != null)
            {
                _player = Server.GetServer().FindPlayer(character.netUser.userID);
            }
        }

        /// <summary>
        /// Cancels the metabolism update.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }

        /// <summary>
        /// Gets whether the metabolism update has been cancelled.
        /// </summary>
        public bool Cancelled
        {
            get
            {
                return _cancelled;
            }
        }

        /// <summary>
        /// Returns the player whose metabolism is being updated.
        /// </summary>
        public Player Player
        {
            get
            {
                return _player;
            }
        }

        /// <summary>
        /// Returns the raw Metabolism component.
        /// </summary>
        public Metabolism Metabolism
        {
            get
            {
                return _m;
            }
        }

        /// <summary>
        /// Gets the time delta for this metabolic tick.
        /// </summary>
        public float Delta
        {
            get
            {
                return _delta;
            }
        }
    }
}