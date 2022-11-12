namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when an AI or player dies.
    /// </summary>
    public class DeathEvent : HurtEvent
    {
        private bool _drop;

        public DeathEvent(ref DamageEvent d)
            : base(ref d)
        {
            _drop = true;
        }

        /// <summary>
        /// Gets / Sets if we should drop the items on death.
        /// </summary>
        public bool DropItems
        {
            get
            {
                return _drop;
            }
            set
            {
                _drop = value;
            }
        }
    }
}