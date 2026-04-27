using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    /// <summary>
    /// Represents a player with an associated SteamID, permissions, and groups within the permission system.
    /// </summary>
    public class PermissionPlayer
    {
        /// <summary>
        /// Represents the unique 64-bit Steam identifier for a player in the permissions system.
        /// </summary>
        [JsonProperty]
        public ulong SteamID
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the collection of permissions assigned to a player in the permissions system.
        /// Each permission is defined as a string and determines the player's ability to perform specific actions or access certain features.
        /// </summary>
        [JsonProperty]
        public List<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// Represents the list of group names to which a permission player belongs.
        /// </summary>
        /// <remarks>
        /// Groups can define a specific set of permissions applied collectively to all members of the group.
        /// This property allows the management and assignment of groups for a player.
        /// </remarks>
        /// <value>
        /// A list of strings where each string is the name of a group.
        /// </value>
        [JsonProperty]
        public List<string> Groups
        {
            get;
            set;
        } = new List<string>();
    }
}