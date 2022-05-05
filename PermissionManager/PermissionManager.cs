using System;
using System.Collections.Generic;
using System.Linq;
using Fougerite;
using Fougerite.Permissions;

namespace PermissionManager
{
    public class PermissionManager : Fougerite.Module
    {
        private PermissionSystem _permissionSystem;
        
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
            _permissionSystem = PermissionSystem.GetPermissionSystem();
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
                    if (!player.Admin && !_permissionSystem.PlayerHasPermission(player, "pem.admin")) return;
                    if (args.Length == 0)
                    {
                        player.MessageFrom("PermissionSystem", "=== PermissionSystem v" + Version + " ===");
                        player.MessageFrom("PermissionSystem", "/pem reload - Reloads all Permissions");
                        player.MessageFrom("PermissionSystem", "/pem newplayer - Creates a PermissionPlayer by name (online)");
                        player.MessageFrom("PermissionSystem", "/pem delplayer - Removes a PermissionPlayer by name (online)");
                        player.MessageFrom("PermissionSystem", "/pem delofflplayer - Removes a PermissionPlayer by steamid (offline)");
                        player.MessageFrom("PermissionSystem", "/pem addperm - Adds Permission to a PermissionPlayer");
                        player.MessageFrom("PermissionSystem", "/pem delperm - Removes Permission from a PermissionPlayer");
                        player.MessageFrom("PermissionSystem", "/pem listperms - Lists a PermissionPlayer's permissions");
                        player.MessageFrom("PermissionSystem", "/pem addtogroup - Adds a PermissionPlayer to a group");
                        player.MessageFrom("PermissionSystem", "/pem delfromgroup - Removes PermissionPlayer from a group");
                        return;
                    }

                    #region PermissionPlayerHandling
                    string secondcommand = args[0];
                    switch (secondcommand)
                    {
                        case "reload":
                        {
                            _permissionSystem.ReloadPermissions();
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
                                _permissionSystem.CreatePermissionPlayer(pl);
                                player.MessageFrom("PermissionSystem", "Permissions can now be assigned to this player! (" + playername + ")");
                            }
                            else
                            {
                                player.MessageFrom("PermissionSystem", playername + " not found!");
                            }

                            break;
                        }
                        case "newoffplayer":
                        {
                            if (args.Length <= 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pem newoffplayer steamid");
                                return;
                            }
                            
                            string steamid = string.Join("", Merge(args, 1)).Trim();
                            ulong uid;
                            if (!ulong.TryParse(steamid, out uid))
                            {
                                player.MessageFrom("PermissionSystem", "Use a steamid (Yes, sorry)");
                                return;
                            }
                            
                            _permissionSystem.CreatePermissionPlayer(uid);
                            player.MessageFrom("PermissionSystem", "Permissions can now be assigned to this player! (" + steamid + ")");

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
                                if (_permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                {
                                    player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                    player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                    return;
                                }
                                
                                bool success = _permissionSystem.RemovePermissionPlayer(pl);
                                player.MessageFrom("PermissionSystem", success ? playername + " removed!"
                                    : playername + " is not a PermissionPlayer!");
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
                            if (_permissionSystem.PlayerHasPermission(uid, "pem.admin") && !player.Admin)
                            {
                                player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                return;
                            }

                            bool success = _permissionSystem.RemovePermissionPlayer(uid);
                            player.MessageFrom("PermissionSystem", success ? steamid + " removed!"
                                : steamid + " is not a PermissionPlayer!");

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
                                    if (_permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    bool success = _permissionSystem.AddPermission(pl, permission);
                                    player.MessageFrom("PermissionSystem", success ? "Added for " + pl.Name + " permission: " + permission 
                                        : pl.Name + " is not a PermissionPlayer!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                // If target has pem.admin, we need rcon permissions.
                                if (_permissionSystem.PlayerHasPermission(uid, "pem.admin") && !player.Admin)
                                {
                                    player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                    player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                    return;
                                }
                                bool success = _permissionSystem.AddPermission(uid, permission);
                                player.MessageFrom("PermissionSystem", success ? "Added to " + uid + " permission: " + permission 
                                    : target + " is not a PermissionPlayer!");
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
                                    if (_permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    bool success = _permissionSystem.RemovePermission(pl, permission);
                                    player.MessageFrom("PermissionSystem", success ? "Removed for " + pl.Name + " permission: " + permission 
                                        : target + " is not a PermissionPlayer!");
                                }
                                else
                                {
                                    player.MessageFrom("PermissionSystem", target + " not found!");
                                }
                            }
                            else
                            {
                                // If target has pem.admin, we need rcon permissions.
                                if (_permissionSystem.PlayerHasPermission(uid, "pem.admin") && !player.Admin)
                                {
                                    player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                    player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                    return;
                                }
                                bool success = _permissionSystem.RemovePermission(uid, permission);
                                player.MessageFrom("PermissionSystem", success ? "Removed for " + uid + " permission: " + permission 
                                    : target + " is not a PermissionPlayer!");
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
                                    var ppl = _permissionSystem.GetPlayerBySteamID(pl);
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
                                var ppl = _permissionSystem.GetPlayerBySteamID(uid);
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
                                player.MessageFrom("PermissionSystem", "/pem addtogroup steamid/name groupname");
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
                                    var ppl = _permissionSystem.GetPlayerBySteamID(pl);
                                    if (ppl != null)
                                    {
                                        // If target has pem.admin, we need rcon permissions.
                                        if (_permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                        {
                                            player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                            player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                            return;
                                        }
                                        bool success = _permissionSystem
                                            .AddGroupToPlayer(ppl.SteamID, group);
                                        player.MessageFrom("PermissionSystem", success ? "Added " + pl.Name + " to " + group 
                                            : group + " doesn't exist!");
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
                                var ppl = _permissionSystem.GetPlayerBySteamID(uid);
                                if (ppl != null)
                                {
                                    // If target has pem.admin, we need rcon permissions.
                                    if (_permissionSystem.PlayerHasPermission(ppl.SteamID, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    bool success = _permissionSystem
                                        .AddGroupToPlayer(ppl.SteamID, group);
                                    player.MessageFrom("PermissionSystem", success ? "Added " + uid + " to " + group 
                                        : group + " doesn't exist!");
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
                                    var ppl = _permissionSystem.GetPlayerBySteamID(pl);
                                    if (ppl != null)
                                    {
                                        // If target has pem.admin, we need rcon permissions.
                                        if (_permissionSystem.PlayerHasPermission(pl, "pem.admin") && !player.Admin)
                                        {
                                            player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                            player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                            return;
                                        }
                                        
                                        bool success = _permissionSystem
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
                                var ppl = _permissionSystem.GetPlayerBySteamID(uid);
                                if (ppl != null)
                                {
                                    // If target has pem.admin, we need rcon permissions.
                                    if (_permissionSystem.PlayerHasPermission(ppl.SteamID, "pem.admin") && !player.Admin)
                                    {
                                        player.MessageFrom("PermissionSystem", "You need RCON access to modify a PermissionManager.");
                                        player.MessageFrom("PermissionSystem", "Ensure you have also created the PermissionPlayer.");
                                        return;
                                    }
                                    
                                    bool success = _permissionSystem
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
                    if (!player.Admin && !_permissionSystem.PlayerHasPermission(player, "pem.admin")) return;
                    if (args.Length == 0)
                    {
                        player.MessageFrom("PermissionSystem", "=== PermissionSystem v" + Version + " ===");
                        player.MessageFrom("PermissionSystem", "/pem createg - Creates a group");
                        player.MessageFrom("PermissionSystem", "/pem delg - Deletes a group");
                        player.MessageFrom("PermissionSystem", "/pem listpermsg - Lists the permissions of a group");
                        player.MessageFrom("PermissionSystem", "/pem addpermg - Adds permission to a group");
                        player.MessageFrom("PermissionSystem", "/pem delpermg - Removes permission from a group");
                        player.MessageFrom("PermissionSystem", "/pem changenickg - Changes nickname of a group");
                        player.MessageFrom("PermissionSystem", "/pem membersg - Lists members of a group (steamids only)");
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
                            string group = string.Join(" ", Merge(args, 1)).Trim().Replace(" ", "_");
                            bool success = _permissionSystem.CreateGroup(group);
                            player.MessageFrom("PermissionSystem", success ? "Group " + group + " created!"
                                : group + " already exists!");
                            break;
                        }
                        case "delg":
                        {
                            if (args.Length < 2)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg delg groupname");
                                return;
                            }
                            
                            string group = string.Join(" ", Merge(args, 1)).Trim();
                            bool success = _permissionSystem.RemoveGroup(group);
                            player.MessageFrom("PermissionSystem", success ? "Group " + group + " deleted!"
                                : "Failed to delete group: " + group + "! Maybe It doesn't exist, or It's the default group!");
                            break;
                        }
                        case "listpermsg":
                        {
                            if (args.Length < 2)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg listperms groupname");
                                return;
                            }
                            
                            string group = string.Join(" ", Merge(args, 1)).Trim();
                            PermissionGroup pgroup = _permissionSystem.GetGroupByName(group);
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
                        case "addpermg":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg addpermg groupname permission");
                                return;
                            }
                            
                            string group = args[1];
                            string permission = args[2];

                            bool success = _permissionSystem.AddPermissionToGroup(group, permission);
                            player.MessageFrom("PermissionSystem", success ? "Group " + group + " received permission: " + permission
                                : "Failed to add " + permission + " to " + group + "!");
                            break;
                        }
                        case "delpermg":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg delpermg groupname permission");
                                return;
                            }
                            
                            string group = args[1];
                            string permission = args[2];

                            bool success = _permissionSystem.RemovePermissionFromGroup(group, permission);
                            player.MessageFrom("PermissionSystem", success ? "Removed " + permission 
                                + " from group: " + group
                                : "Failed to remove " + permission + " from " + group + "!");
                            break;
                        }
                        case "changenickg":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg changenickg groupname nickname");
                                return;
                            }
                            
                            string group = args[1];
                            string nickname = args[2];

                            bool success = _permissionSystem.SetGroupNickName(group, nickname);
                            player.MessageFrom("PermissionSystem", success ? "Changed " + group 
                                + "'s nickname to: " + nickname
                                : "Failed to change " + group + "'s nickname to " + nickname + "!");
                            break;
                        }
                        case "changenameg":
                        {
                            if (args.Length < 3)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg changenickg groupname nickname");
                                return;
                            }
                            
                            string group = args[1];
                            string newname = string.Join(" ", Merge(args, 2)).Trim().Replace(" ", "_");

                            bool success = _permissionSystem.ChangeGroupName(group, newname);
                            player.MessageFrom("PermissionSystem", success ? "Changed " + group 
                                + "'s name to: " + newname
                                : "Failed to change " + group + "'s name to " + newname + "!");
                            break;
                        }
                        case "membersg":
                        {
                            if (args.Length < 1)
                            {
                                player.MessageFrom("PermissionSystem", "/pemg membersg groupname");
                                return;
                            }
                            
                            string group = string.Join(" ", Merge(args, 0)).Trim();
                            PermissionGroup pgroup = _permissionSystem.GetGroupByName(group);
                            
                            if (pgroup != null)
                            {
                                uint id = pgroup.UniqueID;
                                List<string> collectedPlayers = new List<string>();
                                foreach (PermissionPlayer x in _permissionSystem.GetPermissionPlayers())
                                {
                                    string gname = x.Groups.FirstOrDefault(y => _permissionSystem.GetUniqueID(y.Trim().ToLower()) == id);
                                    if (!string.IsNullOrEmpty(gname))
                                    {
                                        collectedPlayers.Add(x.SteamID.ToString());
                                    }
                                }
                                
                                player.MessageFrom("PermissionSystem", pgroup.GroupName + "'s members: " 
                                    + string.Join(", ", collectedPlayers.ToArray()));
                            }
                            else
                            {
                                player.MessageFrom("PermissionSystem", group + " doesn't exist!");
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