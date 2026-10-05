using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fougerite.Concurrent;
using Newtonsoft.Json;

namespace Fougerite.Tools
{
    /// <summary>
    /// Holds SteamIDs that are trusted regardless of what the Steam Web API reports about their account,
    /// used by <see cref="SteamAuthMode.RustOwners"/> and <see cref="SteamAuthMode.SteamPaidAccounts"/> to
    /// still allow a Spacewar player we trust even when they don't own Rust or their account is limited.
    /// Backed by Save\TrustedSteamIDs.json, which is safe to edit by hand while the server is stopped.
    /// </summary>
    public class TrustedSteamIDs
    {
        private static readonly Lazy<TrustedSteamIDs> Instance = new Lazy<TrustedSteamIDs>(() => new TrustedSteamIDs());
        private readonly string _path = Util.GetRootFolder().Combine("\\Save\\TrustedSteamIDs.json");
        private readonly object _lock = new object();
        private HashSet<ulong> _ids = new HashSet<ulong>();

        private TrustedSteamIDs()
        {
            Reload();
        }

        /// <summary>
        /// Retrieves the singleton instance of the TrustedSteamIDs class.
        /// </summary>
        /// <returns>The active TrustedSteamIDs instance.</returns>
        public static TrustedSteamIDs GetInstance()
        {
            return Instance.Value;
        }

        /// <summary>
        /// (Re)loads the trusted SteamID list from disk, creating a default file with an example entry if missing.
        /// </summary>
        public void Reload()
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(_path))
                    {
                        List<ulong> example = new List<ulong>() {76561190000000000};
                        File.WriteAllText(_path, JsonConvert.SerializeObject(example, Formatting.Indented), Encoding.UTF8);
                    }

                    List<ulong> loaded = JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText(_path));
                    _ids = loaded != null ? new HashSet<ulong>(loaded) : new HashSet<ulong>();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[TrustedSteamIDs] Reload Error: {ex}");
                }
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_path, JsonConvert.SerializeObject(_ids.ToList(), Formatting.Indented), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[TrustedSteamIDs] Save Error: {ex}");
            }
        }

        /// <summary>
        /// Checks whether the given SteamID is trusted.
        /// </summary>
        public bool Contains(ulong steamId)
        {
            lock (_lock)
            {
                return _ids.Contains(steamId);
            }
        }

        /// <summary>
        /// Adds a SteamID to the trusted list.
        /// </summary>
        /// <returns>True if it was added, false if it was already present.</returns>
        public bool Add(ulong steamId)
        {
            lock (_lock)
            {
                if (!_ids.Add(steamId))
                {
                    return false;
                }

                Save();
                return true;
            }
        }

        /// <summary>
        /// Removes a SteamID from the trusted list.
        /// </summary>
        /// <returns>True if it was removed, false if it wasn't present.</returns>
        public bool Remove(ulong steamId)
        {
            lock (_lock)
            {
                if (!_ids.Remove(steamId))
                {
                    return false;
                }

                Save();
                return true;
            }
        }

        /// <summary>
        /// Returns a snapshot of every currently trusted SteamID.
        /// </summary>
        public List<ulong> GetAll()
        {
            lock (_lock)
            {
                return _ids.ToList();
            }
        }
    }
}
