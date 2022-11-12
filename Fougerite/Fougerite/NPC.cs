using UnityEngine;

namespace Fougerite
{
    public class NPC
    {
        private Character _char;

        public NPC(Character c)
        {
            _char = c;
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
            get
            {
                return _char.name.Contains("_A(Clone)")
                    ? _char.name.Replace("_A(Clone)", "")
                    : _char.name.Replace("(Clone)", "");
            }
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
    }
}