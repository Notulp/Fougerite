
using Fougerite.Caches;

namespace Fougerite
{
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;

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
        public bool IsDestroyed = false;

        public Entity(object Obj)
        {
            this._obj = Obj;
            if (this.IsStructureMaster())
            {
                this._ownerid = (Obj as StructureMaster).ownerID;
                this._creatorid = (Obj as StructureMaster).creatorID;
                this._name = "Structure Master";
            }

            if (this.IsStructure())
            {
                StructureComponent comp = Obj as StructureComponent;

                if (comp != null && comp._master != null)
                {
                    this._ownerid = comp._master.ownerID;
                    this._creatorid = comp._master.creatorID;
                }
                string clone = this.GetObject<StructureComponent>().ToString();
                var index = clone.IndexOf("(Clone)");
                this._name = clone.Substring(0, index);
            }
            if (this.IsDeployableObject())
            {
                DeployableObject dobj = Obj as DeployableObject;
                this._ownerid = dobj.ownerID;
                this._creatorid = dobj.creatorID;
                string clone = this.GetObject<DeployableObject>().ToString();
                if (clone.Contains("Barricade"))
                {
                    this._name = "Wood Barricade";
                }
                else
                {
                    var index = clone.IndexOf("(Clone)");
                    this._name = clone.Substring(0, index);
                }

                var inventory = dobj.GetComponent<Inventory>();
                if (inventory != null)
                {
                    this.hasInventory = true;
                    this.inv = new EntityInv(inventory, this);
                }
                else
                {
                    this.hasInventory = false;
                }
            }
            else if (this.IsLootableObject())
            {
                this._ownerid = 76561198095992578UL;
                this._creatorid = 76561198095992578UL;
                var loot = Obj as LootableObject;
                this._name = loot.name;
                var inventory = loot._inventory;
                if (inventory != null)
                {
                    this.hasInventory = true;
                    this.inv = new EntityInv(inventory, this);
                }
                else
                {
                    this.hasInventory = false;
                }
            }
            else if (this.IsSupplyCrate())
            {
                this._ownerid = 76561198095992578UL;
                this._creatorid = 76561198095992578UL;
                this._name = "Supply Crate";
                var crate = Obj as SupplyCrate;
                var inventory = crate.lootableObject._inventory;
                if (inventory != null)
                {
                    this.hasInventory = true;
                    this.inv = new EntityInv(inventory, this);
                }
                else
                {
                    this.hasInventory = false;
                }
            }
            else if (IsResourceTarget())
            {
                var x = (ResourceTarget) Obj;
                this._ownerid = 76561198095992578UL;
                this._creatorid = 76561198095992578UL;
                this._name = x.name;
                this.hasInventory = false;
            }
            else
            {
                this.hasInventory = false;
            }

            Player ownerCache = Server.GetServer().GetCachePlayer(_ownerid);
            CachedPlayer cachedPlayerOwner;
            
            if (ownerCache != null)
            {
                this._ownername = ownerCache.Name;
            }
            else if (PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(_ownerid, out cachedPlayerOwner))
            {
                this._ownername = cachedPlayerOwner.Name;
            }
            else if (Server.GetServer().HasRustPP)
            {
                if (Server.GetServer().GetRustPPAPI().Cache.ContainsKey(_ownerid))
                {
                    this._ownername = Server.GetServer().GetRustPPAPI().Cache[_ownerid];
                }
            }
            else
            {
                this._ownername = "UnKnown";
            }

            Player creatorCache = Server.GetServer().GetCachePlayer(_creatorid);
            CachedPlayer cachedPlayerCreator;
            
            if (creatorCache != null)
            {
                this._creatorname = creatorCache.Name;
            }
            else if (PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(_creatorid, out cachedPlayerCreator))
            {
                this._ownername = cachedPlayerCreator.Name;
            }
            else if (Server.GetServer().HasRustPP)
            {
                if (Server.GetServer().GetRustPPAPI().Cache.ContainsKey(_creatorid))
                {
                    this._creatorname = Server.GetServer().GetRustPPAPI().Cache[_creatorid];
                }
            }
            else
            {
                this._creatorname = "UnKnown";
            }
        }

        /// <summary>
        /// Changes the Entity's owner to the specified player.
        /// </summary>
        /// <param name="p"></param>
        public void ChangeOwner(Fougerite.Player p)
        {
            if (this.IsDeployableObject() && !(bool)(this.Object as DeployableObject).GetComponent<SleepingAvatar>())
                this.GetObject<DeployableObject>().SetupCreator(p.PlayerClient.controllable);
            else if (this.IsStructureMaster())
                this.GetObject<StructureMaster>().SetupCreator(p.PlayerClient.controllable);
            else if (this.IsStructure())
            {
                foreach (var st in GetLinkedStructs())
                {
                    if (st.GetObject<StructureMaster>() != null)
                    {
                        this.GetObject<StructureMaster>().SetupCreator(p.PlayerClient.controllable);
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
            if (this.IsDeployableObject())
            {
                try
                {
                    this.GetObject<DeployableObject>().OnKilled();
                } catch
                {
                    TryNetCullDestroy();
                }
            } else if (this.IsStructure())
            {
                DestroyStructure(this.GetObject<StructureComponent>());                
            } else if (this.IsStructureMaster())
            {
                HashSet<StructureComponent> components = this.GetObject<StructureMaster>()._structureComponents;
                foreach (StructureComponent comp in components)
                    DestroyStructure(comp);

                try 
                {
                    this.GetObject<StructureMaster>().OnDestroy();
                } catch
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
                if (this.IsDeployableObject())
                {
                    if (this.GetObject<DeployableObject>() != null)
                        NetCull.Destroy(this.GetObject<DeployableObject>().gameObject);
                }
                else if (this.IsStructureMaster())
                {
                    if (this.GetObject<StructureMaster>() != null)
                        NetCull.Destroy(this.GetObject<StructureMaster>().networkViewID);
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
            } catch
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
            var obj = this.Object as StructureComponent;
            if (obj == null)
            {
                list.Add(this);
                return list;
            }
            foreach (StructureComponent component in obj._master._structureComponents)
            {
                if (component != this.Object as StructureComponent)
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
            return (T)this.Object;
        }

        public TakeDamage GetTakeDamage()
        {
            if (this.IsDeployableObject())
            {
                return this.GetObject<DeployableObject>().GetComponent<TakeDamage>();
            }
            if (this.IsStructure())
            {
                return this.GetObject<StructureComponent>().GetComponent<TakeDamage>();
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
                    var x = (ResourceTarget) _obj;
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
            ResourceTarget str = this.Object as ResourceTarget;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a BasicDoor
        /// </summary>
        /// <returns></returns>
        public bool IsBasicDoor()
        {
            BasicDoor str = this.Object as BasicDoor;
            return str != null;
        }
        
        /// <summary>
        /// Checks if the object is a LootableObject
        /// </summary>
        /// <returns></returns>
        public bool IsLootableObject()
        {
            LootableObject str = this.Object as LootableObject;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a DeployableObject
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsDeployableObject()
        {
            DeployableObject str = this.Object as DeployableObject;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a Chest or a Stash.
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsStorage()
        {
            if (this.IsDeployableObject())
                return this.GetObject<DeployableObject>().GetComponent<SaveableInventory>() != null;

            return false;
        }

        /// <summary>
        /// Checks if the object is a StructureComponent
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsStructure()
        {
            StructureComponent str = this.Object as StructureComponent;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a StructureMaster
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsStructureMaster()
        {
            StructureMaster str = this.Object as StructureMaster;
            return str != null;
        }

        /// <summary>
        /// Checks if the object is a SleepingAvatar
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsSleeper()
        {
            if (this.IsDeployableObject())
                return this.GetObject<DeployableObject>().GetComponent<SleepingAvatar>() != null;

            return false;
        }

        /// <summary>
        /// Checks if the object is a FireBarrel
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsFireBarrel()
        {
            if (this.IsDeployableObject())
                return this.GetObject<DeployableObject>().GetComponent<FireBarrel>() != null;

            return false;
        }

        /// <summary>
        /// Checks if the object is a SupplyCrate
        /// </summary>
        /// <returns>Returns true if it is.</returns>
        public bool IsSupplyCrate()
        {
            SupplyCrate str = this.Object as SupplyCrate;
            return str != null;
        }

        /// <summary>
        /// Enable / Disable the Default Rust Decay on this object?
        /// </summary>
        /// <param name="c"></param>
        public void SetDecayEnabled(bool c)
        {
            if (this.IsDeployableObject())
            {
                this.GetObject<DeployableObject>().SetDecayEnabled(c);
            }
        }

        /// <summary>
        /// Update the Entity's health.
        /// </summary>
        public void UpdateHealth()
        {
            if (this.IsDeployableObject())
            {
                this.GetObject<DeployableObject>().UpdateClientHealth();
            }
            else if (this.IsStructure())
            {
                this.GetObject<StructureComponent>().UpdateClientHealth();
            }
        }

        /// <summary>
        /// Tries to find the Creator of the object in the cache or through the online players. Returns null otherwise.
        /// </summary>
        public Fougerite.Player Creator
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
            get
            {
                return this._ownerid.ToString();
            }
        }

        /// <summary>
        /// Returns the OwnerID as a ulong
        /// </summary>
        public ulong UOwnerID
        {
            get
            {
                return this._ownerid;
            }
        }

        /// <summary>
        /// Returns the CreatorID as a string
        /// </summary>
        public string CreatorID
        {
            get
            {
                return this._creatorid.ToString();
            }
        }
        
        /// <summary>
        /// Returns the OwnerID as a ulong
        /// </summary>
        public ulong UCreatorID
        {
            get
            {
                return this._creatorid;
            }
        }

        /// <summary>
        /// Returns the current health of the entity. Setting It will also update the health.
        /// </summary>
        public float Health
        {
            get
            {
                if (this.IsDeployableObject())
                {
                    return this.GetObject<DeployableObject>().GetComponent<TakeDamage>().health;
                }
                if (this.IsStructure())
                {
                    return this.GetObject<StructureComponent>().GetComponent<TakeDamage>().health;
                }
                if (this.IsStructureMaster())
                {
                    float sum = this.GetObject<StructureMaster>()._structureComponents.Sum<StructureComponent>(s => s.GetComponent<TakeDamage>().health);
                    return sum;
                }
                return 0f;
            }
            set
            {
                if (this.IsDeployableObject())
                {
                    this.GetObject<DeployableObject>().GetComponent<TakeDamage>().health = value;
                }
                else if (this.IsStructure())
                {
                    this.GetObject<StructureComponent>().GetComponent<TakeDamage>().health = value;
                }
                this.UpdateHealth();
            }
        }

        /// <summary>
        /// Gets the maxhealth of the Entity.
        /// </summary>
        public float MaxHealth
        {
            get
            {
                if (this.IsDeployableObject())
                {
                    return this.GetObject<DeployableObject>().GetComponent<TakeDamage>().maxHealth;
                }
                if (this.IsStructure())
                {
                    return this.GetObject<StructureComponent>().GetComponent<TakeDamage>().maxHealth;
                }
                if (this.IsStructureMaster())
                {
                    float sum = this.GetObject<StructureMaster>()._structureComponents.Sum<StructureComponent>(s => s.GetComponent<TakeDamage>().maxHealth);
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
                if (this.IsDeployableObject())
                {
                    return this.GetObject<DeployableObject>().GetInstanceID();
                }
                if (this.IsStructure())
                {
                    return this.GetObject<StructureComponent>().GetInstanceID();
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets the inventory of the Entity if possible.
        /// </summary>
        public EntityInv Inventory
        {
            get
            {
                if (this.hasInventory)
                    return this.inv;
                return null;
            }
        }

        /// <summary>
        /// Gets the name of the Entity
        /// </summary>
        public string Name
        {
            get
            {
                return this._name;
            }
        }

        /// <summary>
        /// Returns the Original Object type of this Entity. (Like DeployaleObject, StructureComponent, SupplyCrate, etc.)
        /// </summary>
        public object Object
        {
            get
            {
                return this._obj;
            }
        }

        /// <summary>
        /// Returns the Owner of the Entity IF ONLINE.
        /// </summary>
        public Fougerite.Player Owner
        {
            get
            {
                return Fougerite.Player.FindByGameID(this.OwnerID);
            }
        }

        /// <summary>
        /// Returns the location of the Entity.
        /// </summary>
        public Vector3 Location
        {
            get
            {
                if (this.IsDeployableObject())
                    return this.GetObject<DeployableObject>().transform.position;
                if (this.IsStructure())
                    return this.GetObject<StructureComponent>().transform.position;
                if (this.IsStructureMaster())
                    return this.GetObject<StructureMaster>().containedBounds.center;
                if (this.IsBasicDoor())
                    return this.GetObject<BasicDoor>().transform.position;
                if (this.IsLootableObject())
                    return this.GetObject<LootableObject>().transform.position;
                if (this.IsResourceTarget())
                    return this.GetObject<ResourceTarget>().transform.position;

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
                if (this.IsDeployableObject())
                    return this.GetObject<DeployableObject>().transform.rotation;

                if (this.IsStructure())
                    return this.GetObject<StructureComponent>().transform.rotation;
                if (this.IsBasicDoor())
                    return this.GetObject<BasicDoor>().transform.rotation;
                if (this.IsLootableObject())
                    return this.GetObject<LootableObject>().transform.rotation;
                if (this.IsResourceTarget())
                    return this.GetObject<ResourceTarget>().transform.rotation;

                return new Quaternion(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Returns the X coordinate of the Entity
        /// </summary>
        public float X
        {
            get
            {
                return this.Location.x;
            }
        }

        /// <summary>
        /// Returns the Y coordinate of the Entity
        /// </summary>
        public float Y
        {
            get
            {
                return this.Location.y;
            }
        }

        /// <summary>
        /// Returns the Z coordinate of the Entity
        /// </summary>
        public float Z
        {
            get
            {
                return this.Location.z;
            }
        }
    }
}
