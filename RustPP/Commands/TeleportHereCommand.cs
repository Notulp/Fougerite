namespace RustPP.Commands
{
    using Fougerite;
    using System;
    using System.Collections.Generic;

    public class TeleportHereCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });

            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Teleport Usage:  /tphere playerName");
                return;
            }

            if (playerName.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
                {
                    Arguments.Args = new string[] { client.Name, pl.Name };
                    teleport.toplayer(ref Arguments);
                }

                pl.MessageFrom(Core.Name, "You have teleported all players to your location");
                return;
            }

            List<string> list = new List<string>();
            list.Add("TargetToHere");
            foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
            {
                if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                {
                    if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.Args = new string[] { client.Name, pl.Name };
                        teleport.toplayer(ref Arguments);
                        pl.MessageFrom(Core.Name, "You have teleported " + client.Name + " to your location");
                        return;
                    }

                    list.Add(client.Name);
                }
            }

            if (list.Count > 1)
            {
                pl.MessageFrom(Core.Name,
                    ((list.Count - 1)).ToString() + " Player" + (((list.Count - 1) > 1) ? "s" : "") + " were found: ");
                for (int j = 1; j < list.Count; j++)
                {
                    pl.MessageFrom(Core.Name, j + " - " + list[j]);
                }

                pl.MessageFrom(Core.Name, "0 - Cancel");
                pl.MessageFrom(Core.Name, "Please enter the number matching the player you were looking for.");
                TeleportToCommand command = ChatCommand.GetCommand("tpto") as TeleportToCommand;
                command.GetTPWaitList().Add(pl.UID, list);
            }
            else
            {
                pl.MessageFrom(Core.Name, "No player found with the name: " + playerName);
            }
        }
    }
}