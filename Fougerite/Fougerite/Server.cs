using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fougerite.Concurrent;
using Fougerite.Events;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;
using Object = UnityEngine.Object;

namespace Fougerite
{
    public class Server
    {
        private ItemsBlocks _items;
        private readonly ConcurrentDictionary<ulong, Player> _players = new ConcurrentDictionary<ulong, Player>();
        private readonly object _playersCacheLock = new object();
        private static Server _server;
        private bool _serverLoaded;
        private bool HRustPP;
        [Obsolete("The bans were moved into the DataStore ages ago.", false)]
        private readonly string _globalBanListIni = Path.Combine(Util.GetRootFolder(), Path.Combine("Save", "GlobalBanList.ini"));
        private readonly List<string> _ConsoleCommandCancelList = new List<string>();
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


        public void LookForRustPP()
        {
            if (HRustPP) { return; }

            if (PluginLoader.GetInstance().Plugins.ContainsKey("RustPP"))
            {
                HRustPP = true;
            }
        }

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

        public void BanPlayer(Player player, string Banner = "Console", string reason = "You were banned.", Player Sender = null, bool AnnounceToServer = false)
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(player, Banner, reason, Sender));
            if (cancel) 
                return;
            
            string red = "[color #FF0000]";
            string green = "[color #009900]";
            string white = "[color #FFFFFF]";
            
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
                foreach (Player pl in Players.Where(pl =>
                             pl.Admin || pl.Moderator || PermissionSystem.GetPermissionSystem().PlayerHasPermission(pl, "bansystem.notification")))
#pragma warning restore CS0618
                {
                    pl.Message($"{red}{player.Name}{white} was banned by: {green}{Banner}");
                    pl.Message($"{red} Reason: {reason}");
                }
            }
            else
            {
                Broadcast($"{red}{player.Name}{white} was banned by: {green}{Banner}");
                Broadcast($"{red} Reason: {reason}");
            }
            BanPlayerIPandID(player.IP, player.SteamID, player.Name, reason, Banner);
        }

        public void BanPlayerIPandID(string ip, string id, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(ip, id, name, reason, adminname));
            if (cancel) 
                return;

            string banLogPath = Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log");
            File.AppendAllText(banLogPath, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}] {name}|{ip}|{adminname}|{reason}\r\n");
            File.AppendAllText(banLogPath, $"[{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}] {name}|{id}|{adminname}|{reason}\r\n");
            DataStore.GetInstance().Add("Ips", ip, name);
            DataStore.GetInstance().Add("Ids", id, name);
        }

        public void BanPlayerIP(string ip, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(ip, name, reason, adminname, false));
            if (cancel) 
                return;
            
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"),
                $"[{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}] {name}|{ip}|{adminname}|{reason}\r\n");
            DataStore.GetInstance().Add("Ips", ip, name);
        }

        public void BanPlayerID(string id, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(id, name, reason, adminname, true));
            if (cancel) 
                return; 
            
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"),
                $"[{DateTime.Now.ToShortDateString()} {DateTime.Now:HH:mm:ss}] {name}|{id}|{adminname}|{reason}\r\n");
            DataStore.GetInstance().Add("Ids", id, name);
        }

        public bool IsBannedID(string id)
        {
            return (DataStore.GetInstance().Get("Ids", id) != null);
        }

        public bool IsBannedIP(string ip)
        {
            return (DataStore.GetInstance().Get("Ips", ip) != null);
        }

        public bool UnbanByName(string name, string UnBanner = "Console", Player Sender = null)
        {
            var ids = FindIDsOfName(name);
            var ips = FindIPsOfName(name);
            string red = "[color #FF0000]";
            string green = "[color #009900]";
            string white = "[color #FFFFFF]";
            if (ids.Count == 0 && ips.Count == 0)
            {
                if (Sender != null) { Sender.Message($"{red}Couldn't find any names matching with {name}"); }
                return false;
            }
#pragma warning disable CS0618
            foreach (Player pl in Players.Where(pl => 
                         pl.Admin || pl.Moderator ||PermissionSystem.GetPermissionSystem().PlayerHasPermission(pl, "bansystem.notification")))
#pragma warning restore CS0618
            {
                pl.Message(
                    $"{red}{name}{white} was unbanned by: {green}{UnBanner}{white} Different matches: {ids.Count}");
            }
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

        public bool UnbanByIP(string ip)
        { 
            if (DataStore.GetInstance().Get("Ips", ip) != null)
            {
                DataStore.GetInstance().Remove("Ips", ip);
                return true;
            }
            return false;
        }

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
            if (_server == null)
            {
                _server = new Server();
            }
            return _server;
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
                return Data.GetData().chat_history;
            }
        }

        /// <summary>
        /// Returns the Chat User history.
        /// </summary>
        public List<string> ChatHistoryUsers
        {
            get
            {
                return Data.GetData().chat_history_username;
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
            if (!ConsoleCommandCancelList.Contains(cmd))
            {
                ConsoleCommandCancelList.Add(cmd);
            }
        }

        /// <summary>
        /// UnRestricts the specified command globally. (Doesn't modify Player's own Restriction table)
        /// </summary>
        /// <param name="cmd"></param>
        public void UnRestrictConsoleCommand(string cmd)
        {
            if (ConsoleCommandCancelList.Contains(cmd))
            {
                ConsoleCommandCancelList.Remove(cmd);
            }
        }

        /// <summary>
        /// Clears all globally restricted commands. (Doesn't modify Player's own Restriction table)
        /// </summary>
        public void CleanRestrictedConsoleCommands()
        {
            ConsoleCommandCancelList.Clear();
        }

        /// <summary>
        /// Returns all globally restricted commands.
        /// </summary>
        public List<string> ConsoleCommandCancelList
        {
            get { return _ConsoleCommandCancelList; }
        }

        /// <summary>
        /// Gets all the current sleepers on the server.
        /// </summary>
        public List<Sleeper> Sleepers
        {
            get
            {
                var query = from s in Object.FindObjectsOfType<SleepingAvatar>()
                            select new Sleeper(s.GetComponent<DeployableObject>());
                return query.ToList();
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
        /// Tries to Grab the current Rust++ API.
        /// </summary>
        /// <returns></returns>
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