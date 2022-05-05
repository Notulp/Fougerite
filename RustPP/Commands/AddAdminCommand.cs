using System.Security;

namespace RustPP.Commands
{
    using Fougerite;
    using RustPP.Permissions;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    internal class AddAdminCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "AddAdmin Usage:  /addadmin playerName");
                return;
            }

            List<Administrator> list = new List<Administrator>();
            list.Add(new Administrator(0, "Cancel"));
            Fougerite.Player fplayer = Fougerite.Server.GetServer().FindPlayer(playerName);
            if (fplayer != null)
            {
                NewAdmin(new Administrator(fplayer.UID, fplayer.Name), pl);
                return;
            }

            foreach (KeyValuePair<ulong, string> entry in Core.userCache)
            {
                if (entry.Value.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    NewAdmin(new Administrator(entry.Key, entry.Value), pl);
                    return;
                }

                if (entry.Value.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(new Administrator(entry.Key, entry.Value));
            }

            if (list.Count == 1)
            {
                foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
                {
                    if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        NewAdmin(new Administrator(client.UID, SecurityElement.Escape(client.Name)), pl);
                        return;
                    }

                    if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                        list.Add(new Administrator(client.UID, SecurityElement.Escape(client.Name)));
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
                pl.MessageFrom(Core.Name, string.Format("{0} - {1}", i, list[i].DisplayName));
            }

            pl.MessageFrom(Core.Name, "0 - Cancel");
            pl.MessageFrom(Core.Name, "Please enter the number matching the player to become administrator.");
            Core.adminAddWaitList[Arguments.argUser.userID] = list;
        }

        public void PartialNameNewAdmin(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            List<Administrator> list = (List<Administrator>)Core.adminAddWaitList[Arguments.argUser.userID];
            NewAdmin(list[id], pl);
        }

        public void NewAdmin(Administrator newAdmin, Fougerite.Player player)
        {
            if (newAdmin.UserID == player.UID)
            {
                player.MessageFrom(Core.Name, "Seriously? You are already an admin...");
            }
            else if (Administrator.IsAdmin(newAdmin.UserID))
            {
                player.MessageFrom(Core.Name, newAdmin.DisplayName + " is already an administrator.");
            }
            else
            {
                string flagstr = Core.config.GetSetting("Settings", "default_admin_flags");

                if (flagstr != null)
                {
                    List<string> flags = new List<string>(flagstr.Split(new char[] { '|' }));
                    newAdmin.Flags = flags;
                }

                Administrator.AddAdmin(newAdmin);
                Administrator.NotifyAdmins(string.Format("{0} has been made an administrator by {1}.",
                    SecurityElement.Escape(newAdmin.DisplayName), player.Name));
            }
        }
    }
}