using System;
using System.Collections.Generic;

namespace Fougerite.PluginLoaders
{
    public class PluginLoader : Singleton<PluginLoader>, ISingleton
    {
        private bool _AllPluginsLoaded = false;
        public readonly Dictionary<string, BasePlugin> Plugins = new Dictionary<string, BasePlugin>();
        public readonly Dictionary<PluginType, IPluginLoader> PluginLoaders = new Dictionary<PluginType, IPluginLoader>();
        public List<String> CurrentlyLoadingPlugins = new List<string>();
        
        public static string ModulesFolder = Config.GetModulesFolder();
        public static string PublicFolder = Config.GetPublicFolder();
        
        // TODO: Collect the commands from the script plugins automatically, or add a feature or not.

        public readonly List<string> HookNames = new List<string>()
        {
            "On_TablesLoaded",
            "On_AllPluginsLoaded",
            "On_BlueprintUse",
            "On_Chat",
            "On_Command",
            "On_Console",
            "On_DoorUse",
            "On_EntityDecay",
            "On_EntityDeployed",
            "On_EntityDestroyed",
            "On_EntityHurt",
            "On_ItemsLoaded",
            "On_NPCHurt",
            "On_NPCKilled",
            "On_PlayerConnected",
            "On_PlayerDisconnected",
            "On_PlayerGathering",
            "On_PlayerHurt",
            "On_PlayerKilled",
            "On_PlayerTeleport",
            "On_PlayerSpawning",
            "On_PlayerSpawned",
            "On_Research",
            "On_ServerInit",
            "On_ServerShutdown",
            "On_ServerSaved",
            "On_Crafting",
            "On_ResourceSpawn",
            "On_ItemAdded",
            "On_ItemRemoved",
            "On_ItemPickup",
            "On_FallDamage",
            "On_Airdrop",
            "On_SteamDeny",
            "On_PlayerApproval",
            "On_PluginShutdown",
            "On_VoiceChat",
            "On_LootUse",
            "On_PlayerBan",
            "On_RepairBench",
            "On_ItemMove",
            "On_GenericSpawnLoad",
            "On_ServerLoaded",
            "On_SupplySignalExploded",
            "On_PlayerMove",
            "On_BeltUse",
            "On_Logger",
            "On_GrenadeThrow",
            "On_ConsoleWithCancel",
            "On_AirdropCrateDropped",
            "On_SupplyDropPlaneCreated",
            "On_NPCSpawned",
            "On_TimedExplosiveSpawned",
            "On_SleeperSpawned",
            "On_CommandRestriction",
            "On_FireBarrelToggle",
            "On_DayCycleChanged",
            "On_Shoot",
            "On_ShotgunShoot",
            "On_BowShoot",
            "On_AnimalMovement",
            "On_ConsumableUse",
            "On_MedikitUse",
            "On_ItemModInstall",
            "On_BloodDraw",
            "On_ArmorEquip",
            "On_ArmorUnEquip",
            "On_FlareThrow",
            "On_FlareIgnite",
            "On_TorchIgnite",
            "On_HeatZoneEnter",
            "On_WorkZoneEnter",
            "On_PluginMessage",
            "On_CraftingCancel",
            "On_CraftingComplete",
            "On_ServerTick",
            "On_MetabolismUpdate"
        };

        public void Initialize()
        {
            BasePlugin.GlobalData = new Dictionary<string, object>();
        }

        public bool CheckDependencies()
        {
            return true;
        }

        public void OnPluginLoaded(BasePlugin plugin)
        {
            if (CurrentlyLoadingPlugins.Contains(plugin.Name))
            {
                CurrentlyLoadingPlugins.Remove(plugin.Name);
            }

            if (plugin.State != PluginState.Loaded)
            {
                Logger.LogError($"[PluginLoader] Failed to initalize {plugin.Name}.");
                return;
            }

            InstallHooks(plugin);
            Plugins[plugin.Name] = plugin;

            if (CurrentlyLoadingPlugins.Count == 0 && !_AllPluginsLoaded)
            {
                _AllPluginsLoaded = true;
                Hooks.AllPluginsLoaded();
            }

            Logger.Log(string.Format("[PluginLoader] Module {0}<{3}> v{1} (by {2}) initiated.", plugin.Name, plugin.Version, plugin.Author, plugin.Type));
        }

        public void LoadPlugin(string name, PluginType t)
        {
            PluginLoaders[t].LoadPlugin(name);
        }

        public void LoadPlugins()
        {
            foreach (IPluginLoader loader in PluginLoaders.Values)
            {
                loader.LoadPlugins();
            }
        }

        public void UnloadPlugins()
        {
            foreach (IPluginLoader loader in PluginLoaders.Values)
            {
                loader.UnloadPlugins();
            }
        }

        public void UnloadPlugin(string name)
        {
            if (Plugins.ContainsKey(name))
            {
                PluginLoaders[Plugins[name].Type].UnloadPlugin(name);
            }
        }

        public void ReloadPlugins()
        {
            foreach (IPluginLoader loader in PluginLoaders.Values)
            {
                loader.ReloadPlugins();
            }
        }

        public void ReloadPlugin(string name)
        {
            if (Plugins.ContainsKey(name))
            {
                PluginLoaders[Plugins[name].Type].ReloadPlugin(name);
            }
        }

        public void ReloadPlugin(BasePlugin plugin)
        {
            if (Plugins.ContainsKey(plugin.Name))
            {
                var loader = PluginLoaders[plugin.Type];
                string name = plugin.Name;
                loader.UnloadPlugin(name);
                plugin = null;
                if (Plugins.ContainsKey(name))
                {
                    Plugins.Remove(name);
                }

                loader.LoadPlugin(name);
            }
        }

        public void InstallHooks(BasePlugin plugin)
        {
            if (plugin.State != PluginState.Loaded)
                return;

            foreach (string method in plugin.Globals)
            {
                if (HookNames.Contains(method))
                {
                    Logger.LogDebug($"[{plugin.Type}] Adding hook: {plugin.Name}.{method}");

                    switch (method)
                    {
                        case "On_ServerInit":
                            Hooks.OnServerInit += plugin.OnServerInit;
                            break;
                        case "On_ServerShutdown":
                            Hooks.OnServerShutdown += plugin.OnServerShutdown;
                            break;
                        case "On_ItemsLoaded":
                            Hooks.OnItemsLoaded += plugin.OnItemsLoaded;
                            break;
                        case "On_TablesLoaded":
                            Hooks.OnTablesLoaded += plugin.OnTablesLoaded;
                            break;
                        case "On_Chat":
                            Hooks.OnChat += plugin.OnChat;
                            break;
                        case "On_Console":
#pragma warning disable CS0618
                            Hooks.OnConsoleReceived += plugin.OnConsole;
#pragma warning restore CS0618
                            break;
                        case "On_ConsoleWithCancel":
                            Hooks.OnConsoleReceivedWithCancel += plugin.OnConsoleWithCancel;
                            break;
                        case "On_Command":
                            Hooks.OnCommand += plugin.OnCommand;
                            break;
                        case "On_PlayerConnected":
                            Hooks.OnPlayerConnected += plugin.OnPlayerConnected;
                            break;
                        case "On_PlayerDisconnected":
                            Hooks.OnPlayerDisconnected += plugin.OnPlayerDisconnected;
                            break;
                        case "On_PlayerKilled":
                            Hooks.OnPlayerKilled += plugin.OnPlayerKilled;
                            break;
                        case "On_PlayerHurt":
                            Hooks.OnPlayerHurt += plugin.OnPlayerHurt;
                            break;
                        case "On_PlayerSpawning":
                            Hooks.OnPlayerSpawning += plugin.OnPlayerSpawn;
                            break;
                        case "On_PlayerSpawned":
                            Hooks.OnPlayerSpawned += plugin.OnPlayerSpawned;
                            break;
                        case "On_PlayerGathering":
                            Hooks.OnPlayerGathering += plugin.OnPlayerGathering;
                            break;
                        case "On_EntityHurt":
                            Hooks.OnEntityHurt += plugin.OnEntityHurt;
                            break;
                        case "On_EntityDecay":
                            Hooks.OnEntityDecay += plugin.OnEntityDecay;
                            break;
                        case "On_EntityDestroyed":
                            Hooks.OnEntityDestroyed += plugin.OnEntityDestroyed;
                            break;
                        case "On_EntityDeployed":
                            Hooks.OnEntityDeployedWithPlacer += plugin.OnEntityDeployed;
                            break;
                        case "On_NPCHurt":
                            Hooks.OnNPCHurt += plugin.OnNPCHurt;
                            break;
                        case "On_NPCKilled":
                            Hooks.OnNPCKilled += plugin.OnNPCKilled;
                            break;
                        case "On_BlueprintUse":
                            Hooks.OnBlueprintUse += plugin.OnBlueprintUse;
                            break;
                        case "On_DoorUse":
                            Hooks.OnDoorUse += plugin.OnDoorUse;
                            break;
                        case "On_AllPluginsLoaded":
                            Hooks.OnAllPluginsLoaded += plugin.OnAllPluginsLoaded;
                            break;
                        case "On_PlayerTeleport":
                            Hooks.OnPlayerTeleport += plugin.OnPlayerTeleport;
                            break;
                        //case "On_PluginInit": plugin.Invoke("On_PluginInit", new object[0]); break;
                        case "On_Crafting":
                            Hooks.OnCrafting += plugin.OnCrafting;
                            break;
                        case "On_ResourceSpawn":
                            Hooks.OnResourceSpawned += plugin.OnResourceSpawned;
                            break;
                        case "On_ItemAdded":
                            Hooks.OnItemAdded += plugin.OnItemAdded;
                            break;
                        case "On_ItemRemoved":
                            Hooks.OnItemRemoved += plugin.OnItemRemoved;
                            break;
                        case "On_Airdrop":
                            Hooks.OnAirdropCalled += plugin.OnAirdrop;
                            break;
                        case "On_AirdropCrateDropped": 
                            Hooks.OnAirdropCrateDropped += plugin.OnAirdropCrateDropped;
                            break;
                        case "On_SteamDeny":
                            Hooks.OnSteamDeny += plugin.OnSteamDeny;
                            break;
                        case "On_PlayerApproval":
                            Hooks.OnPlayerApproval += plugin.OnPlayerApproval;
                            break;
                        case "On_Research":
                            Hooks.OnResearch += plugin.OnResearch;
                            break;
                        case "On_ServerSaved":
                            Hooks.OnServerSaved += plugin.OnServerSaved;
                            break;
                        case "On_VoiceChat":
                            Hooks.OnShowTalker += plugin.OnShowTalker;
                            break;
                        case "On_ItemPickup":
                            Hooks.OnItemPickup += plugin.OnItemPickup;
                            break;
                        case "On_FallDamage":
                            Hooks.OnFallDamage += plugin.OnFallDamage;
                            break;
                        case "On_LootUse":
                            Hooks.OnLootUse += plugin.OnLootUse;
                            break;
                        case "On_PlayerBan":
                            Hooks.OnPlayerBan += plugin.OnBanEvent;
                            break;
                        case "On_RepairBench":
                            Hooks.OnRepairBench += plugin.OnRepairBench;
                            break;
                        case "On_ItemMove":
                            Hooks.OnItemMove += plugin.OnItemMove;
                            break;
                        case "On_GenericSpawnLoad":
                            Hooks.OnGenericSpawnerLoad += plugin.OnGenericSpawnLoad;
                            break;
                        case "On_ServerLoaded":
                            Hooks.OnServerLoaded += plugin.OnServerLoaded;
                            break;
                        case "On_SupplySignalExploded":
                            Hooks.OnSupplySignalExpode += plugin.OnSupplySignalExploded;
                            break;
                        case "On_PlayerMove":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnPlayerMove += plugin.OnPlayerMove;
                            }
                            break;
                        case "On_BeltUse":
                            Hooks.OnBeltUse += plugin.OnBeltUse;
                            break;
                        case "On_Logger":
                            Hooks.OnLogger += plugin.OnLogger;
                            break;
                        case "On_GrenadeThrow":
                            Hooks.OnGrenadeThrow += plugin.OnGrenade;
                            break;
                        case "On_SupplyDropPlaneCreated":
                            Hooks.OnSupplyDropPlaneCreated += plugin.OnSupplyDropPlaneCreated;
                            break;
                        case "On_NPCSpawned":
                            Hooks.OnNPCSpawned += plugin.OnNPCSpawn;
                            break;
                        case "On_TimedExplosiveSpawned":
                            Hooks.OnTimedExplosiveSpawned += plugin.OnTimedExplosiveSpawned;
                            break;
                        case "On_SleeperSpawned":
                            Hooks.OnSleeperSpawned += plugin.OnSleeperSpawned;
                            break;
                        case "On_CommandRestriction":
                            Hooks.OnCommandRestriction += plugin.OnCommandRestriction;
                            break;
                        case "On_FireBarrelToggle":
                            Hooks.OnFireBarrelToggle += plugin.OnFireBarrelToggle;
                            break;
                        case "On_DayCycleChanged":
                            Hooks.OnDayCycleChanged += plugin.OnDayCycleChange;
                            break;
                        case "On_Shoot":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnShoot += plugin.OnShoot;
                            }
                            break;
                        case "On_ShotgunShoot":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnShotgunShoot += plugin.OnShotgunShoot;
                            }
                            break;
                        case "On_BowShoot":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnBowShoot += plugin.OnBowShoot;
                            }
                            break;
                        case "On_AnimalMovement":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnAnimalMovement += plugin.OnAnimalMovement;
                            }
                            break;
                        case "On_ConsumableUse":
                            Hooks.OnConsumableUse += plugin.OnConsumableUse;
                            break;
                        case "On_MedikitUse":
                            Hooks.OnMedikitUse += plugin.OnMedikitUse;
                            break;
                        case "On_ItemModInstall":
                            Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall += plugin.OnItemModInstall;
                            break;
                        case "On_BloodDraw":
                            Hooks.OnBloodDraw += plugin.OnBloodDraw;
                            break;
                        case "On_ArmorEquip":
                            Hooks.OnArmorEquip += plugin.OnArmorEquip;
                            break;
                        case "On_ArmorUnEquip":
                            Hooks.OnArmorUnEquip += plugin.OnArmorUnEquip;
                            break;
                        case "On_FlareThrow":
                            Hooks.OnFlareThrow += plugin.OnFlareThrow;
                            break;
                        case "On_FlareIgnite":
                            Hooks.OnFlareIgnite += plugin.FlareIgnite;
                            break;
                        case "On_TorchIgnite":
                            Hooks.OnBasicTorchIgnite += plugin.OnTorchIgnite;
                            break;
                        case "On_HeatZoneEnter":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnHeatZoneEnter += plugin.OnHeatZoneEnter;
                            }
                            break;
                        case "On_WorkZoneEnter":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnWorkZoneEnter += plugin.OnWorkZoneEnter;
                            }
                            break;
                        case "On_PluginMessage":
                            Hooks.OnPluginMessage += plugin.OnPluginMessage;
                            break;
                        case "On_CraftingCancel":
                            Hooks.OnCraftCancel += plugin.OnCraftingCancel;
                            break;
                        case "On_CraftingComplete":
                            Hooks.OnCraftComplete += plugin.OnCraftingComplete;
                            break;
                        case "On_ServerTick":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnServerTick += plugin.OnServerTick;
                            }
                            break;
                        case "On_MetabolismUpdate":
                            Hooks.OnMetabolismUpdate += plugin.OnMetabolismUpdate;
                            break;
                    }
                }
            }

            if (plugin.Globals.Contains("On_PluginInit"))
                plugin.Invoke("On_PluginInit");
        }

        public void RemoveHooks(BasePlugin plugin)
        {
            if (plugin.State != PluginState.Loaded)
                return;

            foreach (string method in plugin.Globals)
            {
                if (HookNames.Contains(method))
                {
                    Logger.LogDebug($"[{plugin.Type}] Removing hook: {plugin.Name}.{method}");

                    switch (method)
                    {
                        case "On_ServerInit":
                            Hooks.OnServerInit -= plugin.OnServerInit;
                            break;
                        case "On_ServerShutdown":
                            Hooks.OnServerShutdown -= plugin.OnServerShutdown;
                            break;
                        case "On_ItemsLoaded":
                            Hooks.OnItemsLoaded -= plugin.OnItemsLoaded;
                            break;
                        case "On_TablesLoaded":
                            Hooks.OnTablesLoaded -= plugin.OnTablesLoaded;
                            break;
                        case "On_Chat":
                            Hooks.OnChat -= plugin.OnChat;
                            break;
                        case "On_Console":
#pragma warning disable CS0618
                            Hooks.OnConsoleReceived -= plugin.OnConsole;
#pragma warning restore CS0618
                            break;
                        case "On_ConsoleWithCancel":
                            Hooks.OnConsoleReceivedWithCancel -= plugin.OnConsoleWithCancel;
                            break;
                        case "On_Command":
                            Hooks.OnCommand -= plugin.OnCommand;
                            break;
                        case "On_PlayerConnected":
                            Hooks.OnPlayerConnected -= plugin.OnPlayerConnected;
                            break;
                        case "On_PlayerDisconnected":
                            Hooks.OnPlayerDisconnected -= plugin.OnPlayerDisconnected;
                            break;
                        case "On_PlayerKilled":
                            Hooks.OnPlayerKilled -= plugin.OnPlayerKilled;
                            break;
                        case "On_PlayerHurt":
                            Hooks.OnPlayerHurt -= plugin.OnPlayerHurt;
                            break;
                        case "On_PlayerSpawning":
                            Hooks.OnPlayerSpawning -= plugin.OnPlayerSpawn;
                            break;
                        case "On_PlayerSpawned":
                            Hooks.OnPlayerSpawned -= plugin.OnPlayerSpawned;
                            break;
                        case "On_PlayerGathering":
                            Hooks.OnPlayerGathering -= plugin.OnPlayerGathering;
                            break;
                        case "On_EntityHurt":
                            Hooks.OnEntityHurt -= plugin.OnEntityHurt;
                            break;
                        case "On_EntityDecay":
                            Hooks.OnEntityDecay -= plugin.OnEntityDecay;
                            break;
                        case "On_EntityDestroyed":
                            Hooks.OnEntityDestroyed -= plugin.OnEntityDestroyed;
                            break;
                        case "On_EntityDeployed":
                            Hooks.OnEntityDeployedWithPlacer -= plugin.OnEntityDeployed;
                            break;
                        case "On_NPCHurt":
                            Hooks.OnNPCHurt -= plugin.OnNPCHurt;
                            break;
                        case "On_NPCKilled":
                            Hooks.OnNPCKilled -= plugin.OnNPCKilled;
                            break;
                        case "On_BlueprintUse":
                            Hooks.OnBlueprintUse -= plugin.OnBlueprintUse;
                            break;
                        case "On_DoorUse":
                            Hooks.OnDoorUse -= plugin.OnDoorUse;
                            break;
                        case "On_AllPluginsLoaded":
                            Hooks.OnAllPluginsLoaded -= plugin.OnAllPluginsLoaded;
                            break;
                        case "On_PlayerTeleport":
                            Hooks.OnPlayerTeleport -= plugin.OnPlayerTeleport;
                            break;
                        //case "On_PluginInit": plugin.Invoke("On_PluginInit", new object[0]); break;
                        case "On_Crafting":
                            Hooks.OnCrafting -= plugin.OnCrafting;
                            break;
                        case "On_ResourceSpawn":
                            Hooks.OnResourceSpawned -= plugin.OnResourceSpawned;
                            break;
                        case "On_ItemAdded":
                            Hooks.OnItemAdded -= plugin.OnItemAdded;
                            break;
                        case "On_ItemRemoved":
                            Hooks.OnItemRemoved -= plugin.OnItemRemoved;
                            break;
                        case "On_Airdrop":
                            Hooks.OnAirdropCalled -= plugin.OnAirdrop;
                            break;
                        case "On_AirdropCrateDropped": 
                            Hooks.OnAirdropCrateDropped -= plugin.OnAirdropCrateDropped;
                            break;
                        case "On_SteamDeny":
                            Hooks.OnSteamDeny -= plugin.OnSteamDeny;
                            break;
                        case "On_PlayerApproval":
                            Hooks.OnPlayerApproval -= plugin.OnPlayerApproval;
                            break;
                        case "On_Research":
                            Hooks.OnResearch -= plugin.OnResearch;
                            break;
                        case "On_ServerSaved":
                            Hooks.OnServerSaved -= plugin.OnServerSaved;
                            break;
                        case "On_VoiceChat":
                            Hooks.OnShowTalker -= plugin.OnShowTalker;
                            break;
                        case "On_ItemPickup":
                            Hooks.OnItemPickup -= plugin.OnItemPickup;
                            break;
                        case "On_FallDamage":
                            Hooks.OnFallDamage -= plugin.OnFallDamage;
                            break;
                        case "On_LootUse":
                            Hooks.OnLootUse -= plugin.OnLootUse;
                            break;
                        case "On_PlayerBan":
                            Hooks.OnPlayerBan -= plugin.OnBanEvent;
                            break;
                        case "On_RepairBench":
                            Hooks.OnRepairBench -= plugin.OnRepairBench;
                            break;
                        case "On_ItemMove":
                            Hooks.OnItemMove -= plugin.OnItemMove;
                            break;
                        case "On_GenericSpawnLoad":
                            Hooks.OnGenericSpawnerLoad -= plugin.OnGenericSpawnLoad;
                            break;
                        case "On_ServerLoaded":
                            Hooks.OnServerLoaded -= plugin.OnServerLoaded;
                            break;
                        case "On_SupplySignalExploded":
                            Hooks.OnSupplySignalExpode -= plugin.OnSupplySignalExploded;
                            break;
                        case "On_PlayerMove":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnPlayerMove -= plugin.OnPlayerMove;
                            }
                            break;
                        case "On_BeltUse":
                            Hooks.OnBeltUse -= plugin.OnBeltUse;
                            break;
                        case "On_Logger":
                            Hooks.OnLogger -= plugin.OnLogger;
                            break;
                        case "On_GrenadeThrow":
                            Hooks.OnGrenadeThrow -= plugin.OnGrenade;
                            break;
                        case "On_SupplyDropPlaneCreated":
                            Hooks.OnSupplyDropPlaneCreated -= plugin.OnSupplyDropPlaneCreated;
                            break;
                        case "On_NPCSpawned":
                            Hooks.OnNPCSpawned -= plugin.OnNPCSpawn;
                            break;
                        case "On_TimedExplosiveSpawned":
                            Hooks.OnTimedExplosiveSpawned -= plugin.OnTimedExplosiveSpawned;
                            break;
                        case "On_SleeperSpawned":
                            Hooks.OnSleeperSpawned -= plugin.OnSleeperSpawned;
                            break;
                        case "On_CommandRestriction":
                            Hooks.OnCommandRestriction -= plugin.OnCommandRestriction;
                            break;
                        case "On_FireBarrelToggle":
                            Hooks.OnFireBarrelToggle -= plugin.OnFireBarrelToggle;
                            break;
                        case "On_DayCycleChanged":
                            Hooks.OnDayCycleChanged -= plugin.OnDayCycleChange;
                            break;
                        case "On_Shoot":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnShoot -= plugin.OnShoot;
                            }
                            break;
                        case "On_ShotgunShoot":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnShotgunShoot -= plugin.OnShotgunShoot;
                            }
                            break;
                        case "On_BowShoot":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnBowShoot -= plugin.OnBowShoot;
                            }
                            break;
                        case "On_AnimalMovement":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnAnimalMovement -= plugin.OnAnimalMovement;
                            }
                            break;
                        case "On_ConsumableUse":
                            Hooks.OnConsumableUse -= plugin.OnConsumableUse;
                            break;
                        case "On_MedikitUse":
                            Hooks.OnMedikitUse -= plugin.OnMedikitUse;
                            break;
                        case "On_ItemModInstall":
                            Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall -= plugin.OnItemModInstall;
                            break;
                        case "On_BloodDraw":
                            Hooks.OnBloodDraw -= plugin.OnBloodDraw;
                            break;
                        case "On_ArmorEquip":
                            Hooks.OnArmorEquip -= plugin.OnArmorEquip;
                            break;
                        case "On_ArmorUnEquip":
                            Hooks.OnArmorUnEquip -= plugin.OnArmorUnEquip;
                            break;
                        case "On_FlareThrow":
                            Hooks.OnFlareThrow -= plugin.OnFlareThrow;
                            break;
                        case "On_FlareIgnite":
                            Hooks.OnFlareIgnite -= plugin.FlareIgnite;
                            break;
                        case "On_TorchIgnite":
                            Hooks.OnBasicTorchIgnite -= plugin.OnTorchIgnite;
                            break;
                        case "On_HeatZoneEnter":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnHeatZoneEnter -= plugin.OnHeatZoneEnter;
                            }
                            break;
                        case "On_WorkZoneEnter":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnWorkZoneEnter -= plugin.OnWorkZoneEnter;
                            }
                            break;
                        case "On_PluginMessage":
                            Hooks.OnPluginMessage -= plugin.OnPluginMessage;
                            break;
                        case "On_CraftingCancel":
                            Hooks.OnCraftCancel -= plugin.OnCraftingCancel;
                            break;
                        case "On_CraftingComplete":
                            Hooks.OnCraftComplete -= plugin.OnCraftingComplete;
                            break;
                        case "On_ServerTick":
                            if (IsIntensiveEventAllowed(plugin))
                            {
                                Hooks.OnServerTick -= plugin.OnServerTick;
                            }
                            break;
                        case "On_MetabolismUpdate":
                            Hooks.OnMetabolismUpdate -= plugin.OnMetabolismUpdate;
                            break;
                    }
                }

                if (plugin.Globals.Contains("On_PluginShutdown"))
                    plugin.OnPluginShutdown();
            }
        }

        private bool IsIntensiveEventAllowed(BasePlugin plugin)
        {
            return plugin.Type == PluginType.CSharp || plugin.Type == PluginType.CSScript ||
                   Bootstrap.EnableScriptPluginsIntensiveEvents;
        }
    }
}