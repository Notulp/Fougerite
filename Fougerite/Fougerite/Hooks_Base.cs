using System;
using System.Collections;
using System.Collections.Generic;
using Fougerite.Events;
using UnityEngine;

namespace Fougerite
{
    public partial class Hooks
    {
        public static List<object> decayList = new List<object>();
        public static Hashtable talkerTimers = new Hashtable();
        public static bool ServerInitialized = false;

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

        //public static event AirdropCrateDroppedDelegate OnAirdropCrateDropped;
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
        /// This value returns if the server is shutting down.
        /// </summary>
        public static bool IsShuttingDown { get; set; }

        public static readonly List<ulong> uLinkDCCache = new List<ulong>();

        internal static Dictionary<string, Flood> FloodChecks = new Dictionary<string, Flood>();
        internal static Dictionary<string, DateTime> FloodCooldown = new Dictionary<string, DateTime>();
        
        public static void ResetHooks()
        {
            OnPluginInit = delegate { };
            OnPlayerTeleport = delegate(Player param0, Vector3 param1, Vector3 param2) { };
            OnChat = delegate(Player param0, ref ChatString param1) { };
            OnChatRaw = delegate(ref ConsoleSystem.Arg param0) { };
            OnCommand = delegate(Player param0, string param1, string[] param2) { };
            OnCommandRaw = delegate(ref ConsoleSystem.Arg param0) { };
            OnPlayerConnected = delegate(Player param0) { };
            OnPlayerDisconnected = delegate(Player param0) { };
            OnNPCKilled = delegate(DeathEvent param0) { };
            OnNPCHurt = delegate(HurtEvent param0) { };
            OnPlayerKilled = delegate(DeathEvent param0) { };
            OnPlayerHurt = delegate(HurtEvent param0) { };
            OnPlayerSpawned = delegate(Player param0, SpawnEvent param1) { };
            OnPlayerSpawning = delegate(Player param0, SpawnEvent param1) { };
            OnPlayerGathering = delegate(Player param0, GatherEvent param1) { };
            OnEntityHurt = delegate(HurtEvent param0) { };
            OnEntityDestroyed = delegate(DestroyEvent param0) { };
            OnEntityDecay = delegate(DecayEvent param0) { };
            OnEntityDeployed = delegate(Player param0, Entity param1) { };
            OnEntityDeployedWithPlacer = delegate(Player param0, Entity param1, Player param2) { };
            OnConsoleReceived = delegate(ref ConsoleSystem.Arg param0, bool param1) { };
            OnConsoleReceivedWithCancel = delegate(ref ConsoleSystem.Arg param0, bool param1, ConsoleEvent ce) { };
            OnBlueprintUse = delegate(Player param0, BPUseEvent param1) { };
            OnDoorUse = delegate(Player param0, DoorEvent param1) { };
            OnTablesLoaded = delegate(Dictionary<string, LootSpawnList> param0) { };
            OnItemsLoaded = delegate(ItemsBlocks param0) { };
            OnServerInit = delegate { };
            OnServerShutdown = delegate { };
            OnModulesLoaded = delegate { };
            OnRecieveNetwork = delegate(Player param0, Metabolism param1, float param2, float param3,
                float param4, float param5, float param6, float param7)
            {
            };
            OnShowTalker = delegate(uLink.NetworkPlayer param0, Player param1) { };
            OnCrafting = delegate(CraftingEvent param0) { };
            OnResourceSpawned = delegate(ResourceTarget param0) { };
            OnItemRemoved = delegate(InventoryModEvent param0) { };
            OnItemAdded = delegate(InventoryModEvent param0) { };
            OnAirdropCalled = delegate(Vector3 param0) { };
            OnSteamDeny = delegate(SteamDenyEvent param0) { };
            OnPlayerApproval = delegate(PlayerApprovalEvent param0) { };
            OnPlayerMove = delegate(HumanController param0, Vector3 param1, int param2, ushort param3,
                uLink.NetworkMessageInfo param4, Util.PlayerActions param5)
            {
            };
            OnResearch = delegate(ResearchEvent param0) { };
            OnServerSaved = delegate { };
            OnItemPickup = delegate(ItemPickupEvent param0) { };
            OnFallDamage = delegate(FallDamageEvent param0) { };
            OnLootUse = delegate(LootStartEvent param0) { };
            OnShoot = delegate(ShootEvent param0) { };
            OnBowShoot = delegate(BowShootEvent param0) { };
            OnShotgunShoot = delegate(ShotgunShootEvent param0) { };
            OnGrenadeThrow = delegate(GrenadeThrowEvent param0) { };
            OnPlayerBan = delegate(BanEvent param0) { };
            OnRepairBench = delegate(Fougerite.Events.RepairEvent param0) { };
            OnItemMove = delegate(ItemMoveEvent param0) { };
            OnGenericSpawnerLoad = delegate(GenericSpawner param0) { };
            OnServerLoaded = delegate() { };
            OnSupplySignalExpode = delegate(SupplySignalExplosionEvent param0) { };
            OnBeltUse = delegate(BeltUseEvent param0) { };
            OnLogger = delegate(LoggerEvent param0) { };
        }
        
        public delegate void BlueprintUseHandlerDelegate(Player player, BPUseEvent ae);

        public delegate void ChatHandlerDelegate(Player player, ref ChatString text);

        public delegate void ChatRawHandlerDelegate(ref ConsoleSystem.Arg arg);

        public delegate void CommandHandlerDelegate(Player player, string cmd, string[] args);

        public delegate void CommandRawHandlerDelegate(ref ConsoleSystem.Arg arg);

        public delegate void ConnectionHandlerDelegate(Player player);

        public delegate void ConsoleHandlerDelegate(ref ConsoleSystem.Arg arg, bool external);

        public delegate void
            ConsoleHandlerWithCancelDelegate(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent ce);

        public delegate void DisconnectionHandlerDelegate(Player player);

        public delegate void DoorOpenHandlerDelegate(Player player, DoorEvent de);

        public delegate void EntityDecayDelegate(DecayEvent de);

        public delegate void EntityDeployedDelegate(Player player, Entity e);

        public delegate void EntityDeployedWithPlacerDelegate(Player player, Entity e,
            Player actualplacer);

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

        //public delegate void AirdropCrateDroppedDelegate(GameObject go);
    }
}