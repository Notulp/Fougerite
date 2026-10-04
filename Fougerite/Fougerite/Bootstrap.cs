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
        public const string Version = "1.9.95";
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
        /// <summary>
        /// This value decides whether truth.punish, Facepunch's original speedhack and flyhack validations,
        /// should be disabled. You may enable this setting on a RustBuster server.
        /// </summary>
        public static bool DisableFacePunchTruthPunish;
        /// <summary>
        /// Determines who may join when native Steam authentication rejects a connection.
        /// See <see cref="SteamAuthMode"/> for the available modes.
        /// Profile settings only matter for Spacewar players. <see cref="SteamAuthMode.RustOwners"/> requires public
        /// game details, <see cref="SteamAuthMode.SteamPaidAccounts"/> requires that a community profile exists but
        /// accepts private ones, and every other mode works regardless of profile settings.
        /// </summary>
        public static SteamAuthMode SteamAuthenticationMode = SteamAuthMode.Legacy;
        /// <summary>
        /// Holds the Steam Web API key used to verify Spacewar tickets.
        /// The field is internal so that script plugins cannot read the key by accident.
        /// </summary>
        internal static string SteamWebAPIKey = string.Empty;
        /// <summary>
        /// Holds the timeout of a single Steam Web API request in seconds, between 1 and 45.
        /// </summary>
        public static float SteamWebAPITimeout = 10f;
        /// <summary>
        /// Determines whether Spacewar players are admitted when the Steam Web API cannot be reached
        /// because of an outage, a rate limit or a timeout.
        /// </summary>
        public static bool SteamWebAPIFailOpen;
        
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
        /// Applies options from the Fougerite.cfg.
        /// Any key that is missing from the file is registered via <see cref="IniParser.AddDefault"/> so that
        /// it will be written together with a descriptive comment the next time <see cref="IniParser.Save"/>
        /// is called.
        /// </summary>
        /// <returns></returns>
        public bool ApplyOptions()
        {
            Config.AddDefault("Fougerite", "enabled",
                "true",
                "uncomment to disable Fougerite, to disable version announce at login,\n" +
                "or to enable structure and deployed item decay");

            Config.AddDefault("Fougerite", "RemovePlayersFromCache",
                "false",
                "Remove players from cache after they disconnect.");

            Config.AddDefault("Fougerite", "BanOnInvalidPacket",
                "true",
                "If a player sends an invalid packet (Possibly hacker) ban him.");

            Config.AddDefault("Fougerite", "AutoBanCraft",
                "true",
                "Autoban people for Crafting Hack? (If this is disabled, the crafting will be only cancelled/logged.)");

            Config.AddDefault("Fougerite", "SaveNotification",
                "The server is currently saving! You have to wait before placing an object.",
                "Text to display to the player when the server is saving, and the building parts cannot be placed due the subthread.");

            Config.AddDefault("Fougerite", "RustChat",
                "true",
                "Enable the default ChatSystem output for the Player.Message methods?");

            Config.AddDefault("Fougerite", "RPCChat",
                "false",
                "Send additional RPCPackets of the chat for the clients? (This is recommended for RustBuster Servers only.)");

            Config.AddDefault("Fougerite", "ClientFunction",
                "FougeriteChatSystem",
                "Specify the client side's RPC method.");

            Config.AddDefault("Fougerite", "EnableScriptPluginsIntensiveEvents",
                "false",
                "Enable intensive events for script plugins (Py, Lua, JS)\n" +
                "This gives scripts access to events like OnPlayerMove, OnShoot, OnShotgunShoot, OnBowShoot, OnAnimalMovement,\n" +
                "OnHeatZoneEnter, OnWorkZoneEnter.\n" +
                "Use this carefully, as these events are called very often and may cause performance issues (server laggs).\n" +
                "It is recommended to use C# plugins for these events instead.\n" +
                "Script plugins are generally slower than C# plugins. Python is the fastest among script plugins.\n" +
                "Use at your own risk.");

            Config.AddDefault("Fougerite", "SilentConsoleCommands",
                "false",
                "Suppress the default \"Fougerite: Class.Function was executed!\" response\n" +
                "when a console command doesn't explicitly specify a reply text?");

            Config.AddDefault("Fougerite", "ServerMessageName",
                "Fougerite",
                "Specifies the name of the message displayed by the server for system notifications.\n" +
                "This value is typically configurable and determines the title or identifier of server broadcast messages.");

            Config.AddDefault("Fougerite", "FloodConnections",
                "2",
                "How many connections can be made from the same IP / 3 seconds?");

            Config.AddDefault("Fougerite", "SaveTime",
                "10",
                "How many minutes shall pass until the server saves everything?");

            Config.AddDefault("Fougerite", "SaveCopies",
                "5",
                "How many copies should the server make of the save files, before deleting them? Do not set this below 5.");

            Config.AddDefault("Fougerite", "StopServerOnSaveFail",
                "false",
                "Stop Server on Saving failure?");

            Config.AddDefault("Fougerite", "CrucialSavePoint",
                "0",
                "Ensure that manual save won't be executed, if the server is going to autosave? How many minutes should be the critical point\n" +
                "where the manual save won't run if less than X minutes is left before autosave? This should be LESS than 'SaveTime'. 0 to disable.");

            Config.AddDefault("Fougerite", "EnableDefaultRustDecay",
                "true",
                "Enable Default Rust Decay? (May cause problems or laggs after a huge map.)");

            Config.AddDefault("Fougerite", "DisableFacePunchTruthPunish",
                "false",
                "Setting this to true will disable truth.punish, FacePunch's original speedhack and flyhack validations\n" +
                "You may disable this setting on a RustBuster server.");

            Config.AddDefault("Fougerite", "SteamAuthMode",
                "Legacy",
                "Decides who may join when Steam rejects the connection ticket (RustBuster on Spacewar 480, cracked clients).\n" +
                "Players on the real Rust AppID (252490) are always checked natively by Steam and are never affected by\n" +
                "profile privacy. The profile requirements below only apply to Spacewar (480) players.\n" +
                "\n" +
                "Legacy                   Old behaviour. Nothing is verified, plugins like AuthAllow decide via SteamDenyEvent.ForceAllow.\n" +
                "                         Profile requirement: none.\n" +
                "RustOnly                 Only players whose ticket passes native Steam auth for Rust (252490). Spacewar players are rejected.\n" +
                "                         Profile requirement: none.\n" +
                "RustOwners               Rust players, plus Spacewar (480) tickets verified by the Steam Web API whose account owns Rust.\n" +
                "                         Requires SteamWebAPIKey.\n" +
                "                         Profile requirement: PUBLIC GAME DETAILS. Steam privacy settings, 'Game details' must be Public.\n" +
                "                         Players with private or friends only game details are REJECTED even if they own Rust.\n" +
                "SteamPaidAccounts        Rust players, plus Spacewar (480) tickets verified by the Steam Web API from accounts that are\n" +
                "                         not limited (spent at least 5 USD on Steam). Requires SteamWebAPIKey.\n" +
                "                         Profile requirement: the account must have set up a Steam Community profile once.\n" +
                "                         Private and friends only profiles are fine. Accounts that never set one up are REJECTED.\n" +
                "SteamAccounts            Rust players, plus any genuine Steam account on a Spacewar (480) ticket verified by the Steam Web API.\n" +
                "                         Forged and emulated tickets are rejected. Requires SteamWebAPIKey.\n" +
                "                         Profile requirement: none. Private profiles and accounts without a profile are fine.\n" +
                "SteamAccountsUnverified  Rust players, plus Spacewar (480) tickets that pass offline checks. No API key, no requests, fastest.\n" +
                "                         Rejects broken emulators and sloppy forgeries, but a well forged ticket gets in with any SteamID.\n" +
                "                         Profile requirement: none.\n" +
                "AllowAll                 Everyone joins, including players without Steam at all (cracked).\n" +
                "                         Profile requirement: none.\n" +
                "\n" +
                "Outside Legacy, plugins can still deny a player but can't let in one that the mode rejects.");

            Config.AddDefault("Fougerite", "SteamWebAPIKey",
                "",
                "Steam Web API key from https://steamcommunity.com/dev/apikey\n" +
                "Required by RustOwners, SteamPaidAccounts and SteamAccounts, ignored by every other mode. Keep it private.");

            Config.AddDefault("Fougerite", "SteamWebAPITimeout",
                "10",
                "Seconds to wait for one Steam Web API request (1-45). The player waits this long while connecting.\n" +
                "Worst case a join takes longer, because an invalid ticket answer is retried for about 5 seconds and\n" +
                "RustOwners and SteamPaidAccounts make a second request after the ticket check.");

            Config.AddDefault("Fougerite", "SteamWebAPIFailOpen",
                "false",
                "RustOwners, SteamPaidAccounts and SteamAccounts only. If the Steam Web API can't be reached (outage, rate limit, timeout)\n" +
                "let Spacewar players in anyway (true) or reject them (false)?\n" +
                "true means forged tickets get in during an outage. A rejected API key always denies.");

            // Persist any newly added defaults so the file is up-to-date after the first save cycle.
            Config.Save();
            
            // look for the string 'false' to disable.  **not a bool check**
            if (Config.GetValue("Fougerite", "enabled") == "false") 
            {
                Debug.Log("Fougerite is disabled. No modules loaded. No hooks called.");
                return false;
            }

            CR = Config.GetBoolValue("Fougerite", "RemovePlayersFromCache");
            BI = Config.GetBoolValue("Fougerite", "BanOnInvalidPacket");
            AutoBanCraft = Config.GetBoolValue("Fougerite", "AutoBanCraft");
            SaveNotification = Config.GetValue("Fougerite", "SaveNotification");
            RustChat = Config.GetBoolValue("Fougerite", "RustChat");
            RPCChat = Config.GetBoolValue("Fougerite", "RPCChat");
            RPCChatMethod = Config.GetValue("Fougerite", "ClientFunction");
            EnableScriptPluginsIntensiveEvents = Config.GetBoolValue("Fougerite", "EnableScriptPluginsIntensiveEvents");
            SilentConsoleCommands = Config.GetBoolValue("Fougerite", "SilentConsoleCommands");
            ServerMessageName = Config.GetValue("Fougerite", "ServerMessageName");
            Server.GetServer().server_message_name = ServerMessageName;

            if (!RustChat)
            {
                Logger.LogWarning("[RustChat] The default Rust Chat is disabled for the Player.Message methods.");
            }

            if (SilentConsoleCommands)
            {
                Logger.LogWarning("[SilentConsoleCommands] The default console command response is disabled for commands that don't explicitly specify a reply text.");
            }

            int floodVal;
            int.TryParse(Config.GetValue("Fougerite", "FloodConnections"), out floodVal);
            FloodConnections = (floodVal > 0 ? floodVal : 2) + 1;

            int saveTime;
            int.TryParse(Config.GetValue("Fougerite", "SaveTime"), out saveTime);
            ServerSaveHandler.ServerSaveTime = saveTime > 0 ? saveTime : 10;

            int saveCopies;
            int.TryParse(Config.GetValue("Fougerite", "SaveCopies"), out saveCopies);
            ServerSaveHandler.SaveCopies = saveCopies >= 5 ? saveCopies : 5;

            bool stopOnFail;
            bool.TryParse(Config.GetValue("Fougerite", "StopServerOnSaveFail"), out stopOnFail);
            ServerSaveHandler.StopServerOnSaveFail = stopOnFail;

            int crucialPoint;
            int.TryParse(Config.GetValue("Fougerite", "CrucialSavePoint"), out crucialPoint);
            ServerSaveHandler.CrucialSavePoint = crucialPoint > 0 ? crucialPoint : 2;

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
            
            EnableDefaultRustDecay = Config.GetBoolValue("Fougerite", "EnableDefaultRustDecay");
            
            DisableFacePunchTruthPunish = Config.GetBoolValue("Fougerite", "DisableFacePunchTruthPunish");
            if (DisableFacePunchTruthPunish)
            {
                truth.punish = false;
                Logger.LogWarning("[DisableFacePunchTruthPunish] Facepunch's original speedhack and flyhack validations (truth.punish) are disabled.");
            }

            ApplySteamAuthOptions();

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
                    ServerMessageName = ServerMessageName,
                    SteamAuthMode = SteamAuthenticationMode.ToString(),
                    SteamWebAPIKeyConfigured = !string.IsNullOrEmpty(SteamWebAPIKey),
                    SteamWebAPITimeout = SteamWebAPITimeout,
                    SteamWebAPIFailOpen = SteamWebAPIFailOpen
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
        /// Reads the SteamAuthMode related options from Fougerite.cfg, validates them and reports the outcome.
        /// </summary>
        private void ApplySteamAuthOptions()
        {
            string rawMode = (Config.GetValue("Fougerite", "SteamAuthMode") ?? string.Empty).Trim();
            SteamAuthenticationMode = SteamAuthMode.Legacy;
            if (rawMode.Length > 0)
            {
                try
                {
                    SteamAuthMode parsed = (SteamAuthMode)Enum.Parse(typeof(SteamAuthMode), rawMode, true);
                    if (Enum.IsDefined(typeof(SteamAuthMode), parsed))
                    {
                        SteamAuthenticationMode = parsed;
                    }
                    else
                    {
                        Logger.LogError($"[SteamAuth] Unknown SteamAuthMode '{rawMode}', using Legacy.");
                    }
                }
                catch (Exception)
                {
                    Logger.LogError($"[SteamAuth] Unknown SteamAuthMode '{rawMode}', using Legacy.");
                }
            }

            SteamWebAPIKey = (Config.GetValue("Fougerite", "SteamWebAPIKey") ?? string.Empty).Trim();

            int timeout;
            if (!int.TryParse(Config.GetValue("Fougerite", "SteamWebAPITimeout"), out timeout))
            {
                timeout = 10;
            }
            SteamWebAPITimeout = Math.Max(1, Math.Min(45, timeout));

            SteamWebAPIFailOpen = Config.GetBoolValue("Fougerite", "SteamWebAPIFailOpen");

            bool keySet = !string.IsNullOrEmpty(SteamWebAPIKey);
            if (keySet && (SteamWebAPIKey.Length != 32 || !IsHex(SteamWebAPIKey)))
            {
                Logger.LogWarning("[SteamAuth] SteamWebAPIKey doesn't look like a Steam Web API key (32 hex characters).");
            }

            switch (SteamAuthenticationMode)
            {
                case SteamAuthMode.Legacy:
                    Logger.LogWarning("[SteamAuth] SteamAuthMode=Legacy. Steam-rejected players are let in or kept out by plugins " +
                                      "(AuthAllow etc.) and forged tickets are NOT detected. Consider SteamAccounts or RustOwners.");
                    break;
                case SteamAuthMode.RustOnly:
                    Logger.LogWarning("[SteamAuth] SteamAuthMode=RustOnly. Only players passing native Rust Steam auth can join.");
                    break;
                case SteamAuthMode.RustOwners:
                case SteamAuthMode.SteamPaidAccounts:
                case SteamAuthMode.SteamAccounts:
                    if (!keySet)
                    {
                        Logger.LogError($"[SteamAuth] SteamAuthMode={SteamAuthenticationMode} needs SteamWebAPIKey. " +
                                        "Spacewar players will be DENIED until a key is set (behaves like RustOnly).");
                    }
                    else
                    {
                        Logger.LogWarning($"[SteamAuth] SteamAuthMode={SteamAuthenticationMode}, Spacewar tickets are verified " +
                                          $"through the Steam Web API (timeout {SteamWebAPITimeout}s).");
                    }

                    if (SteamAuthenticationMode == SteamAuthMode.RustOwners)
                    {
                        Logger.LogWarning("[SteamAuth] RustOwners needs PUBLIC game details on the Steam profile of Spacewar players. " +
                                          "Players with private game details are rejected even if they own Rust.");
                    }
                    else if (SteamAuthenticationMode == SteamAuthMode.SteamPaidAccounts)
                    {
                        Logger.LogWarning("[SteamAuth] SteamPaidAccounts needs Spacewar players to have set up a Steam Community " +
                                          "profile once. Private profiles are fine.");
                    }
                    break;
                case SteamAuthMode.SteamAccountsUnverified:
                    Logger.LogWarning("[SteamAuth] SteamAuthMode=SteamAccountsUnverified. Spacewar tickets are checked offline only. " +
                                      "Sloppy forgeries are rejected, but a well forged ticket can still join and claim any SteamID.");
                    break;
                case SteamAuthMode.AllowAll:
                    Logger.LogWarning("[SteamAuth] SteamAuthMode=AllowAll. Anyone can join, including players without Steam.");
                    break;
            }

            if (SteamWebAPIFailOpen && (SteamAuthenticationMode == SteamAuthMode.RustOwners
                                        || SteamAuthenticationMode == SteamAuthMode.SteamPaidAccounts
                                        || SteamAuthenticationMode == SteamAuthMode.SteamAccounts))
            {
                Logger.LogWarning("[SteamAuth] SteamWebAPIFailOpen=true. Unverified Spacewar players get in while the Steam Web API is unreachable.");
            }
        }

        private static bool IsHex(string value)
        {
            foreach (char c in value)
            {
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
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
