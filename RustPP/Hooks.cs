
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace RustPP
{
    using Fougerite;
    using RustPP.Commands;
    using RustPP.Permissions;
    using RustPP.Social;
    using System.Collections;
    using System.Linq;
    using System.Security;
    using Fougerite.Events;

    internal class Hooks
    {
        public static void broadcastDeath(string victim, string killer, string weapon)
        {
            if (Core.config.GetBoolSetting("Settings", "pvp_death_broadcast"))
            {
                char ch = '⊕';
                Server.GetServer().BroadcastFrom(Core.Name, killer + " " + ch.ToString() + " " + victim + " (" + weapon + ")");
            }
        }

        public static bool checkOwner(DeployableObject obj, Controllable controllable)
        {
            bool flag;
            var pl = Server.GetServer().GetCachePlayer(controllable.playerClient.userID);
            if (obj.ownerID == pl.UID)
            {
                flag = true;
            }
            else if (obj is SleepingBag)
            {
                flag = false;
            }
            else
            {
                ShareCommand command = ChatCommand.GetCommand("share") as ShareCommand;
                ArrayList list = (ArrayList)command.GetSharedDoors()[obj.ownerID];
                if (list == null)
                {
                    flag = false;
                }
                else if (list.Contains(pl.UID))
                {
                    flag = true;
                }
                flag = false;
            }
            return flag;
        }

        public static void deployableKO(DeployableObject dep, DamageEvent e)
        {
            try
            {
                InstaKOCommand command = ChatCommand.GetCommand("instako") as InstaKOCommand;
                if (command.IsOn(e.attacker.client.userID))
                {
                    try
                    {
                        Helper.Log("StructDestroyed.txt", string.Concat(new object[] { e.attacker.client.netUser.displayName, " [", e.attacker.client.netUser.userID, "] destroyed (InstaKO) ", NetUser.FindByUserID(dep.ownerID).displayName, "'s ", dep.gameObject.name.Replace("(Clone)", "") }));
                    }
                    catch
                    {
                        if (Core.userCache.ContainsKey(dep.ownerID))
                        {
                            Helper.Log("StructDestroyed.txt", string.Concat(new object[] { e.attacker.client.netUser.displayName, " [", e.attacker.client.netUser.userID, "] destroyed (InstaKO) ", Core.userCache[dep.ownerID], "'s ", dep.gameObject.name.Replace("(Clone)", "") }));
                        }
                    }
                    dep.OnKilled();
                }
                else
                {
                    dep.UpdateClientHealth();
                }
            }
            catch
            {
                dep.UpdateClientHealth();
            }
        }

        public static bool IsFriend(HurtEvent e) // ref
        {
            GodModeCommand command = (GodModeCommand)ChatCommand.GetCommand("god");
            Fougerite.Player victim = e.Victim as Fougerite.Player;
            if (victim != null)
            {
                if (command.IsOn(victim.UID))
                {
                    FallDamage dmg = victim.FallDamage;
                    if (dmg != null)
                    {
                        dmg.ClearInjury();
                    }
                    return true;
                }
                Fougerite.Player attacker = e.Attacker as Fougerite.Player;
                if (attacker != null)
                {
                    FriendsCommand command2 = (FriendsCommand) ChatCommand.GetCommand("friends");
                    if (command2.ContainsException(attacker.UID) && command2.ContainsException(victim.UID))
                    {
                        return false;
                    }
                    bool b = Core.config.GetBoolSetting("Settings", "friendly_fire");
                    try
                    {
                        FriendList list = (FriendList) command2.GetFriendsLists()[attacker.UID];
                        if (list == null || b)
                        {
                            return false;
                        }
                        return list.isFriendWith(victim.UID);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public static bool KeepItem()
        {
            return Core.config.GetBoolSetting("Settings", "keepitems");
        }

        public static bool loginNotice(Fougerite.Player pl)
        {
            try
            {
                string name = SecurityElement.Escape(pl.Name);
                name = Util.GetUtil().ToValidXmlCharactersString(name);
                
                if (Core.blackList.Contains(pl.UID))
                {
                    Core.tempConnect.Add(pl.UID);
                    pl.Disconnect();
                    return false;
                }
                if (Core.config.GetBoolSetting("WhiteList", "enabled") && !Core.whiteList.Contains(pl.UID))
                {
                    pl.Disconnect();
                    return false;
                }
                if (!Core.userCache.ContainsKey(pl.UID))
                {
                    Core.userCache.Add(pl.UID, name);
                }
                else if (pl.Name != Core.userCache[pl.UID])
                {
                    Core.userCache[pl.UID] = name;
                }
                if (Administrator.IsAdmin(pl.UID) && Administrator.GetAdmin(pl.UID).HasPermission("RCON"))
                {
                    pl.PlayerClient.netUser.admin = true;
                }
                Core.motd(pl);
                if (Core.config.GetBoolSetting("Settings", "join_notice"))
                {
                    foreach (var client in Fougerite.Server.GetServer().Players.Where(client => client.UID != pl.UID))
                    {
                        client.MessageFrom(Core.Name, pl.Name + " " + RustPPModule.JoinMsg);
                    }
                }
            }
            catch
            {
            }
            return true;
        }

        public static void logoffNotice(Fougerite.Player user)
        {
            try
            {
                if (Core.tempConnect.Contains(user.UID))
                {
                    Core.tempConnect.Remove(user.UID);
                }
                else if (Core.config.GetBoolSetting("Settings", "leave_notice"))
                {
                    foreach (var client in Fougerite.Server.GetServer().Players.Where(client => client.UID != user.UID))
                    {
                        client.MessageFrom(Core.Name, user.Name + " " + RustPPModule.LeaveMsg);
                    }
                }
            }
            catch { }
        }

        public static void structureKO(StructureComponent sc, DamageEvent e)
        {
            try
            {
                InstaKOCommand command = ChatCommand.GetCommand("instako") as InstaKOCommand;
                if (command.IsOn(e.attacker.client.userID))
                {
                    sc.StartCoroutine("DelayedKill");
                }
                else
                {
                    sc.UpdateClientHealth();
                }
            }
            catch
            {
                sc.UpdateClientHealth();
            }
        }
    }
}