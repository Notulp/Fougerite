namespace RustPP
{
    using Fougerite;
    using Fougerite.Events;
    using Rust;
    using RustPP.Commands;
    using System;
    using System.Collections;
    using System.IO;
    using System.Timers;

    public class RustPPModule : Fougerite.Module
    {
        public override string Name
        {
            get { return "RustPP"; }
        }

        public override string Author
        {
            get { return "xEnt22, mikec, DreTaX"; }
        }

        public override string Description
        {
            get { return "Rust++ Legacy Module"; }
        }

        public override Version Version
        {
            get { return new Version("1.1.8.1"); }
        }

        public static string GetAbsoluteFilePath(string fileName)
        {
            return Path.Combine(ConfigsFolder, fileName);
        }

        public override uint Order
        {
            get { return uint.MinValue; }
        }

        public static string ConfigFile;
        public static string ConfigsFolder;
        public static string JoinMsg = "has joined the server";
        public static string LeaveMsg = "has left the server";
        public static string StarterMsg = "You have spawned a Starter Kit!";
        public static string StarterCDMsg = "You must wait awhile before using this..";

        public override void Initialize()
        {
            ConfigsFolder = ModuleFolder;
            ConfigFile = Path.Combine(ConfigsFolder, "Rust++.cfg");
            Core.Init();
            Core.config = new IniParser(ConfigFile);

            if (Core.config == null)
            {
                Logger.LogError("[RPP] Can't load config!");
                return;
            }

            string chatname = Core.config.GetSetting("Settings", "chatname");
            if (!string.IsNullOrEmpty(chatname))
            {
                Core.Name = Core.config.GetSetting("Settings", "chatname");
            }

            if (Core.config.ContainsSetting("Settings", "joinmsg"))
            {
                JoinMsg = Core.config.GetSetting("Settings", "joinmsg");
            }

            if (Core.config.ContainsSetting("Settings", "leavemsg"))
            {
                LeaveMsg = Core.config.GetSetting("Settings", "leavemsg");
            }

            if (Core.config.ContainsSetting("Settings", "startermsg"))
            {
                StarterMsg = Core.config.GetSetting("Settings", "startermg");
            }

            if (Core.config.ContainsSetting("Settings", "startercdmsg"))
            {
                StarterCDMsg = Core.config.GetSetting("Settings", "startercdmsg");
            }

            TimedEvents.startEvents();

            Fougerite.Hooks.OnDoorUse += DoorUse;
            Fougerite.Hooks.OnEntityHurt += EntityHurt;
            Fougerite.Hooks.OnPlayerConnected += PlayerConnect;
            Fougerite.Hooks.OnPlayerDisconnected += PlayerDisconnect;
            Fougerite.Hooks.OnPlayerHurt += PlayerHurt;
            Fougerite.Hooks.OnPlayerKilled += PlayerKilled;
            Fougerite.Hooks.OnServerShutdown += OnServerShutdown;
            Fougerite.Hooks.OnShowTalker += ShowTalker;
            Fougerite.Hooks.OnChatRaw += ChatReceived;
            Fougerite.Hooks.OnChat += Chat;
            Fougerite.Hooks.OnFallDamage += OnFallDamage;
            Fougerite.Hooks.OnServerSaved += OnServerSaved;
            Server.GetServer().LookForRustPP();
        }

        public override void DeInitialize()
        {
            Logger.LogDebug("DeInitialize RPP");

            Fougerite.Hooks.OnDoorUse -= DoorUse;
            Fougerite.Hooks.OnEntityHurt -= EntityHurt;
            Fougerite.Hooks.OnPlayerConnected -= PlayerConnect;
            Fougerite.Hooks.OnPlayerDisconnected -= PlayerDisconnect;
            Fougerite.Hooks.OnPlayerHurt -= PlayerHurt;
            Fougerite.Hooks.OnPlayerKilled -= PlayerKilled;
            Fougerite.Hooks.OnServerShutdown -= OnServerShutdown;
            Fougerite.Hooks.OnShowTalker -= ShowTalker;
            Fougerite.Hooks.OnChatRaw -= ChatReceived;
            Fougerite.Hooks.OnChat -= Chat;
            Fougerite.Hooks.OnFallDamage -= OnFallDamage;
            Fougerite.Hooks.OnServerSaved -= OnServerSaved;
            TimedEvents.timer.Stop();
            Logger.LogDebug("DeInitialized RPP");
        }

        void OnServerSaved(int amount, double seconds)
        {
            Helper.CreateSaves();
        }

        void TimeEvent(object x, ElapsedEventArgs y)
        {
            TimedEvents.startEvents();
        }

        void ChatReceived(ref ConsoleSystem.Arg arg)
        {
            var pl = Server.GetServer().GetCachePlayer(arg.argUser.userID);
            var command = ChatCommand.GetCommand("tpto") as TeleportToCommand;
            if (command != null && command.GetTPWaitList().Contains(pl.UID))
            {
                command.PartialNameTP(ref arg, arg.GetInt(0));
                arg.ArgsStr = string.Empty;
            }
            else if (Core.friendWaitList.Contains(arg.argUser.userID))
            {
                (ChatCommand.GetCommand("addfriend") as AddFriendCommand)?.PartialNameAddFriend(ref arg, arg.GetInt(0));
                Core.friendWaitList.Remove(arg.argUser.userID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.shareWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("share") as ShareCommand)?.PartialNameDoorShare(ref arg, arg.GetInt(0));
                Core.shareWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.banWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("ban") as BanCommand)?.PartialNameBan(ref arg, arg.GetInt(0));
                Core.banWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.kickWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("kick") as KickCommand)?.PartialNameKick(ref arg, arg.GetInt(0));
                Core.kickWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.killWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("kill") as KillCommand)?.PartialNameKill(ref arg, arg.GetInt(0));
                Core.killWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.unfriendWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("unfriend") as UnfriendCommand)?.PartialNameUnfriend(ref arg, arg.GetInt(0));
                Core.unfriendWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.unshareWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("unshare") as UnshareCommand)?.PartialNameUnshareDoors(ref arg, arg.GetInt(0));
                Core.unshareWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.whiteWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("addwl") as WhiteListAddCommand)?.PartialNameWhitelist(ref arg, arg.GetInt(0));
                Core.whiteWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.adminAddWaitList.Contains(arg.argUser.userID))
            {
                (ChatCommand.GetCommand("addadmin") as AddAdminCommand)?.PartialNameNewAdmin(ref arg, arg.GetInt(0));
                Core.adminAddWaitList.Remove(arg.argUser.userID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.adminRemoveWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("unadmin") as RemoveAdminCommand)?.PartialNameRemoveAdmin(ref arg, arg.GetInt(0));
                Core.adminRemoveWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.adminFlagsWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("getflags") as GetFlagsCommand)?.PartialNameGetFlags(ref arg, arg.GetInt(0));
                Core.adminFlagsWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.muteWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("mute") as MuteCommand)?.PartialNameMute(ref arg, arg.GetInt(0));
                Core.muteWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.unmuteWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("unmute") as UnmuteCommand)?.PartialNameUnmute(ref arg, arg.GetInt(0));
                Core.unmuteWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.adminFlagWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("addflag") as AddFlagCommand)?.PartialNameAddFlags(ref arg, arg.GetInt(0));
                Core.adminFlagWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.adminUnflagWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("unflag") as RemoveFlagsCommand)?.PartialNameRemoveFlags(ref arg, arg.GetInt(0));
                Core.adminUnflagWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }
            else if (Core.unbanWaitList.Contains(pl.UID))
            {
                (ChatCommand.GetCommand("unban") as UnbanCommand)?.PartialNameUnban(ref arg, arg.GetInt(0));
                Core.unbanWaitList.Remove(pl.UID);
                arg.ArgsStr = string.Empty;
            }

            if (Core.IsEnabled())
                Core.handleCommand(ref arg);
        }

        void Chat(Fougerite.Player p, ref ChatString text)
        {
            if (Core.IsEnabled() && Core.muteList.Contains(p.UID))
            {
                text.NewText = "";
                p.MessageFrom(Core.Name, "You are muted.");
            }
        }

        void OnFallDamage(FallDamageEvent falldamageevent)
        {
            if (Core.IsEnabled())
            {
                if (falldamageevent.Player != null)
                {
                    GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
                    if (command.IsOn(falldamageevent.Player.UID))
                    {
                        falldamageevent.Cancel();
                    }
                }
            }
        }

        void ShowTalker(uLink.NetworkPlayer player, Fougerite.Player p)
        {
            if (!Core.IsEnabled())
                return;

            if (!Core.config.GetBoolSetting("Settings", "voice_notifications"))
                return;

            if (Fougerite.Hooks.talkerTimers.ContainsKey(p.UID))
            {
                if ((Environment.TickCount - ((int)Fougerite.Hooks.talkerTimers[p.UID])) <
                    int.Parse(Core.config.GetSetting("Settings", "voice_notification_delay")))
                    return;

                Fougerite.Hooks.talkerTimers[p.UID] = Environment.TickCount;
            }
            else
            {
                Fougerite.Hooks.talkerTimers.Add(p.UID, Environment.TickCount);
            }

            Notice.Inventory(player, "☎ " + p.Name);
        }

        void OnServerShutdown()
        {
            if (Core.IsEnabled()) Helper.CreateSaves();
        }

        void PlayerKilled(DeathEvent event2)
        {
            event2.DropItems = !RustPP.Hooks.KeepItem();
            if (event2.AttackerIsPlayer && event2.VictimIsPlayer)
            {
                if (event2.Attacker == null || event2.Victim == null)
                {
                    return;
                }

                Fougerite.Player attacker = event2.Attacker as Fougerite.Player;
                Fougerite.Player victim = event2.Victim as Fougerite.Player;
                if (attacker != victim)
                    RustPP.Hooks.broadcastDeath(victim.Name, attacker.Name, event2.WeaponName);
            }
        }

        void DoorUse(Fougerite.Player p, DoorEvent de)
        {
            if (Core.IsEnabled() && !de.Open)
            {
                ShareCommand command = ChatCommand.GetCommand("share") as ShareCommand;
                ArrayList list = (ArrayList)command.GetSharedDoors()[Convert.ToUInt64(de.Entity.OwnerID)];
                if (list == null)
                    de.Open = false;
                else if (list.Contains(p.UID))
                    de.Open = true;
                else
                    de.Open = false;
            }
        }

        void PlayerHurt(HurtEvent he)
        {
            if (RustPP.Hooks.IsFriend(he))
                he.DamageAmount = 0f;
        }

        void PlayerDisconnect(Fougerite.Player player)
        {
            if (Core.IsEnabled())
                RustPP.Hooks.logoffNotice(player);
        }

        void PlayerConnect(Fougerite.Player player)
        {
            if (Core.IsEnabled())
                RustPP.Hooks.loginNotice(player);
        }

        void EntityHurt(HurtEvent he)
        {
            if (Core.IsEnabled())
            {
                if (!he.AttackerIsPlayer || he.Attacker == null)
                {
                    return;
                }

                InstaKOCommand command = ChatCommand.GetCommand("instako") as InstaKOCommand;
                InstaKOAllCommand command2 = ChatCommand.GetCommand("instakoall") as InstaKOAllCommand;
                Fougerite.Player pl = (Fougerite.Player)he.Attacker;
                if (command != null)
                {
                    if (command.IsOn(pl.UID))
                    {
                        if (he.Entity != null)
                        {
                            try
                            {
                                if (!he.IsDecay)
                                    he.Entity.Destroy();
                                else if (Fougerite.Hooks.decayList.Contains(he.Entity))
                                    Fougerite.Hooks.decayList.Remove(he.Entity);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogDebug("EntityHurt EX: " + ex);
                            }
                        }
                        else Logger.LogDebug("he.Entity is null!");
                    }
                }

                if (command2 != null)
                {
                    if (command2.IsOn(pl.UID))
                    {
                        if (he.Entity != null)
                        {
                            var list = he.Entity.GetLinkedStructs();
                            foreach (var x in list)
                            {
                                try
                                {
                                    x.Destroy();
                                    if (Fougerite.Hooks.decayList.Contains(x))
                                        Fougerite.Hooks.decayList.Remove(x);
                                }
                                catch
                                {
                                }
                            }

                            try
                            {
                                he.Entity.Destroy();
                                if (Fougerite.Hooks.decayList.Contains(he.Entity))
                                    Fougerite.Hooks.decayList.Remove(he.Entity);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogDebug("EntityHurt EX: " + ex);
                            }
                        }
                        else Logger.LogDebug("he.Entity is null!");
                    }
                }
            }
        }
    }
}