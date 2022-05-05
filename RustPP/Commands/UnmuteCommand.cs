namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using RustPP.Permissions;
    using System;
    using System.Collections.Generic;

    internal class UnmuteCommand : ChatCommand
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
            foreach (PList.Player muted in Core.muteList.PlayerList)
            {
                Logger.LogDebug(string.Format("[UnmuteCommand] muted.DisplayName={0}, playerName={1}",
                    muted.DisplayName, playerName));
                if (muted.DisplayName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    UnmutePlayer(muted, pl);
                    return;
                }

                if (muted.DisplayName.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(muted);
            }

            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, "No player in the mute list matches the name: " + playerName);
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
            pl.MessageFrom(Core.Name, "Please enter the number matching the player to unmute.");
            Core.unmuteWaitList[pl.UID] = list;
        }

        public void PartialNameUnmute(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            PList list = (PList)Core.unmuteWaitList[pl.UID];
            UnmutePlayer(list.PlayerList[id], pl);
        }

        public void UnmutePlayer(PList.Player unmute, Fougerite.Player myAdmin)
        {
            Core.muteList.Remove(unmute.UserID);
            Administrator.NotifyAdmins(string.Format("{0} has been unmuted by {1}.", unmute.DisplayName, myAdmin.Name));
            Fougerite.Player client = Fougerite.Server.GetServer().FindPlayer(unmute.UserID.ToString());
            if (client != null)
                client.MessageFrom(Core.Name, string.Format("You have been unmuted by {0}", myAdmin.Name));
        }
    }
}