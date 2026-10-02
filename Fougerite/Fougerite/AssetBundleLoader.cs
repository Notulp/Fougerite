using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fougerite.Concurrent;
using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// Shared, serialised asset-bundle loader for RustBuster plugins.
    ///
    /// <para>
    /// <b>Problem it solves.</b>  When several plugins each call
    /// <c>WWW.LoadFromCacheOrDownload</c> independently they all start at the
    /// same moment (end of the loading screen), producing a single large
    /// managed-heap spike that coincides with Mono JIT-compiling player code.
    /// </para>
    ///
    /// <para>
    /// <b>How to use.</b>  Instead of calling <c>WWW.LoadFromCacheOrDownload</c>
    /// directly, a plugin calls
    /// <see cref="LoadBundle(string, int, Action{AssetBundle})"/>.  RustBuster
    /// owns the queue and processes entries one at a time; the callback receives
    /// the loaded <see cref="AssetBundle"/> (or <c>null</c> on error) when
    /// it is the plugin's turn.
    /// </para>
    ///
    /// <para>
    /// The queue is started automatically on first use and runs on the same
    /// <see cref="Loom"/> MonoBehaviour that the rest of the codebase uses for
    /// main-thread coroutines.  No plugin needs to manage the runner lifecycle.
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
        private bool _running;

        /// <summary>
        /// Returns the AssetBundleLoader's instance.
        /// </summary>
        /// <returns></returns>
        public static AssetBundleLoader GetAssetBundleLoader()
        {
            return Instance.Value;
        }

        /// <summary>
        /// Enqueues an asset-bundle load request.  The bundle will be loaded
        /// via <c>WWW.LoadFromCacheOrDownload</c> after all previously queued
        /// requests have completed, so at most one WWW download is in flight
        /// at any given moment.
        /// </summary>
        /// <param name="path">
        /// The URL or local path passed to
        /// <c>WWW.LoadFromCacheOrDownload</c>.  Local paths must begin with
        /// <c>file://</c>.
        /// </param>
        /// <param name="version">
        /// Cache version number.  Pass <c>0</c> to disable caching (equivalent
        /// to constructing a plain <c>WWW</c>).
        /// </param>
        /// <param name="done">
        /// Callback invoked on the main Unity thread once the bundle has been
        /// loaded.  The <see cref="AssetBundle"/> argument is <c>null</c> when
        /// loading failed; inspect <c>done</c>'s closure for error details if
        /// needed.  The callback is always invoked, even on failure, so callers
        /// can release waiting state unconditionally.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="path"/> or <paramref name="done"/> is
        /// <c>null</c>.
        /// </exception>
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
                if (!_running)
                {
                    _running = true;
                    Loom.Current.StartCoroutine(ProcessQueue());
                }
            }
        }

        /// <summary>
        /// Returns the number of bundle requests that are currently waiting in
        /// the queue (not counting the one actively loading, if any).
        /// </summary>
        public int PendingCount
        {
            get
            {
                lock (_lock)
                    return _queue.Count;
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

                SafeInvoke(req, bundle);

                // Yield one frame between bundles.  This gives the GC a chance
                // to collect the previous WWW and lets the JIT finish any
                // trampoline allocations for the loaded assembly before the
                // next managed-heap spike begins.
                yield return null;
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
        /// Merges an already-loaded <see cref="AssetBundle"/> into the game's
        /// own <c>Facepunch.Bundling</c> asset registry, so that
        /// <c>Facepunch.Bundling.Load</c>/<c>LoadAll</c> (and therefore every
        /// game system built on top of them, e.g. item/datablock lookup) can
        /// see assets coming from <paramref name="bundle"/> exactly as if it
        /// had been shipped with the game.
        ///
        /// <para>
        /// <c>Facepunch.Bundling</c> is the single place the client resolves
        /// every bundled asset through (<c>Load</c>/<c>LoadAll</c> for
        /// prefabs, <c>ItemDataBlock</c>s, <c>LootSpawnList</c>s, textures,
        /// etc. — see the existing calls to
        /// <c>Facepunch.Bundling.LoadAll&lt;ItemDataBlock&gt;()</c> in
        /// <c>Hooks.cs</c>). Internally it stores everything in a private,
        /// static <c>LoadedBundleMap</c> that is built exactly once, when the
        /// game finishes streaming its own numbered bundles (e.g.
        /// <c>gameobject.00X</c>, <c>texture.00X</c>) through
        /// <c>Facepunch.Load.Loader</c>. A bundle whose name/manifest entry
        /// matches the pattern the loader expects gets folded into the exact
        /// same registry automatically.
        /// </para>
        ///
        /// <para>
        /// <b>Why reflection is required here.</b>  <c>Bundling</c> does not
        /// expose any public API to register a bundle after the fact — the
        /// map is only ever populated from inside the loader's boot sequence.
        /// This method reaches into the private <c>Map</c>/<c>Assets</c>
        /// structures (documented in the decompiled <c>Bundling</c> source)
        /// to append <paramref name="bundle"/> to the list of bundles backing
        /// <paramref name="assetType"/>, which is the same storage the loader
        /// itself writes to. No part of the game's own bundle manifest is
        /// touched; only the live, in-memory lookup tables are extended.
        /// </para>
        ///
        /// <para>
        /// <b>Precedence / overriding caveat.</b>  Lookups are cached by path
        /// per asset-type bucket, and the first bundle (in registration order)
        /// that <c>Contains(path)</c> a given path wins. Injecting after the
        /// game's own bundles means genuinely new paths are always resolved,
        /// but a path that collides with one already shipped by the game will
        /// still resolve to the game's original asset unless that path was
        /// never looked up before (the "not found" result is cached too,
        /// which is why this method clears the relevant path cache after
        /// inserting).
        /// </para>
        ///
        /// <para>
        /// <b>Known limitation.</b> The real per-asset <c>Facepunch.Load.Item</c>
        /// metadata (content type, declared asset type, etc.) is not
        /// reconstructed for the injected entry — <c>null</c> is stored in its
        /// place. Only <c>Load</c>/<c>LoadAll</c>/<c>Contains</c> lookups (the
        /// documented, exercised code paths) are guaranteed to work; any game
        /// code that reads that per-bundle metadata back out is not supported.
        /// </para>
        /// </summary>
        /// <param name="bundle">
        /// A fully loaded bundle, e.g. the one handed to the callback passed
        /// to <see cref="LoadBundle"/>.
        /// </param>
        /// <param name="assetType">
        /// The asset type the bundle should be registered under (the same
        /// type you intend to pass to <c>Facepunch.Bundling.Load&lt;T&gt;</c>
        /// afterwards, e.g. <c>typeof(GameObject)</c> or
        /// <c>typeof(ItemDataBlock)</c>).
        /// </param>
        /// <returns>
        /// <c>true</c> if the bundle was merged into the registry;
        /// <c>false</c> if <c>Facepunch.Bundling</c> has not finished loading
        /// yet, or the internal layout could not be reached (e.g. it changed
        /// between game versions).
        /// </returns>
        public bool InjectIntoFacepunchBundling(AssetBundle bundle, Type assetType)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));
            if (assetType == null)
                throw new ArgumentNullException(nameof(assetType));

            try
            {
                Util util = Util.GetUtil();
                Type bundlingType = typeof(Facepunch.Bundling);

                if (!(bool)(util.GetInstanceProperty(bundlingType, null, "Loaded") ?? false))
                {
                    Logger.Log("[AssetBundleLoader] Facepunch.Bundling has not finished loading, cannot inject yet.");
                    return false;
                }

                object map = util.GetInstanceField(bundlingType, null, "Map");
                if (map == null)
                    return false;

                object assets = util.GetInstanceField(map.GetType(), map, "Assets");
                if (assets == null)
                    return false;

                Array lists = util.GetInstanceField(assets.GetType(), assets, "AllLoadedBundleAssetLists") as Array;
                if (lists == null)
                    return false;

                object targetList = null;
                foreach (object candidate in lists)
                {
                    Type typeOfAssets = util.GetInstanceField(candidate.GetType(), candidate, "TypeOfAssets") as Type;
                    if (typeOfAssets == assetType)
                    {
                        targetList = candidate;
                        break;
                    }
                }

                if (targetList == null)
                {
                    Logger.Log(
                        $"[AssetBundleLoader] No existing bundle of type {assetType} was found to merge into; injecting brand-new asset types is not supported.");
                    return false;
                }

                Type listType = targetList.GetType();
                Array bundles = util.GetInstanceField(listType, targetList, "Bundles") as Array;
                if (bundles == null)
                    return false;

                Type loadedBundleType = bundles.GetType().GetElementType();
                if (loadedBundleType == null)
                    return false;

                object loadedBundle = Activator.CreateInstance(
                    loadedBundleType,
                    AnyAccess,
                    null,
                    new object[] { bundle, null },
                    null);

                Array grown = Array.CreateInstance(loadedBundleType, bundles.Length + 1);
                Array.Copy(bundles, grown, bundles.Length);
                grown.SetValue(loadedBundle, bundles.Length);
                util.SetInstanceField(listType, targetList, "Bundles", grown);

                // The "path not found" lookups are cached - clear it so the
                // newly appended bundle actually gets a chance to be searched.
                object pathCache = util.GetInstanceField(listType, targetList, "pathsToFoundBundles");
                (pathCache as IDictionary<string, short>)?.Clear();

                Logger.Log(
                    $"[AssetBundleLoader] Injected bundle '{bundle.name}' into Facepunch.Bundling for type {assetType}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[AssetBundleLoader] InjectIntoFacepunchBundling failed: {ex}");
                return false;
            }
        }
    }
}