namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using System;

    public class LoadoutCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            int items;
            if (int.TryParse(Core.config.GetSetting("AdminLoadout", "items"), out items))
            {
                var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
                for (int i = 1; i <= items; i++)
                {
                    string name = Core.config.GetSetting("AdminLoadout", "item" + i + "_name");
                    string amount = Core.config.GetSetting("AdminLoadout", "item" + i + "_amount");
                    Arguments.Args = new string[] { name, amount };
                    string newargs = Arguments.ArgsStr;
                    inv.give(ref Arguments);
                    Logger.LogDebug(string.Format("[Loadout] gave {0} to {1}", newargs, Arguments.argUser.displayName));
                }

                pl.MessageFrom(Core.Name, "You have spawned an Admin Loadout!");
            }
        }
    }
}