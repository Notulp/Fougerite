using System.Runtime.InteropServices;
using UnityEngine;

namespace RustPP.Commands
{
    using Fougerite;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class TeleportToCommand : ChatCommand
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        delegate IntPtr LoadLibrary_Delegate(string lpFileName);


        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);


        public static Hashtable tpWaitList = new Hashtable();

        public bool V3Equal(Vector3 a, Vector3 b)
        {
            return Vector3.SqrMagnitude(a - b) < 0.0001;
        }

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (ChatArguments.Length == 3)
            {
                float n, n2, n3;
                bool b = float.TryParse(ChatArguments[0], out n);
                bool b2 = float.TryParse(ChatArguments[1], out n2);
                bool b3 = float.TryParse(ChatArguments[2], out n3);
                if (b && b2 && b3)
                {
                    pl.TeleportTo(n, n2, n3, false);
                    pl.MessageFrom(Core.Name, "You have teleported to the coords!");
                    return;
                }
            }

            string playerName = string.Join(" ", ChatArguments).Trim(new char[] { ' ', '"' });
            if (playerName == string.Empty)
            {
                pl.MessageFrom(Core.Name, "Teleport Usage:  /tpto playerName");
                return;
            }

            List<string> list = new List<string>();
            list.Add("ToTarget");
            foreach (Fougerite.Player client in Fougerite.Server.GetServer().Players)
            {
                if (client.Name.ToUpperInvariant().Contains(playerName.ToUpperInvariant()))
                {
                    if (client.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        Arguments.Args = new string[] { pl.Name, client.Name };
                        if (client.IsOnline)
                        {
                            if (V3Equal(client.Location, Vector3.zero))
                            {
                                pl.MessageFrom(Core.Name, client.Name + " is still loading and has null position!");
                                return;
                            }

                            pl.TeleportTo(client, 1.5f, false);
                            pl.MessageFrom(Core.Name, "You have teleported to " + client.Name);
                        }
                        else
                        {
                            pl.MessageFrom(Core.Name, client.Name + " seems to be offline");
                        }

                        return;
                    }

                    list.Add(client.Name);
                }
            }

            if (list.Count != 0)
            {
                pl.MessageFrom(Core.Name,
                    ((list.Count - 1)).ToString() + " Player" + (((list.Count - 1) > 1) ? "s" : "") + " were found: ");
                for (int j = 1; j < list.Count; j++)
                {
                    pl.MessageFrom(Core.Name, j + " - " + list[j]);
                }

                pl.MessageFrom(Core.Name, "0 - Cancel");
                pl.MessageFrom(Core.Name, "Please enter the number matching the player you were looking for.");
                tpWaitList[pl.UID] = list;
            }
            else
            {
                pl.MessageFrom(Core.Name, "No player found with the name: " + playerName);
            }
        }

        public Hashtable GetTPWaitList()
        {
            return tpWaitList;
        }

        public void PartialNameTP(ref ConsoleSystem.Arg Arguments, int choice)
        {
            var pl = Fougerite.Server.Cache[Arguments.argUser.userID];
            if (tpWaitList.Contains(pl.UID))
            {
                System.Collections.Generic.List<string> list =
                    (System.Collections.Generic.List<string>)tpWaitList[pl.UID];
                string str = list[choice];
                if (choice == 0)
                {
                    pl.MessageFrom(Core.Name, "Cancelled!");
                    tpWaitList.Remove(pl.UID);
                }
                else
                {
                    if (list[0] == "ToTarget")
                    {
                        Arguments.Args = new string[] { pl.Name, str };
                    }
                    else
                    {
                        Arguments.Args = new string[] { str, pl.Name };
                    }

                    teleport.toplayer(ref Arguments);
                    tpWaitList.Remove(pl.UID);
                }
            }
        }
    }
}