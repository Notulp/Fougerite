using System;
using System.Collections.Generic;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// Centralizes the lifecycle of the single unmanaged Fougerite Mono plugin registry
    /// (<see cref="NativeMono.mono_fg_create_domain"/> / <see cref="NativeMono.mono_fg_unload_domain"/>) so
    /// that every C# based plugin loader (<see cref="CSharpPluginLoader"/>, <see cref="CSScriptPluginLoader"/>, ...)
    /// shares one lazily created, reference counted instance instead of each loader independently creating
    /// and tearing it down.
    /// </summary>
    /// <remarks>
    /// Before this existed, only <see cref="CSharpPluginLoader"/> called <c>mono_fg_create_domain</c>/
    /// <c>mono_fg_unload_domain</c> directly. That meant:
    /// <list type="bullet">
    /// <item>If DLL modules (<c>EnableCSharp</c>) were disabled while C# script plugins
    /// (<c>EnableCSScript</c>) were enabled, the registry was never created at all.</item>
    /// <item><c>mono_fg_unload_domain</c> unloads <b>every</b> tracked plugin domain and releases the whole
    /// registry, so whichever loader called it first would rip the registry out from under any other
    /// loader that still had plugins tracked in it.</item>
    /// </list>
    /// Every loader that registers a plugin with <see cref="Icalls.mono_fg_load_plugin"/> must call
    /// <see cref="EnsureCreated"/> first (lazy, thread-safe, idempotent) and <see cref="ReleaseOwner"/> once
    /// it no longer has any plugin tracked in the registry (e.g. after its own <c>UnloadPlugins()</c>
    /// finished). The registry itself is only torn down once every owner that ever asked for it released it.
    /// </remarks>
    internal static class NativeDomainManager
    {
        private static readonly object SyncRoot = new object();
        private static readonly HashSet<string> ActiveOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _created;

        /// <summary>
        /// Lazily creates the native plugin registry (once, shared across owners) and registers
        /// <paramref name="owner"/> as one of its current users. Safe to call repeatedly/concurrently, and
        /// safe to call again after a previous <see cref="ReleaseOwner"/> tore the registry down, in which
        /// case it is simply recreated.
        /// </summary>
        /// <param name="owner">A stable identifier for the caller, e.g. "CSharp" or "CSScript".</param>
        /// <returns>True if the registry is ready to use (already existed, or was just created).</returns>
        public static bool EnsureCreated(string owner)
        {
            lock (SyncRoot)
            {
                if (!_created)
                {
                    try
                    {
                        int result = NativeMono.mono_fg_create_domain();
                        if (result == 0)
                        {
                            Logger.LogError("[NativeDomainManager] Failed to create the unmanaged Fougerite Mono domain.");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[NativeDomainManager] Exception while creating the Fougerite domain: {ex}");
                        return false;
                    }

                    _created = true;
                    Logger.LogDebug("[NativeDomainManager] Unmanaged Fougerite Mono domain created.");
                }

                ActiveOwners.Add(owner);
                return true;
            }
        }

        /// <summary>
        /// Tells the manager that <paramref name="owner"/> no longer needs the native registry, typically
        /// right after it finished unloading every plugin it had tracked in it. The registry itself is only
        /// torn down via <see cref="NativeMono.mono_fg_unload_domain"/> once every owner that ever called
        /// <see cref="EnsureCreated"/> has released it, so one loader unloading its plugins never tears the
        /// registry down while another loader still has plugins tracked in it.
        /// </summary>
        /// <param name="owner">The same identifier previously passed to <see cref="EnsureCreated"/>.</param>
        public static void ReleaseOwner(string owner)
        {
            lock (SyncRoot)
            {
                if (!ActiveOwners.Remove(owner) || !_created)
                    return;

                if (ActiveOwners.Count > 0)
                {
                    Logger.LogDebug(
                        $"[NativeDomainManager] {owner} released the Fougerite Mono domain, still in use by: {string.Join(", ", ActiveOwners.ToArray())}.");
                    return;
                }

                try
                {
                    NativeMono.mono_fg_unload_domain();
                    Logger.LogDebug("[NativeDomainManager] Unmanaged Fougerite Mono domain unloaded, no owners left.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[NativeDomainManager] Exception while unloading the Fougerite domain: {ex}");
                }
                finally
                {
                    // Always reset, even on failure, so a later EnsureCreated() attempts to recreate it
                    // instead of permanently believing a (possibly half torn down) registry still exists.
                    _created = false;
                }
            }
        }

        /// <summary>
        /// True if the native registry is currently believed to exist. For diagnostics only.
        /// </summary>
        public static bool IsCreated
        {
            get
            {
                lock (SyncRoot)
                {
                    return _created;
                }
            }
        }
    }
}
