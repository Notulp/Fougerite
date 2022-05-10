namespace RustPP.Commands
{
    using Fougerite;
    using Rust;
    using System;

    public class AnnounceCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            string strText = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });

            if (strText == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Announce Usage:  /announce your message here");
            }
            else
            {
                char ch = '☢';
                foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
                {
                    client.Notice(ch.ToString(), strText, 5f);
                }
            }
        }
    }
}