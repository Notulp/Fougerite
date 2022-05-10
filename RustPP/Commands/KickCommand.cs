using Fougerite.Permissions;

namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using RustPP.Permissions;
    using System;
    using System.Collections.Generic;

    internal class KickCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Kick Usage:  /kick playerName");
            }

            PList list = new PList();
            list.Add(0, "Cancel");
            foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
            {
                if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    KickPlayer(client, pl);
                    return;
                }
                else if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(client.UID, client.Name);
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
            pl.MessageFrom(Core.Name, "Please enter the number matching the player to kick.");
            Core.kickWaitList[pl.UID] = list;
        }

        public void PartialNameKick(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            PList list = (PList)Core.kickWaitList[pl.UID];
            Fougerite.Player client = Fougerite.Server.GetServer().FindPlayer(list.PlayerList[id].UserID.ToString());
            if (client != null) KickPlayer(client, pl);
        }

        public void KickPlayer(Fougerite.Player badPlayer, Fougerite.Player myAdmin)
        {
            if (badPlayer == myAdmin)
            {
                myAdmin.MessageFrom(Core.Name, "You can't kick yourself.");
            }
            else if (Administrator.IsAdmin(badPlayer.UID)
                     && !Administrator.GetAdmin(myAdmin.UID).HasPermission("RCON")
                     && !PermissionSystem.GetPermissionSystem().PlayerHasPermission(myAdmin.UID, "RCON"))
            {
                myAdmin.MessageFrom(Core.Name, badPlayer.Name + " is an administrator. You can't kick administrators.");
            }
            else
            {
                Administrator.NotifyAdmins(string.Format("{0} has been kicked by {1}.", badPlayer.Name, myAdmin.Name));
                badPlayer.Disconnect();
            }
        }
    }
}