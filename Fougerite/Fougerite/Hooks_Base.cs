using System;
using System.Collections;
using System.Collections.Generic;
using Fougerite.Concurrent;
using Fougerite.Events;
using UnityEngine;

namespace Fougerite
{
    public partial class Hooks
    {
        public static ConcurrentDictionary<int, Entity> DecayList = new ConcurrentDictionary<int, Entity>();
        public static Hashtable talkerTimers = new Hashtable();
        public static bool ServerInitialized = false;
        public static readonly List<ulong> uLinkDCCache = new List<ulong>();
        internal static Dictionary<string, Flood> FloodChecks = new Dictionary<string, Flood>();
        internal static Dictionary<string, DateTime> FloodCooldown = new Dictionary<string, DateTime>();

        /// <summary>
        /// This delegate runs when all plugins loaded. (First time)
        /// </summary>
        public static event AllPluginsLoadedDelegate OnAllPluginsLoaded;

        /// <summary>
        /// This delegate runs when a blueprint is being used.
        /// </summary>
        public static event BlueprintUseHandlerDelegate OnBlueprintUse;

        /// <summary>
        /// This delegate runs when a chat message is received.
        /// </summary>
        public static event ChatHandlerDelegate OnChat;

        /// <summary>
        /// This delegate runs when a chat message is received.
        /// </summary>
        public static event ChatRawHandlerDelegate OnChatRaw;

        /// <summary>
        /// This delegate runs when a command is executed.
        /// </summary>
        public static event CommandHandlerDelegate OnCommand;

        /// <summary>
        /// This delegate runs when a command is being executed
        /// </summary>
        public static event CommandRawHandlerDelegate OnCommandRaw;

        /// <summary>
        /// This delegate runs when a console message is received.
        /// </summary>
        [Obsolete("Use OnConsoleReceivedWithCancel", false)]
        public static event ConsoleHandlerDelegate OnConsoleReceived;

        /// <summary>
        /// This delegate runs when a console message is received.
        /// </summary>
        public static event ConsoleHandlerWithCancelDelegate OnConsoleReceivedWithCancel;

        /// <summary>
        /// This delegate runs when a door is opened/closed.
        /// </summary>
        public static event DoorOpenHandlerDelegate OnDoorUse;

        /// <summary>
        /// This delegate runs when an entity is attacked by the default rust decay.
        /// </summary>
        public static event EntityDecayDelegate OnEntityDecay;

        [Obsolete("Use OnEntityDeployedWithPlacer", false)]
        public static event EntityDeployedDelegate OnEntityDeployed;

        /// <summary>
        /// This delegate runs when an Entity is placed on the ground.
        /// </summary>
        public static event EntityDeployedWithPlacerDelegate OnEntityDeployedWithPlacer;

        /// <summary>
        /// This delegate runs when an entity is damaged.
        /// </summary>
        public static event EntityHurtDelegate OnEntityHurt;

        /// <summary>
        /// This delegate runs when an entity is destroyed.
        /// </summary>
        public static event EntityDestroyedDelegate OnEntityDestroyed;

        /// <summary>
        /// This delegate runs when the item datablocks are loaded.
        /// </summary>
        public static event ItemsDatablocksLoaded OnItemsLoaded;

        /// <summary>
        /// This delegate runs when an AI is hurt.
        /// </summary>
        public static event HurtHandlerDelegate OnNPCHurt;

        /// <summary>
        /// This delegate runs when an AI is killed.
        /// </summary>
        public static event KillHandlerDelegate OnNPCKilled;

        /// <summary>
        /// This delegate runs when a player is connecting to the server.
        /// </summary>
        public static event ConnectionHandlerDelegate OnPlayerConnected;

        /// <summary>
        /// This delegate runs when a player disconnected from the server.
        /// </summary>
        public static event DisconnectionHandlerDelegate OnPlayerDisconnected;

        /// <summary>
        /// This delegate runs when a player is gathering from an animal or from a resource.
        /// </summary>
        public static event PlayerGatheringHandlerDelegate OnPlayerGathering;

        /// <summary>
        /// This delegate runs when a player is hurt.
        /// </summary>
        public static event HurtHandlerDelegate OnPlayerHurt;

        /// <summary>
        /// This delegate runs when a player is killed
        /// </summary>
        public static event KillHandlerDelegate OnPlayerKilled;

        /// <summary>
        /// This delegate runs when a player just spawned.
        /// </summary>
        public static event PlayerSpawnHandlerDelegate OnPlayerSpawned;

        /// <summary>
        /// This delegate runs when a player is about to spawn.
        /// </summary>
        public static event PlayerSpawnHandlerDelegate OnPlayerSpawning;

        /// <summary>
        /// This delegate runs when a plugin is loaded.
        /// </summary>
        public static event PluginInitHandlerDelegate OnPluginInit;

        /// <summary>
        /// This delegate runs when a player is teleported using Fougerite API.
        /// </summary>
        public static event TeleportDelegate OnPlayerTeleport;

        /// <summary>
        /// This delegate runs when the server started loading.
        /// </summary>
        public static event ServerInitDelegate OnServerInit;

        /// <summary>
        /// This delegate runs when the server is stopping.
        /// </summary>
        public static event ServerShutdownDelegate OnServerShutdown;

        /// <summary>
        /// This delegate runs when a player is talking through the microphone.
        /// </summary>
        public static event ShowTalkerDelegate OnShowTalker;

        /// <summary>
        /// This delegate runs when the LootTables are loaded.
        /// </summary>
        public static event LootTablesLoaded OnTablesLoaded;

        /// <summary>
        /// This delegate runs when all C# plugins loaded.
        /// </summary>
        public static event ModulesLoadedDelegate OnModulesLoaded;

        [Obsolete("This method is no longer called since the rust api doesn't call It.", false)]
        public static event RecieveNetworkDelegate OnRecieveNetwork;

        /// <summary>
        /// This delegate runs when a player starts crafting.
        /// </summary>
        public static event CraftingDelegate OnCrafting;

        /// <summary>
        /// This delegate runs when a resource object spawned.
        /// </summary>
        public static event ResourceSpawnDelegate OnResourceSpawned;

        /// <summary>
        /// This delegate runs when an item is removed from a specific inventory.
        /// </summary>
        public static event ItemRemovedDelegate OnItemRemoved;

        /// <summary>
        /// This delegate runs when an item is added to a specific inventory.
        /// </summary>
        public static event ItemAddedDelegate OnItemAdded;

        /// <summary>
        /// This delegate runs when an airdrop is called.
        /// </summary>
        public static event AirdropDelegate OnAirdropCalled;

        /// <summary>
        /// This delegate runs when a supplydropplane is created.
        /// </summary>
        public static event SupplyDropPlaneCreatedDelegate OnSupplyDropPlaneCreated;

        /// <summary>
        /// This delegate runs when the crate is created from the airdrop.
        /// </summary>
        public static event AirdropCrateDroppedDelegate OnAirdropCrateDropped;
        
        /// <summary>
        /// This delegate runs when a player is kicked by steam.
        /// </summary>
        public static event SteamDenyDelegate OnSteamDeny;

        /// <summary>
        /// This delegate runs when a player is being approved.
        /// </summary>
        public static event PlayerApprovalDelegate OnPlayerApproval;

        /// <summary>
        /// This delegate runs when a player is moving. (Even if standing at one place)
        /// </summary>
        public static event PlayerMoveDelegate OnPlayerMove;

        /// <summary>
        /// This delegate runs when a player researched an item.
        /// </summary>
        public static event ResearchDelegate OnResearch;

        /// <summary>
        /// This delegate runs when the server is being saved.
        /// </summary>
        public static event ServerSavedDelegate OnServerSaved;

        /// <summary>
        /// This delegate runs when an item is picked up by a player.
        /// </summary>
        public static event ItemPickupDelegate OnItemPickup;

        /// <summary>
        /// This delegate runs when a player received fall damage.
        /// </summary>
        public static event FallDamageDelegate OnFallDamage;

        /// <summary>
        /// This delegate runs when a player is looting something.
        /// </summary>
        public static event LootEnterDelegate OnLootUse;

        /// <summary>
        /// This delegate runs when a player is shooting a weapon.
        /// </summary>
        public static event ShootEventDelegate OnShoot;

        /// <summary>
        /// This delegate runs when a player is shooting a shotgun.
        /// </summary>
        public static event ShotgunShootEventDelegate OnShotgunShoot;

        /// <summary>
        /// This delegate runs when a player is shooting a bow.
        /// </summary>
        public static event BowShootEventDelegate OnBowShoot;

        /// <summary>
        /// This delegate runs when a player throws a grenade.
        /// </summary>
        public static event GrenadeThrowEventDelegate OnGrenadeThrow;

        /// <summary>
        /// This delegate runs when a player got banned.
        /// </summary>
        public static event BanEventDelegate OnPlayerBan;

        /// <summary>
        /// This delegate runs when a player is using the repair bench.
        /// </summary>
        public static event RepairBenchEventDelegate OnRepairBench;

        /// <summary>
        /// This delegate runs when an item is being moved in an inventory to a different slot / inventory.
        /// </summary>
        public static event ItemMoveEventDelegate OnItemMove;

        /// <summary>
        /// This delegate runs when the ResourceSpawner loaded.
        /// </summary>
        public static event GenericSpawnerLoadDelegate OnGenericSpawnerLoad;

        /// <summary>
        /// This delegate runs when the server finished loading.
        /// </summary>
        public static event ServerLoadedDelegate OnServerLoaded;

        /// <summary>
        /// This delegate runs when a supply signal explodes at a position.
        /// </summary>
        public static event SupplySignalDelegate OnSupplySignalExpode;

        /// <summary>
        /// This delegate runs when a belt slot is used.
        /// </summary>
        public static event BeltUseDelegate OnBeltUse;

        /// <summary>
        /// This delegate runs when the logger functions are triggered.
        /// </summary>
        public static event LoggerDelegate OnLogger;

        /// <summary>
        /// This delegate runs when an NPC is spawned.
        /// </summary>
        public static event NPCSpawnedEventDelegate OnNPCSpawned;

        /// <summary>
        /// This delegate runs when a C4 is placed.
        /// </summary>
        public static event TimedExplosiveEventDelegate OnTimedExplosiveSpawned;

        /// <summary>
        /// This delegate runs when a Sleeper is spawned.
        /// </summary>
        public static event SleeperSpawnEventDelegate OnSleeperSpawned;

        /// <summary>
        /// This delegate runs when a command is restricted on unrestricted.
        /// </summary>
        public static event CommandRestrictionEventDelegate OnCommandRestriction;

        /// <summary>
        /// This value returns if the server is shutting down.
        /// </summary>
        public static bool IsShuttingDown { get; set; }

        /// <summary>
        /// Does what It says.
        /// UnHooks all plugins from the events.
        /// </summary>
        public static void ResetHooks()
        {
            OnPluginInit = delegate { };
            OnPlayerTeleport = delegate { };
            OnChat = delegate { };
            OnChatRaw = delegate { };
            OnCommand = delegate { };
            OnCommandRaw = delegate { };
            OnPlayerConnected = delegate { };
            OnPlayerDisconnected = delegate { };
            OnNPCKilled = delegate { };
            OnNPCHurt = delegate { };
            OnNPCSpawned = delegate {  };
            OnPlayerKilled = delegate { };
            OnPlayerHurt = delegate { };
            OnPlayerSpawned = delegate { };
            OnPlayerSpawning = delegate { };
            OnPlayerGathering = delegate { };
            OnEntityHurt = delegate { };
            OnEntityDestroyed = delegate { };
            OnEntityDecay = delegate { };
            OnEntityDeployed = delegate { };
            OnEntityDeployedWithPlacer = delegate { };
            OnConsoleReceived = delegate { };
            OnConsoleReceivedWithCancel = delegate { };
            OnBlueprintUse = delegate { };
            OnDoorUse = delegate { };
            OnTablesLoaded = delegate { };
            OnItemsLoaded = delegate { };
            OnServerInit = delegate { };
            OnServerShutdown = delegate { };
            OnModulesLoaded = delegate { };
            OnRecieveNetwork = delegate { };
            OnShowTalker = delegate { };
            OnCrafting = delegate { };
            OnResourceSpawned = delegate { };
            OnItemRemoved = delegate { };
            OnItemAdded = delegate { };
            OnAirdropCalled = delegate { };
            OnSteamDeny = delegate { };
            OnPlayerApproval = delegate { };
            OnPlayerMove = delegate { };
            OnResearch = delegate { };
            OnServerSaved = delegate { };
            OnItemPickup = delegate { };
            OnFallDamage = delegate { };
            OnLootUse = delegate { };
            OnShoot = delegate { };
            OnBowShoot = delegate { };
            OnShotgunShoot = delegate { };
            OnGrenadeThrow = delegate { };
            OnPlayerBan = delegate { };
            OnRepairBench = delegate { };
            OnItemMove = delegate { };
            OnGenericSpawnerLoad = delegate { };
            OnServerLoaded = delegate { };
            OnSupplySignalExpode = delegate { };
            OnBeltUse = delegate { };
            OnLogger = delegate { };
            OnAirdropCrateDropped = delegate { };
            OnSupplyDropPlaneCreated = delegate {  };
            OnTimedExplosiveSpawned = delegate {  };
            OnSleeperSpawned = delegate {  };
            OnCommandRestriction = delegate {  };
        }
        
        public delegate void BlueprintUseHandlerDelegate(Player player, BPUseEvent ae);

        public delegate void ChatHandlerDelegate(Player player, ref ChatString text);

        public delegate void ChatRawHandlerDelegate(ref ConsoleSystem.Arg arg);

        public delegate void CommandHandlerDelegate(Player player, string cmd, string[] args);

        public delegate void CommandRawHandlerDelegate(ref ConsoleSystem.Arg arg);

        public delegate void ConnectionHandlerDelegate(Player player);

        public delegate void ConsoleHandlerDelegate(ref ConsoleSystem.Arg arg, bool external);

        public delegate void ConsoleHandlerWithCancelDelegate(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent ce);

        public delegate void DisconnectionHandlerDelegate(Player player);

        public delegate void DoorOpenHandlerDelegate(Player player, DoorEvent de);

        public delegate void EntityDecayDelegate(DecayEvent de);

        public delegate void EntityDeployedDelegate(Player player, Entity e);

        public delegate void EntityDeployedWithPlacerDelegate(Player player, Entity e, Player actualplacer);

        public delegate void EntityHurtDelegate(HurtEvent he);

        public delegate void EntityDestroyedDelegate(DestroyEvent de);

        public delegate void HurtHandlerDelegate(HurtEvent he);

        public delegate void ItemsDatablocksLoaded(ItemsBlocks items);

        public delegate void KillHandlerDelegate(DeathEvent de);

        public delegate void LootTablesLoaded(Dictionary<string, LootSpawnList> lists);

        public delegate void PlayerGatheringHandlerDelegate(Player player, GatherEvent ge);

        public delegate void PlayerSpawnHandlerDelegate(Player player, SpawnEvent se);

        public delegate void ShowTalkerDelegate(uLink.NetworkPlayer player, Player p);

        public delegate void PluginInitHandlerDelegate();

        public delegate void TeleportDelegate(Player player, Vector3 from, Vector3 dest);

        public delegate void ServerInitDelegate();

        public delegate void ServerShutdownDelegate();

        public delegate void ModulesLoadedDelegate();

        public delegate void RecieveNetworkDelegate(Player player, Metabolism m, float cal, float water,
            float rad, float anti, float temp, float poison);

        public delegate void CraftingDelegate(CraftingEvent e);

        public delegate void ResourceSpawnDelegate(ResourceTarget t);

        public delegate void ItemRemovedDelegate(InventoryModEvent e);

        public delegate void ItemAddedDelegate(InventoryModEvent e);

        public delegate void AirdropDelegate(Vector3 v);

        public delegate void SteamDenyDelegate(SteamDenyEvent sde);

        public delegate void PlayerApprovalDelegate(PlayerApprovalEvent e);

        public delegate void PlayerMoveDelegate(HumanController hc, Vector3 origin, int encoded, ushort stateFlags,
            uLink.NetworkMessageInfo info, Util.PlayerActions action);

        public delegate void ResearchDelegate(ResearchEvent re);

        public delegate void ServerSavedDelegate(int Amount, double Seconds);

        public delegate void ItemPickupDelegate(ItemPickupEvent itemPickupEvent);

        public delegate void FallDamageDelegate(FallDamageEvent fallDamageEvent);

        public delegate void LootEnterDelegate(LootStartEvent lootStartEvent);

        public delegate void ShootEventDelegate(ShootEvent shootEvent);

        public delegate void ShotgunShootEventDelegate(ShotgunShootEvent shootEvent);

        public delegate void BowShootEventDelegate(BowShootEvent bowshootEvent);

        public delegate void GrenadeThrowEventDelegate(GrenadeThrowEvent grenadeThrowEvent);

        public delegate void BanEventDelegate(BanEvent banEvent);

        public delegate void RepairBenchEventDelegate(Fougerite.Events.RepairEvent repairEvent);

        public delegate void ItemMoveEventDelegate(ItemMoveEvent itemMoveEvent);

        public delegate void GenericSpawnerLoadDelegate(GenericSpawner genericSpawner);

        public delegate void ServerLoadedDelegate();

        public delegate void SupplySignalDelegate(SupplySignalExplosionEvent supplySignalExplosionEvent);

        public delegate void AllPluginsLoadedDelegate();

        public delegate void BeltUseDelegate(BeltUseEvent beltUseEvent);

        public delegate void LoggerDelegate(LoggerEvent loggerEvent);

        public delegate void AirdropCrateDroppedDelegate(SupplyDropPlane plane, Entity supplyCrate);

        public delegate void SupplyDropPlaneCreatedDelegate(SupplyDropPlane plane);

        public delegate void NPCSpawnedEventDelegate(NPC npc);

        public delegate void TimedExplosiveEventDelegate(TimedExplosiveEvent timedExplosiveEvent);

        public delegate void SleeperSpawnEventDelegate(Sleeper sleeper);

        public delegate void CommandRestrictionEventDelegate(CommandRestrictionEvent commandRestrictionEvent);
    }
}