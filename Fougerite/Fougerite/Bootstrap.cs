using System;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using Fougerite.Caches;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;

namespace Fougerite
{
    public class Bootstrap : Facepunch.MonoBehaviour
    {
        /// <summary>
        /// Returns the Current Fougerite Version
        /// </summary>
        public const string Version = "1.8.2";
        /// <summary>
        /// This value decides wheather we should remove the player classes from the cache upon disconnect.
        /// </summary>
        public static bool CR = false;
        /// <summary>
        /// This value decides wheater we should ban a player for sending invalid packets.
        /// </summary>
        public static bool BI = false;
        /// <summary>
        /// This value decides wheather we should ban a player for Craft hacking.
        /// </summary>
        public static bool AutoBanCraft = true;
        /// <summary>
        /// This value decides wheather we should enable the default rust decay.
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
        public static bool RPCChat = false;
        /// <summary>
        /// Specify the client side's RPC method.
        /// </summary>
        public static string RPCChatMethod = "FougeriteChatSystem";
        
        internal static readonly Thread CurrentThread = Thread.CurrentThread;
        private static readonly FileSystemWatcher IgnoredWatcher = new FileSystemWatcher(Path.Combine(Util.GetRootFolder(), "Save"), "IgnoredPlugins.txt");
        private static GameObject _timergo;

        public static void AttachBootstrap()
        {
            try
            {
                var type = typeof(Bootstrap);
                new GameObject(type.FullName).AddComponent(type);
                Debug.Log(string.Format("<><[ Fougerite v{0} ]><>", Version));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.Log("Error while loading Fougerite!");
            }
        }

        public void Awake()
        {
            UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
        }

        public bool ApplyOptions()
        {
            // look for the string 'false' to disable.  **not a bool check**
            if (Fougerite.Config.GetValue("Fougerite", "enabled") == "false") 
            {
                Debug.Log("Fougerite is disabled. No modules loaded. No hooks called.");
                return false;
            }
            if (Fougerite.Config.GetValue("Fougerite", "RemovePlayersFromCache") != null)
            {
                CR = Fougerite.Config.GetBoolValue("Fougerite", "RemovePlayersFromCache");
            }
            if (Fougerite.Config.GetValue("Fougerite", "BanOnInvalidPacket") != null)
            {
                BI = Fougerite.Config.GetBoolValue("Fougerite", "BanOnInvalidPacket");
            }
            if (Fougerite.Config.GetValue("Fougerite", "AutoBanCraft") != null)
            {
                AutoBanCraft = Fougerite.Config.GetBoolValue("Fougerite", "AutoBanCraft");
            }
            if (Fougerite.Config.GetValue("Fougerite", "SaveNotification") != null)
            {
                SaveNotification = Fougerite.Config.GetValue("Fougerite", "SaveNotification");
            }
            if (Fougerite.Config.GetValue("Fougerite", "RustChat") != null)
            {
                RustChat = Fougerite.Config.GetBoolValue("Fougerite", "RustChat");
            }
            if (Fougerite.Config.GetValue("Fougerite", "RPCChat") != null)
            {
                RPCChat = Fougerite.Config.GetBoolValue("Fougerite", "RPCChat");
            }
            if (Fougerite.Config.GetValue("Fougerite", "RPCChatMethod") != null)
            {
                RPCChatMethod = Fougerite.Config.GetValue("Fougerite", "RPCChatMethod");
            }

            if (!RustChat)
            {
                Logger.LogWarning("[RustChat] The default Rust Chat is disabled for the Player.Message methods.");
            }
            
            if (Fougerite.Config.GetValue("Fougerite", "FloodConnections") != null)
            {
                int v;
                int.TryParse(Fougerite.Config.GetValue("Fougerite", "FloodConnections"), out v);
                if (v <= 0)
                {
                    v = 2;
                }
                FloodConnections = v + 1;
            }
            if (Fougerite.Config.GetValue("Fougerite", "SaveTime") != null)
            {
                int v;
                int.TryParse(Fougerite.Config.GetValue("Fougerite", "SaveTime"), out v);
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
            if (Fougerite.Config.GetValue("Fougerite", "SaveCopies") != null)
            {
                int v;
                int.TryParse(Fougerite.Config.GetValue("Fougerite", "SaveCopies"), out v);
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
            if (Fougerite.Config.GetValue("Fougerite", "StopServerOnSaveFail") != null)
            {
                bool v = false;
                bool.TryParse(Fougerite.Config.GetValue("Fougerite", "StopServerOnSaveFail"), out v);
                ServerSaveHandler.StopServerOnSaveFail = v;
            }
            else
            {
                ServerSaveHandler.StopServerOnSaveFail = false;
            }
            if (Fougerite.Config.GetValue("Fougerite", "CrucialSavePoint") != null)
            {
                int v = 2;
                int.TryParse(Fougerite.Config.GetValue("Fougerite", "CrucialSavePoint"), out v);
                ServerSaveHandler.CrucialSavePoint = v;
            }
            else
            {
                ServerSaveHandler.CrucialSavePoint = 2;
            }

            if (!File.Exists(Util.GetRootFolder() + "\\Save\\IgnoredPlugins.txt"))
            {
                File.Create(Util.GetRootFolder() + "\\Save\\IgnoredPlugins.txt").Dispose();
            }


            string[] lines = File.ReadAllLines(Util.GetRootFolder() + "\\Save\\IgnoredPlugins.txt");
            foreach (var x in lines)
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
            
            if (!Fougerite.Config.GetBoolValue("Fougerite", "deployabledecay") && !Fougerite.Config.GetBoolValue("Fougerite", "decay"))
            {
                decay.decaytickrate = float.MaxValue / 2;
                decay.deploy_maxhealth_sec = float.MaxValue;
                decay.maxperframe = -1;
                decay.maxtestperframe = -1;
            }
            if (!Fougerite.Config.GetBoolValue("Fougerite", "structuredecay") && !Fougerite.Config.GetBoolValue("Fougerite", "decay"))
            {
                structure.maxframeattempt = -1;
                structure.framelimit = -1;
                structure.minpercentdmg = float.MaxValue;
            }
            if (Fougerite.Config.GetValue("Fougerite", "EnableDefaultRustDecay") != null)
            {
                EnableDefaultRustDecay = Fougerite.Config.GetBoolValue("Fougerite", "EnableDefaultRustDecay");
            }
            else
            {
                NetCull.Callbacks.beforeEveryUpdate += new NetCull.UpdateFunctor(EnvDecay.Callbacks.RunDecayThink);
                Logger.LogWarning("[RustDecay] The default Rust Decay is enabled. (Config option not found)");
            }
            if (EnableDefaultRustDecay)
            {
                NetCull.Callbacks.beforeEveryUpdate += new NetCull.UpdateFunctor(EnvDecay.Callbacks.RunDecayThink);
                Logger.LogWarning("[RustDecay] The default Rust Decay is enabled.");
            }
            else
            {
                Logger.LogWarning("[RustDecay] The default Rust Decay is disabled.");
            }
            return true;
        }

        private void OnIgnoredChanged(object sender, FileSystemEventArgs e)
        {
            IgnoredPlugins.Clear();
            string[] lines = File.ReadAllLines(Util.GetRootFolder() + "\\Save\\IgnoredPlugins.txt");
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

        public void Start()
        {
            string FougeriteDirectoryConfig = Path.Combine(Util.GetServerFolder(), "FougeriteDirectory.cfg");
            
            // Init Configs
            Config.Init(FougeriteDirectoryConfig);
            
            // Init Logger
            Logger.Init();
            
            // Init CTimer
            _timergo = new GameObject();
            _timergo.AddComponent<CTimerHandler>();
            UnityEngine.Object.DontDestroyOnLoad(_timergo);
            CTimer.StartWatching();
            
            // Initialize sqlite
            SQLiteConnector.GetInstance.Setup();
            
            // Load default permissions API.
            PermissionSystem.GetPermissionSystem();
            
            // Load Player Cache
            PlayerCache.GetPlayerCache().LoadPlayersCache();

            Rust.Steam.Server.SetModded();
            Rust.Steam.Server.Official = false;

            if (ApplyOptions()) 
            {
                //ModuleManager.LoadModules();
                CSharpPluginLoader.GetInstance();
                PythonPluginLoader.GetInstance();
                JavaScriptPluginLoader.GetInstance();
                LuaPluginLoader.GetInstance();
                Fougerite.Hooks.ServerStarted();
                Fougerite.ShutdownCatcher.Hook();
            }
        }
    }
}
