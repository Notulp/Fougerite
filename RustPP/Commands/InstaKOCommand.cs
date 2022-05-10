namespace RustPP.Commands
{
    using Fougerite;
    using System;
    using System.Collections.Generic;

    public class InstaKOCommand : ChatCommand
    {
        public System.Collections.Generic.List<ulong> userIDs = new System.Collections.Generic.List<ulong>();

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (pl.CommandCancelList.Contains("instako"))
            {
                if (userIDs.Contains(pl.UID))
                {
                    userIDs.Remove(pl.UID);
                    pl.MessageFrom(Core.Name, "InstaKO mode has been deactivated!");
                }

                return;
            }

            if (!this.userIDs.Contains(pl.UID))
            {
                this.userIDs.Add(pl.UID);
                pl.MessageFrom(Core.Name, "InstaKO mode has been activated!");
            }
            else
            {
                this.userIDs.Remove(pl.UID);
                pl.MessageFrom(Core.Name, "InstaKO mode has been deactivated!");
            }
        }

        public bool IsOn(ulong uid)
        {
            return this.userIDs.Contains(uid);
        }
    }
}