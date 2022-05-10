namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using System;
    using System.IO;

    public class RulesCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (File.Exists(RustPPModule.GetAbsoluteFilePath("rules.txt")))
            {
                foreach (string str in File.ReadAllLines(RustPPModule.GetAbsoluteFilePath("rules.txt")))
                {
                    pl.MessageFrom(Core.Name, str);
                }
            }
            else
            {
                pl.MessageFrom(Core.Name, "No rules are currently set.");
            }
        }
    }
}