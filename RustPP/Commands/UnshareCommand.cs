using System.Runtime.Remoting;

namespace RustPP.Commands
{
    using Fougerite;
    using System;
    using System.Collections;

    public class UnshareCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Sharing Doors Usage:  /unshare playerName");
                return;
            }

            ShareCommand command = (ShareCommand)ChatCommand.GetCommand("share");
            ArrayList shareList = (ArrayList)command.GetSharedDoors()[Arguments.argUser.userID];
            if (shareList == null)
            {
                pl.MessageFrom(Core.Name, "You aren't sharing doors with anyone.");
                return;
            }

            PList list = new PList();
            list.Add(0, "Cancel");
            foreach (ulong id in shareList)
            {
                if (Core.userCache.ContainsKey(id))
                {
                    if (Core.userCache[id].Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        UnshareDoors(new PList.Player(id, Core.userCache[id]), pl);
                    }
                    else if (Core.userCache[id].ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                        list.Add(id, Core.userCache[id]);
                }
                else
                {
                    Fougerite.Player client = Fougerite.Server.GetServer().FindPlayer(id.ToString());
                    if (client != null)
                    {
                        if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                        {
                            UnshareDoors(new PList.Player(id, client.Name), pl);
                        }
                        else if (Core.userCache[id].ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                            list.Add(id, Core.userCache[id]);
                    }
                }
            }

            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, string.Format("You aren't sharing doors with {0}.", playerName));
                return;
            }

            pl.MessageFrom(Core.Name,
                string.Format("{0}  players{1} {2}: ", ((list.Count - 1)).ToString(),
                    (((list.Count - 1) > 1) ? "s match" : " matches"), playerName));
            for (int i = 1; i < list.Count; i++)
            {
                pl.MessageFrom(Core.Name, string.Format("{0} - {1}", i, list.PlayerList[i].DisplayName));
            }

            pl.MessageFrom(Core.Name, "0 - Cancel");
            pl.MessageFrom(Core.Name, "Please enter the number matching the player you won't share doors with.");
            Core.unshareWaitList[pl.UID] = list;
        }

        public void PartialNameUnshareDoors(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            PList list = (PList)Core.unshareWaitList[pl.UID];
            UnshareDoors(list.PlayerList[id], pl);
        }

        public void UnshareDoors(PList.Player exfriend, Fougerite.Player unsharing)
        {
            ShareCommand command = (ShareCommand)ChatCommand.GetCommand("share");

            ((ArrayList)command.GetSharedDoors()[unsharing.UID]).Remove(exfriend.UserID);
            unsharing.MessageFrom(Core.Name, string.Format("{0} can use your doors no longer.", exfriend.DisplayName));
            Fougerite.Player client = Fougerite.Server.GetServer().FindPlayer(exfriend.UserID.ToString());
            if (client != null)
                client.MessageFrom(Core.Name,
                    string.Format("{0} is no longer sharing his doors with you.", unsharing.Name));
        }
    }
}