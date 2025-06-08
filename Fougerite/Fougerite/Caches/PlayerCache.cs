using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Fougerite.Concurrent;
using Fougerite.Tools;
using Newtonsoft.Json;

namespace Fougerite.Caches
{
    public class PlayerCache
    {
        private static PlayerCache _playerCache;
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
            if (_playerCache == null)
            {
                _playerCache = new PlayerCache();
            }
            
            return _playerCache;
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