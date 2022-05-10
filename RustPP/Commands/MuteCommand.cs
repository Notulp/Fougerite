using Fougerite.Permissions;

namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using RustPP.Permissions;
    using System;
    using System.Collections.Generic;

    internal class MuteCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Mute Usage:  /mute playerName");
                return;
            }

            PList list = new PList();
            list.Add(0, "Cancel");
            foreach (KeyValuePair<ulong, string> entry in Core.userCache)
            {
                if (entry.Value.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    MutePlayer(new PList.Player(entry.Key, entry.Value), pl);
                    return;
                }

                if (entry.Value.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(entry.Key, entry.Value);
            }

            if (list.Count == 1)
            {
                foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
                {
                    if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        MutePlayer(new PList.Player(client.UID, client.Name), pl);
                        return;
                    }

                    if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                        list.Add(client.UID, client.Name);
                }
            }

            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, "No player matches the name: " + playerName);
                return;
            }

            pl.MessageFrom(Core.Name,
                string.Format("{0}  player{1} {2}: ", ((list.Count - 1)).ToString(),
                    (((list.Count - 1) > 1) ? "s match" : " matches"), playerName));
            for (int i = 1; i < list.Count; i++)
            {
                pl.MessageFrom(Core.Name, string.Format("{0} - {1}", i, list.PlayerList[i].DisplayName));
            }

            pl.MessageFrom(Core.Name, "0 - Cancel");
            pl.MessageFrom(Core.Name, "Please enter the number matching the player you were looking for.");
            Core.muteWaitList[pl.UID] = list;
        }

        public void PartialNameMute(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            PList list = (PList)Core.muteWaitList[pl.UID];
            MutePlayer(list.PlayerList[id], pl);
        }

        public void MutePlayer(PList.Player mute, Fougerite.Player myAdmin)
        {
            if (mute.UserID == myAdmin.UID)
            {
                myAdmin.MessageFrom(Core.Name, "There is no point muting yourself.");
                return;
            }

            if (Core.muteList.Contains(mute.UserID))
            {
                myAdmin.MessageFrom(Core.Name, string.Format("{0} is already muted.", mute.DisplayName));
                return;
            }

            if (Administrator.IsAdmin(mute.UserID))
            {
                Administrator mutingAdmin = Administrator.GetAdmin(myAdmin.UID);
                Administrator mutedAdmin = Administrator.GetAdmin(mute.UserID);
                
                // What the fuck is this mikec tho lol
                if (mutedAdmin.HasPermission("CanUnmute") 
                    || mutedAdmin.HasPermission("CanAddFlags") 
                    || mutedAdmin.HasPermission("RCON")
                    || PermissionSystem.GetPermissionSystem().PlayerHasPermission(mutedAdmin.UserID, "CanUnmute")
                    || PermissionSystem.GetPermissionSystem().PlayerHasPermission(mutedAdmin.UserID, "CanAddFlags")
                    || PermissionSystem.GetPermissionSystem().PlayerHasPermission(mutedAdmin.UserID, "RCON"))
                {
                    if (!mutedAdmin.HasPermission("RCON") 
                        && !PermissionSystem.GetPermissionSystem().PlayerHasPermission(mutedAdmin.UserID, "RCON"))
                    {
                        if (mutingAdmin.HasPermission("RCON") 
                            || mutingAdmin.HasPermission("CanUnflag")
                            || PermissionSystem.GetPermissionSystem().PlayerHasPermission(mutingAdmin.UserID, "RCON")
                            || PermissionSystem.GetPermissionSystem().PlayerHasPermission(mutingAdmin.UserID, "CanUnflag"))
                        {
                            mutedAdmin.Flags.Remove("CanUnmute");
                            mutedAdmin.Flags.Remove("CanMute");
                            mutedAdmin.Flags.Remove("CanAddFlags");
                            mutedAdmin.Flags.Remove("CanUnflag");
                        }
                    }
                    else
                    {
                        myAdmin.MessageFrom(Core.Name,
                            string.Format("{0} is an administrator. You can't mute administrators.", mute.DisplayName));
                        //return;
                    }
                }
            }

            Core.muteList.Add(mute);
            Administrator.NotifyAdmins(string.Format("{0} has been muted by {1}.", mute.DisplayName, myAdmin.Name));
        }
    }
}