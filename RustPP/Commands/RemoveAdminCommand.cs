namespace RustPP.Commands
{
    using Fougerite;
    using RustPP.Permissions;
    using System;
    using System.Collections.Generic;

    internal class RemoveAdminCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Remove Admin Usage:  /unadmin playerName");
                return;
            }

            Fougerite.Player p = Server.GetServer().FindPlayer(playerName);
            if (p != null)
            {
                Administrator nadministrator = Administrator.AdminList.Find(delegate(Administrator obj)
                {
                    return obj.UserID == p.UID;
                });
                if (nadministrator != null)
                {
                    RemoveAdmin(nadministrator, pl);
                    return;
                }
            }

            List<Administrator> list = new List<Administrator>();
            list.Add(new Administrator(0, "Cancel"));
            Administrator administrator = Administrator.AdminList.Find(delegate(Administrator obj)
            {
                return obj.DisplayName.Equals(playerName, StringComparison.OrdinalIgnoreCase);
            });
            if (administrator != null)
            {
                RemoveAdmin(administrator, pl);
                return;
            }

            list.AddRange(Administrator.AdminList.FindAll(delegate(Administrator obj)
            {
                return obj.DisplayName.ToUpperInvariant().Contains(playerName.ToUpperInvariant());
            }));
            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, string.Format("No adminstrator matches the name:  {0}", playerName));
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
            pl.MessageFrom(Core.Name, "Please enter the number matching the adminstrator to remove.");
            Core.adminRemoveWaitList[pl.UID] = list;
        }

        public void PartialNameRemoveAdmin(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            List<Administrator> list = (List<Administrator>)Core.adminRemoveWaitList[pl.UID];
            RemoveAdmin(list[id], pl);
        }

        public void RemoveAdmin(Administrator exAdmin, Fougerite.Player myAdmin)
        {
            if (exAdmin.UserID == myAdmin.UID)
            {
                myAdmin.MessageFrom(Core.Name, "You can't remove yourself as admin.");
            }
            else
            {
                Administrator.NotifyAdmins(string.Format("{0} is no longer an administrator; removed by {1}.",
                    exAdmin.DisplayName, myAdmin.Name));
                Administrator.DeleteAdmin(exAdmin.UserID);
            }
        }
    }
}