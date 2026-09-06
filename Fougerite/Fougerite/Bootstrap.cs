using System;
using System.IO;
using System.Threading;
using Fougerite.Caches;
using Fougerite.Concurrent;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;
using Fougerite.Tools;
using Newtonsoft.Json;
using UnityEngine;
using MonoBehaviour = Facepunch.MonoBehaviour;

namespace Fougerite
{
    public class Bootstrap : MonoBehaviour
    {
        /// <summary>
        /// Returns the Current Fougerite Version
        /// </summary>
        public const string Version = "1.9.92";
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
        public static readonly ConcurrentList<string> IgnoredPlugins = new ConcurrentList<string>();
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
        /// <summary>
        /// Enable intensive events for script plugins (Py, Lua, JS)
        /// This gives scripts access to events like OnPlayerMove, OnShoot, OnShotgunShoot, OnBowShoot, OnAnimalMovement,
        /// OnHeatZoneEnter, OnWorkZoneEnter.
        /// Use this carefully, as these events are called very often and may cause performance issues (server laggs).
        /// It is recommended to use C# plugins for these events instead.
        /// Script plugins are generally slower than C# plugins. Python is the fastest among script plugins.
        /// Use at your own risk.
        /// </summary>
        public static bool EnableScriptPluginsIntensiveEvents;
        /// <summary>
        /// Suppress the default "Fougerite: Class.Function was executed!" response 
        /// when a console command doesn't explicitly specify a reply text?
        /// </summary>
        public static bool SilentConsoleCommands;
        /// <summary>
        /// Specifies the name of the message displayed by the server for system notifications.
        /// This value is typically configurable and determines the title or identifier of server broadcast messages.
        /// </summary>
        public static string ServerMessageName;
        
        internal static readonly Thread CurrentThread = Thread.CurrentThread;
        private static readonly FileSystemWatcher IgnoredWatcher = new FileSystemWatcher(Path.Combine(Util.GetRootFolder(), "Save"), "IgnoredPlugins.txt");

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
            if (Config.GetValue("Fougerite", "ClientFunction") != null)
            {
                RPCChatMethod = Config.GetValue("Fougerite", "ClientFunction");
            }
            if (Config.GetValue("Fougerite", "EnableScriptPluginsIntensiveEvents") != null)
            {
                EnableScriptPluginsIntensiveEvents = Config.GetBoolValue("Fougerite", "EnableScriptPluginsIntensiveEvents");
            }
            if (Config.GetValue("Fougerite", "SilentConsoleCommands") != null)
            {
                SilentConsoleCommands = Config.GetBoolValue("Fougerite", "SilentConsoleCommands");
            }
            if (Config.GetValue("Fougerite", "ServerMessageName") != null)
            {
                ServerMessageName = Config.GetValue("Fougerite", "ServerMessageName");
                Server.GetServer().server_message_name = ServerMessageName;
            }

            if (!RustChat)
            {
                Logger.LogWarning("[RustChat] The default Rust Chat is disabled for the Player.Message methods.");
            }

            if (SilentConsoleCommands)
            {
                Logger.LogWarning("[SilentConsoleCommands] The default console command response is disabled for commands that don't explicitly specify a reply text.");
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

            string ignoredPluginsPath = Util.GetRootFolder().Combine("\\Save\\IgnoredPlugins.txt");
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
                NetCull.Callbacks.beforeEveryUpdate += new NetCull.UpdateFunctor(StructureMaster.Callbacks.RunDecayThink);
                Logger.LogWarning("[RustDecay] The default Rust Decay is enabled. (Config option not found)");
            }
            if (EnableDefaultRustDecay)
            {
                NetCull.Callbacks.beforeEveryUpdate += EnvDecay.Callbacks.RunDecayThink;
                NetCull.Callbacks.beforeEveryUpdate += new NetCull.UpdateFunctor(StructureMaster.Callbacks.RunDecayThink);
                Logger.LogWarning("[RustDecay] The default Rust Decay is enabled.");
            }
            else
            {
                Logger.LogWarning("[RustDecay] The default Rust Decay is disabled.");
            }
            
            var combinedDump = new
            {
                fougerite = new
                {
                    RemovePlayersFromCache = CR,
                    BanOnInvalidPacket = BI,
                    AutoBanCraft = AutoBanCraft,
                    FloodConnections = FloodConnections - 1,
                    SaveTime = ServerSaveHandler.ServerSaveTime,
                    SaveCopies = ServerSaveHandler.SaveCopies,
                    StopServerOnSaveFail = ServerSaveHandler.StopServerOnSaveFail,
                    CrucialSavePoint = ServerSaveHandler.CrucialSavePoint,
                    EnableScriptPluginsIntensiveEvents = EnableScriptPluginsIntensiveEvents,
                    IgnoredPluginsCount = IgnoredPlugins.Count,
                    SilentConsoleCommands = SilentConsoleCommands,
                    RustChat = RustChat,
                    RPCChat = RPCChat,
                    RPCChatMethod = RPCChatMethod,
                    EnableDefaultRustDecay = EnableDefaultRustDecay,
                    ServerMessageName = ServerMessageName
                },
                decay = new { decay.deploy_maxhealth_sec, decay.decaytickrate, decay.maxperframe, decay.maxtestperframe },
                structure = new { structure.minpercentdmg, structure.framelimit, structure.maxframeattempt },
                save = new { save.friendly, save.autosavetime, save.profile },
                chat = new { chat.enabled, chat.serverlog },
                airdrop = new { airdrop.min_players },
                dmg = new { dmg.godadmins },
                env = new { env.daylength, env.nightlength },
                falldamage = new { falldamage.min_vel, falldamage.max_vel, falldamage.enabled, falldamage.injury_length },
                footsteps = new { footsteps.quality },
                gametip = new { gametip.scale },
                global = new { global.logprint, global.fpslog },
                gunshots = new { gunshots.aiscared },
                interp = new { interp.ratio, interp.delayms },
                inv = new { inv.loglevel, inv.clientupdates },
                netcull = new { netcull.log },
                packet = new { packet.loglevel, packet.dropclockthresh, packet.verify, packet.dropms, packet.dropsec },
                player = new {
                    backpackLockTime = Util.GetUtil().GetStaticField("player", "backpackLockTime")
                },
                server = new 
                { 
                    server.framerate, 
                    server.clienttimeout, 
                    server.hostname, 
                    server.maxplayers, 
                    server.port, 
                    server.pvp, 
                    server.map, 
                    server.datadir,
                    server.sendrate,
                    server.lan,
                    server.ip,
                    server.timesrc,
                    server.sendbuffer,
                    server.receivebuffer,
                    server.log,
                    server.steamgroup
                },
                sleepers = new { sleepers.loglevel, sleepers.pointsolver, sleepers.on },
                terrain = new {
                    manual = Util.GetUtil().GetStaticField("terrain", "manual"),
                    idleinterval = Util.GetUtil().GetStaticField("terrain", "idleinterval")
                },
                truth = new { truth.punish, truth.threshold },
                voice = new { voice.distance },
                wildlife = new {
                    forceupdate = Util.GetUtil().GetStaticField("wildlife", "forceupdate")
                }
            };

            Logger.Log($"[EngineMetricsDump] {JsonConvert.SerializeObject(combinedDump, Formatting.Indented)}");
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
            string[] lines = File.ReadAllLines(Util.GetRootFolder().Combine("\\Save\\IgnoredPlugins.txt"));
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
            string FougeriteDirectoryConfig = Util.GetServerFolder().Combine("FougeriteDirectory.cfg");
            
            // Init Configs
            Config.Init(FougeriteDirectoryConfig);
            
            // Init Logger
            Logger.Init();

            // Attempt to log unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            
            // Loom
            Loom.Initialize();
            
            // Initialize a default serializer for the datetime problem
            // https://stackoverflow.com/questions/24025350/xamarin-android-json-net-serilization-fails-on-4-2-2-device-only-timezonenotfoun
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Include,
            };
            
            // Load DataStore
            DataStore.GetInstance().Load();
            
            // Update Banlist
            UpdateBanList();
            
            // Initialize sqlite
            SQLiteConnector.GetInstance.Setup();
            
            // Load default permissions API.
            PermissionSystem.GetPermissionSystem();
            
            // Load Player Cache
            PlayerCache.GetPlayerCache().LoadPlayersCache();
            
            // Init other Caches.
            EntityCache.GetInstance();
            NPCCache.GetInstance();
            SleeperCache.GetInstance();

            Rust.Steam.Server.SetModded();
            Rust.Steam.Server.Official = false;
            
            FougeriteTickManager.Initialize();

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
        /// Updates the banlist from the Banlist.txt file (Old compatibility).
        /// </summary>
        private void UpdateBanList()
        {
            // Load Banlist
            try
            {
                Server.GetServer().UpdateBanlist();
            }
            catch (Exception ex)
            {
                Logger.LogError($"UpdateBanlist failed: {ex}");
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
