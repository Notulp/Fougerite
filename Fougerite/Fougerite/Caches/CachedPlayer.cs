using System;
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

        /// <summary>
        /// The last login date of the player.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastLogin
        {
            get;
            set;
        }
        
        /// <summary>
        /// The last logout date of the player.
        /// Can be null.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? LastLogout
        {
            get;
            set;
        }
    }
}