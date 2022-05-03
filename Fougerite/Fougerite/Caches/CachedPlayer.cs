using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fougerite.Caches
{
    public class CachedPlayer
    {
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
        /// Contains all the IPs used by this player.
        /// </summary>
        [JsonProperty]
        public List<string> IPAddresses
        {
            get;
            set;
        }
    }
}