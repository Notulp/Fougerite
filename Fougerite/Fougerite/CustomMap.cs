using System;
using System.Diagnostics;
using System.IO;
using Fougerite.Concurrent;
using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// Where the server's custom map is in its lifecycle.
    /// </summary>
    public enum CustomMapState
    {
        /// <summary>No plugin has claimed the map. The -map level loads as usual.</summary>
        None = 0,

        /// <summary>A plugin claimed the map. The level load waits for it.</summary>
        Pending = 1,

        /// <summary>A scene was committed. It is loaded instead of the -map level.</summary>
        Committed = 2,

        /// <summary>The owner gave up. The -map level loads.</summary>
        Abandoned = 3,

        /// <summary>The owner failed or the claim timed out. The server shuts down.</summary>
        Failed = 4,

        /// <summary>RustLevel.Load has been started with the committed scene.</summary>
        Loading = 5,

        /// <summary>The level finished loading and OnCustomMapLoaded has fired.</summary>
        Loaded = 6
    }

    /// <summary>
    /// The owner's handle to the server's custom map. Only one exists.
    /// </summary>
    public sealed class CustomMapHandle
    {
        private readonly string _owner;

        internal CustomMapHandle(string owner)
        {
            _owner = owner;
        }

        /// <summary>The name passed to Claim.</summary>
        public string Owner
        {
            get { return _owner; }
        }

        /// <summary>False if this handle no longer owns the map.</summary>
        public bool IsValid
        {
            get { return CustomMap.GetInstance().IsCurrent(this); }
        }

        /// <summary>The state of the claim.</summary>
        public CustomMapState State
        {
            get { return CustomMap.GetInstance().State; }
        }

        /// <summary>The level given with -map.</summary>
        public string ServerLevelName
        {
            get { return CustomMap.GetInstance().ServerLevelName; }
        }

        /// <summary>Gives the owner more time, counted from now.</summary>
        public bool ExtendTimeout(float seconds)
        {
            return CustomMap.GetInstance().ExtendTimeout(this, seconds);
        }

        /// <summary>
        /// Loads sceneName from bundle instead of the -map level. The bundle must already be
        /// loaded. Fougerite owns it from now on: do not Unload it.
        /// </summary>
        public bool Commit(AssetBundle bundle, string sceneName)
        {
            return CustomMap.GetInstance().Commit(this, bundle, sceneName);
        }

        /// <summary>
        /// Loads the bundle at path with AssetBundle.CreateFromFile and commits sceneName from
        /// it. Fails the claim if the file is missing or not a valid bundle. Meant for script
        /// plugins that cannot easily handle an AssetBundle themselves.
        /// </summary>
        public bool CommitFile(string path, string sceneName)
        {
            return CustomMap.GetInstance().CommitFile(this, path, sceneName);
        }

        /// <summary>Gives up the claim. The -map level loads.</summary>
        public bool Abandon(string reason)
        {
            return CustomMap.GetInstance().Abandon(this, reason);
        }

        /// <summary>The server must not start without this map. Shuts the server down.</summary>
        public bool Fail(string reason)
        {
            return CustomMap.GetInstance().Fail(this, reason);
        }
    }

    /// <summary>
    /// Lets one plugin replace the server's map with a scene from its own asset bundle.
    /// This is the server half of RustBuster's client-side CustomMap: both sides must load
    /// the same scene from the same bundle.
    ///
    /// WHAT YOU NEED TO PREPARE
    /// ------------------------
    /// 1. The bundle. The same streamed scene bundle you give the clients, built in Unity
    ///    4.5.5f1 for StandaloneWindows. The scene name you commit is the .unity file name
    ///    without its extension, not the bundle's file name. Keep mesh colliders readable, the
    ///    server needs their mesh data.
    ///
    /// 2. The file on the server. Put the bundle anywhere the server can read it, for example
    ///    next to your plugin. It is loaded from disk, nothing is downloaded on the server.
    ///
    /// 3. The clients. Add the same file to the RustBuster downloadables and ship a client
    ///    plugin that commits the same scene. A client on a different map than the server is
    ///    not playable: collision, building placement, spawns and animals all run on the
    ///    server's scene.
    ///
    /// 4. The save. The world save is restored onto whatever map loads. A save made on another
    ///    map restores its buildings at the same coordinates on yours. Give a custom map its
    ///    own -datadir.
    ///
    /// HOW THE LOAD WORKS
    /// ------------------
    /// The level load waits until every plugin has loaded, and then, if a plugin claimed the
    /// map, until that plugin has committed, abandoned or failed. Only then is the level
    /// loaded. Nothing of the -map level is loaded before that point.
    ///
    /// The "yourscene-TREES" scene is loaded on top if it can be, exactly like the stock map
    /// does with its own trees scene.
    ///
    /// EXAMPLE
    /// -------
    ///     public override void Initialize()
    ///     {
    ///         CustomMapHandle map = CustomMap.GetInstance().Claim(Name, 120f);
    ///         if (map == null)
    ///         {
    ///             return; // Another plugin owns the map, or the level is already loaded.
    ///         }
    ///
    ///         Hooks.OnCustomMapLoaded += OnCustomMapLoaded;
    ///
    ///         // CommitFile fails the claim, and so stops the server, if the bundle is
    ///         // missing or broken. Use Commit with your own AssetBundle if you would rather
    ///         // Abandon and fall back to the -map level.
    ///         map.CommitFile(Path.Combine(ModuleFolder, "hapis_island.unity3d"), "hapis_island");
    ///     }
    ///
    ///     private void OnCustomMapLoaded(CustomMapLoadedEvent ev)
    ///     {
    ///         if (!ev.IsCustomMap)
    ///         {
    ///             return;
    ///         }
    ///
    ///         // The scene and the save are in. OnServerLoaded fires right after this.
    ///     }
    ///
    /// RULES
    /// -----
    ///   - One map per server start. The first Claim wins and every later one returns null.
    ///   - Claim while the plugins load. A plugin reloaded after the level loaded gets null.
    ///   - Commit, Abandon and Fail are final. Pending is the only state they can leave.
    ///   - If none of them is called within the timeout, the server shuts down.
    ///   - Do not Unload the bundle you committed.
    /// </summary>
    public sealed class CustomMap
    {
        private static readonly Lazy<CustomMap> Instance = new Lazy<CustomMap>(() => new CustomMap());

        /// <summary>Claim timeout used when none is given.</summary>
        public const float DefaultTimeoutSeconds = 120f;

        /// <summary>The longest a claim may hold the load, in seconds.</summary>
        public const float MaxTimeoutSeconds = 600f;

        /// <summary>How long the load waits for the plugins before it gives up on them.</summary>
        public const float PluginWaitSeconds = 60f;

        private readonly object Sync = new object();
        private readonly Stopwatch ClaimTimer = new Stopwatch();
        private readonly Stopwatch PluginTimer = new Stopwatch();

        private bool _windowOpen = true;
        private CustomMapState _state;
        private CustomMapHandle _handle;
        private string _serverLevel;
        private string _sceneName;
        private AssetBundle _bundle;
        private float _timeoutSeconds = DefaultTimeoutSeconds;
        private bool _waitingLogged;
        private bool _quitQueued;

        private CustomMap()
        {
        }

        /// <summary>
        /// Returns the CustomMap's instance.
        /// </summary>
        /// <returns></returns>
        public static CustomMap GetInstance()
        {
            return Instance.Value;
        }

        /// <summary>Where the server's map is in its lifecycle.</summary>
        public CustomMapState State
        {
            get
            {
                lock (Sync) return _state;
            }
        }

        /// <summary>True if a plugin owns the map.</summary>
        public bool IsClaimed
        {
            get
            {
                lock (Sync) return _handle != null;
            }
        }

        /// <summary>True while Claim would succeed.</summary>
        public bool CanClaim
        {
            get
            {
                lock (Sync) return _windowOpen && _handle == null;
            }
        }

        /// <summary>The owner of the map, or null.</summary>
        public string Owner
        {
            get
            {
                lock (Sync) return _handle != null ? _handle.Owner : null;
            }
        }

        /// <summary>The level given with -map.</summary>
        public string ServerLevelName
        {
            get
            {
                lock (Sync) return _serverLevel ?? global::server.map;
            }
        }

        /// <summary>The committed scene, or null.</summary>
        public string SceneName
        {
            get
            {
                lock (Sync) return _sceneName;
            }
        }

        /// <summary>
        /// Claims the server's map. Returns null if another plugin already owns it, or if the
        /// level has already been chosen. Call it while your plugin loads.
        /// </summary>
        public CustomMapHandle Claim(string owner)
        {
            return Claim(owner, DefaultTimeoutSeconds);
        }

        /// <summary>
        /// Claims the server's map. Returns null if another plugin already owns it, or if the
        /// level has already been chosen. Call it while your plugin loads.
        /// </summary>
        /// <param name="owner">Your plugin name, for the logs.</param>
        /// <param name="timeoutSeconds">How long the load may wait for you.</param>
        public CustomMapHandle Claim(string owner, float timeoutSeconds)
        {
            if (string.IsNullOrEmpty(owner))
            {
                owner = "Unknown";
            }

            CustomMapHandle handle;
            lock (Sync)
            {
                if (!_windowOpen)
                {
                    Logger.LogWarning($"[CustomMap] {owner} tried to claim the map, but the level was already chosen.");
                    return null;
                }

                if (_handle != null)
                {
                    Logger.LogWarning($"[CustomMap] {owner} tried to claim the map, but {_handle.Owner} already owns it.");
                    return null;
                }

                handle = new CustomMapHandle(owner);
                _handle = handle;
                _state = CustomMapState.Pending;
                _timeoutSeconds = ClampTimeout(timeoutSeconds);
                ClaimTimer.Reset();
                ClaimTimer.Start();
            }

            Logger.Log($"[CustomMap] {owner} claimed the map, holding the level load for up to {ClampTimeout(timeoutSeconds):F0}s.");
            return handle;
        }

        internal bool IsCurrent(CustomMapHandle handle)
        {
            lock (Sync) return handle != null && ReferenceEquals(handle, _handle);
        }

        internal bool ExtendTimeout(CustomMapHandle handle, float seconds)
        {
            lock (Sync)
            {
                if (!ReferenceEquals(handle, _handle) || _state != CustomMapState.Pending) return false;
                _timeoutSeconds = ClampTimeout((float)ClaimTimer.Elapsed.TotalSeconds + seconds);
                return true;
            }
        }

        internal bool Commit(CustomMapHandle handle, AssetBundle bundle, string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Logger.LogError("[CustomMap] Commit called without a scene name.");
                return false;
            }

            if (bundle == null)
            {
                Logger.LogError($"[CustomMap] Commit for {sceneName} was given a null or unloaded bundle.");
                return false;
            }

            lock (Sync)
            {
                if (!ReferenceEquals(handle, _handle))
                {
                    return false;
                }

                if (!_windowOpen || _state != CustomMapState.Pending)
                {
                    Logger.LogWarning($"[CustomMap] Commit refused, the claim is {_state} and can no longer change the level.");
                    return false;
                }

                _bundle = bundle;
                _sceneName = sceneName;
                _state = CustomMapState.Committed;
                ClaimTimer.Stop();
            }

            Logger.Log($"[CustomMap] {handle.Owner} committed scene {sceneName}.");
            return true;
        }

        internal bool CommitFile(CustomMapHandle handle, string path, string sceneName)
        {
            if (!IsCurrent(handle))
            {
                return false;
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return Fail(handle, $"map bundle not found: {path}");
            }

            AssetBundle bundle;
            try
            {
                bundle = AssetBundle.CreateFromFile(path);
            }
            catch (Exception ex)
            {
                return Fail(handle, $"could not load {path}: {ex.Message}");
            }

            if (bundle == null)
            {
                return Fail(handle, $"{path} is not a valid bundle, or was built with the wrong Unity version");
            }

            if (Commit(handle, bundle, sceneName))
            {
                return true;
            }

            bundle.Unload(false);
            return false;
        }

        internal bool Abandon(CustomMapHandle handle, string reason)
        {
            lock (Sync)
            {
                if (!ReferenceEquals(handle, _handle) || _state != CustomMapState.Pending) return false;
                _state = CustomMapState.Abandoned;
                ClaimTimer.Stop();
            }

            Logger.LogWarning($"[CustomMap] {handle.Owner} abandoned the map ({reason ?? "no reason"}), loading the -map level.");
            return true;
        }

        internal bool Fail(CustomMapHandle handle, string reason)
        {
            lock (Sync)
            {
                if (!ReferenceEquals(handle, _handle)) return false;
                if (_state != CustomMapState.Pending && _state != CustomMapState.Committed) return false;
            }

            FailInternal($"{handle.Owner}: {reason ?? "the map could not be loaded"}");
            return true;
        }

        /// <summary>Polled by Hooks.ServerLoadedHook every frame before RustLevel.Load.</summary>
        internal bool CanBeginLevelLoad(string serverLevel)
        {
            if (!Hooks.AllPluginsLoadedOnce)
            {
                if (!PluginTimer.IsRunning) PluginTimer.Start();

                if (PluginTimer.Elapsed.TotalSeconds < PluginWaitSeconds)
                {
                    if (!_waitingLogged)
                    {
                        _waitingLogged = true;
                        Logger.Log("[CustomMap] Waiting for the plugins before loading the level.");
                    }
                    return false;
                }

                if (PluginTimer.IsRunning)
                {
                    PluginTimer.Stop();
                    Logger.LogWarning($"[CustomMap] The plugins did not finish loading within {PluginWaitSeconds:F0}s, loading the level anyway.");
                }
            }

            CustomMapState state;
            bool timedOut;
            string owner;
            float timeout;

            lock (Sync)
            {
                if (_serverLevel == null) _serverLevel = serverLevel;
                state = _state;
                owner = _handle != null ? _handle.Owner : null;
                timeout = _timeoutSeconds;
                timedOut = state == CustomMapState.Pending && ClaimTimer.Elapsed.TotalSeconds > _timeoutSeconds;
            }

            if (timedOut)
            {
                FailInternal($"{owner} did not load its map within {timeout:F0}s");
                return false;
            }

            return state != CustomMapState.Pending && state != CustomMapState.Failed;
        }

        /// <summary>Called by Hooks.ServerLoadedHook right before RustLevel.Load.</summary>
        internal string ResolveLevelName(string serverLevel)
        {
            string result;
            string owner;
            CustomMapState state;

            lock (Sync)
            {
                _windowOpen = false;
                if (_serverLevel == null) _serverLevel = serverLevel;
                state = _state;
                owner = _handle != null ? _handle.Owner : null;

                if (state == CustomMapState.Committed)
                {
                    _state = CustomMapState.Loading;
                    result = _sceneName;
                }
                else
                {
                    result = serverLevel;
                }
            }

            if (state == CustomMapState.Committed)
            {
                Logger.Log($"[CustomMap] Loading {result} from {owner} instead of {serverLevel}.");
            }
            else if (!Application.CanStreamedLevelBeLoaded(serverLevel))
            {
                Logger.LogError($"[CustomMap] -map {serverLevel} is not a level of the game and no plugin committed it.");
            }

            return result;
        }

        /// <summary>Called by Hooks.ServerLoadedHook right after RustLevel.Load.</summary>
        internal Events.CustomMapLoadedEvent LevelLoaded(string loadedLevel)
        {
            lock (Sync)
            {
                bool custom = _state == CustomMapState.Loading;
                if (custom) _state = CustomMapState.Loaded;

                return new Events.CustomMapLoadedEvent(
                    loadedLevel,
                    _serverLevel,
                    custom ? _handle.Owner : null,
                    custom ? _bundle : null,
                    custom);
            }
        }

        private float ClampTimeout(float seconds)
        {
            if (float.IsNaN(seconds) || seconds <= 0f) return DefaultTimeoutSeconds;
            return seconds > MaxTimeoutSeconds ? MaxTimeoutSeconds : seconds;
        }

        private void FailInternal(string reason)
        {
            lock (Sync)
            {
                _state = CustomMapState.Failed;
                ClaimTimer.Stop();

                if (_quitQueued) return;
                _quitQueued = true;
            }

            // Quitting before the level is loaded never reaches the save in OnServerShutdown,
            // because ServerLoaded is still false.
            Logger.LogError($"[CustomMap] {reason}. Shutting the server down.");
            Application.Quit();
        }
    }
}