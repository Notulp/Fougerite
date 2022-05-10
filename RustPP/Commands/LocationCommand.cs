namespace RustPP.Commands
{
    using Fougerite;
    using RustPP.Permissions;
    using System;

    public class LocationCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string targetName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (targetName.Equals(string.Empty) || targetName.Equals(Arguments.argUser.displayName))
            {
                string reply;
                if (GetLocationString(ref Arguments.argUser, pl, out reply))
                {
                    Arguments.ReplyWith(reply);
                    pl.MessageFrom(Core.Name, reply);
                }

                return;
            }

            if (!Administrator.IsAdmin(pl.UID))
            {
                pl.MessageFrom(Core.Name, "Only Administrators can get the locations of other players.");
                return;
            }

            foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
            {
                if (targetName.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                    targetName.Equals(client.Name, StringComparison.OrdinalIgnoreCase) ||
                    targetName.ToUpperInvariant().Contains(client.Name.ToUpperInvariant()))
                {
                    string reply;
                    if (GetLocationString(ref Arguments.argUser, client, out reply))
                    {
                        Arguments.ReplyWith(reply);
                        pl.MessageFrom(Core.Name, reply);
                    }
                }
            }
        }

        public bool GetLocationString(ref NetUser source, Fougerite.Player location, out string reply)
        {
            bool flag = false;
            try
            {
                string[] v3 = location.Location.ToString("F").Trim(new char[] { '(', ')', ' ' })
                    .Split(new char[] { ',' });
                reply = string.Format("{3} Location: X:{0} Y:{1} Z:{2}", v3[0], v3[1], v3[2],
                    (location.PlayerClient.netUser == source ? "Your" : string.Format("{0}'s", location.Name)));
                flag = true;
            }
            catch (Exception)
            {
                reply = string.Empty;
            }

            return flag;
        }
    }
}