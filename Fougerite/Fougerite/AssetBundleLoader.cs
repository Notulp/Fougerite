using System;
using System.Collections;
using System.Collections.Generic;
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
                    Logger.Log("[AssetBundleLoader] WWW ctor threw for '" + req.Path + "': " + ex.Message);
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
                    Logger.Log("[AssetBundleLoader] Error loading '" + req.Path + "': " + www.error);
                }
                else
                {
                    try
                    {
                        bundle = www.assetBundle;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("[AssetBundleLoader] assetBundle property threw for '" + req.Path + "': " +
                                   ex.Message);
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
                Logger.Log("[AssetBundleLoader] Callback threw for '" + req.Path + "': " + ex.Message);
            }
        }
    }
}