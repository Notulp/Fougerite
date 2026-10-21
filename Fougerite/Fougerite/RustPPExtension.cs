using System;
using System.Collections.Generic;
using Fougerite.Permissions;
using RustPP;
using RustPP.Commands;
using RustPP.Permissions;
using RustPP.Social;

namespace Fougerite
{
    /// <summary>
    /// The existence of this class should have never happened.
    /// It creates a vice-versa link to Rust++.
    /// The plugin Rust++ It self that was brought in should have been replaced with a far better
    /// set of structure, json, and easy to access API even for script plugins.
    /// Although I have shown the examples years later on how proper plugin APIs should be
    /// I am afraid due to plugins using this, is going to remain as-is.
    /// </summary>
    [Obsolete("I do not recommend using this, read the documentation for the reason. Not that it matters anymore, so feel free to do so.", false)]
    public class RustPPExtension
    {
        /// <summary>
        /// Returns the RustPP friend list of the given SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID (as a ulong) to look up.</param>
        /// <returns>The player's FriendList.</returns>
        public FriendList FriendsOf(ulong steamid)
        {
            FriendsCommand command2 = (FriendsCommand) ChatCommand.GetCommand("friends");
            FriendList list = (FriendList) command2.GetFriendsLists()[steamid];
            return list;
        }

        /// <summary>
        /// Returns the RustPP friend list of the given SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID (as a string) to look up.</param>
        /// <returns>The player's FriendList.</returns>
        public FriendList FriendsOf(string steamid)
        {
            FriendsCommand command2 = (FriendsCommand) ChatCommand.GetCommand("friends");
            FriendList list = (FriendList) command2.GetFriendsLists()[Convert.ToUInt64(steamid)];
            return list;
        }

        /// <summary>
        /// Returns RustPP's registered "share" chat command instance.
        /// </summary>
        public ShareCommand GetShareCommand
        {
            get
            {
                return (ShareCommand)ChatCommand.GetCommand("share");
            }
        }

        /// <summary>
        /// Returns RustPP's registered "friends" chat command instance.
        /// </summary>
        public FriendsCommand GetFriendsCommand
        {
            get
            {
                return (FriendsCommand) ChatCommand.GetCommand("friends");
            }
        }

        /// <summary>
        /// Checks whether the given player (by SteamID) has the given RustPP admin flag/permission.
        /// </summary>
        /// <param name="userID">The SteamID of the player.</param>
        /// <param name="perm">The permission/flag to check.</param>
        /// <returns>True if the player has the permission.</returns>
        public bool HasPermission(ulong userID, string perm)
        {
            var admin = GetAdmin(userID);
            return (admin != null && admin.HasPermission(perm) || PermissionSystem.GetPermissionSystem().PlayerHasPermission(userID, perm));
        }

        /// <summary>
        /// Checks whether the given admin (by name) has the given RustPP admin flag/permission.
        /// </summary>
        /// <param name="name">The admin's name.</param>
        /// <param name="perm">The permission/flag to check.</param>
        /// <returns>True if the admin has the permission.</returns>
        public bool HasPermission(string name, string perm)
        {
            var admin = GetAdmin(name);
            
            // This only works for admins, the API isn't used though.
            return (admin != null && admin.HasPermission(perm) 
                    || (admin != null && PermissionSystem.GetPermissionSystem().PlayerHasPermission(admin.UserID, perm)));
        }

        /// <summary>
        /// Checks whether the given SteamID is a registered RustPP admin.
        /// </summary>
        /// <param name="uid">The SteamID to check.</param>
        /// <returns>True if the player is an admin.</returns>
        public bool IsAdmin(ulong uid)
        {
            return Administrator.IsAdmin(uid);
        }

        /// <summary>
        /// Checks whether the given name is a registered RustPP admin.
        /// </summary>
        /// <param name="name">The admin's name.</param>
        /// <returns>True if the name is an admin.</returns>
        public bool IsAdmin(string name)
        {
            return Administrator.IsAdmin(name);
        }

        /// <summary>
        /// Gets the RustPP Administrator entry for the given SteamID.
        /// </summary>
        /// <param name="userID">The SteamID to look up.</param>
        /// <returns>The matching Administrator, or null if none is found.</returns>
        public Administrator GetAdmin(ulong userID)
        {
            return Administrator.GetAdmin(userID);
        }

        /// <summary>
        /// Gets the RustPP Administrator entry for the given name.
        /// </summary>
        /// <param name="name">The admin's name to look up.</param>
        /// <returns>The matching Administrator, or null if none is found.</returns>
        public Administrator GetAdmin(string name)
        {
            return Administrator.GetAdmin(name);
        }

        /// <summary>
        /// Creates a new RustPP Administrator entry with specific flags.
        /// </summary>
        /// <param name="userID">The SteamID of the admin.</param>
        /// <param name="name">The admin's name.</param>
        /// <param name="flags">The permission flags to grant.</param>
        /// <returns>The new Administrator instance.</returns>
        public Administrator Admin(ulong userID, string name, string flags)
        {
            return new Administrator(userID, name, flags);
        }

        /// <summary>
        /// Creates a new RustPP Administrator entry with default flags.
        /// </summary>
        /// <param name="userID">The SteamID of the admin.</param>
        /// <param name="name">The admin's name.</param>
        /// <returns>The new Administrator instance.</returns>
        public Administrator Admin(ulong userID, string name)
        {
            return new Administrator(userID, name);
        }

        /// <summary>
        /// Removes the given player from RustPP's InstaKO (one-hit-kill) list.
        /// </summary>
        /// <param name="userID">The SteamID of the player.</param>
        public void RemoveInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            if (command.userIDs.Contains(userID))
            {
                command.userIDs.Remove(userID);
            }
        }

        /// <summary>
        /// Adds the given player to RustPP's InstaKO (one-hit-kill) list.
        /// </summary>
        /// <param name="userID">The SteamID of the player.</param>
        public void AddInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            if (!command.userIDs.Contains(userID))
            {
                command.userIDs.Add(userID);
            }
        }

        /// <summary>
        /// Checks whether the given player is on RustPP's InstaKO (one-hit-kill) list.
        /// </summary>
        /// <param name="userID">The SteamID of the player.</param>
        /// <returns>True if the player is on the InstaKO list.</returns>
        public bool HasInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            return command.userIDs.Contains(userID);
        }

        /// <summary>
        /// Removes the given player from RustPP's god-mode list.
        /// </summary>
        /// <param name="userID">The SteamID of the player.</param>
        public void RemoveGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            if (command.userIDs.Contains(userID))
            {
                command.userIDs.Remove(userID);
            }
        }

        /// <summary>
        /// Adds the given player to RustPP's god-mode list.
        /// </summary>
        /// <param name="userID">The SteamID of the player.</param>
        public void AddGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            if (!command.userIDs.Contains(userID))
            {
                command.userIDs.Add(userID);
            }
        }

        /// <summary>
        /// Checks whether the given player is in RustPP's god-mode list.
        /// </summary>
        /// <param name="userID">The SteamID of the player.</param>
        /// <returns>True if the player has god mode enabled.</returns>
        public bool HasGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            return command.userIDs.Contains(userID);
        }

        /// <summary>
        /// Forces RustPP to save its data (friends, admins, mutes, etc.) to disk.
        /// </summary>
        public void RustPPSave()
        {
            Helper.CreateSaves();
        }

        /// <summary>
        /// Checks whether the given SteamID is muted in RustPP.
        /// </summary>
        /// <param name="id">The SteamID to check.</param>
        /// <returns>True if the player is muted.</returns>
        public bool IsMuted(ulong id)
        {
            return Core.muteList.Contains(id);
        }

        /// <summary>
        /// Checks whether the given player is muted in RustPP.
        /// </summary>
        /// <param name="pl">The player to check.</param>
        /// <returns>True if the player is muted.</returns>
        public bool IsMuted(Player pl)
        {
            return Core.muteList.Contains(pl.UID);
        }

        /// <summary>
        /// Unmutes the given SteamID in RustPP.
        /// </summary>
        /// <param name="id">The SteamID to unmute.</param>
        public void UnMute(ulong id)
        {
            Core.muteList.Remove(id);
        }

        /// <summary>
        /// Unmutes the given player in RustPP.
        /// </summary>
        /// <param name="pl">The player to unmute.</param>
        public void UnMute(Player pl)
        {
            Core.muteList.Remove(pl.UID);
        }

        /// <summary>
        /// Mutes the given SteamID in RustPP.
        /// </summary>
        /// <param name="id">The SteamID to mute.</param>
        /// <param name="name">The player's name, stored alongside the mute entry.</param>
        public void Mute(ulong id, string name)
        {
            Core.muteList.Add(new PList.Player(id, name));
        }

        /// <summary>
        /// Mutes the given player in RustPP.
        /// </summary>
        /// <param name="pl">The player to mute.</param>
        public void Mute(Player pl)
        {
            Core.muteList.Add(new PList.Player(pl.UID, pl.Name));
        }

        /// <summary>
        /// Returns RustPP's internal SteamID-to-name user cache.
        /// </summary>
        public Dictionary<ulong, string> Cache
        {
            get
            {
                return Core.userCache;
            }
        }
    }
}
