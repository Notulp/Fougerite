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
                            Hooks.OnServerInit += plugin.OnServerInit;
                            break;
                        case PluginLoaderEvents.OnServerShutdown:
                            Hooks.OnServerShutdown += plugin.OnServerShutdown;
                            break;
                        case PluginLoaderEvents.OnItemsLoaded:
                            Hooks.OnItemsLoaded += plugin.OnItemsLoaded;
                            break;
                        case PluginLoaderEvents.OnTablesLoaded:
                            Hooks.OnTablesLoaded += plugin.OnTablesLoaded;
                            break;
                        case PluginLoaderEvents.OnChat:
                            Hooks.OnChat += plugin.OnChat;
                            break;
                        case PluginLoaderEvents.OnConsole:
#pragma warning disable CS0618
                            Hooks.OnConsoleReceived += plugin.OnConsole;
#pragma warning restore CS0618
                            break;
                        case PluginLoaderEvents.OnConsoleWithCancel:
                            Hooks.OnConsoleReceivedWithCancel += plugin.OnConsoleWithCancel;
                            break;
                        case PluginLoaderEvents.OnCommand:
                            Hooks.OnCommand += plugin.OnCommand;
                            break;
                        case PluginLoaderEvents.OnPlayerConnected:
                            Hooks.OnPlayerConnected += plugin.OnPlayerConnected;
                            break;
                        case PluginLoaderEvents.OnPlayerDisconnected:
                            Hooks.OnPlayerDisconnected += plugin.OnPlayerDisconnected;
                            break;
                        case PluginLoaderEvents.OnPlayerKilled:
                            Hooks.OnPlayerKilled += plugin.OnPlayerKilled;
                            break;
                        case PluginLoaderEvents.OnPlayerHurt:
                            Hooks.OnPlayerHurt += plugin.OnPlayerHurt;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawning:
                            Hooks.OnPlayerSpawning += plugin.OnPlayerSpawn;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawned:
                            Hooks.OnPlayerSpawned += plugin.OnPlayerSpawned;
                            break;
                        case PluginLoaderEvents.OnPlayerGathering:
                            Hooks.OnPlayerGathering += plugin.OnPlayerGathering;
                            break;
                        case PluginLoaderEvents.OnEntityHurt:
                            Hooks.OnEntityHurt += plugin.OnEntityHurt;
                            break;
                        case PluginLoaderEvents.OnEntityDecay:
                            Hooks.OnEntityDecay += plugin.OnEntityDecay;
                            break;
                        case PluginLoaderEvents.OnEntityDestroyed:
                            Hooks.OnEntityDestroyed += plugin.OnEntityDestroyed;
                            break;
                        case PluginLoaderEvents.OnEntityDeployed:
                            Hooks.OnEntityDeployedWithPlacer += plugin.OnEntityDeployed;
                            break;
                        case PluginLoaderEvents.OnNPCHurt:
                            Hooks.OnNPCHurt += plugin.OnNPCHurt;
                            break;
                        case PluginLoaderEvents.OnNPCKilled:
                            Hooks.OnNPCKilled += plugin.OnNPCKilled;
                            break;
                        case PluginLoaderEvents.OnBlueprintUse:
                            Hooks.OnBlueprintUse += plugin.OnBlueprintUse;
                            break;
                        case PluginLoaderEvents.OnDoorUse:
                            Hooks.OnDoorUse += plugin.OnDoorUse;
                            break;
                        case PluginLoaderEvents.OnAllPluginsLoaded:
                            Hooks.OnAllPluginsLoaded += plugin.OnAllPluginsLoaded;
                            break;
                        case PluginLoaderEvents.OnPlayerTeleport:
                            Hooks.OnPlayerTeleport += plugin.OnPlayerTeleport;
                            break;
                        //case PluginEvent.OnPluginInit: plugin.Invoke(PluginEvent.OnPluginInit, new object[0]); break;
                        case PluginLoaderEvents.OnCrafting:
                            Hooks.OnCrafting += plugin.OnCrafting;
                            break;
                        case PluginLoaderEvents.OnResourceSpawn:
                            Hooks.OnResourceSpawned += plugin.OnResourceSpawned;
                            break;
                        case PluginLoaderEvents.OnItemAdded:
                            Hooks.OnItemAdded += plugin.OnItemAdded;
                            break;
                        case PluginLoaderEvents.OnItemRemoved:
                            Hooks.OnItemRemoved += plugin.OnItemRemoved;
                            break;
                        case PluginLoaderEvents.OnAirdrop:
                            Hooks.OnAirdropCalled += plugin.OnAirdrop;
                            break;
                        case PluginLoaderEvents.OnAirdropCrateDropped: 
                            Hooks.OnAirdropCrateDropped += plugin.OnAirdropCrateDropped;
                            break;
                        case PluginLoaderEvents.OnSteamDeny:
                            Hooks.OnSteamDeny += plugin.OnSteamDeny;
                            break;
                        case PluginLoaderEvents.OnPlayerApproval:
                            Hooks.OnPlayerApproval += plugin.OnPlayerApproval;
                            break;
                        case PluginLoaderEvents.OnResearch:
                            Hooks.OnResearch += plugin.OnResearch;
                            break;
                        case PluginLoaderEvents.OnServerSaved:
                            Hooks.OnServerSaved += plugin.OnServerSaved;
                            break;
                        case PluginLoaderEvents.OnVoiceChat:
                            Hooks.OnShowTalker += plugin.OnShowTalker;
                            break;
                        case PluginLoaderEvents.OnItemPickup:
                            Hooks.OnItemPickup += plugin.OnItemPickup;
                            break;
                        case PluginLoaderEvents.OnFallDamage:
                            Hooks.OnFallDamage += plugin.OnFallDamage;
                            break;
                        case PluginLoaderEvents.OnLootUse:
                            Hooks.OnLootUse += plugin.OnLootUse;
                            break;
                        case PluginLoaderEvents.OnPlayerBan:
                            Hooks.OnPlayerBan += plugin.OnBanEvent;
                            break;
                        case PluginLoaderEvents.OnRepairBench:
                            Hooks.OnRepairBench += plugin.OnRepairBench;
                            break;
                        case PluginLoaderEvents.OnItemMove:
                            Hooks.OnItemMove += plugin.OnItemMove;
                            break;
                        case PluginLoaderEvents.OnGenericSpawnLoad:
                            Hooks.OnGenericSpawnerLoad += plugin.OnGenericSpawnLoad;
                            break;
                        case PluginLoaderEvents.OnServerLoaded:
                            Hooks.OnServerLoaded += plugin.OnServerLoaded;
                            break;
                        case PluginLoaderEvents.OnSupplySignalExploded:
                            Hooks.OnSupplySignalExpode += plugin.OnSupplySignalExploded;
                            break;
                        case PluginLoaderEvents.OnPlayerMove:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnPlayerMove += plugin.OnPlayerMove;
                            }
                            break;
                        case PluginLoaderEvents.OnBeltUse:
                            Hooks.OnBeltUse += plugin.OnBeltUse;
                            break;
                        case PluginLoaderEvents.OnLogger:
                            Hooks.OnLogger += plugin.OnLogger;
                            break;
                        case PluginLoaderEvents.OnGrenadeThrow:
                            Hooks.OnGrenadeThrow += plugin.OnGrenade;
                            break;
                        case PluginLoaderEvents.OnSupplyDropPlaneCreated:
                            Hooks.OnSupplyDropPlaneCreated += plugin.OnSupplyDropPlaneCreated;
                            break;
                        case PluginLoaderEvents.OnNPCSpawned:
                            Hooks.OnNPCSpawned += plugin.OnNPCSpawn;
                            break;
                        case PluginLoaderEvents.OnTimedExplosiveSpawned:
                            Hooks.OnTimedExplosiveSpawned += plugin.OnTimedExplosiveSpawned;
                            break;
                        case PluginLoaderEvents.OnSleeperSpawned:
                            Hooks.OnSleeperSpawned += plugin.OnSleeperSpawned;
                            break;
                        case PluginLoaderEvents.OnCommandRestriction:
                            Hooks.OnCommandRestriction += plugin.OnCommandRestriction;
                            break;
                        case PluginLoaderEvents.OnFireBarrelToggle:
                            Hooks.OnFireBarrelToggle += plugin.OnFireBarrelToggle;
                            break;
                        case PluginLoaderEvents.OnDayCycleChanged:
                            Hooks.OnDayCycleChanged += plugin.OnDayCycleChange;
                            break;
                        case PluginLoaderEvents.OnShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShoot += plugin.OnShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnShotgunShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShotgunShoot += plugin.OnShotgunShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnBowShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnBowShoot += plugin.OnBowShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnAnimalMovement:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnAnimalMovement += plugin.OnAnimalMovement;
                            }
                            break;
                        case PluginLoaderEvents.OnConsumableUse:
                            Hooks.OnConsumableUse += plugin.OnConsumableUse;
                            break;
                        case PluginLoaderEvents.OnMedikitUse:
                            Hooks.OnMedikitUse += plugin.OnMedikitUse;
                            break;
                        case PluginLoaderEvents.OnItemModInstall:
                            Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall += plugin.OnItemModInstall;
                            break;
                        case PluginLoaderEvents.OnBloodDraw:
                            Hooks.OnBloodDraw += plugin.OnBloodDraw;
                            break;
                        case PluginLoaderEvents.OnArmorEquip:
                            Hooks.OnArmorEquip += plugin.OnArmorEquip;
                            break;
                        case PluginLoaderEvents.OnArmorUnEquip:
                            Hooks.OnArmorUnEquip += plugin.OnArmorUnEquip;
                            break;
                        case PluginLoaderEvents.OnFlareThrow:
                            Hooks.OnFlareThrow += plugin.OnFlareThrow;
                            break;
                        case PluginLoaderEvents.OnFlareIgnite:
                            Hooks.OnFlareIgnite += plugin.FlareIgnite;
                            break;
                        case PluginLoaderEvents.OnTorchIgnite:
                            Hooks.OnBasicTorchIgnite += plugin.OnTorchIgnite;
                            break;
                        case PluginLoaderEvents.OnHeatZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnHeatZoneEnter += plugin.OnHeatZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnWorkZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnWorkZoneEnter += plugin.OnWorkZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnPluginMessage:
                            Hooks.OnPluginMessage += plugin.OnPluginMessage;
                            break;
                        case PluginLoaderEvents.OnCraftingCancel:
                            Hooks.OnCraftCancel += plugin.OnCraftingCancel;
                            break;
                        case PluginLoaderEvents.OnCraftingComplete:
                            Hooks.OnCraftComplete += plugin.OnCraftingComplete;
                            break;
                        case PluginLoaderEvents.OnServerTick:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnServerTick += plugin.OnServerTick;
                            }
                            break;
                        case PluginLoaderEvents.OnMetabolismUpdate:
                            Hooks.OnMetabolismUpdate += plugin.OnMetabolismUpdate;
                            break;
                        case PluginLoaderEvents.OnWebSocketMessage:
                            Hooks.OnWebSocketMessage += plugin.OnWebSocketMessage;
                            break;
                        case PluginLoaderEvents.OnWebSocketConnected:
                            Hooks.OnWebSocketConnected += plugin.OnWebSocketConnected;
                            break;
                        case PluginLoaderEvents.OnWebSocketClosed:
                            Hooks.OnWebSocketClosed += plugin.OnWebSocketClosed;
                            break;
                        case PluginLoaderEvents.OnWebSocketError:
                            Hooks.OnWebSocketError += plugin.OnWebSocketError;
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
                            Hooks.OnServerInit -= plugin.OnServerInit;
                            break;
                        case PluginLoaderEvents.OnServerShutdown:
                            Hooks.OnServerShutdown -= plugin.OnServerShutdown;
                            break;
                        case PluginLoaderEvents.OnItemsLoaded:
                            Hooks.OnItemsLoaded -= plugin.OnItemsLoaded;
                            break;
                        case PluginLoaderEvents.OnTablesLoaded:
                            Hooks.OnTablesLoaded -= plugin.OnTablesLoaded;
                            break;
                        case PluginLoaderEvents.OnChat:
                            Hooks.OnChat -= plugin.OnChat;
                            break;
                        case PluginLoaderEvents.OnConsole:
#pragma warning disable CS0618
                            Hooks.OnConsoleReceived -= plugin.OnConsole;
#pragma warning restore CS0618
                            break;
                        case PluginLoaderEvents.OnConsoleWithCancel:
                            Hooks.OnConsoleReceivedWithCancel -= plugin.OnConsoleWithCancel;
                            break;
                        case PluginLoaderEvents.OnCommand:
                            Hooks.OnCommand -= plugin.OnCommand;
                            break;
                        case PluginLoaderEvents.OnPlayerConnected:
                            Hooks.OnPlayerConnected -= plugin.OnPlayerConnected;
                            break;
                        case PluginLoaderEvents.OnPlayerDisconnected:
                            Hooks.OnPlayerDisconnected -= plugin.OnPlayerDisconnected;
                            break;
                        case PluginLoaderEvents.OnPlayerKilled:
                            Hooks.OnPlayerKilled -= plugin.OnPlayerKilled;
                            break;
                        case PluginLoaderEvents.OnPlayerHurt:
                            Hooks.OnPlayerHurt -= plugin.OnPlayerHurt;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawning:
                            Hooks.OnPlayerSpawning -= plugin.OnPlayerSpawn;
                            break;
                        case PluginLoaderEvents.OnPlayerSpawned:
                            Hooks.OnPlayerSpawned -= plugin.OnPlayerSpawned;
                            break;
                        case PluginLoaderEvents.OnPlayerGathering:
                            Hooks.OnPlayerGathering -= plugin.OnPlayerGathering;
                            break;
                        case PluginLoaderEvents.OnEntityHurt:
                            Hooks.OnEntityHurt -= plugin.OnEntityHurt;
                            break;
                        case PluginLoaderEvents.OnEntityDecay:
                            Hooks.OnEntityDecay -= plugin.OnEntityDecay;
                            break;
                        case PluginLoaderEvents.OnEntityDestroyed:
                            Hooks.OnEntityDestroyed -= plugin.OnEntityDestroyed;
                            break;
                        case PluginLoaderEvents.OnEntityDeployed:
                            Hooks.OnEntityDeployedWithPlacer -= plugin.OnEntityDeployed;
                            break;
                        case PluginLoaderEvents.OnNPCHurt:
                            Hooks.OnNPCHurt -= plugin.OnNPCHurt;
                            break;
                        case PluginLoaderEvents.OnNPCKilled:
                            Hooks.OnNPCKilled -= plugin.OnNPCKilled;
                            break;
                        case PluginLoaderEvents.OnBlueprintUse:
                            Hooks.OnBlueprintUse -= plugin.OnBlueprintUse;
                            break;
                        case PluginLoaderEvents.OnDoorUse:
                            Hooks.OnDoorUse -= plugin.OnDoorUse;
                            break;
                        case PluginLoaderEvents.OnAllPluginsLoaded:
                            Hooks.OnAllPluginsLoaded -= plugin.OnAllPluginsLoaded;
                            break;
                        case PluginLoaderEvents.OnPlayerTeleport:
                            Hooks.OnPlayerTeleport -= plugin.OnPlayerTeleport;
                            break;
                        //case PluginEvent.OnPluginInit: plugin.Invoke(PluginEvent.OnPluginInit, new object[0]); break;
                        case PluginLoaderEvents.OnCrafting:
                            Hooks.OnCrafting -= plugin.OnCrafting;
                            break;
                        case PluginLoaderEvents.OnResourceSpawn:
                            Hooks.OnResourceSpawned -= plugin.OnResourceSpawned;
                            break;
                        case PluginLoaderEvents.OnItemAdded:
                            Hooks.OnItemAdded -= plugin.OnItemAdded;
                            break;
                        case PluginLoaderEvents.OnItemRemoved:
                            Hooks.OnItemRemoved -= plugin.OnItemRemoved;
                            break;
                        case PluginLoaderEvents.OnAirdrop:
                            Hooks.OnAirdropCalled -= plugin.OnAirdrop;
                            break;
                        case PluginLoaderEvents.OnAirdropCrateDropped: 
                            Hooks.OnAirdropCrateDropped -= plugin.OnAirdropCrateDropped;
                            break;
                        case PluginLoaderEvents.OnSteamDeny:
                            Hooks.OnSteamDeny -= plugin.OnSteamDeny;
                            break;
                        case PluginLoaderEvents.OnPlayerApproval:
                            Hooks.OnPlayerApproval -= plugin.OnPlayerApproval;
                            break;
                        case PluginLoaderEvents.OnResearch:
                            Hooks.OnResearch -= plugin.OnResearch;
                            break;
                        case PluginLoaderEvents.OnServerSaved:
                            Hooks.OnServerSaved -= plugin.OnServerSaved;
                            break;
                        case PluginLoaderEvents.OnVoiceChat:
                            Hooks.OnShowTalker -= plugin.OnShowTalker;
                            break;
                        case PluginLoaderEvents.OnItemPickup:
                            Hooks.OnItemPickup -= plugin.OnItemPickup;
                            break;
                        case PluginLoaderEvents.OnFallDamage:
                            Hooks.OnFallDamage -= plugin.OnFallDamage;
                            break;
                        case PluginLoaderEvents.OnLootUse:
                            Hooks.OnLootUse -= plugin.OnLootUse;
                            break;
                        case PluginLoaderEvents.OnPlayerBan:
                            Hooks.OnPlayerBan -= plugin.OnBanEvent;
                            break;
                        case PluginLoaderEvents.OnRepairBench:
                            Hooks.OnRepairBench -= plugin.OnRepairBench;
                            break;
                        case PluginLoaderEvents.OnItemMove:
                            Hooks.OnItemMove -= plugin.OnItemMove;
                            break;
                        case PluginLoaderEvents.OnGenericSpawnLoad:
                            Hooks.OnGenericSpawnerLoad -= plugin.OnGenericSpawnLoad;
                            break;
                        case PluginLoaderEvents.OnServerLoaded:
                            Hooks.OnServerLoaded -= plugin.OnServerLoaded;
                            break;
                        case PluginLoaderEvents.OnSupplySignalExploded:
                            Hooks.OnSupplySignalExpode -= plugin.OnSupplySignalExploded;
                            break;
                        case PluginLoaderEvents.OnPlayerMove:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnPlayerMove -= plugin.OnPlayerMove;
                            }
                            break;
                        case PluginLoaderEvents.OnBeltUse:
                            Hooks.OnBeltUse -= plugin.OnBeltUse;
                            break;
                        case PluginLoaderEvents.OnLogger:
                            Hooks.OnLogger -= plugin.OnLogger;
                            break;
                        case PluginLoaderEvents.OnGrenadeThrow:
                            Hooks.OnGrenadeThrow -= plugin.OnGrenade;
                            break;
                        case PluginLoaderEvents.OnSupplyDropPlaneCreated:
                            Hooks.OnSupplyDropPlaneCreated -= plugin.OnSupplyDropPlaneCreated;
                            break;
                        case PluginLoaderEvents.OnNPCSpawned:
                            Hooks.OnNPCSpawned -= plugin.OnNPCSpawn;
                            break;
                        case PluginLoaderEvents.OnTimedExplosiveSpawned:
                            Hooks.OnTimedExplosiveSpawned -= plugin.OnTimedExplosiveSpawned;
                            break;
                        case PluginLoaderEvents.OnSleeperSpawned:
                            Hooks.OnSleeperSpawned -= plugin.OnSleeperSpawned;
                            break;
                        case PluginLoaderEvents.OnCommandRestriction:
                            Hooks.OnCommandRestriction -= plugin.OnCommandRestriction;
                            break;
                        case PluginLoaderEvents.OnFireBarrelToggle:
                            Hooks.OnFireBarrelToggle -= plugin.OnFireBarrelToggle;
                            break;
                        case PluginLoaderEvents.OnDayCycleChanged:
                            Hooks.OnDayCycleChanged -= plugin.OnDayCycleChange;
                            break;
                        case PluginLoaderEvents.OnShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShoot -= plugin.OnShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnShotgunShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnShotgunShoot -= plugin.OnShotgunShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnBowShoot:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnBowShoot -= plugin.OnBowShoot;
                            }
                            break;
                        case PluginLoaderEvents.OnAnimalMovement:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnAnimalMovement -= plugin.OnAnimalMovement;
                            }
                            break;
                        case PluginLoaderEvents.OnConsumableUse:
                            Hooks.OnConsumableUse -= plugin.OnConsumableUse;
                            break;
                        case PluginLoaderEvents.OnMedikitUse:
                            Hooks.OnMedikitUse -= plugin.OnMedikitUse;
                            break;
                        case PluginLoaderEvents.OnItemModInstall:
                            Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall -= plugin.OnItemModInstall;
                            break;
                        case PluginLoaderEvents.OnBloodDraw:
                            Hooks.OnBloodDraw -= plugin.OnBloodDraw;
                            break;
                        case PluginLoaderEvents.OnArmorEquip:
                            Hooks.OnArmorEquip -= plugin.OnArmorEquip;
                            break;
                        case PluginLoaderEvents.OnArmorUnEquip:
                            Hooks.OnArmorUnEquip -= plugin.OnArmorUnEquip;
                            break;
                        case PluginLoaderEvents.OnFlareThrow:
                            Hooks.OnFlareThrow -= plugin.OnFlareThrow;
                            break;
                        case PluginLoaderEvents.OnFlareIgnite:
                            Hooks.OnFlareIgnite -= plugin.FlareIgnite;
                            break;
                        case PluginLoaderEvents.OnTorchIgnite:
                            Hooks.OnBasicTorchIgnite -= plugin.OnTorchIgnite;
                            break;
                        case PluginLoaderEvents.OnHeatZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnHeatZoneEnter -= plugin.OnHeatZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnWorkZoneEnter:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnWorkZoneEnter -= plugin.OnWorkZoneEnter;
                            }
                            break;
                        case PluginLoaderEvents.OnPluginMessage:
                            Hooks.OnPluginMessage -= plugin.OnPluginMessage;
                            break;
                        case PluginLoaderEvents.OnCraftingCancel:
                            Hooks.OnCraftCancel -= plugin.OnCraftingCancel;
                            break;
                        case PluginLoaderEvents.OnCraftingComplete:
                            Hooks.OnCraftComplete -= plugin.OnCraftingComplete;
                            break;
                        case PluginLoaderEvents.OnServerTick:
                            if (IsIntensiveEventAllowed(plugin, method))
                            {
                                Hooks.OnServerTick -= plugin.OnServerTick;
                            }
                            break;
                        case PluginLoaderEvents.OnMetabolismUpdate:
                            Hooks.OnMetabolismUpdate -= plugin.OnMetabolismUpdate;
                            break;
                        case PluginLoaderEvents.OnWebSocketMessage:
                            Hooks.OnWebSocketMessage -= plugin.OnWebSocketMessage;
                            break;
                        case PluginLoaderEvents.OnWebSocketConnected:
                            Hooks.OnWebSocketConnected -= plugin.OnWebSocketConnected;
                            break;
                        case PluginLoaderEvents.OnWebSocketClosed:
                            Hooks.OnWebSocketClosed -= plugin.OnWebSocketClosed;
                            break;
                        case PluginLoaderEvents.OnWebSocketError:
                            Hooks.OnWebSocketError -= plugin.OnWebSocketError;
                            break;
                    }
                }

                if (plugin.Globals.Contains(PluginLoaderEvents.OnPluginShutdown))
                    plugin.OnPluginShutdown();
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