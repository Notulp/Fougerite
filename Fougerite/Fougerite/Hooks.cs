using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using Facepunch;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;
using Rust;

namespace Fougerite
{
    using uLink;
    using Fougerite.Events;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Text.RegularExpressions;

    public class Hooks
    {
        public static System.Collections.Generic.List<object> decayList = new System.Collections.Generic.List<object>();
        public static Hashtable talkerTimers = new Hashtable();
        public static bool ServerInitialized = false;

        /// <summary>
        /// This delegate runs when all plugins loaded. (First time)
        /// </summary>
        public static event AllPluginsLoadedDelegate OnAllPluginsLoaded;

        /// <summary>
        /// This delegate runs when a blueprint is being used.
        /// </summary>
        public static event BlueprintUseHandlerDelegate OnBlueprintUse;

        /// <summary>
        /// This delegate runs when a chat message is received.
        /// </summary>
        public static event ChatHandlerDelegate OnChat;

        /// <summary>
        /// This delegate runs when a chat message is received.
        /// </summary>
        public static event ChatRawHandlerDelegate OnChatRaw;

        /// <summary>
        /// This delegate runs when a command is executed.
        /// </summary>
        public static event CommandHandlerDelegate OnCommand;

        /// <summary>
        /// This delegate runs when a command is being executed
        /// </summary>
        public static event CommandRawHandlerDelegate OnCommandRaw;

        /// <summary>
        /// This delegate runs when a console message is received.
        /// </summary>
        [System.Obsolete("Use OnConsoleReceivedWithCancel", false)]
        public static event ConsoleHandlerDelegate OnConsoleReceived;

        /// <summary>
        /// This delegate runs when a console message is received.
        /// </summary>
        public static event ConsoleHandlerWithCancelDelegate OnConsoleReceivedWithCancel;

        /// <summary>
        /// This delegate runs when a door is opened/closed.
        /// </summary>
        public static event DoorOpenHandlerDelegate OnDoorUse;

        /// <summary>
        /// This delegate runs when an entity is attacked by the default rust decay.
        /// </summary>
        public static event EntityDecayDelegate OnEntityDecay;

        [System.Obsolete("Use OnEntityDeployedWithPlacer", false)]
        public static event EntityDeployedDelegate OnEntityDeployed;

        /// <summary>
        /// This delegate runs when an Entity is placed on the ground.
        /// </summary>
        public static event EntityDeployedWithPlacerDelegate OnEntityDeployedWithPlacer;

        /// <summary>
        /// This delegate runs when an entity is damaged.
        /// </summary>
        public static event EntityHurtDelegate OnEntityHurt;

        /// <summary>
        /// This delegate runs when an entity is destroyed.
        /// </summary>
        public static event EntityDestroyedDelegate OnEntityDestroyed;

        /// <summary>
        /// This delegate runs when the item datablocks are loaded.
        /// </summary>
        public static event ItemsDatablocksLoaded OnItemsLoaded;

        /// <summary>
        /// This delegate runs when an AI is hurt.
        /// </summary>
        public static event HurtHandlerDelegate OnNPCHurt;

        /// <summary>
        /// This delegate runs when an AI is killed.
        /// </summary>
        public static event KillHandlerDelegate OnNPCKilled;

        /// <summary>
        /// This delegate runs when a player is connecting to the server.
        /// </summary>
        public static event ConnectionHandlerDelegate OnPlayerConnected;

        /// <summary>
        /// This delegate runs when a player disconnected from the server.
        /// </summary>
        public static event DisconnectionHandlerDelegate OnPlayerDisconnected;

        /// <summary>
        /// This delegate runs when a player is gathering from an animal or from a resource.
        /// </summary>
        public static event PlayerGatheringHandlerDelegate OnPlayerGathering;

        /// <summary>
        /// This delegate runs when a player is hurt.
        /// </summary>
        public static event HurtHandlerDelegate OnPlayerHurt;

        /// <summary>
        /// This delegate runs when a player is killed
        /// </summary>
        public static event KillHandlerDelegate OnPlayerKilled;

        /// <summary>
        /// This delegate runs when a player just spawned.
        /// </summary>
        public static event PlayerSpawnHandlerDelegate OnPlayerSpawned;

        /// <summary>
        /// This delegate runs when a player is about to spawn.
        /// </summary>
        public static event PlayerSpawnHandlerDelegate OnPlayerSpawning;

        /// <summary>
        /// This delegate runs when a plugin is loaded.
        /// </summary>
        public static event PluginInitHandlerDelegate OnPluginInit;

        /// <summary>
        /// This delegate runs when a player is teleported using Fougerite API.
        /// </summary>
        public static event TeleportDelegate OnPlayerTeleport;

        /// <summary>
        /// This delegate runs when the server started loading.
        /// </summary>
        public static event ServerInitDelegate OnServerInit;

        /// <summary>
        /// This delegate runs when the server is stopping.
        /// </summary>
        public static event ServerShutdownDelegate OnServerShutdown;

        /// <summary>
        /// This delegate runs when a player is talking through the microphone.
        /// </summary>
        public static event ShowTalkerDelegate OnShowTalker;

        /// <summary>
        /// This delegate runs when the LootTables are loaded.
        /// </summary>
        public static event LootTablesLoaded OnTablesLoaded;

        /// <summary>
        /// This delegate runs when all C# plugins loaded.
        /// </summary>
        public static event ModulesLoadedDelegate OnModulesLoaded;

        [System.Obsolete("This method is no longer called since the rust api doesn't call It.", false)]
        public static event RecieveNetworkDelegate OnRecieveNetwork;

        /// <summary>
        /// This delegate runs when a player starts crafting.
        /// </summary>
        public static event CraftingDelegate OnCrafting;

        /// <summary>
        /// This delegate runs when a resource object spawned.
        /// </summary>
        public static event ResourceSpawnDelegate OnResourceSpawned;

        /// <summary>
        /// This delegate runs when an item is removed from a specific inventory.
        /// </summary>
        public static event ItemRemovedDelegate OnItemRemoved;

        /// <summary>
        /// This delegate runs when an item is added to a specific inventory.
        /// </summary>
        public static event ItemAddedDelegate OnItemAdded;

        /// <summary>
        /// This delegate runs when an airdrop is called.
        /// </summary>
        public static event AirdropDelegate OnAirdropCalled;

        //public static event AirdropCrateDroppedDelegate OnAirdropCrateDropped;
        /// <summary>
        /// This delegate runs when a player is kicked by steam.
        /// </summary>
        public static event SteamDenyDelegate OnSteamDeny;

        /// <summary>
        /// This delegate runs when a player is being approved.
        /// </summary>
        public static event PlayerApprovalDelegate OnPlayerApproval;

        /// <summary>
        /// This delegate runs when a player is moving. (Even if standing at one place)
        /// </summary>
        public static event PlayerMoveDelegate OnPlayerMove;

        /// <summary>
        /// This delegate runs when a player researched an item.
        /// </summary>
        public static event ResearchDelegate OnResearch;

        /// <summary>
        /// This delegate runs when the server is being saved.
        /// </summary>
        public static event ServerSavedDelegate OnServerSaved;

        /// <summary>
        /// This delegate runs when an item is picked up by a player.
        /// </summary>
        public static event ItemPickupDelegate OnItemPickup;

        /// <summary>
        /// This delegate runs when a player received fall damage.
        /// </summary>
        public static event FallDamageDelegate OnFallDamage;

        /// <summary>
        /// This delegate runs when a player is looting something.
        /// </summary>
        public static event LootEnterDelegate OnLootUse;

        /// <summary>
        /// This delegate runs when a player is shooting a weapon.
        /// </summary>
        public static event ShootEventDelegate OnShoot;

        /// <summary>
        /// This delegate runs when a player is shooting a shotgun.
        /// </summary>
        public static event ShotgunShootEventDelegate OnShotgunShoot;

        /// <summary>
        /// This delegate runs when a player is shooting a bow.
        /// </summary>
        public static event BowShootEventDelegate OnBowShoot;

        /// <summary>
        /// This delegate runs when a player throws a grenade.
        /// </summary>
        public static event GrenadeThrowEventDelegate OnGrenadeThrow;

        /// <summary>
        /// This delegate runs when a player got banned.
        /// </summary>
        public static event BanEventDelegate OnPlayerBan;

        /// <summary>
        /// This delegate runs when a player is using the repair bench.
        /// </summary>
        public static event RepairBenchEventDelegate OnRepairBench;

        /// <summary>
        /// This delegate runs when an item is being moved in an inventory to a different slot / inventory.
        /// </summary>
        public static event ItemMoveEventDelegate OnItemMove;

        /// <summary>
        /// This delegate runs when the ResourceSpawner loaded.
        /// </summary>
        public static event GenericSpawnerLoadDelegate OnGenericSpawnerLoad;

        /// <summary>
        /// This delegate runs when the server finished loading.
        /// </summary>
        public static event ServerLoadedDelegate OnServerLoaded;

        /// <summary>
        /// This delegate runs when a supply signal explodes at a position.
        /// </summary>
        public static event SupplySignalDelegate OnSupplySignalExpode;

        /// <summary>
        /// This delegate runs when a belt slot is used.
        /// </summary>
        public static event BeltUseDelegate OnBeltUse;

        /// <summary>
        /// This delegate runs when the logger functions are triggered.
        /// </summary>
        public static event LoggerDelegate OnLogger;

        /// <summary>
        /// This value returns if the server is shutting down.
        /// </summary>
        public static bool IsShuttingDown { get; set; }

        public static readonly List<ulong> uLinkDCCache = new List<ulong>();

        internal static Dictionary<string, Flood> FloodChecks = new Dictionary<string, Flood>();
        internal static Dictionary<string, DateTime> FloodCooldown = new Dictionary<string, DateTime>();

        public static void AllPluginsLoaded()
        {
            try
            {
                if (OnAllPluginsLoaded != null)
                {
                    OnAllPluginsLoaded();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AllPluginsLoadedEvent Error: " + ex);
            }
        }

        public static void BlueprintUse(IBlueprintItem item, BlueprintDataBlock bdb)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            //Fougerite.Player player = Fougerite.Player.FindByPlayerClient(item.controllable.playerClient);
            Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(item.controllable.playerClient.userID);
            if (player != null)
            {
                BPUseEvent ae = new BPUseEvent(bdb, item);
                if (OnBlueprintUse != null)
                {
                    try
                    {
                        OnBlueprintUse(player, ae);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("BluePrintUseEvent Error: " + ex.ToString());
                    }
                }

                if (!ae.Cancel)
                {
                    PlayerInventory internalInventory = player.Inventory.InternalInventory as PlayerInventory;
                    if (internalInventory != null && internalInventory.BindBlueprint(bdb))
                    {
                        int count = 1;
                        if (item.Consume(ref count))
                        {
                            internalInventory.RemoveItem(item.slot);
                        }

                        player.Notice("", "You can now craft: " + bdb.resultItem.name, 4f);
                    }
                    else
                    {
                        player.Notice("", "You already have this blueprint", 4f);
                    }
                }
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("BluePrintEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void ChatReceived(ref ConsoleSystem.Arg arg)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            if (!chat.enabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(arg.ArgsStr))
            {
                return;
            }

            var quotedName = Facepunch.Utility.String.QuoteSafe(arg.argUser.displayName);
            var quotedMessage = Facepunch.Utility.String.QuoteSafe(arg.GetString(0));
            if (quotedMessage.Trim('"').StartsWith("/"))
            {
                Logger.LogDebug("[CHAT-CMD] " + quotedName + " executed " + quotedMessage);
            }

            if (OnChatRaw != null)
            {
                try
                {
                    OnChatRaw(ref arg);
                }
                catch (Exception ex)
                {
                    Logger.LogError("ChatRawEvent Error: " + ex.ToString());
                }
            }

            if (string.IsNullOrEmpty(arg.ArgsStr))
            {
                return;
            }

            if (quotedMessage.Trim('"').StartsWith("/"))
            {
                string[] args = Facepunch.Utility.String.SplitQuotesStrings(quotedMessage.Trim('"'));
                var command = args[0].TrimStart('/');
                Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(arg.argUser.playerClient.userID);
                if (command == "fougerite")
                {
                    player.Message(
                        "[color #00FFFF]This Server is running Fougerite V[color yellow]" + Bootstrap.Version);
                    player.Message("[color green]Fougerite Team: www.fougerite.com");
                    player.Message("[color #0C86AE]Pluton Team: www.pluton-team.org");
                }

                var cargs = new string[args.Length - 1];
                Array.Copy(args, 1, cargs, 0, cargs.Length);
                if (OnCommand != null)
                {
                    // If player has *, restrict all commands.
                    if (player.CommandCancelList.Contains("*") || player.CommandCancelList.Contains(command))
                    {
                        player.Message("You cannot execute " + command + " at the moment!");
                        return;
                    }

                    try
                    {
                        OnCommand(player, command, cargs);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("CommandEvent Error: " + ex.ToString());
                    }
                }
            }
            else
            {
                Logger.ChatLog(quotedName, quotedMessage);
                var chatstr = new ChatString(quotedMessage);
                try
                {
                    if (OnChat != null)
                    {
                        OnChat(Fougerite.Server.GetServer().FindPlayer(arg.argUser.playerClient.userID), ref chatstr);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("ChatEvent Error: " + ex.ToString());
                }

                if (string.IsNullOrEmpty(chatstr.NewText) || chatstr.NewText.Length == 0)
                {
                    return;
                }

                string newchat = Facepunch.Utility.String
                    .QuoteSafe(chatstr.NewText.Substring(1, chatstr.NewText.Length - 2)).Replace("\\\"", "" + '\u0022');

                if (string.IsNullOrEmpty(newchat) || newchat.Length == 0)
                {
                    return;
                }

                string s = Regex.Replace(newchat, @"\[/?color\b.*?\]", string.Empty);
                if (s.Length <= 100)
                {
                    Fougerite.Data.GetData().chat_history.Add(chatstr);
                    Fougerite.Data.GetData().chat_history_username.Add(quotedName);
                    ConsoleNetworker.Broadcast("chat.add " + quotedName + " " + newchat);
                    return;
                }

                string[] ns = Util.GetUtil().SplitInParts(newchat, 100).ToArray();
                var arr = Regex.Matches(newchat, @"\[/?color\b.*?\]")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .ToArray();
                int i = 0;
                if (arr.Length == 0)
                {
                    arr = new[] { "" };
                }

                foreach (var x in ns)
                {
                    Fougerite.Data.GetData().chat_history.Add(x);
                    Fougerite.Data.GetData().chat_history_username.Add(quotedName);

                    if (i == 1)
                    {
                        ConsoleNetworker.Broadcast("chat.add " + quotedName + " " + '"' + arr[arr.Length - 1] + x);
                    }
                    else
                    {
                        ConsoleNetworker.Broadcast("chat.add " + quotedName + " " + x + '"');
                    }

                    i++;
                }
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("ChatEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static bool HandleRunCommand(ref ConsoleSystem.Arg arg, bool bWantReply = true)
        {
            // Run the plugin handles first.
            try
            {
                // What a crappy way from Garry Newfag to call COMMANDS to initialize classes.
                if (ServerInitialized)
                {
                    bool success = ConsoleReceived(ref arg);
                    if (!success)
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ServerInitialized)
                {
                    Logger.LogError("HandleCommand Error: " + ex);
                }
                // Ignore, should never happen.
            }

            bool flag;
            Type[] typeArray = ConsoleSystem.FindTypes(arg.Class);
            if (typeArray.Length == 0)
            {
                return false;
            }

            if (bWantReply)
            {
                string[] textArray1 = new string[] { "command ", arg.Class, ".", arg.Function, " was executed" };
                arg.ReplyWith(string.Concat(textArray1));
            }

            Type[] typeArray2 = typeArray;
            int index = 0;
            while (true)
            {
                if (index >= typeArray2.Length)
                {
                    if (bWantReply)
                    {
                        arg.ReplyWith("Command not found: " + arg.Class + "." + arg.Function);
                    }

                    return false;
                }

                Type type = typeArray2[index];
                MethodInfo method = type.GetMethod(arg.Function);
                if ((method != null) && method.IsStatic)
                {
                    if (!arg.CheckPermissions(method.GetCustomAttributes(true)))
                    {
                        if (bWantReply)
                        {
                            arg.ReplyWith("No permission: " + arg.Class + "." + arg.Function);
                        }

                        return false;
                    }

                    ConsoleSystem.Arg[] argArray1 = new ConsoleSystem.Arg[] { arg };
                    object[] parameters = argArray1;
                    try
                    {
                        method.Invoke(null, parameters);
                    }
                    catch (Exception exception)
                    {
                        string[] textArray2 = new string[]
                            { "Error: ", arg.Class, ".", arg.Function, " - ", exception.Message };
                        Debug.LogWarning(string.Concat(textArray2));
                        string[] textArray3 = new string[]
                            { "Error: ", arg.Class, ".", arg.Function, " - ", exception.Message };
                        arg.ReplyWith(string.Concat(textArray3));
                        flag = false;
                        break;
                    }

                    arg = parameters[0] as ConsoleSystem.Arg;
                    return true;
                }

                FieldInfo field = type.GetField(arg.Function);
                if ((field != null) && field.IsStatic)
                {
                    if (!arg.CheckPermissions(field.GetCustomAttributes(true)))
                    {
                        if (bWantReply)
                        {
                            arg.ReplyWith("No permission: " + arg.Class + "." + arg.Function);
                        }

                        return false;
                    }

                    Type fieldType = field.FieldType;
                    if (!arg.HasArgs(1))
                    {
                        if (bWantReply)
                        {
                            string[] textArray5 = new string[]
                            {
                                arg.Class, ".", arg.Function, ": ",
                                Facepunch.Utility.String.QuoteSafe(field.GetValue(null).ToString()),
                                " (", fieldType.Name, ")"
                            };
                            arg.ReplyWith(string.Concat(textArray5));
                        }
                    }
                    else
                    {
                        try
                        {
                            string str = field.GetValue(null).ToString();
                            if (ReferenceEquals(fieldType, typeof(float)))
                            {
                                field.SetValue(null, float.Parse(arg.Args[0]));
                            }

                            if (ReferenceEquals(fieldType, typeof(int)))
                            {
                                field.SetValue(null, int.Parse(arg.Args[0]));
                            }

                            if (ReferenceEquals(fieldType, typeof(string)))
                            {
                                field.SetValue(null, arg.Args[0]);
                            }

                            if (ReferenceEquals(fieldType, typeof(bool)))
                            {
                                field.SetValue(null, bool.Parse(arg.Args[0]));
                            }

                            if (bWantReply)
                            {
                                string[] textArray4 = new string[10];
                                textArray4[0] = arg.Class;
                                textArray4[1] = ".";
                                textArray4[2] = arg.Function;
                                textArray4[3] = ": changed ";
                                textArray4[4] = Facepunch.Utility.String.QuoteSafe(str);
                                textArray4[5] = " to ";
                                textArray4[6] = Facepunch.Utility.String.QuoteSafe(field.GetValue(null).ToString());
                                textArray4[7] = " (";
                                textArray4[8] = fieldType.Name;
                                textArray4[9] = ")";
                                arg.ReplyWith(string.Concat(textArray4));
                            }
                        }
                        catch (Exception)
                        {
                            if (bWantReply)
                            {
                                arg.ReplyWith("error setting value: " + arg.Class + "." + arg.Function);
                            }
                        }
                    }

                    return true;
                }

                PropertyInfo property = type.GetProperty(arg.Function);
                if ((property != null) && (property.GetGetMethod().IsStatic && property.GetSetMethod().IsStatic))
                {
                    if (!arg.CheckPermissions(property.GetCustomAttributes(true)))
                    {
                        if (bWantReply)
                        {
                            arg.ReplyWith("No permission: " + arg.Class + "." + arg.Function);
                        }

                        return false;
                    }

                    Type propertyType = property.PropertyType;
                    if (!arg.HasArgs(1))
                    {
                        if (bWantReply)
                        {
                            string[] textArray7 = new string[]
                            {
                                arg.Class, ".", arg.Function, ": ",
                                Facepunch.Utility.String.QuoteSafe(property.GetValue(null, null).ToString()), " (",
                                propertyType.Name, ")"
                            };
                            arg.ReplyWith(string.Concat(textArray7));
                        }
                    }
                    else
                    {
                        try
                        {
                            string str = property.GetValue(null, null).ToString();
                            if (ReferenceEquals(propertyType, typeof(float)))
                            {
                                property.SetValue(null, float.Parse(arg.Args[0]), null);
                            }

                            if (ReferenceEquals(propertyType, typeof(int)))
                            {
                                property.SetValue(null, int.Parse(arg.Args[0]), null);
                            }

                            if (ReferenceEquals(propertyType, typeof(string)))
                            {
                                property.SetValue(null, arg.Args[0], null);
                            }

                            if (ReferenceEquals(propertyType, typeof(bool)))
                            {
                                property.SetValue(null, bool.Parse(arg.Args[0]), null);
                            }

                            if (bWantReply)
                            {
                                string[] textArray6 = new string[10];
                                textArray6[0] = arg.Class;
                                textArray6[1] = ".";
                                textArray6[2] = arg.Function;
                                textArray6[3] = ": changed ";
                                textArray6[4] = Facepunch.Utility.String.QuoteSafe(str);
                                textArray6[5] = " to ";
                                textArray6[6] =
                                    Facepunch.Utility.String.QuoteSafe(property.GetValue(null, null).ToString());
                                textArray6[7] = " (";
                                textArray6[8] = propertyType.Name;
                                textArray6[9] = ")";
                                arg.ReplyWith(string.Concat(textArray6));
                            }
                        }
                        catch (Exception)
                        {
                            if (bWantReply)
                            {
                                arg.ReplyWith("error setting value: " + arg.Class + "." + arg.Function);
                            }
                        }
                    }

                    return true;
                }

                index++;
            }

            return false;
        }


        public static bool ConsoleReceived(ref ConsoleSystem.Arg a)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            StringComparison ic = StringComparison.InvariantCultureIgnoreCase;
            bool external = a.argUser == null;
            bool adminRights = (a.argUser != null && a.argUser.admin) || external;
            string Class = a.Class;
            string Function = a.Function;


            ulong UID = 0;
            if (a.argUser != null)
            {
                UID = a.argUser.userID;
            }

            string userid = "[external][external]";
            if (adminRights && !external)
                userid = string.Format("[{0}][{1}]", a.argUser.displayName, UID.ToString());

            string logmsg = string.Format("[ConsoleReceived] userid={0} adminRights={1} command={2}.{3} args={4}",
                userid, adminRights.ToString(), Class, Function, (a.HasArgs(1) ? a.ArgsStr : "none"));
            Logger.LogDebug(logmsg);

            string clss = Class.ToLower();
            string func = Function.ToLower();
            string data;
            if (!string.IsNullOrEmpty(func))
            {
                data = clss + "." + func;
            }
            else
            {
                data = clss;
            }

            // Allow server console to execute anything
            if (!external && Server.GetServer().ConsoleCommandCancelList.Contains(data))
            {
                a.ReplyWith("This console command is globally restricted!");
                return false;
            }

            // We have a player
            if (UID > 0)
            {
                Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(UID);
                if (player != null && player.ConsoleCommandCancelList.Contains(data))
                {
                    a.ReplyWith("This console command is restricted for you!");
                    player.Message("This console command is restricted for you!");
                    return false;
                }
            }

            if (OnConsoleReceivedWithCancel != null)
            {
                ConsoleEvent ce = new ConsoleEvent();
                try
                {
                    OnConsoleReceivedWithCancel(ref a, external, ce);
                }
                catch (Exception ex)
                {
                    Logger.LogError("OnConsoleReceivedWithCancel Error: " + ex);
                }

                if (ce.Cancelled)
                {
                    return false;
                }
            }

            if (OnConsoleReceived != null)
            {
                try
                {
                    OnConsoleReceived(ref a, external);
                }
                catch (Exception ex)
                {
                    Logger.LogError("ConsoleReceived Error: " + ex);
                }
            }

            if (Class.Equals("fougerite", ic) && Function.Equals("reload", ic))
            {
                if (adminRights)
                {
                    if (a.HasArgs(1))
                    {
                        string plugin = a.ArgsStr;
                        foreach (var x in PluginLoader.GetInstance().Plugins.Keys)
                        {
                            if (string.Equals(x, plugin, StringComparison.CurrentCultureIgnoreCase))
                            {
                                PluginLoader.GetInstance().ReloadPlugin(x);
                                a.ReplyWith("Fougerite: Plugin " + x + " reloaded!");
                                break;
                            }
                        }
                    }
                    else
                    {
                        PluginLoader.GetInstance().ReloadPlugins();
                        a.ReplyWith("Fougerite: Reloaded!");
                    }
                }
            }
            else if (Class.Equals("fougerite", ic) && Function.Equals("unload", ic))
            {
                if (adminRights)
                {
                    if (a.HasArgs(1))
                    {
                        string plugin = a.ArgsStr;
                        foreach (var x in PluginLoader.GetInstance().Plugins.Keys)
                        {
                            if (string.Equals(x, plugin, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (PluginLoader.GetInstance().Plugins[x].State == PluginState.Loaded)
                                {
                                    PluginLoader.GetInstance().UnloadPlugin(x);
                                    a.ReplyWith("Fougerite: UnLoaded " + x + "!");
                                }
                                else
                                {
                                    a.ReplyWith("Fougerite: " + x + " is already unloaded!");
                                }

                                break;
                            }
                        }
                    }
                }
            }
            else if (Class.Equals("fougerite", ic) && Function.Equals("save", ic))
            {
                if (adminRights)
                {
                    DateTime now = DateTime.Now;
                    DateTime then = ServerSaveHandler.NextServerSaveTime;
                    double diff = (then - now).TotalMinutes;
                    if (ServerSaveHandler.CrucialSavePoint != 0 && diff <= ServerSaveHandler.CrucialSavePoint)
                    {
                        a.ReplyWith("Fougerite: " + ServerSaveHandler.CrucialSavePoint +
                                    " minutes before autosave. Please wait for It to finish.");
                    }
                    else
                    {
                        World.GetWorld().ServerSaveHandler.ManualBackGroundSave();
                        a.ReplyWith("Fougerite: Saved!");
                    }
                }
            }
            else if (Class.Equals("fougerite", ic) && Function.Equals("urgentsave", ic))
            {
                if (adminRights)
                {
                    DateTime now = DateTime.Now;
                    DateTime then = ServerSaveHandler.NextServerSaveTime;
                    double diff = (then - now).TotalMinutes;
                    if (ServerSaveHandler.CrucialSavePoint != 0 && diff <= ServerSaveHandler.CrucialSavePoint)
                    {
                        a.ReplyWith("Fougerite: " + ServerSaveHandler.CrucialSavePoint +
                                    " minutes before autosave. Please wait for It to finish.");
                    }
                    else
                    {
                        World.GetWorld().ServerSaveHandler.ManualSave();
                        a.ReplyWith("Fougerite: Saved!");
                    }
                }
            }
            else if (Class.Equals("fougerite", ic) && Function.Equals("rpctracer", ic))
            {
                if (adminRights)
                {
                    Logger.showRPC = !Logger.showRPC;
                    a.ReplyWith("Toggled rpctracer to:" + Logger.showRPC);
                }
            }

            if (string.IsNullOrEmpty(a.Reply))
            {
                a.ReplyWith(string.Format("Fougerite: {0}.{1} was executed!", Class, Function));
            }

            if (sw == null) return true;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("ConsoleEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return true;
        }

        public static bool CheckOwner(DeployableObject obj, Controllable controllable)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            DoorEvent de = new DoorEvent(new Entity(obj));
            if (obj.ownerID == controllable.playerClient.userID)
            {
                de.Open = true;
            }

            if (!(obj is SleepingBag) && OnDoorUse != null)
            {
                try
                {
                    OnDoorUse(Fougerite.Server.GetServer().FindPlayer(controllable.playerClient.userID), de);
                }
                catch (Exception ex)
                {
                    Logger.LogError("DoorUseEvent Error: " + ex.ToString());
                }
            }

            if (sw == null) return de.Open;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("CheckOwnerEvent(DoorOpen) Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return de.Open;
        }

        public static float EntityDecay(object entity, float dmg)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            if (entity == null)
                return 0f;

            try
            {
                DecayEvent de = new DecayEvent(new Entity(entity), ref dmg);
                try
                {
                    if (OnEntityDecay != null)
                        OnEntityDecay(de);
                }
                catch (Exception ex)
                {
                    Logger.LogError("EntityDecayEvent Error: " + ex.ToString());
                }

                if (decayList.Contains(entity))
                    decayList.Remove(entity);

                decayList.Add(entity);
                return de.DamageAmount;
            }
            catch
            {
                // Ignore? Was left here from magma
            }

            if (sw != null)
            {
                sw.Stop();
                if (sw.Elapsed.TotalSeconds > 0)
                    Logger.LogSpeed("EntityDecayEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            }

            return 0f;
        }

        public static void EntityDeployed(object entity, ref uLink.NetworkMessageInfo info)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            Entity e = new Entity(entity);
            uLink.NetworkPlayer nplayer = info.sender;
            Fougerite.Player creator = e.Creator;
            var data = nplayer.GetLocalData();
            Fougerite.Player ActualPlacer = null;
            if (data is NetUser user)
            {
                ActualPlacer = Fougerite.Server.GetServer().FindPlayer(user.userID);
            }


            try
            {
                if (OnEntityDeployed != null)
                    OnEntityDeployed(creator, e);
            }
            catch (Exception ex)
            {
                Logger.LogError("EntityDeployedEvent Error: " + ex.ToString());
            }

            try
            {
                if (OnEntityDeployedWithPlacer != null)
                    OnEntityDeployedWithPlacer(creator, e, ActualPlacer);
            }
            catch (Exception ex)
            {
                Logger.LogError("EntityDeployedWithPlacerEvent Error: " + ex.ToString());
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("EntityDeployedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void EntityHurt2(TakeDamage tkd, ref DamageEvent e)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            HurtEvent he = new HurtEvent(ref e);
            he.DamageAmount = e.amount;
            if (he.VictimIsPlayer)
            {
                Player vp = (Player)he.Victim;
                try
                {
                    if (OnPlayerHurt != null)
                    {
                        OnPlayerHurt(he);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("PlayerHurtEvent Error: " + ex);
                }

                if (vp.Health - he.DamageAmount > 0 && e.status == LifeStatus.WasKilled)
                {
                    e.status = LifeStatus.IsAlive;
                }

                switch (e.status)
                {
                    case LifeStatus.IsAlive:
                        e.amount = he.DamageAmount;
                        tkd._health -= he.DamageAmount;
                        break;
                    case LifeStatus.WasKilled:
                        tkd._health = 0f;
                        break;
                }
            }
            else if (he.VictimIsSleeper)
            {
                Sleeper vp = (Sleeper)he.Victim;
                try
                {
                    if (OnPlayerHurt != null)
                    {
                        OnPlayerHurt(he);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("PlayerHurtEvent (Sleeper) Error: " + ex);
                }

                if (vp.Health - he.DamageAmount > 0 && e.status == LifeStatus.WasKilled)
                {
                    e.status = LifeStatus.IsAlive;
                }

                switch (e.status)
                {
                    case LifeStatus.IsAlive:
                        e.amount = he.DamageAmount;
                        tkd._health -= he.DamageAmount;
                        break;
                    case LifeStatus.WasKilled:
                        tkd._health = 0f;
                        break;
                }
            }
            else if (he.VictimIsNPC)
            {
                if (he.Victim is NPC victim && victim.Health > 0f)
                {
                    try
                    {
                        if (OnNPCHurt != null)
                        {
                            OnNPCHurt(he);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("NPCHurtEvent Error: " + ex.ToString());
                    }

                    switch (e.status)
                    {
                        case LifeStatus.IsAlive:
                        {
                            tkd._health -= he.DamageAmount;
                            break;
                        }
                        case LifeStatus.WasKilled:
                        {
                            DeathEvent de = new DeathEvent(ref e);
                            try
                            {
                                if (OnNPCKilled != null)
                                {
                                    OnNPCKilled(de);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("NPCKilledEvent Error: " + ex);
                            }

                            tkd._health = 0f;
                            break;
                        }
                    }
                }
            }
            else if (he.VictimIsEntity)
            {
                var ent = he.Entity;
                if (decayList.Contains(he.Entity))
                    he.IsDecay = true;

                try
                {
                    if (OnEntityHurt != null)
                    {
                        OnEntityHurt(he);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("EntityHurtEvent Error: " + ex);
                }

                if (ent.IsStructure() && !he.IsDecay)
                {
                    StructureComponent component = ent.Object as StructureComponent;
                    if (component != null &&
                        ((component.IsType(StructureComponent.StructureComponentType.Ceiling) ||
                          component.IsType(StructureComponent.StructureComponentType.Foundation)) ||
                         component.IsType(StructureComponent.StructureComponentType.Pillar)))
                    {
                        he.DamageAmount = 0f;
                    }
                }

                if (!tkd.takenodamage)
                {
                    switch (e.status)
                    {
                        case LifeStatus.IsAlive:
                            if (!ent.IsDestroyed)
                            {
                                tkd._health -= he.DamageAmount;
                            }

                            break;
                        case LifeStatus.WasKilled:
                            DestroyEvent de2 = new DestroyEvent(ref e, ent, he.IsDecay);
                            try
                            {
                                if (OnEntityDestroyed != null)
                                {
                                    OnEntityDestroyed(de2);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("EntityDestroyEvent Error: " + ex);
                            }

                            if (!ent.IsDestroyed)
                            {
                                tkd._health = 0f;
                            }

                            break;
                        case LifeStatus.IsDead:
                            DestroyEvent de22 = new DestroyEvent(ref e, ent, he.IsDecay);
                            try
                            {
                                if (OnEntityDestroyed != null)
                                {
                                    OnEntityDestroyed(de22);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("EntityDestroyEvent Error: " + ex);
                            }

                            if (!ent.IsDestroyed)
                            {
                                tkd._health = 0f;
                                ent.Destroy();
                            }

                            break;
                    }
                }
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("EntityHurt/Destroy Event Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        /*public static void EntityHurt(object entity, ref DamageEvent e)
        {
            if (entity == null)
                return;
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                var ent = new Entity(entity);
                HurtEvent he = new HurtEvent(ref e, ent);
                if (decayList.Contains(entity))
                    he.IsDecay = true;

                if (ent.IsStructure() && !he.IsDecay)
                {
                    StructureComponent component = entity as StructureComponent;
                    if (component != null &&
                        ((component.IsType(StructureComponent.StructureComponentType.Ceiling) ||
                          component.IsType(StructureComponent.StructureComponentType.Foundation)) ||
                         component.IsType(StructureComponent.StructureComponentType.Pillar)))
                    {
                        he.DamageAmount = 0f;
                    }
                }
                TakeDamage takeDamage = ent.GetTakeDamage();
                takeDamage.health += he.DamageAmount;

                // when entity is destroyed
                if (e.status != LifeStatus.IsAlive)
                {
                    DestroyEvent de = new DestroyEvent(ref e, ent, he.IsDecay);
                    if (OnEntityDestroyed != null)
                        OnEntityDestroyed(de);
                }
                else
                {
                    if (OnEntityHurt != null)
                        OnEntityHurt(he);
                }

                //Zone3D zoned = Zone3D.GlobalContains(ent);
                //if ((zoned == null) || !zoned.Protected)
                //{
                if ((he.Entity.GetTakeDamage().health - he.DamageAmount) <= 0f)
                {
                    he.Entity.Destroy();
                }
                else
                {
                    TakeDamage damage2 = ent.GetTakeDamage();
                    damage2.health -= he.DamageAmount;
                }
                //}
                
            }
            catch (Exception ex) { Logger.LogDebug("EntityHurtEvent Error " + ex); }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("EntityHurtEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }*/

        public static void hijack(string name)
        {
            if ((((name != "!Ng") && (name != ":rabbit_prefab_a")) &&
                 ((name != ";res_woodpile") && (name != ";res_ore_1"))) &&
                ((((((((((((((name != ";res_ore_2") & (name != ";res_ore_3")) & (name != ":stag_prefab")) &
                           (name != ":boar_prefab")) & (name != ":chicken_prefab")) & (name != ":bear_prefab")) &
                        (name != ":wolf_prefab")) & (name != ":mutant_bear")) & (name != ":mutant_wolf")) &
                     (name != "AmmoLootBox")) & (name != "MedicalLootBox")) & (name != "BoxLoot")) &
                  (name != "WeaponLootBox")) & (name != "SupplyCrate")))
                Logger.LogDebug("Hijack: " + name);
        }

        public static ItemDataBlock[] ItemsLoaded(System.Collections.Generic.List<ItemDataBlock> items,
            Dictionary<string, int> stringDB, Dictionary<int, int> idDB)
        {
            ItemsBlocks blocks = new ItemsBlocks(items);
            try
            {
                if (OnItemsLoaded != null)
                    OnItemsLoaded(blocks);
            }
            catch (Exception ex)
            {
                Logger.LogError("DataBlockLoadEvent Error: " + ex.ToString());
            }

            int num = 0;
            foreach (ItemDataBlock block in blocks)
            {
                stringDB.Add(block.name, num);
                idDB.Add(block.uniqueID, num);
                num++;
            }

            Fougerite.Server.GetServer().Items = blocks;
            return blocks.ToArray();
        }

        public static bool ItemPickup(ItemPickup pickup, Controllable controllable)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            IInventoryItem item;
            Inventory local = controllable.GetLocal<Inventory>();
            if (local == null)
            {
                return false;
            }

            Inventory inventory2 = pickup.GetLocal<Inventory>();
            if ((inventory2 == null) || object.ReferenceEquals(item = inventory2.firstItem, null))
            {
                pickup.RemoveThis();
                return false;
            }

            ItemPickupEvent ipe = new ItemPickupEvent(controllable, item, local,
                Inventory.AddExistingItemResult.BadItemArgument, PickupEventType.Before);
            try
            {
                if (OnItemPickup != null)
                {
                    OnItemPickup(ipe);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ItemPickupEvent Error: " + ex);
            }

            if (ipe.Cancelled)
            {
                if (sw != null)
                {
                    sw.Stop();
                    if (sw.Elapsed.TotalSeconds > 0)
                        Logger.LogSpeed("ItemPickupEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
                }

                return false;
            }

            Inventory.AddExistingItemResult result = local.AddExistingItem(item, false);
            ItemPickupEvent aftercall = new ItemPickupEvent(controllable, item, local, result, PickupEventType.After);
            switch (result)
            {
                case Inventory.AddExistingItemResult.CompletlyStacked:
                    inventory2.RemoveItem(item);
                    break;

                case Inventory.AddExistingItemResult.Moved:
                    break;

                case Inventory.AddExistingItemResult.PartiallyStacked:
                    pickup.UpdateItemInfo(item);
                    try
                    {
                        if (OnItemPickup != null)
                        {
                            OnItemPickup(aftercall);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("ItemPickupEvent Error: " + ex);
                    }

                    return true;

                case Inventory.AddExistingItemResult.Failed:
                    try
                    {
                        if (OnItemPickup != null)
                        {
                            OnItemPickup(aftercall);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("ItemPickupEvent Error: " + ex);
                    }

                    return false;

                case Inventory.AddExistingItemResult.BadItemArgument:
                    pickup.RemoveThis();
                    try
                    {
                        if (OnItemPickup != null)
                        {
                            OnItemPickup(aftercall);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("ItemPickupEvent Error: " + ex);
                    }

                    return false;

                default:
                    throw new NotImplementedException();
            }

            if (sw != null)
            {
                sw.Stop();
                if (sw.Elapsed.TotalSeconds > 0)
                    Logger.LogSpeed("ItemPickupEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            }

            pickup.RemoveThis();
            try
            {
                if (OnItemPickup != null)
                {
                    OnItemPickup(aftercall);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ItemPickupEvent Error: " + ex);
            }

            return true;
        }

        public static void FallDamage(FallDamage fd, float speed, float num, bool flag, bool flag2)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            FallDamageEvent fde = new FallDamageEvent(fd, speed, num, flag, flag2);
            try
            {
                if (OnFallDamage != null)
                {
                    OnFallDamage(fde);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("FallDamageEvent Error: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("FallDamageEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void ConnectHandler(NetUser user)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            GameEvent.DoPlayerConnected(user.playerClient);
            PlayerConnect(user);
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("ConnectHandler Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static bool PlayerConnect(NetUser user)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            bool connected = false;

            if (user.playerClient == null)
            {
                Logger.LogDebug("PlayerConnect user.playerClient is null");
                return connected;
            }

            ulong uid = user.userID;
            string nip = user.networkPlayer.externalIP.ToString();
            string nname = user.displayName.ToString();
            if (uLinkDCCache.Contains(uid))
            {
                uLinkDCCache.Remove(uid);
            }

            if (FloodCooldown.ContainsKey(nip))
            {
                Fougerite.Server.GetServer().BanPlayerIP(nip, nname, "FloodCooldown", "Fougerite");
                return connected;
            }

            Fougerite.Server srv = Fougerite.Server.GetServer();
            Fougerite.Player player = new Fougerite.Player(user.playerClient);
            if (!Fougerite.Server.Cache.ContainsKey(uid))
            {
                Fougerite.Server.Cache.Add(uid, player);
            }
            else
            {
                Fougerite.Server.Cache[uid] = player;
            }

            if (srv.ContainsPlayer(uid))
            {
                Logger.LogError(string.Format("[PlayerConnect] Server.Players already contains {0} {1}", player.Name,
                    player.SteamID));
                connected = user.connected;
                return connected;
            }

            srv.AddPlayer(uid, player);
            //server.Players.Add(player);
            //Rust.Steam.Server.Steam_UpdateServer(server.maxplayers, srv.Players.Count, server.hostname, server.map, "rust,modded,fougerite");

            try
            {
                if (OnPlayerConnected != null)
                {
                    OnPlayerConnected(player);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerConnectedEvent Error " + ex.ToString());
                return connected;
            }

            connected = user.connected;

            if (Fougerite.Config.GetBoolValue("Fougerite", "tellversion"))
            {
                player.Message(string.Format("This server is powered by Fougerite v.{0}!", Bootstrap.Version));
            }

            Logger.LogDebug("User Connected: " + player.Name + " (" + player.SteamID + ")" + " (" + player.IP + ")");

            if (!FloodChecks.ContainsKey(player.IP))
            {
                // Create the flood class.
                Flood f = new Flood(player.IP);
                FloodChecks[player.IP] = f;
            }
            else
            {
                var data = FloodChecks[player.IP];
                if (data.Amount < Bootstrap.FloodConnections) // Allow n connections from the same IP / 3 secs.
                {
                    data.Increase();
                    data.Reset();
                }
                else
                {
                    data.Stop();
                    if (FloodChecks.ContainsKey(player.IP))
                    {
                        FloodChecks.Remove(player.IP);
                    }

                    FloodCooldown[player.IP] = DateTime.Now;
                }
            }

            if (sw == null) return connected;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("PlayerConnectedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return connected;
        }

        public static void PlayerDisconnect(uLink.NetworkPlayer nplayer)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            NetUser user = nplayer.GetLocalData() as NetUser;
            if (user == null)
            {
                return;
            }

            ulong uid = user.userID;
            Fougerite.Player player = null;
            if (Fougerite.Server.Cache.ContainsKey(uid))
            {
                player = Fougerite.Server.Cache[uid];
            }
            else
            {
                Fougerite.Server.GetServer().RemovePlayer(uid);
                Logger.LogWarning(
                    "[WeirdDisconnect] Player was null at the disconnection. Something might be wrong? OPT: " +
                    Fougerite.Bootstrap.CR);
                return;
            }

            player.DisconnectTime = DateTime.UtcNow.Ticks;
            player.IsDisconnecting = true;
            Fougerite.Server.GetServer().RemovePlayer(uid);
            //if (Fougerite.Server.GetServer().Players.Contains(player)) { Fougerite.Server.GetServer().Players.Remove(player); }
            //player.PlayerClient.netUser.Dispose();
            Fougerite.Server.Cache[uid] = player;
            //Rust.Steam.Server.Steam_UpdateServer(server.maxplayers, Fougerite.Server.GetServer().Players.Count, server.hostname, server.map, "modded, fougerite");
            try
            {
                if (OnPlayerDisconnected != null)
                {
                    OnPlayerDisconnected(player);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerDisconnectedEvent Error " + ex.ToString());
            }

            Logger.LogDebug("User Disconnected: " + player.Name + " (" + player.SteamID + ")" + " (" + player.IP + ")");
            if (Fougerite.Bootstrap.CR && Fougerite.Server.Cache.ContainsKey(uid))
            {
                Fougerite.Server.Cache.Remove(uid);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("PlayerDisconnectEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerGather(Inventory rec, ResourceTarget rt, ResourceGivePair rg, ref int amount)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, rg, amount);
            try
            {
                if (OnPlayerGathering != null)
                {
                    OnPlayerGathering(player, ge);
                }

                amount = ge.Quantity;
                if (!ge.Override)
                {
                    amount = Mathf.Min(amount, rg.AmountLeft());
                }

                rg.ResourceItemName = ge.Item;
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerGatherEvent Error: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("GatherEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerGatherWood(IMeleeWeaponItem rec, ResourceTarget rt, ref ItemDataBlock db,
            ref int amount, ref string name)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.inventory.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, db, amount);
            ge.Item = "Wood";
            try
            {
                if (OnPlayerGathering != null)
                {
                    OnPlayerGathering(player, ge);
                }

                db = Fougerite.Server.GetServer().Items.Find(ge.Item);
                amount = ge.Quantity;
                name = ge.Item;
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerGatherWoodEvent Error: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("WoodGatherEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static bool PlayerKilled(ref DamageEvent de)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            bool flag = false;
            try
            {
                DeathEvent event2 = new DeathEvent(ref de);
                flag = event2.DropItems;
                if (OnPlayerKilled != null)
                    OnPlayerKilled(event2);

                flag = event2.DropItems;
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerKilledEvent Error: " + ex);
            }

            if (sw == null) return flag;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("PlayerKilledEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return flag;
        }

        public static void PlayerSpawned(PlayerClient pc, Vector3 pos, bool camp)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(pc.userID);
            SpawnEvent se = new SpawnEvent(pos, camp);
            try
            {
                if (OnPlayerSpawned != null && player != null)
                {
                    OnPlayerSpawned(player, se);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerSpawnedEvent Error: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("PlayerSpawned Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static Vector3 PlayerSpawning(PlayerClient pc, Vector3 pos, bool camp)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(pc.userID);
            SpawnEvent se = new SpawnEvent(pos, camp);
            try
            {
                if (OnPlayerSpawning != null && player != null)
                {
                    OnPlayerSpawning(player, se);
                }

                return new Vector3(se.X, se.Y, se.Z);
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerSpawningEvent Error: " + ex);
            }

            if (sw == null) return pos;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("PlayerSpawningEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return pos;
        }

        public static void PluginInit()
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                if (OnPluginInit != null)
                {
                    OnPluginInit();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PluginInitEvent Error: " + ex.ToString());
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("PluginInit Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerTeleport(Fougerite.Player player, Vector3 from, Vector3 dest)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                if (OnPlayerTeleport != null)
                {
                    OnPlayerTeleport(player, from, dest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("TeleportEvent Error: " + ex.ToString());
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("TeleportEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        private static DateTime LasTime = DateTime.Now;

        public static void RecieveNetwork(Metabolism m, float cal, float water, float rad, float anti, float temp,
            float poison)
        {
            DateTime now = DateTime.Now;
            DateTime then = LasTime;
            double diff = (now - then).TotalMinutes;
            if (diff > 5)
            {
                LasTime = DateTime.Now;
                Logger.LogWarning("[RecieveNetwork] A metabolism hack was prevented.");
            }
        }

        public static void CraftingEvent(CraftingInventory inv, BlueprintDataBlock blueprint, int amount,
            ulong startTime)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                CraftingEvent e = new CraftingEvent(inv, blueprint, amount, startTime);
                if (OnCrafting != null)
                {
                    OnCrafting(e);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("CraftingEvent Error: " + ex.ToString());
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("CraftEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void AnimalMovement(BaseAIMovement m, BasicWildLifeAI ai, ulong simMillis)
        {
            var movement = m as NavMeshMovement;
            if (movement == null || !movement)
            {
                return;
            }

            if (movement._agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                TakeDamage dmg = ai.GetComponent<TakeDamage>();
                bool IsAlive = dmg != null && ai.GetComponent<TakeDamage>().alive;
                if (IsAlive)
                {
                    TakeDamage.KillSelf(ai.GetComponent<IDBase>());
                    Logger.LogWarning("[NavMesh] AI destroyed for having invalid path.");
                }
            }
        }

        public static void ResourceSpawned(ResourceTarget target)
        {
            try
            {
                if (OnResourceSpawned != null)
                {
                    OnResourceSpawned(target);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ResourceSpawnedEvent Error: " + ex);
            }
        }

        public static void BowShootEvent(BowWeaponDataBlock db, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info, IBowWeaponItem bwi)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                if (OnBowShoot != null)
                {
                    BowShootEvent se = new BowShootEvent(db, rep, info, bwi);
                    OnBowShoot(se);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("BowShootEvent Error: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("BowShootEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        
        private static void GrenadeEvent(HandGrenadeDataBlock hgd, uLink.BitStream stream, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            IHandGrenadeItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<IHandGrenadeItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
            {
                rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                Vector3 origin = stream.ReadVector3();
                Vector3 forward = stream.ReadVector3();
                GameObject obj2 = hgd.ThrowItem(rep, origin, forward);
                if (obj2 != null)
                {
                    obj2.rigidbody.AddTorque((Vector3)(new Vector3(UnityEngine.Random.Range((float)-1f, (float)1f),
                        UnityEngine.Random.Range((float)-1f, (float)1f),
                        UnityEngine.Random.Range((float)-1f, (float)1f)) * 10f));
                    try
                    {
                        if (OnGrenadeThrow != null)
                        {
                            GrenadeThrowEvent se = new GrenadeThrowEvent(hgd, obj2, rep, info, item);
                            OnGrenadeThrow(se);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("GrenadeThrowEvent Error: " + ex);
                    }
                }

                int count = 1;
                if (item.Consume(ref count))
                {
                    item.inventory.RemoveItem(item.slot);
                }
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("GrenadeEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void OnServerSaveEvent(int amount, double seconds)
        {
            try
            {
                if (Hooks.OnServerSaved != null)
                {
                    Hooks.OnServerSaved(amount, seconds);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ServerSavedEvent Error: " + ex);
            }

            // Save the permissions.
            PermissionSystem.GetPermissionSystem().SaveToDisk();
        }

        public static void GlobalQuit()
        {
            Logger.Log("Detecting quit. Saving...");
            ConsoleSystem.Run("server.close", false);
            //ConsoleSystem.Run("save.all", false);
            global.Console_AllowClose();
            ServerShutdown();
            //Application.Quit();
            LibRust.Shutdown();
            Process.GetCurrentProcess().Kill();
        }

        public static bool ItemRemoved(Inventory inv, int slot, InventoryItem match, bool mustMatch)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            Collection<InventoryItem> collection = inv.collection;
            InventoryItem inventoryItem;
            if (mustMatch && (!collection.Get(slot, out inventoryItem) ||
                              !object.ReferenceEquals((object)inventoryItem, (object)match)) ||
                !collection.Evict(slot, out inventoryItem))
            {
                return false;
            }

            Fougerite.Events.InventoryModEvent e = null;
            try
            {
                e = new Fougerite.Events.InventoryModEvent(inv, slot, inventoryItem.iface, "Remove");
                if (OnItemRemoved != null)
                {
                    OnItemRemoved(e);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("InventoryRemoveEvent Error: " + ex);
            }

            if (e != null && e.Cancelled)
            {
                if (sw != null)
                {
                    sw.Stop();
                    if (sw.Elapsed.TotalSeconds > 0)
                        Logger.LogSpeed("ItemRemoved Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
                }

                return false;
            }

            if (inventoryItem == inv._activeItem)
            {
                inv.DeactivateItem();
            }

            inv.ItemRemoved(slot, inventoryItem.iface);
            inv.MarkSlotDirty(slot);
            if (sw != null)
            {
                sw.Stop();
                if (sw.Elapsed.TotalSeconds > 0)
                    Logger.LogSpeed("ItemRemoved Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            }

            return true;
        }

        public static bool ItemAdded(ref Inventory.Payload.Assignment args)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            Fougerite.Events.InventoryModEvent e = null;
            try
            {
                e = new Fougerite.Events.InventoryModEvent(args.inventory, args.slot, args.item.iface, "Add");
                if (OnItemAdded != null)
                {
                    OnItemAdded(e);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("InventoryAddEvent Error: " + ex);
            }

            if (e == null || (e != null && !e.Cancelled))
            {
                if (args.inventory.CheckSlotFlagsAgainstSlot(args.datablock._itemFlags, args.slot) &&
                    args.item.CanMoveToSlot(args.inventory, args.slot))
                {
                    ++args.attemptsMade;
                    if (args.collection.Occupy(args.slot, args.item))
                    {
                        if (!args.fresh && (bool)((UnityEngine.Object)args.item.inventory))
                            args.item.inventory.RemoveItem(args.item.slot);
                        args.item.SetUses(args.uses);
                        args.item.OnAddedTo(args.inventory, args.slot);
                        args.inventory.ItemAdded(args.slot, args.item.iface);
                        if (sw != null)
                        {
                            sw.Stop();
                            if (sw.Elapsed.TotalSeconds > 0)
                                Logger.LogSpeed("ItemAdded Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
                        }

                        return true;
                    }
                }
            }

            if (sw != null)
            {
                sw.Stop();
                if (sw.Elapsed.TotalSeconds > 0)
                    Logger.LogSpeed("ItemAdded Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            }

            return false;
        }

        public static void Airdrop(Vector3 v)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                if (OnAirdropCalled != null)
                {
                    OnAirdropCalled(v);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AirdropEvent Error: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("Airdrop Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void Airdrop2(SupplyDropZone srz)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                if (OnAirdropCalled != null)
                {
                    OnAirdropCalled(srz.GetSupplyTargetPosition());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AirdropEvent Error: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("Airdrop2 Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        /*public static void AirdropCrateDropped(GameObject go)
        {
            try
            {
                if (OnAirdropCrateDropped != null)
                {
                    OnAirdropCrateDropped(go);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AirdropCrateDroppedEvent Error: " + ex);
            }
        }*/

        public static void SteamDeny(ClientConnection cc, NetworkPlayerApproval approval, string strReason,
            NetError errornum)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            SteamDenyEvent sde = new SteamDenyEvent(cc, approval, strReason, errornum);
            try
            {
                if (OnSteamDeny != null)
                {
                    OnSteamDeny(sde);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("SteamDenyEvent Error: " + ex);
            }

            if (sde.ForceAllow)
            {
                if (sw != null)
                {
                    sw.Stop();
                    if (sw.Elapsed.TotalSeconds > 0)
                        Logger.LogSpeed("Airdrop Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
                }

                return;
            }

            string deny = "Auth failed: " + strReason + " - " + cc.UserName + " (" +
                          cc.UserID.ToString() +
                          ")";
            ConsoleSystem.Print(deny, false);
            approval.Deny((uLink.NetworkConnectionError)errornum);
            ConnectionAcceptor.CloseConnection(cc);
            Rust.Steam.Server.OnUserLeave(cc.UserID);
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("SteamDeny Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void HandleuLinkDisconnect(string msg, object NetworkPlayer)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                UnityEngine.Object[] obj = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
                GameObject[] objArray = null;
                if (obj is GameObject[])
                {
                    objArray = obj as GameObject[];
                }
                else
                {
                    Logger.LogWarning("[uLink Failure] Array was not GameObject?!");
                }

                if (objArray == null)
                {
                    Logger.LogWarning(
                        "[uLink Failure] Something bad happened during the disconnection... Report this.");
                    return;
                }

                if (NetworkPlayer is uLink.NetworkPlayer)
                {
                    uLink.NetworkPlayer np = (uLink.NetworkPlayer)NetworkPlayer;
                    var data = np.GetLocalData();
                    NetUser user = data as NetUser;
                    if (user != null)
                    {
                        ulong id = user.userID;
                        var client = user.playerClient;
                        var loc = user.playerClient.lastKnownPosition;
                        Fougerite.Server.Cache[id].IsDisconnecting = true;
                        Fougerite.Server.Cache[id].DisconnectLocation = loc;
                        Fougerite.Server.Cache[id].UpdatePlayerClient(client);
                        var srv = Fougerite.Server.GetServer();
                        if (srv.DPlayers.ContainsKey(id))
                        {
                            srv.DPlayers[id].IsDisconnecting = true;
                            srv.DPlayers[id].DisconnectLocation = loc;
                            srv.DPlayers[id].UpdatePlayerClient(client);
                        }
                    }
                }

                foreach (GameObject obj2 in objArray)
                {
                    try
                    {
                        if (obj2 != null)
                        {
                            obj2.SendMessage(msg, NetworkPlayer, SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("[uLink Error] Disconnect failure, report to DreTaX: " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("[uLink Error] Full Exception: " + ex);
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("HandleuLinkDisconnect Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerApproval(ConnectionAcceptor ca, NetworkPlayerApproval approval)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            if (ca.m_Connections.Count >= server.maxplayers)
            {
                approval.Deny(uLink.NetworkConnectionError.TooManyConnectedPlayers);
            }
            else
            {
                ClientConnection clientConnection = new ClientConnection();
                if (!clientConnection.ReadConnectionData(approval.loginData))
                {
                    approval.Deny(uLink.NetworkConnectionError.IncorrectParameters);
                    return;
                }

                Fougerite.Server srv = Fougerite.Server.GetServer();
                ulong uid = clientConnection.UserID;
                string ip = approval.ipAddress;
                string name = clientConnection.UserName;

                if (FloodCooldown.ContainsKey(ip))
                {
                    DateTime now = DateTime.Now;
                    DateTime then = FloodCooldown[ip];
                    double diff = (now - then).TotalMinutes;
                    if (diff >= 15)
                    {
                        Logger.LogWarning("[Flood Protection] " + ip + " was removed from the cooldown.");
                        FloodCooldown.Remove(ip);
                    }
                }

                if (clientConnection.Protocol != 1069)
                {
                    Debug.Log((object)("Denying entry to client with invalid protocol version (" + ip + ")"));
                    approval.Deny(uLink.NetworkConnectionError.IncompatibleVersions);
                }
                else if (BanList.Contains(uid))
                {
                    Debug.Log((object)("Rejecting client (" + uid.ToString() + "in banlist)"));
                    approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                }
                else if (srv.IsBannedID(uid.ToString()) || srv.IsBannedIP(ip))
                {
                    if (!srv.IsBannedIP(ip))
                    {
                        srv.BanPlayerIP(ip, name, "IP is not banned-" + uid.ToString(), "Console");
                        Logger.LogDebug("[FougeriteBan] Detected banned ID, but IP is not banned: "
                                        + name + " - " + ip + " - " + uid);
                    }
                    else
                    {
                        if (DataStore.GetInstance().Get("Ips", ip).ToString() != name)
                        {
                            DataStore.GetInstance().Add("Ips", ip, name);
                        }
                    }

                    if (!srv.IsBannedID(uid.ToString()))
                    {
                        srv.BanPlayerID(uid.ToString(), name, "ID is not banned-" + ip, "Console");
                        Logger.LogDebug("[FougeriteBan] Detected banned IP, but ID is not banned: "
                                        + name + " - " + ip + " - " + uid);
                    }
                    else
                    {
                        if (DataStore.GetInstance().Get("Ids", uid.ToString()).ToString() != name)
                        {
                            DataStore.GetInstance().Add("Ids", uid.ToString(), name);
                        }
                    }

                    Logger.LogWarning("[FougeriteBan] Disconnected: " + name
                                                                      + " - " + ip + " - " + uid);
                    approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                }
                else if (ca.IsConnected(uid))
                {
                    PlayerApprovalEvent ape =
                        new PlayerApprovalEvent(ca, approval, clientConnection, true, uid, ip, name);
                    try
                    {
                        if (OnPlayerApproval != null)
                        {
                            OnPlayerApproval(ape);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("PlayerApprovalEvent Error: " + ex);
                    }

                    if (ape.ForceAccept)
                    {
                        if (Fougerite.Server.Cache.ContainsKey(uid) && !ape.ServerHasPlayer)
                        {
                            Fougerite.Server.Cache[uid].Disconnect();
                        }

                        Accept(ca, approval, clientConnection);
                        return;
                    }

                    Debug.Log((object)("Denying entry to " + uid.ToString() + " because they're already connected"));
                    approval.Deny(uLink.NetworkConnectionError.AlreadyConnectedToAnotherServer);
                }
                else if (FloodCooldown.ContainsKey(ip))
                {
                    approval.Deny(uLink.NetworkConnectionError.CreateSocketOrThreadFailure);
                }
                else
                {
                    PlayerApprovalEvent ape =
                        new PlayerApprovalEvent(ca, approval, clientConnection, false, uid, ip, name);
                    try
                    {
                        if (OnPlayerApproval != null)
                        {
                            OnPlayerApproval(ape);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("PlayerApprovalEvent2 Error: " + ex);
                    }

                    Accept(ca, approval, clientConnection);
                }
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("PlayerApprovalEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        private static void Accept(ConnectionAcceptor ca, NetworkPlayerApproval approval,
            ClientConnection clientConnection)
        {
            ca.m_Connections.Add(clientConnection);
            ca.StartCoroutine(clientConnection.AuthorisationRoutine(approval));
            approval.Wait();
        }

        public static bool ProcessGetClientMove(HumanController hc, uLink.NetworkMessageInfo info)
        {
            if (info.sender != hc.networkView.owner)
            {
                return false;
            }

            return true;
        }

        public static void ClientMove(HumanController hc, Vector3 origin, int encoded, ushort stateFlags,
            uLink.NetworkMessageInfo info)
        {
            if (info.sender != hc.networkView.owner)
            {
                return;
            }

            if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) ||
                float.IsNaN(origin.y) || float.IsInfinity(origin.y) ||
                float.IsNaN(origin.z) || float.IsInfinity(origin.z))
            {
                Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                if (player == null)
                {
                    // Should never happen but just to be sure.
                    if (hc.netUser == null) return;
                    if (hc.netUser.connected)
                    {
                        hc.netUser.Kick(NetError.NoError, true);
                    }
                }
                else
                {
                    Logger.LogWarning("[TeleportHack] " + player.Name + " sent invalid packets. " + player.SteamID);
                    Server.GetServer().Broadcast(player.Name + " might have tried to teleport with hacks.");
                    if (Fougerite.Bootstrap.BI)
                    {
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "TeleportHack");
                        return;
                    }

                    player.Disconnect();
                }

                return;
            }

            var data = stateFlags = (ushort)(stateFlags & -24577);
            Util.PlayerActions action = ((Util.PlayerActions)data);
            try
            {
                if (OnPlayerMove != null)
                {
                    OnPlayerMove(hc, origin, encoded, stateFlags, info, action);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerMoveEvent Error: " + ex);
            }
        }

        public static InventoryItem.MergeResult ResearchItem(ResearchToolItem<ToolDataBlock> rti,
            IInventoryItem otherItem)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            BlueprintDataBlock block2;
            PlayerInventory inventory = rti.inventory as PlayerInventory;
            if ((inventory == null) || (otherItem.inventory != inventory))
            {
                return InventoryItem.MergeResult.Failed;
            }

            ItemDataBlock datablock = otherItem.datablock;
            if ((datablock == null) || !datablock.isResearchable)
            {
                return InventoryItem.MergeResult.Failed;
            }

            if (!inventory.AtWorkBench())
            {
                return InventoryItem.MergeResult.Failed;
            }

            if (!BlueprintDataBlock.FindBlueprintForItem<BlueprintDataBlock>(otherItem.datablock, out block2))
            {
                return InventoryItem.MergeResult.Failed;
            }

            if (inventory.KnowsBP(block2))
            {
                return InventoryItem.MergeResult.Failed;
            }

            ResearchEvent researchEvent = new ResearchEvent(otherItem);
            ;
            try
            {
                if (OnResearch != null)
                {
                    OnResearch(researchEvent);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ResearchItem Error: " + ex.ToString());
            }

            if (!researchEvent.Cancelled)
            {
                inventory.BindBlueprint(block2);
                Notice.Popup(inventory.networkView.owner, "?", "You can now craft " + otherItem.datablock.name, 4f);
                int numWant = 1;
                if (rti.Consume(ref numWant))
                {
                    rti.inventory.RemoveItem(rti.slot);
                }
            }

            if (sw != null)
            {
                sw.Stop();
                if (sw.Elapsed.TotalSeconds > 0)
                    Logger.LogSpeed("ResearchItem Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            }

            return !researchEvent.Cancelled ? InventoryItem.MergeResult.Combined : InventoryItem.MergeResult.Failed;
        }

        public static void SetLooter(LootableObject lo, uLink.NetworkPlayer ply)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            lo.occupierText = null;
            if (ply == uLink.NetworkPlayer.unassigned)
            {
                lo.ClearLooter();
            }
            else
            {
                if (ply == NetCull.player)
                {
                    if (!lo.thisClientIsInWindow)
                    {
                        try
                        {
                            lo._currentlyUsingPlayer = ply;
                            RPOS.OpenLootWindow(lo);
                            lo.thisClientIsInWindow = true;
                        }
                        catch (Exception exception)
                        {
                            Logger.LogError("[SetLooter] Error: " + exception);
                            NetCull.RPC((UnityEngine.MonoBehaviour)lo, "StopLooting", uLink.RPCMode.Server);
                            lo.thisClientIsInWindow = false;
                            ply = uLink.NetworkPlayer.unassigned;
                        }
                    }
                }
                else if ((lo._currentlyUsingPlayer == NetCull.player) &&
                         (NetCull.player != uLink.NetworkPlayer.unassigned))
                {
                    lo.ClearLooter();
                }

                lo._currentlyUsingPlayer = ply;
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("SetLooterEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void OnUseEnter(LootableObject lo, Useable use)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            var ulinkuser = uLink.NetworkView.Get((UnityEngine.MonoBehaviour)use.user).owner;
            lo._useable = use;
            lo._currentlyUsingPlayer = ulinkuser;
            lo._inventory.AddNetListener(lo._currentlyUsingPlayer);
            lo.SendCurrentLooter();
            lo.CancelInvokes();
            lo.InvokeRepeating("RadialCheck", 0f, 10f);

            if (sw != null)
            {
                sw.Stop();
                if (sw.Elapsed.TotalSeconds > 0)
                    Logger.LogSpeed("ChestEnterEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            }
        }

        public static UseResponse EnterHandler(Useable use, Character attempt, UseEnterRequest request)
        {
            if (!use.canUse)
            {
                return UseResponse.Fail_NotIUseable;
            }

            Useable.EnsureServer();
            if (((int)use.callState) != 0)
            {
                Logger.LogWarning(
                    "Some how Enter got called from a call stack originating with " + use.callState +
                    " fix your script to not do this.", use);
                return UseResponse.Fail_InvalidOperation;
            }

            if (Useable.hasException)
            {
                Useable.ClearException(false);
            }

            if (attempt == null)
            {
                return UseResponse.Fail_NullOrMissingUser;
            }

            if (attempt.signaledDeath)
            {
                return UseResponse.Fail_UserDead;
            }

            LootableObject lootableObject = use.GetComponent<LootableObject>();

            if (use._user == null)
            {
                if (use.implementation != null)
                {
                    try
                    {
                        UseResponse response;
                        use.callState = FunctionCallState.Enter;
                        if (use.canCheck)
                        {
                            try
                            {
                                response = (UseResponse)use.useCheck.CanUse(attempt, request);
                            }
                            catch (Exception exception)
                            {
                                Useable.lastException = exception;
                                return UseResponse.Fail_CheckException;
                            }

                            if (((int)response) != 1)
                            {
                                if (response.Succeeded())
                                {
                                    Logger.LogError(
                                        "A IUseableChecked return a invalid value that should have cause success [" +
                                        response + "], but it was not UseCheck.Success! fix your script.",
                                        use.implementation);
                                    return UseResponse.Fail_Checked_BadResult;
                                }

                                if (use.wantDeclines)
                                {
                                    try
                                    {
                                        use.useDecline.OnUseDeclined(attempt, response, request);
                                    }
                                    catch (Exception exception2)
                                    {
                                        Logger.LogError(
                                            string.Concat(new object[]
                                            {
                                                "Caught exception in OnUseDeclined \r\n (response was ", response, ")",
                                                exception2
                                            }), use.implementation);
                                    }
                                }

                                return response;
                            }
                        }
                        else
                        {
                            response = UseResponse.Pass_Unchecked;
                        }

                        try
                        {
                            use._user = attempt;
                            try
                            {
                                var ulinkuser = uLink.NetworkView.Get((UnityEngine.MonoBehaviour)use.user).owner;
                                NetUser user = ulinkuser.GetLocalData() as NetUser;
                                LootStartEvent lt = null;
                                if (user != null)
                                {
                                    Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(user.userID);
                                    if (pl != null)
                                    {
                                        lt = new LootStartEvent(lootableObject, pl, use, ulinkuser);
                                        try
                                        {
                                            if (OnLootUse != null)
                                            {
                                                OnLootUse(lt);
                                            }
                                        }
                                        catch (Exception ex2)
                                        {
                                            Logger.LogError("LootStartEvent Error: " + ex2);
                                        }

                                        if (lt.IsCancelled)
                                        {
                                            use._user = null;
                                            return UseResponse.Pass_Unchecked;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex3)
                            {
                                Logger.LogError("LootStartEvent Outer Error: " + ex3);
                            }

                            OnUseEnter(lootableObject, use);
                            //use.use.OnUseEnter(use);
                        }
                        catch (Exception exception3)
                        {
                            use._user = null;
                            Logger.LogError(
                                "Exception thrown during Useable.Enter. Object not set as used!\r\n" + exception3,
                                attempt);
                            Useable.lastException = exception3;
                            return UseResponse.Fail_EnterException;
                        }

                        if (response.Succeeded())
                        {
                            use.LatchUse();
                        }

                        return response;
                    }
                    finally
                    {
                        use.callState = FunctionCallState.None;
                    }
                }

                return UseResponse.Fail_Destroyed;
            }

            if (use._user == attempt)
            {
                if (use.wantDeclines && (use.implementation != null))
                {
                    try
                    {
                        use.useDecline.OnUseDeclined(attempt, UseResponse.Fail_Redundant, request);
                    }
                    catch (Exception exception4)
                    {
                        Logger.LogError(
                            "Caught exception in OnUseDeclined \r\n (response was Fail_Redundant)" + exception4,
                            use.implementation);
                    }
                }

                return UseResponse.Fail_Redundant;
            }

            if (use.wantDeclines && (use.implementation != null))
            {
                try
                {
                    use.useDecline.OnUseDeclined(attempt, UseResponse.Fail_Vacancy, request);
                }
                catch (Exception exception5)
                {
                    Logger.LogError("Caught exception in OnUseDeclined \r\n (response was Fail_Vacancy)" + exception5,
                        use.implementation);
                }
            }

            return UseResponse.Fail_Vacancy;
        }


        public static void RPCFix(Class48 c48, Class5 class5_0, uLink.NetworkPlayer networkPlayer_1)
        {
            Class56 class2 = c48.method_270(networkPlayer_1);
            if (class2 != null)
            {
                c48.method_277(class5_0, class2);
            }
            else
            {
                if (IsShuttingDown)
                {
                    return;
                }

                NetUser user = networkPlayer_1.GetLocalData() as NetUser;
                if (user != null)
                {
                    ulong id = user.userID;
                    if (uLinkDCCache.Contains(id))
                    {
                        return;
                    }

                    Logger.LogDebug("===Fougerite uLink===");
                    if (Fougerite.Server.Cache.ContainsKey(id))
                    {
                        Fougerite.Player player = Fougerite.Server.Cache[id];
                        if (player != null)
                        {
                            Logger.LogDebug("[Fougerite uLink] Detected RPC Failing Player: " + player.Name + "-" +
                                            player.SteamID + " Trying to kick...");
                            if (player.IsOnline)
                            {
                                player.Disconnect(false);
                                Logger.LogDebug("[Fougerite uLink] Should be kicked!");
                                return; // Return to avoid the RPC Logging
                            }

                            Logger.LogDebug("[Fougerite uLink] Server says It's offline. Not touching.");
                            uLinkDCCache.Add(player.UID);
                        }
                    }
                    else
                    {
                        Logger.LogDebug("[Fougerite uLink] Not existing in cache...");
                        uLinkDCCache.Add(id);
                    }
                }
                else
                {
                    Logger.LogDebug("===Fougerite uLink===");
                    Logger.LogDebug("[Fougerite uLink] Not existing in cache... (2x0)");
                }

                Logger.LogDebug("[Fougerite uLink] Private RPC (internal RPC " + class5_0.enum0_0 + ")" +
                                " was not sent because a connection to " + class5_0.networkPlayer_1 +
                                " was not found!");
                //NetworkLog.Error<string, string, uLink.NetworkPlayer, string>(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, "Private RPC ", (class5_0.method_11() ? class5_0.string_0 : ("(internal RPC " + class5_0.enum0_0 + ")")) + " was not sent because a connection to ", class5_0.networkPlayer_1, " was not found!");
            }
        }

        public static void RPCCatch(object obj)
        {
            var info = obj as uLink.NetworkMessageInfo;
            if (info == null)
            {
                return;
            }

            if (info.sender == uLink.NetworkPlayer.server)
            {
                return;
            }

            var netuser = info.sender.localData as NetUser;
            if (netuser == null)
            {
                return;
            }

            Logger.LogWarning("[Fougerite uLink] RPC Message from " + netuser.displayName + "-" + netuser.userID +
                              " triggered an exception. Kicking...");
            if (netuser.connected)
            {
                netuser.Kick(NetError.Facepunch_Kick_Violation, true);
            }
        }

        public static void uLinkCatch(Class0 instance)
        {
            string ip = ((IPEndPoint)(instance.endPoint_0)).Address.ToString();
            Logger.Log("[uLink Ignore] Ignored Socket from: " + ip);
        }

        public static Inventory.SlotOperationResult FGSlotOperation(Inventory inst, int fromSlot, Inventory toInventory,
            int toSlot, Inventory.SlotOperationsInfo info)
        {
            IInventoryItem itemf;
            IInventoryItem itemf2;
            if (((byte)((SlotOperations.Combine | SlotOperations.Move | SlotOperations.Stack) & info.SlotOperations)) ==
                0)
            {
                return Inventory.SlotOperationResult.Error_NoOpArgs;
            }

            if ((inst == null) || (toInventory == null))
            {
                return Inventory.SlotOperationResult.Error_MissingInventory;
            }

            if (inst == toInventory)
            {
                if (toSlot == fromSlot)
                {
                    return Inventory.SlotOperationResult.Error_SameSlot;
                }

                if ((((byte)(SlotOperations.EnsureAuthenticLooter & info.SlotOperations)) == 0x80) &&
                    !inst.IsAnAuthorizedLooter(info.Looter,
                        ((byte)(SlotOperations.ReportCheater & info.SlotOperations)) == 0x40, "slotop_srcdst"))
                {
                    return Inventory.SlotOperationResult.Error_NotALooter;
                }
            }
            else if (((byte)(SlotOperations.EnsureAuthenticLooter & info.SlotOperations)) == 0x80)
            {
                bool reportCheater = ((byte)(SlotOperations.ReportCheater & info.SlotOperations)) == 0x40;
                if (!inst.IsAnAuthorizedLooter(info.Looter, reportCheater, "slotop_src") ||
                    !toInventory.IsAnAuthorizedLooter(info.Looter, reportCheater, "slotop_dst"))
                {
                    ItemMoveEvent ime4 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                    if (ime4.Player != null)
                    {
                        Logger.LogError("[ItemLoot] The Game says " + ime4.Player.Name +
                                        " probably cheats with inv. Report this to DreTaX on fougerite.com");
                    }

                    return Inventory.SlotOperationResult.Error_NotALooter;
                }
            }

            if (!inst.GetItem(fromSlot, out itemf))
            {
                return Inventory.SlotOperationResult.Error_EmptySourceSlot;
            }

            if (toInventory.GetItem(toSlot, out itemf2))
            {
                InventoryItem.MergeResult failed;
                inst.MarkSlotDirty(fromSlot);
                toInventory.MarkSlotDirty(toSlot);
                if ((((byte)((SlotOperations.Combine | SlotOperations.Stack) & info.SlotOperations)) == 1) &&
                    (itemf.datablock.uniqueID == itemf2.datablock.uniqueID))
                {
                    failed = itemf.TryStack(itemf2);
                }
                else if (((byte)((SlotOperations.Combine | SlotOperations.Stack) & info.SlotOperations)) != 0)
                {
                    failed = itemf.TryCombine(itemf2);
                }
                else
                {
                    failed = InventoryItem.MergeResult.Failed;
                }

                switch (failed)
                {
                    case InventoryItem.MergeResult.Merged:
                        ItemMoveEvent ime2 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                        try
                        {
                            if (OnItemMove != null)
                            {
                                OnItemMove(ime2);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("ItemMoveEvent Error: " + ex);
                        }

                        return Inventory.SlotOperationResult.Success_Stacked;

                    case InventoryItem.MergeResult.Combined:
                        ItemMoveEvent ime3 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                        try
                        {
                            if (OnItemMove != null)
                            {
                                OnItemMove(ime3);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("ItemMoveEvent Error: " + ex);
                        }

                        return Inventory.SlotOperationResult.Success_Combined;
                }

                if (((byte)(SlotOperations.Move & info.SlotOperations)) == 4)
                {
                    return Inventory.SlotOperationResult.Error_OccupiedDestination;
                }

                return Inventory.SlotOperationResult.NoOp;
            }

            if (((byte)(SlotOperations.Move & info.SlotOperations)) == 0)
            {
                return Inventory.SlotOperationResult.Error_EmptyDestinationSlot;
            }

            if (!inst.MoveItemAtSlotToEmptySlot(toInventory, fromSlot, toSlot))
            {
                return Inventory.SlotOperationResult.Error_Failed;
            }

            if (inst != null)
            {
                inst.MarkSlotDirty(fromSlot);
            }

            if (toInventory != null)
            {
                toInventory.MarkSlotDirty(toSlot);
            }

            ItemMoveEvent ime = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
            try
            {
                if (OnItemMove != null)
                {
                    OnItemMove(ime);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ItemMoveEvent Error: " + ex);
            }

            return Inventory.SlotOperationResult.Success_Moved;
        }

        public static bool FGCompleteRepair(RepairBench inst, Inventory ingredientInv)
        {
            BlueprintDataBlock block;
            if (!inst.CanRepair(ingredientInv))
            {
                return false;
            }

            IInventoryItem repairItem = inst.GetRepairItem();
            if (!BlueprintDataBlock.FindBlueprintForItem<BlueprintDataBlock>(repairItem.datablock, out block))
            {
                return false;
            }

            RepairEvent re = new RepairEvent(inst, ingredientInv);
            try
            {
                if (OnRepairBench != null)
                {
                    OnRepairBench(re);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("RepairEvent Error: " + ex);
            }

            if (re._cancel)
            {
                return false;
            }

            for (int i = 0; i < block.ingredients.Length; i++)
            {
                BlueprintDataBlock.IngredientEntry entry = block.ingredients[i];
                int count = Mathf.RoundToInt(block.ingredients[i].amount * inst.GetResourceScalar());
                if (count > 0)
                {
                    while (count > 0)
                    {
                        int totalNum = 0;
                        IInventoryItem item2 = ingredientInv.FindItem(entry.Ingredient, out totalNum);
                        if (item2 != null)
                        {
                            if (item2.Consume(ref count))
                            {
                                ingredientInv.RemoveItem(item2.slot);
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            float num4 = repairItem.maxcondition - repairItem.condition;
            float num5 = (num4 * 0.2f) + 0.05f;
            repairItem.SetMaxCondition(repairItem.maxcondition - num5);
            repairItem.SetCondition(repairItem.maxcondition);
            return true;
        }

        public static void Action1BHook(ItemRepresentation itr, byte[] data, uLink.NetworkMessageInfo info)
        {
            if (data == null)
            {
                return;
            }

            if (itr == null)
            {
                return;
            }

            if (data.Length > 500)
            {
                return;
            }

            try
            {
                uLink.BitStream
                    stream = new uLink.BitStream(data, false); // Can only read once from the BitStream, so we copy It.

                Vector3 v = stream.ReadVector3();
                Vector3 v2 = stream.ReadVector3();
                if (v == null || v2 == null)
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }

                if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y)
                    || float.IsNaN(v.z) || float.IsInfinity(v.z))
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }

                if (float.IsNaN(v2.x) || float.IsInfinity(v2.x) || float.IsNaN(v2.y) || float.IsInfinity(v2.y)
                    || float.IsNaN(v2.z) || float.IsInfinity(v2.z))
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                uLink.BitStream copy = new uLink.BitStream(data, false);
                itr.RunServerAction(1, copy, ref info);
            }
            catch (Exception ex)
            {
                Logger.LogError("[Action1Error] Failed to call RunServerAction, Check logs.");
                Logger.LogDebug("Error: " + ex);
            }
        }

        internal static readonly Dictionary<ulong, int> ActionCooldown = new Dictionary<ulong, int>();

        public static void Action1Hook(ItemRepresentation itr, uLink.BitStream stream, uLink.NetworkMessageInfo info)
        {
            if (stream._data == null)
            {
                return;
            }

            if (itr == null)
            {
                return;
            }

            if (ServerSaveHandler.ServerIsSaving)
            {
                if (itr.networkViewOwner != null)
                {
                    NetUser user = itr.networkViewOwner.GetLocalData() as NetUser;
                    if (user != null)
                    {
                        Fougerite.Player player = Fougerite.Server.GetServer().FindPlayer(user.userID.ToString());
                        if (player != null)
                        {
                            if (!ActionCooldown.ContainsKey(player.UID))
                            {
                                ActionCooldown[player.UID] = 1;
                            }
                            else
                            {
                                ActionCooldown[player.UID] = ActionCooldown[player.UID] + 1;
                            }

                            if (ActionCooldown[player.UID] < 3)
                            {
                                player.Message(Bootstrap.SaveNotification);
                            }
                        }
                    }
                }

                return;
            }

            ActionCooldown.Clear();

            uLink.BitStream
                copy = new uLink.BitStream(stream._data,
                    false); // Can only read once from the BitStream, so we copy It.
            try
            {
                Vector3 v = copy.ReadVector3();
                Vector3 v2 = copy.ReadVector3();
                if (v == null || v2 == null)
                {
                    return;
                }

                if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y)
                    || float.IsNaN(v.z) || float.IsInfinity(v.z))
                {
                    return;
                }

                if (float.IsNaN(v2.x) || float.IsInfinity(v2.x) || float.IsNaN(v2.y) || float.IsInfinity(v2.y)
                    || float.IsNaN(v2.z) || float.IsInfinity(v2.z))
                {
                    return;
                }
            }
            catch
            {
                // Ignore
            }

            try
            {
                itr.RunServerAction(1, stream, ref info);
            }
            catch (Exception ex)
            {
                Logger.LogError("[Action2Error] Failed to call RunServerAction, Check logs.");
                Logger.LogDebug("Error: " + ex);
            }
        }

        public static bool OnBanEventHandler(BanEvent be)
        {
            try
            {
                if (OnPlayerBan != null)
                {
                    OnPlayerBan(be);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("BanEvent Error: " + ex);
            }

            return be.Cancelled;
        }

        public static void GenericHook(GenericSpawner gs)
        {
            try
            {
                if (OnGenericSpawnerLoad != null)
                {
                    OnGenericSpawnerLoad(gs);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("GenericSpawnerLoad Error: " + ex);
            }
        }

        public static IEnumerator ServerLoadedHook(ServerInit init, string levelName)
        {
            yield return RustLevel.Load(levelName);
            try
            {
                if (OnServerLoaded != null)
                {
                    OnServerLoaded();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("ServerLoaded Error: " + ex);
            }

            GameObject go = new GameObject();
            ServerSaveHandler h = go.AddComponent<ServerSaveHandler>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            World.GetWorld().ServerSaveHandler = h;
            ServerInitialized = true;
            Logger.Log("Server Initialized.");
            UnityEngine.Object.Destroy(init.gameObject);
            yield break;
        }

        public static void DoBeltUseHook(InventoryHolder holder, int beltNum)
        {
            try
            {
                if (holder == null)
                {
                    Logger.LogWarning("[DoBeltUse] Holder is null.");
                    return;
                }

                if (holder.inventory == null)
                {
                    Logger.LogWarning("[DoBeltUse] Inventory is null.");
                    return;
                }

                if (float.IsNaN(beltNum) || float.IsInfinity(beltNum) || beltNum < 0 || beltNum > 6)
                {
                    Logger.LogWarning("[DoBeltUse] Belt number is different. " + beltNum);
                    return;
                }

                PlayerInventory inventory;
                IInventoryItem item;
                BeltUseEvent be = new BeltUseEvent(holder, beltNum);
                try
                {
                    if (OnBeltUse != null)
                    {
                        OnBeltUse(be);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("BeltUseEvent Error: " + ex);
                }

                if (be.Cancelled)
                {
                    return;
                }

                if ((!holder.dead && (holder.GetPlayerInventory(out inventory))) &&
                    inventory.GetItem(30 + beltNum, out item))
                {
                    if (be.Bypassed || holder.ValidateAntiBeltSpam(NetCull.timeInMillis))
                    {
                        item.OnBeltUse();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[DoBeltUse Error] " + ex);
            }
        }

        public static void OnSupplySignalExplosion(SignalGrenade grenade)
        {
            Vector3 randompos = grenade.rigidbody.position +
                                new Vector3(UnityEngine.Random.Range((float)-20f, (float)20f), 75f,
                                    UnityEngine.Random.Range((float)-20f, (float)20f));
            SupplySignalExplosionEvent sg = new SupplySignalExplosionEvent(grenade, randompos);

            try
            {
                if (OnSupplySignalExpode != null)
                {
                    OnSupplySignalExpode(sg);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[SupplySignalExplosion Error] " + ex);
            }

            if (sg.Cancelled)
            {
                return;
            }

            SupplyDropZone.CallAirDropAt(randompos);
        }

        #region DataBlockFixes

        /*public static void DeployableItemDoAction1(DeployableItemDataBlock instance, uLink.BitStream stream, ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            try
            {
                IDeployableItem item;
                NetCull.VerifyRPC(ref info, false);
                if (rep.Item<IDeployableItem>(out item) && (item.uses > 0))
                {
                    Vector3 vector3;
                    Quaternion quaternion;
                    TransCarrier carrier;
                    Vector3 origin = stream.ReadVector3();
                    Vector3 direction = stream.ReadVector3();
                    Ray ray = new Ray(origin, direction);
                    if (!instance.CheckPlacement(ray, out vector3, out quaternion, out carrier))
                    {
                        Notice.Popup(info.sender, "?", "You can't place that here", 4f);
                    }
                    else
                    {
                        DeployableObject component = NetCull.InstantiateStatic(instance.DeployableObjectPrefabName, vector3, quaternion).GetComponent<DeployableObject>();
                        if (component != null)
                        {
                            try
                            {
                                component.SetupCreator(item.controllable);
                                instance.SetupDeployableObject(stream, rep, ref info, component, carrier);
                            }
                            finally
                            {
                                int count = 1;
                                Hooks.EntityDeployed(component, ref info);
                                if (item.Consume(ref count))
                                {
                                    item.inventory.RemoveItem(item.slot);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("DeployableItemDoAction1 Error: " + ex);
            }
        }*/

        public static bool DeployableCheckHook(DeployableItemDataBlock instance, Ray ray, out Vector3 pos,
            out Quaternion rot, out TransCarrier carrier)
        {
            DeployableItemDataBlock.DeployPlaceResults results;
            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction;
            if (float.IsNaN(origin.x) || float.IsInfinity(origin.x)
                                      || float.IsNaN(origin.y) || float.IsInfinity(origin.y)
                                      || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                carrier = null;
                return false;
            }

            if (float.IsNaN(direction.x) || float.IsInfinity(direction.x)
                                         || float.IsNaN(direction.y) || float.IsInfinity(direction.y)
                                         || float.IsNaN(direction.z) || float.IsInfinity(direction.z))
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                carrier = null;
                return false;
            }

            instance.CheckPlacementResults(ray, out pos, out rot, out carrier, out results);
            return results.Valid();
        }


        public static void BulletWeaponDoAction1(BulletWeaponDataBlock instance, uLink.BitStream stream,
            ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            GameObject obj2;
            NetEntityID yid;
            IDRemoteBodyPart part;
            bool flag;
            bool flag2;
            bool flag3;
            BodyPart part2;
            Vector3 vector;
            Vector3 vector2;
            Transform transform;
            IBulletWeaponItem item;
            NetCull.VerifyRPC(ref info, false);
            instance.ReadHitInfo(stream, out obj2, out flag, out flag2, out part2, out part, out yid, out transform,
                out vector, out vector2, out flag3);
            if ((rep.Item<IBulletWeaponItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp)) &&
                (item.uses > 0))
            {
                if (float.IsNaN(vector.x) || float.IsInfinity(vector.x) || float.IsNaN(vector.y) ||
                    float.IsInfinity(vector.y)
                    || float.IsNaN(vector.z) || float.IsInfinity(vector.z))
                {
                    return;
                }

                if (float.IsNaN(vector2.x) || float.IsInfinity(vector2.x) || float.IsNaN(vector2.y) ||
                    float.IsInfinity(vector2.y)
                    || float.IsNaN(vector2.z) || float.IsInfinity(vector2.z))
                {
                    return;
                }

                try
                {
                    if (OnShoot != null)
                    {
                        ShootEvent se = new ShootEvent(instance, obj2, rep, info, item, part, flag, flag2, flag3, part2,
                            vector, vector2);
                        OnShoot(se);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("ShootEvent Error: " + ex);
                }

                TakeDamage local = item.inventory.GetLocal<TakeDamage>();
                if ((local == null) || !local.dead)
                {
                    int count = 1;
                    item.Consume(ref count);
                    rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                    if (obj2 != null)
                    {
                        instance.ApplyDamage(obj2, transform, flag3, vector, part2, rep);
                    }

                    if (gunshots.aiscared && local != null)
                    {
                        local.GetComponent<Character>().AudibleMessage(20f, "HearDanger", local.transform.position);
                        local.GetComponent<Character>().AudibleMessage(10f, "HearDanger", vector);
                    }

                    if (!item.TryConditionLoss(0.33f, 0.01f))
                    {
                    }
                }
            }
        }

        public static void HandGrenadeDoAction1(HandGrenadeDataBlock grenade, uLink.BitStream stream,
            ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            IHandGrenadeItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<IHandGrenadeItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
            {
                rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                Vector3 origin = stream.ReadVector3();
                Vector3 forward = stream.ReadVector3();
                if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) || float.IsNaN(origin.y) ||
                    float.IsInfinity(origin.y)
                    || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
                {
                    return;
                }

                if (float.IsNaN(forward.x) || float.IsInfinity(forward.x) || float.IsNaN(forward.y) ||
                    float.IsInfinity(forward.y)
                    || float.IsNaN(forward.z) || float.IsInfinity(forward.z))
                {
                    return;
                }

                GameObject obj2 = grenade.ThrowItem(rep, origin, forward);
                if (obj2 != null)
                {
                    obj2.rigidbody.AddTorque(new Vector3(UnityEngine.Random.Range((float)-1f, (float)1f),
                        UnityEngine.Random.Range((float)-1f, (float)1f),
                        UnityEngine.Random.Range((float)-1f, (float)1f)) * 10f);
                    try
                    {
                        if (OnGrenadeThrow != null)
                        {
                            GrenadeThrowEvent se = new GrenadeThrowEvent(grenade, obj2, rep, info, item);
                            OnGrenadeThrow(se);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("GrenadeThrowEvent Error: " + ex);
                    }
                }

                int count = 1;
                if (item.Consume(ref count))
                {
                    item.inventory.RemoveItem(item.slot);
                }
            }
        }

        public static void ShotgunDoAction1(ShotgunDataBlock instance, uLink.BitStream stream, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info)
        {
            NetCull.VerifyRPC(ref info, false);
            IBulletWeaponItem found = null;
            if (rep.Item<IBulletWeaponItem>(out found) && (found.uses > 0))
            {
                TakeDamage local = found.inventory.GetLocal<TakeDamage>();
                if (((local == null) || !local.dead) && found.ValidatePrimaryMessageTime(info.timestamp))
                {
                    int count = 1;
                    found.Consume(ref count);
                    found.itemRepresentation.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                    instance.GetBulletRange(rep);

                    int pellets = instance.numPellets;
                    ShotgunShootEvent tempcall = new ShotgunShootEvent(instance, rep, info, found, pellets,
                        ShotgunEventType.BeforeShot);
                    try
                    {
                        if (OnShotgunShoot != null)
                        {
                            OnShotgunShoot(tempcall);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("ShotgunShootEvent Error: " + ex);
                    }

                    pellets = tempcall.Pellets;

                    for (uint i = 0; i < pellets; i++)
                    {
                        GameObject obj2;
                        NetEntityID yid;
                        IDRemoteBodyPart part;
                        bool flag;
                        bool flag2;
                        bool flag3;
                        BodyPart part2;
                        Vector3 vector;
                        Vector3 vector2;
                        Transform transform;
                        instance.ReadHitInfo(stream, out obj2, out flag, out flag2, out part2, out part, out yid,
                            out transform, out vector, out vector2, out flag3);

                        if (float.IsNaN(vector.x) || float.IsInfinity(vector.x) || float.IsNaN(vector.y) ||
                            float.IsInfinity(vector.y)
                            || float.IsNaN(vector.z) || float.IsInfinity(vector.z))
                        {
                            return;
                        }

                        if (float.IsNaN(vector2.x) || float.IsInfinity(vector2.x) || float.IsNaN(vector2.y) ||
                            float.IsInfinity(vector2.y)
                            || float.IsNaN(vector2.z) || float.IsInfinity(vector2.z))
                        {
                            return;
                        }

                        try
                        {
                            if (OnShotgunShoot != null)
                            {
                                ShotgunShootEvent se = new ShotgunShootEvent(instance, rep, info, found,
                                    ShotgunEventType.AfterShot, part, flag, flag2, flag3, part2, vector, vector2);
                                OnShotgunShoot(se);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("ShotgunShootEvent Error: " + ex);
                        }

                        if (obj2 != null)
                        {
                            instance.ApplyDamage(obj2, transform, flag3, vector, part2, rep);
                        }
                    }

                    found.TryConditionLoss(0.5f, 0.02f);
                }
            }
        }

        public static void StructureComponentDoAction1(StructureComponentDataBlock instance, uLink.BitStream stream,
            ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            IStructureComponentItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<IStructureComponentItem>(out item) && (item.uses > 0))
            {
                StructureComponent structureToPlacePrefab = instance.structureToPlacePrefab;
                Vector3 origin = stream.ReadVector3();
                Vector3 direction = stream.ReadVector3();
                Vector3 position = stream.ReadVector3();
                Quaternion rotation = stream.ReadQuaternion();
                uLink.NetworkViewID viewID = stream.ReadNetworkViewID();
                if (viewID == null || float.IsNaN(viewID.id) || float.IsInfinity(viewID.id))
                {
                    return;
                }

                if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) || float.IsNaN(origin.y) ||
                    float.IsInfinity(origin.y)
                    || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
                {
                    return;
                }

                if (float.IsNaN(direction.x) || float.IsInfinity(direction.x) || float.IsNaN(direction.y) ||
                    float.IsInfinity(direction.y)
                    || float.IsNaN(direction.z) || float.IsInfinity(direction.z))
                {
                    return;
                }

                if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) ||
                    float.IsInfinity(position.y)
                    || float.IsNaN(position.z) || float.IsInfinity(position.z))
                {
                    return;
                }

                if (float.IsNaN(rotation.x) || float.IsInfinity(rotation.x) || float.IsNaN(rotation.y) ||
                    float.IsInfinity(rotation.y)
                    || float.IsNaN(rotation.z) || float.IsInfinity(rotation.z) || float.IsNaN(rotation.w) ||
                    float.IsInfinity(rotation.w))
                {
                    return;
                }

                StructureMaster component = null;
                if (viewID == uLink.NetworkViewID.unassigned)
                {
                    if (instance.MasterFromRay(new Ray(origin, direction)))
                    {
                        return;
                    }

                    if (structureToPlacePrefab.type != StructureComponent.StructureComponentType.Foundation)
                    {
                        //Debug.Log("ERROR, tried to place non foundation structure on terrain!");
                    }
                    else
                    {
                        component = NetCull.InstantiateClassic<StructureMaster>(
                            Bundling.Load<StructureMaster>("content/structures/StructureMasterPrefab"), position,
                            rotation, 0);
                        component.SetupCreator(item.controllable);
                    }
                }
                else
                {
                    component = uLink.NetworkView.Find(viewID).gameObject.GetComponent<StructureMaster>();
                }

                if (component == null)
                {
                    return;
                    //Debug.Log("NO master, something seriously wrong");
                }

                if (instance._structureToPlace.CheckLocation(component, position, rotation) &&
                    instance.CheckBlockers(position))
                {
                    StructureComponent component2 = NetCull
                        .InstantiateStatic(instance.structureToPlaceName, position, rotation)
                        .GetComponent<StructureComponent>();
                    if (component2 != null)
                    {
#pragma warning disable 618
                        component.AddStructureComponent(component2);
#pragma warning restore 618
                        int count = 1;
                        Hooks.EntityDeployed(component2, ref info);
                        if (item.Consume(ref count))
                        {
                            item.inventory.RemoveItem(item.slot);
                        }
                    }
                }
            }
        }

        public static void TorchDoAction1(TorchItemDataBlock instance, uLink.BitStream stream, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info)
        {
            ITorchItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<ITorchItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
            {
                if (item.isLit)
                {
                    item.Extinguish();
                }

                rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                Vector3 origin = stream.ReadVector3();
                Vector3 forward = stream.ReadVector3();
                if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) || float.IsNaN(origin.y) ||
                    float.IsInfinity(origin.y)
                    || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
                {
                    return;
                }

                if (float.IsNaN(forward.x) || float.IsInfinity(forward.x) || float.IsNaN(forward.y) ||
                    float.IsInfinity(forward.y)
                    || float.IsNaN(forward.z) || float.IsInfinity(forward.z))
                {
                    return;
                }


                instance.ThrowFlare(rep, origin, forward);
                int count = 1;
                if (item.Consume(ref count))
                {
                    item.inventory.RemoveItem(item.slot);
                }
            }
        }

        #endregion

        public static void ResetHooks()
        {
            OnPluginInit = delegate { };
            OnPlayerTeleport = delegate(Fougerite.Player param0, Vector3 param1, Vector3 param2) { };
            OnChat = delegate(Fougerite.Player param0, ref ChatString param1) { };
            OnChatRaw = delegate(ref ConsoleSystem.Arg param0) { };
            OnCommand = delegate(Fougerite.Player param0, string param1, string[] param2) { };
            OnCommandRaw = delegate(ref ConsoleSystem.Arg param0) { };
            OnPlayerConnected = delegate(Fougerite.Player param0) { };
            OnPlayerDisconnected = delegate(Fougerite.Player param0) { };
            OnNPCKilled = delegate(DeathEvent param0) { };
            OnNPCHurt = delegate(HurtEvent param0) { };
            OnPlayerKilled = delegate(DeathEvent param0) { };
            OnPlayerHurt = delegate(HurtEvent param0) { };
            OnPlayerSpawned = delegate(Fougerite.Player param0, SpawnEvent param1) { };
            OnPlayerSpawning = delegate(Fougerite.Player param0, SpawnEvent param1) { };
            OnPlayerGathering = delegate(Fougerite.Player param0, GatherEvent param1) { };
            OnEntityHurt = delegate(HurtEvent param0) { };
            OnEntityDestroyed = delegate(DestroyEvent param0) { };
            OnEntityDecay = delegate(DecayEvent param0) { };
            OnEntityDeployed = delegate(Fougerite.Player param0, Entity param1) { };
            OnEntityDeployedWithPlacer = delegate(Fougerite.Player param0, Entity param1, Fougerite.Player param2) { };
            OnConsoleReceived = delegate(ref ConsoleSystem.Arg param0, bool param1) { };
            OnConsoleReceivedWithCancel = delegate(ref ConsoleSystem.Arg param0, bool param1, ConsoleEvent ce) { };
            OnBlueprintUse = delegate(Fougerite.Player param0, BPUseEvent param1) { };
            OnDoorUse = delegate(Fougerite.Player param0, DoorEvent param1) { };
            OnTablesLoaded = delegate(Dictionary<string, LootSpawnList> param0) { };
            OnItemsLoaded = delegate(ItemsBlocks param0) { };
            OnServerInit = delegate { };
            OnServerShutdown = delegate { };
            OnModulesLoaded = delegate { };
            OnRecieveNetwork = delegate(Fougerite.Player param0, Metabolism param1, float param2, float param3,
                float param4, float param5, float param6, float param7)
            {
            };
            OnShowTalker = delegate(uLink.NetworkPlayer param0, Fougerite.Player param1) { };
            OnCrafting = delegate(Fougerite.Events.CraftingEvent param0) { };
            OnResourceSpawned = delegate(ResourceTarget param0) { };
            OnItemRemoved = delegate(Fougerite.Events.InventoryModEvent param0) { };
            OnItemAdded = delegate(Fougerite.Events.InventoryModEvent param0) { };
            OnAirdropCalled = delegate(Vector3 param0) { };
            OnSteamDeny = delegate(SteamDenyEvent param0) { };
            OnPlayerApproval = delegate(PlayerApprovalEvent param0) { };
            OnPlayerMove = delegate(HumanController param0, Vector3 param1, int param2, ushort param3,
                uLink.NetworkMessageInfo param4, Util.PlayerActions param5)
            {
            };
            OnResearch = delegate(ResearchEvent param0) { };
            OnServerSaved = delegate { };
            OnItemPickup = delegate(ItemPickupEvent param0) { };
            OnFallDamage = delegate(FallDamageEvent param0) { };
            OnLootUse = delegate(LootStartEvent param0) { };
            OnShoot = delegate(ShootEvent param0) { };
            OnBowShoot = delegate(BowShootEvent param0) { };
            OnShotgunShoot = delegate(ShotgunShootEvent param0) { };
            OnGrenadeThrow = delegate(GrenadeThrowEvent param0) { };
            OnPlayerBan = delegate(BanEvent param0) { };
            OnRepairBench = delegate(RepairEvent param0) { };
            OnItemMove = delegate(ItemMoveEvent param0) { };
            OnGenericSpawnerLoad = delegate(GenericSpawner param0) { };
            OnServerLoaded = delegate() { };
            OnSupplySignalExpode = delegate(SupplySignalExplosionEvent param0) { };
            OnBeltUse = delegate(BeltUseEvent param0) { };
            OnLogger = delegate(LoggerEvent param0) { };
            foreach (Fougerite.Player player in Fougerite.Server.GetServer().Players)
            {
                player.FixInventoryRef();
            }
        }

        public static void ServerShutdown()
        {
            IsShuttingDown = true;
            try
            {
                if (OnServerShutdown != null)
                    OnServerShutdown();
            }
            catch (Exception ex)
            {
                Logger.LogError("ServerShutdownEvent Error: " + ex.ToString());
            }

            World.GetWorld().ServerSaveHandler.ManualSave();
        }

        public static void ServerStarted()
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            DataStore.GetInstance().Load();
            Server.GetServer().UpdateBanlist();
            try
            {
                if (OnServerInit != null)
                    OnServerInit();
            }
            catch (Exception ex)
            {
                Logger.LogError("ServerInitEvent Error: " + ex.ToString());
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("ServerStartedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        private static DateTime LasTime2 = DateTime.Now;

        public static bool ConfirmVoice(VoiceCom com, byte[] data)
        {
            uLink.NetworkPlayer nplayer = com.networkViewOwner;

            DateTime now = DateTime.Now;
            DateTime then = LasTime2;
            double diff = (now - then).TotalSeconds;
            if (data == null)
            {
                if (diff > 10)
                {
                    LasTime2 = DateTime.Now;
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(nplayer);
                    if (player != null)
                    {
                        Logger.LogWarning("[VoiceByteOverflown] Received null value. Possible Sender: " + player.Name +
                                          " - " + player.SteamID + " - " + player.IP);
                    }
                    else
                    {
                        Logger.LogWarning("[VoiceByteOverflown] Received null value.");
                    }
                }

                return false;
            }

            if (data.Length > 15000)
            {
                if (diff > 10)
                {
                    LasTime2 = DateTime.Now;
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(nplayer);
                    if (player != null)
                    {
                        Logger.LogWarning("[VoiceByteOverflown] Received a huge amount of byte, clearing. " +
                                          data.Length + " " + player.Name + " - " + player.SteamID + " - " + player.IP);
                    }
                    else
                    {
                        Logger.LogWarning("[VoiceByteOverflown] Received a huge amount of byte, clearing. " +
                                          data.Length);
                    }
                }

                Array.Clear(data, 0, data.Length);
                return false;
            }

            for (uint i = 0; i < data.Length;)
            {
                try
                {
                    uint conversion = (uint)BitConverter.ToInt32(data, (int)i);
                    if (conversion > 10000)
                    {
                        if (diff > 10)
                        {
                            LasTime2 = DateTime.Now;
                            Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(nplayer);
                            if (player != null)
                            {
                                Logger.LogWarning("[VoiceByteOverflown] Received a huge amount of byte, clearing. " +
                                                  conversion + " " + player.Name + " - " + player.SteamID + " - " +
                                                  player.IP);
                            }
                            else
                            {
                                Logger.LogWarning("[VoiceByteOverflown] Received a huge amount of byte, clearing. " +
                                                  conversion);
                            }
                        }

                        Array.Clear(data, 0, data.Length);
                        return false;
                    }

                    i += conversion + 6;
                }
                catch
                {
                    if (diff > 10)
                    {
                        LasTime2 = DateTime.Now;
                        Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(nplayer);
                        if (player != null)
                        {
                            Logger.LogWarning(
                                "[VoiceByteOverflown] Seems like an error occured while reading the voice bytes. Someone is trying to send false packets? "
                                + player.Name + " - " + player.SteamID + " - " + player.IP);
                        }
                        else
                        {
                            Logger.LogWarning(
                                "[VoiceByteOverflown] Seems like an error occured while reading the voice bytes. Someone is trying to send false packets?");
                        }
                    }

                    Array.Clear(data, 0, data.Length);
                    return false;
                }
            }

            return true;
        }

        public static void ShowTalker(PlayerClient p, PlayerClient p2)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            var pl = Fougerite.Server.GetServer().FindPlayer(p2.userID);
            try
            {
                if (OnShowTalker != null)
                    OnShowTalker(p.netPlayer, pl);
            }
            catch (Exception ex)
            {
                Logger.LogError("ShowTalkerEvent Error: " + ex.ToString());
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("MicUseEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        private static DateTime LasTime11 = DateTime.Now;

        public static void FallDamageCheck(FallDamage fd, float v)
        {
            DateTime now = DateTime.Now;
            DateTime then = LasTime11;
            double diff = (now - then).TotalSeconds;
            if (diff > 10)
            {
                Logger.LogWarning("[Legbreak RPC] Bypassed a legbreak RPC possibly sent by a hacker. Value: " + v);
                LasTime11 = DateTime.Now;
            }
            //fd.SetLegInjury(v);
        }

        public static bool RegisterHook(ServerSave save)
        {
            if (ServerSaveHandler.ServerIsSaving)
            {
                // Return false if the registers already contain our save class. (Tell that we have already added it)
                if (ServerSaveManager.Instances.registers.Contains(save))
                {
                    return false;
                }

                // If we already added It to our temp dictionary, then return false. (Tell that we have already added it)
                if (ServerSaveHandler.UnProcessedSaves.ContainsKey(save))
                {
                    return false;
                }

                ServerSaveHandler.UnProcessedSaves.Add(save, 1);
            }
            else
            {
                if (!ServerSaveManager.Instances.registers.Add(save))
                {
                    return false;
                }

                ServerSaveManager.Instances.ordered.Add(save);
            }

            return true;
        }

        public static bool UnRegisterHook(ServerSave save)
        {
            if (ServerSaveHandler.ServerIsSaving)
            {
                // Return false if the registers doesn't contain our save class. (Tell that It doesn't exist)
                if (!ServerSaveManager.Instances.registers.Contains(save))
                {
                    return false;
                }

                // If we have already added the value for later processing return false (Tell that It doesn't exist)
                if (ServerSaveHandler.UnProcessedSaves.ContainsKey(save))
                {
                    return false;
                }

                ServerSaveHandler.UnProcessedSaves.Add(save, 2);
            }
            else
            {
                if (!ServerSaveManager.Instances.registers.Remove(save))
                {
                    return false;
                }

                ServerSaveManager.Instances.ordered.Remove(save);
            }

            return true;
        }

        internal static void ModulesLoaded()
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            try
            {
                if (OnModulesLoaded != null)
                    OnModulesLoaded();
            }
            catch (Exception ex)
            {
                Logger.LogError("ModulesLoadedEvent Error: " + ex.ToString());
            }

            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0)
                Logger.LogSpeed("ModulesLoadedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void TossBypass(InventoryHolder holder, uLink.BitStream stream, uLink.NetworkMessageInfo info)
        {
            if (info == null || info.sender == null)
            {
                return;
            }

            Inventory inventory = holder.inventory;
            Facepunch.NetworkView networkView = holder.networkView;

            if (networkView.owner != info.networkView.owner)
            {
                return;
            }

            if (networkView.owner != info.sender)
            {
                return;
            }

            int data;
            try
            {
                data = (int)Inventory.RPCInteger(stream);
            }
            catch
            {
                return;
            }

            if (float.IsNaN(data) || float.IsInfinity(data) || data > 39)
            {
                return;
            }

            InventoryItem item;
            if (inventory.collection.Get(data, out item))
            {
                DropHelper.DropItem(inventory, data);
            }
        }

        public static void LoggerEvent(LoggerEventType type, string message)
        {
            try
            {
                if (OnLogger != null)
                {
                    Events.LoggerEvent evt = new LoggerEvent(type, message);
                    OnLogger(evt);
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorIgnore("LoggerEvent Error: " + ex.ToString(), null, true);
            }
        }

        public static Dictionary<string, LootSpawnList> TablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            try
            {
                if (OnTablesLoaded != null)
                    OnTablesLoaded(lists);
            }
            catch (Exception ex)
            {
                Logger.LogError("TablesLoadedEvent Error: " + ex.ToString());
            }

            return lists;
        }

        private static DateTime LasTime3 = DateTime.Now;

        public static void ITSPHook(Inventory instance, byte slotNumber, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime3;
            double diff = (now - then).TotalSeconds;
            if (float.IsNaN(slotNumber) || float.IsInfinity(slotNumber) || slotNumber > 39)
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[ITSP InvalidPacket] " + slotNumber
                                                                  + " - " + player.Name + " - " + player.SteamID +
                                                                  " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid ITSP Packet.");
                    }

                    LasTime3 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.IsAnAuthorizedLooter(info.sender) && instance.collection.Get(slotNumber, out item))
            {
                instance.SplitStack(slotNumber);
            }
        }

        private static DateTime LasTime4 = DateTime.Now;

        public static void IACTHook(Inventory instance, byte itemIndex, byte action, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime4;
            double diff = (now - then).TotalSeconds;
            if (float.IsNaN(itemIndex) || float.IsInfinity(itemIndex) || itemIndex > 39
                || float.IsNaN(action) || float.IsInfinity(action) ||
                !Enum.IsDefined(typeof(InventoryItem.MenuItem), action))
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[IACT InvalidPacket] " + itemIndex + " - " + action
                                          + " - " + player.Name + " - " + player.SteamID + " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid IACT Packet.");
                    }

                    LasTime4 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if ((info.sender == instance.networkView.owner) && instance.collection.Get(itemIndex, out item))
            {
                item.OnMenuOption((InventoryItem.MenuItem)action);
            }
        }

        private static bool CheckSenderIsNonOwningClient(Inventory instance, uLink.NetworkPlayer sender)
        {
            if (sender.isClient && (sender != instance.networkView.owner))
            {
                return true;
            }

            return false;
        }

        private static DateTime LasTime5 = DateTime.Now;

        public static void IASTHook(Inventory instance, byte itemIndex, uLink.NetworkViewID itemRepID,
            uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime5;
            double diff = (now - then).TotalSeconds;
            // Just to make sure.
            if (float.IsNaN(itemIndex) || float.IsInfinity(itemIndex) || itemIndex > 39)
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[IAST InvalidPacket] " + itemIndex + " - " + itemRepID
                                          + " - " + player.Name + " - " + player.SteamID + " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid IAST Packet.");
                    }

                    LasTime5 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (!CheckSenderIsNonOwningClient(instance, info.sender) && instance.collection.Get(itemIndex, out item))
            {
                instance.SetActiveItemManually(itemIndex,
                    !(itemRepID != uLink.NetworkViewID.unassigned)
                        ? null
                        : uLink.NetworkView.Find(itemRepID).GetComponent<ItemRepresentation>(),
                    new uLink.NetworkViewID?(itemRepID));
            }
        }

        public static void OC1Hook(Controllable instance, uLink.NetworkViewID rootViewID, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (uLink.NetworkView.Find(rootViewID) != null)
            {
                instance.OverrideControlOfHandleRPC(rootViewID, rootViewID, ref info);
            }
        }

        public static void OC2Hook(Controllable instance, uLink.NetworkViewID rootViewID,
            uLink.NetworkViewID parentViewID, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (uLink.NetworkView.Find(rootViewID) != null)
            {
                instance.OverrideControlOfHandleRPC(rootViewID, parentViewID, ref info);
            }
        }

        public static void CLRHook(Controllable instance, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            Controllable bt = instance.ch.bt;
            Facepunch.NetworkView networkView = bt.networkView;

            if ((networkView != null) && (networkView.viewID != uLink.NetworkViewID.unassigned))
            {
                NetCull.RemoveRPCsByName(networkView, "Controllable:RFH");
                while (bt._sentRootControlCount > instance.ch.id)
                {
                    string view = Controllable.kClientSideRootNumberRPCName[bt._sentRootControlCount--];
                    NetCull.RemoveRPCsByName(networkView, view);
                }
            }

            instance.ch.Delete();
            if (((bt != null) && ((bt.RT & 0xc00) == 0)) &&
                ((networkView != null) && (networkView.viewID != uLink.NetworkViewID.unassigned)))
            {
                networkView.RPC<byte>("Controllable:RFH", uLink.RPCMode.OthersBuffered, (byte)bt._sentRootControlCount);
            }

            instance.SharedPostCLR();
        }

        public static void CLDHook(Controllable instance, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (info.sender != instance.networkViewOwner)
            {
                return;
            }

            Controllable.Disconnect(instance);
        }

        private static DateTime LasTime6 = DateTime.Now;

        public static void ISMVHook(Inventory instance, byte fromSlot, byte toSlot, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime6;
            double diff = (now - then).TotalSeconds;
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[ISMV InvalidPacket] " + fromSlot
                                                                  + " - " + toSlot + " - " + player.Name + " - " +
                                                                  player.SteamID + " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid ISMV Packet.");
                    }

                    LasTime6 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory.SlotOperationResult message =
                    instance.SlotOperation(fromSlot, toSlot, Inventory.SlotOperationsMove(info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        private static DateTime LasTime7 = DateTime.Now;

        public static void ITMGHook(Inventory instance, NetEntityID toInvID, byte fromSlot, byte toSlot,
            bool tryCombine, uLink.NetworkMessageInfo info)
        {
            if (info == null || toInvID == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime7;
            double diff = (now - then).TotalSeconds;
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[ITMG InvalidPacket] " + fromSlot
                                                                  + " - " + toSlot + " - " + player.Name + " - " +
                                                                  player.SteamID + " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid ITMG Packet.");
                    }

                    LasTime7 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory component = toInvID.GetComponent<Inventory>();
                Inventory.SlotOperationResult message = instance.SlotOperation(fromSlot, component, toSlot,
                    Inventory.SlotOperationsMerge(tryCombine, info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        private static DateTime LasTime8 = DateTime.Now;

        public static void ITMVHook(Inventory instance, NetEntityID toInvID, byte fromSlot, byte toSlot,
            uLink.NetworkMessageInfo info)
        {
            if (info == null || toInvID == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime8;
            double diff = (now - then).TotalSeconds;
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[ITMV InvalidPacket] " + fromSlot
                                                                  + " - " + toSlot + " - " + player.Name + " - " +
                                                                  player.SteamID + " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid ITMV Packet.");
                    }

                    LasTime8 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory component = toInvID.GetComponent<Inventory>();
                Inventory.SlotOperationResult message = instance.SlotOperation(fromSlot, component, toSlot,
                    Inventory.SlotOperationsMove(info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        private static DateTime LasTime9 = DateTime.Now;

        public static void ITSMHook(Inventory instance, byte fromSlot, byte toSlot, bool tryCombine,
            uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime9;
            double diff = (now - then).TotalSeconds;
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[ITSM InvalidPacket] " + fromSlot
                                                                  + " - " + toSlot + " - " + player.Name + " - " +
                                                                  player.SteamID + " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid ITSM Packet.");
                    }

                    LasTime9 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory.SlotOperationResult message = instance.SlotOperation(fromSlot, toSlot,
                    Inventory.SlotOperationsMerge(tryCombine, info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        private static DateTime LasTime10 = DateTime.Now;

        public static void SVUCHook(Inventory instance, byte cell, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime then = LasTime10;
            double diff = (now - then).TotalSeconds;
            if (float.IsNaN(cell) || float.IsInfinity(cell) || cell > 39)
            {
                if (diff > 10)
                {
                    Fougerite.Player player = Fougerite.Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning("[SVUC InvalidPacket] " + cell
                                                                  + " - " + player.Name + " - " + player.SteamID +
                                                                  " - " + player.IP);
                        Fougerite.Server.GetServer().BanPlayer(player, "Console", "Invalid SVUC Packet.");
                    }

                    LasTime10 = DateTime.Now;
                }

                return;
            }

            if (instance.IsAnAuthorizedLooter(info.sender, true, "reqinvcellupdate"))
            {
                instance.MarkSlotDirty(cell);
            }
        }


        public static void CRFSHook(CraftingInventory instance, int amount, int blueprintUID,
            uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (info.sender != instance.networkViewOwner)
            {
                return;
            }

            if (float.IsNaN(amount) || float.IsNaN(blueprintUID) || float.IsInfinity(amount) ||
                float.IsInfinity(blueprintUID))
            {
                return;
            }

            if (float.IsNaN(info.timestampInMillis) || float.IsInfinity(info.timestampInMillis))
            {
                return;
            }

            BlueprintDataBlock bd = CraftingInventory.FindBlueprint(blueprintUID);
            if (bd != null)
            {
                instance.StartCrafting(bd, amount, info.timestampInMillis);
            }
        }

        private static NGC.Procedure Message(NGC instance, byte[] data, int offset, int length,
            uLink.NetworkMessageInfo info)
        {
            try
            {
                int num4;
                byte[] buffer;
                int startIndex = offset;
                int num2 = BitConverter.ToInt32(data, startIndex);
                startIndex += 4;
                int num3 = offset + length;
                if (startIndex == num3)
                {
                    buffer = null;
                    num4 = 0;
                }
                else
                {
                    num4 = num3 - startIndex;
                    buffer = new byte[num4];
                    int num5 = 0;
                    do
                    {
                        byte val = data[startIndex++];
                        buffer[num5++] = val;
                    } while (startIndex < num3);
                }

                return instance.Message(num2, buffer, num4, info);
            }
            catch (Exception ex)
            {
                Logger.LogError("Caught an NGC Error: " + ex);
                // ignore
            }

            return null;
        }

        public static void CHook(NGC instance, byte[] data, uLink.NetworkMessageInfo info)
        {
            if (data.Length > 30000)
            {
                Array.Clear(data, 0, data.Length);
                Logger.LogError("CHook Received: " + data.Length);
                return;
            }

            NGC.Procedure procedure = Message(instance, data, 0, data.Length, info);
            if (procedure != null && !procedure.Call())
            {
                if (procedure.view != null)
                {
                    Logger.LogWarning(
                        "Did not call rpc " +
                        procedure.view.prefab.installation.methods[procedure.message].method.Name + " for view " +
                        procedure.view.name + " (entid:{procedure.view.id},msg:{procedure.message})", instance);
                }
                else if (NGC.log_nonexistant_ngc_errors)
                {
                    Logger.LogWarning(
                        $"Did not call rpc to non existant view# {procedure.target}. ( message id was {procedure.message} )",
                        instance);
                }
            }
        }

        public static bool AHook(NGC instance, byte[] data, uLink.NetworkMessageInfo info)
        {
            if (data.Length > 30000)
            {
                Array.Clear(data, 0, data.Length);
                Logger.LogError("AHook Received: " + data.Length);
                return false;
            }

            if (info.sender != uLink.NetworkPlayer.server)
            {
                Array.Clear(data, 0, data.Length);
                Logger.LogError("AHook Received: " + data.Length + " Not server.");
                return false;
            }

            return true;
        }

        public static bool InternalRPCCheck(Class5 class5)
        {
            string str;
            try
            {
                str = class5.class56_0.ipendPoint_0.Address.ToString();
            }
            catch
            {
                return false;
            }

            if (!Server.GetServer().IsBannedIP(str))
            {
                try
                {
                    Class5.Enum0 num = (Class5.Enum0)class5.enum0_0;
                    Enum8 num2 = class5.enum8_0;
                    if (Enum.IsDefined(typeof(Class5.Enum0), num) && Enum.IsDefined(typeof(Enum8), num2))
                    {
                        return true;
                    }
                }
                catch
                {
                    //ignore
                }

                Server.GetServer().BanPlayerIP(str, "1", "uLink AuthorizationCheck", "Fougerite");
                Logger.LogWarning("[Fougerite uLinkInternalCheck] Hoax IP automatically banned, and rejected: " + str,
                    null);
            }

            return false;
        }

        public static void uLinkAuthorizationCheck(Class48 class48, Class5 class5_0)
        {
            string str;
            try
            {
                str = class5_0.class56_0.ipendPoint_0.Address.ToString();
            }
            catch
            {
                return;
            }

            if (!Server.GetServer().IsBannedIP(str))
            {
                NetworkLog.Debug<string, Class5>(NetworkLogFlags.RPC, "Server handling ", class5_0);
                NetworkLog.Debug<string, string, string, Struct15>(NetworkLogFlags.Timestamp, "Server got message ",
                    class5_0.string_0, " with timestamp ", class5_0.struct15_0);
                if (class5_0.method_16())
                {
                    NetworkLog.Error<Class5, string>(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, class5_0,
                        " is from another server and will be dropped!");
                }
                else
                {
                    if (!class5_0.method_15())
                    {
                        if (class48.bool_1)
                        {
                            Logger.LogWarning(
                                "[Fougerite uLinkAuthorizationCheck] Hoax IP automatically banned, and rejected: " +
                                str, null);
                            Server.GetServer().BanPlayerIP(str, "1", "uLink AuthorizationCheck", "Fougerite");
                            return;
                        }

                        class48.vmethod_35(new Class5(class48, class5_0));
                    }
                    else if (class5_0.method_1())
                    {
                        class48.method_281(class5_0);
                        return;
                    }

                    if (class5_0.method_14())
                    {
                        class48.method_73(class5_0);
                    }
                }
            }
        }

        public delegate void BlueprintUseHandlerDelegate(Fougerite.Player player, BPUseEvent ae);

        public delegate void ChatHandlerDelegate(Fougerite.Player player, ref ChatString text);

        public delegate void ChatRawHandlerDelegate(ref ConsoleSystem.Arg arg);

        public delegate void CommandHandlerDelegate(Fougerite.Player player, string cmd, string[] args);

        public delegate void CommandRawHandlerDelegate(ref ConsoleSystem.Arg arg);

        public delegate void ConnectionHandlerDelegate(Fougerite.Player player);

        public delegate void ConsoleHandlerDelegate(ref ConsoleSystem.Arg arg, bool external);

        public delegate void
            ConsoleHandlerWithCancelDelegate(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent ce);

        public delegate void DisconnectionHandlerDelegate(Fougerite.Player player);

        public delegate void DoorOpenHandlerDelegate(Fougerite.Player player, DoorEvent de);

        public delegate void EntityDecayDelegate(DecayEvent de);

        public delegate void EntityDeployedDelegate(Fougerite.Player player, Entity e);

        public delegate void EntityDeployedWithPlacerDelegate(Fougerite.Player player, Entity e,
            Fougerite.Player actualplacer);

        public delegate void EntityHurtDelegate(HurtEvent he);

        public delegate void EntityDestroyedDelegate(DestroyEvent de);

        public delegate void HurtHandlerDelegate(HurtEvent he);

        public delegate void ItemsDatablocksLoaded(ItemsBlocks items);

        public delegate void KillHandlerDelegate(DeathEvent de);

        public delegate void LootTablesLoaded(Dictionary<string, LootSpawnList> lists);

        public delegate void PlayerGatheringHandlerDelegate(Fougerite.Player player, GatherEvent ge);

        public delegate void PlayerSpawnHandlerDelegate(Fougerite.Player player, SpawnEvent se);

        public delegate void ShowTalkerDelegate(uLink.NetworkPlayer player, Fougerite.Player p);

        public delegate void PluginInitHandlerDelegate();

        public delegate void TeleportDelegate(Fougerite.Player player, Vector3 from, Vector3 dest);

        public delegate void ServerInitDelegate();

        public delegate void ServerShutdownDelegate();

        public delegate void ModulesLoadedDelegate();

        public delegate void RecieveNetworkDelegate(Fougerite.Player player, Metabolism m, float cal, float water,
            float rad, float anti, float temp, float poison);

        public delegate void CraftingDelegate(Fougerite.Events.CraftingEvent e);

        public delegate void ResourceSpawnDelegate(ResourceTarget t);

        public delegate void ItemRemovedDelegate(Fougerite.Events.InventoryModEvent e);

        public delegate void ItemAddedDelegate(Fougerite.Events.InventoryModEvent e);

        public delegate void AirdropDelegate(Vector3 v);

        public delegate void SteamDenyDelegate(SteamDenyEvent sde);

        public delegate void PlayerApprovalDelegate(PlayerApprovalEvent e);

        public delegate void PlayerMoveDelegate(HumanController hc, Vector3 origin, int encoded, ushort stateFlags,
            uLink.NetworkMessageInfo info, Util.PlayerActions action);

        public delegate void ResearchDelegate(ResearchEvent re);

        public delegate void ServerSavedDelegate(int Amount, double Seconds);

        public delegate void ItemPickupDelegate(ItemPickupEvent itemPickupEvent);

        public delegate void FallDamageDelegate(FallDamageEvent fallDamageEvent);

        public delegate void LootEnterDelegate(LootStartEvent lootStartEvent);

        public delegate void ShootEventDelegate(ShootEvent shootEvent);

        public delegate void ShotgunShootEventDelegate(ShotgunShootEvent shootEvent);

        public delegate void BowShootEventDelegate(BowShootEvent bowshootEvent);

        public delegate void GrenadeThrowEventDelegate(GrenadeThrowEvent grenadeThrowEvent);

        public delegate void BanEventDelegate(BanEvent banEvent);

        public delegate void RepairBenchEventDelegate(RepairEvent repairEvent);

        public delegate void ItemMoveEventDelegate(ItemMoveEvent itemMoveEvent);

        public delegate void GenericSpawnerLoadDelegate(GenericSpawner genericSpawner);

        public delegate void ServerLoadedDelegate();

        public delegate void SupplySignalDelegate(SupplySignalExplosionEvent supplySignalExplosionEvent);

        public delegate void AllPluginsLoadedDelegate();

        public delegate void BeltUseDelegate(BeltUseEvent beltUseEvent);

        public delegate void LoggerDelegate(LoggerEvent loggerEvent);

        //public delegate void AirdropCrateDroppedDelegate(GameObject go);
    }
}