using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fougerite.Concurrent;
using Fougerite.Events;
using UnityEngine;

namespace Fougerite.PluginLoaders
{
    public class BasePlugin : CountedInstance, IPlugin
    {
        /// <summary>
        /// The author.
        /// </summary>
        public string Author;

        /// <summary>
        /// The about.
        /// </summary>
        public string About;

        /// <summary>
        /// The version.
        /// </summary>
        public string Version;

        /// <summary>
        /// Makes pluginloader ignore this plugin at 'pluton.reload'.
        /// </summary>
        public bool DontReload = false;

        public bool HasErrors = false;

        public string LastError = string.Empty;

        public readonly List<string> CommandList;

        /// <summary>
        /// Name of the Plugin.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// DirectoryInfo of the directory in which the plugin is in.
        /// </summary>
        /// <value>The root dir.</value>
        public DirectoryInfo RootDir { get; private set; }

        /// <summary>
        /// Global methods of the plugin.
        /// </summary>
        /// <value>The globals.</value>
        public IList<string> Globals { get; protected set; }
        
        /// <summary>
        /// Global methods of the plugin along with their functions.
        /// </summary>
        /// <value>The globals.</value>
        public Dictionary<string, object> CachedGlobals { get; protected set; }

        /// <summary>
        /// Dictionary that holds the timers.
        /// </summary>
        public readonly Dictionary<string, TimedEvent> Timers;

        /// <summary>
        /// List of parallel timers.
        /// </summary>
        public readonly List<TimedEvent> ParallelTimers;

        /// <summary>
        /// A global storage that any plugin can easily access.
        /// </summary>
        public static Dictionary<string, object> GlobalData;

        /// <summary>
        /// The type of the plugin.
        /// </summary>
        public PluginType Type = PluginType.Undefined;

        /// <summary>
        /// The current state of the plugin.
        /// </summary>
        public PluginState State = PluginState.NotLoaded;


        public virtual void Load(string code = "")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasePlugin"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="code">Code.</param>
        /// <param name="rootdir">RootDir.</param>
        public BasePlugin(string name, DirectoryInfo rootdir)
        {
            Name = name;
            RootDir = rootdir;
            Globals = new List<string>();
            CachedGlobals = new Dictionary<string, object>();

            Timers = new Dictionary<string, TimedEvent>();
            ParallelTimers = new List<TimedEvent>();
            CommandList = new List<string>();
        }

        /// <summary>
        /// Format exceptions to give meaningful reports.
        /// </summary>
        /// <returns>String representation of the exception.</returns>
        /// <param name="ex">The exception object.</param>
        public virtual string FormatException(Exception ex)
        {
            string nuline = Environment.NewLine;
            return ex.Message + nuline + ex.TargetSite.ToString() + nuline + ex.StackTrace;
        }

        /// <summary>
        /// Invoke the specified method and args.
        /// </summary>
        /// <param name="method">Method.</param>
        /// <param name="args">Arguments.</param>
        public virtual object Invoke(string method, params object[] args)
        {
            return null;
        }

        /// <summary>
        /// Normalizes the path.
        /// </summary>
        /// <returns>The path.</returns>
        /// <param name="path">Path.</param>
        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>
        /// Validates the relative path.
        /// </summary>
        /// <returns>The relative path.</returns>
        /// <param name="path">Path.</param>
        public string ValidateRelativePath(string path)
        {
            string normalizedPath = NormalizePath(Path.Combine(RootDir.FullName, path));
            string rootDirNormalizedPath = NormalizePath(RootDir.FullName);

            if (!normalizedPath.StartsWith(rootDirNormalizedPath))
                return null;

            return normalizedPath;
        }

        /// <summary>
        /// Creates the dir.
        /// </summary>
        /// <returns><c>true</c>, if dir was created, <c>false</c> otherwise.</returns>
        /// <param name="path">Path.</param>
        public bool CreateDir(string path)
        {
            try
            {
                path = ValidateRelativePath(path);
                if (string.IsNullOrEmpty(path))
                    return false;

                if (Directory.Exists(path))
                    return true;

                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return false;
        }

        /// <summary>
        /// Deletes the log.
        /// </summary>
        /// <param name="path">Path.</param>
        public void DeleteLog(string path)
        {
            path = ValidateRelativePath(path + ".log");
            if (path == null)
                return;

            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary>
        /// Log the specified text at path.log.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="text">Text.</param>
        public void Log(string path, string text)
        {
            path = ValidateRelativePath(path + ".log");
            if (string.IsNullOrEmpty(path))
                return;

            File.AppendAllText(path,
                "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "] " + text + "\r\n");
        }

        /// <summary>
        /// Rotates the log.
        /// </summary>
        /// <param name="logfile">Logfile.</param>
        /// <param name="max">Max.</param>
        public void RotateLog(string logfile, int max = 6)
        {
            logfile = ValidateRelativePath(logfile + ".log");
            if (logfile == null)
                return;

            string pathh, pathi;
            int i, h;
            for (i = max, h = i - 1; i > 1; i--, h--)
            {
                pathi = ValidateRelativePath(logfile + i + ".log");
                pathh = ValidateRelativePath(logfile + h + ".log");

                try
                {
                    if (!File.Exists(pathi))
                        File.Create(pathi);

                    if (!File.Exists(pathh))
                    {
                        File.Replace(logfile, pathi, null);
                    }
                    else
                    {
                        File.Replace(pathh, pathi, null);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        "[Plugin] RotateLog " + logfile + ", " + pathh + ", " + pathi + ", " + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Wether or not the specified '.json' file exists.
        /// </summary>
        /// <returns><c>true</c>, if the file exists, <c>false</c> otherwise.</returns>
        /// <param name="path">Path to the '.json' file.</param>
        public bool JsonFileExists(string path)
        {
            path = ValidateRelativePath(path + ".json");
            if (path == null)
                return false;

            return File.Exists(path);
        }

        /// <summary>
        /// Reads a '.json' file.
        /// </summary>
        /// <returns>The json string.</returns>
        /// <param name="path">Path to the '.json' file.</param>
        public string FromJsonFile(string path)
        {
            path = ValidateRelativePath(path + ".json");
            if (JsonFileExists(path))
                return File.ReadAllText(path);

            return null;
        }

        /// <summary>
        /// Saves a json string at the specified path with '.json' extension.
        /// </summary>
        /// <param name="path">File name.</param>
        /// <param name="json">The json string to save.</param>
        public void ToJsonFile(string path, string json)
        {
            path = ValidateRelativePath(path + ".json");
            if (path == null)
                return;

            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Gets the ini.
        /// </summary>
        /// <returns>An IniParser object.</returns>
        /// <param name="path">File name.</param>
        public IniParser GetIni(string path)
        {
            path = ValidateRelativePath(path + ".ini");
            if (path == null)
                return (IniParser) null;

            if (File.Exists(path))
                return new IniParser(path);

            return (IniParser) null;
        }

        /// <summary>
        /// Checks if the specified ini file exists.
        /// </summary>
        /// <returns><c>true</c>, if it exists, <c>false</c> otherwise.</returns>
        /// <param name="path">File name.</param>
        public bool IniExists(string path)
        {
            path = ValidateRelativePath(path + ".ini");
            if (path == null)
                return false;

            return File.Exists(path);
        }

        /// <summary>
        /// Creates the ini.
        /// </summary>
        /// <returns>The ini.</returns>
        /// <param name="path">Path.</param>
        public IniParser CreateIni(string path = null)
        {
            try
            {
                path = ValidateRelativePath(path + ".ini");
                if (String.IsNullOrEmpty(path))
                {
                    path = Name;
                }

                if (IniExists(path))
                    return GetIni(path);

                File.WriteAllText(path, "");
                return new IniParser(path);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return (IniParser) null;
        }

        /// <summary>
        /// Gets the inis.
        /// </summary>
        /// <returns>The inis.</returns>
        /// <param name="path">Path.</param>
        public List<IniParser> GetInis(string path)
        {
            path = ValidateRelativePath(path);
            if (path == null)
                return new List<IniParser>();

            return Directory.GetFiles(path).Select(p => new IniParser(p)).ToList();
        }


        /// <summary>
        /// Gets the plugin.
        /// </summary>
        /// <returns>The plugin.</returns>
        /// <param name="name">Name.</param>
        public BasePlugin GetPlugin(string name)
        {
            BasePlugin plugin;
            if (!PluginLoader.GetInstance().Plugins.TryGetValue(name, out plugin))
            {
                return null;
            }

            return plugin;
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <returns>The date.</returns>
        public string GetDate()
        {
            return DateTime.Now.ToShortDateString();
        }

        /// <summary>
        /// Gets the ticks.
        /// </summary>
        /// <returns>The ticks.</returns>
        public int GetTicks()
        {
            return Environment.TickCount;
        }

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <returns>The time.</returns>
        public string GetTime()
        {
            return DateTime.Now.ToShortTimeString();
        }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        /// <returns>The timestamp.</returns>
        public long GetTimestamp()
        {
            TimeSpan span = (TimeSpan) (DateTime.UtcNow - new DateTime(0x7b2, 1, 1, 0, 0, 0));
            return (long) span.TotalSeconds;
        }

        public void OnTimerCB(TimedEvent evt)
        {
            if (Globals.Contains(evt.Name + "Callback"))
            {
                Invoke(evt.Name + "Callback", evt);
            }
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <returns>The timer.</returns>
        /// <param name="name">Name.</param>
        /// <param name="timeoutDelay">Timeout delay.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                timedEvent = new TimedEvent(name, (double) timeoutDelay);
                timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(OnTimerCB);
                Timers.Add(name, timedEvent);
            }

            return timedEvent;
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <returns>The timer.</returns>
        /// <param name="name">Name.</param>
        /// <param name="timeoutDelay">Timeout delay.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay, Action<TimedEvent> callback)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                timedEvent = new TimedEvent(name, (double) timeoutDelay);
                timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
                Timers.Add(name, timedEvent);
            }

            return timedEvent;
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <returns>The timer.</returns>
        /// <param name="name">Name.</param>
        /// <param name="timeoutDelay">Timeout delay.</param>
        /// <param name="args">Arguments.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                timedEvent = new TimedEvent(name, (double) timeoutDelay);
                timedEvent.Args = args;
                timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(OnTimerCB);
                Timers.Add(name, timedEvent);
            }

            return timedEvent;
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <returns>The timer.</returns>
        /// <param name="name">Name.</param>
        /// <param name="timeoutDelay">Timeout delay.</param>
        /// <param name="args">Arguments.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args,
            Action<TimedEvent> callback)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                timedEvent = new TimedEvent(name, (double) timeoutDelay);
                timedEvent.Args = args;
                timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
                Timers.Add(name, timedEvent);
            }

            return timedEvent;
        }

        /// <summary>
        /// Gets a timer.
        /// </summary>
        /// <returns>The timer.</returns>
        /// <param name="name">Name.</param>
        public TimedEvent GetTimer(string name)
        {
            TimedEvent result;
            if (Timers.ContainsKey(name))
            {
                result = Timers[name];
            }
            else
            {
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Kills the timer.
        /// </summary>
        /// <param name="name">Name.</param>
        public void KillTimer(string name)
        {
            TimedEvent timer = GetTimer(name);
            if (timer == null)
                return;

            timer.Kill();
            Timers.Remove(name);
        }

        /// <summary>
        /// Kills the timers.
        /// </summary>
        public void KillTimers()
        {
            foreach (TimedEvent current in Timers.Values)
            {
                current.Kill();
            }

            foreach (TimedEvent timer in ParallelTimers)
            {
                timer.Kill();
            }

            Timers.Clear();
            ParallelTimers.Clear();
        }

        /// <summary>
        /// Creates a parallel timer.
        /// </summary>
        /// <returns>The parallel timer.</returns>
        /// <param name="name">Name.</param>
        /// <param name="timeoutDelay">Timeout delay.</param>
        /// <param name="args">Arguments.</param>
        public TimedEvent CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = new TimedEvent(name, (double) timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(OnTimerCB);
            ParallelTimers.Add(timedEvent);
            return timedEvent;
        }

        /// <summary>
        /// Creates a parallel timer.
        /// </summary>
        /// <returns>The parallel timer.</returns>
        /// <param name="name">Name.</param>
        /// <param name="timeoutDelay">Timeout delay.</param>
        /// <param name="args">Arguments.</param>
        public TimedEvent CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args,
            Action<TimedEvent> callback)
        {
            TimedEvent timedEvent = new TimedEvent(name, (double) timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
            ParallelTimers.Add(timedEvent);
            return timedEvent;
        }

        /// <summary>
        /// Gets the parallel timer.
        /// </summary>
        /// <returns>The parallel timer.</returns>
        /// <param name="name">Name.</param>
        public List<TimedEvent> GetParallelTimer(string name)
        {
            return (from timer in ParallelTimers
                where timer.Name == name
                select timer).ToList();
        }

        /// <summary>
        /// Kills the parallel timer.
        /// </summary>
        /// <param name="name">Name.</param>
        public void KillParallelTimer(string name)
        {
            foreach (TimedEvent timer in GetParallelTimer(name))
            {
                timer.Kill();
                ParallelTimers.Remove(timer);
            }
        }

        public Dictionary<string, object> CreateDict()
        {
            return new Dictionary<string, object>();
        }
        
        public Dictionary<string, string> CreateStringDict()
        {
            return new Dictionary<string, string>();
        } 

        public Dictionary<object, object> CreateDynamicDict()
        {
            return new Dictionary<object, object>();
        }

        public ReaderWriterLock CreateReaderWriterLock()
        {
            return new ReaderWriterLock();
        }
        
        public ConcurrentDictionary<string, object> CreateConcurrentDict()
        {
            return new ConcurrentDictionary<string, object>();
        }
        
        public ConcurrentDictionary<string, string> CreateDynamicConcurrentStringDict()
        {
            return new ConcurrentDictionary<string, string>();
        }

        public ConcurrentDictionary<object, object> CreateDynamicConcurrentDict()
        {
            return new ConcurrentDictionary<object, object>();
        }

        public List<object> CreateList()
        {
            return new List<object>();
        }
        
        public List<string> CreateStringList()
        {
            return new List<string>();
        }
        
        public ConcurrentList<object> CreateConcurrentList()
        {
            return new ConcurrentList<object>();
        }
        
        public ConcurrentList<string> CreateConcurrentStringList()
        {
            return new ConcurrentList<string>();
        }

        public void OnTablesLoaded(Dictionary<string, LootSpawnList> tables)
        {
            this.Invoke("On_TablesLoaded", tables);
        }

        public void OnAllPluginsLoaded()
        {
            this.Invoke("On_AllPluginsLoaded");
        }

        public void OnBlueprintUse(Fougerite.Player player, BPUseEvent evt)
        {
            this.Invoke("On_BlueprintUse", player, evt);
        }

        public void OnChat(Fougerite.Player player, ref ChatString text)
        {
            this.Invoke("On_Chat", player, text);
        }

        public void OnCommand(Fougerite.Player player, string command, string[] args)
        {
            if (CommandList.Count != 0 && !CommandList.Contains(command) &&
                !Fougerite.Server.ForceCallForCommands.Contains(command))
            {
                return;
            }

            this.Invoke("On_Command", player, command, args);
        }

        public void OnConsole(ref ConsoleSystem.Arg arg, bool external)
        {
            string clss = arg.Class.ToLower();
            string func = arg.Function.ToLower();
            if (!external)
            {
                Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(arg.argUser.userID);
                arg.ReplyWith(player.Name + " executed: " + clss + "." + func);
                this.Invoke("On_Console", player, arg);
            }
            else
            {
                arg.ReplyWith("Rcon: " + clss + "." + func);
                this.Invoke("On_Console", null, arg);
            }
        }
        
        public void OnConsoleWithCancel(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent consoleEvent)
        {
            string clss = arg.Class.ToLower();
            string func = arg.Function.ToLower();
            if (!external)
            {
                Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(arg.argUser.userID);
                arg.ReplyWith(player.Name + " executed: " + clss + "." + func);
                this.Invoke("On_ConsoleWithCancel", player, arg, consoleEvent);
            }
            else
            {
                arg.ReplyWith("Rcon: " + clss + "." + func);
                this.Invoke("On_ConsoleWithCancel", null, arg, consoleEvent);
            }
        }

        public void OnDoorUse(Fougerite.Player player, DoorEvent evt)
        {
            this.Invoke("On_DoorUse", player, evt);
        }

        public void OnEntityDecay(DecayEvent evt)
        {
            this.Invoke("On_EntityDecay", evt);
        }

        public void OnEntityDeployed(Fougerite.Player player, Entity entity, Fougerite.Player actualplacer)
        {
            try
            {
                this.Invoke("On_EntityDeployed", player, entity, actualplacer);
            }
            catch (Exception ex)
            {
                Fougerite.Logger.LogError("[IronPython] Error in plugin " + Name + " when invoking On_EntityDeployed ensure you have 3 parameters:" + ex);
            }
        }

        public void OnEntityDestroyed(DestroyEvent evt)
        {
            this.Invoke("On_EntityDestroyed", evt);
        }

        public void OnEntityHurt(HurtEvent evt)
        {
            this.Invoke("On_EntityHurt", evt);
        }

        public void OnItemsLoaded(ItemsBlocks items)
        {
            this.Invoke("On_ItemsLoaded", items);
        }

        public void OnNPCHurt(HurtEvent evt)
        {
            this.Invoke("On_NPCHurt", evt);
        }

        public void OnNPCKilled(DeathEvent evt)
        {
            this.Invoke("On_NPCKilled", evt);
        }

        public void OnPlayerConnected(Fougerite.Player player)
        {
            this.Invoke("On_PlayerConnected", player);
        }

        public void OnPlayerDisconnected(Fougerite.Player player)
        {
            this.Invoke("On_PlayerDisconnected", player);
        }

        public void OnPlayerGathering(Fougerite.Player player, GatherEvent evt)
        {
            this.Invoke("On_PlayerGathering", player, evt);
        }

        public void OnPlayerHurt(HurtEvent evt)
        {
            this.Invoke("On_PlayerHurt", evt);
        }

        public void OnPlayerKilled(DeathEvent evt)
        {
            this.Invoke("On_PlayerKilled", evt);
        }

        public void OnPlayerTeleport(Fougerite.Player player, Vector3 from, Vector3 dest)
        {
            this.Invoke("On_PlayerTeleport", player, from, dest);
        }

        public void OnPlayerSpawn(Fougerite.Player player, SpawnEvent evt)
        {
            this.Invoke("On_PlayerSpawning", player, evt);
        }

        public void OnPlayerSpawned(Fougerite.Player player, SpawnEvent evt)
        {
            this.Invoke("On_PlayerSpawned", player, evt);
        }

        public void OnResearch(ResearchEvent evt)
        {
            this.Invoke("On_Research", evt);
        }

        public void OnServerInit()
        {
            this.Invoke("On_ServerInit");
        }

        public void OnServerShutdown()
        {
            this.Invoke("On_ServerShutdown");
        }

        public void OnServerSaved(int amount, double seconds)
        {
            this.Invoke("On_ServerSaved", amount, seconds);
        }

        public void OnCrafting(CraftingEvent e)
        {
            this.Invoke("On_Crafting", e);
        }

        public void OnResourceSpawned(ResourceTarget t)
        {
            this.Invoke("On_ResourceSpawn", t);
        }

        public void OnItemAdded(InventoryModEvent e)
        {
            this.Invoke("On_ItemAdded", e);
        }

        public void OnItemRemoved(InventoryModEvent e)
        {
            this.Invoke("On_ItemRemoved", e);
        }

        public void OnItemPickup(ItemPickupEvent e)
        {
            this.Invoke("On_ItemPickup", e);
        }

        public void OnFallDamage(FallDamageEvent e)
        {
            this.Invoke("On_FallDamage", e);
        }

        public void OnAirdrop(Vector3 v)
        {
            this.Invoke("On_Airdrop", v);
        }

        public void OnAirdropCrateDropped(SupplyDropPlane plane, Entity supplyCrate)
        {
            this.Invoke("On_AirdropCrateDropped", plane, supplyCrate);
        }

        public void OnSteamDeny(SteamDenyEvent e)
        {
            this.Invoke("On_SteamDeny", e);
        }

        public void OnPlayerApproval(PlayerApprovalEvent e)
        {
            this.Invoke("On_PlayerApproval", e);
        }

        public void OnPluginShutdown()
        {
            this.Invoke("On_PluginShutdown");
        }

        public void OnShowTalker(uLink.NetworkPlayer np, Fougerite.Player player)
        {
            this.Invoke("On_VoiceChat", np, player);
        }

        public void OnLootUse(LootStartEvent le)
        {
            this.Invoke("On_LootUse", le);
        }

        public void OnBanEvent(BanEvent be)
        {
            this.Invoke("On_PlayerBan", be);
        }

        public void OnRepairBench(Fougerite.Events.RepairEvent be)
        {
            this.Invoke("On_RepairBench", be);
        }

        public void OnItemMove(ItemMoveEvent be)
        {
            this.Invoke("On_ItemMove", be);
        }

        public void OnGenericSpawnLoad(GenericSpawner gs)
        {
            this.Invoke("On_GenericSpawnLoad", gs);
        }

        public void OnServerLoaded()
        {
            this.Invoke("On_ServerLoaded");
        }

        public void OnSupplySignalExploded(SupplySignalExplosionEvent evt)
        {
            this.Invoke("On_SupplySignalExploded", evt);
        }

        public void OnPlayerMove(HumanController hc, Vector3 v, int p, ushort p2,
            uLink.NetworkMessageInfo networkMessageInfo, Util.PlayerActions action)
        {
            this.Invoke("On_PlayerMove", hc, v, p, p2, networkMessageInfo, action);
        }

        public void OnBeltUse(BeltUseEvent ev)
        {
            this.Invoke("On_BeltUse", ev);
        }

        public void OnLogger(LoggerEvent ev)
        {
            this.Invoke("On_Logger", ev);
        }

        public void OnGrenade(GrenadeThrowEvent ev)
        {
            this.Invoke("On_GrenadeThrow", ev);
        }
    }
}