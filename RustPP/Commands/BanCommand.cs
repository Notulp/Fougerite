using Fougerite.Permissions;

namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using RustPP.Permissions;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    internal class BanCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string queryName = Arguments.ArgsStr.Trim(new char[] { ' ', '"' });
            if (queryName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Ban Usage:  /ban playerName");
                return;
            }

            var query = from entry in Core.userCache
                let sim = entry.Value.Similarity(queryName)
                where sim > 0.4d
                group new PList.Player(entry.Key, entry.Value) by sim
                into matches
                select matches.FirstOrDefault();

            if (query.Count() == 1)
            {
                BanPlayer(query.First(), pl);
            }
            else
            {
                pl.MessageFrom(Core.Name, string.Format("{0}  players match  {1}: ", query.Count(), queryName));
                for (int i = 1; i < query.Count(); i++)
                {
                    pl.MessageFrom(Core.Name, string.Format("{0} - {1}", i, query.ElementAt(i).DisplayName));
                }

                pl.MessageFrom(Core.Name, "0 - Cancel");
                pl.MessageFrom(Core.Name, "Please enter the number matching the player to ban.");
                Core.banWaitList[pl.UID] = query;
            }
        }

        public void PartialNameBan(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Canceled!");
                return;
            }

            if (Core.banWaitList[pl.UID] is IEnumerable<PList.Player> list) BanPlayer(list.ElementAt(id), pl);
        }

        public void BanPlayer(PList.Player ban, Fougerite.Player myAdmin)
        {
            if (ban.UserID == myAdmin.UID)
            {
                myAdmin.MessageFrom(Core.Name, "Seriously? You can't ban yourself.");
                return;
            }

            if (Administrator.IsAdmin(ban.UserID)
                && !Administrator.GetAdmin(myAdmin.UID).HasPermission("RCON")
                && !PermissionSystem.GetPermissionSystem().PlayerHasPermission(myAdmin.UID, "RCON"))
            {
                myAdmin.MessageFrom(Core.Name, ban.DisplayName + " is an administrator. You can't ban administrators.");
                return;
            }

            if (RustPP.Core.blackList.Contains(ban.UserID))
            {
                Logger.LogError(string.Format("[BanPlayer] {0}, id={1} already on blackList.", ban.DisplayName,
                    ban.UserID));
                Core.blackList.Remove(ban.UserID);
            }

            Core.blackList.Add(ban);
            Administrator.DeleteAdmin(ban.UserID);
            Administrator.NotifyAdmins(string.Format("{0} has been banned by {1}.", ban.DisplayName, myAdmin.Name));
            Fougerite.Player client = Fougerite.Server.GetServer().FindPlayer(ban.UserID.ToString());
            if (client != null)
                client.Disconnect();
        }
    }
}