using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fougerite.Caches
{
    public class CachedPlayer
    {
        /// <summary>
        /// Returns the SteamID of this player.
        /// </summary>
        [JsonProperty]
        public ulong SteamID
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the current name.
        /// </summary>
        [JsonProperty]
        public string Name
        {
            get;
            set;
        }
        
        /// <summary>
        /// Contains all the names used by this player.
        /// </summary>
        [JsonProperty]
        public List<string> Aliases
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the last known location of the player.
        /// </summary>
        [JsonProperty]
        public string Location
        {
            get;
            set;
        }
    }
}