using System;
using RustPP;
using RustPP.Commands;
using RustPP.Permissions;
using RustPP.Social;
using System.Collections.Generic;
using Fougerite.Permissions;

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
    [Obsolete("I do not recommend using this, read the documentation for the reason.", false)]
    public class RustPPExtension
    {
        public FriendList FriendsOf(ulong steamid)
        {
            FriendsCommand command2 = (FriendsCommand) ChatCommand.GetCommand("friends");
            FriendList list = (FriendList) command2.GetFriendsLists()[steamid];
            return list;
        }

        public FriendList FriendsOf(string steamid)
        {
            FriendsCommand command2 = (FriendsCommand) ChatCommand.GetCommand("friends");
            FriendList list = (FriendList) command2.GetFriendsLists()[Convert.ToUInt64(steamid)];
            return list;
        }

        public ShareCommand GetShareCommand
        {
            get
            {
                return (ShareCommand)ChatCommand.GetCommand("share");
            }
        }

        public FriendsCommand GetFriendsCommand
        {
            get
            {
                return (FriendsCommand) ChatCommand.GetCommand("friends");
            }
        }

        public bool HasPermission(ulong userID, string perm)
        {
            var admin = GetAdmin(userID);
            return (admin != null && admin.HasPermission(perm) || PermissionSystem.GetPermissionSystem().PlayerHasPermission(userID, perm));
        }

        public bool HasPermission(string name, string perm)
        {
            var admin = GetAdmin(name);
            
            // This only works for admins, the API isn't used though.
            return (admin != null && admin.HasPermission(perm) 
                    || (admin != null && PermissionSystem.GetPermissionSystem().PlayerHasPermission(admin.UserID, perm)));
        }

        public bool IsAdmin(ulong uid)
        {
            return Administrator.IsAdmin(uid);
        }

        public bool IsAdmin(string name)
        {
            return Administrator.IsAdmin(name);
        }

        public Administrator GetAdmin(ulong userID)
        {
            return Administrator.GetAdmin(userID);
        }

        public Administrator GetAdmin(string name)
        {
            return Administrator.GetAdmin(name);
        }

        public Administrator Admin(ulong userID, string name, string flags)
        {
            return new Administrator(userID, name, flags);
        }

        public Administrator Admin(ulong userID, string name)
        {
            return new Administrator(userID, name);
        }

        public void RemoveInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            if (command.userIDs.Contains(userID))
            {
                command.userIDs.Remove(userID);
            }
        }

        public void AddInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            if (!command.userIDs.Contains(userID))
            {
                command.userIDs.Add(userID);
            }
        }

        public bool HasInstaKO(ulong userID)
        {
            InstaKOCommand command = (InstaKOCommand)ChatCommand.GetCommand("instako");
            return command.userIDs.Contains(userID);
        }

        public void RemoveGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            if (command.userIDs.Contains(userID))
            {
                command.userIDs.Remove(userID);
            }
        }

        public void AddGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            if (!command.userIDs.Contains(userID))
            {
                command.userIDs.Add(userID);
            }
        }

        public bool HasGod(ulong userID)
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            return command.userIDs.Contains(userID);
        }

        public void RustPPSave()
        {
            Helper.CreateSaves();
        }

        public bool IsMuted(ulong id)
        {
            return Core.muteList.Contains(id);
        }

        public bool IsMuted(Player pl)
        {
            return Core.muteList.Contains(pl.UID);
        }

        public void UnMute(ulong id)
        {
            Core.muteList.Remove(id);
        }

        public void UnMute(Player pl)
        {
            Core.muteList.Remove(pl.UID);
        }

        public void Mute(ulong id, string name)
        {
            Core.muteList.Add(new PList.Player(id, name));
        }

        public void Mute(Player pl)
        {
            Core.muteList.Add(new PList.Player(pl.UID, pl.Name));
        }

        public Dictionary<ulong, string> Cache
        {
            get
            {
                return Core.userCache;
            }
        }
    }
}
