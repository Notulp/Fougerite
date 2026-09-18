using System;

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when the server is about to broadcast a movement sound for a
    /// Player, so nearby players can hear them.
    ///
    /// Vanilla only ever sent "HearFootstep", which is wrong for a swimmer: legacy has no
    /// water traversal, so someone crossing a lake was either silent or sounded like they
    /// were walking on gravel. The event carries the water state, so a plugin can swap in
    /// its own sound, change how far it carries, or cancel it outright.
    /// </summary>
    public class AudibleSoundEvent
    {
        private readonly Player _player;
        private readonly bool _inWater;
        private readonly bool _swimming;
        private readonly bool _sprinting;
        private readonly bool _crouching;
        private string _soundName;
        private float _range;
        private bool _cancelled;

        /// <summary>
        /// Initializes a new instance of <see cref="AudibleSoundEvent"/>.
        /// </summary>
        public AudibleSoundEvent(Player player, string soundName, float range, bool inWater,
            bool swimming, bool sprinting, bool crouching)
        {
            _player = player;
            _soundName = soundName;
            _range = range;
            _inWater = inWater;
            _swimming = swimming;
            _sprinting = sprinting;
            _crouching = crouching;
        }

        /// <summary>
        /// The player making the sound.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// The RPC name broadcast to nearby clients. Vanilla sends "HearFootstep". Set your
        /// own to play something else, as long as the client knows how to receive it.
        /// </summary>
        public string SoundName
        {
            get { return _soundName; }
            set { _soundName = value; }
        }

        /// <summary>
        /// How far the sound carries, in metres. Vanilla uses 5 walking and 10 sprinting.
        /// </summary>
        public float Range
        {
            get { return _range; }
            set { _range = value; }
        }

        /// <summary>
        /// True when any part of the player is below the waterline.
        /// </summary>
        public bool InWater
        {
            get { return _inWater; }
        }

        /// <summary>
        /// True when the player's client reports the swimming state flag.
        ///
        /// Advisory. The client sets this bit, so treat it as a hint about presentation
        /// rather than as something to base a rule on.
        /// </summary>
        public bool Swimming
        {
            get { return _swimming; }
        }

        /// <summary>
        /// True when the player is sprinting.
        /// </summary>
        public bool Sprinting
        {
            get { return _sprinting; }
        }

        /// <summary>
        /// True when the player is crouching.
        /// </summary>
        public bool Crouching
        {
            get { return _crouching; }
        }

        /// <summary>
        /// Returns true if the event has been cancelled by a plugin.
        /// </summary>
        public bool Cancelled
        {
            get { return _cancelled; }
        }

        /// <summary>
        /// Cancels the event, so nothing is broadcast and nobody hears this player.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
    }
}