namespace RustPP.Commands
{
    using Fougerite;
    using System;
    using RustPP;
    using RustPP.Permissions;
    using System.Collections.Generic;

    internal class WhiteListAddCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Whitelist Usage:  /addwl playerName");
                return;
            }

            PList list = new PList();
            list.Add(0, "Cancel");
            foreach (KeyValuePair<ulong, string> entry in Core.userCache)
            {
                if (entry.Value.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    Whitelist(new PList.Player(entry.Key, entry.Value), pl);
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
                        Whitelist(new PList.Player(client.UID, client.Name), pl);
                        return;
                    }

                    if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                        list.Add(client.UID, client.Name);
                }
            }

            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, "No player found with the name: " + playerName);
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
            pl.MessageFrom(Core.Name, "Please enter the number matching the player to whitelist.");
            Core.whiteWaitList[pl.UID] = list;
        }

        public void PartialNameWhitelist(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (Core.whiteWaitList.Contains(pl.UID))
            {
                if (id == 0)
                {
                    pl.MessageFrom(Core.Name, "Cancelled!");
                    return;
                }

                PList list = (PList)Core.whiteWaitList[pl.UID];
                Whitelist(list.PlayerList[id], pl);
            }
        }

        public void Whitelist(PList.Player white, Fougerite.Player myAdmin)
        {
            if (Core.whiteList.Contains(white.UserID))
            {
                myAdmin.MessageFrom(Core.Name, white.DisplayName + " is already whitelisted.");
            }
            else
            {
                Core.whiteList.Add(white);
                Administrator.NotifyAdmins(string.Format("{0} has been whitelisted by {1}.", white.DisplayName,
                    myAdmin.Name));
            }
        }
    }
}