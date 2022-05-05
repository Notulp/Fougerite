using System.Collections;
using RustPP.Commands;

namespace RustPP
{
    public class ShareListCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (ShareCommand.shared_doors.ContainsKey(pl.UID))
            {
                ArrayList list = ShareCommand.shared_doors[pl.UID] as ArrayList;
                if (list == null)
                {
                    pl.MessageFrom(Core.Name, "You don't have anybody on sharelist!");
                    return;
                }

                string names = "";
                foreach (object x in list)
                {
                    // Do safe casting just in case.
                    ulong UID = x is ulong ? (ulong)x : 0;
                    if (UID > 0)
                    {
                        var cache = Core.userCache;
                        if (cache.ContainsKey(UID))
                        {
                            string name = cache[UID];
                            names += name + ", ";
                        }
                        else
                        {
                            names += "Unknown, ";
                        }
                    }
                    else
                    {
                        names += "Unknown, ";
                    }
                }

                pl.MessageFrom(Core.Name, names);
            }
            else
            {
                pl.MessageFrom(Core.Name, "You don't have anybody on sharelist!");
            }
        }
    }
}