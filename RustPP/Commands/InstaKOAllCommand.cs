using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fougerite;
using RustPP.Commands;

namespace RustPP
{
    public class InstaKOAllCommand : ChatCommand
    {
        public System.Collections.Generic.List<ulong> userIDs = new System.Collections.Generic.List<ulong>();

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (pl.CommandCancelList.Contains("instakoall"))
            {
                if (userIDs.Contains(pl.UID))
                {
                    userIDs.Remove(pl.UID);
                    pl.MessageFrom(Core.Name, "InstaKO ALL mode has been deactivated!");
                }

                return;
            }

            if (!this.userIDs.Contains(pl.UID))
            {
                this.userIDs.Add(pl.UID);
                pl.MessageFrom(Core.Name, "InstaKO ALL mode has been activated!");
            }
            else
            {
                this.userIDs.Remove(pl.UID);
                pl.MessageFrom(Core.Name, "InstaKO ALL mode has been deactivated!");
            }
        }

        public bool IsOn(ulong uid)
        {
            return this.userIDs.Contains(uid);
        }
    }
}