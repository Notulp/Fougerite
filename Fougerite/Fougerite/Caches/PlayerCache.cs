using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Fougerite.Concurrent;
using Fougerite.Tools;
using Newtonsoft.Json;

namespace Fougerite.Caches
{
    public class PlayerCache
    {
        private static readonly Lazy<PlayerCache> Instance = new Lazy<PlayerCache>(() => new PlayerCache());
        private readonly string _cachedPlayersPath;
        private readonly object _cacheLock = new object();

        private PlayerCache()
        {
            _cachedPlayersPath = Util.GetRootFolder().Combine("\\Save\\CachedPlayers.json");
        }
        
        /// <summary>
        /// This is a Serialized Cache where we store all the history of connected players.
        /// Useful for finding the owner's name of an entity when the player didn't connect to the server
        /// since server startup.
        /// It can be extended to do basically anything.
        /// </summary>
        public ConcurrentDictionary<ulong, CachedPlayer> CachedPlayers
        {
            get;
            private set;
        } = new ConcurrentDictionary<ulong, CachedPlayer>();

        /// <summary>
        /// Get the instance.
        /// </summary>
        /// <returns></returns>
        public static PlayerCache GetPlayerCache()
        {
            return Instance.Value;
        }
        
        /// <summary>
        /// Finds a player by their SteamID (Key).
        /// </summary>
        /// <param name="steamId">The ulong SteamID of the player.</param>
        /// <returns>The CachedPlayer object if found, otherwise, null.</returns>
        public CachedPlayer GetPlayerBySteamId(ulong steamId)
        {
            return CachedPlayers.TryGetValue(steamId, out CachedPlayer player) ? player : null;
        }
        
        /// <summary>
        /// Finds a player by their SteamID provided as a string.
        /// </summary>
        /// <param name="steamIdStr">The SteamID as a string.</param>
        /// <returns>The CachedPlayer object if the string is a valid ulong and the player exists, otherwise, null.</returns>
        public CachedPlayer GetPlayerBySteamId(string steamIdStr)
        {
            return ulong.TryParse(steamIdStr, out ulong steamId) ? GetPlayerBySteamId(steamId) : null;
        }

        /// <summary>
        /// Finds the first player matching the specified current name.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="name">The current name to search for.</param>
        /// <returns>The CachedPlayer object if found, otherwise, null.</returns>
        public CachedPlayer GetPlayerByName(string name)
        {
            return CachedPlayers.Values.FirstOrDefault(p => 
                p.Name != null && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds the first player that has the specified IP address in their history.
        /// </summary>
        /// <param name="ip">The IP address string.</param>
        /// <returns>The CachedPlayer object if found, otherwise, null.</returns>
        public CachedPlayer GetPlayerByIP(string ip)
        {
            return CachedPlayers.Values.FirstOrDefault(p => 
                p.IPAddresses != null && p.IPAddresses.Contains(ip));
        }

        /// <summary>
        /// Finds the first player that has used the specified name as an alias in the past.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="alias">The alias/previous name.</param>
        /// <returns>The CachedPlayer object if found, otherwise, null.</returns>
        public CachedPlayer GetPlayerByAlias(string alias)
        {
            return CachedPlayers.Values.FirstOrDefault(p => 
                p.Aliases != null && p.Aliases.Any(a => a.Equals(alias, StringComparison.OrdinalIgnoreCase)));
        }
        
        /// <summary>
        /// Returns all players currently using the specified name.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>A list of CachedPlayer objects.</returns>
        public List<CachedPlayer> GetPlayersByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new List<CachedPlayer>();
            
            return CachedPlayers.Values.Where(p => 
                p.Name != null && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Returns all players who have used the specified alias in their history.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="alias">The alias to search for.</param>
        /// <returns>A list of CachedPlayer objects.</returns>
        public List<CachedPlayer> GetPlayersByAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias)) 
                return new List<CachedPlayer>();
            
            return CachedPlayers.Values.Where(p => 
                p.Aliases != null && p.Aliases.Any(a => a.Equals(alias, StringComparison.OrdinalIgnoreCase))).ToList();
        }
        
        /// <summary>
        /// Finds all players whose current name contains the specified search string.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="namePart">The partial name to search for.</param>
        /// <returns>A list of CachedPlayer objects containing the string.</returns>
        public List<CachedPlayer> GetPlayersByNameContains(string namePart)
        {
            if (string.IsNullOrEmpty(namePart)) 
                return new List<CachedPlayer>();
            
            return CachedPlayers.Values.Where(p => 
                    p.Name != null && p.Name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        /// <summary>
        /// Finds all players who have at least one alias containing the specified search string.
        /// Match is case-insensitive.
        /// </summary>
        /// <param name="aliasPart">The partial alias to search for.</param>
        /// <returns>A list of CachedPlayer objects where at least one alias matches.</returns>
        public List<CachedPlayer> GetPlayersByAliasContains(string aliasPart)
        {
            if (string.IsNullOrEmpty(aliasPart)) 
                return new List<CachedPlayer>();

            return CachedPlayers.Values.Where(p => 
                    p.Aliases != null && p.Aliases.Any(a => 
                        a != null && a.IndexOf(aliasPart, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
        }

        /// <summary>
        /// Returns a list of all players that have used a specific IP.
        /// Useful for detecting alt accounts/ban evaders.
        /// </summary>
        /// <param name="ip">The IP address string.</param>
        /// <returns>A list of CachedPlayer objects.</returns>
        public List<CachedPlayer> GetPlayersByIP(string ip)
        {
            if (string.IsNullOrEmpty(ip)) 
                return new List<CachedPlayer>();
            
            return CachedPlayers.Values.Where(p => 
                p.IPAddresses != null && p.IPAddresses.Contains(ip)).ToList();
        }

        /// <summary>
        /// This is supposed to be called once, and on ServerInit.
        /// </summary>
        internal void LoadPlayersCache()
        {
            lock (_cacheLock)
            {
                try
                {
                    JsonSerializer serializer = new JsonSerializer();
                    // https://stackoverflow.com/questions/24025350/xamarin-android-json-net-serilization-fails-on-4-2-2-device-only-timezonenotfoun
                    serializer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    serializer.NullValueHandling = NullValueHandling.Include;

                    if (!File.Exists(_cachedPlayersPath))
                    {
                        File.Create(_cachedPlayersPath).Dispose();

                        using (StreamWriter sw = new StreamWriter(_cachedPlayersPath, false, Encoding.UTF8))
                        {
                            using (JsonWriter writer = new JsonTextWriter(sw))
                            {
                                writer.Formatting = Formatting.Indented;
                                // We are serializing the original dictionary class
                                serializer.Serialize(writer, CachedPlayers.GetShallowCopy());
                            }
                        }
                    }

                    var deserializedDict =
                        JsonConvert.DeserializeObject<Dictionary<ulong, CachedPlayer>>(
                            File.ReadAllText(_cachedPlayersPath));

                    // Assign deserialized dict.
                    CachedPlayers = new ConcurrentDictionary<ulong, CachedPlayer>(deserializedDict);

                    Logger.Log("[PlayerCache] Loaded.");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PlayerCache] Error: {ex}");
                }
            }
        }
        
        public void SaveToDisk()
        {
            lock (_cacheLock)
            {
                string cachedplayers = "";

                try
                {
                    if (!File.Exists(_cachedPlayersPath))
                    {
                        File.Create(_cachedPlayersPath).Dispose();
                    }

                    // Backup the data from the current files.
                    cachedplayers = File.ReadAllText(_cachedPlayersPath);

                    // Empty the files.
                    if (File.Exists(_cachedPlayersPath))
                    {
                        File.WriteAllText(_cachedPlayersPath, string.Empty);
                    }

                    JsonSerializer serializer = new JsonSerializer();
                    // https://stackoverflow.com/questions/24025350/xamarin-android-json-net-serilization-fails-on-4-2-2-device-only-timezonenotfoun
                    serializer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    serializer.NullValueHandling = NullValueHandling.Include;

                    using (StreamWriter sw = new StreamWriter(_cachedPlayersPath, false, Encoding.UTF8))
                    {
                        using (JsonWriter writer = new JsonTextWriter(sw))
                        {
                            writer.Formatting = Formatting.Indented;
                            // We are serializing the original dictionary class
                            serializer.Serialize(writer, CachedPlayers.GetShallowCopy());
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PlayerCache] SaveToDisk Error: {ex}");
                    File.WriteAllText(_cachedPlayersPath, cachedplayers);
                }
            }
        }
    }
}