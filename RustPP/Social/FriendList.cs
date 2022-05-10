namespace RustPP.Social
{
    using Fougerite;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    public class FriendList : ArrayList
    {
        public void AddFriend(string fName, ulong fUID)
        {
            this.Add(new Friend(fName, fUID));
        }

        public string GetRealName(string name)
        {
            foreach (Friend friend in this)
            {
                if (name.Equals(friend.GetDisplayName(), StringComparison.OrdinalIgnoreCase))
                {
                    return friend.GetDisplayName();
                }
            }
            return name;
        }

        public bool HasFriends()
        {
            return (this.Count != 0);
        }

        public bool isFriendWith(string name)
        {
            foreach (Friend friend in this)
            {
                if (friend.GetDisplayName().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool isFriendWith(ulong userID)
        {
            foreach (Friend friend in this)
            {
                if (friend.GetUserID() == userID)
                {
                    return true;
                }
            }
            return false;
        }

        public void OutputList(ref ConsoleSystem.Arg arg)
        {
            var pl = Server.GetServer().GetCachePlayer(arg.argUser.userID);
            List<string> onlineFriends = new List<string>();
            List<string> offlineFriends = new List<string>();
            foreach (Friend friend in this)
            {
                Fougerite.Player client = Fougerite.Server.GetServer().FindPlayer(friend.GetUserID().ToString());
                if (client != null)
                {
                    onlineFriends.Add(client.Name);
                    friend.SetDisplayName(client.Name);
                } else
                {
                    offlineFriends.Add(friend.GetDisplayName());
                }
            }

            int friendsPerRow = 7;
            pl.MessageFrom(Core.Name,
                string.Format("You currently have {0} friend{1} online:",
                    (onlineFriends.Count == 0 ? "no" : onlineFriends.Count.ToString()), ((onlineFriends.Count != 1) ? "s" : string.Empty)));

            if (onlineFriends.Count <= friendsPerRow && onlineFriends.Count > 0)
            {
                pl.MessageFrom(Core.Name, string.Join(", ", onlineFriends.ToArray()));
            } else if (onlineFriends.Count > 0)
            {
                int i = friendsPerRow;
                for (; i <= onlineFriends.Count; i += friendsPerRow)
                {
                    pl.MessageFrom(Core.Name, string.Join(", ", onlineFriends.GetRange(i - friendsPerRow, friendsPerRow).ToArray()));
                }
                if (offlineFriends.Count % friendsPerRow > 0 || i - friendsPerRow == friendsPerRow)
                    pl.MessageFrom(Core.Name, string.Join(", ", onlineFriends.GetRange(i - friendsPerRow, offlineFriends.Count % friendsPerRow).ToArray()));
            }

            pl.MessageFrom(Core.Name,
                string.Format("You have {0} offline friend{1}:",
                    (offlineFriends.Count == 0 ? "no" : offlineFriends.Count.ToString()), ((offlineFriends.Count != 1) ? "s" : string.Empty)));
           
            if (offlineFriends.Count <= friendsPerRow && offlineFriends.Count > 0)
            {
                pl.MessageFrom(Core.Name, string.Join(", ", offlineFriends.ToArray()));
            } else if (offlineFriends.Count > 0)
            {
                int i = friendsPerRow;
                for (; i <= offlineFriends.Count; i += friendsPerRow)
                {
                    pl.MessageFrom(Core.Name, string.Join(", ", offlineFriends.GetRange(i - friendsPerRow, friendsPerRow).ToArray()));
                }
                if (offlineFriends.Count % friendsPerRow > 0 || i - friendsPerRow == friendsPerRow)
                {
                    pl.MessageFrom(Core.Name, string.Join(", ", offlineFriends.GetRange(i - friendsPerRow, offlineFriends.Count % friendsPerRow).ToArray()));
                }
            }
        }

        public void RemoveFriend(string fName)
        {
            foreach (Friend friend in this)
            {
                if (fName.Equals(friend.GetDisplayName(), StringComparison.OrdinalIgnoreCase))
                {
                    this.Remove(friend);
                    break;
                }
            }
        }

        public void RemoveFriend(ulong fUID)
        {
            foreach (Friend friend in this)
            {
                if (fUID == friend.GetUserID())
                {
                    this.Remove(friend);
                    break;
                }
            }
        }

        [Serializable]
        public class Friend
        {
            private string _displayName;
            private ulong _userID;

            public Friend(string dName, ulong uID)
            {
                this._displayName = dName;
                this._userID = uID;
            }

            public string GetDisplayName()
            {
                return this._displayName;
            }

            public ulong GetUserID()
            {
                return this._userID;
            }

            public void SetDisplayName(string name)
            {
                this._displayName = name;
            }
        }
    }
}