namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using RustPP.Social;
    using System;
    using System.Collections.Generic;
    using System.Security;

    internal class AddFriendCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Friends Management Usage:  /addfriend playerName");
                return;
            }

            PList list = new PList();
            list.Add(0, "Cancel");
            foreach (KeyValuePair<ulong, string> entry in Core.userCache)
            {
                if (entry.Value.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                {
                    AddFriend(new PList.Player(entry.Key, entry.Value), pl);
                    return;
                }
                else if (entry.Value.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                    list.Add(entry.Key, entry.Value);
            }

            if (list.Count == 1)
            {
                foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
                {
                    if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        AddFriend(new PList.Player(client.UID, client.Name), pl);
                        return;
                    }

                    if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                        list.Add(client.UID, client.Name);
                }
            }

            if (list.Count == 1)
            {
                pl.MessageFrom(Core.Name, string.Format("No player matches the name {0}. Sorry.", playerName));
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
            pl.MessageFrom(Core.Name, "Please enter the number matching the player to add as your friend.");
            Core.friendWaitList[pl.UID] = list;
        }

        public void PartialNameAddFriend(ref ConsoleSystem.Arg Arguments, int id)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (id == 0)
            {
                pl.MessageFrom(Core.Name, "Cancelled!");
                return;
            }

            PList list = (PList)Core.friendWaitList[pl.UID];
            AddFriend(list.PlayerList[id], pl);
        }

        public void AddFriend(PList.Player friend, Fougerite.Player friending)
        {
            if (friending.UID == friend.UserID)
            {
                friending.MessageFrom(Core.Name, "You can't add yourself as a friend!");
                return;
            }

            FriendsCommand command = (FriendsCommand)ChatCommand.GetCommand("friends");
            FriendList list = (FriendList)command.GetFriendsLists()[friending.UID];
            if (list == null)
            {
                list = new FriendList();
            }

            if (list.isFriendWith(friend.UserID))
            {
                friending.MessageFrom(Core.Name,
                    string.Format("You are already friends with {0}.", friend.DisplayName));
                return;
            }

            list.AddFriend(SecurityElement.Escape(friend.DisplayName), friend.UserID);
            command.GetFriendsLists()[friending.UID] = list;
            friending.MessageFrom(Core.Name,
                string.Format("You have added {0} to your friends list.", friend.DisplayName));
            Fougerite.Player ffriend = Fougerite.Server.GetServer().FindPlayer(friend.UserID.ToString());
            if (ffriend != null)
                ffriend.MessageFrom(Core.Name,
                    string.Format("{0} has added you to their friends list.", friending.Name));
        }
    }
}