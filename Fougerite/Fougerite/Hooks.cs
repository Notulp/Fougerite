using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fougerite.Caches;
using Fougerite.Events;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;
using Fougerite.Tools;
using Rust;
using uLink;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Fougerite
{
    public partial class Hooks
    {
        public static void AllPluginsLoaded()
        {
            using (new Stopper(nameof(Hooks), nameof(AllPluginsLoaded)))
            {
                try
                {
                    ExecuteSubscribers(OnAllPluginsLoaded, "AllPluginsLoadedEvent");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"AllPluginsLoadedEvent Error: {ex}");
                }
            }
        }

        public static void BlueprintUse(IBlueprintItem item, BlueprintDataBlock bdb)
        {
            using (new Stopper(nameof(Hooks), nameof(BlueprintUse)))
            {
                //Fougerite.Player player = Fougerite.Player.FindByPlayerClient(item.controllable.playerClient);
                Player player = Server.GetServer().FindPlayer(item.controllable.playerClient.userID);
                if (player != null)
                {
                    BPUseEvent ae = new BPUseEvent(bdb, item);
                    if (OnBlueprintUse != null)
                    {
                        try
                        {
                            ExecuteSubscribers(OnBlueprintUse, "BluePrintUseEvent", player, ae);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"BluePrintUseEvent Error: {ex}");
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

                            player.Notice("", $"You can now craft: {bdb.resultItem.name}", 4f);
                        }
                        else
                        {
                            player.Notice("", "You already have this blueprint", 4f);
                        }
                    }
                }
            }
        }

        public static void ChatReceived(ref ConsoleSystem.Arg arg)
        {
            using (new Stopper(nameof(Hooks), nameof(ChatReceived)))
            {
                if (!chat.enabled)
                {
                    return;
                }

                // This must have values
                if (string.IsNullOrEmpty(arg.ArgsStr) || arg.argUser == null)
                {
                    return;
                }

                string quotedName = Facepunch.Utility.String.QuoteSafe(arg.argUser.displayName);
                string quotedMessage = Facepunch.Utility.String.QuoteSafe(arg.GetString(0));
                bool wasCommand = quotedMessage.Trim('"').StartsWith("/");
                Player player = Server.GetServer().FindPlayer(arg.argUser.playerClient.userID);

                if (wasCommand)
                {
                    Logger.LogDebug($"[CHAT-CMD] {quotedName} executed {quotedMessage}");
                    string[] args = Facepunch.Utility.String.SplitQuotesStrings(quotedMessage.Trim('"'));
                    var command = args[0].TrimStart('/');
                    
                    if (command == "fougerite")
                    {
                        player.Message($"[color #00FFFF]This Server is running Fougerite V[color yellow]{Bootstrap.Version}");
                        player.Message("[color green]Fougerite Team: www.fougerite.com");
                        player.Message("[color #0C86AE]Pluton Team: www.pluton-team.org");
                    }
                    
                    // If player has *, restrict all commands.
                    if (player.CommandCancelList.Contains("*", StringComparer.OrdinalIgnoreCase) || player.CommandCancelList.Contains(command, StringComparer.OrdinalIgnoreCase))
                    {
                        player.Message($"You cannot execute {command} at the moment!");
                        return;
                    }
                    
                    // Execute Raw Event after restriction check
                    if (OnChatRaw != null)
                    {
                        try
                        {
                            OnChatRaw(ref arg);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ChatRawEvent Error: {ex}");
                        }
                    }

                    string[] cargs = new string[args.Length - 1];
                    Array.Copy(args, 1, cargs, 0, cargs.Length);
                    if (OnCommand != null)
                    {
                        try
                        {
                            ExecuteSubscribers(OnCommand, "CommandEvent", player, command, cargs);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"CommandEvent Error: {ex}");
                        }
                    }
                }
                else
                {
                    // Execute raw event first
                    if (OnChatRaw != null)
                    {
                        try
                        {
                            OnChatRaw(ref arg);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ChatRawEvent Error: {ex}");
                        }
                    }
                    
                    Logger.ChatLog(quotedName, quotedMessage);
                    ChatString chatstr = new ChatString(quotedMessage);
                    try
                    {
                        if (OnChat != null)
                        {
                            OnChat(player, ref chatstr);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"ChatEvent Error: {ex}");
                    }

                    // Check for empty text
                    if (string.IsNullOrEmpty(chatstr.NewText) || chatstr.NewText.IsNullOrWhiteSpace())
                    {
                        return;
                    }

                    string newchat = Facepunch.Utility.String
                        .QuoteSafe(chatstr.NewText.Substring(1, chatstr.NewText.Length - 2))
                        .Replace("\\\"", "\"");

                    // Check for empty text again
                    if (string.IsNullOrEmpty(newchat) || newchat.IsNullOrWhiteSpace())
                    {
                        return;
                    }

                    string s = Regex.Replace(newchat, @"\[/?color\b.*?\]", string.Empty);
                    if (s.Length <= 100)
                    {
                        AddChatToHistory(player.UID, quotedName, chatstr.NewText);
                        ConsoleNetworker.Broadcast($"chat.add {quotedName} {newchat}");
                        return;
                    }

                    string[] ns = Util.GetUtil().SplitInParts(newchat, 100).ToArray();
                    string[] arr = Regex.Matches(newchat, @"\[/?color\b.*?\]")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray();
                    int i = 0;
                    if (arr.Length == 0)
                    {
                        arr = new[] { "" };
                    }

                    foreach (string x in ns)
                    {
                        AddChatToHistory(player.UID, quotedName, x);

                        ConsoleNetworker.Broadcast(i == 1
                            ? $"chat.add {quotedName} \"{arr[arr.Length - 1]}{x}"
                            : $"chat.add {quotedName} {x}\"");

                        i++;
                    }
                }
            }
        }
        
        /// <summary>
        /// Helper to handle thread-safe history additions and enforce a strict 2000 entry boundary limit.
        /// Clamps sizes down to 1000 entries immediately upon breach.
        /// </summary>
        private static void AddChatToHistory(ulong steamId, string quotedName, string message)
        {
            var dataInstance = Data.GetData();
            var utilInstance = Util.GetUtil();
          
#pragma warning disable CS0618 // Type or member is obsolete
            dataInstance.chat_history.Add(message);
            dataInstance.chat_history_username.Add(quotedName);
#pragma warning restore CS0618 // Type or member is obsolete

            utilInstance.ChatHistory.Add(new ChatEntry
            {
                SteamID = steamId,
                Username = quotedName,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
            
#pragma warning disable CS0618 // Type or member is obsolete
            if (dataInstance.chat_history.Count > 2000)
            {
                int removeCount = dataInstance.chat_history.Count - 1000;
                if (removeCount > 0)
                {
                    dataInstance.chat_history.RemoveRange(0, removeCount);
                }
            }

            if (dataInstance.chat_history_username.Count > 2000)
            {
                int removeCount = dataInstance.chat_history_username.Count - 1000;
                if (removeCount > 0)
                {
                    dataInstance.chat_history_username.RemoveRange(0, removeCount);
                }
            }
#pragma warning restore CS0618 // Type or member is obsolete
            
            if (utilInstance.ChatHistory.Count > 2000)
            {
                int overflowCount = utilInstance.ChatHistory.Count - 1000;
                for (int i = 0; i < overflowCount; i++)
                {
                    utilInstance.ChatHistory.RemoveAt(0);
                }
            }
        }

        public static bool HandleRunCommand(ref ConsoleSystem.Arg arg, bool bWantReply = true)
        {
            using (new Stopper(nameof(Hooks), nameof(HandleRunCommand)))
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
                        Logger.LogError($"HandleCommand Error: {ex}");
                    }
                    // Ignore, should never happen.
                }

                //bool flag;
                Type[] typeArray = ConsoleSystem.FindTypes(arg.Class);
                if (typeArray.Length == 0)
                {
                    return false;
                }

                if (bWantReply && !Bootstrap.SilentConsoleCommands)
                {
                    string[] textArray1 = { "command ", arg.Class, ".", arg.Function, " was executed" };
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
                            arg.ReplyWith($"Command not found: {arg.Class}.{arg.Function}");
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
                                arg.ReplyWith($"No permission: {arg.Class}.{arg.Function}");
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
                            string[] textArray2 = { "Error: ", arg.Class, ".", arg.Function, " - ", exception.Message };
                            Debug.LogWarning(string.Concat(textArray2));
                            string[] textArray3 = { "Error: ", arg.Class, ".", arg.Function, " - ", exception.Message };
                            arg.ReplyWith(string.Concat(textArray3));
                            //flag = false;
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
                                arg.ReplyWith($"No permission: {arg.Class}.{arg.Function}");
                            }

                            return false;
                        }

                        Type fieldType = field.FieldType;
                        if (!arg.HasArgs(1))
                        {
                            if (bWantReply)
                            {
                                string[] textArray5 = {
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
                                    string[] textArray4 =
                                    {
                                        arg.Class, ".", arg.Function, ": changed ", Facepunch.Utility.String.QuoteSafe(str),
                                        " to ", Facepunch.Utility.String.QuoteSafe(field.GetValue(null).ToString()),
                                        " (", fieldType.Name, ")"
                                    };
                                    arg.ReplyWith(string.Concat(textArray4));
                                }
                            }
                            catch (Exception)
                            {
                                if (bWantReply)
                                {
                                    arg.ReplyWith($"error setting value: {arg.Class}.{arg.Function}");
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
                                arg.ReplyWith($"No permission: {arg.Class}.{arg.Function}");
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
                                    string[] textArray6 =
                                    {
                                        arg.Class, ".", arg.Function, ": changed ", Facepunch.Utility.String.QuoteSafe(str),
                                        " to ", Facepunch.Utility.String.QuoteSafe(property.GetValue(null, null).ToString()),
                                        " (", propertyType.Name, ")"
                                    };
                                    
                                    arg.ReplyWith(string.Concat(textArray6));
                                }
                            }
                            catch (Exception)
                            {
                                if (bWantReply)
                                {
                                    arg.ReplyWith($"error setting value: {arg.Class}.{arg.Function}");
                                }
                            }
                        }

                        return true;
                    }

                    index++;
                }

                return false;
            }
        }

        public static bool ConsoleReceived(ref ConsoleSystem.Arg a)
        {
            using (new Stopper(nameof(Hooks), nameof(ConsoleReceived)))
            {
                StringComparison ic = StringComparison.InvariantCultureIgnoreCase;
                bool external = a.argUser == null;
                bool adminRights = (a.argUser != null && (a.argUser.admin || PermissionSystem.GetPermissionSystem().PlayerHasPermission(a.argUser.userID, "RCON"))) || external;
                string Class = a.Class;
                string Function = a.Function;
                
                // Player chat commands are chat.say, console is global.say
                // Let the chat event handle that... (Idk why legacy uses InvariantCultureIgnoreCase)
                if (Class.Equals("chat", ic) && Function.Equals("say", ic))
                {
                    return true;
                }

                ulong UID = 0;
                if (a.argUser != null)
                {
                    UID = a.argUser.userID;
                }

                string userid = "[external][external]";
                if (adminRights && !external)
                    userid = $"[{a.argUser.displayName}][{UID.ToString()}]";

                string logmsg =
                    $"[ConsoleReceived] userid={userid} adminRights={adminRights.ToString()} command={Class}.{Function} args={(a.HasArgs(1) ? a.ArgsStr : "none")}";
                Logger.LogDebug(logmsg);

                string clss = Class.ToLower();
                string func = Function.ToLower();
                string data;
                if (!string.IsNullOrEmpty(func))
                {
                    data = $"{clss}.{func}";
                }
                else
                {
                    data = clss;
                }

                // Allow server console to execute anything
                if (!external && (Server.GetServer().ConsoleCommandCancelList.Contains(data, StringComparer.OrdinalIgnoreCase)
                    || Server.GetServer().ConsoleCommandCancelList.Contains("*", StringComparer.OrdinalIgnoreCase)))
                {
                    a.ReplyWith("This console command is globally restricted!");
                    return false;
                }

                // We have a player
                if (UID > 0)
                {
                    Player player = Server.GetServer().FindPlayer(UID);
                    if (player != null && (player.ConsoleCommandCancelList.Contains(data, StringComparer.OrdinalIgnoreCase)
                        || player.ConsoleCommandCancelList.Contains("*", StringComparer.OrdinalIgnoreCase)))
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
                        Logger.LogError($"OnConsoleReceivedWithCancel Error: {ex}");
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
                        Logger.LogError($"ConsoleReceived Error: {ex}");
                    }
                }

                if (Class.Equals("fougerite", ic) && Function.Equals("reload", ic))
                {
                    if (adminRights)
                    {
                        if (a.HasArgs(1))
                        {
                            string plugin = a.ArgsStr;
                            bool found = false;
                            foreach (string x in PluginLoader.GetInstance().Plugins.Keys)
                            {
                                if (string.Equals(x, plugin, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    PluginLoader.GetInstance().ReloadPlugin(x);
                                    a.ReplyWith($"Fougerite: Plugin {x} reloaded!");
                                    break;
                                }
                            }
                            
                            if (!found)
                            {
                                a.ReplyWith($"Fougerite: {plugin} not found!");
                            }
                        }
                        else
                        {
                            PluginLoader.GetInstance().ReloadPlugins();
                            a.ReplyWith("Fougerite: Reloaded!");
                        }
                    }
                }
                else if (Class.Equals("fougerite", ic) && Function.Equals("load", ic))
                {
                    if (adminRights)
                    {
                        if (a.HasArgs(1))
                        {
                            string plugin = a.ArgsStr;
                            bool alreadyLoaded = false;
                            foreach (string x in PluginLoader.GetInstance().Plugins.Keys)
                            {
                                if (string.Equals(x, plugin, StringComparison.OrdinalIgnoreCase) && PluginLoader.GetInstance().Plugins[x].State == PluginState.Loaded)
                                {
                                    alreadyLoaded = true;
                                    a.ReplyWith($"Fougerite: {x} is already loaded!");
                                    break;
                                }
                            }

                            if (!alreadyLoaded)
                            {
                                PluginLoader.GetInstance().LoadPlugin(plugin, true);
                                a.ReplyWith($"Fougerite: Loaded plugin {plugin}!");
                            }
                        }
                        else
                        {
                            a.ReplyWith("Fougerite: Please specify a plugin name to load.");
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
                            bool found = false;
                            foreach (string x in PluginLoader.GetInstance().Plugins.Keys)
                            {
                                if (string.Equals(x, plugin, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (PluginLoader.GetInstance().Plugins[x].State == PluginState.Loaded)
                                    {
                                        found = true;
                                        PluginLoader.GetInstance().UnloadPlugin(x);
                                        a.ReplyWith($"Fougerite: UnLoaded {x}!");
                                    }

                                    break;
                                }
                            }

                            if (!found)
                            {
                                a.ReplyWith($"Fougerite: {plugin} is already unloaded!");
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
                            a.ReplyWith(
                                $"Fougerite: {ServerSaveHandler.CrucialSavePoint} minutes before autosave. Please wait for It to finish.");
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
                            a.ReplyWith(
                                $"Fougerite: {ServerSaveHandler.CrucialSavePoint} minutes before autosave. Please wait for It to finish.");
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
                        a.ReplyWith($"Toggled rpctracer to:{Logger.showRPC}");
                    }
                }

                if (string.IsNullOrEmpty(a.Reply) && !Bootstrap.SilentConsoleCommands)
                {
                    a.ReplyWith($"Fougerite: {Class}.{Function} was executed!");
                }


                return true;
            }
        }

        public static bool CheckOwner(DeployableObject obj, Controllable controllable)
        {
            using (new Stopper(nameof(Hooks), nameof(CheckOwner)))
            {
                DoorEvent de = new DoorEvent(EntityCache.GetInstance().GrabOrAllocate(obj.GetInstanceID(), obj));
                // Possibly was used for sleeping bag stuff, and they refer to CheckOwner
                // Also for the Doors of course
                if (obj.ownerID == controllable.playerClient.userID)
                {
                    de.Open = true;
                }

                BasicDoor basicDoor = obj.GetComponent<BasicDoor>();
                if (basicDoor != null && OnDoorUse != null)
                {
                    de.State = (BasicDoor.State) basicDoor.state;
                    de.BasicDoor = basicDoor;
                    
                    try
                    {
                        ExecuteSubscribers(OnDoorUse, "DoorUseEvent", Server.GetServer().FindPlayer(controllable.playerClient.userID), de);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"DoorUseEvent Error: {ex}");
                    }
                }

                return de.Open;
            }
        }

        public static float EntityDecay(object entity, float dmg)
        {
            using (new Stopper(nameof(Hooks), nameof(EntityDecay)))
            {
                if (entity == null)
                    return 0f;

                try
                {
                    int instanceId = 0;
                    if (entity is DeployableObject deployableObject)
                        instanceId = deployableObject.GetInstanceID();
                    else if (entity is StructureComponent structureComponent)
                        instanceId = structureComponent.GetInstanceID();
                    else if (entity is StructureMaster structureMaster)
                        instanceId = structureMaster.GetInstanceID();
                    // Leaving these ifs for some weird plugin supports i guess
                    else if (entity is LootableObject lootableObject)
                        instanceId = lootableObject.GetInstanceID();
                    else if (entity is ResourceTarget resourceTarget)
                        instanceId = resourceTarget.GetInstanceID();
                    else if (entity is SupplyCrate supplyCrate)
                        instanceId = supplyCrate.GetInstanceID();

                    // Grab our already created entity class
                    Entity ent = EntityCache.GetInstance().GrabOrAllocate(instanceId, entity);
                    DecayEvent de = new DecayEvent(ent, ref dmg);
                    try
                    {
                        ExecuteSubscribers(OnEntityDecay, "EntityDecayEvent", de);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"EntityDecayEvent Error: {ex}");
                    }

                    DecayList[ent.InstanceID] = ent;
                    return de.DamageAmount;
                }
                catch
                {
                    // Ignore? Was left here from magma
                }

                return 0f;
            }
        }

        public static void EntityDeployed(object entity, ref uLink.NetworkMessageInfo info)
        {
            using (new Stopper(nameof(Hooks), nameof(EntityDeployed)))
            {
                int instanceId = 0;
                if (entity is DeployableObject deployableObject)
                    instanceId = deployableObject.GetInstanceID();
                else if (entity is StructureComponent structureComponent)
                    instanceId = structureComponent.GetInstanceID();
                else if (entity is StructureMaster structureMaster)
                    instanceId = structureMaster.GetInstanceID();
                // Leaving these ifs for some weird plugin supports i guess
                else if (entity is LootableObject lootableObject)
                    instanceId = lootableObject.GetInstanceID();
                else if (entity is ResourceTarget resourceTarget)
                    instanceId = resourceTarget.GetInstanceID();
                else if (entity is SupplyCrate supplyCrate)
                    instanceId = supplyCrate.GetInstanceID();

                // Grab our already created entity class
                Entity e = EntityCache.GetInstance().GrabOrAllocate(instanceId, entity);
                // Freshly created object will not assign the ownerids yet, as the NGC.Instantiate hook is called earlier than
                // Rust's SetupCharacter, SetupCreator functions..
                e.InitiateFix();
                
                uLink.NetworkPlayer nplayer = info.sender;
                Player creator = e.Creator;
                object data = nplayer.GetLocalData();
                Player ActualPlacer = null;
                if (data is NetUser user)
                {
                    ActualPlacer = Server.GetServer().FindPlayer(user.userID);
                }

                try
                {
                    ExecuteSubscribers(OnEntityDeployed, "EntityDeployedEvent", creator, e);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"EntityDeployedEvent Error: {ex}");
                }

                try
                {
                    ExecuteSubscribers(OnEntityDeployedWithPlacer, "EntityDeployedWithPlacerEvent", creator, e, ActualPlacer);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"EntityDeployedWithPlacerEvent Error: {ex}");
                }

            }
        }

        public static void EntityHurt2(TakeDamage tkd, ref DamageEvent e)
        {
            using (new Stopper(nameof(Hooks), nameof(EntityHurt2)))
            {
                HurtEvent he = new HurtEvent(ref e);
                he.DamageAmount = e.amount;
                if (he.VictimIsPlayer)
                {
                    Player vp = (Player)he.Victim;
                    try
                    {
                        ExecuteSubscribers(OnPlayerHurt, "PlayerHurtEvent", he);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"PlayerHurtEvent Error: {ex}");
                    }

                    if (vp.Health - he.DamageAmount > 0 && e.status == LifeStatus.WasKilled)
                    {
                        e.status = LifeStatus.IsAlive;
                    }

                    switch (e.status)
                    {
                        case LifeStatus.IsAlive:
                        {
                            e.amount = he.DamageAmount;
                            tkd._health -= he.DamageAmount;
                            break;
                        }
                        case LifeStatus.WasKilled:
                        {
                            tkd._health = 0f;
                            break;
                        }
                    }
                }
                else if (he.VictimIsSleeper)
                {
                    Sleeper vp = (Sleeper)he.Victim;
                    try
                    {
                        ExecuteSubscribers(OnPlayerHurt, "PlayerHurtEvent", he);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"PlayerHurtEvent (Sleeper) Error: {ex}");
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
                            ExecuteSubscribers(OnNPCHurt, "NPCHurtEvent", he);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"NPCHurtEvent Error: {ex}");
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
                                    ExecuteSubscribers(OnNPCKilled, "NPCKilledEvent", de);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError($"NPCKilledEvent Error: {ex}");
                                }

                                tkd._health = 0f;
                                break;
                            }
                        }
                    }
                }
                else if (he.VictimIsEntity)
                {
                    Entity ent = he.Entity;
                    // Double validate this weird logic...
                    if (!he.IsDecay && DecayList.ContainsKey(he.Entity.InstanceID))
                        he.IsDecay = true;

                    try
                    {
                        ExecuteSubscribers(OnEntityHurt, "EntityHurtEvent", he);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"EntityHurtEvent Error: {ex}");
                    }

                    // This should have been before the event so plugins could still modify the damage
                    // However you can still set the entity's health to your damage if its a ceiling, etc...
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
                            {
                                if (!ent.IsDestroyed)
                                {
                                    tkd._health -= he.DamageAmount;
                                }

                                break;
                            }
                            case LifeStatus.WasKilled:
                            {
                                DestroyEvent de2 = new DestroyEvent(ref e, ent, he.IsDecay);

                                try
                                {
                                    ExecuteSubscribers(OnEntityDestroyed, "EntityDestroyEvent", de2);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError($"EntityDestroyEvent Error: {ex}");
                                }
                                
                                if (DecayList.ContainsKey(ent.InstanceID))
                                {
                                    DecayList.TryRemove(ent.InstanceID);
                                }

                                if (!ent.IsDestroyed)
                                {
                                    tkd._health = 0f;
                                }

                                break;
                            }
                            case LifeStatus.IsDead:
                            {
                                DestroyEvent de22 = new DestroyEvent(ref e, ent, he.IsDecay);
                                try
                                {
                                    ExecuteSubscribers(OnEntityDestroyed, "EntityDestroyEvent", de22);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError($"EntityDestroyEvent Error: {ex}");
                                }
                                
                                if (DecayList.ContainsKey(ent.InstanceID))
                                {
                                    DecayList.TryRemove(ent.InstanceID);
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
                }
            }
        }
        
        public static void ShowTalker(PlayerClient p, PlayerClient p2)
        {
            using (new Stopper(nameof(Hooks), nameof(ShowTalker)))
            {
                Player pl = Server.GetServer().FindPlayer(p2.userID);
                try
                {
                    ExecuteSubscribers(OnShowTalker, "ShowTalkerEvent", p.netPlayer, pl);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ShowTalkerEvent Error: {ex}");
                }
            }
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
        
        public static ItemDataBlock[] ItemsLoaded(List<ItemDataBlock> items,
            Dictionary<string, int> stringDB, Dictionary<int, int> idDB)
        {
            using (new Stopper(nameof(Hooks), nameof(ItemsLoaded)))
            {
                ItemsBlocks blocks = new ItemsBlocks(items);
                try
                {
                    ExecuteSubscribers(OnItemsLoaded, "DataBlockLoadEvent", blocks);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"DataBlockLoadEvent Error: {ex}");
                }

                int num = 0;
                foreach (ItemDataBlock block in blocks)
                {
                    stringDB.Add(block.name, num);
                    idDB.Add(block.uniqueID, num);
                    num++;
                }

                Server.GetServer().Items = blocks;
                return blocks.ToArray();
            }
        }

        public static bool ItemPickup(ItemPickup pickup, Controllable controllable)
        {
            using (new Stopper(nameof(Hooks), nameof(ItemPickup)))
            {

                IInventoryItem item;
                Inventory local = controllable.GetLocal<Inventory>();
                if (local == null)
                {
                    return false;
                }

                Inventory inventory2 = pickup.GetLocal<Inventory>();
                if ((inventory2 == null) || ReferenceEquals(item = inventory2.firstItem, null))
                {
                    pickup.RemoveThis();
                    return false;
                }

                ItemPickupEvent ipe = new ItemPickupEvent(controllable, item, local,
                    Inventory.AddExistingItemResult.BadItemArgument, PickupEventType.Before);
                try
                {
                    ExecuteSubscribers(OnItemPickup, "ItemPickupEvent", ipe);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ItemPickupEvent Error: {ex}");
                }

                if (ipe.Cancelled)
                {
                    return false;
                }

                Inventory.AddExistingItemResult result = local.AddExistingItem(item, false);
                ItemPickupEvent aftercall =
                    new ItemPickupEvent(controllable, item, local, result, PickupEventType.After);
                switch (result)
                {
                    case Inventory.AddExistingItemResult.CompletlyStacked:
                    {
                        inventory2.RemoveItem(item);
                        break;
                    }
                    case Inventory.AddExistingItemResult.Moved:
                        break;
                    case Inventory.AddExistingItemResult.PartiallyStacked:
                    {
                        pickup.UpdateItemInfo(item);
                        try
                        {
                            ExecuteSubscribers(OnItemPickup, "ItemPickupEvent", aftercall);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ItemPickupEvent Error: {ex}");
                        }

                        return true;
                    }
                    case Inventory.AddExistingItemResult.Failed:
                    {
                        try
                        {
                            ExecuteSubscribers(OnItemPickup, "ItemPickupEvent", aftercall);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ItemPickupEvent Error: {ex}");
                        }

                        return false;
                    }
                    case Inventory.AddExistingItemResult.BadItemArgument:
                    {
                        pickup.RemoveThis();
                        try
                        {
                            ExecuteSubscribers(OnItemPickup, "ItemPickupEvent", aftercall);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ItemPickupEvent Error: {ex}");
                        }

                        return false;
                    }
                    default:
                        throw new NotImplementedException();
                }

                pickup.RemoveThis();
                try
                {
                    ExecuteSubscribers(OnItemPickup, "ItemPickupEvent", aftercall);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ItemPickupEvent Error: {ex}");
                }

                return true;
            }
        }

        public static void FallDamage(FallDamage fd, float speed)
        {
            using (new Stopper(nameof(Hooks), nameof(FallDamage)))
            {
                float num = (speed - falldamage.min_vel) / (falldamage.max_vel - falldamage.min_vel);
                bool flag = num > 0.25f;
                bool flag2 = num > 0.35f || UnityEngine.Random.Range(0, 3) == 0 || fd.healthFraction < 0.5f;
                
                FallDamageEvent fde = new FallDamageEvent(fd, speed, num, flag, flag2);
                try
                {
                    ExecuteSubscribers(OnFallDamage, "FallDamageEvent", fde);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"FallDamageEvent Error: {ex}");
                }

                if (!fde.Cancelled)
                {
                    if (fde.Bleeding)
                    {
                        fd.GetComponent<HumanBodyTakeDamage>().AddBleedingLevel(3f + (num - 0.25f) * 10f);
                    }
                    
                    if (fde.BrokenLegs)
                    {
                        fd.AddLegInjury(1f);
                    }
                    
                    TakeDamage.HurtSelf(fd.idMain, 10f + num * fd.maxHealth, null);
                }
            }
        }

        public static void ConnectHandler(NetUser user)
        {
            using (new Stopper(nameof(Hooks), nameof(ConnectHandler)))
            {
                GameEvent.DoPlayerConnected(user.playerClient);
                PlayerConnect(user);
            }
        }

        public static bool PlayerConnect(NetUser user)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerConnect)))
            {
                // Sanity check
                if (user.playerClient == null)
                {
                    Logger.LogDebug("PlayerConnect user.playerClient is null");
                    return false;
                }

                // Grab values into variables
                ulong uid = user.userID;
                string nip = user.networkPlayer.externalIP;
                string nname = user.displayName;

                // This was a check for some attacks and what not where attackers have sent
                // random steamids to the servers causing fake connections.
                // Obviously if this is a real connection we should remove It, although I should have documented this more.
                if (uLinkDCCache.Contains(uid))
                {
                    uLinkDCCache.Remove(uid);
                }

                // Flood check, again same attacking pattern.
                if (FloodCooldown.ContainsKey(nip))
                {
                    Server.GetServer().BanPlayerIP(nip, nname, "FloodCooldown", "Fougerite");
                    return false;
                }

                Server srv = Server.GetServer();

                // Create our API player class
                Player player = new Player(user.playerClient);

                // Does the player have RCON or * permissions?
                if (PermissionSystem.GetPermissionSystem().PlayerHasPermission(player.UID, "RCON"))
                {
                    // Force the user to an RCON admin.
                    player.PlayerClient.netUser.admin = true;
                }

                // Add It to the consistent cache list
                srv.AddCachePlayer(uid, player);

                CachedPlayer cachedPlayer;
                if (!PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(uid, out cachedPlayer))
                {
                    cachedPlayer = new CachedPlayer
                    {
                        Name = player.Name,
                        IPAddresses = new List<string>() { player.IP },
                        Aliases = new List<string>() { player.Name }
                    };
                    PlayerCache.GetPlayerCache().CachedPlayers[uid] = cachedPlayer;
                }
                else
                {
                    cachedPlayer.Name = player.Name;
                    cachedPlayer.LastLogin = DateTime.Now;
                    // Sanity check, shouldn't happen unless user messes with file.
                    if (cachedPlayer.Aliases == null)
                    {
                        cachedPlayer.Aliases = new List<string>();
                    }

                    // Sanity check, shouldn't happen unless user messes with file.
                    if (cachedPlayer.IPAddresses == null)
                    {
                        cachedPlayer.IPAddresses = new List<string>();
                    }

                    // Check if this name is in the aliases
                    if (!cachedPlayer.Aliases.Contains(player.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        cachedPlayer.Aliases.Add(player.Name);
                    }

                    // Check if IP is in the list
                    if (!cachedPlayer.IPAddresses.Contains(player.IP))
                    {
                        cachedPlayer.IPAddresses.Add(player.IP);
                    }
                }

                // This in theory should never happen as two same ID connections would be disconnected on
                // the steam auth event, but I must have put this check here for a good reason.
                if (srv.ContainsPlayer(uid))
                {
                    Logger.LogError($"[PlayerConnect] Server.Players already contains {player.Name} {player.SteamID}");
                    return user.connected;
                }

                // Throw player into the current list as well.
                srv.AddPlayer(uid, player);

                try
                {
                    ExecuteSubscribers(OnPlayerConnected, "PlayerConnectedEvent", player);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerConnectedEvent Error {ex}");
                }

                bool connected = user.connected;

                if (Config.GetBoolValue("Fougerite", "tellversion"))
                {
                    player.Message($"This server is powered by Fougerite v.{Bootstrap.Version}!");
                }

                Logger.LogDebug($"User Connected: {player.Name} ({player.SteamID}) ({player.IP})");

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

                return connected;
            }
        }

        public static void PlayerDisconnect(uLink.NetworkPlayer nplayer)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerDisconnect)))
            {
                NetUser user = nplayer.GetLocalData() as NetUser;
                if (user == null)
                {
                    return;
                }

                ulong uid = user.userID;
                string name = user.displayName;
                
                Player player = Server.GetServer().GetCachePlayer(uid);
                if (player == null)
                {
                    Server.GetServer().RemovePlayer(uid);
                    Logger.LogWarning(
                        $"[WeirdDisconnect] Player was null at the disconnection ({uid} - {name}). Something might be wrong? OPT: {Bootstrap.CR}");
                    return;
                }

                player.DisconnectTime = DateTime.UtcNow.Ticks;
                player.IsDisconnecting = true;

                CachedPlayer cachedPlayer;
                if (PlayerCache.GetPlayerCache().CachedPlayers.TryGetValue(uid, out cachedPlayer))
                {
                    cachedPlayer.LastLogout = DateTime.Now;
                }

                // Remove the player from the current players
                Server.GetServer().RemovePlayer(uid);

                try
                {
                    ExecuteSubscribers(OnPlayerDisconnected, "PlayerDisconnectedEvent", player);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerDisconnectedEvent Error {ex}");
                }

                Logger.LogDebug($"User Disconnected: {player.Name} ({player.SteamID}) ({player.IP})");
                if (Bootstrap.CR)
                {
                    Server.GetServer().RemoveCachePlayer(uid);
                }
            }
        }

        public static void PlayerGather(Inventory rec, ResourceTarget rt, ResourceGivePair rg, ref int amount)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerGather)))
            {
                Player player = Player.FindByNetworkPlayer(rec.networkView.owner);
                GatherEvent ge = new GatherEvent(rt, rg, amount);
                try
                {
                    ExecuteSubscribers(OnPlayerGathering, "PlayerGatherEvent", player, ge);

                    amount = ge.Quantity;
                    if (!ge.Override)
                    {
                        amount = Mathf.Min(amount, rg.AmountLeft());
                    }

                    ItemDataBlock item = Server.GetServer().Items.Find(ge.Item);
                    if (item != null)
                        rg.ResourceItemName = item.name;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerGatherEvent Error: {ex}");
                }
            }
        }
        
        public static void PlayerGatherWood(MeleeWeaponDataBlock weaponDataBlock, uLink.BitStream stream,
            ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerGatherWood)))
            {
                NetCull.VerifyRPC(ref info, false);
                GameObject gameObject;
                NetEntityID netEntityID;
                if (stream.ReadBoolean())
                {
                    netEntityID = stream.Read<NetEntityID>(new object[0]);
                    if (!netEntityID.isUnassigned)
                    {
                        gameObject = netEntityID.gameObject;
                        if (!gameObject)
                        {
                            netEntityID = NetEntityID.unassigned;
                        }
                    }
                    else
                    {
                        gameObject = null;
                    }
                }
                else
                {
                    netEntityID = NetEntityID.unassigned;
                    gameObject = null;
                }

                Vector3 hitPos = stream.ReadVector3();
                bool flag = stream.ReadBoolean();
                IMeleeWeaponItem meleeWeaponItem;
                if (!rep.Item<global::IMeleeWeaponItem>(out meleeWeaponItem))
                {
                    return;
                }

                TakeDamage local = meleeWeaponItem.inventory.GetLocal<TakeDamage>();
                if (local && local.dead)
                {
                    return;
                }

                if (!meleeWeaponItem.ValidatePrimaryMessageTime(info.timestamp))
                {
                    return;
                }

                IDBase idbase = gameObject ? IDBase.Get(gameObject) : null;
                TakeDamage takeDamage = idbase ? idbase.idMain.GetLocal<TakeDamage>() : null;

                if (gameObject)
                {
                    float num = Vector3.Distance(local.transform.position, gameObject.transform.position);
                    if (num >= 6f)
                    {
                        return;
                    }
                }

                Metabolism component = meleeWeaponItem.inventory.GetComponent<Metabolism>();
                if (component)
                {
                    component.SubtractCalories(global::UnityEngine.Random.Range(weaponDataBlock.caloriesPerSwing * 0.8f,
                        weaponDataBlock.caloriesPerSwing * 1.2f));
                }

                rep.ActionStream(1, global::uLink.RPCMode.AllExceptOwner, stream);
                ResourceTarget resourceTarget = ((!(idbase == null) || !(gameObject == null))
                    ? ((!(idbase == null)) ? idbase.gameObject : gameObject).GetComponent<ResourceTarget>()
                    : null);

                if (flag || (resourceTarget && (takeDamage == null || takeDamage.dead)))
                {
                    if (flag)
                    {
                        Collider[] hitColliders = Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(hitPos, 4.5f);
                        GameObject treeGameObject = null; 
                        foreach (var col in hitColliders)
                        {
                            if (col != null && col.tag == "Tree Collider")
                            {
                                treeGameObject = col.gameObject;
                                break;
                            }
                        }
                        
                        // Player sent a position for farming a tree, but there is no tree collider
                        // Could be a potential tree farming cheat
                        if (treeGameObject == null) 
                            return;
                        
                        Player player = Player.FindByNetworkPlayer(info.sender);
                        // Sanity check
                        if (player == null) 
                            return;
                        
                        // Player is too far from the position, potential tree farming cheat
                        if (Vector3.Distance(player.Location, hitPos) >= 6f) 
                            return;
                        
                        WoodBlockerTemp wbt = WoodBlockerTemp.GetBlockerForPoint(hitPos);
                        if (wbt != null && wbt.HasWood())
                        {
                            float efficiency =
                                weaponDataBlock.efficiencies[(int)ResourceTarget.ResourceTargetType.StaticTree];
                            weaponDataBlock.resourceGatherLevel += efficiency;

                            if (weaponDataBlock.resourceGatherLevel >= 1f)
                            {
                                int qty = Mathf.FloorToInt(weaponDataBlock.resourceGatherLevel);
                                string itemName = "Wood";
                                ItemDataBlock db = DatablockDictionary.GetByName(itemName);

                                GatherEvent ge = new GatherEvent(resourceTarget, db, qty, wbt, treeGameObject);

                                ExecuteSubscribers(OnPlayerGathering, "PlayerGatherWoodEvent",
                                    Player.FindByNetworkPlayer(info.sender), ge);

                                ItemDataBlock item = Server.GetServer().Items.Find(ge.Item);
                                if (item != null)
                                    db = item;
                                qty = ge.Quantity;

                                int numAdded = meleeWeaponItem.inventory.AddItemAmount(db, qty);
                                int numGiven = qty - numAdded;

                                if (numGiven > 0)
                                {
                                    weaponDataBlock.resourceGatherLevel -= (float)numGiven;
                                    wbt.ConsumeWood((float)numGiven);
                                    Notice.Inventory(info.sender, numGiven.ToString() + " x " + db.name);
                                }
                            }
                        }
                    }
                    else if (resourceTarget)
                    {
                        resourceTarget.DoGather(meleeWeaponItem.inventory,
                            weaponDataBlock.efficiencies[(int)resourceTarget.type]);
                    }
                }

                if (idbase)
                {
                    float damage = weaponDataBlock.GetDamage();
                    TakeDamage.Hurt(meleeWeaponItem.inventory, idbase, new DamageTypeList(0f, 0f, damage, 0f, 0f, 0f),
                        new WeaponImpact(weaponDataBlock, meleeWeaponItem, rep));
                }

                if (gameObject)
                {
                    meleeWeaponItem.TryConditionLoss(0.25f, 0.025f);
                }
            }
        }

        public static void WoodBlockerTempAwake(WoodBlockerTemp woodBlockerTemp)
        {
            WoodBlockerTemp.TryInitBlockers();
            var maxWood = UnityEngine.Random.Range(10, 15);
            woodBlockerTemp.numWood = maxWood;
            woodBlockerTemp.maxWood = maxWood;
            WoodBlockerTemp._blockers.Add(woodBlockerTemp);
            UnityEngine.Object.Destroy(woodBlockerTemp.gameObject, 300f);
        }

        public static bool PlayerKilled(ref DamageEvent de)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerKilled)))
            {
                bool flag = false;
                try
                {
                    DeathEvent event2 = new DeathEvent(ref de);
                    if (event2.VictimIsPlayer && event2.Victim is Player victim)
                    {
                        victim.justDied = true;
                    }

                    flag = event2.DropItems;
                    ExecuteSubscribers(OnPlayerKilled, "PlayerKilledEvent", event2);

                    flag = event2.DropItems;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerKilledEvent Error: {ex}");
                }

                return flag;
            }
        }

        public static void PlayerSpawned(PlayerClient pc, Vector3 pos, bool camp)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerSpawned)))
            {
                Player player = Server.GetServer().FindPlayer(pc.userID);
                SpawnEvent se = new SpawnEvent(pos, camp);
                try
                {
                    if (player != null)
                        ExecuteSubscribers(OnPlayerSpawned, "PlayerSpawnedEvent", player, se);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerSpawnedEvent Error: {ex}");
                }
            }
        }

        public static Vector3 PlayerSpawning(PlayerClient pc, Vector3 pos, bool camp)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerSpawning)))
            {
                Player player = Server.GetServer().FindPlayer(pc.userID);
                SpawnEvent se = new SpawnEvent(pos, camp);
                try
                {
                    if (player != null)
                        ExecuteSubscribers(OnPlayerSpawning, "PlayerSpawningEvent", player, se);
                    
                    return new Vector3(se.X, se.Y, se.Z);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerSpawningEvent Error: {ex}");
                }

                return pos;
            }
        }

        public static void PluginInit()
        {
            using (new Stopper(nameof(Hooks), nameof(PluginInit)))
            {
                try
                {
                    ExecuteSubscribers(OnPluginInit, "PluginInitEvent");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PluginInitEvent Error: {ex}");
                }
            }
        }

        public static void PlayerTeleport(Player player, Vector3 from, Vector3 dest)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerTeleport)))
            {
                try
                {
                    ExecuteSubscribers(OnPlayerTeleport, "TeleportEvent", player, from, dest);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"TeleportEvent Error: {ex}");
                }
            }
        }

        public static void CraftingEvent(CraftingInventory inv, BlueprintDataBlock blueprint, int amount,
            ulong startTime)
        {
            using (new Stopper(nameof(Hooks), nameof(CraftingEvent)))
            {
                try
                {
                    CraftingEvent e = new CraftingEvent(inv, blueprint, amount, startTime);
                    ExecuteSubscribers(OnCrafting, "CraftingEvent", e);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"CraftingEvent Error: {ex}");
                }
            }
        }
        
        public static void CraftingCancelEvent(CraftingInventory inv)
        {
            using (new Stopper(nameof(Hooks), nameof(CraftingCancelEvent)))
            {
                try
                {
                    if (inv.isCrafting) 
                    {
                        CraftCancelEvent e = new CraftCancelEvent(inv);
                        ExecuteSubscribers(OnCraftCancel, "OnCraftCancel", e);
                        
                        if (e.Cancelled) 
                            return;
                        
                        inv.crafting = default(global::CraftingSession);
                        inv.EndCrafting();
                        inv.UpdateCraftingDataToOwner();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"CraftingCancelEvent Error: {ex}");
                }
            }
        }

        public static void CraftingCompleteEvent(CraftingInventory inv)
        {
            using (new Stopper(nameof(Hooks), nameof(CraftingCompleteEvent)))
            {
                try
                {
                    if (inv.isCrafting)
                    {
                        CraftCompleteEvent e = new CraftCompleteEvent(inv, CraftCompleteEventType.Before);
                        ExecuteSubscribers(OnCraftComplete, "OnCraftComplete", e);

                        try
                        {
                            inv.crafting.blueprint.CompleteWork(inv.crafting.amount, inv);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"{nameof(CraftingCompleteEvent)} Error while completing craft: {ex}");
                        }
                        finally
                        {
                            // Clean up session as per original code
                            inv.crafting = default(global::CraftingSession);
                            inv.EndCrafting();
                            inv.UpdateCraftingDataToOwner();
                        }
                        
                        CraftCompleteEvent e2 = new CraftCompleteEvent(inv, CraftCompleteEventType.After);
                        ExecuteSubscribers(OnCraftComplete, "OnCraftComplete", e2);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"CraftingCompleteEvent Error: {ex}");
                }
            }
        }

        public static void AnimalMovement(BaseAIMovement m, BasicWildLifeAI ai, ulong simMillis)
        {
            using (new Stopper(nameof(Hooks), nameof(AnimalMovement)))
            {
                // Get the NPC from the Character and find It's NPCCache entry
                Character character = ai.GetComponent<Character>();
                NPC npc = null;
                if (character != null)
                {
                    npc = NPCCache.GetInstance().GetEntityByInstanceId(character.GetInstanceID());
                }
                
                // AI movement should be NavMeshMovement
                NavMeshMovement movement = m as NavMeshMovement;
                if (movement == null || !movement)
                {
                    return;
                }

                // We will kill the AI if It has an invalid path
                if (movement._agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    TakeDamage dmg = ai.GetComponent<TakeDamage>();
                    bool IsAlive = dmg != null && dmg.alive;
                    if (IsAlive)
                    {
                        TakeDamage.KillSelf(ai.GetComponent<IDBase>());
                        Logger.LogWarning("[NavMesh] AI destroyed for having invalid path.");
                    }
                }
                else
                {
                    AnimalMovementEvent ev = new AnimalMovementEvent(npc, movement, simMillis);
                    try
                    {
                        ExecuteSubscribers(OnAnimalMovement, "AnimalMovementEvent", ev);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"AnimalMovementEvent Error: {ex}");
                    }
                }
            }
        }

        public static void ResourceSpawned(ResourceTarget target)
        {
            using (new Stopper(nameof(Hooks), nameof(ResourceSpawned)))
            {
                try
                {
                    ExecuteSubscribers(OnResourceSpawned, "ResourceSpawnedEvent", target);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ResourceSpawnedEvent Error: {ex}");
                }
            }
        }

        public static void BowShootEvent(BowWeaponDataBlock db, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info, IBowWeaponItem bwi)
        {
            using (new Stopper(nameof(Hooks), nameof(BowShootEvent)))
            {
                try
                {
                    BowShootEvent se = new BowShootEvent(db, rep, info, bwi);
                    ExecuteSubscribers(OnBowShoot, "BowShootEvent", se);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"BowShootEvent Error: {ex}");
                }
            }
        }

        public static void OnServerSaveEvent(int amount, double seconds)
        {
            using (new Stopper(nameof(Hooks), nameof(OnServerSaveEvent)))
            {
                try
                {
                    ExecuteSubscribers(OnServerSaved, "ServerSavedEvent", amount, seconds);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ServerSavedEvent Error: {ex}");
                }

                // Save the permissions.
                PermissionSystem.GetPermissionSystem().SaveToDisk();

                // Save PlayersCache
                PlayerCache.GetPlayerCache().SaveToDisk();
            }
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
            using (new Stopper(nameof(Hooks), nameof(ItemRemoved)))
            {
                Collection<InventoryItem> collection = inv.collection;
                InventoryItem inventoryItem;
                if (mustMatch && (!collection.Get(slot, out inventoryItem) ||
                                  !ReferenceEquals(inventoryItem, match)) ||
                    !collection.Evict(slot, out inventoryItem))
                {
                    return false;
                }

                InventoryModEvent e = null;
                try
                {
                    e = new InventoryModEvent(inv, slot, inventoryItem.iface, "Remove", inventoryItem.inventory);
                    ExecuteSubscribers(OnItemRemoved, "InventoryRemoveEvent", e);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"InventoryRemoveEvent Error: {ex}");
                }

                if (e != null && e.Cancelled)
                {
                    return false;
                }

                if (inventoryItem == inv._activeItem)
                {
                    inv.DeactivateItem();
                }

                inv.ItemRemoved(slot, inventoryItem.iface);
                inv.MarkSlotDirty(slot);

                return true;
            }
        }

        public static bool ItemAdded(ref Inventory.Payload.Assignment args)
        {
            using (new Stopper(nameof(Hooks), nameof(ItemAdded)))
            {
                InventoryModEvent e = null;
                try
                {
                    e = new InventoryModEvent(args.inventory, args.slot, args.item.iface, "Add", args.item?.inventory);
                    ExecuteSubscribers(OnItemAdded, "InventoryAddEvent", e);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"InventoryAddEvent Error: {ex}");
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

                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public static void Airdrop(Vector3 v)
        {
            using (new Stopper(nameof(Hooks), nameof(Airdrop)))
            {
                try
                {
                    Vector3 vector3_1 = v;
                    float num = 20f * NetCull.LoadPrefab("C130").GetComponent<SupplyDropPlane>().maxSpeed;
                    Vector3 vector3_2 = vector3_1 + SupplyDropZone.RandomDirectionXZ() * num;
                    Vector3 pos = vector3_1 + new Vector3(0.0f, 300f, 0.0f);
                    Vector3 position = vector3_2 + new Vector3(0.0f, 400f, 0.0f);
                    Quaternion rotation = Quaternion.LookRotation((pos - position).normalized);
                    NetCull.InstantiateClassic("C130", position, rotation, 0).GetComponent<SupplyDropPlane>().SetDropTarget(pos);
                    
                    ExecuteSubscribers(OnAirdropCalled, "AirdropEvent", v);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"AirdropEvent Error: {ex}");
                }
            }
        }

        public static void SupplyDropPlaneCreated(SupplyDropPlane plane)
        {
            using (new Stopper(nameof(Hooks), nameof(SupplyDropPlaneCreated)))
            {
                try
                {
                    ExecuteSubscribers(OnSupplyDropPlaneCreated, "SupplyDropPlaneCreated", plane);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"SupplyDropPlaneCreated Error: {ex}");
                }
            }
        }

        public static void AirdropCrateDropped(SupplyDropPlane plane)
        {
            using (new Stopper(nameof(Hooks), nameof(AirdropCrateDropped)))
            {
                Transform transform = plane.transform;
                Vector3 forward = transform.forward;
                Vector3 position = transform.position - (forward * 50f);
                GameObject obj = NetCull.InstantiateClassic(nameof(SupplyCrate), position,
                    Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f)), 0);
                obj.rigidbody.centerOfMass = new Vector3(0f, -1.5f, 0f);
                obj.rigidbody.AddForceAtPosition(-forward * 50f, obj.transform.position - new Vector3(0f, 1f, 0f));

                SupplyCrate supplyCrate = obj.GetComponent<SupplyCrate>();
                Entity entity = EntityCache.GetInstance().GrabOrAllocate(supplyCrate.GetInstanceID(), supplyCrate);

                try
                {
                    ExecuteSubscribers(OnAirdropCrateDropped, "AirdropCrateDroppedEvent", plane, entity);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"AirdropCrateDroppedEvent Error: {ex}");
                }
            }
        }

        public static void SteamDeny(ClientConnection cc, NetworkPlayerApproval approval, string strReason,
            NetError errornum)
        {
            using (new Stopper(nameof(Hooks), nameof(SteamDeny)))
            {
                SteamDenyEvent sde = new SteamDenyEvent(cc, approval, strReason, errornum);
                try
                {
                    ExecuteSubscribers(OnSteamDeny, "SteamDenyEvent", sde);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"SteamDenyEvent Error: {ex}");
                }

                if (sde.ForceAllow)
                {
                    return;
                }

                string deny = $"Auth failed: {strReason} - {cc.UserName} ({cc.UserID})";
                Logger.Log(deny);
                approval.Deny((uLink.NetworkConnectionError)errornum);
                ConnectionAcceptor.CloseConnection(cc);
                Rust.Steam.Server.OnUserLeave(cc.UserID);
            }
        }

        public static void HandleuLinkDisconnect(string msg, object NetworkPlayer)
        {
            using (new Stopper(nameof(Hooks), nameof(HandleuLinkDisconnect)))
            {
                try
                {
                    GameObject[] objArray = (GameObject[]) UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
                    if (NetworkPlayer is uLink.NetworkPlayer np)
                    {
                        object data = np.GetLocalData();
                        if (data is NetUser user)
                        {
                            ulong id = user.userID;
                            PlayerClient client = user.playerClient;
                            Vector3 loc = user.playerClient.lastKnownPosition;

                            Player player = Server.GetServer().GetCachePlayer(id);
                            // Sanity check
                            if (player != null)
                            {
                                player.IsDisconnecting = true;
                                player.DisconnectLocation = loc;
                                player.UpdatePlayerClient(client);
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
                            Logger.LogError($"[uLink Error] Disconnect failure, report to DreTaX: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug($"[uLink Error] Full Exception: {ex}");
                }
            }
        }

        public static void PlayerApproval(ConnectionAcceptor ca, NetworkPlayerApproval approval)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerApproval)))
            {
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

                    Server srv = Server.GetServer();
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
                            Logger.LogWarning($"[Flood Protection] {ip} was removed from the cooldown.");
                            FloodCooldown.Remove(ip);
                        }
                    }

                    if (clientConnection.Protocol != 1069)
                    {
                        Debug.Log($"Denying entry to client with invalid protocol version ({ip})");
                        approval.Deny(uLink.NetworkConnectionError.IncompatibleVersions);
                    }
                    else if (BanList.Contains(uid))
                    {
                        Debug.Log($"Rejecting client ({uid}in banlist)");
                        approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                    }
                    else if (srv.IsBannedID(uid.ToString()) || srv.IsBannedIP(ip))
                    {
                        if (!srv.IsBannedIP(ip))
                        {
                            srv.BanPlayerIP(ip, name, $"IP is not banned-{uid}", "Console");
                            Logger.LogDebug(
                                $"[FougeriteBan] Detected banned ID, but IP is not banned: {name} - {ip} - {uid}");
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
                            srv.BanPlayerID(uid.ToString(), name, $"ID is not banned-{ip}", "Console");
                            Logger.LogDebug(
                                $"[FougeriteBan] Detected banned IP, but ID is not banned: {name} - {ip} - {uid}");
                        }
                        else
                        {
                            if (DataStore.GetInstance().Get("Ids", uid.ToString()).ToString() != name)
                            {
                                DataStore.GetInstance().Add("Ids", uid.ToString(), name);
                            }
                        }

                        Logger.LogWarning($"[FougeriteBan] Disconnected: {name} - {ip} - {uid}");
                        approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                    }
                    else if (ca.IsConnected(uid))
                    {
                        PlayerApprovalEvent ape =
                            new PlayerApprovalEvent(ca, approval, clientConnection, true, uid, ip, name, uLink.NetworkConnectionError.AlreadyConnectedToAnotherServer);
                        try
                        {
                            ExecuteSubscribers(OnPlayerApproval, "PlayerApprovalEvent", ape);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"PlayerApprovalEvent Error: {ex}");
                        }

                        if (ape.ForceAccept)
                        {
                            Player temp = srv.GetCachePlayer(uid);
                            // This type of thing can happen when we approve a steamid that is already on server
                            // such as it can happen on cracked servers
                            if (temp != null && !ape.ServerHasPlayer)
                            {
                                temp.Disconnect();
                            }

                            Accept(ca, approval, clientConnection);
                            return;
                        }

                        Logger.Log($"Denying entry to {uid} because they're already connected");
                        approval.Deny(ape.DenyReason);
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
                            ExecuteSubscribers(OnPlayerApproval, "PlayerApprovalEvent2", ape);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"PlayerApprovalEvent2 Error: {ex}");
                        }

                        if (ape.AboutToDeny)
                        {
                            approval.Deny(ape.DenyReason);
                            return;
                        }

                        Accept(ca, approval, clientConnection);
                    }
                }
            }
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
                Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
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
                    Logger.LogWarning($"[TeleportHack] {player.Name} sent invalid packets. {player.SteamID}");
                    Server.GetServer().Broadcast($"{player.Name} might have tried to teleport with hacks.");
                    if (Bootstrap.BI)
                    {
                        Server.GetServer().BanPlayer(player, "Console", "TeleportHack");
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
                ExecuteSubscribers(OnPlayerMove, "PlayerMoveEvent", hc, origin, encoded, stateFlags, info, action);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerMoveEvent Error: {ex}");
            }
        }

        public static InventoryItem.MergeResult ResearchItem(ResearchToolItem<ToolDataBlock> rti,
            IInventoryItem otherItem)
        {
            using (new Stopper(nameof(Hooks), nameof(ResearchItem)))
            {
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
                
                try
                {
                    ExecuteSubscribers(OnResearch, "ResearchItemEvent", researchEvent);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ResearchItem Error: {ex}");
                }

                if (!researchEvent.Cancelled)
                {
                    inventory.BindBlueprint(block2);
                    Notice.Popup(inventory.networkView.owner, "?", $"You can now craft {otherItem.datablock.name}", 4f);
                    int numWant = 1;
                    if (rti.Consume(ref numWant))
                    {
                        rti.inventory.RemoveItem(rti.slot);
                    }
                }
                
                return !researchEvent.Cancelled ? InventoryItem.MergeResult.Combined : InventoryItem.MergeResult.Failed;
            }
        }

        public static void SetLooter(LootableObject lo, uLink.NetworkPlayer ply)
        {
            using (new Stopper(nameof(Hooks), nameof(SetLooter)))
            {
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
                                Logger.LogError($"[SetLooter] Error: {exception}");
                                NetCull.RPC(lo, "StopLooting", uLink.RPCMode.Server);
                                lo.thisClientIsInWindow = false;
                                ply = uLink.NetworkPlayer.unassigned;
                            }
                        }
                    }
                    else if ((lo._currentlyUsingPlayer == NetCull.player) && (NetCull.player != uLink.NetworkPlayer.unassigned))
                    {
                        lo.ClearLooter();
                    }

                    lo._currentlyUsingPlayer = ply;
                }
            }
        }

        public static void OnUseEnter(LootableObject lo, Useable use)
        {
            using (new Stopper(nameof(Hooks), nameof(OnUseEnter)))
            {
                uLink.NetworkPlayer ulinkuser = uLink.NetworkView.Get(use.user).owner;
                lo._useable = use;
                lo._currentlyUsingPlayer = ulinkuser;
                lo._inventory.AddNetListener(lo._currentlyUsingPlayer);
                lo.SendCurrentLooter();
                lo.CancelInvokes();
                lo.InvokeRepeating(nameof(LootableObject.RadialCheck), 0f, 10f);
            }
        }

        public static UseResponse EnterHandler(Useable use, Character attempt, UseEnterRequest request)
        {
            using (new Stopper(nameof(Hooks), nameof(EnterHandler)))
            {
                if (!use.canUse)
                {
                    return UseResponse.Fail_NotIUseable;
                }

                Useable.EnsureServer();
                if (((int)use.callState) != 0)
                {
                    Logger.LogWarning(
                        $"Some how Enter got called from a call stack originating with {use.callState} fix your script to not do this.", use);
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
                                            $"A IUseableChecked return a invalid value that should have cause success [{response}], but it was not UseCheck.Success! fix your script.",
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
                                                    "Caught exception in OnUseDeclined \r\n (response was ", response,
                                                    ")",
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
                                    uLink.NetworkPlayer ulinkuser = uLink.NetworkView.Get(use.user).owner;
                                    NetUser user = ulinkuser.GetLocalData() as NetUser;
                                    LootStartEvent lt = null;
                                    if (user != null)
                                    {
                                        Player pl = Server.GetServer().FindPlayer(user.userID);
                                        if (pl != null)
                                        {
                                            lt = new LootStartEvent(lootableObject, pl, use, ulinkuser);
                                            try
                                            {
                                                ExecuteSubscribers(OnLootUse, "LootStartEvent", lt);
                                            }
                                            catch (Exception ex2)
                                            {
                                                Logger.LogError($"LootStartEvent Error: {ex2}");
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
                                    Logger.LogError($"LootStartEvent Outer Error: {ex3}");
                                }

                                OnUseEnter(lootableObject, use);
                                //use.use.OnUseEnter(use);
                            }
                            catch (Exception exception3)
                            {
                                use._user = null;
                                Logger.LogError(
                                    $"Exception thrown during Useable.Enter. Object not set as used!\r\n{exception3}",
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
                                $"Caught exception in OnUseDeclined \r\n (response was Fail_Redundant){exception4}",
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
                        Logger.LogError(
                            $"Caught exception in OnUseDeclined \r\n (response was Fail_Vacancy){exception5}",
                            use.implementation);
                    }
                }

                return UseResponse.Fail_Vacancy;
            }
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
                        Logger.LogError(
                            $"[ItemLoot] The Game says {ime4.Player.Name} probably cheats with inv. Report this to DreTaX on fougerite.com");
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
                    {
                        ItemMoveEvent ime2 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                        try
                        {
                            ExecuteSubscribers(OnItemMove, "ItemMoveEvent", ime2);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ItemMoveEvent Error: {ex}");
                        }

                        return Inventory.SlotOperationResult.Success_Stacked;
                    }

                    case InventoryItem.MergeResult.Combined:
                    {
                        ItemMoveEvent ime3 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                        try
                        {
                            ExecuteSubscribers(OnItemMove, "ItemMoveEvent", ime3);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ItemMoveEvent Error: {ex}");
                        }

                        return Inventory.SlotOperationResult.Success_Combined;
                    }
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
                ExecuteSubscribers(OnItemMove, "ItemMoveEvent", ime);
            }
            catch (Exception ex)
            {
                Logger.LogError($"ItemMoveEvent Error: {ex}");
            }

            return Inventory.SlotOperationResult.Success_Moved;
        }

        public static bool FGCompleteRepair(RepairBench inst, Inventory ingredientInv)
        {
            using (new Stopper(nameof(Hooks), nameof(FGCompleteRepair)))
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

                Fougerite.Events.RepairEvent re = new Fougerite.Events.RepairEvent(inst, ingredientInv);
                try
                {
                    ExecuteSubscribers(OnRepairBench, "RepairEvent", re);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"RepairEvent Error: {ex}");
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
        }

        public static bool OnBanEventHandler(BanEvent be)
        {
            using (new Stopper(nameof(Hooks), nameof(OnBanEventHandler)))
            {
                try
                {
                    ExecuteSubscribers(OnPlayerBan, "BanEvent", be);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"BanEvent Error: {ex}");
                }

                return be.Cancelled;
            }
        }

        public static void GenericHook(GenericSpawner gs)
        {
            using (new Stopper(nameof(Hooks), nameof(GenericHook)))
            {
                try
                {
                    ExecuteSubscribers(OnGenericSpawnerLoad, "GenericSpawnerLoad", gs);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"GenericSpawnerLoad Error: {ex}");
                }
            }
        }

        public static IEnumerator ServerLoadedHook(ServerInit init, string levelName)
        {
            yield return RustLevel.Load(levelName);
            
            // Do our own stuff
            GameObject go = new GameObject();
            ServerSaveHandler h = go.AddComponent<ServerSaveHandler>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            World.GetWorld().ServerSaveHandler = h;
            ServerInitialized = true;
            Server.GetServer().ServerLoaded = true;
            
            try
            {
                ExecuteSubscribers(OnServerLoaded, "ServerLoaded");
            }
            catch (Exception ex)
            {
                Logger.Log($"ServerLoaded Error: {ex}");
            }
            
            // The hooked NGC functions are called before the object.ReadSave() functions.
            // They are inconsistent implementations, so I fix up the Entity data manually after loading.
            foreach (Entity loadedEntity in World.GetWorld().Entities)
            {
                loadedEntity.InitiateFix();
            }
            
            Logger.Log("Server Initialized.");
            UnityEngine.Object.Destroy(init.gameObject);
            World.GetWorld().AllTrees.AddRange(World.GetWorld().GetAllTreeInstances());
            yield break;
        }

        public static void DoBeltUseHook(InventoryHolder holder, int beltNum)
        {
            using (new Stopper(nameof(Hooks), nameof(DoBeltUseHook)))
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
                        Logger.LogWarning($"[DoBeltUse] Belt number is different. {beltNum}");
                        return;
                    }

                    PlayerInventory inventory;
                    IInventoryItem item;
                    BeltUseEvent be = new BeltUseEvent(holder, beltNum);
                    try
                    {
                        ExecuteSubscribers(OnBeltUse, "BeltUseEvent", be);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"BeltUseEvent Error: {ex}");
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
                    Logger.LogError($"[DoBeltUse Error] {ex}");
                }
            }
        }

        public static void OnSupplySignalExplosion(SignalGrenade grenade)
        {
            using (new Stopper(nameof(Hooks), nameof(OnSupplySignalExplosion)))
            {
                Vector3 randompos = grenade.rigidbody.position +
                                    new Vector3(UnityEngine.Random.Range(-20f, 20f), 75f,
                                        UnityEngine.Random.Range(-20f, 20f));
                SupplySignalExplosionEvent sg = new SupplySignalExplosionEvent(grenade, randompos);

                try
                {
                    ExecuteSubscribers(OnSupplySignalExpode, "SupplySignalExplosionEvent", sg);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[SupplySignalExplosion Error] {ex}");
                }

                if (sg.Cancelled)
                {
                    return;
                }

                SupplyDropZone.CallAirDropAt(randompos);
            }
        }

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
                    ShootEvent se = new ShootEvent(instance, obj2, rep, info, item, part, flag, flag2, flag3, part2,
                        vector, vector2);
                    ExecuteSubscribers(OnShoot, "ShootEvent", se);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ShootEvent Error: {ex}");
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
            using (new Stopper(nameof(Hooks), nameof(HandGrenadeDoAction1)))
            {
                IHandGrenadeItem item;
                NetCull.VerifyRPC(ref info, false);
                if (rep.Item<IHandGrenadeItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
                {
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

                    rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                    GameObject obj2 = grenade.ThrowItem(rep, origin, forward);
                    if (obj2 != null)
                    {
                        obj2.rigidbody.AddTorque(new Vector3(
                            UnityEngine.Random.Range(-1f, 1f),
                            UnityEngine.Random.Range(-1f, 1f),
                            UnityEngine.Random.Range(-1f, 1f)) * 10f);
                        try
                        {
                            GrenadeThrowEvent se = new GrenadeThrowEvent(grenade, obj2, rep, info, item);
                            ExecuteSubscribers(OnGrenadeThrow, "GrenadeThrowEvent", se);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"GrenadeThrowEvent Error: {ex}");
                        }
                    }

                    int count = 1;
                    if (item.Consume(ref count))
                    {
                        item.inventory.RemoveItem(item.slot);
                    }
                }
            }
        }
        
        /// <summary>
        /// Runs when a player throws a Flare using a TorchItemDataBlock.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="stream"></param>
        /// <param name="rep"></param>
        /// <param name="info"></param>
        public static void TorchDoAction1(TorchItemDataBlock instance, uLink.BitStream stream, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info)
        {
            ITorchItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<ITorchItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
            {
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
                
                FlareThrowEvent fe = new FlareThrowEvent(instance, item, origin, forward);
                try
                {
                    ExecuteSubscribers(OnFlareThrow, "FlareThrowEvent", fe);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"FlareThrowEvent Error: {ex}");
                }

                if (fe.Cancelled)
                {
                    return;
                }

                if (item.isLit)
                {
                    item.Extinguish();
                }

                rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);

                instance.ThrowFlare(rep, origin, forward);
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
                        ExecuteSubscribers(OnShotgunShoot, "ShotgunShootEvent", tempcall);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"ShotgunShootEvent Error: {ex}");
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
                            ShotgunShootEvent se = new ShotgunShootEvent(instance, rep, info, found,
                                ShotgunEventType.AfterShot, part, flag, flag2, flag3, part2, vector, vector2);
                            ExecuteSubscribers(OnShotgunShoot, "ShotgunShootEvent", se);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"ShotgunShootEvent Error: {ex}");
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

        public static void ServerShutdown()
        {
            IsShuttingDown = true;
            try
            {
                ExecuteSubscribers(OnServerShutdown, "ServerShutdownEvent");
            }
            catch (Exception ex)
            {
                Logger.LogError($"ServerShutdownEvent Error: {ex}");
            }

            // For early quit
            if (!Server.GetServer().ServerLoaded) 
                return;
            
            foreach (var x in World.GetWorld().GetZones())
            {
                x.HideMarkers();
            }

            World.GetWorld().ServerSaveHandler.ManualSave();
        }
        
        internal static void ModulesLoaded()
        {
            using (new Stopper(nameof(Hooks), nameof(ModulesLoaded)))
            {
                try
                {
                    ExecuteSubscribers(OnModulesLoaded, "ModulesLoadedEvent");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ModulesLoadedEvent Error: {ex}");
                }
            }
        }

        public static void ServerStarted()
        {
            using (new Stopper(nameof(Hooks), nameof(ServerStarted)))
            {
                try
                {
                    ExecuteSubscribers(OnServerInit, "ServerInitEvent");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ServerInitEvent Error: {ex}");
                }
            }
        }

        public static void NPCSpawned(NPC npc)
        {
            using (new Stopper(nameof(Hooks), nameof(NPCSpawned)))
            {
                try
                {
                    ExecuteSubscribers(OnNPCSpawned, "NPCSpawned", npc);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"NPCSpawned Error: {ex}");
                }
            }
        }
        
        /// <summary>
        /// A hook of the NetCull.Instantiated function.
        /// Re-created to add all spawned Entities into the EntityCache, so we may as well have a synchronized
        /// list for plugins without having to worry of crashes.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="instantiatedGroup"></param>
        /// <param name="setGroup"></param>
        /// <param name="ia"></param>
        /// <returns></returns>
        public static UnityEngine.Object Instantiated(UnityEngine.Object instance, int instantiatedGroup, int setGroup, ref NetCull.InstantiateArgs ia)
        {
            int? group = ia.group;
            if ((group.GetValueOrDefault() != instantiatedGroup || group == null) && CullGrid.IsCellGroupID(setGroup))
            {
                Facepunch.NetworkView view;
                if (!NetCull.GetNetworkView(instance, out view))
                {
                    Debug.LogError($"Could not get view, will not be dynamic group {instance}", instance);
                }
                else
                {
                    NetworkCullInfo cullInfo = NetCull.RegisterCullInfo(view, ia.piggy, (bool) ia.piggy, ia.owner);
                    if (ia.owner != null)
                    {
                        if (ia.playerRoot)
                        {
                            cullInfo.playerRoot = true;
                            CullGrid.RegisterPlayerRootNetworkCullInfo(cullInfo);
                        }
                        else
                        {
                            cullInfo.playerRoot = false;
                            CullGrid.RegisterPlayerNonRootNetworkCullInfo(cullInfo);
                        }
                    }
                    try
                    {
                        cullInfo.OnInitialRegistrationComplete();
                    }
                    catch (Exception exception1)
                    {
                        Debug.LogError(exception1);
                    }
                }
            }

            // This is casted to a GameObject in the NetCull class originally so this should always work.
            if (instance is GameObject gameObject)
            {
                object underLying = null;
                if (gameObject.GetComponent<DeployableObject>() != null)
                {
                    underLying = gameObject.GetComponent<DeployableObject>();
                }
                else if (gameObject.GetComponent<StructureMaster>() != null)
                {
                    underLying = gameObject.GetComponent<StructureMaster>();
                }
                else if (gameObject.GetComponent<StructureComponent>() != null)
                {
                    underLying = gameObject.GetComponent<StructureComponent>();
                }
                else if (gameObject.GetComponent<StructureMaster>() != null)
                {
                    underLying = gameObject.GetComponent<StructureMaster>();
                }
                else if (gameObject.GetComponent<LootableObject>() != null)
                {
                    underLying = gameObject.GetComponent<LootableObject>();
                }
                else if (gameObject.GetComponent<ResourceTarget>() != null)
                {
                    underLying = gameObject.GetComponent<ResourceTarget>();
                }
                else if (gameObject.GetComponent<SupplyCrate>() != null)
                {
                    underLying = gameObject.GetComponent<SupplyCrate>();
                }

                if (underLying == null) 
                    return instance;
                
                Entity entity = new Entity(underLying);
                EntityCache.GetInstance().Add(entity);
            }
            
            return instance;
        }
        
        /// <summary>
        /// A hook of the NGC.Instantiate function. (A separate call to it when using certain static instantiate and other functions)
        /// Re-created to add all spawned Entities into the EntityCache, so we may as well have a synchronized
        /// list for plugins without having to worry of crashes.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="groupToUse"></param>
        /// <returns></returns>
        public static UnityEngine.Object InstantiateNGC(ref NetCull.InstantiateArgs args, int groupToUse)
        {
            NGC.Prefab prefab;
            NGC component;
            if (groupToUse < 0)
            {
                Debug.LogError("Group cant be less than zero");
                return null;
            }
            if (!NGC.Prefab.Register.Find(args.prefabName, out prefab))
            {
                Debug.LogError($"No NGC Prefab with name:{args.prefabName}");
                return null;
            }
            if (!NGC.Global.byGroup.TryGetValue((ushort) groupToUse, out component))
            {
                component = NetCull.InstantiateClassic("!Ng", Vector3.zero, Quaternion.identity, groupToUse).GetComponent<NGC>();
            }

            UnityEngine.Object obj = component.ServerInstantiate(prefab, ref args);
            // This is casted to a GameObject in the NetCull class originally so this should always work.
            if (obj is GameObject gameObject)
            {
                object underLying = null;
                if (gameObject.GetComponent<DeployableObject>() != null)
                {
                    underLying = gameObject.GetComponent<DeployableObject>();
                }
                else if (gameObject.GetComponent<StructureMaster>() != null)
                {
                    underLying = gameObject.GetComponent<StructureMaster>();
                }
                else if (gameObject.GetComponent<StructureComponent>() != null)
                {
                    underLying = gameObject.GetComponent<StructureComponent>();
                }
                else if (gameObject.GetComponent<LootableObject>() != null)
                {
                    underLying = gameObject.GetComponent<LootableObject>();
                }
                else if (gameObject.GetComponent<ResourceTarget>() != null)
                {
                    underLying = gameObject.GetComponent<ResourceTarget>();
                }
                else if (gameObject.GetComponent<SupplyCrate>() != null)
                {
                    underLying = gameObject.GetComponent<SupplyCrate>();
                }

                if (underLying == null) 
                    return obj;
                
                Entity entity = new Entity(underLying);
                EntityCache.GetInstance().Add(entity);
            }
            
            return obj;
        }

        /// <summary>
        /// A hook of the NetCull.Destroy function.
        /// Re-created to remove all destroyed Entities from the EntityCache, so we may as well have a synchronized
        /// list for plugins without having to worry of crashes.
        /// </summary>
        /// <param name="view"></param>
        public static void DestroyByView(Facepunch.NetworkView view)
        {
            // Sanity check, shouldn't happen.
            if (view == null)
            {
                return;
            }
            
            GameObject go = view.gameObject;
            object underLying = null;
            
            int id = go.GetInstanceID();
            if (go.GetComponent<DeployableObject>() != null)
            {
                underLying = go.GetComponent<DeployableObject>();
                id = ((DeployableObject) underLying).GetInstanceID();
            }
            else if (go.GetComponent<StructureMaster>() != null)
            {
                underLying = go.GetComponent<StructureMaster>();
                id = ((StructureMaster) underLying).GetInstanceID();
            }
            else if (go.GetComponent<StructureComponent>() != null)
            {
                underLying = go.GetComponent<StructureComponent>();
                id = ((StructureComponent) underLying).GetInstanceID();
            }
            else if (go.GetComponent<LootableObject>() != null)
            {
                underLying = go.GetComponent<LootableObject>();
                id = ((LootableObject) underLying).GetInstanceID();
            }
            else if (go.GetComponent<ResourceTarget>() != null)
            {
                underLying = go.GetComponent<ResourceTarget>();
                id = ((ResourceTarget) underLying).GetInstanceID();
            }
            else if (go.GetComponent<SupplyCrate>() != null)
            {
                underLying = go.GetComponent<SupplyCrate>();
                id = ((SupplyCrate) underLying).GetInstanceID();
            }
            
            if (underLying != null && EntityCache.GetInstance().Contains(id))
            {
                EntityCache.GetInstance().Remove(id);
            }
            
            if (underLying != null && DecayList.ContainsKey(id))
            {
                DecayList.TryRemove(id);
            }
            
            NetworkCullInfo info;
            NetInstance.PreServerDestroy(view);
            if (NetworkCullInfo.Find(view, out info))
            {
                NetCull.ShutdownNetworkCullInfoAndDestroy(info);
            }
            else
            {
                NetCull.RemoveRPCs(view.viewID);
                uLink.Network.Destroy(view);
            }
        }

        /// <summary>
        /// A hook of the NetCull.Destroy function.
        /// Re-created to remove all destroyed Entities from the EntityCache, so we may as well have a synchronized
        /// list for plugins without having to worry of crashes.
        /// </summary>
        /// <param name="viewID"></param>
        public static void DestroyByNetworkId(uLink.NetworkViewID viewID)
        {
            if (viewID != uLink.NetworkViewID.unassigned)
            {
                Facepunch.NetworkView networkView = Facepunch.NetworkView.Find(viewID);
                if (networkView != null)
                {
                    GameObject go = networkView.gameObject;
                    object underLying = null;
            
                    int id = go.GetInstanceID();
                    if (go.GetComponent<DeployableObject>() != null)
                    {
                        underLying = go.GetComponent<DeployableObject>();
                        id = ((DeployableObject) underLying).GetInstanceID();
                    }
                    else if (go.GetComponent<StructureMaster>() != null)
                    {
                        underLying = go.GetComponent<StructureMaster>();
                        id = ((StructureMaster) underLying).GetInstanceID();
                    }
                    else if (go.GetComponent<StructureComponent>() != null)
                    {
                        underLying = go.GetComponent<StructureComponent>();
                        id = ((StructureComponent) underLying).GetInstanceID();
                    }
                    else if (go.GetComponent<LootableObject>() != null)
                    {
                        underLying = go.GetComponent<LootableObject>();
                        id = ((LootableObject) underLying).GetInstanceID();
                    }
                    else if (go.GetComponent<ResourceTarget>() != null)
                    {
                        underLying = go.GetComponent<ResourceTarget>();
                        id = ((ResourceTarget) underLying).GetInstanceID();
                    }
                    else if (go.GetComponent<SupplyCrate>() != null)
                    {
                        underLying = go.GetComponent<SupplyCrate>();
                        id = ((SupplyCrate) underLying).GetInstanceID();
                    }

                    if (underLying != null && EntityCache.GetInstance().Contains(id))
                    {
                        EntityCache.GetInstance().Remove(id);
                    }
                    
                    if (underLying != null && DecayList.ContainsKey(id))
                    {
                        DecayList.TryRemove(id);
                    }
                }
            }
            
            NetworkCullInfo info;
            NetInstance.PreServerDestroy(viewID);
            if (NetworkCullInfo.Find(viewID, out info))
            {
                NetCull.ShutdownNetworkCullInfoAndDestroy(info);
            }
            else
            {
                NetCull.RemoveRPCs(viewID);
                uLink.Network.Destroy(viewID);
            }
        }
        
        /// <summary>
        /// A hook of the NetCull.Destroy function.
        /// Re-created to remove all destroyed Entities from the EntityCache, so we may as well have a synchronized
        /// list for plugins without having to worry of crashes.
        /// </summary>
        /// <param name="go"></param>
        public static void DestroyByGameObject(GameObject go)
        {
            // Sanity check, shouldn't happen.
            if (go == null)
            {
                return;
            }
            
            object underLying = null;
            int id = go.GetInstanceID();

            if (go.GetComponent<DeployableObject>() != null)
            {
                underLying = go.GetComponent<DeployableObject>();
                id = ((DeployableObject) underLying).GetInstanceID();
            }
            else if (go.GetComponent<StructureMaster>() != null)
            {
                underLying = go.GetComponent<StructureMaster>();
                id = ((StructureMaster) underLying).GetInstanceID();
            }
            else if (go.GetComponent<StructureComponent>() != null)
            {
                underLying = go.GetComponent<StructureComponent>();
                id = ((StructureComponent) underLying).GetInstanceID();
            }
            else if (go.GetComponent<LootableObject>() != null)
            {
                underLying = go.GetComponent<LootableObject>();
                id = ((LootableObject) underLying).GetInstanceID();
            }
            else if (go.GetComponent<ResourceTarget>() != null)
            {
                underLying = go.GetComponent<ResourceTarget>();
                id = ((ResourceTarget) underLying).GetInstanceID();
            }
            else if (go.GetComponent<SupplyCrate>() != null)
            {
                underLying = go.GetComponent<SupplyCrate>();
                id = ((SupplyCrate) underLying).GetInstanceID();
            }
            
            if (underLying != null && EntityCache.GetInstance().Contains(id))
            {
                EntityCache.GetInstance().Remove(id);
            }
            
            if (underLying != null && DecayList.ContainsKey(id))
            {
                DecayList.TryRemove(id);
            }

            NGCView component = go.GetComponent<NGCView>();
            if (component)
            {
                NGC.DispatchNetDestroy(component);
            }
            else
            {
                NetworkCullInfo info;
                NetInstance.PreServerDestroy(go);
                if (NetworkCullInfo.Find(go, out info))
                {
                    NetCull.ShutdownNetworkCullInfoAndDestroy(info);
                }
                else
                {
                    Facepunch.NetworkView view2 = Facepunch.NetworkView.Get(go);
                    if (view2)
                    {
                        NetCull.RemoveRPCs(view2.viewID);
                    }
                    uLink.Network.Destroy(go);
                }
            }
        }
        
        /// <summary>
        /// A hook of the WildlifeManager.AddWildlifeInstance function.
        /// Used to cache NPCs basically.
        /// AI spawns even before the physics is baked / server is initialized.
        /// </summary>
        /// <param name="ai"></param>
        /// <returns></returns>
        public static bool AddWildlifeInstance(BasicWildLifeAI ai)
        {
            // Check for DataShutdown, and add It to the Data class before if possible
            bool value = !WildlifeManager.DataShutdown && WildlifeManager.Data.Add(ai);
            
            // Grab the character
            Character ch = ai.GetComponent<Character>();
            
            // Check DataShutdown and Addition
            if (ch != null && value)
            {
                // All good, create the NPC class and throw it to our cache
                NPC npc = new NPC(ch);
                NPCCache.GetInstance().Add(npc);
                
                // Call event, from this point a plugin can kill the NPC as well as It's already in the Data class
                NPCSpawned(npc);
            }
            
            return value;
        }

        /// <summary>
        /// A hook of the WildlifeManager.AddWildlifeInstance function.
        /// Used to cache NPCs basically.
        /// </summary>
        /// <param name="ai"></param>
        /// <returns></returns>
        public static bool RemoveWildlifeInstance(BasicWildLifeAI ai)
        {
            // Grab the character
            Character ch = ai.GetComponent<Character>();
            if (ch != null && NPCCache.GetInstance().Contains(ch.GetInstanceID()))
            {
                NPCCache.GetInstance().Remove(ch.GetInstanceID());
            }
            
            return WildlifeManager.DataInitialized && WildlifeManager.Data.Remove(ai);
        }
        
        /// <summary>
        /// A hook of TimedExplosive.Awake function.
        /// Runs when a C4 is placed.
        /// </summary>
        /// <param name="timedExplosive"></param>
        public static void TimedExplosiveSpawn(TimedExplosive timedExplosive)
        {
            using (new Stopper(nameof(Hooks), nameof(TimedExplosiveSpawn)))
            {
                // Set testView first like in the original code
                timedExplosive.testView = timedExplosive.GetComponent<NGCView>();

                // Event
                TimedExplosiveEvent timedExplosiveEvent = new TimedExplosiveEvent(timedExplosive);
                try
                {
                    ExecuteSubscribers(OnTimedExplosiveSpawned, "TimedExplosiveSpawnedEvent", timedExplosiveEvent);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"TimedExplosiveSpawnedEvent Error: {ex}");
                }

                // Return on cancel
                // Cancelling will leave the C4 there and ticking.
                // On my end doing the exact same as TimedExplosive.Explode() does (open in a reverse tool)
                // RPC to ClientExplode (this threw nullref) and NetCull.Destroy seem to have failed
                // I didn't research any further, but you are welcome to try
                if (timedExplosiveEvent.Cancelled)
                    return;
                
                timedExplosive.Invoke(nameof(TimedExplosive.Explode), timedExplosive.fuseLength);
            }
        }
        
        /// <summary>
        /// A hook of SleepingAvatar.Registry.Register function.
        /// Runs when a Sleeper is created.
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        public static bool SleeperRegister(SleepingAvatar avatar)
        {
            // Sanity check
            if (avatar == null)
            {
                return false;
            }
            
            SleepingAvatar avatar2;
            if (SleepingAvatar.Registry.all.TryGetValue(avatar.creatorID, out avatar2))
            {
                if (avatar2 == avatar)
                {
                    return false;
                }
                avatar2.registered = false;
            }
            SleepingAvatar.Registry.all[avatar.creatorID] = avatar;
            avatar.registered = true;

            // Add It to the cache
            DeployableObject deployableObject = avatar.GetComponent<DeployableObject>();
            if (deployableObject != null)
            {
                int instanceId = deployableObject.GetInstanceID();
                // Freshly created Sleeper object will not assign the ownerids yet, as the NGC.Instantiate hook is called earlier than
                // Rust's SetupCharacter, SetupCreator functions..
                Entity ent = EntityCache.GetInstance().GrabOrAllocate(instanceId, deployableObject);
                ent.InitiateFix();
                
                Sleeper sleeper = new Sleeper(deployableObject);
                SleeperCache.GetInstance().Add(sleeper);
                
                try
                {
                    ExecuteSubscribers(OnSleeperSpawned, "SleeperSpawnedEvent", sleeper);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"SleeperSpawnedEvent Error: {ex}");
                }
            }

            return true;
        }

        /// <summary>
        /// A hook of FireBarrel.SetOn function.
        /// Gets called when you toggle a Campfire or Furnace.
        /// </summary>
        /// <param name="fireBarrel"></param>
        /// <param name="on"></param>
        public static void FireBarrelSetOn(FireBarrel fireBarrel, bool on)
        {
            var deployable = fireBarrel.GetComponent<DeployableObject>();
            // Sanity check
            if (deployable == null)
                return;
            
            Entity e = EntityCache.GetInstance().GrabOrAllocate(deployable.GetInstanceID(), deployable);
            float cookDuration = fireBarrel.GetCookDuration();
            cookDuration = UnityEngine.Random.Range(cookDuration * 0.5f, cookDuration);
            FireBarrelToggleEvent fireBarrelToggleEvent = new FireBarrelToggleEvent(fireBarrel, on, e, cookDuration);
            
            try
            {
                ExecuteSubscribers(OnFireBarrelToggle, "FireBarrelToggleEvent", fireBarrelToggleEvent);
            }
            catch (Exception ex)
            {
                Logger.LogError($"FireBarrelToggleEvent Error: {ex}");
            }
            
            if (fireBarrelToggleEvent.Cancelled)
            {
                return;
            }
            
            fireBarrel.isOn = fireBarrelToggleEvent.On;
            if (fireBarrelToggleEvent.On)
            {
                if (fireBarrel._deployable)
                {
                    fireBarrel._deployable.SetDecayEnabled(false);
                }
                
                fireBarrel.InvokeRepeating(nameof(FireBarrel.ConsumeFuel), fireBarrelToggleEvent.CookDuration, fireBarrelToggleEvent.CookDuration);
                EnvDecay.RefreshRadialDecay(fireBarrel.transform.position, FireBarrel.decayResetRange);
            }
            else
            {
                fireBarrel.CancelInvoke(nameof(FireBarrel.ConsumeFuel));
                if (fireBarrel._deployable)
                {
                    fireBarrel._deployable.SetDecayEnabled(true);
                }
            }

            fireBarrel.DecayTouch();
            if (fireBarrel._heatZone)
            {
                fireBarrel._heatZone.SetOn(fireBarrelToggleEvent.On);
            }

            fireBarrel.UpdateNetState();
        }

        /// <summary>
        /// A hook of ConsumableDataBlock.UseItem function.
        /// </summary>
        /// <param name="consumableDataBlock"></param>
        /// <param name="item"></param>
        public static void ConsumableUseItem(ConsumableDataBlock consumableDataBlock, IConsumableItem item)
        {
            Inventory inventory = item.inventory;
            Metabolism local = inventory.GetLocal<Metabolism>();
            if (local == null)
            {
                return;
            }
            if (!local.CanConsumeYet())
            {
                return;
            }
            
            ConsumableUseEvent consumeEvent = new ConsumableUseEvent(consumableDataBlock, item);
    
            try
            {
                ExecuteSubscribers(OnConsumableUse, "ConsumableUseEvent", consumeEvent);
            }
            catch (Exception ex)
            {
                Logger.LogError($"ConsumableUseEvent Callback Error: {ex}");
            }
            
            if (consumeEvent.Cancelled)
            {
                return;
            }
            
            local.MarkConsumptionTime();
            float remainingSpace = Mathf.Min(local.GetRemainingCaloricSpace(), consumeEvent.Calories);
            if (consumeEvent.Calories > 0f)
            {
                local.AddCalories(remainingSpace);
            }
            
            if (consumeEvent.Water > 0f)
            {
                local.AddWater(consumeEvent.Water);
            }
            
            if (consumeEvent.AntiRads > 0f)
            {
                local.AddAntiRad(consumeEvent.AntiRads);
            }
            
            if (consumeEvent.HealthToHeal != 0f)
            {
                HumanBodyTakeDamage local2 = inventory.GetLocal<HumanBodyTakeDamage>();
                if (local2 != null)
                {
                    if (consumableDataBlock.healthToHeal > 0f)
                    {
                        local2.HealOverTime(consumeEvent.HealthToHeal);
                    }
                    else
                    {
                        TakeDamage.HurtSelf(inventory.idMain, Mathf.Abs(consumeEvent.HealthToHeal), null);
                    }
                }
            }
            
            if (consumeEvent.PoisonAmount > 0f)
            {
                local.AddPoison(consumeEvent.PoisonAmount);
            }
            
            item.FireClientSideItemEvent(InventoryItem.ItemEvent.Used);
            int amountToProcess = consumeEvent.AmountToConsume;
            if (item.Consume(ref amountToProcess))
            {
                inventory.RemoveItem(item.slot);
            }
            else
            {
                inventory.MarkSlotDirty(item.slot);
            }
        }

        /// <summary>
        /// Runs when a BasicHealthKit is used.
        /// </summary>
        /// <param name="basicHealthKitDataBlock"></param>
        /// <param name="hk"></param>
        public static void BasicHealthKitUse(BasicHealthKitDataBlock basicHealthKitDataBlock, IBasicHealthKit hk)
        {
            if (Time.time < hk.lastUseTime + 5f)
            {
                return;
            }
            int slot = hk.slot;
            Inventory inventory = hk.inventory;
            HumanBodyTakeDamage local = inventory.GetLocal<HumanBodyTakeDamage>();
            if (!local)
            {
                return;
            }
            Metabolism local2 = inventory.GetLocal<Metabolism>();
            if (!local2)
            {
                return;
            }
            
            if (local.healthLoss == 0f)
            {
                return;
            }
            
            MedikitUseEvent medEvent = new MedikitUseEvent(basicHealthKitDataBlock, hk);

            try
            {
                ExecuteSubscribers(OnMedikitUse, "MedikitUseEvent", medEvent);
            }
            catch (Exception ex)
            {
                Logger.LogError($"MedikitUseEvent Error: {ex}");
            }
            
            if (medEvent.Cancelled)
            {
                return;
            }
            
            if (medEvent.StopsBleeding)
            {
                local.Bandage(1000f);
            }
            float healAmount = UnityEngine.Random.Range(medEvent.HealthAddMin, medEvent.HealthAddMax);
            if (healAmount > 0f)
            {
                local.HealOverTime(healAmount);
            }
            
            hk.lastUseTime = Time.time;
            int consumeAmount = medEvent.AmountToConsume;
            bool itemDestroyed = hk.Consume(ref consumeAmount);
            if (consumeAmount == 0)
            {
                inventory.MarkSlotDirty(slot);
                hk.FireClientSideItemEvent(InventoryItem.ItemEvent.Used);
            }
            
            if (itemDestroyed)
            {
                inventory.RemoveItem(slot);
            }
        }
        
        
        /// <summary>
        /// This runs when an Item Mod is being installed.
        /// </summary>
        public static void ItemAddMod<T>(HeldItem<T> held, ItemModDataBlock mod) where T : HeldItemDataBlock
        {
            try
            {
                ItemModInstallEvent<T> modEvent = new ItemModInstallEvent<T>(held, mod);
                OnItemMod<T>.Raise(modEvent);

                if (modEvent.Cancelled)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnItemMod Callback Error: {ex}");
            }
            
            held.RecalculateMods();
            int usedModSlots = held.usedModSlots;
            held._itemMods[usedModSlots] = mod;
            held.RecalculateMods();
            held.OnModAdded(mod);
            held.MarkDirty();
        }

        /// <summary>
        /// This runs when a Blood Draw item is used.
        /// </summary>
        /// <param name="bloodDrawDatablock"></param>
        /// <param name="item"></param>
        public static void BloodDrawUse(BloodDrawDatablock bloodDrawDatablock, IBloodDrawItem item)
        {
            if (Time.time < item.lastUseTime + 2f)
            {
                return;
            }
            Inventory inventory = item.inventory;
            HumanBodyTakeDamage local = inventory.GetLocal<HumanBodyTakeDamage>();
            BloodDrawEvent be = new BloodDrawEvent(item, bloodDrawDatablock.bloodToTake);
            try
            {
                ExecuteSubscribers(OnBloodDraw, "BloodDrawEvent", be);
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnBloodDraw Error: {ex}");
            }
            
            if (be.Cancelled)
            {
                return;
            }
            
            if (local.health <= be.BloodToTake)
            {
                Notice.Popup(inventory.networkView.owner, "\uf161", "You're too weak to use this");
                return;
            }
            
            IDMain idMain = inventory.idMain;
            TakeDamage.Hurt(idMain, idMain, be.BloodToTake, null);
            inventory.AddItem(ref BloodDrawDatablock.LateLoaded.blood, Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Default, true, Inventory.Slot.KindFlags.Belt), 1);
            item.lastUseTime = Time.time;
            item.FireClientSideItemEvent(InventoryItem.ItemEvent.Used);
        }
        
        
        /// <summary>
        /// Hook called by ArmorDataBlock.OnEquipped.
        /// </summary>
        public static void ArmorEquipped(ArmorDataBlock block, IEquipmentItem item)
        {
            item.FireClientSideItemEvent(InventoryItem.ItemEvent.Equipped);
            
            // In this event patch there is no way to cancel the item equip, due to the code of facepunch.
            // You can however drop the item right after equipping it in the event or whatever.
            // Or catch it on itemmove or something.
            ArmorEquipEvent ae = new ArmorEquipEvent(block, item, ArmorChangeType.Equipped);
            try
            {
                ExecuteSubscribers(OnArmorEquip, "OnArmorEquip", ae);
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnArmorEquip Error: {ex}");
            }
        }

        /// <summary>
        /// Hook called by ArmorDataBlock.OnUnEquipped.
        /// </summary>
        public static void ArmorUnEquipped(ArmorDataBlock block, IEquipmentItem item)
        {
            item.FireClientSideItemEvent(InventoryItem.ItemEvent.UnEquipped);
            
            // In this event patch there is no way to cancel the item equip, due to the code of facepunch.
            // You can however drop the item right after equipping it in the event or whatever.
            // Or catch it on itemmove or something.
            ArmorEquipEvent ae = new ArmorEquipEvent(block, item, ArmorChangeType.Unequipped);
            try
            {
                ExecuteSubscribers(OnArmorUnEquip, "OnArmorUnEquip", ae);
            }
            catch (Exception ex)
            {
                Logger.LogError($"OnArmorUnEquip Error: {ex}");
            }
        }
        
        /// <summary>
        /// Hook called by TorchItemDataBlock.DoAction2.
        /// Handles the logic for igniting the flare.
        /// </summary>
        public static void TorchDoAction2(TorchItemDataBlock instance, uLink.BitStream stream, ItemRepresentation itemRep, ref uLink.NetworkMessageInfo info)
        {
            using (new Stopper(nameof(Hooks), nameof(TorchDoAction2)))
            {
                ITorchItem torchItem;
                if (itemRep.Item<ITorchItem>(out torchItem))
                {
                    FlareIgniteEvent tie = new FlareIgniteEvent(instance, torchItem, stream, itemRep, info);
                    try
                    {
                        ExecuteSubscribers(OnFlareIgnite, "FlareIgniteEvent", tie);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"FlareIgniteEvent Error: {ex}");
                    }

                    torchItem.Ignite();
                    itemRep.Action(2, uLink.RPCMode.OthersExceptOwnerBuffered);
                }
            }
        }
        
        /// <summary>
        /// Hook called by BasicTorchItemDataBlock.DoAction2.
        /// </summary>
        public static void BasicTorchDoAction2(BasicTorchItemDataBlock instance, uLink.BitStream stream, ItemRepresentation itemRep, ref uLink.NetworkMessageInfo info)
        {
            using (new Stopper(nameof(Hooks), nameof(BasicTorchDoAction2)))
            {
                IBasicTorchItem torchItem;
                if (itemRep.Item<IBasicTorchItem>(out torchItem))
                {
                    itemRep.Action(2, uLink.RPCMode.OthersExceptOwnerBuffered);
                    
                    BasicTorchIgniteEvent btie = new BasicTorchIgniteEvent(instance, torchItem, stream, itemRep, info);
                    try
                    {
                        ExecuteSubscribers(OnBasicTorchIgnite, "BasicTorchIgniteEvent", btie);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"BasicTorchIgniteEvent Error: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// A hook of SleepingAvatar.Registry.UnRegister function.
        /// Runs when a Sleeper is destroyed/killed.
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        public static bool SleeperUnRegister(SleepingAvatar avatar)
        {
            if (avatar != null)
            {
                if (!avatar.registered) 
                    return false;
                
                if (SleepingAvatar.Registry.all.TryGetValue(avatar.creatorID, out SleepingAvatar avatar2) && avatar2 == avatar)
                {
                    SleepingAvatar.Registry.all.Remove(avatar.creatorID);
                }
                avatar.registered = false;
                
                // Remove It from the cache
                DeployableObject deployableObject = avatar.GetComponent<DeployableObject>();
                if (deployableObject != null)
                {
                    SleeperCache.GetInstance().Remove(deployableObject.GetInstanceID());
                }
                
                return true;
            }
            if (!ReferenceEquals(avatar, null))
            {
                Debug.LogWarning("Got missing avatar in UnRegister, running scan to find invalid entries..", avatar);
                SleepingAvatar.Registry.CleanUpPossibleMissingPairs();
            }
            return false;
        }

        /// <summary>
        /// A hook of the EnvironmentControlCenter.DayCycleChange function.
        /// Runs when the day cycle changes.
        /// </summary>
        /// <param name="ecc"></param>
        public static void DayCycleChange(EnvironmentControlCenter ecc)
        {
            if (ecc.sky == null)
            {
                ecc.sky = (TOD_Sky) UnityEngine.Object.FindObjectOfType(typeof(TOD_Sky));
                if (ecc.sky == null)
                {
                    return;
                }
            }
            
            float num = env.daylength * 60f;
            if (ecc.IsNight())
            {
                num = env.nightlength * 60f;
            }
            float num2 = num / 24f;
            float num3 = Time.deltaTime / num2;
            float num4 = Time.deltaTime / (30f * num) * 2f;
            ecc.sky.Cycle.Hour += num3;
            ecc.sky.Cycle.MoonPhase += num4;
            if (ecc.sky.Cycle.MoonPhase < -1f)
            {
                ecc.sky.Cycle.MoonPhase += 2f;
            }
            else if (ecc.sky.Cycle.MoonPhase > 1f)
            {
                ecc.sky.Cycle.MoonPhase -= 2f;
            }
            if (ecc.sky.Cycle.Hour >= 24f)
            {
                ecc.sky.Cycle.Hour = 0f;
                int num5 = DateTime.DaysInMonth(ecc.sky.Cycle.Year, ecc.sky.Cycle.Month);
                if (++ecc.sky.Cycle.Day > num5)
                {
                    ecc.sky.Cycle.Day = 1;
                    if (++ecc.sky.Cycle.Month > 12)
                    {
                        ecc.sky.Cycle.Month = 1;
                        ecc.sky.Cycle.Year++;
                    }
                }
            }

            bool callHook = false;
            bool? previousCycleWasNight = null;
            if (_isNight == null || _isNight != ecc.IsNight())
            {
                previousCycleWasNight = _isNight;
                _isNight = ecc.IsNight();
                callHook = true;
            }
            
            if (callHook)
            {
                DayCycleChangeEvent ev = new DayCycleChangeEvent(ecc, previousCycleWasNight);
                try
                {
                    ExecuteSubscribers(OnDayCycleChanged, "DayCycleChangeEvent", ev);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"DayCycleChangeEvent Error: {ex}");
                }
            }
        }
        
        /// <summary>
        /// Runs when a player enters a HeatZone.
        /// It is called continually while the player remains in the zone.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="other"></param>
        public static void HeatZoneOnTriggerStay(HeatZone instance, Collider other)
        {
            using (new Stopper(nameof(Hooks), nameof(HeatZoneOnTriggerStay)))
            {
                if (!instance._isOn) 
                    return;
                
                Metabolism metabolism = instance.GetFromCollider(other);
                if (metabolism == null) 
                    return;

                HeatZoneEnterEvent hze = new HeatZoneEnterEvent(instance, other, metabolism);
                try
                {
                    ExecuteSubscribers(OnHeatZoneEnter, "HeatZoneEvent", hze);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"HeatZoneEvent Error: {ex}");
                }

                if (!hze.Cancelled)
                {
                    metabolism.MarkWarm();
                }
            }
        }
        
        /// <summary>
        /// Runs when a player enters a WorkZone.
        /// It is called continually while the player remains in the zone.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="other"></param>
        public static void WorkZoneOnTriggerStay(WorkZone instance, Collider other)
        {
            using (new Stopper(nameof(Hooks), nameof(WorkZoneOnTriggerStay)))
            {
                if (!instance._isOn) 
                    return;

                CraftingInventory craftingInv = instance.GetFromCollider(other);
                if (craftingInv == null)
                    return;

                WorkZoneEnterEvent wze = new WorkZoneEnterEvent(instance, other, craftingInv);
                try
                {
                    ExecuteSubscribers(OnWorkZoneEnter, "WorkZoneEvent", wze);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"WorkZoneEvent Error: {ex}");
                }

                if (!wze.Cancelled)
                {
                    craftingInv.MarkWorkBench();
                }
            }
        }


        /// <summary>
        /// Updates the metabolism state of the player and triggers metabolism-related events. This includes
        /// calculations of metabolic vitals, such as hunger and health, and ensures appropriate logic is
        /// executed depending on the player's current life status.
        /// </summary>
        /// <param name="m">The metabolism object representing the player's metabolic state.</param>
        /// <returns>A <see cref="LifeStatus"/> value indicating the life state of the player after the metabolic update</returns>
        public static LifeStatus MetabolicUpdateHook(Metabolism m)
        {
            using (new Stopper(nameof(Hooks), nameof(MetabolicUpdateHook)))
            {
                // Replicate original check, global::LifeStatus lifeStatus = ((!base.alive) ? global::LifeStatus.IsDead : global::LifeStatus.IsAlive);
                LifeStatus lifeStatus = ((!m.alive) ? LifeStatus.IsDead : LifeStatus.IsAlive);
        
                if (lifeStatus == LifeStatus.IsAlive)
                {
                    try
                    {
                        float time = Time.time;
                        float delta = time - m._lastTickTime;

                        // Original timing logic
                        if (delta > 0f && (m.selfTick || delta >= m.tickRate))
                        {
                            m._lastTickTime = time;
                            MetabolismEvent e = new MetabolismEvent(m, delta);
                            ExecuteSubscribers(OnMetabolismUpdate, "OnMetabolismUpdate", e);

                            if (e.Cancelled) 
                                return lifeStatus;

                            // Call the now-public CalculateMetabolicVitals
                            var vitalsUpdate = m.CalculateMetabolicVitals(delta);

                            if (vitalsUpdate.Changed)
                            {
                                if (vitalsUpdate.IsHurt)
                                {
                                    lifeStatus = TakeDamage.HurtSelf(m, vitalsUpdate.HurtAmount, null);
                                }
                                else
                                {
                                    m.takeDamage.Heal(m, vitalsUpdate.HealAmount);
                                }
                            }

                            if (lifeStatus == LifeStatus.IsAlive)
                            {
                                m.DoNetworkUpdate();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"MetabolismUpdate Error: {ex}");
                        lifeStatus = ((!m.dead) ? lifeStatus : LifeStatus.IsDead);
                    }
                }
                return lifeStatus;
            }
        }
        
        /// <summary>
        /// Runs on every server tick, right before the server updates the world state.
        /// </summary>
        public static void OnServerTickHook()
        {
            if (!ServerInitialized || IsShuttingDown)
                return;
            
            ExecuteSubscribers(OnServerTick, "OnServerTick");
        }

        /// <summary>
        /// Invokes a logger event with the specified event type and message.
        /// </summary>
        /// <param name="type">The type of logger event (Log, LogError, LogWarning).</param>
        /// <param name="message">The message associated with the logger event.</param>
        public static void LoggerEvent(LoggerEventType type, string message)
        {
            LoggerEvent evt = new LoggerEvent(type, message);
            ExecuteSubscribers(OnLogger, "OnLogger", evt);
        }

        /// <summary>
        /// Triggers when a WebSocket message is received.
        /// </summary>
        /// <param name="wsEvent">The WebSocketEvent containing the details of the received message.</param>
        public static void SocketMessageReceived(WebSocketEvent wsEvent)
        {
            ExecuteSubscribers(OnWebSocketMessage, "OnWebSocketMessage", wsEvent);
        }

        /// <summary>
        /// Triggered when a WebSocket connection is established.
        /// </summary>
        /// <param name="e">The event containing details about the WebSocket connection.</param>
        public static void SocketConnected(WebSocketEvent e)
        {
            ExecuteSubscribers(OnWebSocketConnected, "OnWebSocketConnected", e);
        }

        /// <summary>
        /// Invoked when a WebSocket connection is closed.
        /// </summary>
        /// <param name="e">The event object containing information about the closed WebSocket connection.</param>
        public static void SocketClosed(WebSocketEvent e)
        {
            ExecuteSubscribers(OnWebSocketClosed, "OnWebSocketClosed", e);
        }

        /// <summary>
        /// Triggers when a WebSocket error occurs.
        /// </summary>
        /// <param name="e">The WebSocketEvent containing the error details.</param>
        public static void SocketErrorEvent(WebSocketEvent e)
        {
            ExecuteSubscribers(OnWebSocketError, "OnWebSocketError", e);
        }

        /// <summary>
        /// Triggered when loot tables from the game are fully loaded.
        /// </summary>
        /// <param name="lists">A dictionary containing the loot tables, where the keys are table names and the values are LootSpawnList objects representing the loot data.</param>
        /// <returns>Returns the modified dictionary of loot tables after all subscribers have executed.</returns>
        public static Dictionary<string, LootSpawnList> TablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            ExecuteSubscribers(OnTablesLoaded, "OnTablesLoaded", lists);
            return lists;
        }
        
        /// <summary>
        /// Triggers the Inter-Plugin communication event.
        /// </summary>
        /// <param name="e">The message event container.</param>
        /// <returns>A response enum indicating the result of the delivery.</returns>
        internal static PluginMessageResponse PluginMessage(PluginMessageEvent e)
        {
            var pluginLoader = PluginLoader.GetInstance();
            
            BasePlugin target = pluginLoader.Plugins.Values.FirstOrDefault(p => string.Equals(p.Name, e.ReceiverName, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return PluginMessageResponse.TargetNotFound;
            }

            if (target.State != PluginState.Loaded)
            {
                return PluginMessageResponse.TargetDisabled;
            }

            // Dispatch to all subscribers
            bool wasAllSuccessful = ExecuteSubscribers(OnPluginMessage, "OnPluginMessage", e);
            if (wasAllSuccessful)
            {
                return e.Cancelled ? PluginMessageResponse.Rejected : PluginMessageResponse.Success;
            }
            
            return PluginMessageResponse.Error;
        }

        /// <summary>
        /// Runs when a command or console command is being restricted / unrestricted.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="restrictionType"></param>
        /// <param name="restrictionScale"></param>
        /// <param name="command"></param>
        /// <param name="isBeingRestricted"></param>
        /// <returns></returns>
        internal static bool RestrictionChange(Player player, CommandRestrictionType restrictionType,
            CommandRestrictionScale restrictionScale, string command, bool isBeingRestricted)
        {
            CommandRestrictionEvent commandRestrictionEvent = new CommandRestrictionEvent(player, command,
                restrictionType, restrictionScale, isBeingRestricted);

            try
            {
                ExecuteSubscribers(OnCommandRestriction, "RestrictionChangeEvent", commandRestrictionEvent);
            }
            catch (Exception ex)
            {
                Logger.LogError($"RestrictionChangeEvent Error: {ex}");
            }

            return commandRestrictionEvent.Cancelled;
        }
    }
}