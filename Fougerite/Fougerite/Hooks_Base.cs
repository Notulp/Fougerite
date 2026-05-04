using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Fougerite.Concurrent;
using Fougerite.Events;
using UnityEngine;

namespace Fougerite
{
    public partial class Hooks
    {
        public static ConcurrentDictionary<int, Entity> DecayList = new ConcurrentDictionary<int, Entity>();
        [Obsolete("Left for backwards compatibility reasons... Do not use. Use DecayList.", false)]
        public static List<object> decayList = new List<object>();
        public static Hashtable talkerTimers = new Hashtable();
        public static bool ServerInitialized = false;
        public static readonly List<ulong> uLinkDCCache = new List<ulong>();
        internal static ConcurrentDictionary<string, Flood> FloodChecks = new ConcurrentDictionary<string, Flood>();
        internal static ConcurrentDictionary<string, DateTime> FloodCooldown = new ConcurrentDictionary<string, DateTime>();
        private static bool? _isNight = null;

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
        /// This delegate runs when a firebarrel is toggled.
        /// </summary>
        public static event FireBarrelToggleEventDelegate OnFireBarrelToggle;
        
        /// <summary>
        /// This delegate runs when the day cycle changes from day to night or night to day.
        /// </summary>
        public static event DayCycleChangeEventDelegate OnDayCycleChanged;
        
        /// <summary>
        /// This delegate runs when an animal moves.
        /// </summary>
        public static event AnimalMovementEventDelegate OnAnimalMovement;
        
        /// <summary>
        /// This delegate runs when an item is consumed.
        /// </summary>
        public static event ConsumableUseEventDelegate OnConsumableUse;
        
        /// <summary>
        /// This delegate runs when a player uses a medical kit or bandage.
        /// </summary>
        public static event MedikitUseEventDelegate OnMedikitUse;

        /// <summary>
        /// This delegate runs when a plugin sends a message to another plugin.
        /// </summary>
        public static event PluginMessageHandlerDelegate OnPluginMessage;
        
        /// <summary>
        /// This delegate runs when a player attempts to cancel a crafting operation.
        /// </summary>
        public static event CraftingCancelDelegate OnCraftCancel;

        /// <summary>
        /// This delegate runs when a crafting operation completes.
        /// </summary>
        public static event CraftingCompleteDelegate OnCraftComplete;
        
        /// <summary>
        /// This delegate runs on every server tick.
        /// Use with caution as it can be called multiple times per second and may cause performance issues if used improperly.
        /// It only runs if the server has been fully initialized, and is not shutting down.
        /// </summary>
        public static event ServerTickDelegate OnServerTick;
        
        /// <summary>
        /// This delegate runs when a player's metabolism updates.
        /// Called every 3 second for each player, so carefully use It in script plugins.
        /// </summary>
        public static event MetabolismUpdateDelegate OnMetabolismUpdate;
        
        /// <summary>
        /// A central registry used to manage the lifecycle of generic item modification events.
        /// Since OnItemMod<T> creates a unique static class for every weapon type, this helper
        /// allows the framework to clear all weapon-specific events simultaneously during a hook reset.
        /// </summary>
        internal static class OnItemModInstallHelper
        {
            /// <summary>
            /// A collection of delegates used to reset the OnItemModInstall events.
            /// </summary>
            private static readonly List<Action> ClearActions = new List<Action>();

            /// <summary>
            /// Registers a cleanup delegate for a specific weapon type. 
            /// This is typically called automatically by the static constructor of OnItemMod<T>.
            /// </summary>
            /// <param name="clearAction">The delegate that sets the generic event to null/empty.</param>
            internal static void Register(Action clearAction)
            {
                ClearActions.Add(clearAction);
            }

            /// <summary>
            /// Iterates through all registered weapon types and clears their event subscriptions.
            /// This prevents memory leaks and "ghost" events when plugins are reloaded.
            /// </summary>
            internal static void Clear()
            {
                foreach (var action in ClearActions)
                {
                    action();
                }
            }
        }

        /// <summary>
        /// A type-safe container for item modification events. 
        /// Because this class is generic, the C# compiler creates a unique instance of 
        /// <see cref="OnItemModInstall"/> for every subclass of <see cref="HeldItemDataBlock"/>.
        /// </summary>
        /// <typeparam name="T">The specific data block type of the item (like BulletWeaponDataBlock).</typeparam>
        public static class OnItemMod<T> where T : HeldItemDataBlock
        {
            /// <summary>
            /// Triggered when an attachment (mod) is being installed on a weapon of type T.
            /// Use BulletWeaponDataBlock for guns.
            /// </summary>
            public static event ItemModInstalledEventDelegate<T> OnItemModInstall;

            /// <summary>
            /// Static constructor. Registers this specific generic type with the 
            /// <see cref="OnItemModInstallHelper"/> the first time it is accessed.
            /// </summary>
            static OnItemMod()
            {
                OnItemModInstallHelper.Register(() => OnItemModInstall = delegate { });
            }

            /// <summary>
            /// Raises the modification event and notifies all subscribers via the standard 
            /// Fougerite <see cref="Hooks.ExecuteSubscribers"/> logic.
            /// </summary>
            /// <param name="e">The event arguments containing the weapon instance and the mod data.</param>
            internal static void Raise(ItemModInstallEvent<T> e)
            {
                ExecuteSubscribers(OnItemModInstall, "OnItemModInstall", e);
            }
        }
        
        /// <summary>
        /// This delegate runs when a player uses a Blood Draw Kit.
        /// </summary>
        public static event BloodDrawUseEventDelegate OnBloodDraw;
        
        /// <summary>
        /// This delegate runs when armor is equipped.
        /// </summary>
        public static event ArmorEquippedEventDelegate OnArmorEquip;

        /// <summary>
        /// This delegate runs when armor is unequipped.
        /// </summary>
        public static event ArmorEquippedEventDelegate OnArmorUnEquip;
        
        /// <summary>
        /// This delegate runs when a player throws a flare.
        /// </summary>
        public static event FlareThrowEventDelegate OnFlareThrow;
        
        /// <summary>
        /// This delegate runs when a player ignites a flare.
        /// If you need de-selection handling then use OnBeltUse, and write yourself the logic.
        /// </summary>
        public static event FlareIgniteEventDelegate OnFlareIgnite;
        
        /// <summary>
        /// This delegate runs when a player ignites a basic torch.
        /// If you need de-selection handling then use OnBeltUse, and write yourself the logic.
        /// </summary>
        public static event BasicTorchIgniteEventDelegate OnBasicTorchIgnite;
        
        /// <summary>
        /// This delegate runs when a player is within the trigger of a HeatZone.
        /// This event runs continously while the player is inside the trigger, so be careful when using It in script plugins.
        /// </summary>
        public static event HeatZoneEnterEventDelegate OnHeatZoneEnter;
        
        /// <summary>
        /// This delegate runs when a player is within the trigger of a WorkZone.
        /// This event runs continously while the player is inside the trigger, so be careful when using It in script plugins.
        /// </summary>
        public static event WorkZoneEnterEventDelegate OnWorkZoneEnter;
        
        /// <summary>
        /// This delegate runs when a websocket message is received.
        /// </summary>
        public static event WebSocketEventHandlerDelegate OnWebSocketMessage;

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
            OnFireBarrelToggle = delegate {  };
            OnDayCycleChanged = delegate {  };
            OnAnimalMovement = delegate {  };
            OnConsumableUse = delegate {  };
            OnMedikitUse = delegate {  };
            OnItemModInstallHelper.Clear();
            OnBloodDraw = delegate {  };
            OnArmorEquip = delegate {  };
            OnArmorUnEquip = delegate {  };
            OnFlareThrow = delegate {  };
            OnFlareIgnite = delegate {  };
            OnBasicTorchIgnite = delegate {  };
            OnPluginMessage = delegate {  };
            OnCraftCancel = delegate {  };
            OnCraftComplete = delegate {  };
            OnServerTick = delegate { };
            OnMetabolismUpdate = delegate { };
            OnWebSocketMessage = delegate { };
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
        
        public delegate void FireBarrelToggleEventDelegate(FireBarrelToggleEvent fbte);
        
        public delegate void DayCycleChangeEventDelegate(DayCycleChangeEvent dcche);
        
        public delegate void AnimalMovementEventDelegate(AnimalMovementEvent ame);
        
        public delegate void ConsumableUseEventDelegate(ConsumableUseEvent e);
        
        public delegate void MedikitUseEventDelegate(MedikitUseEvent e);
        
        public delegate void ItemModInstalledEventDelegate<T>(ItemModInstallEvent<T> e) where T : HeldItemDataBlock;
        
        public delegate void BloodDrawUseEventDelegate(BloodDrawEvent be);
        
        public delegate void ArmorEquippedEventDelegate(ArmorEquipEvent ae);
        
        public delegate void FlareThrowEventDelegate(FlareThrowEvent fe);
        
        public delegate void FlareIgniteEventDelegate(FlareIgniteEvent tie);
        
        public delegate void BasicTorchIgniteEventDelegate(BasicTorchIgniteEvent btie);
        
        public delegate void HeatZoneEnterEventDelegate(HeatZoneEnterEvent hze);
        
        public delegate void WorkZoneEnterEventDelegate(WorkZoneEnterEvent wze);
        
        public delegate void PluginMessageHandlerDelegate(PluginMessageEvent e);
        
        public delegate void CraftingCancelDelegate(CraftCancelEvent e);
        
        public delegate void CraftingCompleteDelegate(CraftCompleteEvent e);
        
        public delegate void ServerTickDelegate();
        
        public delegate void MetabolismUpdateDelegate(MetabolismEvent e);
        
        public delegate void WebSocketEventHandlerDelegate(WebSocketEvent e);
        
        /// <summary>
        /// Flags for Method.Invoke
        /// </summary>
        private static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        /// <summary>
        /// Safely calls all subscribers of the event and handles each error individually
        /// rather than having the chain of invocation broken on one exception.
        /// Console/ChatRaw is excluded due to passing refs.
        /// </summary>
        /// <param name="delegateOfEvent"></param>
        /// <param name="eventName"></param>
        /// <param name="parameters"></param>
        public static bool ExecuteSubscribers(Delegate delegateOfEvent, string eventName, params object[] parameters)
        {
            // Sanity check
            if (delegateOfEvent == null)
            {
                return false;
            }
            
            // Additional stuff
            Binder binder = Type.DefaultBinder;
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            
            // Iterate all subscribers
            bool success = true;
            foreach (Delegate x in delegateOfEvent.GetInvocationList())
            {
                try
                {
                    x.Method.Invoke(x.Target, Flags, binder, parameters, cultureInfo);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"{eventName} Error: {ex}");
                    success = false;
                }
            }
            
            return success;
        }
    }
}