using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Fougerite.Concurrent;
using Fougerite.Events;
using Fougerite.Tools;
using Newtonsoft.Json;
using ReaderWriterLock = Fougerite.Concurrent.ReaderWriterLock;

namespace Fougerite.Permissions
{
    /// <summary>
    /// Manages the core functionality for validating hierarchical permissions, group assignments, 
    /// and player-specific access controls.
    /// The heart of the permission system.
    /// I recommend using groups, and assigning players to them.
    /// </summary>
    public class PermissionSystem
    {
        private static readonly Lazy<PermissionSystem> Instance = new Lazy<PermissionSystem>(() => new PermissionSystem());
        private static readonly ReaderWriterLock PermLock = new ReaderWriterLock();
        private static readonly ReaderWriterLock DisableLock = new ReaderWriterLock();
        private readonly PermissionHandler _handler;
        private readonly Dictionary<ulong, bool> _disabledpermissions = new Dictionary<ulong, bool>();
        private readonly string _groupPermissionsPath;
        private readonly string _playerPermissionsPath;

        /// <summary>
        /// Initializes the PermissionSystem singleton and loads permission data from persistent storage.
        /// </summary>
        private PermissionSystem()
        {
            _handler = new PermissionHandler();
            _groupPermissionsPath = Util.GetRootFolder().Combine("\\Save\\GroupPermissions.json");
            _playerPermissionsPath = Util.GetRootFolder().Combine("\\Save\\PlayerPermissions.json");
            ReloadPermissions();
        }

        /// <summary>
        /// Temporarily revokes all permissions for a player until the server restarts or they are manually restored.
        /// </summary>
        /// <param name="steamid">The SteamID of the player whose permissions should be disabled.</param>
        /// <param name="removeDefaultGroupPermissions">If true, permissions from the default group will also be revoked.</param>
        /// <returns>True if the player's permissions were successfully forced off, otherwise, false.</returns>
        public bool ForceOffPermissions(ulong steamid, bool removeDefaultGroupPermissions)
        {
            DisableLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (!_disabledpermissions.ContainsKey(steamid))
                {
                    _disabledpermissions.Add(steamid, removeDefaultGroupPermissions);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] ForceOffPermissions Error: {ex}");
            }
            finally
            {
                DisableLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Restores a player's permissions that were previously revoked via the ForceOff mechanism.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <returns>True if the revocation was removed, otherwise, false.</returns>
        public bool RemoveForceOffPermissions(ulong steamid)
        {
            DisableLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_disabledpermissions.ContainsKey(steamid))
                {
                    _disabledpermissions.Remove(steamid);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] RemoveForceOffPermissions Error: {ex}");
            }
            finally
            {
                DisableLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Checks if the player currently has their permissions revoked via the ForceOff mechanism.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <returns>True if permissions are forced off, otherwise, false.</returns>
        public bool HasPermissionsForcedOff(ulong steamid)
        {
            DisableLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _disabledpermissions.ContainsKey(steamid);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] HasPermissionsForcedOff Error: {ex}");
            }
            finally
            {
                DisableLock.ReleaseReaderLock();
            }

            return false;
        }

        /// <summary>
        /// Determines if a player has their permissions revoked and if default group access is also disabled.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <returns>True if the player has their default permissions forced off, otherwise, false.</returns>
        public bool HasDefaultPermissionsForcedOff(ulong steamid)
        {
            DisableLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (_disabledpermissions.ContainsKey(steamid))
                {
                    return _disabledpermissions[steamid];
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] HasDefaultPermissionsForcedOff Error: {ex}");
            }
            finally
            {
                DisableLock.ReleaseReaderLock();
            }

            return false;
        }

        /// <summary>
        /// Gets a copy of the dictionary containing all players whose permissions are currently revoked.
        /// </summary>
        public Dictionary<ulong, bool> DisabledPermissions
        {
            get
            {
                DisableLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return new Dictionary<ulong, bool>(_disabledpermissions);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] DisabledPermissions Error: {ex}");
                }
                finally
                {
                    DisableLock.ReleaseReaderLock();
                }
                
                return new Dictionary<ulong, bool>();
            }
        }

        /// <summary>
        /// Generates a unique numeric identifier for a given string, primarily used for identifying groups.
        /// </summary>
        /// <param name="value">The string to hash.</param>
        /// <returns>A unique 32-bit unsigned integer identifier.</returns>
        public uint GetUniqueID(string value)
        {
            return SuperFastHashUInt16Hack.Hash(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Reloads group and player permission records from JSON files, creating default templates if missing.
        /// </summary>
        public void ReloadPermissions()
        {
            try
            {
                JsonSerializer serializer = new JsonSerializer();
                // https://stackoverflow.com/questions/24025350/xamarin-android-json-net-serilization-fails-on-4-2-2-device-only-timezonenotfoun
                serializer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                serializer.NullValueHandling = NullValueHandling.Include;
                List<PermissionPlayer> emptyplayers = new List<PermissionPlayer>();
                List<PermissionGroup> emptygroups = new List<PermissionGroup>();

                if (!File.Exists(_groupPermissionsPath))
                {
                    File.Create(_groupPermissionsPath).Dispose();
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
                        GroupPermissions = new List<string>() {"grouppermission2.gar", "grouppermission2.something", "grouppermission2.something.*"},
                        NickName = "SomeNickname"
                    });

                    using (StreamWriter sw =
                        new StreamWriter(_groupPermissionsPath, false,
                            Encoding.UTF8))
                    {
                        using (JsonWriter writer = new JsonTextWriter(sw))
                        {
                            writer.Formatting = Formatting.Indented;
                            serializer.Serialize(writer, emptygroups);
                        }
                    }
                }

                if (!File.Exists(_playerPermissionsPath))
                {
                    File.Create(_playerPermissionsPath).Dispose();

                    emptyplayers.Add(new PermissionPlayer()
                    {
                        SteamID = 76562531000,
                        Permissions = new List<string>()
                            {"*", "permission", "permission2.something", "permission3", "permission4.commands.*"},
                        Groups = new List<string>() {"Group1"}
                    });

                    using (StreamWriter sw =
                        new StreamWriter(_playerPermissionsPath, false,
                            Encoding.UTF8))
                    {
                        using (JsonWriter writer = new JsonTextWriter(sw))
                        {
                            writer.Formatting = Formatting.Indented;
                            serializer.Serialize(writer, emptyplayers);
                        }
                    }
                }

                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _handler.PermissionGroups = JsonConvert.DeserializeObject<List<PermissionGroup>>(File.ReadAllText(_groupPermissionsPath));
                    _handler.PermissionPlayers = JsonConvert.DeserializeObject<List<PermissionPlayer>>(File.ReadAllText(_playerPermissionsPath));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] ReloadPermissions Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }

                Logger.Log("[PermissionSystem] Loaded.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] Error: {ex}");
            }
        }

        /// <summary>
        /// Retrieves the singleton instance of the PermissionSystem.
        /// </summary>
        /// <returns>The active PermissionSystem instance.</returns>
        public static PermissionSystem GetPermissionSystem()
        {
            return Instance.Value;
        }

        /// <summary>
        /// Serializes the current in-memory permission data to disk, with a backup restoration mechanism in case of failure.
        /// </summary>
        public void SaveToDisk()
        {
            string grouppermissions = "";
            string playerpermissions = "";

            try
            {
                if (!File.Exists(_groupPermissionsPath))
                {
                    File.Create(_groupPermissionsPath).Dispose();
                }

                if (!File.Exists(_playerPermissionsPath))
                {
                    File.Create(_playerPermissionsPath).Dispose();
                }

                // Backup the data from the current files.
                grouppermissions = File.ReadAllText(_groupPermissionsPath);
                playerpermissions = File.ReadAllText(_playerPermissionsPath);

                // Empty the files.
                if (File.Exists(_groupPermissionsPath))
                {
                    File.WriteAllText(_groupPermissionsPath, string.Empty);
                }

                if (File.Exists(_playerPermissionsPath))
                {
                    File.WriteAllText(_playerPermissionsPath, string.Empty);
                }

                // Initialize empty list just in case.
                List<PermissionGroup> PermissionGroups = new List<PermissionGroup>();
                List<PermissionPlayer> PermissionPlayers = new List<PermissionPlayer>();

                // Grab the data from the memory using lock.
                PermLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    PermissionGroups = new List<PermissionGroup>(_handler.PermissionGroups);
                    PermissionPlayers = new List<PermissionPlayer>(_handler.PermissionPlayers);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] SaveToDisk Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseReaderLock();
                }

                JsonSerializer serializer = new JsonSerializer();
                // https://stackoverflow.com/questions/24025350/xamarin-android-json-net-serilization-fails-on-4-2-2-device-only-timezonenotfoun
                serializer.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                serializer.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                serializer.NullValueHandling = NullValueHandling.Include;

                using (StreamWriter sw = new StreamWriter(_groupPermissionsPath, false, Encoding.UTF8))
                {
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        serializer.Serialize(writer, PermissionGroups);
                    }
                }

                using (StreamWriter sw = new StreamWriter(_playerPermissionsPath, false, Encoding.UTF8))
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
                File.WriteAllText(_groupPermissionsPath, grouppermissions);
                File.WriteAllText(_playerPermissionsPath, playerpermissions);
            }
        }

        /// <summary>
        /// Gets a shallow copy of all registered permission groups.
        /// </summary>
        /// <returns>A list of PermissionGroup objects.</returns>
        public List<PermissionGroup> GetPermissionGroups()
        {
            PermLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return new List<PermissionGroup>(_handler.PermissionGroups);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] GetPermissionGroups Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseReaderLock();
            }
            
            return new List<PermissionGroup>();
        }
        
        /// <summary>
        /// Gets a shallow copy of all player permission records stored in the database.
        /// </summary>
        /// <returns>A list of PermissionPlayer objects.</returns>
        public List<PermissionPlayer> GetPermissionPlayers()
        {
            PermLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return new List<PermissionPlayer>(_handler.PermissionPlayers);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] GetPermissionPlayers Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseReaderLock();
            }
            
            return new List<PermissionPlayer>();
        }

        /// <summary>
        /// Retrieves a permission group record by its name.
        /// </summary>
        /// <param name="groupname">The case-insensitive name of the group.</param>
        /// <returns>The found PermissionGroup object, or null if it does not exist.</returns>
        public PermissionGroup GetGroupByName(string groupname)
        {
            PermLock.AcquireReaderLock(Timeout.Infinite);
            groupname = groupname.Trim().ToLower();
            uint uniqueid = GetUniqueID(groupname);
            try
            {
                return _handler.PermissionGroups.FirstOrDefault(x => x.UniqueID == uniqueid);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] GetGroupByName Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseReaderLock();
            }

            return null;
        }

        /// <summary>
        /// Retrieves a permission group record by its unique numeric ID.
        /// </summary>
        /// <param name="groupid">The unique identifier of the group.</param>
        /// <returns>The found PermissionGroup object, or null if it does not exist.</returns>
        public PermissionGroup GetGroupByID(int groupid)
        {
            PermLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _handler.PermissionGroups.FirstOrDefault(x => x.UniqueID == groupid);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] GetGroupByID Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseReaderLock();
            }

            return null;
        }

        /// <summary>
        /// Retrieves a player's permission record from the database by their SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <returns>The PermissionPlayer record, or null if not found.</returns>
        public PermissionPlayer GetPlayerBySteamID(ulong steamid)
        {
            PermLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                return _handler.PermissionPlayers.FirstOrDefault(x => x.SteamID == steamid);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] GetPlayerBySteamID Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseReaderLock();
            }

            return null;
        }
        
        /// <summary>
        /// Retrieves a player's permission record from the database using a Player instance.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <returns>The PermissionPlayer record, or null if the player is null or record is not found.</returns>
        public PermissionPlayer GetPlayerBySteamID(Player player)
        {
            return player == null ? null : GetPlayerBySteamID(player.UID);
        }

        /// <summary>
        /// Determines if a player is assigned to the specified permission group.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <param name="groupname">The name of the group.</param>
        /// <returns>True if the player is in the group, otherwise, false.</returns>
        public bool PlayerHasGroup(Player player, string groupname)
        {
            return player != null && PlayerHasGroup(player.UID, groupname);
        }
        
        /// <summary>
        /// Determines if a player is assigned to the specified permission group by SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <param name="groupname">The name of the group.</param>
        /// <returns>True if the player is in the group, otherwise, false.</returns>
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
        /// Determines if the provided PermissionPlayer record is assigned to the specified group.
        /// </summary>
        /// <param name="permissionplayer">The permission player record.</param>
        /// <param name="groupname">The name of the group.</param>
        /// <returns>True if the player record contains the group, otherwise, false.</returns>
        public bool PlayerHasGroup(PermissionPlayer permissionplayer, string groupname)
        {
            return permissionplayer != null && PlayerHasGroup(permissionplayer.SteamID, groupname);
        }
        
        /// <summary>
        /// Validates if a player possesses a specific permission, accounting for direct assignments, group memberships, and wildcards.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <param name="permission">The permission string to validate.</param>
        /// <returns>True if access is granted, otherwise, false.</returns>
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
                    bool haspermission = defaul.GroupPermissions.Any(x => Matches(x, permission));
                    if (haspermission) 
                        return true;
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
                
                bool haspermission = group.GroupPermissions.Any(x => Matches(x, permission));
                if (haspermission) 
                    return true;
            }

            foreach (var x in permissionplayer.Permissions)
            {
                if (Matches(x, permission)) 
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Validates if a PermissionPlayer possesses a specific permission string.
        /// </summary>
        /// <param name="permissionPlayer">The permission player record.</param>
        /// <param name="permission">The permission string.</param>
        /// <returns>True if access is granted, otherwise, false.</returns>
        public bool PlayerHasPermission(PermissionPlayer permissionPlayer, string permission)
        {
            return permissionPlayer != null && PlayerHasPermission(permissionPlayer.SteamID, permission);
        }
        
        /// <summary>
        /// Validates if a player possui possess a specific permission string.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <param name="permission">The permission string.</param>
        /// <returns>True if access is granted, otherwise, false.</returns>
        public bool PlayerHasPermission(Player player, string permission)
        {
            return player != null && PlayerHasPermission(player.UID, permission);
        }

        /// <summary>
        /// Creates a new player entry in the permission database for the given Player instance.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <returns>The newly created or existing PermissionPlayer record.</returns>
        public PermissionPlayer CreatePermissionPlayer(Player player)
        {
            return player == null ? null : CreatePermissionPlayer(player.UID);
        }
        
        /// <summary>
        /// Creates a new player entry in the permission database for the given SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID to register.</param>
        /// <returns>The newly created or existing PermissionPlayer record.</returns>
        public PermissionPlayer CreatePermissionPlayer(ulong steamid)
        {
            PermissionEvent pe = new PermissionEvent(PermissionActionType.CreatePermissionPlayer, steamId: steamid);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return null;
            }
            
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer == null)
            {
                PermissionPlayer permissionPlayer = new PermissionPlayer()
                {
                    Groups = new List<string>(),
                    Permissions = new List<string>(),
                    SteamID = steamid
                };
                
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _handler.PermissionPlayers.Add(permissionPlayer);
                    return permissionPlayer;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem Error] CreatePermissionPlayer: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return permissionplayer;
        }
        
        /// <summary>
        /// Registers a new player with an initial set of groups and direct permissions.
        /// </summary>
        /// <param name="steamid">The SteamID to register.</param>
        /// <param name="groups">Initial list of group names.</param>
        /// <param name="permissions">Initial list of permission strings.</param>
        /// <returns>The newly created or existing PermissionPlayer record.</returns>
        public PermissionPlayer CreatePermissionPlayer(ulong steamid, List<string> groups, List<string> permissions)
        {
            PermissionEvent pe = new PermissionEvent(PermissionActionType.CreatePermissionPlayer, steamId: steamid);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return null;
            }
            
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer == null)
            {
                PermissionPlayer permissionPlayer = new PermissionPlayer()
                {
                    Groups = groups,
                    Permissions = permissions,
                    SteamID = steamid
                };
                
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _handler.PermissionPlayers.Add(permissionPlayer);
                    return permissionPlayer;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] CreatePermissionPlayer Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return permissionplayer;
        }

        /// <summary>
        /// Deletes the permission record for the given Player instance from the database.
        /// </summary>
        /// <param name="player">The player instance to remove.</param>
        /// <returns>True if the record was successfully removed, otherwise, false.</returns>
        public bool RemovePermissionPlayer(Player player)
        {
            return player != null && RemovePermissionPlayer(player.UID);
        }

        /// <summary>
        /// Deletes a specific PermissionPlayer object from the database.
        /// </summary>
        /// <param name="permissionPlayer">The record object to remove.</param>
        /// <returns>True if the record was removed, otherwise, false.</returns>
        public bool RemovePermissionPlayer(PermissionPlayer permissionPlayer)
        {
            if (permissionPlayer == null)
            {
                return false;
            }
            
            PermLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_handler.PermissionPlayers.Contains(permissionPlayer))
                {
                    _handler.PermissionPlayers.Remove(permissionPlayer);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] RemovePermissionPlayer Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Deletes a player's permission record from the database by their SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID to remove.</param>
        /// <returns>True if the record was found and removed, otherwise, false.</returns>
        public bool RemovePermissionPlayer(ulong steamid)
        {
            PermissionEvent pe = new PermissionEvent(PermissionActionType.RemovePermissionPlayer, steamId: steamid);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }
            
            var permissionplayer = GetPlayerBySteamID(steamid);
            if (permissionplayer != null)
            {
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    _handler.PermissionPlayers.Remove(permissionplayer);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] RemovePermissionPlayer (ulong) Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return false;
        }
        
        /// <summary>
        /// Assigns a player to a specific permission group.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <param name="groupname">The name of the group to add.</param>
        /// <returns>True if the group was added, false if the player was not found or already in the group.</returns>
        public bool AddGroupToPlayer(ulong steamid, string groupname)
        {
            groupname = groupname.Trim().ToLower();
            
            PermissionEvent pe = new PermissionEvent(PermissionActionType.AddGroupToPlayer, steamId: steamid, groupName: groupname);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }
            PermLock.AcquireWriterLock(Timeout.Infinite);
            try
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

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] AddGroupToPlayer Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Removes a player from a specific permission group.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <param name="groupname">The name of the group to remove.</param>
        /// <returns>True if the player was removed from the group, otherwise, false.</returns>
        public bool RemoveGroupFromPlayer(ulong steamid, string groupname)
        {
            groupname = groupname.Trim().ToLower();
            
            PermissionEvent pe = new PermissionEvent(PermissionActionType.RemoveGroupFromPlayer, steamId: steamid, groupName: groupname);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }

            PermLock.AcquireWriterLock(Timeout.Infinite);
            try
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

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] RemoveGroupFromPlayer Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Creates a new permission group with an optional set of starting permissions and a nickname.
        /// </summary>
        /// <param name="groupname">The unique name for the group.</param>
        /// <param name="permissions">Optional initial list of granted permission strings.</param>
        /// <param name="nickname">Optional display nickname for the group.</param>
        /// <returns>True if the group was created, false if it already exists.</returns>
        public bool CreateGroup(string groupname, List<string> permissions = null, string nickname = null)
        {
            PermissionEvent pe = new PermissionEvent(PermissionActionType.CreateGroup, groupName: groupname, nickName: nickname);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }
            
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
            
            PermLock.AcquireWriterLock(Timeout.Infinite);
            try
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
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] CreateGroup Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Deletes a permission group and unassigns all players currently associated with it.
        /// </summary>
        /// <param name="groupname">The name of the group to disband.</param>
        /// <returns>True if the group was removed, false if it's the "Default" group or was not found.</returns>
        public bool RemoveGroup(string groupname)
        {
            groupname = groupname.Trim().ToLower();
            
            PermissionEvent pe = new PermissionEvent(PermissionActionType.RemoveGroup, groupName: groupname);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }
            
            // Disable the removal of the default group.
            if (groupname == "default")
            {
                return false;
            }
            
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
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
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] RemoveGroup Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return false;
        }

        /// <summary>
        /// Grants a specific permission string to all members of a group.
        /// </summary>
        /// <param name="groupname">The name of the group.</param>
        /// <param name="permission">The permission string to grant.</param>
        /// <returns>True if the operation completed successfully, otherwise, false.</returns>
        public bool AddPermissionToGroup(string groupname, string permission)
        {
            groupname = groupname.Trim().ToLower();
            permission = permission.Trim().ToLower();

            PermissionEvent pe = new PermissionEvent(PermissionActionType.AddPermissionToGroup, groupName: groupname, permission: permission);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }

            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    if (!group.GroupPermissions.Contains(permission))
                    {
                        group.GroupPermissions.Add(permission);
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] AddPermissionToGroup Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return false;
        }

        /// <summary>
        /// Revokes a specific permission string from a group.
        /// </summary>
        /// <param name="groupname">The name of the group.</param>
        /// <param name="permission">The permission string to revoke.</param>
        /// <returns>True if the permission was found and removed, otherwise, false.</returns>
        public bool RemovePermissionFromGroup(string groupname, string permission)
        {
            groupname = groupname.Trim().ToLower();
            permission = permission.Trim().ToLower();
            
            PermissionEvent pe = new PermissionEvent(PermissionActionType.RemovePermissionFromGroup, groupName: groupname, permission: permission);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    if (group.GroupPermissions.Contains(permission))
                    {
                        group.GroupPermissions.Remove(permission);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] RemovePermissionFromGroup Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a permission group explicitly contains a specific permission.
        /// </summary>
        /// <param name="groupname">The name of the group.</param>
        /// <param name="permission">The permission string to check.</param>
        /// <returns>True if the group possesses the permission, otherwise, false.</returns>
        public bool GroupHasPermission(string groupname, string permission)
        {
            groupname = groupname.Trim().ToLower();
            permission = permission.Trim().ToLower();
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                PermLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return group.GroupPermissions.Contains(permission);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] GroupHasPermission Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseReaderLock();
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the human-readable nickname assigned to a group.
        /// </summary>
        /// <param name="groupname">The name of the group.</param>
        /// <param name="nickname">The new nickname.</param>
        /// <returns>True if the nickname was updated, otherwise, false.</returns>
        public bool SetGroupNickName(string groupname, string nickname)
        {
            groupname = groupname.Trim().ToLower();
            PermissionGroup group = GetGroupByName(groupname);

            if (group != null)
            {
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
                {
                    group.NickName = nickname;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] SetGroupNickName Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return false;
        }

        /// <summary>
        /// Renames a permission group and updates all player references to reflect the new name.
        /// </summary>
        /// <param name="groupname">The current name of the group.</param>
        /// <param name="newname">The desired new name.</param>
        /// <returns>True if the renaming was successful, otherwise, false.</returns>
        public bool ChangeGroupName(string groupname, string newname)
        {
            groupname = groupname.Trim().ToLower();
            newname = newname.Trim();
            PermissionGroup group = GetGroupByName(groupname);
            PermissionGroup newGroup = GetGroupByName(newname.ToLower());

            if (group != null && newGroup == null)
            {
                uint id = group.UniqueID;
                PermLock.AcquireWriterLock(Timeout.Infinite);
                try
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
                catch (Exception ex)
                {
                    Logger.LogError($"[PermissionSystem] ChangeGroupName Error: {ex}");
                }
                finally
                {
                    PermLock.ReleaseWriterLock();
                }
            }

            return false;
        }
        
        /// <summary>
        /// Directly grants a specific permission string to a Player instance.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <param name="permission">The permission string to grant.</param>
        /// <returns>True if granted, otherwise, false.</returns>
        public bool AddPermission(Player player, string permission)
        {
            return player != null && AddPermission(player.UID, permission);
        }
        
        /// <summary>
        /// Directly grants a specific permission string to a PermissionPlayer record.
        /// </summary>
        /// <param name="permissionPlayer">The permission player record.</param>
        /// <param name="permission">The permission string to grant.</param>
        /// <returns>True if granted, otherwise, false.</returns>
        public bool AddPermission(PermissionPlayer permissionPlayer, string permission)
        {
            return permissionPlayer != null && AddPermission(permissionPlayer.SteamID, permission);
        }
        
        /// <summary>
        /// Directly grants a specific permission string to a SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <param name="permission">The permission string to grant.</param>
        /// <returns>True if granted, otherwise, false.</returns>
        public bool AddPermission(ulong steamid, string permission)
        {
            permission = permission.Trim().ToLower();
            
            PermissionEvent pe = new PermissionEvent(PermissionActionType.AddPermission, steamId: steamid, permission: permission);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }
            PermLock.AcquireWriterLock(Timeout.Infinite);
            try
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
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] AddPermission Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Directly revokes a specific permission string from a Player instance.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <param name="permission">The permission string to revoke.</param>
        /// <returns>True if the permission was found and revoked, otherwise, false.</returns>
        public bool RemovePermission(Player player, string permission)
        {
            return player != null && RemovePermission(player.UID, permission);
        }
        
        /// <summary>
        /// Directly revokes a specific permission string from a PermissionPlayer record.
        /// </summary>
        /// <param name="permissionPlayer">The permission player record.</param>
        /// <param name="permission">The permission string to revoke.</param>
        /// <returns>True if the permission was found and revoked, otherwise, false.</returns>
        public bool RemovePermission(PermissionPlayer permissionPlayer, string permission)
        {
            return permissionPlayer != null && RemovePermission(permissionPlayer.SteamID, permission);
        }
        
        /// <summary>
        /// Directly revokes a specific permission string from a SteamID.
        /// </summary>
        /// <param name="steamid">The SteamID of the player.</param>
        /// <param name="permission">The permission string to revoke.</param>
        /// <returns>True if the permission was found and revoked, otherwise, false.</returns>
        public bool RemovePermission(ulong steamid, string permission)
        {
            permission = permission.Trim().ToLower();
            
            PermissionEvent pe = new PermissionEvent(PermissionActionType.RemovePermission, steamId: steamid, permission: permission);
            if (Hooks.OnPermissionChangeHook(pe))
            {
                return false;
            }
            PermLock.AcquireWriterLock(Timeout.Infinite);
            try
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
            catch (Exception ex)
            {
                Logger.LogError($"[PermissionSystem] RemovePermission Error: {ex}");
            }
            finally
            {
                PermLock.ReleaseWriterLock();
            }

            return false;
        }

        /// <summary>
        /// Validates if a requested permission string matches a pattern, supporting exact matches and hierarchical wildcards (e.g., "admin.*").
        /// </summary>
        /// <param name="pattern">The stored permission pattern.</param>
        /// <param name="requested">The specific permission being validated.</param>
        /// <returns>True if the pattern authorizes the requested permission, otherwise, false.</returns>
        public bool Matches(string pattern, string requested)
        {
            pattern = pattern.Trim().ToLower();
            requested = requested.Trim().ToLower();

            if (pattern == "*") // global wildcard
                return true;

            if (pattern.EndsWith(".*")) // hierarchical wildcard
            {
                string prefix = pattern.Substring(0, pattern.Length - 2); // drop ".*"
                return requested == prefix || requested.StartsWith($"{prefix}.");
            }

            return pattern == requested; // exact match
        }
    }
}