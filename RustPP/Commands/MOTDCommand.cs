using Fougerite;

namespace RustPP.Commands
{
    using RustPP;
    using System;

    internal class MOTDCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            Core.motd(pl);
        }
    }
}