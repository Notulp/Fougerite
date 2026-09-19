namespace Fougerite.Events
{
    public enum AnimalMovementType
    {
        NavMeshMovement,
        AnimalMovement
    }
    
    /// <summary>
    /// This event is fired when an animal NPC moves.
    /// </summary>
    public class AnimalMovementEvent
    {
        private readonly NPC _npc;
        private readonly NavMeshMovement _movement;
        private readonly ulong _simMillis;
        private readonly AnimalMovementType _type;
        private readonly BasicWildLifeMovement _basicWildLifeMovement;
        
        public AnimalMovementEvent(NPC npc, NavMeshMovement movement, ulong simMillis, AnimalMovementType type, BasicWildLifeMovement basicWildLifeMovement)
        {
            _npc = npc;
            _movement = movement;
            _simMillis = simMillis;
            _type = type;
            _basicWildLifeMovement = basicWildLifeMovement;
        }

        /// <summary>
        /// The NPC that is moving.
        /// </summary>
        public NPC NPC
        {
            get { return _npc; }
        }
        
        /// <summary>
        /// The NavMeshMovement component of the animal.
        /// Null if Type is not NavMeshMovement.
        /// </summary>
        public NavMeshMovement NavMeshMovement
        {
            get { return _movement; }
        }
        
        /// <summary>
        /// The simulation time in milliseconds when the movement update occurred.
        /// Calculated by WildlifeManager. It's obfuscated in the original code.
        /// Figure it out if you must.
        /// </summary>
        public ulong SimMillis
        {
            get { return _simMillis; }
        }

        /// <summary>
        /// Specifies the type of movement associated with the animal NPC.
        /// </summary>
        public AnimalMovementType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Provides movement control for wildlife NPCs using basic wildlife movement logic.
        /// Null if Type is not AnimalMovement.
        /// </summary>
        public BasicWildLifeMovement BasicWildLifeMovement
        {
            get { return _basicWildLifeMovement; }
        }
    }
}