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
            
            if (IsStructureMaster())
            {
                _ownerid = ((StructureMaster)Obj).ownerID;
                _creatorid = ((StructureMaster)Obj).creatorID;
                _name = "Structure Master";
            }

            if (IsStructure())
            {
                StructureComponent comp = Obj as StructureComponent;
                if (comp != null && comp._master != null)
                {
                    _ownerid = comp._master.ownerID;
                    _creatorid = comp._master.creatorID;
                    string clone = comp.ToString();
                    var index = clone.IndexOf("(Clone)");
                    _name = clone.Substring(0, index);
                }
            }

            if (IsDeployableObject())
            {
                DeployableObject dobj = Obj as DeployableObject;
                if (dobj != null)
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
                        var index = clone.IndexOf("(Clone)");
                        _name = clone.Substring(0, index);
                    }

                    var inventory = dobj.GetComponent<Inventory>();
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
            }
            else if (IsLootableObject())
            {
                _ownerid = 76561198095992578UL;
                _creatorid = 76561198095992578UL;
                var loot = Obj as LootableObject;
                if (loot != null)
                {
                    _name = loot.name;
                    var inventory = loot._inventory;
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
            }
            else if (IsSupplyCrate())
            {
                _ownerid = 76561198095992578UL;
                _creatorid = 76561198095992578UL;
                _name = "Supply Crate";
                var crate = Obj as SupplyCrate;
                if (crate != null)
                {
                    var inventory = crate.lootableObject._inventory;
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
            }
            else if (IsResourceTarget())
            {
                var x = (ResourceTarget)Obj;
                _ownerid = 76561198095992578UL;
                _creatorid = 76561198095992578UL;
                _name = x.name;
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
            if (IsDeployableObject() && GetObject<DeployableObject>().GetComponent<SleepingAvatar>() == null)
                GetObject<DeployableObject>().SetupCreator(p.PlayerClient.controllable);
            else if (IsStructureMaster())
                GetObject<StructureMaster>().SetupCreator(p.PlayerClient.controllable);
            else if (IsStructure())
            {
                foreach (Entity st in GetLinkedStructs())
                {
                    if (st.GetObject<StructureMaster>() != null)
                    {
                        GetObject<StructureMaster>().SetupCreator(p.PlayerClient.controllable);
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
            if (IsDeployableObject() && GetObject<DeployableObject>().GetComponent<SleepingAvatar>() == null)
            {
                DeployableObject deployableObject = GetObject<DeployableObject>();
                deployableObject.creatorID = steamId;
                deployableObject.ownerID = steamId;
                deployableObject.CacheCreator();
                deployableObject.CreatorSet();
            }
            else if (IsStructureMaster())
            {
                StructureMaster structureMaster = GetObject<StructureMaster>();
                structureMaster.creatorID = steamId;
                structureMaster.ownerID = steamId;
                structureMaster.CacheCreator();
            }
            else if (IsStructure())
            {
                foreach (Entity st in GetLinkedStructs())
                {
                    if (st.GetObject<StructureMaster>() != null)
                    {
                        StructureMaster structureMaster = GetObject<StructureMaster>();
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

            if (IsDeployableObject())
            {
                try
                {
                    GetObject<DeployableObject>().OnKilled();
                }
                catch
                {
                    TryNetCullDestroy();
                }
            }
            else if (IsStructure())
            {
                DestroyStructure(GetObject<StructureComponent>());
            }
            else if (IsStructureMaster())
            {
                HashSet<StructureComponent> components = GetObject<StructureMaster>()._structureComponents;
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
                if (IsDeployableObject())
                {
                    if (GetObject<DeployableObject>() != null)
                        NetCull.Destroy(GetObject<DeployableObject>().gameObject);
                }
                else if (IsStructureMaster())
                {
                    if (GetObject<StructureMaster>() != null)
                        NetCull.Destroy(GetObject<StructureMaster>().networkViewID);
                }
            }
            catch
            {
                // Ignore.
            }
        }

        private static void DestroyStructure(StructureComponent comp)
        {
            try
            {
                comp._master.RemoveComponent(comp);
                comp._master = null;
                comp.StartCoroutine("DelayedKill");
            }
            catch
            {
                NetCull.Destroy(comp.networkViewID);
            }
        }

        /// <summary>
        /// Gets all connected structures to the entity.
        /// </summary>
        /// <returns>Returns a list containing all connected structures. If the entity isn't a structure, then It returns It self in a list.</returns>
        public List<Entity> GetLinkedStructs()
        {
            List<Entity> list = new List<Entity>();
            var obj = Object as StructureComponent;
            if (obj == null)
            {
                list.Add(this);
                return list;
            }

            foreach (StructureComponent component in obj._master._structureComponents)
            {
                if (component != Object as StructureComponent)
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

        public TakeDamage GetTakeDamage()
        {
            if (IsDeployableObject())
            {
                return GetObject<DeployableObject>().GetComponent<TakeDamage>();
            }

            if (IsStructure())
            {
                return GetObject<StructureComponent>().GetComponent<TakeDamage>();
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
                if (IsResourceTarget())
                {
                    var x = (ResourceTarget)_obj;
                    return x;
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
                if (IsSupplyCrate())
                {
                    var x = (SupplyCrate)_obj;
                    return x; 
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
            if (IsDeployableObject())
                return GetObject<DeployableObject>().GetComponent<SaveableInventory>() != null;

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
            if (IsDeployableObject())
                return GetObject<DeployableObject>().GetComponent<SleepingAvatar>() != null;

            return false;
        }

        /// <summary>
        /// Checks if the object is a FireBarrel
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsFireBarrel()
        {
            if (IsDeployableObject())
                return GetObject<DeployableObject>().GetComponent<FireBarrel>() != null;

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
            if (IsDeployableObject())
            {
                GetObject<DeployableObject>().SetDecayEnabled(c);
            }
        }

        /// <summary>
        /// Update the Entity's health.
        /// </summary>
        public void UpdateHealth()
        {
            if (IsDeployableObject())
            {
                GetObject<DeployableObject>().UpdateClientHealth();
            }
            else if (IsStructure())
            {
                GetObject<StructureComponent>().UpdateClientHealth();
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
                if (IsDeployableObject())
                {
                    return GetObject<DeployableObject>().GetComponent<TakeDamage>().health;
                }

                if (IsStructure())
                {
                    return GetObject<StructureComponent>().GetComponent<TakeDamage>().health;
                }

                if (IsStructureMaster())
                {
                    float sum = GetObject<StructureMaster>()._structureComponents.Sum(s => s.GetComponent<TakeDamage>().health);
                    return sum;
                }

                return 0f;
            }
            set
            {
                if (IsDeployableObject())
                {
                    GetObject<DeployableObject>().GetComponent<TakeDamage>().health = value;
                }
                else if (IsStructure())
                {
                    GetObject<StructureComponent>().GetComponent<TakeDamage>().health = value;
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
                if (IsDeployableObject())
                {
                    return GetObject<DeployableObject>().GetComponent<TakeDamage>().maxHealth;
                }

                if (IsStructure())
                {
                    return GetObject<StructureComponent>().GetComponent<TakeDamage>().maxHealth;
                }

                if (IsStructureMaster())
                {
                    float sum = GetObject<StructureMaster>()._structureComponents.Sum(s => s.GetComponent<TakeDamage>().maxHealth);
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
                if (IsDeployableObject())
                    return GetObject<DeployableObject>().transform.position;
                if (IsStructure())
                    return GetObject<StructureComponent>().transform.position;
                if (IsStructureMaster())
                    return GetObject<StructureMaster>().containedBounds.center;
                if (IsBasicDoor())
                    return GetObject<BasicDoor>().transform.position;
                if (IsLootableObject())
                    return GetObject<LootableObject>().transform.position;
                if (IsResourceTarget())
                    return GetObject<ResourceTarget>().transform.position;
                if (IsSupplyCrate())
                    return GetObject<SupplyCrate>().transform.position;

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
                if (IsDeployableObject())
                    return GetObject<DeployableObject>().transform.rotation;
                if (IsSupplyCrate())
                    return GetObject<SupplyCrate>().transform.rotation;
                if (IsStructure())
                    return GetObject<StructureComponent>().transform.rotation;
                if (IsBasicDoor())
                    return GetObject<BasicDoor>().transform.rotation;
                if (IsLootableObject())
                    return GetObject<LootableObject>().transform.rotation;
                if (IsResourceTarget())
                    return GetObject<ResourceTarget>().transform.rotation;

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
        /// Grabs and stores the instance id of the entity which should be unique
        /// for entity lifetime while the server is running.
        /// It's different / server restart.
        /// </summary>
        private void GetInstanceId()
        {
            if (IsDeployableObject())
                _instanceId = GetObject<DeployableObject>().GetInstanceID();
            else if (IsStructure())
                _instanceId = GetObject<StructureComponent>().GetInstanceID();
            else if (IsStructureMaster())
                _instanceId = GetObject<StructureMaster>().GetInstanceID();
            else if (IsBasicDoor())
                _instanceId = GetObject<BasicDoor>().GetInstanceID();
            else if (IsLootableObject())
                _instanceId = GetObject<LootableObject>().GetInstanceID();
            else if (IsResourceTarget())
                _instanceId = GetObject<ResourceTarget>().GetInstanceID();
            else if (IsSupplyCrate())
                _instanceId = GetObject<SupplyCrate>().GetInstanceID();
        }
    }
}