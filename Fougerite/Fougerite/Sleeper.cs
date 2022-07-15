
using Fougerite.Caches;
using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// This class represents a sleeping player.
    /// </summary>
    public class Sleeper
    {
        private DeployableObject _sleeper;
        private ulong _uid;
        private int _instanceid;
        private string _name;
        public bool IsDestroyed = false;

        public Sleeper(DeployableObject obj)
        {
            this._sleeper = obj;
            this._instanceid = this._sleeper.GetInstanceID();
            this._uid = this._sleeper.ownerID;
            CachedPlayer cachedPlayer;
            bool success = PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(UID, out cachedPlayer);
            this._name = success ? cachedPlayer.Name : this._sleeper.ownerName;
        }

        /// <summary>
        /// Gets the Sleeper's health.
        /// </summary>
        public float Health
        {
            get
            {
                return this._sleeper.GetComponent<TakeDamage>().health;
            }
            set
            {
                this._sleeper.GetComponent<TakeDamage>().health = value;
                this.UpdateHealth();
            }
        }

        public void UpdateHealth()
        {
            this._sleeper.UpdateClientHealth();
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
                this._sleeper.OnKilled();
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
                NetCull.Destroy(this._sleeper.networkViewID);
            }
            catch { }
        }

        /// <summary>
        /// Returns the DeployableObject of the sleeper.
        /// </summary>
        public DeployableObject Object
        {
            get { return this._sleeper; }
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
            get { return this._uid.ToString(); }
        }

        /// <summary>
        /// Returns the SteamID of the sleeper.
        /// </summary>
        public string SteamID
        {
            get { return this._uid.ToString(); }
        }

        /// <summary>
        /// Returns the SteamID of the sleeper.
        /// </summary>
        public ulong UID
        {
            get { return this._uid; }
        }

        /// <summary>
        /// Returns the owner name of the sleeper.
        /// </summary>
        public string OwnerName
        {
            get { return this._sleeper.ownerName; }
        }

        /// <summary>
        /// Returns the Position of the sleeper.
        /// </summary>
        public Vector3 Location
        {
            get { return this._sleeper.transform.position; }
        }

        /// <summary>
        /// Returns the X coordinate of the sleeper.
        /// </summary>
        public float X
        {
            get { return this._sleeper.transform.position.x; }
        }

        /// <summary>
        /// Returns the Y coordinate of the sleeper.
        /// </summary>
        public float Y
        {
            get { return this._sleeper.transform.position.y; }
        }

        /// <summary>
        /// Returns the Z coordinate of the sleeper.
        /// </summary>
        public float Z
        {
            get { return this._sleeper.transform.position.z; }
        }

        /// <summary>
        /// Returns the InstanceID (Unique ID) of the sleeper.
        /// </summary>
        public int InstanceID
        {
            get
            {
                return this._instanceid;
            }
        }
    }
}
