using UnityEngine;

namespace Fougerite
{
    public class NPC
    {
        private Character _char;
        private readonly int _instanceId;
        private readonly string _name;
        private readonly HostileWildlifeAI _hostileWildlifeAI;
        private readonly BasicWildLifeAI _basicWildLifeAI;

        public NPC(Character c)
        {
            _char = c;
            _instanceId = _char.GetInstanceID();
            _name = _char.name;
            _name = _name.Contains("_A(Clone)") ? _name.Replace("_A(Clone)", "") : _name.Replace("(Clone)", "");
            _basicWildLifeAI = _char.GetComponent<BasicWildLifeAI>();
            _hostileWildlifeAI = _char.GetComponent<HostileWildlifeAI>();
        }

        /// <summary>
        /// Kills the NPC.
        /// </summary>
        public void Kill()
        {
            if (Character.alive)
            {
                Character.Signal_ServerCharacterDeath();
                Character.SendMessage("OnKilled", new DamageEvent(), SendMessageOptions.DontRequireReceiver);
            }
        }
        
        /// <summary>
        /// Deals a specific amount of damage to the NPC.
        /// </summary>
        /// <param name="dmg"></param>
        public void Damage(float dmg)
        {
            if (IsAlive)
            {
                TakeDamage.HurtSelf(_char, dmg);
            }
        }

        /// <summary>
        /// Returns the HostileWildlifeAI component.
        /// Null if the NPC isn't a HostileWildlifeAI.
        /// </summary>
        public HostileWildlifeAI HostileWildlifeAI
        {
            get
            {
                return _hostileWildlifeAI;
            }
        }
        
        /// <summary>
        /// Returns the BasicWildLifeAI component.
        /// Every AI class derives from this, so this should never return null.
        /// </summary>
        public BasicWildLifeAI BasicWildLifeAI
        {
            get
            {
                return _basicWildLifeAI;
            }
        }

        /// <summary>
        /// Returns If the NPC is alive.
        /// </summary>
        public bool IsAlive
        {
            get { return Character.alive; }
        }

        /// <summary>
        /// Returns the Character of the NPC.
        /// </summary>
        public Character Character
        {
            get { return _char; }
            set { _char = value; }
        }

        /// <summary>
        /// Returns the Health of the NPC.
        /// </summary>
        public float Health
        {
            get { return _char.health; }
            set { _char.takeDamage.health = value; }
        }

        /// <summary>
        /// Returns the Name of the NPC.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Returns the Location of the NPC.
        /// </summary>
        public Vector3 Location
        {
            get { return _char.transform.position; }
        }

        /// <summary>
        /// Location X Coordinate
        /// </summary>
        public float X
        {
            get { return _char.transform.position.x; }
        }

        /// <summary>
        /// Location Y Coordinate
        /// </summary>
        public float Y
        {
            get { return _char.transform.position.y; }
        }

        /// <summary>
        /// Location Z Coordinate
        /// </summary>
        public float Z
        {
            get { return _char.transform.position.z; }
        }

        /// <summary>
        /// Returns the InstanceID (Unique ID) of the NPC.
        /// </summary>
        public int InstanceID
        {
            get { return _instanceId; }
        }
        
        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static bool operator ==(NPC b1, NPC b2)
        {
            if (ReferenceEquals(b1, b2)) 
                return true;
            if (ReferenceEquals(b1, null)) 
                return false;
            if (ReferenceEquals(b2, null))
                return false;

            return b1.Equals(b2);
        }

        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static bool operator !=(NPC b1, NPC b2)
        {
            return !(b1 == b2);
        }

        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            NPC b2 = obj as NPC;
            return b2 != null && _name == b2._name && b2._instanceId == _instanceId;
        }

        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _instanceId;
        }
    }
}