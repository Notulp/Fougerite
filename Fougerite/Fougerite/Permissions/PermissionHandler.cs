using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    public class PermissionHandler
    {
        [JsonProperty]
        public List<PermissionPlayer> PermissionPlayers
        {
            get;
            set;
        } = new List<PermissionPlayer>();

        [JsonProperty]
        public List<PermissionGroup> PermissionGroups
        {
            get;
            set;
        } = new List<PermissionGroup>();
    }
}