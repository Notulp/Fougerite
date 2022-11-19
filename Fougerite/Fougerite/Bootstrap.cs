using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Fougerite.Caches;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;
using UnityEngine;
using MonoBehaviour = Facepunch.MonoBehaviour;

namespace Fougerite
{
    public class Bootstrap : MonoBehaviour
    {
        /// <summary>
        /// Returns the Current Fougerite Version
        /// </summary>
        public const string Version = "1.8.3";
        /// <summary>
        /// This value decides whether we should remove the player classes from the cache upon disconnect.
        /// </summary>
        public static bool CR;
        /// <summary>
        /// This value decides wheter we should ban a player for sending invalid packets.
        /// </summary>
        public static bool BI;
        /// <summary>
        /// This value decides whether we should ban a player for Craft hacking.
        /// </summary>
        public static bool AutoBanCraft = true;
        /// <summary>
        /// This value decides whether we should enable the default rust decay.
        /// </summary>
        public static bool EnableDefaultRustDecay = true;
        /// <summary>
        /// This value decides how many connections can be made from the same ip per seconds.
        /// </summary>
        public static int FloodConnections = 3;
        /// <summary>
        /// Contains the ignored plugin names.
        /// </summary>
        public static readonly List<string> IgnoredPlugins = new List<string>();
        /// <summary>
        /// Text to display to the player when the server is saving, and the building parts cannot be placed due the subthread.
        /// </summary>
        public static string SaveNotification = "The server is currently saving! You have to wait before placing an object.";
        /// <summary>
        /// Enable the default ChatSystem output for the Player.Message methods?
        /// </summary>
        public static bool RustChat = true;
        /// <summary>
        /// Send additional RPCPackets of the chat for the clients? (This is recommended for RustBuster Servers only.)
        /// </summary>
        public static bool RPCChat;
        /// <summary>
        /// Specify the client side's RPC method.
        /// </summary>
        public static string RPCChatMethod = "FougeriteChatSystem";
        
        internal static readonly Thread CurrentThread = Thread.CurrentThread;
        private static readonly FileSystemWatcher IgnoredWatcher = new FileSystemWatcher(Path.Combine(Util.GetRootFolder(), "Save"), "IgnoredPlugins.txt");
        private static GameObject _timergo;

        /// <summary>
        /// Called by a patched function.
        /// Fougerite initializes here.
        /// </summary>
        public static void AttachBootstrap()
        {
            try
            {
                Type type = typeof(Bootstrap);
                new GameObject(type.FullName).AddComponent(type);
                Debug.Log($"<><[ Fougerite v{Version} ]><>");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.Log("Error while loading Fougerite!");
            }
        }

        /// <summary>
        /// MonoBehaviour Awake().
        /// </summary>
        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Applies options from the Fougerite.cfg
        /// </summary>
        /// <returns></returns>
        public bool ApplyOptions()
        {
            // look for the string 'false' to disable.  **not a bool check**
            if (Config.GetValue("Fougerite", "enabled") == "false") 
            {
                Debug.Log("Fougerite is disabled. No modules loaded. No hooks called.");
                return false;
            }
            if (Config.GetValue("Fougerite", "RemovePlayersFromCache") != null)
            {
                CR = Config.GetBoolValue("Fougerite", "RemovePlayersFromCache");
            }
            if (Config.GetValue("Fougerite", "BanOnInvalidPacket") != null)
            {
                BI = Config.GetBoolValue("Fougerite", "BanOnInvalidPacket");
            }
            if (Config.GetValue("Fougerite", "AutoBanCraft") != null)
            {
                AutoBanCraft = Config.GetBoolValue("Fougerite", "AutoBanCraft");
            }
            if (Config.GetValue("Fougerite", "SaveNotification") != null)
            {
                SaveNotification = Config.GetValue("Fougerite", "SaveNotification");
            }
            if (Config.GetValue("Fougerite", "RustChat") != null)
            {
                RustChat = Config.GetBoolValue("Fougerite", "RustChat");
            }
            if (Config.GetValue("Fougerite", "RPCChat") != null)
            {
                RPCChat = Config.GetBoolValue("Fougerite", "RPCChat");
            }
            if (Config.GetValue("Fougerite", "RPCChatMethod") != null)
            {
                RPCChatMethod = Config.GetValue("Fougerite", "RPCChatMethod");
            }

            if (!RustChat)
            {
                Logger.LogWarning("[RustChat] The default Rust Chat is disabled for the Player.Message methods.");
            }
            
            if (Config.GetValue("Fougerite", "FloodConnections") != null)
            {
                int v;
                int.TryParse(Config.GetValue("Fougerite", "FloodConnections"), out v);
                if (v <= 0)
                {
                    v = 2;
                }
                FloodConnections = v + 1;
            }
            if (Config.GetValue("Fougerite", "SaveTime") != null)
            {
                int v;
                int.TryParse(Config.GetValue("Fougerite", "SaveTime"), out v);
                if (v <= 0)
                {
                    v = 10;
                }
                ServerSaveHandler.ServerSaveTime = v;
            }
            else
            {
                ServerSaveHandler.ServerSaveTime = 10;
            }
            if (Config.GetValue("Fougerite", "SaveCopies") != null)
            {
                int v;
                int.TryParse(Config.GetValue("Fougerite", "SaveCopies"), out v);
                if (v <= 4)
                {
                    v = 5;
                }
                ServerSaveHandler.SaveCopies = v;
            }
            else
            {
                ServerSaveHandler.SaveCopies = 5;
            }
            if (Config.GetValue("Fougerite", "StopServerOnSaveFail") != null)
            {
                bool v = false;
                bool.TryParse(Config.GetValue("Fougerite", "StopServerOnSaveFail"), out v);
                ServerSaveHandler.StopServerOnSaveFail = v;
            }
            else
            {
                ServerSaveHandler.StopServerOnSaveFail = false;
            }
            if (Config.GetValue("Fougerite", "CrucialSavePoint") != null)
            {
                int v = 2;
                int.TryParse(Config.GetValue("Fougerite", "CrucialSavePoint"), out v);
                ServerSaveHandler.CrucialSavePoint = v;
            }
            else
            {
                ServerSaveHandler.CrucialSavePoint = 2;
            }

            string ignoredPluginsPath = Path.Combine(Util.GetRootFolder(), "\\Save\\IgnoredPlugins.txt");
            if (!File.Exists(ignoredPluginsPath))
            {
                File.Create(ignoredPluginsPath).Dispose();
            }

            string[] lines = File.ReadAllLines(ignoredPluginsPath);
            foreach (string x in lines)
            {
                if (!x.StartsWith(";"))
                {
                    IgnoredPlugins.Add(x.ToLower());
                }
            }
            
            IgnoredWatcher.EnableRaisingEvents = true;
            IgnoredWatcher.Changed += OnIgnoredChanged;

            // Remove the default rust saving methods.
            save.autosavetime = int.MaxValue;
            
            if (!Config.GetBoolValue("Fougerite", "deployabledecay") && !Config.GetBoolValue("Fougerite", "decay"))
            {
                decay.decaytickrate = float.MaxValue / 2;
                decay.deploy_maxhealth_sec = float.MaxValue;
                decay.maxperframe = -1;
                decay.maxtestperframe = -1;
            }
            if (!Config.GetBoolValue("Fougerite", "structuredecay") && !Config.GetBoolValue("Fougerite", "decay"))
            {
                structure.maxframeattempt = -1;
                structure.framelimit = -1;
                structure.minpercentdmg = float.MaxValue;
            }
            if (Config.GetValue("Fougerite", "EnableDefaultRustDecay") != null)
            {
                EnableDefaultRustDecay = Config.GetBoolValue("Fougerite", "EnableDefaultRustDecay");
            }
            else
            {
                NetCull.Callbacks.beforeEveryUpdate += EnvDecay.Callbacks.RunDecayThink;
                Logger.LogWarning("[RustDecay] The default Rust Decay is enabled. (Config option not found)");
            }
            if (EnableDefaultRustDecay)
            {
                NetCull.Callbacks.beforeEveryUpdate += EnvDecay.Callbacks.RunDecayThink;
                Logger.LogWarning("[RustDecay] The default Rust Decay is enabled.");
            }
            else
            {
                Logger.LogWarning("[RustDecay] The default Rust Decay is disabled.");
            }
            return true;
        }

        /// <summary>
        /// Handles IgnoredPlugins.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnIgnoredChanged(object sender, FileSystemEventArgs e)
        {
            IgnoredPlugins.Clear();
            string[] lines = File.ReadAllLines(Path.Combine(Util.GetRootFolder(), "\\Save\\IgnoredPlugins.txt"));
            foreach (var x in lines)
            {
                if (!x.StartsWith(";"))
                {
                    IgnoredPlugins.Add(x.ToLower());
                }
            }
            Loom.QueueOnMainThread(() => {
                Logger.Log("[IgnoredPluginsWatcher] Detected IgnoredPlugins change, reloaded list. ");
            });
        }

        /// <summary>
        /// Runs when the MonoBehaviour is starting.
        /// </summary>
        public void Start()
        {
            string FougeriteDirectoryConfig = Path.Combine(Util.GetServerFolder(), "FougeriteDirectory.cfg");
            
            // Init Configs
            Config.Init(FougeriteDirectoryConfig);
            
            // Init Logger
            Logger.Init();

            // Attempt to log unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            // Init CTimer
            _timergo = new GameObject();
            _timergo.AddComponent<CTimerHandler>();
            DontDestroyOnLoad(_timergo);
            CTimer.StartWatching();
            
            // Initialize sqlite
            SQLiteConnector.GetInstance.Setup();
            
            // Load default permissions API.
            PermissionSystem.GetPermissionSystem();
            
            // Load Player Cache
            PlayerCache.GetPlayerCache().LoadPlayersCache();
            
            // Init Entity Cache
            EntityCache.GetInstance();

            Rust.Steam.Server.SetModded();
            Rust.Steam.Server.Official = false;

            if (ApplyOptions()) 
            {
                //ModuleManager.LoadModules();
                CSharpPluginLoader.GetInstance();
                PythonPluginLoader.GetInstance();
                JavaScriptPluginLoader.GetInstance();
                LuaPluginLoader.GetInstance();
                Hooks.ServerStarted();
                ShutdownCatcher.Hook();
            }
        }

        /// <summary>
        /// Logs all unhandled exceptions.
        /// Unity handles this event differently via Mono, but It may catch informative errors.
        /// This would work for sub threads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.LogError($"[UnHandledException] Error: {ex}");
            }
        }
    }
}
