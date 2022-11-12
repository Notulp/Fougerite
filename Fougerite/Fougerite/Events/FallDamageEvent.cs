namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when the player suffers fall damage.
    /// </summary>
    public class FallDamageEvent
    {
        private readonly float _fallspeed;
        private readonly float _num;
        private readonly FallDamage _fd;
        private readonly bool _flag;
        private readonly bool _flag2;
        private readonly Player _player;

        public FallDamageEvent(FallDamage fd, float speed, float num, bool flag, bool flag2)
        {
            _fd = fd;
            _player = Server.GetServer().FindPlayer(fd.idMain.netUser.userID);
            _fallspeed = speed;
            _num = num;
            _flag = flag;
            _flag2 = flag2;
        }

        /// <summary>
        /// Gets the player of the event.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// Gets the speed of the fall.
        /// </summary>
        public float FloatSpeed
        {
            get { return _fallspeed; }
        }

        /// <summary>
        /// Gets the damage of the fall damage.
        /// </summary>
        public float Num
        {
            get { return _num; }
        }

        /// <summary>
        /// Returns the original FallDamage class
        /// </summary>
        public FallDamage FallDamage
        {
            get { return _fd; }
        }
        
        /// <summary>
        /// Checks if the player is going to bleed from this event.
        /// </summary>
        public bool Bleeding
        {
            get { return _flag; }
        }

        /// <summary>
        /// Checks if the player is going to get broken legs from this event.
        /// </summary>
        public bool BrokenLegs
        {
            get { return _flag2; }
        }

        /// <summary>
        /// Cancels the fall damage event.
        /// </summary>
        public void Cancel()
        {
            if (_player.IsOnline)
            {
                if (BrokenLegs)
                {
                    _fd.ClearInjury();
                }
                if (Bleeding)
                {
                    _player.HumanBodyTakeDmg.SetBleedingLevel(0f);
                }
            }
        }
    }
}
