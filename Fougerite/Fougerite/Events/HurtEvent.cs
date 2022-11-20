namespace Fougerite.Events
{
    using Fougerite;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class is created when a Player or an AI or an Entity is Hurt.
    /// </summary>
    public class HurtEvent
    {
        private object _attacker;
        private DamageEvent _de;
        private bool _decay;
        private Entity _ent;
        private object _victim;
        private string _weapon;
        private WeaponImpact _wi;
        private readonly bool _playervictim;
        private readonly bool _entityvictim = false;
        private readonly bool _npcvictim = false;
        private readonly bool _playerattacker;
        private readonly bool _entityattacker = false;
        private readonly bool _metabolismattacker = false;
        private readonly bool _npcattacker = false;
        private readonly bool _sleeper;
        private readonly LifeStatus _status;

        public HurtEvent(ref DamageEvent d)
        {
            //Logger.LogDebug(string.Format("[DamageEvent] {0}", d.ToString()));
            try
            {
                _sleeper = false;
                DamageEvent = d;
                WeaponData = null;
                _status = d.status;
                string weaponName = "Unknown";
                if (d.victim.idMain is DeployableObject deployableObject)
                {
                    if (d.victim.id.ToString().ToLower().Contains("sleeping"))
                    {
                        _sleeper = true;
                        Victim = new Sleeper(deployableObject);
                    }
                    else
                    {
                        Victim = new Entity(d.victim.idMain.GetComponent<DeployableObject>());
                        _ent = new Entity(d.victim.idMain.GetComponent<DeployableObject>());
                        _entityvictim = true;
                    }
                    _playervictim = false;
                }
                else if (d.victim.idMain is StructureComponent)
                {
                    Victim = new Entity(d.victim.idMain.GetComponent<StructureComponent>());
                    _ent = new Entity(d.victim.idMain.GetComponent<StructureComponent>());
                    _playervictim = false;
                    _entityvictim = true;
                    _entityvictim = true;
                }
                else if (d.victim.id is SpikeWall)
                {
                    _playerattacker = false;
                    Victim = new Entity(d.victim.idMain.GetComponent<DeployableObject>());
                    _ent = new Entity(d.victim.idMain.GetComponent<DeployableObject>());
                    _entityvictim = true;
                }
                else if (d.victim.client != null)
                {
                    Player temp = Server.GetServer().GetCachePlayer(d.victim.client.userID);
                    Victim = temp ?? Player.FindByPlayerClient(d.victim.client);
                    _playervictim = true;
                }
                else if (d.victim.character != null)
                {
                    Victim = new NPC(d.victim.character);
                    _npcvictim = true;
                    _playervictim = false;
                }
                
                // These should be unassigned on decay
                if (d.attacker.networkViewID == uLink.NetworkViewID.unassigned &&
                    d.victim.networkViewID == uLink.NetworkViewID.unassigned)
                {
                    switch (d.attacker.idOwnerMain)
                    {
                        // Check for structures and deployableobjects
                        case StructureComponent structureComponent when d.victim.idMain is StructureComponent structureComponent2:
                        {
                            // The attacker on decay is the victim it self
                            IsDecay = structureComponent.GetInstanceID() == structureComponent2.GetInstanceID();
                            break;
                        }
                        case DeployableObject deployableObject3 when d.victim.idMain is DeployableObject deployableObject2:
                        {
                            // The attacker on decay is the victim it self
                            IsDecay = deployableObject3.GetInstanceID() == deployableObject2.GetInstanceID();
                            break;
                        }
                    }
                }

                if (!(bool) d.attacker.id)
                {
                    if (d.victim.client != null)
                    {
                        weaponName = DamageType;
                        _playerattacker = false;
                        Attacker = null;
                    }
                }
                else if (d.attacker.id is SpikeWall)
                {
                    _playerattacker = false;
                    Attacker = new Entity(d.attacker.idMain.GetComponent<DeployableObject>());
                    _entityattacker = true;
                    weaponName = d.attacker.id.ToString().Contains("Large") ? "Large Spike Wall" : "Spike Wall";
                }
                else if (d.attacker.id is SupplyCrate)
                {
                    _playerattacker = false;
                    Attacker = new Entity(d.attacker.idMain.GetComponent<SupplyCrate>());
                    _entityattacker = true;
                    weaponName = "Supply Crate";
                }
                else if (d.attacker.id is Metabolism && d.victim.id is Metabolism)
                {
                    Player temp = Server.GetServer().GetCachePlayer(d.attacker.client.userID);
                    Attacker = temp ?? Player.FindByPlayerClient(d.attacker.client);
                    _playerattacker = false;
                    _metabolismattacker = true;
                    Victim = Attacker;
                    ICollection<string> list = new List<string>();
                    
                    if (Victim is Player vic)
                    {
                        if (vic.IsStarving)
                        {
                            list.Add("Starvation");
                        }

                        if (vic.IsRadPoisoned)
                        {
                            list.Add("Radiation");
                        }

                        if (vic.IsPoisoned)
                        {
                            list.Add("Poison");
                        }

                        if (vic.IsBleeding)
                        {
                            list.Add("Bleeding");
                        }
                    }

                    if (list.Contains("Bleeding"))
                    {
                        if (DamageType != "Unknown" && !list.Contains(DamageType))
                            list.Add(DamageType);
                    }
                    weaponName = list.Count > 0 ? $"Self ({string.Join(",", list.ToArray())})" : DamageType;
                }
                else if (d.attacker.client != null)
                {
                    Player temp = Server.GetServer().GetCachePlayer(d.attacker.client.userID);
                    Attacker = temp ?? Player.FindByPlayerClient(d.attacker.client);

                    _playerattacker = true;
                    if (d.extraData != null)
                    {
                        WeaponImpact extraData = d.extraData as WeaponImpact;
                        WeaponData = extraData;
                        if (extraData != null && extraData.dataBlock != null)
                        {
                            weaponName = extraData.dataBlock.name;
                        }
                    }
                    else
                    {
                        if (d.attacker.id is TimedExplosive)
                        {
                            weaponName = "Explosive Charge";
                        }
                        else if (d.attacker.id is TimedGrenade)
                        {
                            weaponName = "F1 Grenade";
                        }
                        else
                        {
                            weaponName = "Hunting Bow";
                        }
                        if (d.victim.client != null)
                        {
                            if (!d.attacker.IsDifferentPlayer(d.victim.client) && !(Victim is Entity))
                            {
                                weaponName = "Fall Damage";
                            }
                            else if (!d.attacker.IsDifferentPlayer(d.victim.client) && (Victim is Entity))
                            {
                                weaponName = "Hunting Bow";
                            }
                        }
                    }
                }
                else if (d.attacker.character != null)
                {
                    Attacker = new NPC(d.attacker.character);
                    _playerattacker = false;
                    _npcattacker = true;
                    var temp = (NPC) Attacker;
                    weaponName = $"{temp.Name} Claw";
                }
                WeaponName = weaponName;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[HurtEvent] Error: {ex}");
            }
        }

        public HurtEvent(ref DamageEvent d, Entity en)
            : this(ref d)
        {
            Entity = en;
        }

        /// <summary>
        /// Gets the Attacker of the event. Can be AI or Player or even decay.
        /// </summary>
        public object Attacker
        {
            get
            {
                return _attacker;
            }
            set
            {
                _attacker = value;
            }
        }

        /// <summary>
        /// Returns the lifestatus of the object.
        /// </summary>
        public LifeStatus LifeStatus
        {
            get { return _status; }
        }

        [Obsolete("Sleeper is deprecated, please use VictimIsSleeper instead.")]
        public bool Sleeper
        {
            get
            {
                return _sleeper;
            }
        }

        /// <summary>
        /// Checks if the Victim object is a Sleeper.
        /// </summary>
        public bool VictimIsSleeper
        {
            get
            {
                return _sleeper;
            }
        }

        /// <summary>
        /// Gets or Sets the Damage of the event.
        /// </summary>
        public float DamageAmount
        {
            get
            {
                return _de.amount;
            }
            set
            {
                _de.amount = value;
            }
        }

        /// <summary>
        /// Gets the original DamageEvent class.
        /// </summary>
        public DamageEvent DamageEvent
        {
            get
            {
                return _de;
            }
            set
            {
                _de = value;
            }
        }

        /// <summary>
        /// This gettertries to find the DamageType.
        /// </summary>
        public string DamageType
        {
            get
            {
                string str = "Unknown";
                switch (((int)DamageEvent.damageTypes))
                {
                    case 0:
                        return "Bleeding";

                    case 1:
                        return "Generic";

                    case 2:
                        return "Bullet";

                    case 3:
                    case 5:
                    case 6:
                    case 7:
                        return str;

                    case 4:
                        return "Melee";

                    case 8:
                        return "Explosion";

                    case 0x10:
                        return "Radiation";

                    case 0x20:
                        return "Cold";
                }
                return str;
            }
        }

        /// <summary>
        /// This grabs the Victim as an Entity. Null if the Victim is not an Entity. Use HurtEvent.Victim though.
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

        /// <summary>
        /// Checks if the attacker is decay.
        /// </summary>
        public bool IsDecay
        {
            get
            {
                return _decay;
            }
            set
            {
                _decay = value;
            }
        }

        /// <summary>
        /// Gets the Victim object. Can be AI, Player, Entity.
        /// </summary>
        public object Victim
        {
            get
            {
                return _victim;
            }
            set
            {
                _victim = value;
            }
        }

        /// <summary>
        /// Gets the original WeaponImpact class.
        /// </summary>
        public WeaponImpact WeaponData
        {
            get
            {
                return _wi;
            }
            set
            {
                _wi = value;
            }
        }

        /// <summary>
        /// Gets the weapon's name that caused the damage.
        /// </summary>
        public string WeaponName
        {
            get
            {
                return _weapon;
            }
            set
            {
                _weapon = value;
            }
        }

        /// <summary>
        /// Checks if the Victim object is a player.
        /// </summary>
        public bool VictimIsPlayer
        {
            get
            {
                return _playervictim;
            }       
        }

        /// <summary>
        /// Checks if the Victim object is an Entity.
        /// </summary>
        public bool VictimIsEntity
        {
            get
            {
                return _entityvictim;
            }
        }

        /// <summary>
        /// Checks if the Victim object is an AI.
        /// </summary>
        public bool VictimIsNPC
        {
            get
            {
                return _npcvictim;
            }
        }

        /// <summary>
        /// Checks if the Attacker object is a player.
        /// </summary>
        public bool AttackerIsPlayer
        {
            get
            {
                return _playerattacker;
            }
        }

        /// <summary>
        /// Checks if the Attacker object is an Entity.
        /// </summary>
        public bool AttackerIsEntity
        {
            get
            {
                return _entityattacker;
            }
        }

        /// <summary>
        /// Checks if the Attacker object is metabolism (Hunger for example).
        /// </summary>
        public bool AttackerIsMetabolism
        {
            get
            {
                return _metabolismattacker;
            }
        }

        /// <summary>
        /// Checks if the Attacker object is an AI.
        /// </summary>
        public bool AttackerIsNPC
        {
            get
            {
                return _npcattacker;
            }
        }
    }
}
