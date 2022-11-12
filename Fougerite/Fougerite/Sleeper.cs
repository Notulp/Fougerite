
using Fougerite.Caches;
using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// This class represents a sleeping player.
    /// </summary>
    public class Sleeper
    {
        private readonly DeployableObject _sleeper;
        private readonly ulong _uid;
        private readonly int _instanceid;
        private readonly string _name;
        public bool IsDestroyed;

        public Sleeper(DeployableObject obj)
        {
            _sleeper = obj;
            _instanceid = _sleeper.GetInstanceID();
            _uid = _sleeper.ownerID;
            CachedPlayer cachedPlayer;
            bool success = PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(UID, out cachedPlayer);
            _name = success ? cachedPlayer.Name : _sleeper.ownerName;
        }

        /// <summary>
        /// Gets the Sleeper's health.
        /// </summary>
        public float Health
        {
            get
            {
                return _sleeper.GetComponent<TakeDamage>().health;
            }
            set
            {
                _sleeper.GetComponent<TakeDamage>().health = value;
                UpdateHealth();
            }
        }

        public void UpdateHealth()
        {
            _sleeper.UpdateClientHealth();
        }

        /// <summary>
        /// Destroys the sleeper.
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }
            
            try
            {
                _sleeper.OnKilled();
            }
            catch
            {
                TryNetCullDestroy();
            }
            IsDestroyed = true;
        }

        private void TryNetCullDestroy()
        {
            try
            {
                NetCull.Destroy(_sleeper.networkViewID);
            }
            catch
            {
                // Ignore.
            }
        }

        /// <summary>
        /// Returns the DeployableObject of the sleeper.
        /// </summary>
        public DeployableObject Object
        {
            get { return _sleeper; }
        }

        /// <summary>
        /// Returns the Name of the sleeper.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Returns the SteamID of the sleeper.
        /// </summary>
        public string OwnerID
        {
            get { return _uid.ToString(); }
        }

        /// <summary>
        /// Returns the SteamID of the sleeper.
        /// </summary>
        public string SteamID
        {
            get { return _uid.ToString(); }
        }

        /// <summary>
        /// Returns the SteamID of the sleeper.
        /// </summary>
        public ulong UID
        {
            get { return _uid; }
        }

        /// <summary>
        /// Returns the owner name of the sleeper.
        /// </summary>
        public string OwnerName
        {
            get { return _sleeper.ownerName; }
        }

        /// <summary>
        /// Returns the Position of the sleeper.
        /// </summary>
        public Vector3 Location
        {
            get { return _sleeper.transform.position; }
        }

        /// <summary>
        /// Returns the X coordinate of the sleeper.
        /// </summary>
        public float X
        {
            get { return _sleeper.transform.position.x; }
        }

        /// <summary>
        /// Returns the Y coordinate of the sleeper.
        /// </summary>
        public float Y
        {
            get { return _sleeper.transform.position.y; }
        }

        /// <summary>
        /// Returns the Z coordinate of the sleeper.
        /// </summary>
        public float Z
        {
            get { return _sleeper.transform.position.z; }
        }

        /// <summary>
        /// Returns the InstanceID (Unique ID) of the sleeper.
        /// </summary>
        public int InstanceID
        {
            get
            {
                return _instanceid;
            }
        }
    }
}
