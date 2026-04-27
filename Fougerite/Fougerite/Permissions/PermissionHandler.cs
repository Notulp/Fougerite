using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    /// <summary>
    /// The PermissionHandler class provides a structure to store and manage
    /// permissions for both players and groups within the system.
    /// It serves as the container for handling these entities and their associated data.
    /// </summary>
    public class PermissionHandler
    {
        /// Represents a collection of players with associated permissions in the permission system.
        /// Each player is tracked as a `PermissionPlayer` instance, which contains details such as their SteamID, assigned permissions, and group memberships.
        /// This property is serialized and deserialized using JSON format to enable persistence of data.
        /// Typically used internally by the permission system for managing and querying player-specific permissions.
        /// The property initializes with an empty list of `PermissionPlayer` objects by default.
        [JsonProperty]
        public List<PermissionPlayer> PermissionPlayers
        {
            get;
            set;
        } = new List<PermissionPlayer>();

        /// <summary>
        /// Represents a collection of permission groups managed within the permission system.
        /// </summary>
        /// <remarks>
        /// Each permission group is defined by a unique identifier, a group name, a nickname,
        /// and a set of permissions. This property is used to store and manage the list of
        /// permission groups that are loaded or manipulated during the operation of the system.
        /// It allows for serialization and deserialization to and from JSON for persistence and
        /// retrieval purposes.
        /// </remarks>
        /// <value>
        /// A list of <see cref="PermissionGroup"/> objects representing the groups with their
        /// respective attributes and permissions.
        /// </value>
        [JsonProperty]
        public List<PermissionGroup> PermissionGroups
        {
            get;
            set;
        } = new List<PermissionGroup>();
    }
}