using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Fougerite.Caches;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;
using Rust;
using uLink;
using Fougerite.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
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
                    if (OnAllPluginsLoaded != null)
                    {
                        OnAllPluginsLoaded();
                    }
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
                            OnBlueprintUse(player, ae);
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
                            OnCommand(player, command, cargs);
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
                    if (string.IsNullOrEmpty(chatstr.NewText))
                    {
                        return;
                    }

                    string newchat = Facepunch.Utility.String
                        .QuoteSafe(chatstr.NewText.Substring(1, chatstr.NewText.Length - 2))
                        .Replace("\\\"", "\"");

                    // Check for empty text again
                    if (string.IsNullOrEmpty(newchat))
                    {
                        return;
                    }

                    string s = Regex.Replace(newchat, @"\[/?color\b.*?\]", string.Empty);
                    if (s.Length <= 100)
                    {
                        Data.GetData().chat_history.Add(chatstr);
                        Data.GetData().chat_history_username.Add(quotedName);
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
                        Data.GetData().chat_history.Add(x);
                        Data.GetData().chat_history_username.Add(quotedName);

                        ConsoleNetworker.Broadcast(i == 1
                            ? $"chat.add {quotedName} \"{arr[arr.Length - 1]}{x}"
                            : $"chat.add {quotedName} {x}\"");

                        i++;
                    }
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

                if (bWantReply)
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
                            string[] textArray2 = new string[]
                                { "Error: ", arg.Class, ".", arg.Function, " - ", exception.Message };
                            Debug.LogWarning(string.Concat(textArray2));
                            string[] textArray3 = new string[]
                                { "Error: ", arg.Class, ".", arg.Function, " - ", exception.Message };
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
                            foreach (string x in PluginLoader.GetInstance().Plugins.Keys)
                            {
                                if (string.Equals(x, plugin, StringComparison.OrdinalIgnoreCase))
                                {
                                    PluginLoader.GetInstance().ReloadPlugin(x);
                                    a.ReplyWith($"Fougerite: Plugin {x} reloaded!");
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
                            foreach (string x in PluginLoader.GetInstance().Plugins.Keys)
                            {
                                if (string.Equals(x, plugin, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (PluginLoader.GetInstance().Plugins[x].State == PluginState.Loaded)
                                    {
                                        PluginLoader.GetInstance().UnloadPlugin(x);
                                        a.ReplyWith($"Fougerite: UnLoaded {x}!");
                                    }
                                    else
                                    {
                                        a.ReplyWith($"Fougerite: {x} is already unloaded!");
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

                if (string.IsNullOrEmpty(a.Reply))
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
                        OnDoorUse(Server.GetServer().FindPlayer(controllable.playerClient.userID), de);
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
                    Entity ent = new Entity(entity);
                    DecayEvent de = new DecayEvent(ent, ref dmg);
                    try
                    {
                        if (OnEntityDecay != null)
                            OnEntityDecay(de);
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
                Entity e = new Entity(entity);
                uLink.NetworkPlayer nplayer = info.sender;
                Player creator = e.Creator;
                var data = nplayer.GetLocalData();
                Player ActualPlacer = null;
                if (data is NetUser user)
                {
                    ActualPlacer = Server.GetServer().FindPlayer(user.userID);
                }

                try
                {
                    if (OnEntityDeployed != null)
                        OnEntityDeployed(creator, e);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"EntityDeployedEvent Error: {ex}");
                }

                try
                {
                    if (OnEntityDeployedWithPlacer != null)
                        OnEntityDeployedWithPlacer(creator, e, ActualPlacer);
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
                        if (OnPlayerHurt != null)
                        {
                            OnPlayerHurt(he);
                        }
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
                        if (OnPlayerHurt != null)
                        {
                            OnPlayerHurt(he);
                        }
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
                            if (OnNPCHurt != null)
                            {
                                OnNPCHurt(he);
                            }
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
                                    if (OnNPCKilled != null)
                                    {
                                        OnNPCKilled(de);
                                    }
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
                    var ent = he.Entity;
                    // Double validate this weird logic...
                    if (!he.IsDecay && DecayList.ContainsKey(he.Entity.InstanceID))
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
                                    if (OnEntityDestroyed != null)
                                    {
                                        OnEntityDestroyed(de2);
                                    }
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
                                    if (OnEntityDestroyed != null)
                                    {
                                        OnEntityDestroyed(de22);
                                    }
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
                    if (OnShowTalker != null)
                        OnShowTalker(p.netPlayer, pl);
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
                    if (OnItemsLoaded != null)
                        OnItemsLoaded(blocks);
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
                    if (OnItemPickup != null)
                    {
                        OnItemPickup(ipe);
                    }
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
                            if (OnItemPickup != null)
                            {
                                OnItemPickup(aftercall);
                            }
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
                            if (OnItemPickup != null)
                            {
                                OnItemPickup(aftercall);
                            }
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
                            if (OnItemPickup != null)
                            {
                                OnItemPickup(aftercall);
                            }
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
                    if (OnItemPickup != null)
                    {
                        OnItemPickup(aftercall);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ItemPickupEvent Error: {ex}");
                }

                return true;
            }
        }

        public static void FallDamage(FallDamage fd, float speed, float num, bool flag, bool flag2)
        {
            using (new Stopper(nameof(Hooks), nameof(FallDamage)))
            {
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
                    Logger.LogError($"FallDamageEvent Error: {ex}");
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
                    if (OnPlayerConnected != null)
                    {
                        OnPlayerConnected(player);
                    }
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
                Player player = Server.GetServer().GetCachePlayer(uid);
                if (player == null)
                {
                    Server.GetServer().RemovePlayer(uid);
                    Logger.LogWarning(
                        $"[WeirdDisconnect] Player was null at the disconnection. Something might be wrong? OPT: {Bootstrap.CR}");
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
                    if (OnPlayerDisconnected != null)
                    {
                        OnPlayerDisconnected(player);
                    }
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
                    Logger.LogError($"PlayerGatherEvent Error: {ex}");
                }
            }
        }

        public static void PlayerGatherWood(IMeleeWeaponItem rec, ResourceTarget rt, ref ItemDataBlock db,
            ref int amount, ref string name)
        {
            using (new Stopper(nameof(Hooks), nameof(PlayerGatherWood)))
            {
                Player player = Player.FindByNetworkPlayer(rec.inventory.networkView.owner);
                GatherEvent ge = new GatherEvent(rt, db, amount)
                {
                    Item = "Wood"
                };

                try
                {
                    if (OnPlayerGathering != null)
                    {
                        OnPlayerGathering(player, ge);
                    }

                    db = Server.GetServer().Items.Find(ge.Item);
                    amount = ge.Quantity;
                    name = ge.Item;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerGatherWoodEvent Error: {ex}");
                }
            }
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
                    if (OnPlayerKilled != null)
                        OnPlayerKilled(event2);

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
                    if (OnPlayerSpawned != null && player != null)
                    {
                        OnPlayerSpawned(player, se);
                    }
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
                    if (OnPlayerSpawning != null && player != null)
                    {
                        OnPlayerSpawning(player, se);
                    }

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
                    if (OnPluginInit != null)
                    {
                        OnPluginInit();
                    }
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
                    if (OnPlayerTeleport != null)
                    {
                        OnPlayerTeleport(player, from, dest);
                    }
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
                    if (OnCrafting != null)
                    {
                        OnCrafting(e);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"CraftingEvent Error: {ex}");
                }
            }
        }

        public static void AnimalMovement(BaseAIMovement m, BasicWildLifeAI ai, ulong simMillis)
        {
            using (new Stopper(nameof(Hooks), nameof(AnimalMovement)))
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
        }

        public static void ResourceSpawned(ResourceTarget target)
        {
            using (new Stopper(nameof(Hooks), nameof(ResourceSpawned)))
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
                    if (OnBowShoot != null)
                    {
                        BowShootEvent se = new BowShootEvent(db, rep, info, bwi);
                        OnBowShoot(se);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"BowShootEvent Error: {ex}");
                }
            }
        }

        
        public static void GrenadeEvent(HandGrenadeDataBlock hgd, uLink.BitStream stream, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info)
        {
            using (new Stopper(nameof(Hooks), nameof(GrenadeEvent)))
            {
                IHandGrenadeItem item;
                bool proceed = true;
                try
                {
                    NetCull.VerifyRPC(ref info);
                }
                catch (Exception)
                {
                    proceed = false;
                }

                if (proceed && rep.Item<IHandGrenadeItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
                {
                    rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                    Vector3 origin = stream.ReadVector3();
                    Vector3 forward = stream.ReadVector3();

                    // Sanity checks.
                    if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) || float.IsNaN(origin.y)
                        || float.IsInfinity(origin.y) || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
                    {
                        return;
                    }

                    if (float.IsNaN(forward.x) || float.IsInfinity(forward.x) || float.IsNaN(forward.y)
                        || float.IsInfinity(forward.y) || float.IsNaN(forward.z) || float.IsInfinity(forward.z))
                    {
                        return;
                    }

                    GameObject obj2 = hgd.ThrowItem(rep, origin, forward);
                    if (obj2 != null)
                    {
                        obj2.rigidbody.AddTorque(new Vector3(
                            UnityEngine.Random.Range(-1f, 1f),
                            UnityEngine.Random.Range(-1f, 1f),
                            UnityEngine.Random.Range(-1f, 1f)) * 10f);
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

        public static void OnServerSaveEvent(int amount, double seconds)
        {
            using (new Stopper(nameof(Hooks), nameof(OnServerSaveEvent)))
            {
                try
                {
                    if (OnServerSaved != null)
                    {
                        OnServerSaved(amount, seconds);
                    }
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
                                  !ReferenceEquals((object)inventoryItem, (object)match)) ||
                    !collection.Evict(slot, out inventoryItem))
                {
                    return false;
                }

                InventoryModEvent e = null;
                try
                {
                    e = new InventoryModEvent(inv, slot, inventoryItem.iface, "Remove");
                    if (OnItemRemoved != null)
                    {
                        OnItemRemoved(e);
                    }
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
                    e = new InventoryModEvent(args.inventory, args.slot, args.item.iface, "Add");
                    if (OnItemAdded != null)
                    {
                        OnItemAdded(e);
                    }
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
                    if (OnAirdropCalled != null)
                    {
                        OnAirdropCalled(v);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"AirdropEvent Error: {ex}");
                }
            }
        }

        public static void Airdrop2(SupplyDropZone srz)
        {
            using (new Stopper(nameof(Hooks), nameof(Airdrop2)))
            {
                try
                {
                    if (OnAirdropCalled != null)
                    {
                        OnAirdropCalled(srz.GetSupplyTargetPosition());
                    }
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
                    if (OnSupplyDropPlaneCreated != null)
                    {
                        OnSupplyDropPlaneCreated(plane);
                    }
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
                    if (OnAirdropCrateDropped != null)
                    {
                        OnAirdropCrateDropped(plane, entity);
                    }
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
                    if (OnSteamDeny != null)
                    {
                        OnSteamDeny(sde);
                    }
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
                            var client = user.playerClient;
                            var loc = user.playerClient.lastKnownPosition;

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
                        Debug.Log((object)("Denying entry to client with invalid protocol version (" + ip + ")"));
                        approval.Deny(uLink.NetworkConnectionError.IncompatibleVersions);
                    }
                    else if (BanList.Contains(uid))
                    {
                        Debug.Log((object)("Rejecting client (" + uid + "in banlist)"));
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
                            Logger.LogError($"PlayerApprovalEvent2 Error: {ex}");
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
                if (OnPlayerMove != null)
                {
                    OnPlayerMove(hc, origin, encoded, stateFlags, info, action);
                }
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
                                NetCull.RPC((UnityEngine.MonoBehaviour)lo, "StopLooting", uLink.RPCMode.Server);
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
                uLink.NetworkPlayer ulinkuser = uLink.NetworkView.Get((UnityEngine.MonoBehaviour)use.user).owner;
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
                                    var ulinkuser = uLink.NetworkView.Get((UnityEngine.MonoBehaviour)use.user).owner;
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
                                                if (OnLootUse != null)
                                                {
                                                    OnLootUse(lt);
                                                }
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
                            if (OnItemMove != null)
                            {
                                OnItemMove(ime2);
                            }
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
                            if (OnItemMove != null)
                            {
                                OnItemMove(ime3);
                            }
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
                if (OnItemMove != null)
                {
                    OnItemMove(ime);
                }
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
                    if (OnRepairBench != null)
                    {
                        OnRepairBench(re);
                    }
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
                    if (OnPlayerBan != null)
                    {
                        OnPlayerBan(be);
                    }
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
                    if (OnGenericSpawnerLoad != null)
                    {
                        OnGenericSpawnerLoad(gs);
                    }
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
                if (OnServerLoaded != null)
                {
                    OnServerLoaded();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"ServerLoaded Error: {ex}");
            }
            
            Logger.Log("Server Initialized.");
            UnityEngine.Object.Destroy(init.gameObject);
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
                        if (OnBeltUse != null)
                        {
                            OnBeltUse(be);
                        }
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
                    if (OnShoot != null)
                    {
                        ShootEvent se = new ShootEvent(instance, obj2, rep, info, item, part, flag, flag2, flag3, part2,
                            vector, vector2);
                        OnShoot(se);
                    }
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
                        obj2.rigidbody.AddTorque(new Vector3(
                            UnityEngine.Random.Range(-1f, 1f),
                            UnityEngine.Random.Range(-1f, 1f),
                            UnityEngine.Random.Range(-1f, 1f)) * 10f);
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
                            if (OnShotgunShoot != null)
                            {
                                ShotgunShootEvent se = new ShotgunShootEvent(instance, rep, info, found,
                                    ShotgunEventType.AfterShot, part, flag, flag2, flag3, part2, vector, vector2);
                                OnShotgunShoot(se);
                            }
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
                if (OnServerShutdown != null)
                    OnServerShutdown();
            }
            catch (Exception ex)
            {
                Logger.LogError($"ServerShutdownEvent Error: {ex}");
            }

            World.GetWorld().ServerSaveHandler.ManualSave();
        }
        
        internal static void ModulesLoaded()
        {
            using (new Stopper(nameof(Hooks), nameof(ModulesLoaded)))
            {
                try
                {
                    if (OnModulesLoaded != null)
                        OnModulesLoaded();
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
                    DataStore.GetInstance().Load();
                    Server.GetServer().UpdateBanlist();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"ServerInitEvent Critical Error: {ex}");
                }

                try
                {
                    if (OnServerInit != null)
                        OnServerInit();
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
                    if (OnNPCSpawned != null)
                        OnNPCSpawned(npc);
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
                    Debug.LogError("Could not get view, will not be dynamic group " + instance, instance);
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
                Debug.LogError("No NGC Prefab with name:" + args.prefabName);
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
                // BasicDoor is a DeployableObject anyway
                else if (gameObject.GetComponent<BasicDoor>() != null)
                {
                    underLying = gameObject.GetComponent<BasicDoor>();
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
            // BasicDoor is a DeployableObject anyway
            else if (go.GetComponent<BasicDoor>() != null)
            {
                underLying = go.GetComponent<BasicDoor>();
                id = ((BasicDoor) underLying).GetInstanceID();
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
                    // BasicDoor is a DeployableObject anyway
                    else if (go.GetComponent<BasicDoor>() != null)
                    {
                        underLying = go.GetComponent<BasicDoor>();
                        id = ((BasicDoor) underLying).GetInstanceID();
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
            // BasicDoor is a DeployableObject anyway
            else if (go.GetComponent<BasicDoor>() != null)
            {
                underLying = go.GetComponent<BasicDoor>();
                id = ((BasicDoor) underLying).GetInstanceID();
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
                    if (OnTimedExplosiveSpawned != null)
                        OnTimedExplosiveSpawned(timedExplosiveEvent);
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
                Sleeper sleeper = new Sleeper(deployableObject);
                SleeperCache.GetInstance().Add(sleeper);
                
                try
                {
                    if (OnSleeperSpawned != null)
                        OnSleeperSpawned(sleeper);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"SleeperSpawnedEvent Error: {ex}");
                }
            }

            return true;
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
                if (OnCommandRestriction != null)
                {
                    OnCommandRestriction(commandRestrictionEvent);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"RestrictionChangeEvent Error: {ex}");
            }

            return commandRestrictionEvent.Cancelled;
        }
    }
}