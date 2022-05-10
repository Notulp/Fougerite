namespace RustPP.Commands
{
    using Fougerite;
    using System;

    public class PlayersCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            pl.MessageFrom(Core.Name,
                string.Concat(new object[]
                    { PlayerClient.All.Count, " Player", (PlayerClient.All.Count > 1) ? "s" : "", " Online: " }));
            int num = 0;
            int num2 = 0;
            string str = "";
            foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
            {
                num2++;
                if (num2 >= 60)
                {
                    num = 0;
                    break;
                }

                str = str + client.Name + ", ";
                if (num == 6)
                {
                    num = 0;
                    pl.MessageFrom(Core.Name, str.Substring(0, str.Length - 2));
                    str = "";
                }
                else
                {
                    num++;
                }
            }

            if (num != 0)
            {
                pl.MessageFrom(Core.Name, str.Substring(0, str.Length - 2));
            }
        }
    }
}