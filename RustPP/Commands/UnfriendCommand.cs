namespace RustPP.Commands
{
    using Fougerite;
    using RustPP.Social;
    using System;
    using System.Collections.Generic;

    public class UnfriendCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Friends Management Usage:  /unfriend playerName");
                return;
            }

            FriendsCommand command = (FriendsCommand)ChatCommand.GetCommand("friends");
            FriendList friendsList = (FriendList)command.GetFriendsLists()[pl.UID];
            if (friendsList == null)
            {
                pl.MessageFrom(Core.Name, "You currently have no friends.");
                return;
            }

            if (friendsList.isFriendWith(playerName))
            {
                friendsList.RemoveFriend(playerName);
                pl.MessageFrom(Core.Name, "You have removed " + playerName + " from your friends list.");
                if (friendsList.HasFriends())
                {
                    command.GetFriendsLists()[pl.UID] = friendsList;
                }
                else
                {
                    command.GetFriendsLists().Remove(pl.UID);
                }
            }
            else
            {
                PList list = new PList();
                list.Add(0, "Cancel");
                foreach (KeyValuePair<ulong, string> entry in Core.userCache)
                {
                    if (friendsList.isFriendWith(entry.Key) &&
                        entry.Value.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                        list.Add(entry.Key, entry.Value);
                }

                if (list.Count == 1)
                {
                    foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
                    {
                        if (friendsList.isFriendWith(client.UID) &&
                            client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                            list.Add(client.UID, client.Name);
                    }
                }

                if (list.Count == 1)
                {
                    pl.MessageFrom(Core.Name, string.Format("You are not friends with {0}.", playerName));
                    return;
                }

                pl.MessageFrom(Core.Name,
                    string.Format("{0}  friend{1} {2}: ", ((list.Count - 1)).ToString(),
                        (((list.Count - 1) > 1) ? "s match" : " matches"), playerName));
                for (int i = 1; i < list.Count; i++)
                {
                    pl.MessageFrom(Core.Name, string.Format("{0} - {1}", i, list.PlayerList[i].DisplayName));
                }

                pl.MessageFrom(Core.Name, "0 - Cancel");
                pl.MessageFrom(Core.Name, "Please enter the number matching the friend to remove.");
                Core.unfriendWaitList[pl.UID] = list;
            }
        }

        public void PartialNameUnfriend(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            PList list = (PList)Core.unfriendWaitList[pl.UID];
            Unfriend(list.PlayerList[id], pl);
        }

        public void Unfriend(PList.Player exfriend, Fougerite.Player unfriending)
        {
            FriendsCommand command = (FriendsCommand)ChatCommand.GetCommand("friends");
            FriendList friendsList = (FriendList)command.GetFriendsLists()[unfriending.UID];

            friendsList.RemoveFriend(exfriend.UserID);
            command.GetFriendsLists()[unfriending.UID] = friendsList;
            unfriending.MessageFrom(Core.Name,
                string.Format("You have removed {0} from your friends list.", exfriend.DisplayName));
        }
    }
}