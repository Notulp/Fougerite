using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fougerite.Concurrent;
using Fougerite.Events;
using Fougerite.Tools;
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
        /// Makes pluginloader ignore this plugin at 'fougerite.reload'.
        /// </summary>
        public bool DontReload = false;

        public bool HasErrors = false;

        public string LastError = string.Empty;

        public readonly ConcurrentList<string> CommandList;

        /// <summary>
        /// Name of the Plugin.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; protected set; }

        /// <summary>
        /// DirectoryInfo of the directory in which the plugin is in.
        /// </summary>
        /// <value>The root dir.</value>
        public DirectoryInfo RootDir { get; internal set; }

        /// <summary>
        /// Global methods of the plugin.
        /// </summary>
        /// <value>The globals.</value>
        public ConcurrentList<string> Globals { get; protected set; }
        
        /// <summary>
        /// Global methods of the plugin along with their functions.
        /// </summary>
        /// <value>The globals.</value>
        public ConcurrentDictionary<string, object> CachedGlobals { get; protected set; }

        /// <summary>
        /// Dictionary that holds the timers.
        /// </summary>
        public readonly ConcurrentDictionary<string, TimedEvent> Timers;

        /// <summary>
        /// List of parallel timers.
        /// </summary>
        public readonly ConcurrentList<TimedEvent> ParallelTimers;
        
        /// <summary>
        /// List of WebSockets created by the plugin.
        /// </summary>
        public readonly ConcurrentList<ScriptWebSocket> WebSockets;

        /// <summary>
        /// A global storage that any plugin can easily access.
        /// </summary>
        public static ConcurrentDictionary<string, object> GlobalData;

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
        /// Empty constructor for backward compatibility with old C# Modules.
        /// </summary>
        public BasePlugin()
        {
            Globals = new ConcurrentList<string>();
            CachedGlobals = new ConcurrentDictionary<string, object>();
            Timers = new ConcurrentDictionary<string, TimedEvent>();
            ParallelTimers = new ConcurrentList<TimedEvent>();
            CommandList = new ConcurrentList<string>();
            WebSockets = new ConcurrentList<ScriptWebSocket>();

            if (Type != PluginType.CSharp && Type != PluginType.CSScript) 
                return;
            
            var nameProp = GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            if (nameProp != null)
            {
                Name = nameProp.GetValue(this, null) as string;
            }
            else
            {
                throw new Exception("Name property could not be retrieved via reflection. Please ensure the plugin has a public instance property named 'Name'.");
            }
        }
            
        /// <summary>
        /// Initializes a new instance of the <see cref="BasePlugin"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="rootdir">RootDir.</param>
        public BasePlugin(string name, DirectoryInfo rootdir)
        {
            Name = name;
            RootDir = rootdir;
            Globals = new ConcurrentList<string>();
            CachedGlobals = new ConcurrentDictionary<string, object>();

            Timers = new ConcurrentDictionary<string, TimedEvent>();
            ParallelTimers = new ConcurrentList<TimedEvent>();
            CommandList = new ConcurrentList<string>();
            WebSockets = new ConcurrentList<ScriptWebSocket>();
        }

        /// <summary>
        /// Format exceptions to give meaningful reports.
        /// </summary>
        /// <returns>String representation of the exception.</returns>
        /// <param name="ex">The exception object.</param>
        public virtual string FormatException(Exception ex)
        {
            string nuline = Environment.NewLine;
            return string.Format("{0}{1}{2}{1}{3}", ex.Message, nuline, ex.TargetSite, ex.StackTrace);
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
            path = ValidateRelativePath($"{path}.log");
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
            path = ValidateRelativePath($"{path}.log");
            if (string.IsNullOrEmpty(path))
                return;

            File.AppendAllText(path,
                $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] {text}\r\n");
        }

        /// <summary>
        /// Rotates the log.
        /// </summary>
        /// <param name="logfile">Logfile.</param>
        /// <param name="max">Max.</param>
        public void RotateLog(string logfile, int max = 6)
        {
            logfile = ValidateRelativePath($"{logfile}.log");
            if (logfile == null)
                return;

            string pathh, pathi;
            int i, h;
            for (i = max, h = i - 1; i > 1; i--, h--)
            {
                pathi = ValidateRelativePath($"{logfile}{i}.log");
                pathh = ValidateRelativePath($"{logfile}{h}.log");

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
                        $"[Plugin] RotateLog {logfile}, {pathh}, {pathi}, {ex.StackTrace}");
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
            path = ValidateRelativePath($"{path}.json");
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
            path = ValidateRelativePath($"{path}.json");
            if (JsonFileExists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (StreamReader reader = new StreamReader(fs))
                {
                    return reader.ReadToEnd();
                }
            }

            return null;
        }

        /// <summary>
        /// Saves a json string at the specified path with '.json' extension.
        /// </summary>
        /// <param name="path">File name.</param>
        /// <param name="json">The json string to save.</param>
        public void ToJsonFile(string path, string json)
        {
            path = ValidateRelativePath($"{path}.json");
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
            path = ValidateRelativePath($"{path}.ini");
            if (path == null)
                return null;

            if (File.Exists(path))
                return new IniParser(path);

            return null;
        }

        /// <summary>
        /// Checks if the specified ini file exists.
        /// </summary>
        /// <returns><c>true</c>, if it exists, <c>false</c> otherwise.</returns>
        /// <param name="path">File name.</param>
        public bool IniExists(string path)
        {
            path = ValidateRelativePath($"{path}.ini");
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
                path = ValidateRelativePath($"{path}.ini");
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

            return null;
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
            TimeSpan span = DateTime.UtcNow - new DateTime(0x7b2, 1, 1, 0, 0, 0);
            return (long) span.TotalSeconds;
        }

        /// <summary>
        /// Runs when a Timer is fired.
        /// </summary>
        /// <param name="evt"></param>
        public void BaseOnTimerCB(TimedEvent evt)
        {
            if (Globals.Contains($"{evt.Name}Callback"))
            {
                Invoke($"{evt.Name}Callback", evt);
            }
        }

        /// <summary>
        /// Creates a timer.
        /// </summary>
        /// <returns>The timer.</returns>
        /// <param name="name">Name.</param>
        /// <param name="timeoutDelay">Timeout delay.</param>
        /// <param name="autoReset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="maxElapsedCount">The maximum number of times the timer should fire. 0 = infinite.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay, bool autoReset = false, int maxElapsedCount = 0)
        {
            Util.GetUtil().ThreadTimerCheck();
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                GameObject go = new GameObject($"TimedEvent_{name}_{UnityEngine.Random.Range(1, 999999)}");
                UnityEngine.Object.DontDestroyOnLoad(go);
                timedEvent = go.AddComponent<TimedEvent>();
                
                timedEvent.Name = name;
                timedEvent.PluginName = Name;
                timedEvent.Interval = timeoutDelay;
                timedEvent.AutoReset = autoReset;
                timedEvent.MaxElapsedCount = maxElapsedCount;
                timedEvent.OnFire += BaseOnTimerCB;
                timedEvent.OnKilled += (cbName) => Timers.Remove(name);

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
        /// <param name="callback">The callback function.</param>
        /// <param name="autoReset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="maxElapsedCount">The maximum number of times the timer should fire. 0 = infinite.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay, Action<TimedEvent> callback, bool autoReset = false, int maxElapsedCount = 0)
        {
            Util.GetUtil().ThreadTimerCheck();
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                GameObject go = new GameObject($"TimedEvent_{name}_{UnityEngine.Random.Range(1, 999999)}");
                UnityEngine.Object.DontDestroyOnLoad(go);
                timedEvent = go.AddComponent<TimedEvent>();

                timedEvent.Name = name;
                timedEvent.PluginName = Name;
                timedEvent.Interval = timeoutDelay;
                timedEvent.AutoReset = autoReset;
                timedEvent.MaxElapsedCount = maxElapsedCount;
                timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
                timedEvent.OnKilled += (cbName) => Timers.Remove(cbName);

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
        /// <param name="autoReset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="maxElapsedCount">The maximum number of times the timer should fire. 0 = infinite.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args, bool autoReset = false, int maxElapsedCount = 0)
        {
            Util.GetUtil().ThreadTimerCheck();
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                GameObject go = new GameObject($"TimedEvent_{name}_{UnityEngine.Random.Range(1, 999999)}");
                UnityEngine.Object.DontDestroyOnLoad(go);
                timedEvent = go.AddComponent<TimedEvent>();

                timedEvent.Name = name;
                timedEvent.PluginName = Name;
                timedEvent.Interval = timeoutDelay;
                timedEvent.Args = args;
                timedEvent.AutoReset = autoReset;
                timedEvent.MaxElapsedCount = maxElapsedCount;
                timedEvent.OnFire += BaseOnTimerCB;
                timedEvent.OnKilled += (cbName) => Timers.Remove(cbName);
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
        /// <param name="callback">The callback function.</param>
        /// <param name="autoReset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="maxElapsedCount">The maximum number of times the timer should fire. 0 = infinite.</param>
        public TimedEvent CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args,
            Action<TimedEvent> callback, bool autoReset = false, int maxElapsedCount = 0)
        {
            Util.GetUtil().ThreadTimerCheck();
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                GameObject go = new GameObject($"TimedEvent_{name}_{UnityEngine.Random.Range(1, 999999)}");
                UnityEngine.Object.DontDestroyOnLoad(go);
                timedEvent = go.AddComponent<TimedEvent>();

                timedEvent.Name = name;
                timedEvent.PluginName = Name;
                timedEvent.Interval = timeoutDelay;
                timedEvent.Args = args;
                timedEvent.AutoReset = autoReset;
                timedEvent.MaxElapsedCount = maxElapsedCount;
                timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
                timedEvent.OnKilled += (cbName) => Timers.Remove(cbName);
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
            TimedEvent result = Timers.ContainsKey(name) ? Timers[name] : null;
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
        /// <param name="autoReset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        public TimedEvent CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args, bool autoReset = false)
        {
            Util.GetUtil().ThreadTimerCheck();
            GameObject go = new GameObject($"ParallelTimedEvent_{name}_{UnityEngine.Random.Range(1, 999999)}");
            UnityEngine.Object.DontDestroyOnLoad(go);
            TimedEvent timedEvent = go.AddComponent<TimedEvent>();

            timedEvent.Name = name;
            timedEvent.PluginName = Name;
            timedEvent.Interval = timeoutDelay;
            timedEvent.Args = args;
            timedEvent.AutoReset = autoReset;
            timedEvent.OnFire += BaseOnTimerCB;
            timedEvent.OnKilled += (cbName) => Timers.Remove(cbName);
    
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
        /// <param name="callback">The callback function.</param>
        /// <param name="autoReset">True if the timer should raise the elapsed event each time it elapses, false if only once.</param>
        /// <param name="maxElapsedCount">Optional: Max fires before killing. 0 = infinite.</param>
        public TimedEvent CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args,
            Action<TimedEvent> callback, bool autoReset = false, int maxElapsedCount = 0)
        {
            Util.GetUtil().ThreadTimerCheck();
            GameObject go = new GameObject($"ParallelTimedEvent_{name}_{UnityEngine.Random.Range(1, 999999)}");
            UnityEngine.Object.DontDestroyOnLoad(go);
            TimedEvent timedEvent = go.AddComponent<TimedEvent>();

            timedEvent.Name = name;
            timedEvent.PluginName = Name;
            timedEvent.Interval = timeoutDelay;
            timedEvent.Args = args;
            timedEvent.AutoReset = autoReset;
            timedEvent.MaxElapsedCount = maxElapsedCount;
            timedEvent.OnFire += new TimedEvent.TimedEventFireDelegate(callback);
            timedEvent.OnKilled += (cbName) => Timers.Remove(cbName);
    
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
            return ParallelTimers.Where(timer => timer.Name == name).ToList();
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

        /// <summary>
        /// Sends a synchronous message to a target plugin.
        /// </summary>
        /// <param name="targetName">The name of the target plugin.</param>
        /// <param name="message">The object payload being sent.</param>
        /// <returns>A <see cref="PluginMessageResult"/> indicating the delivery status.</returns>
        public PluginMessageResult SendMessage(string targetName, object message)
        {
            return PluginMessaging.Send(Name, targetName, message);
        }

        /// <summary>
        /// Sends an asynchronous message with a callback.
        /// </summary>
        /// <param name="targetName">The name of the target plugin.</param>
        /// <param name="message">The object payload.</param>
        /// <param name="callback">Action executed when finished, providing the encapsulated result.</param>
        /// <param name="runInThreadPool">If true, dispatch occurs on a background thread.</param>
        public void SendMessageAsync(string targetName, object message, Action<PluginMessageResult> callback, bool runInThreadPool = true)
        {
            PluginMessaging.SendAsync(Name, targetName, message, callback, runInThreadPool);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ScriptWebSocket"/> class with the specified socket ID and URL.
        /// </summary>
        /// <param name="socketId">The unique identifier for the WebSocket.</param>
        /// <param name="url">The destination URL for the WebSocket connection.</param>
        /// <returns>A new instance of the <see cref="ScriptWebSocket"/>.</returns>
        public ScriptWebSocket CreateWebSocket(string socketId, string url)
        {
            ScriptWebSocket socket = new ScriptWebSocket(Name, socketId, url);
            WebSockets.Add(socket);
            return socket;
        }

        /// <summary>
        /// Removes and disposes of a WebSocket connection associated with the specified identifier.
        /// </summary>
        /// <param name="socketId">The unique identifier of the WebSocket to remove.</param>
        /// <returns>True if the WebSocket was found and successfully removed, otherwise, false.</returns>
        public bool RemoveWebSocket(string socketId)
        {
            ScriptWebSocket socket = WebSockets.FirstOrDefault(s => s.SocketId == socketId);
            if (socket != null)
            {
                WebSockets.Remove(socket);
                socket.Dispose();
                return true;
            }

            return false;
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

        public void BaseOnTablesLoaded(Dictionary<string, LootSpawnList> tables)
        {
            Invoke(PluginLoaderEvents.OnTablesLoaded, tables);
        }

        public void BaseOnAllPluginsLoaded()
        {
            Invoke(PluginLoaderEvents.OnAllPluginsLoaded);
        }

        public void BaseOnBlueprintUse(Player player, BPUseEvent evt)
        {
            Invoke(PluginLoaderEvents.OnBlueprintUse, player, evt);
        }

        public void BaseOnChat(Player player, ref ChatString text)
        {
            Invoke(PluginLoaderEvents.OnChat, player, text);
        }

        public void BaseOnCommand(Player player, string command, string[] args)
        {
            if (CommandList.Count != 0 && !CommandList.Contains(command) &&
                !Server.ForceCallForCommands.Contains(command))
            {
                return;
            }

            Invoke(PluginLoaderEvents.OnCommand, player, command, args);
        }

        public void BaseOnConsole(ref ConsoleSystem.Arg arg, bool external)
        {
            string clss = arg.Class.ToLower();
            string func = arg.Function.ToLower();
            if (!external)
            {
                Player player = Server.GetServer().FindPlayer(arg.argUser.userID);
                arg.ReplyWith($"{player.Name} executed: {clss}.{func}");
                Invoke(PluginLoaderEvents.OnConsole, player, arg);
            }
            else
            {
                arg.ReplyWith($"Rcon: {clss}.{func}");
                Invoke(PluginLoaderEvents.OnConsole, null, arg);
            }
        }
        
        public void BaseOnConsoleWithCancel(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent consoleEvent)
        {
            string clss = arg.Class.ToLower();
            string func = arg.Function.ToLower();
            if (!external)
            {
                Player player = Server.GetServer().FindPlayer(arg.argUser.userID);
                arg.ReplyWith($"{player.Name} executed: {clss}.{func}");
                Invoke(PluginLoaderEvents.OnConsoleWithCancel, player, arg, consoleEvent);
            }
            else
            {
                arg.ReplyWith($"Rcon: {clss}.{func}");
                Invoke(PluginLoaderEvents.OnConsoleWithCancel, null, arg, consoleEvent);
            }
        }

        public void BaseOnDoorUse(Player player, DoorEvent evt)
        {
            Invoke(PluginLoaderEvents.OnDoorUse, player, evt);
        }

        public void BaseOnEntityDecay(DecayEvent evt)
        {
            Invoke(PluginLoaderEvents.OnEntityDecay, evt);
        }

        public void BaseOnEntityDeployed(Player player, Entity entity, Player actualplacer)
        {
            try
            {
                Invoke(PluginLoaderEvents.OnEntityDeployed, player, entity, actualplacer);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    $"[IronPython] Error in plugin {Name} when invoking On_EntityDeployed ensure you have 3 parameters:{ex}");
            }
        }

        public void BaseOnEntityDestroyed(DestroyEvent evt)
        {
            Invoke(PluginLoaderEvents.OnEntityDestroyed, evt);
        }

        public void BaseOnEntityHurt(HurtEvent evt)
        {
            Invoke(PluginLoaderEvents.OnEntityHurt, evt);
        }

        public void BaseOnItemsLoaded(ItemsBlocks items)
        {
            Invoke(PluginLoaderEvents.OnItemsLoaded, items);
        }

        public void BaseOnNPCHurt(HurtEvent evt)
        {
            Invoke(PluginLoaderEvents.OnNPCHurt, evt);
        }

        public void BaseOnNPCKilled(DeathEvent evt)
        {
            Invoke(PluginLoaderEvents.OnNPCKilled, evt);
        }

        public void BaseOnPlayerConnected(Player player)
        {
            Invoke(PluginLoaderEvents.OnPlayerConnected, player);
        }

        public void BaseOnPlayerDisconnected(Player player)
        {
            Invoke(PluginLoaderEvents.OnPlayerDisconnected, player);
        }

        public void BaseOnPlayerGathering(Player player, GatherEvent evt)
        {
            Invoke(PluginLoaderEvents.OnPlayerGathering, player, evt);
        }

        public void BaseOnPlayerHurt(HurtEvent evt)
        {
            Invoke(PluginLoaderEvents.OnPlayerHurt, evt);
        }

        public void BaseOnPlayerKilled(DeathEvent evt)
        {
            Invoke(PluginLoaderEvents.OnPlayerKilled, evt);
        }

        public void BaseOnPlayerTeleport(Player player, Vector3 from, Vector3 dest)
        {
            Invoke(PluginLoaderEvents.OnPlayerTeleport, player, from, dest);
        }

        public void BaseOnPlayerSpawn(Player player, SpawnEvent evt)
        {
            Invoke(PluginLoaderEvents.OnPlayerSpawning, player, evt);
        }

        public void BaseOnPlayerSpawned(Player player, SpawnEvent evt)
        {
            Invoke(PluginLoaderEvents.OnPlayerSpawned, player, evt);
        }

        public void BaseOnResearch(ResearchEvent evt)
        {
            Invoke(PluginLoaderEvents.OnResearch, evt);
        }

        public void BaseOnServerInit()
        {
            Invoke(PluginLoaderEvents.OnServerInit);
        }

        public void BaseOnServerShutdown()
        {
            Invoke(PluginLoaderEvents.OnServerShutdown);
        }

        public void BaseOnServerSaved(int amount, double seconds)
        {
            Invoke(PluginLoaderEvents.OnServerSaved, amount, seconds);
        }

        public void BaseOnCrafting(CraftingEvent e)
        {
            Invoke(PluginLoaderEvents.OnCrafting, e);
        }

        public void BaseOnResourceSpawned(ResourceTarget t)
        {
            Invoke(PluginLoaderEvents.OnResourceSpawn, t);
        }

        public void BaseOnItemAdded(InventoryModEvent e)
        {
            Invoke(PluginLoaderEvents.OnItemAdded, e);
        }

        public void BaseOnItemRemoved(InventoryModEvent e)
        {
            Invoke(PluginLoaderEvents.OnItemRemoved, e);
        }

        public void BaseOnItemPickup(ItemPickupEvent e)
        {
            Invoke(PluginLoaderEvents.OnItemPickup, e);
        }

        public void BaseOnFallDamage(FallDamageEvent e)
        {
            Invoke(PluginLoaderEvents.OnFallDamage, e);
        }

        public void BaseOnAirdrop(Vector3 v)
        {
            Invoke(PluginLoaderEvents.OnAirdrop, v);
        }

        public void BaseOnAirdropCrateDropped(SupplyDropPlane plane, Entity supplyCrate)
        {
            Invoke(PluginLoaderEvents.OnAirdropCrateDropped, plane, supplyCrate);
        }

        public void BaseOnSteamDeny(SteamDenyEvent e)
        {
            Invoke(PluginLoaderEvents.OnSteamDeny, e);
        }

        public void BaseOnPlayerApproval(PlayerApprovalEvent e)
        {
            Invoke(PluginLoaderEvents.OnPlayerApproval, e);
        }

        public void BaseOnPluginShutdown()
        {
            Invoke(PluginLoaderEvents.OnPluginShutdown);
        }

        public void BaseOnShowTalker(uLink.NetworkPlayer np, Player player)
        {
            Invoke(PluginLoaderEvents.OnVoiceChat, np, player);
        }

        public void BaseOnLootUse(LootStartEvent le)
        {
            Invoke(PluginLoaderEvents.OnLootUse, le);
        }

        public void BaseOnBanEvent(BanEvent be)
        {
            Invoke(PluginLoaderEvents.OnPlayerBan, be);
        }

        public void BaseOnRepairBench(Fougerite.Events.RepairEvent be)
        {
            Invoke(PluginLoaderEvents.OnRepairBench, be);
        }

        public void BaseOnItemMove(ItemMoveEvent be)
        {
            Invoke(PluginLoaderEvents.OnItemMove, be);
        }

        public void BaseOnGenericSpawnLoad(GenericSpawner gs)
        {
            Invoke(PluginLoaderEvents.OnGenericSpawnLoad, gs);
        }

        public void BaseOnServerLoaded()
        {
            Invoke(PluginLoaderEvents.OnServerLoaded);
        }

        public void BaseOnSupplySignalExploded(SupplySignalExplosionEvent evt)
        {
            Invoke(PluginLoaderEvents.OnSupplySignalExploded, evt);
        }

        public void BaseOnPlayerMove(HumanController hc, Vector3 v, int p, ushort p2,
            uLink.NetworkMessageInfo networkMessageInfo, Util.PlayerActions action)
        {
            Invoke(PluginLoaderEvents.OnPlayerMove, hc, v, p, p2, networkMessageInfo, action);
        }

        public void BaseOnBeltUse(BeltUseEvent ev)
        {
            Invoke(PluginLoaderEvents.OnBeltUse, ev);
        }

        public void BaseOnLogger(LoggerEvent ev)
        {
            Invoke(PluginLoaderEvents.OnLogger, ev);
        }

        public void BaseOnGrenade(GrenadeThrowEvent ev)
        {
            Invoke(PluginLoaderEvents.OnGrenadeThrow, ev);
        }

        public void BaseOnSupplyDropPlaneCreated(SupplyDropPlane plane)
        {
            Invoke(PluginLoaderEvents.OnSupplyDropPlaneCreated, plane);
        }
        
        public void BaseOnNPCSpawn(NPC npc)
        {
            Invoke(PluginLoaderEvents.OnNPCSpawned, npc);
        }

        public void BaseOnTimedExplosiveSpawned(TimedExplosiveEvent ev)
        {
            Invoke(PluginLoaderEvents.OnTimedExplosiveSpawned, ev);
        }

        public void BaseOnSleeperSpawned(Sleeper sleeper)
        {
            Invoke(PluginLoaderEvents.OnSleeperSpawned, sleeper);
        }

        public void BaseOnCommandRestriction(CommandRestrictionEvent ev)
        {
            Invoke(PluginLoaderEvents.OnCommandRestriction, ev);
        }

        public void BaseOnFireBarrelToggle(FireBarrelToggleEvent ev)
        {
            Invoke(PluginLoaderEvents.OnFireBarrelToggle, ev);
        }

        public void BaseOnDayCycleChange(DayCycleChangeEvent ev)
        {
            Invoke(PluginLoaderEvents.OnDayCycleChanged, ev);
        }

        public void BaseOnShoot(ShootEvent ev)
        {
            Invoke(PluginLoaderEvents.OnShoot, ev);
        }

        public void BaseOnShotgunShoot(ShotgunShootEvent ev)
        {
            Invoke(PluginLoaderEvents.OnShotgunShoot, ev);
        }

        public void BaseOnBowShoot(BowShootEvent ev)
        {
            Invoke(PluginLoaderEvents.OnBowShoot, ev);
        }

        public void BaseOnAnimalMovement(AnimalMovementEvent ev)
        {
            Invoke(PluginLoaderEvents.OnAnimalMovement, ev);
        }

        public void BaseOnConsumableUse(ConsumableUseEvent ev)
        {
            Invoke(PluginLoaderEvents.OnConsumableUse, ev);
        }

        public void BaseOnMedikitUse(MedikitUseEvent ev)
        {
            Invoke(PluginLoaderEvents.OnMedikitUse, ev);
        }

        public void BaseOnItemModInstall(ItemModInstallEvent<BulletWeaponDataBlock> ev)
        {
            Invoke(PluginLoaderEvents.OnItemModInstall, ev);
        }

        public void BaseOnBloodDraw(BloodDrawEvent ev)
        {
            Invoke(PluginLoaderEvents.OnBloodDraw, ev);
        }
        
        public void BaseOnArmorEquip(ArmorEquipEvent ev)
        {
            Invoke(PluginLoaderEvents.OnArmorEquip, ev);
        }

        public void BaseOnArmorUnEquip(ArmorEquipEvent ev)
        {
            Invoke(PluginLoaderEvents.OnArmorUnEquip, ev);
        }

        public void BaseOnFlareThrow(FlareThrowEvent ev)
        {
            Invoke(PluginLoaderEvents.OnFlareThrow, ev);
        }

        public void FlareIgnite(FlareIgniteEvent ev)
        {
            Invoke(PluginLoaderEvents.OnFlareIgnite, ev);
        }

        public void BaseOnTorchIgnite(BasicTorchIgniteEvent ev)
        {
            Invoke(PluginLoaderEvents.OnTorchIgnite, ev);
        }

        public void BaseOnHeatZoneEnter(HeatZoneEnterEvent ev)
        {
            Invoke(PluginLoaderEvents.OnHeatZoneEnter, ev);
        }

        public void BaseOnWorkZoneEnter(WorkZoneEnterEvent ev)
        {
            Invoke(PluginLoaderEvents.OnWorkZoneEnter, ev);
        }

        public void BaseOnPluginMessage(PluginMessageEvent ev)
        {
            Invoke(PluginLoaderEvents.OnPluginMessage, ev);
        }

        public void BaseOnCraftingCancel(CraftCancelEvent ev)
        {
            Invoke(PluginLoaderEvents.OnCraftingCancel, ev);
        }

        public void BaseOnCraftingComplete(CraftCompleteEvent ev)
        {
            Invoke(PluginLoaderEvents.OnCraftingComplete, ev);
        }

        public void BaseOnServerTick()
        {
            Invoke(PluginLoaderEvents.OnServerTick);
        }

        public void BaseOnMetabolismUpdate(MetabolismEvent ev)
        {
            Invoke(PluginLoaderEvents.OnMetabolismUpdate, ev);
        }
        
        public void BaseOnWebSocketMessage(WebSocketEvent ev)
        {
            Invoke(PluginLoaderEvents.OnWebSocketMessage, ev);
        }

        public void BaseOnWebSocketConnected(WebSocketEvent ev)
        {
            Invoke(PluginLoaderEvents.OnWebSocketConnected, ev);
        }

        public void BaseOnWebSocketClosed(WebSocketEvent ev)
        {
            Invoke(PluginLoaderEvents.OnWebSocketClosed, ev);
        }

        public void BaseOnWebSocketError(WebSocketEvent ev)
        {
            Invoke(PluginLoaderEvents.OnWebSocketError, ev);
        }

        public void BaseOnPermissionChange(PermissionEvent ev)
        {
            Invoke(PluginLoaderEvents.OnPermissionChange, ev);
        }
    }
}