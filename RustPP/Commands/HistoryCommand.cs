namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using System;

    public class HistoryCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            for (int i = 1 + int.Parse(Core.config.GetSetting("Settings", "chat_history_amount")); i > 0; i--)
            {
                if (Fougerite.Data.GetData().chat_history_username.Count >= i)
                {
                    string playername = Fougerite.Data.GetData()
                        .chat_history_username[Fougerite.Data.GetData().chat_history_username.Count - i];
                    string arg = Fougerite.Data.GetData().chat_history[Fougerite.Data.GetData().chat_history.Count - i];
                    if (playername != null)
                    {
                        pl.MessageFrom(playername, arg);
                    }
                }
            }
        }
    }
}