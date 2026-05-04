namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// The PluginLoaderEvents class defines a collection of event names that represent various actions and events
    /// within the plugin system for the application. These event names can be used as identifiers for subscribing
    /// or triggering specific events in the plugin-loading lifecycle or game interactions.
    /// This class serves primarily as a static registry of event names for referencing within the framework.
    /// </summary>
    public static class PluginLoaderEvents
    {
        public const string OnTablesLoaded = "On_TablesLoaded";
        public const string OnAllPluginsLoaded = "On_AllPluginsLoaded";
        public const string OnBlueprintUse = "On_BlueprintUse";
        public const string OnChat = "On_Chat";
        public const string OnCommand = "On_Command";
        public const string OnConsole = "On_Console";
        public const string OnDoorUse = "On_DoorUse";
        public const string OnEntityDecay = "On_EntityDecay";
        public const string OnEntityDeployed = "On_EntityDeployed";
        public const string OnEntityDestroyed = "On_EntityDestroyed";
        public const string OnEntityHurt = "On_EntityHurt";
        public const string OnItemsLoaded = "On_ItemsLoaded";
        public const string OnNPCHurt = "On_NPCHurt";
        public const string OnNPCKilled = "On_NPCKilled";
        public const string OnPlayerConnected = "On_PlayerConnected";
        public const string OnPlayerDisconnected = "On_PlayerDisconnected";
        public const string OnPlayerGathering = "On_PlayerGathering";
        public const string OnPlayerHurt = "On_PlayerHurt";
        public const string OnPlayerKilled = "On_PlayerKilled";
        public const string OnPlayerTeleport = "On_PlayerTeleport";
        public const string OnPlayerSpawning = "On_PlayerSpawning";
        public const string OnPlayerSpawned = "On_PlayerSpawned";
        public const string OnResearch = "On_Research";
        public const string OnServerInit = "On_ServerInit";
        public const string OnServerShutdown = "On_ServerShutdown";
        public const string OnServerSaved = "On_ServerSaved";
        public const string OnCrafting = "On_Crafting";
        public const string OnResourceSpawn = "On_ResourceSpawn";
        public const string OnItemAdded = "On_ItemAdded";
        public const string OnItemRemoved = "On_ItemRemoved";
        public const string OnItemPickup = "On_ItemPickup";
        public const string OnFallDamage = "On_FallDamage";
        public const string OnAirdrop = "On_Airdrop";
        public const string OnSteamDeny = "On_SteamDeny";
        public const string OnPlayerApproval = "On_PlayerApproval";
        public const string OnPluginShutdown = "On_PluginShutdown";
        public const string OnVoiceChat = "On_VoiceChat";
        public const string OnLootUse = "On_LootUse";
        public const string OnPlayerBan = "On_PlayerBan";
        public const string OnRepairBench = "On_RepairBench";
        public const string OnItemMove = "On_ItemMove";
        public const string OnGenericSpawnLoad = "On_GenericSpawnLoad";
        public const string OnServerLoaded = "On_ServerLoaded";
        public const string OnSupplySignalExploded = "On_SupplySignalExploded";
        public const string OnPlayerMove = "On_PlayerMove";
        public const string OnBeltUse = "On_BeltUse";
        public const string OnLogger = "On_Logger";
        public const string OnGrenadeThrow = "On_GrenadeThrow";
        public const string OnConsoleWithCancel = "On_ConsoleWithCancel";
        public const string OnAirdropCrateDropped = "On_AirdropCrateDropped";
        public const string OnSupplyDropPlaneCreated = "On_SupplyDropPlaneCreated";
        public const string OnNPCSpawned = "On_NPCSpawned";
        public const string OnTimedExplosiveSpawned = "On_TimedExplosiveSpawned";
        public const string OnSleeperSpawned = "On_SleeperSpawned";
        public const string OnCommandRestriction = "On_CommandRestriction";
        public const string OnFireBarrelToggle = "On_FireBarrelToggle";
        public const string OnDayCycleChanged = "On_DayCycleChanged";
        public const string OnShoot = "On_Shoot";
        public const string OnShotgunShoot = "On_ShotgunShoot";
        public const string OnBowShoot = "On_BowShoot";
        public const string OnAnimalMovement = "On_AnimalMovement";
        public const string OnConsumableUse = "On_ConsumableUse";
        public const string OnMedikitUse = "On_MedikitUse";
        public const string OnItemModInstall = "On_ItemModInstall";
        public const string OnBloodDraw = "On_BloodDraw";
        public const string OnArmorEquip = "On_ArmorEquip";
        public const string OnArmorUnEquip = "On_ArmorUnEquip";
        public const string OnFlareThrow = "On_FlareThrow";
        public const string OnFlareIgnite = "On_FlareIgnite";
        public const string OnTorchIgnite = "On_TorchIgnite";
        public const string OnHeatZoneEnter = "On_HeatZoneEnter";
        public const string OnWorkZoneEnter = "On_WorkZoneEnter";
        public const string OnPluginMessage = "On_PluginMessage";
        public const string OnCraftingCancel = "On_CraftingCancel";
        public const string OnCraftingComplete = "On_CraftingComplete";
        public const string OnServerTick = "On_ServerTick";
        public const string OnMetabolismUpdate = "On_MetabolismUpdate";
        public const string OnPluginInit = "On_PluginInit";
        public const string OnWebSocketMessage = "On_WebSocketMessage";
    }
}