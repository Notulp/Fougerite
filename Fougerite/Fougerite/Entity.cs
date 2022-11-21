using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Fougerite.Caches;

namespace Fougerite
{
    /// <summary>
    /// Represents an object on the server. This class is an extended API for easier / safer use.
    /// </summary>
    public class Entity
    {
        public readonly bool hasInventory;
        private readonly object _obj;
        private readonly EntityInv inv;
        private readonly ulong _ownerid;
        private readonly ulong _creatorid;
        private readonly string _creatorname;
        private readonly string _name;
        private readonly string _ownername;
        private int _instanceId;
        public bool IsDestroyed = false;

        public Entity(object Obj)
        {
            _obj = Obj;
            // Cache InstanceId
            GetInstanceId();
            
            if (GetObject(out StructureMaster structureMaster))
            {
                _ownerid = structureMaster.ownerID;
                _creatorid = structureMaster.creatorID;
                _name = "Structure Master";
            }

            if (GetObject(out StructureComponent comp))
            {
                if (comp._master != null)
                {
                    _ownerid = comp._master.ownerID;
                    _creatorid = comp._master.creatorID;
                    string clone = comp.ToString();
                    var index = clone.IndexOf("(Clone)");
                    _name = clone.Substring(0, index);
                }
            }

            if (GetObject(out DeployableObject dobj))
            {
                _ownerid = dobj.ownerID;
                _creatorid = dobj.creatorID;
                string clone = dobj.ToString();
                if (clone.Contains("Barricade"))
                {
                    _name = "Wood Barricade";
                }
                else
                {
                    int index = clone.IndexOf("(Clone)");
                    _name = clone.Substring(0, index);
                }

                Inventory inventory = dobj.GetComponent<Inventory>();
                if (inventory != null)
                {
                    hasInventory = true;
                    inv = new EntityInv(inventory, this);
                }
                else
                {
                    hasInventory = false;
                }

            }
            else if (GetObject(out LootableObject loot))
            {
                _ownerid = 76561198095992578UL;
                _creatorid = 76561198095992578UL;
                _name = loot.name;
                Inventory inventory = loot._inventory;
                if (inventory != null)
                {
                    hasInventory = true;
                    inv = new EntityInv(inventory, this);
                }
                else
                {
                    hasInventory = false;
                }
            }
            else if (GetObject(out SupplyCrate crate))
            {
                _ownerid = 76561198095992578UL;
                _creatorid = 76561198095992578UL;
                _name = "Supply Crate";
                Inventory inventory = crate.lootableObject._inventory;
                if (inventory != null)
                {
                    hasInventory = true;
                    inv = new EntityInv(inventory, this);
                }
                else
                {
                    hasInventory = false;
                }
            }
            else if (GetObject(out ResourceTarget resourceTarget))
            {
                _ownerid = 76561198095992578UL;
                _creatorid = 76561198095992578UL;
                _name = resourceTarget.name;
                hasInventory = false;
            }
            else
            {
                hasInventory = false;
            }

            Player ownerCache = Server.GetServer().GetCachePlayer(_ownerid);
            CachedPlayer cachedPlayerOwner;

            if (ownerCache != null)
            {
                _ownername = ownerCache.Name;
            }
            else if (PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(_ownerid, out cachedPlayerOwner))
            {
                _ownername = cachedPlayerOwner.Name;
            }
            else if (Server.GetServer().HasRustPP)
            {
                if (Server.GetServer().GetRustPPAPI().Cache.ContainsKey(_ownerid))
                {
                    _ownername = Server.GetServer().GetRustPPAPI().Cache[_ownerid];
                }
            }
            else
            {
                _ownername = "UnKnown";
            }

            Player creatorCache = Server.GetServer().GetCachePlayer(_creatorid);
            CachedPlayer cachedPlayerCreator;

            if (creatorCache != null)
            {
                _creatorname = creatorCache.Name;
            }
            else if (PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(_creatorid, out cachedPlayerCreator))
            {
                _ownername = cachedPlayerCreator.Name;
            }
            else if (Server.GetServer().HasRustPP)
            {
                if (Server.GetServer().GetRustPPAPI().Cache.ContainsKey(_creatorid))
                {
                    _creatorname = Server.GetServer().GetRustPPAPI().Cache[_creatorid];
                }
            }
            else
            {
                _creatorname = "UnKnown";
            }
        }

        /// <summary>
        /// Changes the Entity's owner to the specified player.
        /// </summary>
        /// <param name="p"></param>
        public void ChangeOwner(Player p)
        {
            if (GetObject(out DeployableObject deployableObject) && GetObject<DeployableObject>().GetComponent<SleepingAvatar>() == null)
                deployableObject.SetupCreator(p.PlayerClient.controllable);
            else if (GetObject(out StructureMaster structureMaster2))
                structureMaster2.SetupCreator(p.PlayerClient.controllable);
            else if (IsStructure())
            {
                foreach (Entity st in GetLinkedStructs())
                {
                    if (st.GetObject(out StructureMaster structureMaster))
                    {
                        structureMaster.SetupCreator(p.PlayerClient.controllable);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Changes the Entity's owner to the specified steamid.
        /// </summary>
        /// <param name="steamId"></param>
        public void ChangeOwner(ulong steamId)
        {
            if (GetObject(out DeployableObject deployableObject) && GetObject<DeployableObject>().GetComponent<SleepingAvatar>() == null)
            {
                deployableObject.creatorID = steamId;
                deployableObject.ownerID = steamId;
                deployableObject.CacheCreator();
                deployableObject.CreatorSet();
            }
            else if (GetObject(out StructureMaster structureMaster2))
            {
                structureMaster2.creatorID = steamId;
                structureMaster2.ownerID = steamId;
                structureMaster2.CacheCreator();
            }
            else if (IsStructure())
            {
                foreach (Entity st in GetLinkedStructs())
                {
                    if (st.GetObject(out StructureMaster structureMaster))
                    {
                        structureMaster.creatorID = steamId;
                        structureMaster.ownerID = steamId;
                        structureMaster.CacheCreator();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Destroys the entity.
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed)
            {
                return;
            }

            if (GetObject(out DeployableObject deployableObject))
            {
                try
                {
                    deployableObject.OnKilled();
                }
                catch
                {
                    TryNetCullDestroy();
                }
            }
            else if (GetObject(out StructureComponent structureComponent))
            {
                DestroyStructure(structureComponent);
            }
            else if (GetObject(out StructureMaster structureMaster))
            {
                HashSet<StructureComponent> components = structureMaster._structureComponents;
                foreach (StructureComponent comp in components)
                    DestroyStructure(comp);

                try
                {
                    GetObject<StructureMaster>().OnDestroy();
                }
                catch
                {
                    TryNetCullDestroy();
                }
            }

            IsDestroyed = true;
        }

        private void TryNetCullDestroy()
        {
            try
            {
                if (GetObject(out DeployableObject deployableObject))
                    NetCull.Destroy(deployableObject.gameObject);
                else if (GetObject(out StructureMaster structureMaster))
                {
                    if (structureMaster.networkViewID != uLink.NetworkViewID.unassigned)
                        NetCull.Destroy(structureMaster.networkViewID);
                    else if (structureMaster.gameObject != null)
                        NetCull.Destroy(structureMaster.gameObject);
                }
            }
            catch
            {
                // Ignore.
            }
        }

        private static void DestroyStructure(StructureComponent comp)
        {
            // Sanity check, shouldn't happen.
            if (comp == null)
                return;
            
            try
            {
                comp._master.RemoveComponent(comp);
                comp._master = null;
                comp.StartCoroutine("DelayedKill");
            }
            catch
            {
                if (comp.networkViewID != uLink.NetworkViewID.unassigned)
                    NetCull.Destroy(comp.networkViewID);
                else if (comp.gameObject != null)
                    NetCull.Destroy(comp.gameObject);
            }
        }

        /// <summary>
        /// Gets all connected structures to the entity.
        /// </summary>
        /// <returns>Returns a list containing all connected structures. If the entity isn't a structure, then It returns It self in a list.</returns>
        public List<Entity> GetLinkedStructs()
        {
            List<Entity> list;
            if (!GetObject(out StructureComponent obj))
            {
                list = new List<Entity>(1)
                {
                    this
                };
                return list;
            }

            list = new List<Entity>(obj._master._structureComponents.Count);
            foreach (StructureComponent component in obj._master._structureComponents)
            {
                if (component != obj)
                {
                    list.Add(new Entity(component));
                }
            }

            return list;
        }

        /// <summary>
        /// Casts the object to the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetObject<T>()
        {
            if (Object is T objectType)
            {
                return objectType;
            }

            return default(T);
        }
        
        /// <summary>
        /// Casts the object to the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool GetObject<T>(out T type)
        {
            if (Object is T objectType)
            {
                type = objectType;
                return true;
            }

            type = default(T);
            return false;
        }

        public TakeDamage GetTakeDamage()
        {
            if (GetObject(out DeployableObject deployableObject))
            {
                return deployableObject.GetComponent<TakeDamage>();
            }

            if (GetObject(out StructureComponent structureComponent))
            {
                return structureComponent.GetComponent<TakeDamage>();
            }

            return null;
        }

        /// <summary>
        /// Returns the Object as a ResourceTarget If possible.
        /// </summary>
        public ResourceTarget ResourceTarget
        {
            get
            {
                if (GetObject(out ResourceTarget resourceTarget))
                {
                    return resourceTarget;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the Object as a SupplyCrate If possible.
        /// </summary>
        public SupplyCrate SupplyCrate
        {
            get
            {
                if (GetObject(out SupplyCrate supplyCrate))
                {
                    return supplyCrate;
                }

                return null;
            }
        }

        /// <summary>
        /// Checks if the object is a ResourceTarget
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsResourceTarget()
        {
            ResourceTarget str = Object as ResourceTarget;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a BasicDoor
        /// </summary>
        /// <returns></returns>
        public bool IsBasicDoor()
        {
            BasicDoor str = Object as BasicDoor;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a LootableObject
        /// </summary>
        /// <returns></returns>
        public bool IsLootableObject()
        {
            LootableObject str = Object as LootableObject;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a DeployableObject
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsDeployableObject()
        {
            DeployableObject str = Object as DeployableObject;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a Chest or a Stash.
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsStorage()
        {
            if (GetObject(out DeployableObject deployableObject))
                return deployableObject.GetComponent<SaveableInventory>() != null;

            return false;
        }

        /// <summary>
        /// Checks if the object is a StructureComponent
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsStructure()
        {
            StructureComponent str = Object as StructureComponent;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a StructureMaster
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsStructureMaster()
        {
            StructureMaster str = Object as StructureMaster;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a SleepingAvatar
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsSleeper()
        {
            if (GetObject(out DeployableObject deployableObject))
                return deployableObject.GetComponent<SleepingAvatar>() != null;

            return false;
        }

        /// <summary>
        /// Checks if the object is a FireBarrel
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsFireBarrel()
        {
            if (GetObject(out DeployableObject deployableObject))
                return deployableObject.GetComponent<FireBarrel>() != null;

            return false;
        }

        /// <summary>
        /// Checks if the object is a SupplyCrate
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsSupplyCrate()
        {
            SupplyCrate str = Object as SupplyCrate;
            return str != null;
        }

        /// <summary>
        /// Enable / Disable the Default Rust Decay on this object?
        /// </summary>
        /// <param name="c"></param>
        public void SetDecayEnabled(bool c)
        {
            if (GetObject(out DeployableObject deployableObject))
            {
                deployableObject.SetDecayEnabled(c);
            }
        }

        /// <summary>
        /// Update the Entity's health.
        /// </summary>
        public void UpdateHealth()
        {
            if (GetObject(out DeployableObject deployableObject))
            {
                deployableObject.UpdateClientHealth();
            }
            else if (GetObject(out StructureComponent structureComponent))
            {
                structureComponent.UpdateClientHealth();
            }
        }

        /// <summary>
        /// Tries to find the Creator of the object in the cache or through the online players. Returns null otherwise.
        /// </summary>
        public Player Creator
        {
            get
            {
                Player creatorPlayer = Server.GetServer().GetCachePlayer(_creatorid);
                Player ownerPlayer = Server.GetServer().GetCachePlayer(_ownerid);
                return creatorPlayer ?? ownerPlayer;
            }
        }

        /// <summary>
        /// Gets the ownername of the Entity
        /// </summary>
        public string OwnerName
        {
            get { return _ownername; }
        }

        /// <summary>
        /// Gets the creatorname of the Entity
        /// </summary>
        public string CreatorName
        {
            get { return _creatorname; }
        }

        /// <summary>
        /// Returns the OwnerID as a string
        /// </summary>
        public string OwnerID
        {
            get { return _ownerid.ToString(); }
        }

        /// <summary>
        /// Returns the OwnerID as a ulong
        /// </summary>
        public ulong UOwnerID
        {
            get { return _ownerid; }
        }

        /// <summary>
        /// Returns the CreatorID as a string
        /// </summary>
        public string CreatorID
        {
            get { return _creatorid.ToString(); }
        }

        /// <summary>
        /// Returns the OwnerID as a ulong
        /// </summary>
        public ulong UCreatorID
        {
            get { return _creatorid; }
        }

        /// <summary>
        /// Returns the current health of the entity. Setting It will also update the health.
        /// </summary>
        public float Health
        {
            get
            {
                if (GetObject(out DeployableObject deployableObject))
                {
                    return deployableObject.GetComponent<TakeDamage>().health;
                }

                if (GetObject(out StructureComponent structureComponent))
                {
                    return structureComponent.GetComponent<TakeDamage>().health;
                }

                if (GetObject(out StructureMaster structureMaster))
                {
                    float sum = structureMaster._structureComponents.Sum(s => s.GetComponent<TakeDamage>().health);
                    return sum;
                }

                return 0f;
            }
            set
            {
                if (GetObject(out DeployableObject deployableObject))
                {
                    deployableObject.GetComponent<TakeDamage>().health = value;
                }
                else if (GetObject(out StructureComponent structureComponent))
                {
                    structureComponent.GetComponent<TakeDamage>().health = value;
                }

                UpdateHealth();
            }
        }

        /// <summary>
        /// Gets the maxhealth of the Entity.
        /// </summary>
        public float MaxHealth
        {
            get
            {
                if (GetObject(out DeployableObject deployableObject))
                {
                    return deployableObject.GetComponent<TakeDamage>().maxHealth;
                }

                if (GetObject(out StructureComponent structureComponent))
                {
                    return structureComponent.GetComponent<TakeDamage>().maxHealth;
                }

                if (GetObject(out StructureMaster structureMaster))
                {
                    float sum = structureMaster._structureComponents.Sum(s => s.GetComponent<TakeDamage>().maxHealth);
                    return sum;
                }

                return 0f;
            }
        }

        /// <summary>
        /// Gets the unique ID of the entity.
        /// </summary>
        public int InstanceID
        {
            get
            {
                return _instanceId;
            }
        }

        /// <summary>
        /// Gets the inventory of the Entity if possible.
        /// </summary>
        public EntityInv Inventory
        {
            get
            {
                if (hasInventory)
                    return inv;
                return null;
            }
        }

        /// <summary>
        /// Gets the name of the Entity
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Returns the Original Object type of this Entity. (Like DeployaleObject, StructureComponent, SupplyCrate, etc.)
        /// </summary>
        public object Object
        {
            get { return _obj; }
        }

        /// <summary>
        /// Returns the Owner of the Entity IF ONLINE.
        /// </summary>
        public Player Owner
        {
            get { return Player.FindByGameID(OwnerID); }
        }

        /// <summary>
        /// Returns the location of the Entity.
        /// </summary>
        public Vector3 Location
        {
            get
            {
                if (GetObject(out DeployableObject deployableObject))
                    return deployableObject.transform.position;
                if (GetObject(out StructureComponent structureComponent))
                    return structureComponent.transform.position;
                if (GetObject(out StructureMaster structureMaster))
                    return structureMaster.containedBounds.center;
                if (GetObject(out BasicDoor basicDoor))
                    return basicDoor.transform.position;
                if (GetObject(out LootableObject lootableObject))
                    return lootableObject.transform.position;
                if (GetObject(out ResourceTarget resourceTarget))
                    return resourceTarget.transform.position;
                if (GetObject(out SupplyCrate supplyCrate))
                    return supplyCrate.transform.position;

                return Vector3.zero;
            }
        }

        /// <summary>
        /// Returns the rotation of the Entity.
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                if (GetObject(out DeployableObject deployableObject))
                    return deployableObject.transform.rotation;
                if (GetObject(out StructureComponent structureComponent))
                    return structureComponent.transform.rotation;
                if (GetObject(out BasicDoor basicDoor))
                    return basicDoor.transform.rotation;
                if (GetObject(out LootableObject lootableObject))
                    return lootableObject.transform.rotation;
                if (GetObject(out ResourceTarget resourceTarget))
                    return resourceTarget.transform.rotation;
                if (GetObject(out SupplyCrate supplyCrate))
                    return supplyCrate.transform.rotation;

                return new Quaternion(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Returns the X coordinate of the Entity
        /// </summary>
        public float X
        {
            get { return Location.x; }
        }

        /// <summary>
        /// Returns the Y coordinate of the Entity
        /// </summary>
        public float Y
        {
            get { return Location.y; }
        }

        /// <summary>
        /// Returns the Z coordinate of the Entity
        /// </summary>
        public float Z
        {
            get { return Location.z; }
        }
        
        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static bool operator ==(Entity b1, Entity b2)
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
        public static bool operator !=(Entity b1, Entity b2)
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

            Entity b2 = obj as Entity;
            return b2 != null && _instanceId == b2.InstanceID;
        }

        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _instanceId;
        }

        /// <summary>
        /// Grabs and stores the instance id of the entity which should be unique
        /// for entity lifetime while the server is running.
        /// It's different / server restart.
        /// </summary>
        private void GetInstanceId()
        {
            if (GetObject(out DeployableObject deployableObject))
                _instanceId = deployableObject.GetInstanceID();
            else if (GetObject(out StructureComponent structureComponent))
                _instanceId = structureComponent.GetInstanceID();
            else if (GetObject(out StructureMaster structureMaster))
                _instanceId = structureMaster.GetInstanceID();
            else if (GetObject(out BasicDoor basicDoor))
                _instanceId = basicDoor.GetInstanceID();
            else if (GetObject(out LootableObject lootableObject))
                _instanceId = lootableObject.GetInstanceID();
            else if (GetObject(out ResourceTarget resourceTarget))
                _instanceId = resourceTarget.GetInstanceID();
            else if (GetObject(out SupplyCrate supplyCrate))
                _instanceId = supplyCrate.GetInstanceID();
        }
    }
}