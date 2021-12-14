using System;
using System.Collections.Generic;
using Fougerite;
using Fougerite.Permissions;

namespace PermissionManager
{
    public class PermissionManager : Fougerite.Module
    {
        public override string Name
        {
            get { return "PermissionManager"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override string Description
        {
            get { return "PermissionManager"; }
        }

        public override Version Version
        {
            get { return new Version("1.0"); }
        }

        public override void Initialize()
        {
            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
        }

        private string[] Merge(string[] array, int fromindex)
        {
            string[] newarr = new string[array.Length - fromindex];
            int fromstorage = fromindex;
            for (int i = 0; i < newarr.Length; i++)
            {
                newarr[i] = array[fromstorage];
                fromstorage++;
            }

            return newarr;
        }

        private void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            switch (cmd)
            {
                // For players
                case "pem":
                {
                    PermissionSystem permissionSystem = PermissionSystem.GetPermissionSystem();
                    if (!player.Admin && !permissionSystem.PlayerHasPermission(player, "pem.admin")) return;
                    if (args.Length == 0)
                    {
                        player.MessageFrom("PermissionSystem", "=== PermissionSystem v" + Version + " ===");
                        return;
                    }

                    #region PermissionPlayerHandling
                    string secondcommand = args[0];
                    switch (secondcommand)
                    {
                        case "reload":
                        {
                            permissionSystem.ReloadPermissions();
                            player.MessageFrom("PermissionSystem", "Done!");
                            break;
                        }
                        case "newplayer":
                        {
                            if (args.Length <= 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pem newplayer playername");
                                return;
                            }
                            
                            string playername = string.Join(" ", Merge(args, 1)).Trim();
                            Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(playername);
                            if (pl != null)
                            {
                                permissionSystem.CreatePermissionPlayer(pl);
                                player.MessageFrom("PermissionSystem", "Permissions can now be assigned to this player!");
                            }
                            else
                            {
                                player.MessageFrom("PermissionSystem", playername + " not found!");
                            }

                            break;
                        }
                        case "delplayer":
                        {
                            if (args.Length <= 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pem delplayer playername");
                                return;
                            }
                            
                            string playername = string.Join(" ", Merge(args, 1)).Trim();
                            Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(playername);
                            if (pl != null)
                            {
                                // If target has pem.admin, we need rcon permissions.
                                if (permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                {
                                    player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                    player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                    return;
                                }
                                
                                permissionSystem.RemovePermissionPlayer(pl);
                                player.MessageFrom("PermissionSystem", "Permissions can now be assigned to this player!");
                            }
                            else
                            {
                                player.MessageFrom("PermissionSystem", playername + " not found!");
                            }

                            break;
                        }
                        case "delofflplayer":
                        {
                            if (args.Length <= 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pem delofflplayer steamid");
                                return;
                            }
                            
                            string steamid = string.Join("", Merge(args, 1)).Trim();
                            ulong uid;
                            if (!ulong.TryParse(steamid, out uid))
                            {
                                player.MessageFrom("PermissionSystem", "Use a steamid (Yes, sorry)");
                                return;
                            }
                            
                            // If target has pem.admin, we need rcon permissions.
                            if (permissionSystem.PlayerHasPermission(uid, "pem.admin") && !player.Admin)
                            {
                                player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                return;
                            }

                            bool success = permissionSystem.RemovePermissionPlayer(uid);
                            player.MessageFrom("PermissionSystem", success ? "Removed!" : "User doesn't exist!");

                            break;
                        }
                        case "addperm":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pem addperm steamid/name permission");
                                return;
                            }
                            
                            string target = args[1];
                            string permission = args[2];
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    // If target has pem.admin, we need rcon permissions.
                                    if (permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    bool success = permissionSystem.AddPermission(pl, permission);
                                    player.MessageFrom("PermissionSystem", success ? "Added for " + pl.Name + " permission: " + permission 
                                        : "User doesn't exist!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                // If target has pem.admin, we need rcon permissions.
                                if (permissionSystem.PlayerHasPermission(uid, "pem.admin") && !player.Admin)
                                {
                                    player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                    player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                    return;
                                }
                                bool success = permissionSystem.AddPermission(uid, permission);
                                player.MessageFrom("PermissionSystem", success ? "Added to " + uid + " permission: " + permission 
                                    : "User doesn't exist!");
                            }
                            break;
                        }
                        case "delperm":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pem delperm steamid/name permission");
                                return;
                            }
                            
                            string target = args[1];
                            string permission = args[2];
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    // If target has pem.admin, we need rcon permissions.
                                    if (permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    bool success = permissionSystem.RemovePermission(pl, permission);
                                    player.MessageFrom("PermissionSystem", success ? "Removed for " + pl.Name + " permission: " + permission 
                                        : "User doesn't exist!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                // If target has pem.admin, we need rcon permissions.
                                if (permissionSystem.PlayerHasPermission(uid, "pem.admin") && !player.Admin)
                                {
                                    player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                    player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                    return;
                                }
                                bool success = permissionSystem.RemovePermission(uid, permission);
                                player.MessageFrom("PermissionSystem", success ? "Removed for " + uid + " permission: " + permission 
                                    : "User doesn't exist!");
                            }
                            break;
                        }
                        case "listperms":
                        {
                            if (args.Length < 2)
                            {
                                player.MessageFrom("PermissionSystem", "/pem listperms steamid/name");
                                return;
                            }
                            
                            string target = args[1];
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    var ppl = permissionSystem.GetPlayerBySteamID(pl);
                                    if (ppl != null)
                                    {
                                        var list = new List<string>(ppl.Permissions);
                                        player.MessageFrom("PermissionSystem", "Perms: " + string.Join(", ", list.ToArray()));
                                    }
                                    else
                                    {
                                        player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                    }
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                var ppl = permissionSystem.GetPlayerBySteamID(uid);
                                if (ppl != null)
                                {
                                    var list = new List<string>(ppl.Permissions);
                                    player.MessageFrom("PermissionSystem", "Perms: " + string.Join(", ", list.ToArray()));
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                }
                            }
                            break;
                        }
                        case "addtogroup":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pem addtogroup steamid/ groupname");
                                return;
                            }
                            
                            string target = args[1];
                            string group = string.Join(" ",Merge(args, 2)).Trim();
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    var ppl = permissionSystem.GetPlayerBySteamID(pl);
                                    if (ppl != null)
                                    {
                                        // If target has pem.admin, we need rcon permissions.
                                        if (permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                        {
                                            player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                            player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                            return;
                                        }
                                        bool success = permissionSystem
                                            .AddGroupToPlayer(ppl.SteamID, group);
                                        player.MessageFrom("PermissionSystem", success ? "Added " + pl.Name + " to " + group 
                                            : "Group doesn't exist!");
                                    }
                                    else
                                    {
                                        player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                    }
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                var ppl = permissionSystem.GetPlayerBySteamID(uid);
                                if (ppl != null)
                                {
                                    // If target has pem.admin, we need rcon permissions.
                                    if (permissionSystem.PlayerHasPermission(ppl.SteamID, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    bool success = PermissionSystem.GetPermissionSystem()
                                        .AddGroupToPlayer(ppl.SteamID, group);
                                    player.MessageFrom("PermissionSystem", success ? "Added " + uid + " to " + group 
                                        : "Group doesn't exist!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                }
                            }
                            break;
                        }
                        case "delfromgroup":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pem delfromgroup steamid/name groupname");
                                return;
                            }
                            
                            string target = args[1];
                            string group = string.Join(" ",Merge(args, 2)).Trim();
                            ulong uid;
                            if (!ulong.TryParse(target, out uid))
                            {
                                Fougerite.Player pl = Fougerite.Server.GetServer().FindPlayer(target);
                                if (pl != null)
                                {
                                    var ppl = permissionSystem.GetPlayerBySteamID(pl);
                                    if (ppl != null)
                                    {
                                        // If target has pem.admin, we need rcon permissions.
                                        if (permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                        {
                                            player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                            player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                            return;
                                        }
                                        
                                        bool success = permissionSystem
                                            .RemoveGroupFromPlayer(ppl.SteamID, group);
                                        player.MessageFrom("PermissionSystem", success ? "Removed " + pl.Name + " from " + group 
                                            : "Group doesn't exist or user doesn't have It!");
                                    }
                                    else
                                    {
                                        player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                    }
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                var ppl = permissionSystem.GetPlayerBySteamID(uid);
                                if (ppl != null)
                                {
                                    // If target has pem.admin, we need rcon permissions.
                                    if (permissionSystem.PlayerHasPermission(ppl.SteamID, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    
                                    bool success = permissionSystem
                                        .RemoveGroupFromPlayer(ppl.SteamID, group);
                                    player.MessageFrom("PermissionSystem", success ? "Removed " + uid + " from " + group 
                                        : "Group doesn't exist user doesn't have It!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " is not a permissionplayer!");
                                }
                            }
                            break;
                        }
                        default:
                        {
                            player.MessageFrom("PermissionSystem", "Invalid command!");
                            break;
                        }
                    }
                    #endregion

                    break;
                }
                // For group management
                case "pemg":
                {
                    PermissionSystem permissionSystem = PermissionSystem.GetPermissionSystem();
                    if (!player.Admin && !permissionSystem.PlayerHasPermission(player, "pem.admin")) return;
                    if (args.Length == 0)
                    {
                        player.MessageFrom("PermissionSystem", "=== PermissionSystem v" + Version + " ===");
                        return;
                    }
                    
                    string secondcommand = args[0];
                    switch (secondcommand)
                    {
                        case "createg":
                        {
                            if (args.Length < 2)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg createg groupname");
                                return;
                            }
                            string group = string.Join(" ",Merge(args, 1)).Trim();
                            bool success = permissionSystem.CreateGroup(group);
                            player.MessageFrom("PermissionSystem", success ? "Group " + group + " created!"
                                : "Group already exists!");
                            break;
                        }
                        case "delg":
                        {
                            if (args.Length < 2)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg delg groupname");
                                return;
                            }
                            string group = string.Join(" ",Merge(args, 1)).Trim();
                            bool success = permissionSystem.RemoveGroup(group);
                            player.MessageFrom("PermissionSystem", success ? "Group " + group + " deleted!"
                                : "Group doesn't exist!");
                            break;
                        }
                        case "listperms":
                        {
                            if (args.Length < 2)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg listperms groupname");
                                return;
                            }
                            string group = string.Join(" ",Merge(args, 1)).Trim();
                            PermissionGroup pgroup = permissionSystem.GetGroupByName(group);
                            if (pgroup != null)
                            {
                                var list = new List<string>(pgroup.GroupPermissions);
                                player.MessageFrom("PermissionSystem", "Perms: " + string.Join(", ", list.ToArray()));
                            }
                            else
                            {
                                player.MessageFrom("PermissionSystem", "Group " + group + " doesn't exist!");
                            }
                            break;
                        }
                        default:
                        {
                            player.MessageFrom("PermissionSystem", "Invalid command!");
                            break;
                        }
                    }

                    break;
                }
            }
        }
    }
}