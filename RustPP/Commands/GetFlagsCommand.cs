namespace RustPP.Commands
{
    using Fougerite;
    using RustPP.Permissions;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public class GetFlagsCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            string queryName = Arguments.ArgsStr.Trim(new char[] { ' ', '"' });

            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (queryName == string.Empty)
            {
                pl.MessageFrom(RustPP.Core.Name, "Get Admin Flags Usage:  /getflags playerName");
                return;
            }

            var query = from admin in Administrator.AdminList
                let sim = admin.DisplayName.Similarity(queryName)
                where sim > 0.4d
                group admin by sim
                into matches
                select matches.FirstOrDefault();

            if (query.Count() == 1)
            {
                GetFlags(query.First(), pl);
                return;
            }
            else
            {
                pl.MessageFrom(RustPP.Core.Name,
                    string.Format("{0}  administrators match  {1}: ", query.Count(), queryName));
                for (int i = 1; i < query.Count(); i++)
                {
                    pl.MessageFrom(RustPP.Core.Name, string.Format("{0} - {1}", i, query.ElementAt(i).DisplayName));
                }

                pl.MessageFrom(RustPP.Core.Name, "0 - Cancel");
                pl.MessageFrom(RustPP.Core.Name,
                    "Please enter the number matching the administrator you were looking for.");
                RustPP.Core.adminAddWaitList[pl.UID] = query;
            }

            List<Administrator> list = new List<Administrator>();
            list.Add(new Administrator(0, "Cancel"));
            foreach (Administrator administrator in Administrator.AdminList)
            {
                if (administrator.DisplayName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    GetFlags(administrator, pl);
                    return;
                }

                if (administrator.DisplayName.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(administrator);
            }

            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, playerName + " is not an administrator.");
                return;
            }

            if (list.Count == 2)
            {
                GetFlags(list[1], pl);
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
            pl.MessageFrom(Core.Name, "Please enter the number matching the administrator you were looking for.");
            Core.adminFlagsWaitList[pl.UID] = list;
        }

        public void PartialNameGetFlags(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            List<Administrator> list = (List<Administrator>)Core.adminFlagsWaitList[pl.UID];
            GetFlags(list[id], pl);
        }

        public void GetFlags(Administrator administrator, Fougerite.Player myAdmin)
        {
            myAdmin.MessageFrom(Core.Name, string.Format("{0}'s Flags: ", administrator.DisplayName));
            int flagsPerRow = 7;
            if (administrator.Flags.Count <= flagsPerRow && administrator.Flags.Count > 0)
            {
                myAdmin.MessageFrom(Core.Name, string.Join(", ", administrator.Flags.ToArray()));
                return;
            }

            if (administrator.Flags.Count > 0)
            {
                int i = flagsPerRow;
                for (; i <= administrator.Flags.Count; i += flagsPerRow)
                {
                    myAdmin.MessageFrom(Core.Name,
                        string.Join(", ", administrator.Flags.GetRange(i - flagsPerRow, flagsPerRow).ToArray()));
                }

                if (administrator.Flags.Count % flagsPerRow > 0 || i - flagsPerRow == flagsPerRow)
                    myAdmin.MessageFrom(Core.Name,
                        string.Join(", ",
                            administrator.Flags.GetRange(i - flagsPerRow, administrator.Flags.Count % flagsPerRow)
                                .ToArray()));
            }
        }
    }
}