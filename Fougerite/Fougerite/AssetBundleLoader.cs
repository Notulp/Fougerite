using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fougerite.Concurrent;
using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// Loads plugin asset bundles and assets for RustBuster plugins through one shared, simple API.
    /// <code>
    /// AssetBundleLoader loader = AssetBundleLoader.GetAssetBundleLoader();
    ///
    /// // Queue the plugin bundle. Bundles are downloaded one after another, never in parallel.
    /// loader.LoadBundle(heliBundleUrl, 0);
    ///
    /// // Load assets without blocking. Each callback runs on the main thread once the asset is available,
    /// // which includes waiting for the bundle queued above and for the game's own bundles.
    /// loader.LoadAsync&lt;GameObject&gt;("assets/prefabs/helicopter/Mi24", prefab =&gt; heliPrefab = prefab);
    /// loader.LoadAsync&lt;GameObject&gt;("assets/prefabs/helicopter/HellFireMisil", prefab =&gt;
    /// {
    ///     if (prefab == null)
    ///     {
    ///         Debug.LogError("[HeliAI] HellFireMisil prefab not found");
    ///         return;
    ///     }
    ///
    ///     if (prefab.GetComponent&lt;Misil&gt;() == null)
    ///     {
    ///         prefab.AddComponent&lt;Misil&gt;();
    ///     }
    ///     hellFireMisil = prefab;
    /// });
    ///
    /// // Once the bundles are loaded, plain synchronous lookups work as well.
    /// GameObject explosion = loader.Load&lt;GameObject&gt;("assets/prefabs/helicopter/longexplosion");
    /// </code>
    ///
    /// <para>
    /// Asset lookups search every bundle loaded through this class first, in load order, and then the game's own
    /// bundles through <c>Facepunch.Bundling</c>. A plugin bundle can therefore provide new assets as well as replace
    /// a game asset for code that loads through this class.
    /// </para>
    ///
    /// <para>
    /// Plugins that call <c>Facepunch.Bundling.Load</c> directly instead can make their bundle visible to it with
    /// <see cref="LoadAndInjectBundle"/>.
    /// </para>
    ///
    /// <para>
    /// Bundles are loaded one at a time because several plugins starting their downloads at the end of the loading
    /// screen would otherwise cause one large managed heap spike while Mono is busy compiling player code.
    /// The queue runs on the <see cref="Loom"/> MonoBehaviour and needs no management by plugins.
    /// Every member of this class has to be used from the main thread.
    /// </para>
    /// </summary>
    public class AssetBundleLoader
    {
        private sealed class BundleRequest
        {
            internal readonly string Path;
            internal readonly int Version;
            internal readonly Action<AssetBundle> Callback;

            internal BundleRequest(string path, int version, Action<AssetBundle> callback)
            {
                Path = path;
                Version = version;
                Callback = callback;
            }
        }

        private static readonly Lazy<AssetBundleLoader> Instance = new Lazy<AssetBundleLoader>(() => new AssetBundleLoader());
        private readonly object _lock = new object();
        private readonly Queue<BundleRequest> _queue = new Queue<BundleRequest>();
        private readonly List<AssetBundle> _loadedBundles = new List<AssetBundle>();
        private int _enqueuedCount;
        private int _completedCount;
        private bool _running;

        /// <summary>
        /// Returns the shared loader instance used by every plugin.
        /// <code>
        /// AssetBundleLoader loader = AssetBundleLoader.GetAssetBundleLoader();
        /// </code>
        /// </summary>
        /// <returns>The shared loader.</returns>
        public static AssetBundleLoader GetAssetBundleLoader()
        {
            return Instance.Value;
        }

        /// <summary>
        /// Queues a bundle for loading and reports the result through a callback.
        /// <code>
        /// loader.LoadBundle(heliBundleUrl, 0, bundle =&gt;
        /// {
        ///     if (bundle == null)
        ///     {
        ///         Debug.LogError("[HeliAI] Heli bundle failed to load");
        ///     }
        /// });
        /// </code>
        ///
        /// <para>
        /// The bundle is loaded with <c>WWW.LoadFromCacheOrDownload</c> after every previously queued bundle finished,
        /// so at most one download is in flight at any time. A successfully loaded bundle is registered for
        /// <see cref="Load{T}(string)"/> and <see cref="LoadAsync{T}(string, Action{T})"/> before the callback runs.
        /// </para>
        /// </summary>
        /// <param name="path">The URL of the bundle. Local files are addressed with the file scheme.</param>
        /// <param name="version">The cache version. Pass 0 to bypass the cache, which is the right choice for local files.</param>
        /// <param name="done">
        /// Callback invoked on the main thread with the loaded bundle, or with null when loading failed.
        /// It is always invoked exactly once.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="done"/> is null.</exception>
        public void LoadBundle(string path, int version, Action<AssetBundle> done)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (done == null)
                throw new ArgumentNullException(nameof(done));

            BundleRequest req = new BundleRequest(path, version, done);
            lock (_lock)
            {
                _queue.Enqueue(req);
                _enqueuedCount++;
                if (!_running)
                {
                    _running = true;
                    Loom.Current.StartCoroutine(ProcessQueue());
                }
            }
        }

        /// <summary>
        /// Queues a bundle for loading when the plugin only needs its assets and not the bundle itself.
        /// <code>
        /// loader.LoadBundle(heliBundleUrl, 0);
        /// loader.LoadAsync&lt;GameObject&gt;("assets/prefabs/helicopter/Mi24", prefab =&gt; heliPrefab = prefab);
        /// </code>
        /// Failures are logged. <see cref="LoadAsync{T}(string, Action{T})"/> calls made after this one wait for the bundle.
        /// </summary>
        /// <param name="path">The URL of the bundle. Local files are addressed with the file scheme.</param>
        /// <param name="version">The cache version. Pass 0 to bypass the cache, which is the right choice for local files.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
        public void LoadBundle(string path, int version)
        {
            LoadBundle(path, version, bundle => { });
        }

        /// <summary>
        /// Gets the number of bundles waiting in the queue, not counting the one that is currently loading.
        /// <code>
        /// if (loader.PendingCount == 0)
        /// {
        ///     Debug.Log("No bundles waiting");
        /// }
        /// </code>
        /// </summary>
        public int PendingCount
        {
            get
            {
                lock (_lock)
                    return _queue.Count;
            }
        }

        /// <summary>
        /// Loads an asset immediately from the loaded plugin bundles or from the game's own bundles.
        /// <code>
        /// GameObject heliPrefab = loader.Load&lt;GameObject&gt;("assets/prefabs/helicopter/Mi24");
        /// if (heliPrefab == null)
        /// {
        ///     Debug.LogError("[HeliAI] Mi24 prefab not found");
        /// }
        /// </code>
        ///
        /// <para>
        /// The call does not wait for anything. When the bundle holding the asset is still loading, or the game has not
        /// finished loading its own bundles, the result is null. Use <see cref="LoadAsync{T}(string, Action{T})"/> when
        /// the asset may not be available yet.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The asset type, for example GameObject for prefabs.</typeparam>
        /// <param name="path">The asset path exactly as stored in the bundle.</param>
        /// <returns>The asset, or null when no bundle provides it.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
        public T Load<T>(string path) where T : UnityEngine.Object
        {
            return Load(path, typeof(T)) as T;
        }

        /// <summary>
        /// Loads an asset of a type known only at runtime, with the same rules as <see cref="Load{T}(string)"/>.
        /// <code>
        /// UnityEngine.Object asset = loader.Load("assets/prefabs/helicopter/Mi24", typeof(GameObject));
        /// </code>
        /// </summary>
        /// <param name="path">The asset path exactly as stored in the bundle.</param>
        /// <param name="type">The asset type, which has to derive from UnityEngine.Object.</param>
        /// <returns>The asset, or null when no bundle provides it.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> does not derive from UnityEngine.Object.</exception>
        public UnityEngine.Object Load(string path, Type type)
        {
            ValidateAssetRequest(path, type);

            AssetBundle owner = FindLoadedBundle(path);
            if (owner != null)
            {
                UnityEngine.Object asset = owner.Load(path, type);
                if (asset != null)
                {
                    return asset;
                }
            }

            return Facepunch.Bundling.Loaded ? Facepunch.Bundling.Load(path, type) : null;
        }

        /// <summary>
        /// Loads an asset in the background and hands it to a callback on the main thread.
        /// <code>
        /// loader.LoadAsync&lt;GameObject&gt;("assets/prefabs/helicopter/Mi24", prefab =&gt;
        /// {
        ///     if (prefab == null)
        ///     {
        ///         Debug.LogError("[HeliAI] Mi24 prefab not found");
        ///         return;
        ///     }
        ///     heliPrefab = prefab;
        /// });
        /// </code>
        ///
        /// <para>
        /// Before searching, the call waits for every bundle that was queued before it, so queue the plugin bundle
        /// first and request its assets right after. When none of those bundles holds the asset it waits for the game
        /// to finish loading its own bundles and searches them. The asset itself is read with Unity's asynchronous
        /// bundle loading, so large prefabs do not stall the frame.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The asset type, for example GameObject for prefabs.</typeparam>
        /// <param name="path">The asset path exactly as stored in the bundle.</param>
        /// <param name="done">
        /// Callback invoked on the main thread with the asset, or with null when no bundle provides it.
        /// It is always invoked exactly once.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="done"/> is null.</exception>
        public void LoadAsync<T>(string path, Action<T> done) where T : UnityEngine.Object
        {
            ValidateAssetRequest(path, typeof(T));
            if (done == null)
                throw new ArgumentNullException(nameof(done));

            int waitForCompleted;
            lock (_lock)
            {
                waitForCompleted = _enqueuedCount;
            }

            Loom.Current.StartCoroutine(LoadAsyncRoutine(path, typeof(T), waitForCompleted, asset => done(asset as T)));
        }

        private IEnumerator LoadAsyncRoutine(string path, Type type, int waitForCompleted, Action<UnityEngine.Object> done)
        {
            while (CompletedCount < waitForCompleted)
            {
                yield return null;
            }

            AssetBundle owner = FindLoadedBundle(path);
            if (owner != null)
            {
                AssetBundleRequest request = owner.LoadAsync(path, type);
                yield return request;
                if (request != null && request.asset != null)
                {
                    SafeInvokeAsset(path, done, request.asset);
                    yield break;
                }
            }

            if (!Facepunch.Bundling.Loaded)
            {
                bool gameBundlesLoaded = false;
                Facepunch.Bundling.OnceLoaded += () => gameBundlesLoaded = true;
                while (!gameBundlesLoaded)
                {
                    yield return null;
                }
            }

            AssetBundleRequest gameRequest;
#pragma warning disable 618
            // Marked obsolete by Facepunch because it does not work inside the Unity editor, it works in the game.
            bool found = Facepunch.Bundling.LoadAsync(path, type, out gameRequest);
#pragma warning restore 618
            if (found && gameRequest != null)
            {
                yield return gameRequest;
                SafeInvokeAsset(path, done, gameRequest.asset);
                yield break;
            }

            SafeInvokeAsset(path, done, null);
        }

        private int CompletedCount
        {
            get
            {
                lock (_lock)
                    return _completedCount;
            }
        }

        private AssetBundle FindLoadedBundle(string path)
        {
            lock (_lock)
            {
                for (int i = 0; i < _loadedBundles.Count; i++)
                {
                    AssetBundle bundle = _loadedBundles[i];
                    // Unity reports bundles a plugin unloaded itself as null, they are dropped here.
                    if (bundle == null)
                    {
                        _loadedBundles.RemoveAt(i--);
                        continue;
                    }

                    bool contains;
                    try
                    {
                        contains = bundle.Contains(path);
                    }
                    catch (Exception)
                    {
                        // A bundle that was unloaded behind our back can throw instead of comparing equal to null.
                        _loadedBundles.RemoveAt(i--);
                        continue;
                    }

                    if (contains)
                    {
                        return bundle;
                    }
                }
            }
            return null;
        }

        private static void ValidateAssetRequest(string path, Type type)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!typeof(UnityEngine.Object).IsAssignableFrom(type))
                throw new ArgumentException($"{type} does not derive from UnityEngine.Object", nameof(type));
        }

        private static void SafeInvokeAsset(string path, Action<UnityEngine.Object> done, UnityEngine.Object asset)
        {
            try
            {
                done(asset);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AssetBundleLoader] Asset callback threw for '{path}': {ex}");
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (true)
            {
                BundleRequest req;
                lock (_lock)
                {
                    if (_queue.Count == 0)
                    {
                        _running = false;
                        yield break;
                    }

                    req = _queue.Dequeue();
                }

                // Load the bundle.  version == 0 means "no cache".
                WWW www = null;
                try
                {
                    www = req.Version > 0
                        ? WWW.LoadFromCacheOrDownload(req.Path, req.Version)
                        : new WWW(req.Path);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[AssetBundleLoader] WWW ctor threw for '{req.Path}': {ex.Message}");
                }

                if (www == null)
                {
                    SafeInvoke(req, null);
                    MarkCompleted();
                    yield return null;
                    continue;
                }

                // Yield until the WWW is done.
                yield return www;

                AssetBundle bundle = null;
                if (!string.IsNullOrEmpty(www.error))
                {
                    Logger.Log($"[AssetBundleLoader] Error loading '{req.Path}': {www.error}");
                }
                else
                {
                    try
                    {
                        bundle = www.assetBundle;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[AssetBundleLoader] assetBundle property threw for '{req.Path}': {ex.Message}");
                    }
                }

                www.Dispose();

                if (bundle != null)
                {
                    lock (_lock)
                    {
                        _loadedBundles.Add(bundle);
                    }
                }

                SafeInvoke(req, bundle);
                MarkCompleted();

                // Yield one frame between bundles.  This gives the GC a chance
                // to collect the previous WWW and lets the JIT finish any
                // trampoline allocations for the loaded assembly before the
                // next managed-heap spike begins.
                yield return null;
            }
        }

        private void MarkCompleted()
        {
            lock (_lock)
            {
                _completedCount++;
            }
        }

        private void SafeInvoke(BundleRequest req, AssetBundle bundle)
        {
            try
            {
                req.Callback(bundle);
            }
            catch (Exception ex)
            {
                Logger.Log($"[AssetBundleLoader] Callback threw for '{req.Path}': {ex.Message}");
            }
        }

        private const BindingFlags AnyAccess =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Describes the private members of <c>Facepunch.Bundling</c> that injection writes to.
        /// The members are resolved once by name. When any of them is missing the game build differs from the
        /// one this code was written against, injection is disabled and the missing members are logged once.
        /// </summary>
        private sealed class BundlingLayout
        {
            internal FieldInfo MapField;
            internal FieldInfo AssetsField;
            internal FieldInfo AllListsField;
            internal FieldInfo TypeOfAssetsField;
            internal FieldInfo BundlesField;
            internal FieldInfo PathCacheField;
            internal FieldInfo BundleField;
            internal Type LoadedBundleType;
            internal ConstructorInfo LoadedBundleCtor;

            private static BundlingLayout _layout;
            private static bool _resolved;

            /// <summary>
            /// Returns the resolved layout, or null when the layout of <c>Facepunch.Bundling</c> is not the expected one.
            /// Must be called on the main thread.
            /// </summary>
            internal static BundlingLayout Get()
            {
                if (!_resolved)
                {
                    _layout = Resolve();
                    _resolved = true;
                }
                return _layout;
            }

            private static BundlingLayout Resolve()
            {
                List<string> missing = new List<string>();
                Type bundling = typeof(Facepunch.Bundling);
                Type mapType = bundling.GetNestedType("LoadedBundleMap", BindingFlags.NonPublic);
                Type assetMapType = bundling.GetNestedType("LoadedBundleAssetMap", BindingFlags.NonPublic);
                Type listType = bundling.GetNestedType("LoadedBundleListOfAssets", BindingFlags.NonPublic);
                Type loadedBundleType = bundling.GetNestedType("LoadedBundle", BindingFlags.NonPublic);

                BundlingLayout layout = new BundlingLayout
                {
                    MapField = Field(bundling, "Map", missing),
                    AssetsField = Field(mapType, "Assets", missing),
                    AllListsField = Field(assetMapType, "AllLoadedBundleAssetLists", missing),
                    TypeOfAssetsField = Field(listType, "TypeOfAssets", missing),
                    BundlesField = Field(listType, "Bundles", missing),
                    PathCacheField = Field(listType, "pathsToFoundBundles", missing),
                    BundleField = Field(loadedBundleType, "Bundle", missing),
                    LoadedBundleType = loadedBundleType
                };

                if (loadedBundleType != null)
                {
                    foreach (ConstructorInfo ctor in loadedBundleType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        ParameterInfo[] parameters = ctor.GetParameters();
                        if (parameters.Length == 2 && parameters[0].ParameterType == typeof(AssetBundle))
                        {
                            layout.LoadedBundleCtor = ctor;
                            break;
                        }
                    }
                }

                if (layout.LoadedBundleCtor == null)
                {
                    missing.Add("LoadedBundle(AssetBundle, Item)");
                }

                if (missing.Count > 0)
                {
                    Logger.LogError("[AssetBundleLoader] Facepunch.Bundling layout is not the expected one, bundle injection is disabled. " +
                                    $"Missing {string.Join(", ", missing.ToArray())}");
                    return null;
                }

                return layout;
            }

            private static FieldInfo Field(Type owner, string name, List<string> missing)
            {
                FieldInfo field = owner != null ? owner.GetField(name, AnyAccess) : null;
                if (field == null)
                {
                    missing.Add(owner != null ? $"{owner.Name}.{name}" : $"type owning {name}");
                }
                return field;
            }
        }

        /// <summary>
        /// Makes an already loaded <see cref="AssetBundle"/> part of the game's own <c>Facepunch.Bundling</c> registry.
        /// <code>
        /// if (loader.InjectIntoFacepunchBundling(heliBundle, typeof(GameObject)))
        /// {
        ///     heliPrefab = Facepunch.Bundling.Load&lt;GameObject&gt;("assets/prefabs/helicopter/Mi24");
        /// }
        /// </code>
        ///
        /// <para>
        /// After injection, plain calls such as <c>Facepunch.Bundling.Load&lt;GameObject&gt;(path)</c> find its assets exactly
        /// like assets shipped with the game. This is meant for plugin code that calls <c>Facepunch.Bundling</c> directly,
        /// code written against this class can use <see cref="Load{T}(string)"/> without any injection.
        /// </para>
        ///
        /// <para>
        /// The bundle is appended to the asset list whose element type equals <paramref name="assetType"/>.
        /// Lookups for that type and for every type it derives into, for example any Component on a prefab when
        /// injecting under GameObject, then search the bundle as well. The game builds one list per asset type, so a
        /// list for <paramref name="assetType"/> must already exist. GameObject, ScriptableObject based data blocks and
        /// the common texture types all have one.
        /// </para>
        ///
        /// <para>
        /// Precedence follows the game. Bundles are searched in registration order and the first one that contains a
        /// path wins, so an injected bundle adds new paths but cannot replace an asset the game already ships under the
        /// same path. Dependencies that are packed into the bundle, such as the meshes and materials of a prefab, load
        /// together with the prefab and do not have to be injected separately.
        /// </para>
        ///
        /// <para>
        /// Asset paths are matched by <see cref="AssetBundle.Contains"/>, so the paths used with
        /// <c>Facepunch.Bundling.Load</c> must be the exact names the bundle was built with. If the game ever disposes
        /// its bundle registry, the injected bundle is unloaded together with the game's own bundles.
        /// </para>
        ///
        /// <para>
        /// Must be called on the main thread after <c>Facepunch.Bundling.Loaded</c> became true. Use
        /// <see cref="InjectWhenBundlingLoaded"/> or <see cref="LoadAndInjectBundle"/> to have the timing handled.
        /// Injecting the same bundle twice under the same type is detected and treated as success.
        /// </para>
        /// </summary>
        /// <param name="bundle">A fully loaded bundle, for example the one passed to the callback of <see cref="LoadBundle"/>.</param>
        /// <param name="assetType">The asset type to register the bundle under, normally <c>typeof(GameObject)</c> for prefabs.</param>
        /// <returns>True if lookups through <c>Facepunch.Bundling</c> now include the bundle, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bundle"/> or <paramref name="assetType"/> is null.</exception>
        public bool InjectIntoFacepunchBundling(AssetBundle bundle, Type assetType)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));

            if (!Facepunch.Bundling.Loaded)
            {
                Logger.Log($"[AssetBundleLoader] Facepunch.Bundling has not finished loading, cannot inject '{bundle.name}' yet. " +
                           "Use InjectWhenBundlingLoaded to wait for it.");
                return false;
            }

            BundlingLayout layout = BundlingLayout.Get();
            if (layout == null)
            {
                return false;
            }

            try
            {
                object map = layout.MapField.GetValue(null);
                object assets = map != null ? layout.AssetsField.GetValue(map) : null;
                Array lists = assets != null ? layout.AllListsField.GetValue(assets) as Array : null;
                if (lists == null)
                {
                    Logger.Log("[AssetBundleLoader] Facepunch.Bundling has no asset map, cannot inject.");
                    return false;
                }

                object targetList = null;
                foreach (object candidate in lists)
                {
                    if (candidate != null && (Type)layout.TypeOfAssetsField.GetValue(candidate) == assetType)
                    {
                        targetList = candidate;
                        break;
                    }
                }

                if (targetList == null)
                {
                    Logger.Log($"[AssetBundleLoader] The game has no asset list of type {assetType}, " +
                               "injecting a type the game does not already use is not supported.");
                    return false;
                }

                Array bundles = (Array)layout.BundlesField.GetValue(targetList);
                foreach (object existing in bundles)
                {
                    if (existing != null && ReferenceEquals(layout.BundleField.GetValue(existing), bundle))
                    {
                        return true;
                    }
                }

                // Item metadata is only read by the loader while it builds the map, lookups never touch it.
                object loadedBundle = layout.LoadedBundleCtor.Invoke(new object[] { bundle, null });

                Array grown = Array.CreateInstance(layout.LoadedBundleType, bundles.Length + 1);
                Array.Copy(bundles, grown, bundles.Length);
                grown.SetValue(loadedBundle, bundles.Length);
                layout.BundlesField.SetValue(targetList, grown);

                // Misses are cached as well, so paths that were looked up before the injection would stay unresolved.
                IDictionary<string, short> pathCache = layout.PathCacheField.GetValue(targetList) as IDictionary<string, short>;
                if (pathCache != null)
                {
                    pathCache.Clear();
                }

                Logger.Log($"[AssetBundleLoader] Injected bundle '{bundle.name}' into Facepunch.Bundling for type {assetType}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AssetBundleLoader] InjectIntoFacepunchBundling failed for '{bundle.name}': {ex}");
                return false;
            }
        }

        /// <summary>
        /// Injects a bundle into <c>Facepunch.Bundling</c> as soon as the game finished loading its own bundles.
        /// <code>
        /// loader.InjectWhenBundlingLoaded(heliBundle, typeof(GameObject), injected =&gt;
        /// {
        ///     if (injected)
        ///     {
        ///         LoadPrefabs();
        ///     }
        /// });
        /// </code>
        /// The injection happens right away when the game already finished. This uses the game's <c>Facepunch.Bundling.OnceLoaded</c> event,
        /// so the callback runs on the main thread in both cases.
        /// See <see cref="InjectIntoFacepunchBundling"/> for the lookup rules that apply afterwards.
        /// </summary>
        /// <param name="bundle">A fully loaded bundle.</param>
        /// <param name="assetType">The asset type to register the bundle under, normally <c>typeof(GameObject)</c> for prefabs.</param>
        /// <param name="done">Optional callback receiving true when the injection succeeded. May be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bundle"/> or <paramref name="assetType"/> is null.</exception>
        public void InjectWhenBundlingLoaded(AssetBundle bundle, Type assetType, Action<bool> done)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));

            Facepunch.Bundling.OnceLoaded += () =>
            {
                bool injected = InjectIntoFacepunchBundling(bundle, assetType);
                if (done == null)
                {
                    return;
                }

                try
                {
                    done(injected);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[AssetBundleLoader] Injection callback threw for '{bundle.name}': {ex}");
                }
            };
        }

        /// <summary>
        /// Loads a bundle through the shared queue and injects it into <c>Facepunch.Bundling</c> once the game's own
        /// bundles are available, for plugins that keep calling <c>Facepunch.Bundling.Load</c> directly.
        /// <code>
        /// loader.LoadAndInjectBundle(heliBundleUrl, 0, typeof(GameObject), (bundle, injected) =&gt;
        /// {
        ///     if (injected)
        ///     {
        ///         heliPrefab = Facepunch.Bundling.Load&lt;GameObject&gt;("assets/prefabs/helicopter/Mi24");
        ///     }
        /// });
        /// </code>
        /// </summary>
        /// <param name="path">The URL or local path of the bundle, exactly as accepted by <see cref="LoadBundle"/>.</param>
        /// <param name="version">The cache version, exactly as accepted by <see cref="LoadBundle"/>.</param>
        /// <param name="assetType">The asset type to register the bundle under, normally <c>typeof(GameObject)</c> for prefabs.</param>
        /// <param name="done">
        /// Callback invoked on the main thread with the loaded bundle and whether it was injected. The bundle is null
        /// when loading failed. The callback is always invoked exactly once.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/>, <paramref name="assetType"/> or <paramref name="done"/> is null.</exception>
        public void LoadAndInjectBundle(string path, int version, Type assetType, Action<AssetBundle, bool> done)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));
            if (done == null)
                throw new ArgumentNullException(nameof(done));

            LoadBundle(path, version, bundle =>
            {
                if (bundle == null)
                {
                    done(null, false);
                    return;
                }

                InjectWhenBundlingLoaded(bundle, assetType, injected => done(bundle, injected));
            });
        }
    }
}