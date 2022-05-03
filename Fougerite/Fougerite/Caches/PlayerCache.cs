using System.Collections.Generic;
using Fougerite.Concurrent;
using Newtonsoft.Json;

namespace Fougerite.Caches
{
    public static class PlayerCache
    {
        /// <summary>
        /// This is a Serialized Cache where we store all the history of connected players.
        /// Useful for finding the owner's name of an entity when the player didn't connect to the server
        /// since server startup.
        /// It can be extended to do basically anything.
        /// </summary>
        [JsonProperty] 
        public static ConcurrentDictionary<ulong, CachedPlayer> CachedPlayers
        {
            get;
            set;
        } = new ConcurrentDictionary<ulong, CachedPlayer>();
    }
}