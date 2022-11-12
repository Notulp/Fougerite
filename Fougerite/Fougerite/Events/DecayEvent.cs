namespace Fougerite.Events
{

    /// <summary>
    /// This class is created on decay event.
    /// </summary>
    public class DecayEvent
    {
        private float _dmg;
        private Entity _ent;

        public DecayEvent(Entity en, ref float dmg)
        {
            Entity = en;
            DamageAmount = dmg;
        }

        /// <summary>
        /// Gets / Sets the damage of the decay event.
        /// </summary>
        public float DamageAmount
        {
            get
            {
                return _dmg;
            }
            set
            {
                _dmg = value;
            }
        }

        /// <summary>
        /// Gets the Entity that the decay is running on.
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
    }
}