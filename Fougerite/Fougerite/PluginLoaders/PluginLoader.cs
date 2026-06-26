using System;
using System.Collections.Generic;
using Fougerite.Concurrent;

namespace Fougerite.PluginLoaders
{
    public class PluginLoader : Singleton<PluginLoader>, ISingleton
    {
        /// <summary>
        /// A boolean flag indicating whether all plugins have been successfully loaded.
        /// This value is set to <c>true</c> once all plugins complete their loading process.
        /// It ensures that no further loading is pending and can be used to trigger any
        /// post-loading logic or events.
        /// </summary>
        private bool _allPluginsLoaded = false;

        /// <summary>
        /// A dictionary that associates plugin names (<c>string</c>) with their corresponding plugin instances (<c>BasePlugin</c>).
        /// This collection is used to manage and retrieve loaded plugins in the system.
        /// Plugins are added to this dictionary upon being successfully loaded by their respective plugin loaders.
        /// </summary>
        public readonly Dictionary<string, BasePlugin> Plugins = new Dictionary<string, BasePlugin>();

        /// <summary>
        /// A dictionary that maps specific plugin types (<c>PluginType</c>) to their corresponding plugin loader implementations (<c>IPluginLoader</c>).
        /// This collection is used to manage and access the appropriate plugin loader instance during plugin operations.
        /// Plugin loaders, such as <c>CSharpPluginLoader</c> and <c>JavaScriptPluginLoader</c>, register themselves into this dictionary upon initialization.
        /// </summary>
        public readonly Dictionary<PluginType, IPluginLoader> PluginLoaders = new Dictionary<PluginType, IPluginLoader>();

        /// <summary>
        /// Maintains a list of plugin names currently being loaded by the application.
        /// This variable is primarily used to track loading operations and prevent duplicate loading of the same plugin.
        /// It is manipulated during plugin load procedures in classes such as <c>JavaScriptPluginLoader</c> and <c>PythonPluginLoader</c>.
        /// </summary>
        public List<String> CurrentlyLoadingPlugins = new List<string>();

        /// <summary>
        /// Specifies the directory path used for storing modules, which are external components or plugins loaded at runtime.
        /// This variable is statically initialized using the configuration value retrieved from <c>Config.GetModulesFolder()</c>.
        /// </summary>
        public static string ModulesFolder = Config.GetModulesFolder();

        /// <summary>
        /// Represents the directory path used as the public folder for storing publicly accessible resources or files.
        /// This variable is statically initialized using the configuration value retrieved from <c>Config.GetPublicFolder()</c>.
        /// </summary>
        public static string PublicFolder = Config.GetPublicFolder();

        // TODO: Collect the commands from the script plugins automatically, or add a feature or not.

        public readonly List<string> HookNames = new List<string>()
        {
            PluginLoaderEvents.OnTablesLoaded,
            PluginLoaderEvents.OnAllPluginsLoaded,
            PluginLoaderEvents.OnBlueprintUse,
            PluginLoaderEvents.OnChat,
            PluginLoaderEvents.OnCommand,
            PluginLoaderEvents.OnConsole,
            PluginLoaderEvents.OnDoorUse,
            PluginLoaderEvents.OnEntityDecay,
            PluginLoaderEvents.OnEntityDeployed,
            PluginLoaderEvents.OnEntityDestroyed,
            PluginLoaderEvents.OnEntityHurt,
            PluginLoaderEvents.OnItemsLoaded,
            PluginLoaderEvents.OnNPCHurt,
            PluginLoaderEvents.OnNPCKilled,
            PluginLoaderEvents.OnPlayerConnected,
            PluginLoaderEvents.OnPlayerDisconnected,
            PluginLoaderEvents.OnPlayerGathering,
            PluginLoaderEvents.OnPlayerHurt,
            PluginLoaderEvents.OnPlayerKilled,
            PluginLoaderEvents.OnPlayerTeleport,
            PluginLoaderEvents.OnPlayerSpawning,
            PluginLoaderEvents.OnPlayerSpawned,
            PluginLoaderEvents.OnResearch,
            PluginLoaderEvents.OnServerInit,
            PluginLoaderEvents.OnServerShutdown,
            PluginLoaderEvents.OnServerSaved,
            PluginLoaderEvents.OnCrafting,
            PluginLoaderEvents.OnResourceSpawn,
            PluginLoaderEvents.OnItemAdded,
            PluginLoaderEvents.OnItemRemoved,
            PluginLoaderEvents.OnItemPickup,
            PluginLoaderEvents.OnFallDamage,
            PluginLoaderEvents.OnAirdrop,
            PluginLoaderEvents.OnSteamDeny,
            PluginLoaderEvents.OnPlayerApproval,
            PluginLoaderEvents.OnPluginShutdown,
            PluginLoaderEvents.OnVoiceChat,
            PluginLoaderEvents.OnLootUse,
            PluginLoaderEvents.OnPlayerBan,
            PluginLoaderEvents.OnRepairBench,
            PluginLoaderEvents.OnItemMove,
            PluginLoaderEvents.OnGenericSpawnLoad,
            PluginLoaderEvents.OnServerLoaded,
            PluginLoaderEvents.OnSupplySignalExploded,
            PluginLoaderEvents.OnPlayerMove,
            PluginLoaderEvents.OnBeltUse,
            PluginLoaderEvents.OnLogger,
            PluginLoaderEvents.OnGrenadeThrow,
            PluginLoaderEvents.OnConsoleWithCancel,
            PluginLoaderEvents.OnAirdropCrateDropped,
            PluginLoaderEvents.OnSupplyDropPlaneCreated,
            PluginLoaderEvents.OnNPCSpawned,
            PluginLoaderEvents.OnTimedExplosiveSpawned,
            PluginLoaderEvents.OnSleeperSpawned,
            PluginLoaderEvents.OnCommandRestriction,
            PluginLoaderEvents.OnFireBarrelToggle,
            PluginLoaderEvents.OnDayCycleChanged,
            PluginLoaderEvents.OnShoot,
            PluginLoaderEvents.OnShotgunShoot,
            PluginLoaderEvents.OnBowShoot,
            PluginLoaderEvents.OnAnimalMovement,
            PluginLoaderEvents.OnConsumableUse,
            PluginLoaderEvents.OnMedikitUse,
            PluginLoaderEvents.OnItemModInstall,
            PluginLoaderEvents.OnBloodDraw,
            PluginLoaderEvents.OnArmorEquip,
            PluginLoaderEvents.OnArmorUnEquip,
            PluginLoaderEvents.OnFlareThrow,
            PluginLoaderEvents.OnFlareIgnite,
            PluginLoaderEvents.OnTorchIgnite,
            PluginLoaderEvents.OnHeatZoneEnter,
            PluginLoaderEvents.OnWorkZoneEnter,
            PluginLoaderEvents.OnPluginMessage,
            PluginLoaderEvents.OnCraftingCancel,
            PluginLoaderEvents.OnCraftingComplete,
            PluginLoaderEvents.OnServerTick,
            PluginLoaderEvents.OnMetabolismUpdate,
            PluginLoaderEvents.OnWebSocketMessage,
            PluginLoaderEvents.OnWebSocketConnected,
            PluginLoaderEvents.OnWebSocketClosed,
            PluginLoaderEvents.OnWebSocketError
        };

        /// <summary>
        /// Indicates whether all plugins managed by the loader have finished loading.
        /// When this property is set to <c>true</c>, it signifies that the loading process
        /// for all plugins is complete and no pending operations remain.
        /// </summary>
        public bool AllPluginsLoaded
        {
            get { return _allPluginsLoaded; }
        }

        /// Initializes the plugin loader, setting up global shared data for plugins.
        /// This method prepares the environment for plugins by initializing shared storage.
        /// It is called to ensure the plugin framework is fully prepared for operation.
        public void Initialize()
        {
            BasePlugin.GlobalData = new ConcurrentDictionary<string, object>();
        }

        public bool CheckDependencies()
        {
            return true;
        }

        /// Handles the event when a plugin is successfully loaded.
        /// This method performs necessary operations such as installing hooks
        /// and updating the plugin's state within the loader.
        /// <param name="plugin">The instance of the plugin that has been loaded.</param>
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

            if (CurrentlyLoadingPlugins.Count == 0 && !_allPluginsLoaded)
            {
                _allPluginsLoaded = true;
                Hooks.AllPluginsLoaded();
            }

            Logger.Log(string.Format("[PluginLoader] Module {0}<{3}> v{1} (by {2}) initiated.", plugin.Name, plugin.Version, plugin.Author, plugin.Type));
        }

        /// Loads a plugin of a specified type into the runtime environment.
        /// This method identifies the appropriate plugin loader by the given type
        /// and delegates the loading of the plugin to it.
        /// <param name="name">The name of the plugin to load.</param>
        /// <param name="t">The type of the plugin to load, specifying its scripting language or format.</param>
        public void LoadPlugin(string name, PluginType t)
        {
            PluginLoaders[t].LoadPlugin(name);
        }
        
        /// Loads a plugin of a specified type into the runtime environment.
        /// This method identifies the appropriate plugin loader by the given type
        /// and delegates the loading of the plugin to it.
        /// <param name="name">The name of the plugin to load.</param>
        /// <param name="callInit">If we should call the initialize function or not. Only for C# plugins.</param>
        public void LoadPlugin(string name, bool callInit)
        {
            foreach (var pluginLoader in PluginLoaders)
            {
                var plugins = pluginLoader.Value.GetPluginNames();
                foreach (var plugin in plugins)
                {
                    if (string.Equals(plugin, name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (pluginLoader.Key == PluginType.CSharp || pluginLoader.Key == PluginType.CSScript)
                        {
                            CSharpPluginLoader csharpLoader = (CSharpPluginLoader) pluginLoader.Value;
                            csharpLoader.LoadPlugin(plugin, callInit);
                        }
                        else
                        {
                            pluginLoader.Value.LoadPlugin(plugin);
                        }
                        return;
                    }
                }
            }
        }

        /// Loads all plugins using the registered plugin loaders.
        /// This method iterates through the registered plugin loaders
        /// and delegates the responsibility of loading plugins to each loader.
        /// It ensures that all plugins, regardless of their type, are loaded and prepared for operation.
        public void LoadPlugins()
        {
            foreach (IPluginLoader loader in PluginLoaders.Values)
            {
                loader.LoadPlugins();
            }
        }

        /// Unloads all loaded plugins by iterating through the registered plugin loaders.
        /// This method ensures that plugins are properly unloaded and resources are released.
        /// It calls the `UnloadPlugins` method for each plugin loader in the system.
        public void UnloadPlugins()
        {
            foreach (IPluginLoader loader in PluginLoaders.Values)
            {
                loader.UnloadPlugins();
            }
        }

        /// Unloads a plugin with the specified name.
        /// This method removes the plugin from the internal collection and delegates the unloading process
        /// to the appropriate plugin type loader, ensuring proper cleanup of the plugin's resources.
        /// <param name="name">The name of the plugin to unload.</param>
        public void UnloadPlugin(string name)
        {
            if (Plugins.ContainsKey(name))
            {
                PluginLoaders[Plugins[name].Type].UnloadPlugin(name);
            }
        }

        /// Reloads all the plugins managed by the plugin loader.
        /// This method iterates through all registered plugin loaders and triggers the reload functionality for each.
        /// It ensures that the plugins are reloaded within the application environment, potentially reflecting
        /// any updates or changes made to the plugins during runtime.
        public void ReloadPlugins()
        {
            foreach (IPluginLoader loader in PluginLoaders.Values)
            {
                loader.ReloadPlugins();
            }
        }

        /// Reloads a plugin with the specified name.
        /// This method attempts to reload a plugin by delegating the operation to the appropriate
        /// plugin loader based on the plugin type. If the plugin exists within the current plugin
        /// collection, it will invoke the relevant plugin loader to handle the reload process.
        /// <param name="name">The name of the plugin to reload.</param>
        public void ReloadPlugin(string name)
        {
            if (Plugins.ContainsKey(name))
            {
                PluginLoaders[Plugins[name].Type].ReloadPlugin(name);
            }
        }

        /// Reloads a plugin by unloading and reloading it.
        /// This method checks if the specified plugin is already loaded. If the plugin is found,
        /// it unloads the current instance and reloads it using the appropriate plugin loader.
        /// The plugin is removed and reloaded into the system using its name and type.
        /// <param name="plugin">The plugin instance to reload. Must be an already loaded plugin.</param>
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

        /// Installs hooks for a specified plugin, enabling its interaction with predefined events.
        /// This method evaluates the plugin's state and its available global methods.
        /// If the plugin contains event handlers listed in the predefined hook names, the corresponding hooks are installed.
        /// Additionally, the plugin's initialization event is invoked if applicable.
        /// <param name="plugin">The plugin for which hooks will be installed.</param>
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
                        case PluginLoaderEvents.OnServerInit:
                            Hooks.OnServerInit += plugin.BaseOnServerInit;
                            break;
                        case PluginLoaderEvents.OnServerShutdown:
                            Hooks.OnServerShutdown += plugin.BaseOnServerShutdown;
                            break;
                        case PluginLoaderEvents.OnItemsLoaded:
                            Hooks.OnItemsLoaded += plugin.BaseOnItemsLoaded;
                            break;
                        case PluginLoaderEvents.OnTablesLoaded:
                            Hooks.OnTablesLoaded += plugin.BaseOnTablesLoaded;
                            break;
                        case PluginLoaderEvents.OnChat:
                            Hooks.OnChat += plugin.BaseOnChat;
                            break;
                        case PluginLoaderEvents.OnConsole:
#pragma warning disable CS0618
                            Hooks.OnConsoleReceived += plugin.BaseOnConsole;
#pragma warning restore CS0618
                            break;
                        case PluginLoaderEvents.OnConsoleWithCancel:
                            Hooks.OnConsoleReceivedWithCancel += plugin.BaseOnConsoleWithCancel;
                            break;
                        case PluginLoaderEvents.OnCommand:
                            Hooks.OnCommand += plugin.BaseOnCommand;
                            break;
                        case PluginLoaderEvents.OnPlayerConnected:
                            Hooks.OnPlayerConnected += plugin.BaseOnPlayerConnected;
                            break;
                        case PluginLoaderEvents.OnPlayerDisconnected:
                            Hooks.OnPlayerDisconnected += plugin.BaseOnPlayerDisconnected;
                            break;
                        case PluginLoaderEvents.OnPlayerKilled:
                            Hooks.OnPlayerKilled += plugin.BaseOnPlayerKilled;
                            break;
                        case PluginLoaderEvents.OnPlayerHurt:
                            Hooks.OnPlayerHurt += plugin.BaseOnPlayerHurt;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawning:
                            Hooks.OnPlayerSpawning += plugin.BaseOnPlayerSpawn;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawned:
                            Hooks.OnPlayerSpawned += plugin.BaseOnPlayerSpawned;
                            break;
                        case PluginLoaderEvents.OnPlayerGathering:
                            Hooks.OnPlayerGathering += plugin.BaseOnPlayerGathering;
                            break;
                        case PluginLoaderEvents.OnEntityHurt:
                            Hooks.OnEntityHurt += plugin.BaseOnEntityHurt;
                            break;
                        case PluginLoaderEvents.OnEntityDecay:
                            Hooks.OnEntityDecay += plugin.BaseOnEntityDecay;
                            break;
                        case PluginLoaderEvents.OnEntityDestroyed:
                            Hooks.OnEntityDestroyed += plugin.BaseOnEntityDestroyed;
                            break;
                        case PluginLoaderEvents.OnEntityDeployed:
                            Hooks.OnEntityDeployedWithPlacer += plugin.BaseOnEntityDeployed;
                            break;
                        case PluginLoaderEvents.OnNPCHurt:
                            Hooks.OnNPCHurt += plugin.BaseOnNPCHurt;
                            break;
                        case PluginLoaderEvents.OnNPCKilled:
                            Hooks.OnNPCKilled += plugin.BaseOnNPCKilled;
                            break;
                        case PluginLoaderEvents.OnBlueprintUse:
                            Hooks.OnBlueprintUse += plugin.BaseOnBlueprintUse;
                            break;
                        case PluginLoaderEvents.OnDoorUse:
                            Hooks.OnDoorUse += plugin.BaseOnDoorUse;
                            break;
                        case PluginLoaderEvents.OnAllPluginsLoaded:
                            Hooks.OnAllPluginsLoaded += plugin.BaseOnAllPluginsLoaded;
                            break;
                        case PluginLoaderEvents.OnPlayerTeleport:
                            Hooks.OnPlayerTeleport += plugin.BaseOnPlayerTeleport;
                            break;
                        //case PluginEvent.OnPluginInit: plugin.Invoke(PluginEvent.OnPluginInit, new object[0]); break;
                        case PluginLoaderEvents.OnCrafting:
                            Hooks.OnCrafting += plugin.BaseOnCrafting;
                            break;
                        case PluginLoaderEvents.OnResourceSpawn:
                            Hooks.OnResourceSpawned += plugin.BaseOnResourceSpawned;
                            break;
                        case PluginLoaderEvents.OnItemAdded:
                            Hooks.OnItemAdded += plugin.BaseOnItemAdded;
                            break;
                        case PluginLoaderEvents.OnItemRemoved:
                            Hooks.OnItemRemoved += plugin.BaseOnItemRemoved;
                            break;
                        case PluginLoaderEvents.OnAirdrop:
                            Hooks.OnAirdropCalled += plugin.BaseOnAirdrop;
                            break;
                        case PluginLoaderEvents.OnAirdropCrateDropped: 
                            Hooks.OnAirdropCrateDropped += plugin.BaseOnAirdropCrateDropped;
                            break;
                        case PluginLoaderEvents.OnSteamDeny:
                            Hooks.OnSteamDeny += plugin.BaseOnSteamDeny;
                            break;
                        case PluginLoaderEvents.OnPlayerApproval:
                            Hooks.OnPlayerApproval += plugin.BaseOnPlayerApproval;
                            break;
                        case PluginLoaderEvents.OnResearch:
                            Hooks.OnResearch += plugin.BaseOnResearch;
                            break;
                        case PluginLoaderEvents.OnServerSaved:
                            Hooks.OnServerSaved += plugin.BaseOnServerSaved;
                            break;
                        case PluginLoaderEvents.OnVoiceChat:
                            Hooks.OnShowTalker += plugin.BaseOnShowTalker;
                            break;
                        case PluginLoaderEvents.OnItemPickup:
                            Hooks.OnItemPickup += plugin.BaseOnItemPickup;
                            break;
                        case PluginLoaderEvents.OnFallDamage:
                            Hooks.OnFallDamage += plugin.BaseOnFallDamage;
                            break;
                        case PluginLoaderEvents.OnLootUse:
                            Hooks.OnLootUse += plugin.BaseOnLootUse;
                            break;
                        case PluginLoaderEvents.OnPlayerBan:
                            Hooks.OnPlayerBan += plugin.BaseOnBanEvent;
                            break;
                        case PluginLoaderEvents.OnRepairBench:
                            Hooks.OnRepairBench += plugin.BaseOnRepairBench;
                            break;
                        case PluginLoaderEvents.OnItemMove:
                            Hooks.OnItemMove += plugin.BaseOnItemMove;
                            break;
                        case PluginLoaderEvents.OnGenericSpawnLoad:
                            Hooks.OnGenericSpawnerLoad += plugin.BaseOnGenericSpawnLoad;
                            break;
                        case PluginLoaderEvents.OnServerLoaded:
                            Hooks.OnServerLoaded += plugin.BaseOnServerLoaded;
                            break;
                        case PluginLoaderEvents.OnSupplySignalExploded:
                            Hooks.OnSupplySignalExpode += plugin.BaseOnSupplySignalExploded;
                            break;
                        case PluginLoaderEvents.OnPlayerMove:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnPlayerMove += plugin.BaseOnPlayerMove;
                            }
                            break;
                        case PluginLoaderEvents.OnBeltUse:
                            Hooks.OnBeltUse += plugin.BaseOnBeltUse;
                            break;
                        case PluginLoaderEvents.OnLogger:
                            Hooks.OnLogger += plugin.BaseOnLogger;
                            break;
                        case PluginLoaderEvents.OnGrenadeThrow:
                            Hooks.OnGrenadeThrow += plugin.BaseOnGrenade;
                            break;
                        case PluginLoaderEvents.OnSupplyDropPlaneCreated:
                            Hooks.OnSupplyDropPlaneCreated += plugin.BaseOnSupplyDropPlaneCreated;
                            break;
                        case PluginLoaderEvents.OnNPCSpawned:
                            Hooks.OnNPCSpawned += plugin.BaseOnNPCSpawn;
                            break;
                        case PluginLoaderEvents.OnTimedExplosiveSpawned:
                            Hooks.OnTimedExplosiveSpawned += plugin.BaseOnTimedExplosiveSpawned;
                            break;
                        case PluginLoaderEvents.OnSleeperSpawned:
                            Hooks.OnSleeperSpawned += plugin.BaseOnSleeperSpawned;
                            break;
                        case PluginLoaderEvents.OnCommandRestriction:
                            Hooks.OnCommandRestriction += plugin.BaseOnCommandRestriction;
                            break;
                        case PluginLoaderEvents.OnFireBarrelToggle:
                            Hooks.OnFireBarrelToggle += plugin.BaseOnFireBarrelToggle;
                            break;
                        case PluginLoaderEvents.OnDayCycleChanged:
                            Hooks.OnDayCycleChanged += plugin.BaseOnDayCycleChange;
                            break;
                        case PluginLoaderEvents.OnShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShoot += plugin.BaseOnShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnShotgunShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShotgunShoot += plugin.BaseOnShotgunShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnBowShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnBowShoot += plugin.BaseOnBowShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnAnimalMovement:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnAnimalMovement += plugin.BaseOnAnimalMovement;
                            }
                            break;
                        case PluginLoaderEvents.OnConsumableUse:
                            Hooks.OnConsumableUse += plugin.BaseOnConsumableUse;
                            break;
                        case PluginLoaderEvents.OnMedikitUse:
                            Hooks.OnMedikitUse += plugin.BaseOnMedikitUse;
                            break;
                        case PluginLoaderEvents.OnItemModInstall:
                            Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall += plugin.BaseOnItemModInstall;
                            break;
                        case PluginLoaderEvents.OnBloodDraw:
                            Hooks.OnBloodDraw += plugin.BaseOnBloodDraw;
                            break;
                        case PluginLoaderEvents.OnArmorEquip:
                            Hooks.OnArmorEquip += plugin.BaseOnArmorEquip;
                            break;
                        case PluginLoaderEvents.OnArmorUnEquip:
                            Hooks.OnArmorUnEquip += plugin.BaseOnArmorUnEquip;
                            break;
                        case PluginLoaderEvents.OnFlareThrow:
                            Hooks.OnFlareThrow += plugin.BaseOnFlareThrow;
                            break;
                        case PluginLoaderEvents.OnFlareIgnite:
                            Hooks.OnFlareIgnite += plugin.FlareIgnite;
                            break;
                        case PluginLoaderEvents.OnTorchIgnite:
                            Hooks.OnBasicTorchIgnite += plugin.BaseOnTorchIgnite;
                            break;
                        case PluginLoaderEvents.OnHeatZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnHeatZoneEnter += plugin.BaseOnHeatZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnWorkZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnWorkZoneEnter += plugin.BaseOnWorkZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnPluginMessage:
                            Hooks.OnPluginMessage += plugin.BaseOnPluginMessage;
                            break;
                        case PluginLoaderEvents.OnCraftingCancel:
                            Hooks.OnCraftCancel += plugin.BaseOnCraftingCancel;
                            break;
                        case PluginLoaderEvents.OnCraftingComplete:
                            Hooks.OnCraftComplete += plugin.BaseOnCraftingComplete;
                            break;
                        case PluginLoaderEvents.OnServerTick:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnServerTick += plugin.BaseOnServerTick;
                            }
                            break;
                        case PluginLoaderEvents.OnMetabolismUpdate:
                            Hooks.OnMetabolismUpdate += plugin.BaseOnMetabolismUpdate;
                            break;
                        case PluginLoaderEvents.OnWebSocketMessage:
                            Hooks.OnWebSocketMessage += plugin.BaseOnWebSocketMessage;
                            break;
                        case PluginLoaderEvents.OnWebSocketConnected:
                            Hooks.OnWebSocketConnected += plugin.BaseOnWebSocketConnected;
                            break;
                        case PluginLoaderEvents.OnWebSocketClosed:
                            Hooks.OnWebSocketClosed += plugin.BaseOnWebSocketClosed;
                            break;
                        case PluginLoaderEvents.OnWebSocketError:
                            Hooks.OnWebSocketError += plugin.BaseOnWebSocketError;
                            break;
                    }
                }
            }

            if (plugin.Globals.Contains(PluginLoaderEvents.OnPluginInit))
                plugin.Invoke(PluginLoaderEvents.OnPluginInit);
        }

        /// Removes hooks associated with the specified plugin.
        /// This method ensures that all the hooks registered by the plugin are cleaned up
        /// to maintain the integrity of the plugin framework during unload or reload operations.
        /// <param name="plugin">The plugin whose hooks will be removed. It must be in a loaded state for any action to be taken.</param>
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
                        case PluginLoaderEvents.OnServerInit:
                            Hooks.OnServerInit -= plugin.BaseOnServerInit;
                            break;
                        case PluginLoaderEvents.OnServerShutdown:
                            Hooks.OnServerShutdown -= plugin.BaseOnServerShutdown;
                            break;
                        case PluginLoaderEvents.OnItemsLoaded:
                            Hooks.OnItemsLoaded -= plugin.BaseOnItemsLoaded;
                            break;
                        case PluginLoaderEvents.OnTablesLoaded:
                            Hooks.OnTablesLoaded -= plugin.BaseOnTablesLoaded;
                            break;
                        case PluginLoaderEvents.OnChat:
                            Hooks.OnChat -= plugin.BaseOnChat;
                            break;
                        case PluginLoaderEvents.OnConsole:
#pragma warning disable CS0618
                            Hooks.OnConsoleReceived -= plugin.BaseOnConsole;
#pragma warning restore CS0618
                            break;
                        case PluginLoaderEvents.OnConsoleWithCancel:
                            Hooks.OnConsoleReceivedWithCancel -= plugin.BaseOnConsoleWithCancel;
                            break;
                        case PluginLoaderEvents.OnCommand:
                            Hooks.OnCommand -= plugin.BaseOnCommand;
                            break;
                        case PluginLoaderEvents.OnPlayerConnected:
                            Hooks.OnPlayerConnected -= plugin.BaseOnPlayerConnected;
                            break;
                        case PluginLoaderEvents.OnPlayerDisconnected:
                            Hooks.OnPlayerDisconnected -= plugin.BaseOnPlayerDisconnected;
                            break;
                        case PluginLoaderEvents.OnPlayerKilled:
                            Hooks.OnPlayerKilled -= plugin.BaseOnPlayerKilled;
                            break;
                        case PluginLoaderEvents.OnPlayerHurt:
                            Hooks.OnPlayerHurt -= plugin.BaseOnPlayerHurt;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawning:
                            Hooks.OnPlayerSpawning -= plugin.BaseOnPlayerSpawn;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawned:
                            Hooks.OnPlayerSpawned -= plugin.BaseOnPlayerSpawned;
                            break;
                        case PluginLoaderEvents.OnPlayerGathering:
                            Hooks.OnPlayerGathering -= plugin.BaseOnPlayerGathering;
                            break;
                        case PluginLoaderEvents.OnEntityHurt:
                            Hooks.OnEntityHurt -= plugin.BaseOnEntityHurt;
                            break;
                        case PluginLoaderEvents.OnEntityDecay:
                            Hooks.OnEntityDecay -= plugin.BaseOnEntityDecay;
                            break;
                        case PluginLoaderEvents.OnEntityDestroyed:
                            Hooks.OnEntityDestroyed -= plugin.BaseOnEntityDestroyed;
                            break;
                        case PluginLoaderEvents.OnEntityDeployed:
                            Hooks.OnEntityDeployedWithPlacer -= plugin.BaseOnEntityDeployed;
                            break;
                        case PluginLoaderEvents.OnNPCHurt:
                            Hooks.OnNPCHurt -= plugin.BaseOnNPCHurt;
                            break;
                        case PluginLoaderEvents.OnNPCKilled:
                            Hooks.OnNPCKilled -= plugin.BaseOnNPCKilled;
                            break;
                        case PluginLoaderEvents.OnBlueprintUse:
                            Hooks.OnBlueprintUse -= plugin.BaseOnBlueprintUse;
                            break;
                        case PluginLoaderEvents.OnDoorUse:
                            Hooks.OnDoorUse -= plugin.BaseOnDoorUse;
                            break;
                        case PluginLoaderEvents.OnAllPluginsLoaded:
                            Hooks.OnAllPluginsLoaded -= plugin.BaseOnAllPluginsLoaded;
                            break;
                        case PluginLoaderEvents.OnPlayerTeleport:
                            Hooks.OnPlayerTeleport -= plugin.BaseOnPlayerTeleport;
                            break;
                        //case PluginEvent.OnPluginInit: plugin.Invoke(PluginEvent.OnPluginInit, new object[0]); break;
                        case PluginLoaderEvents.OnCrafting:
                            Hooks.OnCrafting -= plugin.BaseOnCrafting;
                            break;
                        case PluginLoaderEvents.OnResourceSpawn:
                            Hooks.OnResourceSpawned -= plugin.BaseOnResourceSpawned;
                            break;
                        case PluginLoaderEvents.OnItemAdded:
                            Hooks.OnItemAdded -= plugin.BaseOnItemAdded;
                            break;
                        case PluginLoaderEvents.OnItemRemoved:
                            Hooks.OnItemRemoved -= plugin.BaseOnItemRemoved;
                            break;
                        case PluginLoaderEvents.OnAirdrop:
                            Hooks.OnAirdropCalled -= plugin.BaseOnAirdrop;
                            break;
                        case PluginLoaderEvents.OnAirdropCrateDropped: 
                            Hooks.OnAirdropCrateDropped -= plugin.BaseOnAirdropCrateDropped;
                            break;
                        case PluginLoaderEvents.OnSteamDeny:
                            Hooks.OnSteamDeny -= plugin.BaseOnSteamDeny;
                            break;
                        case PluginLoaderEvents.OnPlayerApproval:
                            Hooks.OnPlayerApproval -= plugin.BaseOnPlayerApproval;
                            break;
                        case PluginLoaderEvents.OnResearch:
                            Hooks.OnResearch -= plugin.BaseOnResearch;
                            break;
                        case PluginLoaderEvents.OnServerSaved:
                            Hooks.OnServerSaved -= plugin.BaseOnServerSaved;
                            break;
                        case PluginLoaderEvents.OnVoiceChat:
                            Hooks.OnShowTalker -= plugin.BaseOnShowTalker;
                            break;
                        case PluginLoaderEvents.OnItemPickup:
                            Hooks.OnItemPickup -= plugin.BaseOnItemPickup;
                            break;
                        case PluginLoaderEvents.OnFallDamage:
                            Hooks.OnFallDamage -= plugin.BaseOnFallDamage;
                            break;
                        case PluginLoaderEvents.OnLootUse:
                            Hooks.OnLootUse -= plugin.BaseOnLootUse;
                            break;
                        case PluginLoaderEvents.OnPlayerBan:
                            Hooks.OnPlayerBan -= plugin.BaseOnBanEvent;
                            break;
                        case PluginLoaderEvents.OnRepairBench:
                            Hooks.OnRepairBench -= plugin.BaseOnRepairBench;
                            break;
                        case PluginLoaderEvents.OnItemMove:
                            Hooks.OnItemMove -= plugin.BaseOnItemMove;
                            break;
                        case PluginLoaderEvents.OnGenericSpawnLoad:
                            Hooks.OnGenericSpawnerLoad -= plugin.BaseOnGenericSpawnLoad;
                            break;
                        case PluginLoaderEvents.OnServerLoaded:
                            Hooks.OnServerLoaded -= plugin.BaseOnServerLoaded;
                            break;
                        case PluginLoaderEvents.OnSupplySignalExploded:
                            Hooks.OnSupplySignalExpode -= plugin.BaseOnSupplySignalExploded;
                            break;
                        case PluginLoaderEvents.OnPlayerMove:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnPlayerMove -= plugin.BaseOnPlayerMove;
                            }
                            break;
                        case PluginLoaderEvents.OnBeltUse:
                            Hooks.OnBeltUse -= plugin.BaseOnBeltUse;
                            break;
                        case PluginLoaderEvents.OnLogger:
                            Hooks.OnLogger -= plugin.BaseOnLogger;
                            break;
                        case PluginLoaderEvents.OnGrenadeThrow:
                            Hooks.OnGrenadeThrow -= plugin.BaseOnGrenade;
                            break;
                        case PluginLoaderEvents.OnSupplyDropPlaneCreated:
                            Hooks.OnSupplyDropPlaneCreated -= plugin.BaseOnSupplyDropPlaneCreated;
                            break;
                        case PluginLoaderEvents.OnNPCSpawned:
                            Hooks.OnNPCSpawned -= plugin.BaseOnNPCSpawn;
                            break;
                        case PluginLoaderEvents.OnTimedExplosiveSpawned:
                            Hooks.OnTimedExplosiveSpawned -= plugin.BaseOnTimedExplosiveSpawned;
                            break;
                        case PluginLoaderEvents.OnSleeperSpawned:
                            Hooks.OnSleeperSpawned -= plugin.BaseOnSleeperSpawned;
                            break;
                        case PluginLoaderEvents.OnCommandRestriction:
                            Hooks.OnCommandRestriction -= plugin.BaseOnCommandRestriction;
                            break;
                        case PluginLoaderEvents.OnFireBarrelToggle:
                            Hooks.OnFireBarrelToggle -= plugin.BaseOnFireBarrelToggle;
                            break;
                        case PluginLoaderEvents.OnDayCycleChanged:
                            Hooks.OnDayCycleChanged -= plugin.BaseOnDayCycleChange;
                            break;
                        case PluginLoaderEvents.OnShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShoot -= plugin.BaseOnShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnShotgunShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShotgunShoot -= plugin.BaseOnShotgunShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnBowShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnBowShoot -= plugin.BaseOnBowShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnAnimalMovement:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnAnimalMovement -= plugin.BaseOnAnimalMovement;
                            }
                            break;
                        case PluginLoaderEvents.OnConsumableUse:
                            Hooks.OnConsumableUse -= plugin.BaseOnConsumableUse;
                            break;
                        case PluginLoaderEvents.OnMedikitUse:
                            Hooks.OnMedikitUse -= plugin.BaseOnMedikitUse;
                            break;
                        case PluginLoaderEvents.OnItemModInstall:
                            Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall -= plugin.BaseOnItemModInstall;
                            break;
                        case PluginLoaderEvents.OnBloodDraw:
                            Hooks.OnBloodDraw -= plugin.BaseOnBloodDraw;
                            break;
                        case PluginLoaderEvents.OnArmorEquip:
                            Hooks.OnArmorEquip -= plugin.BaseOnArmorEquip;
                            break;
                        case PluginLoaderEvents.OnArmorUnEquip:
                            Hooks.OnArmorUnEquip -= plugin.BaseOnArmorUnEquip;
                            break;
                        case PluginLoaderEvents.OnFlareThrow:
                            Hooks.OnFlareThrow -= plugin.BaseOnFlareThrow;
                            break;
                        case PluginLoaderEvents.OnFlareIgnite:
                            Hooks.OnFlareIgnite -= plugin.FlareIgnite;
                            break;
                        case PluginLoaderEvents.OnTorchIgnite:
                            Hooks.OnBasicTorchIgnite -= plugin.BaseOnTorchIgnite;
                            break;
                        case PluginLoaderEvents.OnHeatZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnHeatZoneEnter -= plugin.BaseOnHeatZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnWorkZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnWorkZoneEnter -= plugin.BaseOnWorkZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnPluginMessage:
                            Hooks.OnPluginMessage -= plugin.BaseOnPluginMessage;
                            break;
                        case PluginLoaderEvents.OnCraftingCancel:
                            Hooks.OnCraftCancel -= plugin.BaseOnCraftingCancel;
                            break;
                        case PluginLoaderEvents.OnCraftingComplete:
                            Hooks.OnCraftComplete -= plugin.BaseOnCraftingComplete;
                            break;
                        case PluginLoaderEvents.OnServerTick:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnServerTick -= plugin.BaseOnServerTick;
                            }
                            break;
                        case PluginLoaderEvents.OnMetabolismUpdate:
                            Hooks.OnMetabolismUpdate -= plugin.BaseOnMetabolismUpdate;
                            break;
                        case PluginLoaderEvents.OnWebSocketMessage:
                            Hooks.OnWebSocketMessage -= plugin.BaseOnWebSocketMessage;
                            break;
                        case PluginLoaderEvents.OnWebSocketConnected:
                            Hooks.OnWebSocketConnected -= plugin.BaseOnWebSocketConnected;
                            break;
                        case PluginLoaderEvents.OnWebSocketClosed:
                            Hooks.OnWebSocketClosed -= plugin.BaseOnWebSocketClosed;
                            break;
                        case PluginLoaderEvents.OnWebSocketError:
                            Hooks.OnWebSocketError -= plugin.BaseOnWebSocketError;
                            break;
                    }
                }

                if (plugin.Globals.Contains(PluginLoaderEvents.OnPluginShutdown))
                    plugin.BaseOnPluginShutdown();
            }
        }

        /// Checks if an intensive event is allowed to be hooked by a plugin.
        /// <param name="plugin">The plugin attempting to hook into the event. Must be an instance of BasePlugin.</param>
        /// <param name="hookName">The name of the event hook that the plugin is trying to access.</param>
        /// <returns>Returns true if the intensive event is allowed for the plugin, otherwise false.</returns>
        private bool IsIntensiveEventAllowed(BasePlugin plugin, string hookName)
        {
            bool allowed = plugin.Type == PluginType.CSharp || plugin.Type == PluginType.CSScript ||
                           Bootstrap.EnableScriptPluginsIntensiveEvents;
            if (!allowed)
            {
                Logger.LogWarning($"[{nameof(PluginLoader)}] {plugin.Name} is trying to hook into {hookName}, which is an intensive event. This is not allowed for {plugin.Type} plugins. To enable this, set EnableScriptPluginsIntensiveEvents to true in the config.");
                Logger.LogWarning("Script plugins using intensive events can cause performance issues, so they are disabled by default, they could perform bad on high player count / bad CPU. Enable the setting if you understand the risks.");
                Logger.LogWarning("The plugin will still load, but the hook will not be registered, so some features may not work as intended.");
            }

            return allowed;
        }
    }
}