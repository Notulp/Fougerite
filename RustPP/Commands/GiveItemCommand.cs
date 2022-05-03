namespace RustPP.Commands
{
    using Facepunch.Utility;
    using Fougerite;
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;

    internal class GiveItemCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (pl.CommandCancelList.Contains("give"))
            {
                return;
            }

            string usage = "Give Item usage:  /give  playerName  itemName  (quantity)";
            if (ChatArguments.Length < 2) // minimum arguments = 2
            {
                pl.MessageFrom(Core.Name, usage);
                return;
            }

            StringComparison ic = StringComparison.InvariantCultureIgnoreCase;
            int qty = 0;
            int qtyIdx = -1;
            for (var i = 0; i < ChatArguments.Length; i++)
            {
                string arg = ChatArguments[i];
                int test;
                if (int.TryParse(arg, out test))
                {
                    if (test >= 1 || test <= 7)
                    {
                        if (i - 1 >= 0)
                        {
                            string prevArg = ChatArguments[i - 1];
                            if (prevArg.Equals("Part", ic) || prevArg.Equals("Kit", ic))
                                continue;
                        }
                    }

                    if (test == 556)
                    {
                        if (i + 1 < ChatArguments.Length)
                        {
                            string nextArg = ChatArguments[i + 1];
                            if (nextArg.Similarity("Ammo") >= 0.75
                                || nextArg.Similarity("Casing") >= 0.8)
                                continue;
                        }
                    }

                    qty = test;
                    qtyIdx = i;
                }
            }

            if (qty == 0)
            {
                qty = 1;
            }
            else if (ChatArguments.Length < 3) // qty given, but there is < 3 arguments => invalid
            {
                pl.MessageFrom(Core.Name, usage);
                return;
            }

            string quantity = qty.ToString();
            double best = 0d;
            string[] remain = qtyIdx > -1
                ? ChatArguments.Slice(0, qtyIdx)
                    .Concat(ChatArguments.Slice(Math.Min(qtyIdx + 1, ChatArguments.Length), ChatArguments.Length))
                    .ToArray()
                : ChatArguments;

            List<string> collect = new List<string>();
            ICollection<string> matches = new List<string>();
            foreach (string name in PlayerClient.All.Select(pc => pc.netUser.displayName))
            {
                for (int i = 0; i < remain.Length; i++)
                {
                    for (int j = i; j < remain.Length; j++)
                    {
                        string[] testArr = remain.Slice(i, j + 1);
                        string testStr = string.Join(" ", testArr);
                        double sim = testStr.Similarity(name);
                        if (sim > best && sim > 0.333d)
                        {
                            best = sim;
                            matches.Clear();
                            matches.Add(name);
                            collect.Clear();
                            collect.AddRange(testArr);
                        }
                        else if (sim == best)
                        {
                            matches.Add(name);
                            collect.AddRange(testArr);
                        }
                    }
                }
            }

            for (int i = 0; i < collect.Count(); i++)
            {
                if (FougeriteEx.ItemWords.Any(x => x.LongestCommonSubstring(collect[i]).StartsWith(collect[i], ic)))
                    collect[i] = string.Empty;
            }

            if (matches.Count == 1)
            {
                string recipName = matches.First();
                string itemName = string.Join(" ", remain.Except(collect).ToArray()).MatchItemName();
                Arguments.Args = new string[] { recipName, itemName, quantity };

                Fougerite.Player np = Fougerite.Server.GetServer().FindPlayer(recipName);
                if (np == null)
                {
                    pl.MessageFrom(RustPP.Core.Name, "Coudln't find player!");
                    return;
                }

                Logger.LogDebug(string.Format("[GiveItemCommand] quantity={0} item={1} recipient={2}", quantity,
                    itemName, recipName));
                inv.giveplayer(ref Arguments);
                pl.MessageFrom(RustPP.Core.Name,
                    string.Format("{0}  {1} were placed in {2}'s inventory.", quantity, itemName, recipName));
                np.MessageFrom(RustPP.Core.Name,
                    string.Format("{0} gave you  {1}  {2}", Arguments.argUser.displayName, quantity, itemName));
            }
            else
            {
                var str = Arguments.ArgsStr.Replace(string.Join(" ", collect.ToArray()), "");
                pl.MessageFrom(Core.Name, string.Format("Unsure about {0}. Please be more specific.", str));
            }
        }
    }
}