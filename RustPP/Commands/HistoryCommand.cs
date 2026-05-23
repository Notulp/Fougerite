using System;
using Fougerite;
using Fougerite.Caches;

namespace RustPP.Commands
{
    public class HistoryCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().FindPlayer(Arguments.argUser.userID);
            if (pl == null)
                return;

            int historyLimit = 10;
            string configSetting = Core.config.GetSetting("Settings", "chat_history_amount");
            if (!string.IsNullOrEmpty(configSetting))
            {
                int.TryParse(configSetting, out historyLimit);
            }

            var utilInstance = Util.GetUtil();
            var historyKeys = utilInstance.ChatHistory.KeysCopy;
            int totalEntries = historyKeys.Count;
            int displayCount = Math.Min(historyLimit, totalEntries);

            if (displayCount <= 0)
            {
                pl.Message("No chat history available.");
                return;
            }
            
            int startIndex = totalEntries - displayCount;
            for (int i = startIndex; i < totalEntries; i++)
            {
                ulong steamId = historyKeys[i];
                
                if (utilInstance.ChatHistory.TryGetValue(steamId, out string chatMessage) && chatMessage != null)
                {
                    string playername;
                    
                    CachedPlayer cachedProfile = PlayerCache.GetPlayerCache().GetPlayerBySteamId(steamId);
                    if (cachedProfile != null && !string.IsNullOrEmpty(cachedProfile.Name))
                    {
                        playername = cachedProfile.Name;
                    }
                    else
                    {
                        playername = $"SteamID_{steamId}";
                    }

                    pl.MessageFrom(playername, chatMessage);
                }
            }
        }
    }
}