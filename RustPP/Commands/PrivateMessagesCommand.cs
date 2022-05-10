namespace RustPP.Commands
{
    using Facepunch.Utility;
    using Fougerite;
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;

    public class PrivateMessagesCommand : ChatCommand
    {
        string green = "[color #009900]";
        string teal = "[color #00FFFF]";

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var sender = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (ChatArguments.Length < 2)
            {
                sender.MessageFrom(Core.Name, "Private Message Usage:  /pm playerName message");
                return;
            }

            string search = ChatArguments[0];
            Fougerite.Player recipient = Fougerite.Player.FindByName(search);
            if (recipient == null)
            {
                sender.MessageFrom(Core.Name, "Couldn't find player " + search);
                return;
            }

            List<string> wth = ChatArguments.ToList();
            wth.Remove(wth[0]);
            string message;
            try
            {
                message = string.Join(" ", wth.ToArray()).Replace(search, "").Trim(new char[] { ' ', '"' })
                    .Replace('"', 'ˮ');
            }
            catch
            {
                sender.MessageFrom(Core.Name, "Something went wrong. Try again.");
                return;
            }

            if (message == string.Empty)
            {
                sender.MessageFrom(Core.Name, "Private Message Usage: /pm playerName message");
            }
            else
            {
                recipient.MessageFrom("PrivateMessage", green + "(" + sender.Name + " -> You):  " + teal + message);
                sender.MessageFrom("PrivateMessage", green + "(You -> " + recipient.Name + "):  " + teal + message);
                //Util.say(recipient.netPlayer, string.Format("\"PM from {0}\"", Arguments.argUser.displayName.Replace('"', 'ˮ')), string.Format("\"{0}\"", message));
                //Util.say(Arguments.argUser.networkPlayer,string.Format("\"PM to {0}\"", recipient.netUser.displayName.Replace('"', 'ˮ')),string.Format("\"{0}\"", message));
                Hashtable replies = (ChatCommand.GetCommand("r") as ReplyCommand).GetReplies();
                if (replies.ContainsKey(recipient.Name))
                    replies[recipient.Name] = sender.Name;
                else
                    replies.Add(recipient.Name, sender.Name);
            }
        }
    }
}