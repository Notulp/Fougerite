using System;

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a Player is below the waterline and the server is about to
    /// apply drowning damage.
    ///
    /// Vanilla killed outright the moment a player touched the waterline. That check now
    /// raises this event instead, so swimming is possible: cancel it to remove drowning
    /// entirely, or adjust DamageAmount to control how fast it kills.
    /// </summary>
    public class WaterDamageEvent
    {
        private readonly Player _player;
        private readonly float _depth;
        private readonly float _oxygen;
        private readonly float _submergedSeconds;
        private readonly bool _swimFlagSet;
        private float _damageAmount;
        private bool _cancelled;

        /// <summary>
        /// Initializes a new instance of <see cref="WaterDamageEvent"/>.
        /// </summary>
        public WaterDamageEvent(Player player, float depth, float oxygen, float submergedSeconds,
            bool swimFlagSet, float damageAmount)
        {
            _player = player;
            _depth = depth;
            _oxygen = oxygen;
            _submergedSeconds = submergedSeconds;
            _swimFlagSet = swimFlagSet;
            _damageAmount = damageAmount;
        }

        /// <summary>
        /// The player who is underwater.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// How far below the waterline the player is, in metres.
        /// </summary>
        public float Depth
        {
            get { return _depth; }
        }

        /// <summary>
        /// Remaining air from 0 to 1, as the server tracks it. Damage is only applied at 0.
        /// </summary>
        public float Oxygen
        {
            get { return _oxygen; }
        }

        /// <summary>
        /// How long the player has been continuously below the waterline, in seconds.
        /// </summary>
        public float SubmergedSeconds
        {
            get { return _submergedSeconds; }
        }

        /// <summary>
        /// Whether the client is reporting the swim state flag.
        ///
        /// Advisory only. The client sets this bit, so a modified client can simply withhold
        /// it. Drowning is driven by the player's position, which the server validates, not
        /// by this flag.
        /// </summary>
        public bool SwimFlagSet
        {
            get { return _swimFlagSet; }
        }

        /// <summary>
        /// How much damage is about to be applied. Settable, so a plugin can scale drowning
        /// without cancelling it.
        /// </summary>
        public float DamageAmount
        {
            get { return _damageAmount; }
            set { _damageAmount = value; }
        }

        /// <summary>
        /// Returns true if the event has been cancelled by a plugin.
        /// </summary>
        public bool Cancelled
        {
            get { return _cancelled; }
        }

        /// <summary>
        /// Cancels the event, so no drowning damage is applied this tick.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
    }
}
