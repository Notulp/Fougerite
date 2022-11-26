using Fougerite.Caches;
using UnityEngine;

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when someone starts looting something.
    /// </summary>
    public class LootStartEvent
    {
        private bool _cancel;
        private readonly Player _player;
        private readonly LootableObject _lo;
        private readonly Useable _ue;
        private readonly Entity _entity;
        private readonly bool _isobject;
        private readonly uLink.NetworkPlayer _np;

        public LootStartEvent(LootableObject lo, Player player, Useable use, uLink.NetworkPlayer nplayer)
        {
            _lo = lo;
            _ue = use;
            _player = player;
            _np = nplayer;
            foreach (Collider collider in Physics.OverlapSphere(lo._inventory.transform.position, 1.2f))
            {
                if (collider.GetComponent<DeployableObject>() != null)
                {
                    DeployableObject deployableObject = collider.GetComponent<DeployableObject>();
                    _entity = EntityCache.GetInstance().GrabOrAllocate(deployableObject.GetInstanceID(), deployableObject);
                    _isobject = true;
                    break;
                }
                if (collider.GetComponent<LootableObject>() != null)
                {
                    LootableObject lootableObject = collider.GetComponent<LootableObject>();
                    _entity = EntityCache.GetInstance().GrabOrAllocate(lootableObject.GetInstanceID(), lootableObject);
                    _isobject = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            _cancel = true;
        }

        /// <summary>
        /// Checks if the stuff we are looting is a storage.
        /// </summary>
        public bool IsObject
        {
            get
            {
                return _isobject;
            }
        }

        /// <summary>
        /// Gets the Entity we are looting.
        /// </summary>
        public Entity Entity
        {
            get
            {
                return _entity;
            }
        }

        /// <summary>
        /// Gets the player who is looting.
        /// </summary>
        public Player Player
        {
            get
            {
                return _player;
            }
        }

        /// <summary>
        /// Gets the Useable class.
        /// </summary>
        public Useable Useable
        {
            get
            {
                return _ue;
            }
        }

        /// <summary>
        /// Gets the LootableObject class.
        /// </summary>
        public LootableObject LootableObject
        {
            get
            {
                return _lo;
            }
        }

        /// <summary>
        /// Checks if the event is cancelled.
        /// </summary>
        public bool IsCancelled
        {
            get
            {
                return _cancel;
            }
        }

        /// <summary>
        /// Gets the lootable object's name.
        /// </summary>
        public string LootName
        {
            get
            {
                return _lo.name;
            }
        }

        /// <summary>
        /// Gets the original Inventory class.
        /// </summary>
        public Inventory RustInventory
        {
            get
            {
                return _lo._inventory;
            }
        }

        /// <summary>
        /// Can change the occupied text?
        /// </summary>
        public string OccupiedText
        {
            get
            {
                return _lo.occupierText;
            }
            set
            {
                _lo.occupierText = value;
            }
        }
    }
}
