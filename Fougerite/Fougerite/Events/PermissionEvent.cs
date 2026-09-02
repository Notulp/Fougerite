using Fougerite.Permissions;

namespace Fougerite.Events
{
    /// <summary>
    /// The type of permission action being performed.
    /// </summary>
    public enum PermissionActionType
    {
        CreateGroup,
        RemoveGroup,
        CreatePermissionPlayer,
        RemovePermissionPlayer,
        AddGroupToPlayer,
        RemoveGroupFromPlayer,
        AddPermissionToGroup,
        RemovePermissionFromGroup,
        AddPermission,
        RemovePermission
    }

    /// <summary>
    /// Runs when a permission system action is about to be performed.
    /// Plugins can inspect the action details and optionally cancel the operation.
    /// </summary>
    public class PermissionEvent
    {
        private readonly PermissionActionType _actionType;
        private readonly ulong _steamId;
        private readonly string _groupName;
        private readonly string _permission;
        private readonly string _nickName;
        private bool _cancelled;

        public PermissionEvent(PermissionActionType actionType, ulong steamId = 0, string groupName = null,
            string permission = null, string nickName = null)
        {
            _actionType = actionType;
            _steamId = steamId;
            _groupName = groupName;
            _permission = permission;
            _nickName = nickName;
        }

        /// <summary>
        /// Gets the type of permission action being performed.
        /// </summary>
        public PermissionActionType ActionType
        {
            get { return _actionType; }
        }

        /// <summary>
        /// Gets the SteamID of the player involved in the action.
        /// Returns 0 if no player is involved (e.g., group-only operations).
        /// </summary>
        public ulong SteamId
        {
            get { return _steamId; }
        }

        /// <summary>
        /// Gets the group name involved in the action.
        /// Returns null if no group is involved.
        /// </summary>
        public string GroupName
        {
            get { return _groupName; }
        }

        /// <summary>
        /// Gets the permission string involved in the action.
        /// Returns null if no permission string is involved.
        /// </summary>
        public string Permission
        {
            get { return _permission; }
        }

        /// <summary>
        /// Gets the nickname involved in the action.
        /// Returns null if no nickname is involved.
        /// </summary>
        public string NickName
        {
            get { return _nickName; }
        }

        /// <summary>
        /// Checks if the event was cancelled.
        /// </summary>
        public bool Cancelled
        {
            get { return _cancelled; }
        }

        /// <summary>
        /// Cancels the permission action, preventing it from being executed.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
    }
}
