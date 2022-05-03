namespace RustPP.Commands
{
    using Fougerite;
    using System;
    using System.Collections;

    public class ReplyCommand : ChatCommand
    {
        private Hashtable replies = new Hashtable();
        string green = "[color #009900]";
        string teal = "[color #00FFFF]";

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            Fougerite.Player sender = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (ChatArguments.Length >= 1)
            {
                if (this.replies.ContainsKey(sender.Name))
                {
                    string replyTo = (string)this.replies[sender.Name];
                    Fougerite.Player recipient = Fougerite.Server.GetServer().FindPlayer(replyTo);
                    if (recipient == null)
                    {
                        sender.MessageFrom(Core.Name, "Couldn't find player " + replyTo);
                        this.replies.Remove(sender.Name);
                        return;
                    }

                    string message = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' }).Replace('"', 'ˮ');
                    if (message == string.Empty)
                    {
                        sender.MessageFrom(Core.Name, "Reply Command Usage: /r message");
                        return;
                    }

                    recipient.MessageFrom("PrivateMessage", green + "(" + sender.Name + " -> You):  " + teal + message);
                    sender.MessageFrom("PrivateMessage", green + "(You -> " + recipient.Name + "):  " + teal + message);
                    if (this.replies.ContainsKey(replyTo))
                    {
                        this.replies[replyTo] = sender.Name;
                    }
                    else
                    {
                        this.replies.Add(replyTo, sender.Name);
                    }
                }
                else
                {
                    sender.MessageFrom(Core.Name, "There's nobody to answer.");
                }
            }
            else
            {
                sender.MessageFrom(Core.Name, "Reply Command Usage:  /r message");
            }
        }

        public Hashtable GetReplies()
        {
            return this.replies;
        }

        public void SetReplies(Hashtable rep)
        {
            this.replies = rep;
        }
    }
}