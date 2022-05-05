namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using RustPP.Permissions;
    using System;

    internal class UnbanCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Unmute Usage:  /unmute playerName");
                return;
            }

            PList list = new PList();
            list.Add(0, "Cancel");
            foreach (PList.Player banned in Core.blackList.PlayerList)
            {
                if (banned.DisplayName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    UnbanPlayer(banned, pl);
                    return;
                }

                if (banned.DisplayName.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(banned);
            }

            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, "No banned player matches the name: " + playerName);
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
            pl.MessageFrom(Core.Name, "Please enter the number matching the player to unban.");
            Core.unbanWaitList[pl.UID] = list;
        }

        public void PartialNameUnban(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            PList list = (PList)Core.unbanWaitList[pl.UID];
            UnbanPlayer(list.PlayerList[id], pl);
        }

        public void UnbanPlayer(PList.Player unban, Fougerite.Player myAdmin)
        {
            Core.blackList.Remove(unban.UserID);
            Administrator.NotifyAdmins(string.Format("{0} has been unbanned by {1}.", unban.DisplayName, myAdmin.Name));
        }
    }
}