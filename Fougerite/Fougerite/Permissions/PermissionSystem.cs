using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    /// <summary>
    /// The heart of the permission system.
    /// I recommend using groups, and assigning players to them.
    /// TODO: Implement hooks?
    /// TODO: Support for x.*
    /// </summary>
    public class PermissionSystem
    {
        private static PermissionSystem _instance;
        private static readonly object _obj = new object();
        private static readonly object _obj2 = new object();
        private readonly PermissionHandler _handler;
        private readonly Dictionary<ulong, bool> _disabledpermissions = new Dictionary<ulong, bool>();

        /// <summary>
        /// PermissionSystem is a Singleton.
        /// </summary>
        private PermissionSystem()
        {
            _handler = new PermissionHandler();
            ReloadPermissions();
        }

        /// <summary>
        /// Temporarily remove all permissions of a player.
        /// Lasts until the server restarts, or removed manually.
        /// removeDefaultGroupPermissions true removes the default
        /// group's as well.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="removeDefaultGroupPermissions"></param>
        /// <returns></returns>
        public bool ForceOffPermissions(ulong steamid, bool removeDefaultGroupPermissions)
        {
            lock (_obj2)
            {
                if (!_disabledpermissions.ContainsKey(steamid))
                {
                    _disabledpermissions.Add(steamid, removeDefaultGroupPermissions);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Removes the temporarily added effect.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool RemoveForceOffPermissions(ulong steamid)
        {
            lock (_obj2)
            {
                if (_disabledpermissions.ContainsKey(steamid))
                {
                    _disabledpermissions.Remove(steamid);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if the player has its permissions forced off.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool HasPermissionsForcedOff(ulong steamid)
        {
            lock (_obj2)
            {
                return _disabledpermissions.ContainsKey(steamid);
            }
        }

        /// <summary>
        /// Checks if a player has its permissions forced off and the
        /// default permissions as well.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool HasDefaultPermissionsForcedOff(ulong steamid)
        {
            lock (_obj2)
            {
                if (_disabledpermissions.ContainsKey(steamid))
                {
                    return _disabledpermissions[steamid];
                }

                return false;
            }
        }

        /// <summary>
        /// Returns a shallow copied dictionary of forced off permissions.
        /// </summary>
        public Dictionary<ulong, bool> DisabledPermissions
        {
            get
            {
                lock (_obj2)
                {
                    return new Dictionary<ulong, bool>(_disabledpermissions);
                }
            }
        }

        /// <summary>
        /// Gets the unique identifier of a string.
        /// This is used for group names.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public uint GetUniqueID(string value)
        {
            return SuperFastHashUInt16Hack.Hash(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Reloads the permissions.
        /// </summary>
        public void ReloadPermissions()
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                List<PermissionPlayer> emptyplayers = new List<PermissionPlayer>();
                List<PermissionGroup> emptygroups = new List<PermissionGroup>();

                if (!File.Exists($"{Util.GetRootFolder()}\\Save\\GroupPermissions.json"))
                {
                    File.Create($"{Util.GetRootFolder()}\\Save\\GroupPermissions.json").Dispose();
                    emptygroups.Add(new PermissionGroup()
                    {
                        GroupName = "Default",
                        GroupPermissions = new List<string>() {"donotdeletethedefaultgroup", "something"},
                        NickName = "Default nick name"
                    });
                    emptygroups.Add(new PermissionGroup()
                    {
                        GroupName = "Group1", 
                        GroupPermissions = new List<string>() {"grouppermission1"},
                        NickName = "Nice nick name"
                    });
                    emptygroups.Add(new PermissionGroup()
                    {
                        GroupName = "Group2",
                        GroupPermissions = new List<string>() {"grouppermission2.gar", "grouppermission2.something"},
                        NickName = "SomeNickname"
                    });

                    using (StreamWriter sw =
                        new StreamWriter($"{Util.GetRootFolder()}\\Save\\GroupPermissions.json", false,
                            Encoding.UTF8))
                    {
                        using (JsonWriter writer = new JsonTextWriter(sw))
                        {
                            writer.Formatting = Formatting.Indented;
                            serializer.Serialize(writer, emptygroups);
                        }
                    }
                }

                if (!File.Exists($"{Util.GetRootFolder()}\\Save\\PlayerPermissions.json"))
                {
                    File.Create($"{Util.GetRootFolder()}\\Save\\PlayerPermissions.json").Dispose();

                    emptyplayers.Add(new PermissionPlayer()
                    {
                        SteamID = 76562531000,
                        Permissions = new List<string>()
                            {"*", "permission", "permission2.something", "permission3"},
                        Groups = new List<string>() {"Group1"}
                    });

                    using (StreamWriter sw =
                        new StreamWriter($"{Util.GetRootFolder()}\\Save\\PlayerPermissions.json", false,
                            Encoding.UTF8))
                    {
                        using (JsonWriter writer = new JsonTextWriter(sw))
                        {
                            writer.Formatting = Formatting.Indented;
                            serializer.Serialize(writer, emptyplayers);
                        }
                    }
                }

                lock (_obj)
                {
                    _handler.PermissionGroups =
                        JsonConvert.DeserializeObject<List<PermissionGroup>>(
                            File.ReadAllText($"{Util.GetRootFolder()}\\Save\\GroupPermissions.json"));
                    _handler.PermissionPlayers =
                        JsonConvert.DeserializeObject<List<PermissionPlayer>>(
                            File.ReadAllText($"{Util.GetRootFolder()}\\Save\\PlayerPermissions.json"));
                }

                Logger.Log("[PermissionSystem] Loaded.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] Error: {ex}");
            }
        }

        /// <summary>
        /// Returns the permission system class.
        /// </summary>
        /// <returns></returns>
        public static PermissionSystem GetPermissionSystem()
        {
            if (_instance == null)
            {
                _instance = new PermissionSystem();
            }

            return _instance;
        }

        /// <summary>
        /// Tries to save the data from the memory to disk.
        /// If any failure occurs It will revert to the current state.
        /// </summary>
        public void SaveToDisk()
        {
            string grouppermissions = "";
            string playerpermissions = "";

            string playerPermissionsPath = $"{Util.GetRootFolder()}\\Save\\PlayerPermissions.json";
            string groupPermissionsPath = $"{Util.GetRootFolder()}\\Save\\GroupPermissions.json";
            
            try
            {
                if (!File.Exists(groupPermissionsPath))
                {
                    File.Create(groupPermissionsPath).Dispose();
                }

                if (!File.Exists(playerPermissionsPath))
                {
                    File.Create(playerPermissionsPath).Dispose();
                }

                // Backup the data from the current files.
                grouppermissions = File.ReadAllText(groupPermissionsPath);
                playerpermissions = File.ReadAllText(playerPermissionsPath);

                // Empty the files.
                if (File.Exists(groupPermissionsPath))
                {
                    File.WriteAllText(groupPermissionsPath, string.Empty);
                }

                if (File.Exists(playerPermissionsPath))
                {
                    File.WriteAllText(playerPermissionsPath, string.Empty);
                }

                // Initialize empty list just in case.
                List<PermissionGroup> PermissionGroups = new List<PermissionGroup>();
                List<PermissionPlayer> PermissionPlayers = new List<PermissionPlayer>();

                // Grab the data from the memory using lock.
                lock (_obj)
                {
                    PermissionGroups = _handler.PermissionGroups;
                    PermissionPlayers = _handler.PermissionPlayers;
                }

                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;

                using (StreamWriter sw =
                    new StreamWriter(groupPermissionsPath, false,
                        Encoding.UTF8))
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        serializer.Serialize(writer, PermissionGroups);
                    }
                }

                using (StreamWriter sw =
                    new StreamWriter(playerPermissionsPath, false,
                        Encoding.UTF8))
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        serializer.Serialize(writer, PermissionPlayers);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] SaveToDisk Error: {ex}");
                File.WriteAllText(groupPermissionsPath, grouppermissions);
                File.WriteAllText(playerPermissionsPath, playerpermissions);
            }
        }

        /// <summary>
        /// Returns all the existing groups.
        /// Note that the list is a shallow copy.
        /// </summary>
        /// <returns></returns>
        public List<PermissionGroup> GetPermissionGroups()
        {
            lock (_obj)
            {
                return new List<PermissionGroup>(_handler.PermissionGroups);
            }
        }
        
        /// <summary>
        /// Returns all the players that exist in the permission database.
        /// This might be a large list depending on how many players you have added to It.
        /// Note that the list is a shallow copy.
        /// </summary>
        /// <returns></returns>
        public List<PermissionPlayer> GetPermissionPlayers()
        {
            lock (_obj)
            {
                return new List<PermissionPlayer>(_handler.PermissionPlayers);
            }
        }

        /// <summary>
        /// Tries to find the group by name.
        /// Returns null if doesn't exist.
        /// Object is not a shallow copy.
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public PermissionGroup GetGroupByName(string groupname)
        {
            groupname = groupname.Trim().ToLower();
            uint uniqueid = GetUniqueID(groupname);
            lock (_obj)
            {
                return _handler.PermissionGroups.FirstOrDefault(x => x.UniqueID == uniqueid);
            }
        }

        /// <summary>
        /// Tries to find the group by name.
        /// Returns null if doesn't exist.
        /// Object is not a shallow copy.
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns></returns>
        public PermissionGroup GetGroupByID(int groupid)
        {
            lock (_obj)
            {
                return _handler.PermissionGroups.FirstOrDefault(x => x.UniqueID == groupid);
            }
        }

        /// <summary>
        /// Tries to find the player's permissions.
        /// Returns null if doesn't exist.
        /// Object is not a shallow copy.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public PermissionPlayer GetPlayerBySteamID(ulong steamid)
        {
            lock (_obj)
            {
                return _handler.PermissionPlayers.FirstOrDefault(x => x.SteamID == steamid);
            }
        }
        
        /// <summary>
        /// Tries to find the player's permissions.
        /// Returns null if doesn't exist.
        /// Object is not a shallow copy.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PermissionPlayer GetPlayerBySteamID(Player player)
        {
            return player == null ? null : GetPlayerBySteamID(player.UID);
        }

        /// <summary>
        /// Checks if the target player is member of a group.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public bool PlayerHasGroup(Player player, string groupname)
        {
            return player != null && PlayerHasGroup(player.UID, groupname);
        }
        
        /// <summary>
        /// Checks if the target player is member of a group.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public bool PlayerHasGroup(ulong steamid, string groupname)
        {
            groupname = groupname.Trim().ToLower();
            if (groupname == "default")
            {
                return true;
            }

            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer == null)
            {
                return false;
            }
            
            uint id = GetUniqueID(groupname);
            
            return permissionplayer.Groups.Any(x => GetUniqueID(x.Trim().ToLower()) == id);
        }
        
        /// <summary>
        /// Checks if the target player is member of a group.
        /// </summary>
        /// <param name="permissionplayer"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public bool PlayerHasGroup(PermissionPlayer permissionplayer, string groupname)
        {
            return permissionplayer != null && PlayerHasGroup(permissionplayer.SteamID, groupname);
        }
        
        /// <summary>
        /// Checks if a target player has a permission.
        /// You should acknowledge that a player can has It's
        /// permissions forced off. Check for that with
        /// HasDefaultPermissionsForcedOff and HasPermissionsForcedOff
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool PlayerHasPermission(ulong steamid, string permission)
        {
            // Check if permissions were revoked.
            if (HasDefaultPermissionsForcedOff(steamid))
            {
                return false;
            }
            
            permission = permission.Trim().ToLower();

            var permissionplayer = GetPlayerBySteamID(steamid);
            // Player has no specific permissions, or groups. Check for the default group.
            // This is gonna apply to most of the players of the server.
            if (permissionplayer == null)
            {
                PermissionGroup defaul = GetGroupByName("Default");
                if (defaul != null)
                {
                    bool haspermission = defaul.GroupPermissions.Any(x => x.Trim() == "*" || x.Trim().ToLower() == permission);
                    if (haspermission) return true;
                }
                
                return false;
            }
            
            // Check if permissions were revoked, but without default permissions.
            if (HasPermissionsForcedOff(steamid))
            {
                return false;
            }
            
            foreach (PermissionGroup group in permissionplayer.Groups.Select(GetGroupByName))
            {
                // Ensure that the group indeed existed
                if (group == null)
                {
                    continue;
                }
                
                bool haspermission = group.GroupPermissions.Any(x => x.Trim() == "*" || x.Trim().ToLower() == permission);
                if (haspermission) return true;
            }

            foreach (var x in permissionplayer.Permissions)
            {
                string pn = x.Trim().ToLower();
                if (pn == "*")
                {
                    return true;
                }
                
                if (pn == permission)
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Checks if a target player has a permission.
        /// You should acknowledge that a player can has It's
        /// permissions forced off. Check for that with
        /// HasDefaultPermissionsForcedOff and HasPermissionsForcedOff
        /// </summary>
        /// <param name="permissionPlayer"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool PlayerHasPermission(PermissionPlayer permissionPlayer, string permission)
        {
            return permissionPlayer != null && PlayerHasPermission(permissionPlayer.SteamID, permission);
        }
        
        /// <summary>
        /// Checks if a target player has a permission.
        /// You should acknowledge that a player can has It's
        /// permissions forced off. Check for that with
        /// HasDefaultPermissionsForcedOff and HasPermissionsForcedOff
        /// </summary>
        /// <param name="player"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool PlayerHasPermission(Player player, string permission)
        {
            return player != null && PlayerHasPermission(player.UID, permission);
        }

        /// <summary>
        /// Adds a new permission player to the list.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PermissionPlayer CreatePermissionPlayer(Player player)
        {
            return player == null ? null : CreatePermissionPlayer(player.UID);
        }
        
        /// <summary>
        /// Adds a new permission player to the list.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public PermissionPlayer CreatePermissionPlayer(ulong steamid)
        {
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer == null)
            {
                PermissionPlayer permissionPlayer = new PermissionPlayer()
                {
                    Groups = new List<string>(),
                    Permissions = new List<string>(),
                    SteamID = steamid
                };
                
                lock (_obj)
                {
                    _handler.PermissionPlayers.Add(permissionPlayer);
                    return permissionPlayer;
                }
            }

            return permissionplayer;
        }
        
        /// <summary>
        /// Adds a new permission player to the list with specific group list and permissions.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="groups"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public PermissionPlayer CreatePermissionPlayer(ulong steamid, List<string> groups, List<string> permissions)
        {
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer == null)
            {
                PermissionPlayer permissionPlayer = new PermissionPlayer()
                {
                    Groups = groups,
                    Permissions = permissions,
                    SteamID = steamid
                };
                
                lock (_obj)
                {
                    _handler.PermissionPlayers.Add(permissionPlayer);
                    return permissionPlayer;
                }
            }

            return permissionplayer;
        }

        /// <summary>
        /// Tries to remove the target player from the list.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public bool RemovePermissionPlayer(Player player)
        {
            return player != null && RemovePermissionPlayer(player.UID);
        }

        /// <summary>
        /// Tries to remove the target player from the list.
        /// </summary>
        /// <param name="permissionPlayer"></param>
        /// <returns></returns>
        public bool RemovePermissionPlayer(PermissionPlayer permissionPlayer)
        {
            if (permissionPlayer == null)
            {
                return false;
            }
            
            lock (_obj)
            {
                if (_handler.PermissionPlayers.Contains(permissionPlayer))
                {
                    _handler.PermissionPlayers.Remove(permissionPlayer);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to remove the target player from the list.
        /// </summary>
        /// <param name="steamid"></param>
        /// <returns></returns>
        public bool RemovePermissionPlayer(ulong steamid)
        {
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer != null)
            {
                lock (_obj)
                {
                    _handler.PermissionPlayers.Remove(permissionplayer);
                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Tries to add player to a group.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public bool AddGroupToPlayer(ulong steamid, string groupname)
        {
            groupname = groupname.Trim().ToLower();
            lock (_obj)
            {
                PermissionPlayer player = _handler.PermissionPlayers.SingleOrDefault(x => x.SteamID == steamid);
                if (player != null)
                {
                    uint id = GetUniqueID(groupname);
                    string gname = player.Groups.FirstOrDefault(y => GetUniqueID(y.Trim().ToLower()) == id);
                    if (string.IsNullOrEmpty(gname))
                    {
                        player.Groups.Add(groupname);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to remove a player from a group.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public bool RemoveGroupFromPlayer(ulong steamid, string groupname)
        {
            groupname = groupname.Trim().ToLower();

            lock (_obj)
            {
                PermissionPlayer player = _handler.PermissionPlayers.SingleOrDefault(x => x.SteamID == steamid);
                if (player != null)
                {
                    uint id = GetUniqueID(groupname);
                    string gname = player.Groups.FirstOrDefault(y => GetUniqueID(y.Trim().ToLower()) == id);
                    if (!string.IsNullOrEmpty(gname))
                    {
                        player.Groups.Remove(gname);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to create a permission group, unless It already exists.
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="permissions"></param>
        /// <param name="nickname"></param>
        /// <returns></returns>
        public bool CreateGroup(string groupname, List<string> permissions = null, string nickname = null)
        {
            if (permissions == null)
            {
                permissions = new List<string>();
            }

            if (nickname == null)
            {
                nickname = $"{groupname}NickName";
            }
            
            PermissionGroup group = GetGroupByName(groupname);
            if (group != null)
            {
                return false;
            }
            
            lock (_obj)
            {
                // Unique ID is set through setter.
                _handler.PermissionGroups.Add(new PermissionGroup()
                {
                    GroupName = groupname,
                    GroupPermissions = permissions,
                    NickName = nickname
                });

                return true;
            }
        }

        /// <summary>
        /// Completely disbands a permission group, and removes everyone from It.
        /// </summary>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public bool RemoveGroup(string groupname)
        {
            groupname = groupname.Trim().ToLower();
            
            // Disable the removal of the default group.
            if (groupname == "default")
            {
                return false;
            }
            
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                lock (_obj)
                {
                    _handler.PermissionGroups.Remove(group);
                    uint id = GetUniqueID(groupname);
                    
                    foreach (var x in _handler.PermissionPlayers)
                    {
                        string gname = x.Groups.FirstOrDefault(y => GetUniqueID(y.Trim().ToLower()) == id);
                        if (!string.IsNullOrEmpty(gname))
                        {
                            x.Groups.Remove(gname);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to add a permission to a group.
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool AddPermissionToGroup(string groupname, string permission)
        {
            groupname = groupname.Trim().ToLower();
            permission = permission.Trim().ToLower();
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                lock (_obj)
                {
                    if (!group.GroupPermissions.Contains(permission))
                    {
                        group.GroupPermissions.Add(permission);
                    }
                    
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to remove a permission from a group.
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool RemovePermissionFromGroup(string groupname, string permission)
        {
            groupname = groupname.Trim().ToLower();
            permission = permission.Trim().ToLower();
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                lock (_obj)
                {
                    if (group.GroupPermissions.Contains(permission))
                    {
                        group.GroupPermissions.Remove(permission);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a group has a permission.
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool GroupHasPermission(string groupname, string permission)
        {
            groupname = groupname.Trim().ToLower();
            permission = permission.Trim().ToLower();
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                lock (_obj)
                {
                    return group.GroupPermissions.Contains(permission);
                }
            }

            return false;
        }

        /// <summary>
        /// Changes the group's nickname.
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="nickname"></param>
        /// <returns></returns>
        public bool SetGroupNickName(string groupname, string nickname)
        {
            groupname = groupname.Trim().ToLower();
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                lock (_obj)
                {
                    group.NickName = nickname;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Changes the group name to something new.
        /// All players that were in the group will have the new group name assigned.
        /// </summary>
        /// <param name="groupname"></param>
        /// <param name="newname"></param>
        /// <returns></returns>
        public bool ChangeGroupName(string groupname, string newname)
        {
            groupname = groupname.Trim().ToLower();
            newname = newname.Trim();
            PermissionGroup group = GetGroupByName(groupname);
            PermissionGroup newGroup = GetGroupByName(newname.ToLower());

            if (group != null && newGroup == null)
            {
                uint id = group.UniqueID;
                lock (_obj)
                {
                    foreach (var x in _handler.PermissionPlayers)
                    {
                        string gname = x.Groups.FirstOrDefault(y => GetUniqueID(y.Trim().ToLower()) == id);
                        if (!string.IsNullOrEmpty(gname))
                        {
                            x.Groups.Remove(gname);
                            x.Groups.Add(newname);
                        }
                    }

                    return true;
                }
            }

            return false;
        }
        
        /// <summary>
        /// Tries to add permission to a player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool AddPermission(Player player, string permission)
        {
            return player != null && AddPermission(player.UID, permission);
        }
        
        /// <summary>
        /// Tries to add permission to a player.
        /// </summary>
        /// <param name="permissionPlayer"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool AddPermission(PermissionPlayer permissionPlayer, string permission)
        {
            return permissionPlayer != null && AddPermission(permissionPlayer.SteamID, permission);
        }
        
        /// <summary>
        /// Tries to add permission to a player.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool AddPermission(ulong steamid, string permission)
        {
            permission = permission.Trim().ToLower();
            lock (_obj)
            {
                PermissionPlayer permissionPlayer = _handler.PermissionPlayers.SingleOrDefault(x => x.SteamID == steamid);
                if (permissionPlayer != null)
                {
                    if (permissionPlayer.Permissions.Contains(permission))
                    {
                        return true;
                    }
                    
                    permissionPlayer.Permissions.Add(permission);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Tries to remove a permission from a player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool RemovePermission(Player player, string permission)
        {
            return player != null && RemovePermission(player.UID, permission);
        }
        
        /// <summary>
        /// Tries to remove a permission from a player.
        /// </summary>
        /// <param name="permissionPlayer"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool RemovePermission(PermissionPlayer permissionPlayer, string permission)
        {
            return permissionPlayer != null && RemovePermission(permissionPlayer.SteamID, permission);
        }
        
        /// <summary>
        /// Tries to remove a permission from a player.
        /// </summary>
        /// <param name="steamid"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public bool RemovePermission(ulong steamid, string permission)
        {
            permission = permission.Trim().ToLower();
            lock (_obj)
            {
                PermissionPlayer permissionPlayer = _handler.PermissionPlayers.SingleOrDefault(x => x.SteamID == steamid);
                if (permissionPlayer != null)
                {
                    if (permissionPlayer.Permissions.Contains(permission))
                    {
                        permissionPlayer.Permissions.Remove(permission);
                    }

                    return true;
                }

                return false;
            }
        }
    }
}