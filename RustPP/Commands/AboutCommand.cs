namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using System;

    public class AboutCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            pl.MessageFrom(Core.Name, "Fougerite is currently running Rust++ v" + Core.Version);
            pl.MessageFrom(Core.Name, "Brought to you by xEnt & EquiFox17 & the Fougerite project.");
        }
    }
}