using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fougerite.Caches;
using Fougerite.Concurrent;
using Fougerite.Events;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;

namespace Fougerite
{
    public class Server
    {
        private ItemsBlocks _items;
        private readonly ConcurrentDictionary<ulong, Player> _players = new ConcurrentDictionary<ulong, Player>();
        private readonly object _playersCacheLock = new object();
        private static readonly Lazy<Server> Instance = new Lazy<Server>(() => new Server());
        private bool _serverLoaded;
        private bool HRustPP;
        [Obsolete("The bans were moved into the DataStore ages ago.", false)]
        private readonly string _globalBanListIni = Path.Combine(Util.GetRootFolder(), Path.Combine("Save", "GlobalBanList.ini"));
        private readonly ConcurrentList<string> _ConsoleCommandCancelList = new ConcurrentList<string>();
        [Obsolete("Use DataStore, this is used in old Javascript plugins from years ago.", false)]
        public Data data = new Data();
        public string server_message_name = "Fougerite";
        /// <summary>
        /// This cache is supposed to be private, so make sure to switch your plugin to use
        /// PlayersCache or GetCachePlayer() if you need to use this.
        /// (We also can't change this to a ConcurrentDictionary because other old plugins may depend on this)
        /// </summary>
        public static IDictionary<ulong, Player> Cache = new Dictionary<ulong, Player>();
        public static IEnumerable<string> ForceCallForCommands = new List<string>();


        /// <summary>
        /// This never should have happened, but back at the time I didn't know how to properly design this
        /// There could have been an internal friend list API, but instead I made this,
        /// and now we have plugins that depend on this, so we can't change it without breaking them.
        /// </summary>
        public void LookForRustPP()
        {
            if (HRustPP) { return; }

            if (PluginLoader.GetInstance().Plugins.ContainsKey("RustPP"))
            {
                HRustPP = true;
            }
        }

        /// <summary>
        /// Some very old banlist ini converter.
        /// </summary>
        internal void UpdateBanlist()
        {
#pragma warning disable CS0618
            if (File.Exists(_globalBanListIni))
#pragma warning restore CS0618
            {
                DataStore.GetInstance().Flush("Ips");
                DataStore.GetInstance().Flush("Ids");
                var ini = GlobalBanList;
                foreach (var ip in ini.EnumSection("Ips"))
                {
                    DataStore.GetInstance().Add("Ips", ip, ini.GetSetting("Ips", ip));
                }
                foreach (var id in ini.EnumSection("Ids"))
                {
                    DataStore.GetInstance().Add("Ids", id, ini.GetSetting("Ids", id));
                }
#pragma warning disable CS0618
                File.Delete(_globalBanListIni);
#pragma warning restore CS0618
                DataStore.GetInstance().Save();
            }
        }

        /// <summary>
        /// Bans an online player from the server, sends notifications, and logs the entry.
        /// </summary>
        /// <param name="player">The player instance to ban.</param>
        /// <param name="Banner">The name of the admin or system issuing the ban.</param>
        /// <param name="reason">The reason provided for the ban.</param>
        /// <param name="Sender">The player instance of the admin (if applicable).</param>
        /// <param name="AnnounceToServer">Whether to broadcast the ban to the entire server.</param>
        public void BanPlayer(Player player, string Banner = "Console", string reason = "You were banned.", Player Sender = null, bool AnnounceToServer = false)
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(player, Banner, reason, Sender));
            if (cancel)
                return;

            const string red = "[color #FF0000]";
            const string green = "[color #009900]";
            const string white = "[color #FFFFFF]";

            if (player.IsOnline && !player.IsDisconnecting)
            {
                player.Message($"{red} {reason}");
                player.Message($"{red} Banned by: {Banner}");
                player.Disconnect();
            }

            if (Sender != null)
            {
                Sender.Message($"You banned {player.Name}");
                Sender.Message($"Player's IP: {player.IP}");
                Sender.Message($"Player's ID: {player.SteamID}");
            }

            if (!AnnounceToServer)
            {
#pragma warning disable CS0618
                var notifyList = Players.Where(pl => pl.Admin 
                                                     || pl.Moderator 
                                                     || PermissionSystem.GetPermissionSystem().PlayerHasPermission(pl, "bansystem.notification"));
                foreach (Player pl in notifyList)
                {
                    pl.Message($"{red}{player.Name}{white} was banned by: {green}{Banner}");
                    pl.Message($"{red} Reason: {reason}");
                }
#pragma warning restore CS0618
            }
            else
            {
                Broadcast($"{red}{player.Name}{white} was banned by: {green}{Banner}");
                Broadcast($"{red} Reason: {reason}");
            }

            BanPlayerIPandID(player.IP, player.SteamID, player.Name, reason, Banner);
        }

        /// <summary>
        /// Permanently bans a player by both IP and SteamID, logging both entries.
        /// </summary>
        /// <param name="ip">The IP address to ban.</param>
        /// <param name="id">The SteamID to ban.</param>
        /// <param name="name">The name of the player.</param>
        /// <param name="reason">The ban reason.</param>
        /// <param name="adminname">The admin name.</param>
        public void BanPlayerIPandID(string ip, string id, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(ip, id, name, reason, adminname));
            if (cancel) 
                return;

            string banLogPath = Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log");
            string timestamp = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}]";

            File.AppendAllText(banLogPath, $"{timestamp} {name}|{ip}|{adminname}|{reason}\r\n");
            File.AppendAllText(banLogPath, $"{timestamp} {name}|{id}|{adminname}|{reason}\r\n");

            DataStore.GetInstance().Add("Ips", ip, name);
            DataStore.GetInstance().Add("Ids", id, name);
        }

        /// <summary>
        /// Bans a specific IP address and logs the entry.
        /// </summary>
        /// <param name="ip">The IP address to ban.</param>
        /// <param name="name">The name of the player associated with the IP.</param>
        /// <param name="reason">The ban reason.</param>
        /// <param name="adminname">The admin name.</param>
        public void BanPlayerIP(string ip, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(ip, name, reason, adminname, false));
            if (cancel) 
                return;

            string timestamp = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}]";
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"), $"{timestamp} {name}|{ip}|{adminname}|{reason}\r\n");

            DataStore.GetInstance().Add("Ips", ip, name);
        }

        /// <summary>
        /// Bans a specific SteamID and logs the entry.
        /// </summary>
        /// <param name="id">The SteamID to ban.</param>
        /// <param name="name">The name of the player associated with the ID.</param>
        /// <param name="reason">The ban reason.</param>
        /// <param name="adminname">The admin name.</param>
        public void BanPlayerID(string id, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(id, name, reason, adminname, true));
            if (cancel) 
                return;

            string timestamp = $"[{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}]";
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"), $"{timestamp} {name}|{id}|{adminname}|{reason}\r\n");

            DataStore.GetInstance().Add("Ids", id, name);
        }

        /// <summary>
        /// Checks if a SteamID is currently in the ban list.
        /// </summary>
        /// <param name="id">The SteamID to check.</param>
        /// <returns>True if banned; otherwise, false.</returns>
        public bool IsBannedID(string id)
        {
            return DataStore.GetInstance().Get("Ids", id) != null;
        }

        /// <summary>
        /// Checks if an IP address is currently in the ban list.
        /// </summary>
        /// <param name="ip">The IP address to check.</param>
        /// <returns>True if banned; otherwise, false.</returns>
        public bool IsBannedIP(string ip)
        {
            return DataStore.GetInstance().Get("Ips", ip) != null;
        }

        /// <summary>
        /// Searches for bans associated with a name and removes the most recent IP and ID matches.
        /// </summary>
        /// <param name="name">The name to search for in the ban list.</param>
        /// <param name="UnBanner">The name of the admin issuing the unban.</param>
        /// <param name="Sender">The admin player instance (if applicable).</param>
        /// <returns>True if any bans were removed; otherwise, false.</returns>
        public bool UnbanByName(string name, string UnBanner = "Console", Player Sender = null)
        {
            var ids = FindIDsOfName(name);
            var ips = FindIPsOfName(name);

            const string red = "[color #FF0000]";
            const string green = "[color #009900]";
            const string white = "[color #FFFFFF]";

            if (ids.Count == 0 && ips.Count == 0)
            {
                Sender?.Message($"{red}Couldn't find any names matching with {name}");
                return false;
            }

#pragma warning disable CS0618
            var notifyList = Players.Where(pl =>
                pl.Admin || pl.Moderator || PermissionSystem.GetPermissionSystem()
                    .PlayerHasPermission(pl, "bansystem.notification"));
            foreach (Player pl in notifyList)
            {
                pl.Message(
                    $"{red}{name}{white} was unbanned by: {green}{UnBanner}{white} Different matches: {ids.Count}");
            }
#pragma warning restore CS0618

            if (ips.Count > 0)
            {
                var iptub = ips.Last();
                DataStore.GetInstance().Remove("Ips", iptub);
            }

            if (ids.Count > 0)
            {
                var idtub = ids.Last();
                DataStore.GetInstance().Remove("Ids", idtub);
            }

            return true;
        }

        /// <summary>
        /// Removes an IP address from the ban list.
        /// </summary>
        /// <param name="ip">The IP address to unban.</param>
        /// <returns>True if the IP was found and removed; otherwise, false.</returns>
        public bool UnbanByIP(string ip)
        {
            if (DataStore.GetInstance().Get("Ips", ip) != null)
            {
                DataStore.GetInstance().Remove("Ips", ip);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a SteamID from the ban list.
        /// </summary>
        /// <param name="id">The SteamID to unban.</param>
        /// <returns>True if the ID was found and removed; otherwise, false.</returns>
        public bool UnbanByID(string id)
        {
            if (DataStore.GetInstance().Get("Ids", id) != null)
            {
                DataStore.GetInstance().Remove("Ids", id);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to return all of the banned ips by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<string> FindIPsOfName(string name)
        {
            var ips = DataStore.GetInstance().Keys("Ips");
            string l = name.ToLower();
            List<string> collection = new List<string>();
            foreach (var ip in ips)
            {
                if (DataStore.GetInstance().Get("Ips", ip) == null) 
                    continue;
                if (DataStore.GetInstance().Get("Ips", ip).ToString().ToLower() == l) 
                    collection.Add(ip.ToString());
            }
            return collection;
        }

        /// <summary>
        /// Tries to return all of the banned ids by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<string> FindIDsOfName(string name)
        {
            var ids = DataStore.GetInstance().Keys("Ids");
            string l = name.ToLower();
            List<string> collection = new List<string>();
            foreach (var id in ids)
            {
                if (DataStore.GetInstance().Get("Ids", id) == null) 
                    continue;
                if (DataStore.GetInstance().Get("Ids", id).ToString().ToLower() == l) 
                    collection.Add(id.ToString());
            }
            return collection;
        }

        /// <summary>
        /// Sends a message to everyone on the server.
        /// </summary>
        /// <param name="arg"></param>
        public void Broadcast(string arg)
        {
            foreach (Player player in Players)
            {
                if (player.IsOnline)
                {
                    player.Message(arg);
                }
            }
        }

        /// <summary>
        /// Sends a message to everyone on the server with a different name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="arg"></param>
        public void BroadcastFrom(string name, string arg)
        {
            foreach (Player player in Players)
            {
                if (player.IsOnline)
                {
                    player.MessageFrom(name, arg);
                }
            }
        }

        /// <summary>
        /// Sends a notification message which will appear in the middle of the screen for everyone.
        /// </summary>
        /// <param name="s"></param>
        public void BroadcastNotice(string s)
        {
            foreach (Player player in Players)
            {
                if (player.IsOnline)
                {
                    player.Notice(s);
                }
            }
        }

        /// <summary>
        /// Sends an inventory notification message which will appear on the right bottom for everyone.
        /// </summary>
        /// <param name="s"></param>
        public void BroadcastInv(string s)
        {
            foreach (Player player in Players)
            {
                if (player.IsOnline)
                {
                    player.InventoryNotice(s);
                }
            }
        }

        /// <summary>
        /// Sends a message directly to the console.
        /// </summary>
        /// <param name="s"></param>
        public void RunServerCommand(string s)
        {
            ConsoleSystem.Run(s);
        }

        /// <summary>
        /// Tries to find the player by uLink.NetworkPlayer
        /// </summary>
        /// <param name="np"></param>
        /// <returns></returns>
        public Player FindByNetworkPlayer(uLink.NetworkPlayer np)
        {
            foreach (Player x in GetServer().Players)
            {
                if (x.PlayerClient.netPlayer == np) 
                    return x;
            }
            return null;
        }

        /// <summary>
        /// Tries to find the player by PlayerClient
        /// </summary>
        /// <param name="pc"></param>
        /// <returns></returns>
        public Player FindByPlayerClient(PlayerClient pc)
        {
            foreach (Player x in GetServer().Players)
            {
                if (x.PlayerClient == pc) 
                    return x;
            }
            return null;
        }

        /// <summary>
        /// Tries to Find the player by SteamID or Name.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public Player FindPlayer(string search)
        {
            if (search.All(char.IsDigit))
            {
                ulong uid;
                if (ulong.TryParse(search, out uid))
                {
                    Player player = GetCachePlayer(uid);
                    if (player != null)
                    {
                        return player;
                    }
                    List<Player> flist = Players.Where(x => x.UID == uid).ToList();
                    if (flist.Count >= 1)
                    {
                        return flist[0];
                    }
                }
                else
                {
                    List<Player> flist = Players.Where(x => x.SteamID == search || x.SteamID.Contains(search)).ToList();
                    if (flist.Count >= 1)
                    {
                        return flist[0];
                    }
                }
            }
            else
            {
                List<Player> list = Players.Where(x => x.Name.ToLower().Contains(search.ToLower()) 
                                                       || string.Equals(x.Name, search, StringComparison.CurrentCultureIgnoreCase)).ToList();
                if (list.Count >= 1)
                {
                    return list[0];
                }
            }
            return null;
        }

        /// <summary>
        /// Tries to Find the player by SteamID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public Player FindPlayer(ulong search)
        {
            Player player = GetCachePlayer(search);
            if (player != null)
            {
                return player;
            }
            
            List<Player> flist = Players.Where(x => x.UID == search).ToList();
            if (flist.Count >= 1)
            {
                return flist[0];
            }

            return null;
        }

        /// <summary>
        /// Returns the instance of the Server class.
        /// </summary>
        /// <returns></returns>
        public static Server GetServer()
        {
            return Instance.Value;
        }

        /// <summary>
        /// Saves the server.
        /// </summary>
        public void Save()
        {
            World.GetWorld().ServerSaveHandler.ManualSave();
        }

        /// <summary>
        /// Returns the Chat History.
        /// </summary>
        public List<string> ChatHistoryMessages
        {
            get
            {
                return Util.GetUtil().ChatHistory.ValuesCopy;
            }
        }

        /// <summary>
        /// Returns the Chat User history.
        /// </summary>
        public List<string> ChatHistoryUsers
        {
            get
            {
#pragma warning disable CS0618 // Type or member is obsolete
                return Data.GetData().chat_history_username;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <summary>
        /// Returns the current ItemBlocks of the Server.
        /// </summary>
        public ItemsBlocks Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
            }
        }

        /// <summary>
        /// Tries to grab the player by ID directly from the cache
        /// where we aren't removing players unless specified in the config. (Thread Safe)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Player GetCachePlayer(ulong id)
        {
            lock (_playersCacheLock)
            {
                Player player;
                Cache.TryGetValue(id, out player);
                return player;
            }
        }

        internal void AddCachePlayer(ulong id, Player player)
        {
            lock (_playersCacheLock)
            {
                Cache[id] = player;
            }
        }

        internal void RemoveCachePlayer(ulong id)
        {
            lock (_playersCacheLock)
            {
                Cache.Remove(id);
            }
        }
        
        /// <summary>
        /// Returns all player's that have connected during runtime, meaning
        /// even offline players can be found in this cache. (Thread safe)
        /// This dictionary is a shallow copy.
        /// </summary>
        public Dictionary<ulong, Player> PlayersCache
        {
            get
            {
                return _players.GetShallowCopy();
            }
        }

        /// <summary>
        /// Returns all online players. (Thread safe)
        /// This list is a shallow copy.
        /// </summary>
        public List<Player> Players
        {
            get
            {
                return _players.ValuesCopy;
            }
        }

        internal void AddPlayer(ulong id, Player player)
        {
            _players[id] = player;
        }

        internal void RemovePlayer(ulong id)
        {
            if (_players.ContainsKey(id))
            {
                _players.TryRemove(id);
            }
        }

        internal bool ContainsPlayer(ulong id)
        {
            return _players.ContainsKey(id);
        }

        /// <summary>
        /// Restricts the specified command globally. (Doesn't modify Player's own Restriction table)
        /// </summary>
        /// <param name="cmd"></param>
        public void RestrictConsoleCommand(string cmd)
        {
            if (!_ConsoleCommandCancelList.Contains(cmd))
            {
                bool result = Hooks.RestrictionChange(null, CommandRestrictionType.ConsoleCommand, 
                    CommandRestrictionScale.Global, cmd, true);
                
                if (!result)
                    _ConsoleCommandCancelList.Add(cmd);
            }
        }

        /// <summary>
        /// UnRestricts the specified command globally. (Doesn't modify Player's own Restriction table)
        /// </summary>
        /// <param name="cmd"></param>
        public void UnRestrictConsoleCommand(string cmd)
        {
            if (_ConsoleCommandCancelList.Contains(cmd))
            {
                bool result = Hooks.RestrictionChange(null, CommandRestrictionType.ConsoleCommand, 
                    CommandRestrictionScale.Global, cmd, false);
                
                if (!result)
                    _ConsoleCommandCancelList.Remove(cmd);
            }
        }

        /// <summary>
        /// Clears all globally restricted commands. (Doesn't modify Player's own Restriction table)
        /// </summary>
        public void CleanRestrictedConsoleCommands()
        {
            foreach (string x in ConsoleCommandCancelList)
            {
                UnRestrictConsoleCommand(x);
            }
        }

        /// <summary>
        /// Returns all globally restricted commands.
        /// </summary>
        public List<string> ConsoleCommandCancelList
        {
            get { return _ConsoleCommandCancelList.GetShallowCopy(); }
        }

        /// <summary>
        /// Gets all the current sleepers on the server.
        /// This list is thread / timer safe.
        /// This is a shallow copy.
        /// </summary>
        public List<Sleeper> Sleepers
        {
            get
            {
                return SleeperCache.GetInstance().GetSleepers();
            }
        }
        
        /// <summary>
        /// Returns the Current Fougerite Version.
        /// </summary>
        public string Version
        {
            get { return Bootstrap.Version; }
        }

        /// <summary>
        /// Returns whether the server is fully loaded by this time.
        /// </summary>
        public bool ServerLoaded
        {
            get { return _serverLoaded; }
            internal set { _serverLoaded = value; }
        }

        /// <summary>
        /// Checks If the current Server has Rust++
        /// </summary>
        public bool HasRustPP 
        {
            get { return HRustPP; }
        }

        /// <summary>
        /// Indicates whether the server is in the process of shutting down.
        /// This property retrieves its value from the Hooks.IsShuttingDown field.
        /// </summary>
        public bool IsShuttingDown
        {
            get
            {
                return Hooks.IsShuttingDown;
            }
        }

        /// <summary>
        /// Indicates whether the server has been fully initialized.
        /// This property relies on the state of Hooks.ServerInitialized
        /// and is typically used internally to check server readiness.
        /// </summary>
        public bool ServerInitialized
        {
            get
            {
                return Hooks.ServerInitialized;
            }
        }

        /// <summary>
        /// Tries to Grab the current Rust++ API.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Obsolete. Read RustPPExtension's documentation to find out why.", false)]
        public RustPPExtension GetRustPPAPI()
        {
            if (HasRustPP) 
            {
                 return new RustPPExtension();
            }
            return null;
        }

        public IniParser GlobalBanList
        {
            get
            {
#pragma warning disable CS0618
                if (File.Exists(_globalBanListIni))
                {
                    return new IniParser(_globalBanListIni);
                }
#pragma warning restore CS0618
                return null;
            }
        }
    }
}