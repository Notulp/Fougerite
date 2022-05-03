using Fougerite.Permissions;

namespace RustPP.Commands
{
    using Fougerite;
    using System;
    using RustPP;
    using RustPP.Permissions;
    using System.Collections.Generic;

    internal class KillCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Kill Usage:  /kill playerName");
            }
            PList list = new PList();
            list.Add(new PList.Player(0, "Cancel"));
            foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
            {
                if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    KillPlayer(client, pl);
                    return;
                }
                if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(client.UID, client.Name);
            }
            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, "No player matches the name: " + playerName);
                return;
            }
            pl.MessageFrom(Core.Name, string.Format("{0}  player{1} {2}: ", ((list.Count - 1)).ToString(), (((list.Count - 1) > 1) ? "s match" : " matches"), playerName));
            for (int i = 1; i < list.Count; i++)
            {
                pl.MessageFrom(Core.Name, string.Format("{0} - {1}", i, list.PlayerList[i].DisplayName));
            }
            pl.MessageFrom(Core.Name, "0 - Cancel");
            pl.MessageFrom(Core.Name, "Please enter the number matching the player you want to murder.");
            Core.killWaitList[pl.UID] = list;
        }

        public void PartialNameKill(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }
            PList list = (PList)Core.killWaitList[pl.UID];
            Fougerite.Player client = Fougerite.Server.GetServer().FindPlayer(list.PlayerList[id].UserID.ToString());
            if (client != null)
                KillPlayer(client, pl);
        }

        public void KillPlayer(Fougerite.Player victim, Fougerite.Player myAdmin)
        {
            if (victim == myAdmin)
            {
                myAdmin.MessageFrom(Core.Name, "Suicide isn't painless. " + Core.Name + " won't let you kill yourself.");
            }
            else if (Administrator.IsAdmin(victim.UID) && !Administrator.GetAdmin(victim.UID).HasPermission("RCON"))
            {
                myAdmin.MessageFrom(Core.Name, victim.Name + " is an administrator. May I suggest a rock?");
            }
            else
            {
                Administrator.NotifyAdmins(string.Format("{0} killed {1} with mind bullets.", myAdmin.Name, victim.Name));
                victim.MessageFrom(myAdmin.Name, string.Format("I killed you with mind bullets. That's telekinesis, {0}.", victim.Name));
                victim.Kill();
                //TakeDamage.Kill(myAdmin.PlayerClient.netUser.playerClient, victim.PlayerClient.netUser.playerClient, null);
            }
        }
    }
}