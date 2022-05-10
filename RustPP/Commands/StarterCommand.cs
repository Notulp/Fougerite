namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using System;
    using System.Collections;

    public class StarterCommand : ChatCommand
    {
        private Hashtable starterkits = new Hashtable();

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (pl.CommandCancelList.Contains("starter"))
            {
                return;
            }

            bool flag = false;
            if (!this.starterkits.ContainsKey(pl.UID))
            {
                flag = true;
                this.starterkits.Add(pl.UID, Environment.TickCount);
            }
            else
            {
                int num = (int)this.starterkits[pl.UID];
                if ((Environment.TickCount - num) <
                    (int.Parse(Core.config.GetSetting("Settings", "starterkit_cooldown")) * 0x3e8))
                {
                    pl.MessageFrom(Core.Name, RustPPModule.StarterCDMsg);
                }
                else
                {
                    flag = true;
                    this.starterkits.Remove(pl.UID);
                    this.starterkits.Add(pl.UID, Environment.TickCount);
                }
            }

            if (flag)
            {
                for (int i = 0; i < int.Parse(Core.config.GetSetting("StarterKit", "items")); i++)
                {
                    Arguments.Args = new string[]
                    {
                        Core.config.GetSetting("StarterKit", "item" + (i + 1) + "_name"),
                        Core.config.GetSetting("StarterKit", "item" + (i + 1) + "_amount")
                    };
                    ConsoleSystem.Arg arg = Arguments;
                    inv.give(ref arg);
                }

                pl.MessageFrom(Core.Name, RustPPModule.StarterMsg);
            }
        }
    }
}