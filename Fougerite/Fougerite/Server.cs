
using Fougerite.Events;
using Fougerite.Permissions;
using Fougerite.PluginLoaders;

namespace Fougerite
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;

    public class Server
    {
        private ItemsBlocks _items;
        private StructureMaster _serverStructs = new StructureMaster();
        public Fougerite.Data data = new Fougerite.Data();
        private Dictionary<ulong, Fougerite.Player> players = new Dictionary<ulong, Fougerite.Player>();
        private static Fougerite.Server server;
        private bool HRustPP = false;
        public string server_message_name = "Fougerite";
        public static IDictionary<ulong, Fougerite.Player> Cache = new Dictionary<ulong, Fougerite.Player>();
        public static IEnumerable<string> ForceCallForCommands = new List<string>();
        private readonly string path = Path.Combine(Util.GetRootFolder(), Path.Combine("Save", "GlobalBanList.ini"));
        private readonly List<string> _ConsoleCommandCancelList = new List<string>();


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
            if (File.Exists(path))
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
                File.Delete(path);
                DataStore.GetInstance().Save();
            }
        }

        public void BanPlayer(Fougerite.Player player, string Banner = "Console", string reason = "You were banned.", Fougerite.Player Sender = null, bool AnnounceToServer = false)
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(player, Banner, reason, Sender));
            if (cancel) { return;}
            string red = "[color #FF0000]";
            string green = "[color #009900]";
            string white = "[color #FFFFFF]";
            
            if (player.IsOnline && !player.IsDisconnecting)
            {
                player.Message(red + " " + reason);
                player.Message(red + " Banned by: " + Banner);
                player.Disconnect();
            }
            if (Sender != null)
            {
                Sender.Message("You banned " + player.Name);
                Sender.Message("Player's IP: " + player.IP);
                Sender.Message("Player's ID: " + player.SteamID);
            }
            if (!AnnounceToServer)
            {
#pragma warning disable CS0618
                foreach (Fougerite.Player pl in Players.Where(pl =>
                             pl.Admin || pl.Moderator || PermissionSystem.GetPermissionSystem().PlayerHasPermission(pl, "bansystem.notification")))
#pragma warning restore CS0618
                {
                    pl.Message(red + player.Name + white + " was banned by: " + green + Banner);
                    pl.Message(red + " Reason: " + reason);
                }
            }
            else
            {
                Broadcast(red + player.Name + white + " was banned by: " + green + Banner);
                Broadcast(red + " Reason: " + reason);
            }
            BanPlayerIPandID(player.IP, player.SteamID, player.Name, reason, Banner);
        }

        public void BanPlayerIPandID(string ip, string id, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(ip, id, name, reason, adminname));
            if (cancel) { return; }
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"), "[" + DateTime.Now.ToShortDateString() + " "
                + DateTime.Now.ToString("HH:mm:ss") + "] " + name + "|" + ip + "|" + adminname + "|" + reason + "\r\n");
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"), "[" + DateTime.Now.ToShortDateString()
                + " " + DateTime.Now.ToString("HH:mm:ss") + "] " + name + "|" + id + "|" + adminname + "|" + reason + "\r\n");
            DataStore.GetInstance().Add("Ips", ip, name);
            DataStore.GetInstance().Add("Ids", id, name);
        }

        public void BanPlayerIP(string ip, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(ip, name, reason, adminname, false));
            if (cancel) { return; }
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"), "[" + DateTime.Now.ToShortDateString() + " "
                + DateTime.Now.ToString("HH:mm:ss") + "] " + name + "|" + ip + "|" + adminname + "|" + reason + "\r\n");
            DataStore.GetInstance().Add("Ips", ip, name);
        }

        public void BanPlayerID(string id, string name = "1", string reason = "You were banned.", string adminname = "Unknown")
        {
            bool cancel = Hooks.OnBanEventHandler(new BanEvent(id, name, reason, adminname, true));
            if (cancel) { return; }
            File.AppendAllText(Path.Combine(Util.GetRootFolder(), "Save\\BanLog.log"), "[" + DateTime.Now.ToShortDateString()
                + " " + DateTime.Now.ToString("HH:mm:ss") + "] " + name + "|" + id + "|" + adminname + "|" + reason + "\r\n");
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

        public bool UnbanByName(string name, string UnBanner = "Console", Fougerite.Player Sender = null)
        {
            var ids = FindIDsOfName(name);
            var ips = FindIPsOfName(name);
            string red = "[color #FF0000]";
            string green = "[color #009900]";
            string white = "[color #FFFFFF]";
            if (ids.Count == 0 && ips.Count == 0)
            {
                if (Sender != null) { Sender.Message(red + "Couldn't find any names matching with " + name); }
                return false;
            }
#pragma warning disable CS0618
            foreach (Fougerite.Player pl in Players.Where(pl => 
                         pl.Admin || pl.Moderator ||PermissionSystem.GetPermissionSystem().PlayerHasPermission(pl, "bansystem.notification")))
#pragma warning restore CS0618
            {
                pl.Message(red + name + white + " was unbanned by: "
                           + green + UnBanner + white + " Different matches: " + ids.Count);
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
                if (DataStore.GetInstance().Get("Ips", ip) == null) { continue;}
                if (DataStore.GetInstance().Get("Ips", ip).ToString().ToLower() == l) collection.Add(ip.ToString());
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
                if (DataStore.GetInstance().Get("Ids", id) == null) continue;
                if (DataStore.GetInstance().Get("Ids", id).ToString().ToLower() == l) collection.Add(id.ToString());
            }
            return collection;
        }

        /// <summary>
        /// Sends a message to everyone on the server.
        /// </summary>
        /// <param name="arg"></param>
        public void Broadcast(string arg)
        {
            foreach (Fougerite.Player player in this.Players)
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
            foreach (Fougerite.Player player in this.Players)
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
            foreach (Fougerite.Player player in this.Players)
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
            foreach (Fougerite.Player player in this.Players)
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
        public Fougerite.Player FindByNetworkPlayer(uLink.NetworkPlayer np)
        {
            foreach (var x in Fougerite.Server.GetServer().Players)
            {
                if (x.PlayerClient.netPlayer == null) continue;
                if (x.PlayerClient.netPlayer == np) return x;
            }
            return null;
        }

        /// <summary>
        /// Tries to find the player by PlayerClient
        /// </summary>
        /// <param name="pc"></param>
        /// <returns></returns>
        public Fougerite.Player FindByPlayerClient(PlayerClient pc)
        {
            foreach (var x in Fougerite.Server.GetServer().Players)
            {
                if (x.PlayerClient == pc) return x;
            }
            return null;
        }

        /// <summary>
        /// Tries to Find the player by SteamID or Name.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public Fougerite.Player FindPlayer(string search)
        {
            if (search.All(char.IsDigit))
            {
                ulong uid;
                if (ulong.TryParse(search, out uid))
                {
                    if (Cache.ContainsKey(uid))
                    {
                        return Cache[uid];
                    }
                    var flist = Players.Where(x => x.UID == uid).ToList();
                    if (flist.Count >= 1)
                    {
                        return flist[0];
                    }
                }
                else
                {
                    var flist = Players.Where(x => x.SteamID == search || x.SteamID.Contains(search)).ToList();
                    if (flist.Count >= 1)
                    {
                        return flist[0];
                    }
                }
            }
            else
            {
                var list = Players.Where(x => x.Name.ToLower().Contains(search.ToLower()) || 
                    string.Equals(x.Name, search, StringComparison.CurrentCultureIgnoreCase)).ToList();
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
        public Fougerite.Player FindPlayer(ulong search)
        {
            if (Cache.ContainsKey(search))
            {
                return Cache[search];
            }
            var flist = Players.Where(x => x.UID == search).ToList();
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
        public static Fougerite.Server GetServer()
        {
            if (server == null)
            {
                server = new Fougerite.Server();
            }
            return server;
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
                return Fougerite.Data.GetData().chat_history;
            }
        }

        /// <summary>
        /// Returns the Chat User history.
        /// </summary>
        public List<string> ChatHistoryUsers
        {
            get
            {
                return Fougerite.Data.GetData().chat_history_username;
            }
        }

        /// <summary>
        /// Returns the current ItemBlocks of the Server.
        /// </summary>
        public ItemsBlocks Items
        {
            get
            {
                return this._items;
            }
            set
            {
                this._items = value;
            }
        }

        /// <summary>
        /// Returns all online players. (Iteration safe.)
        /// </summary>
        public List<Fougerite.Player> Players
        {
            get
            {
                return this.players.Values.ToList();
            }
        }

        internal void AddPlayer(ulong id, Fougerite.Player player)
        {
            if (!this.players.ContainsKey(id))
            {
                this.players.Add(id, player);
            }
            else
            {
                this.players[id] = player;
            }
        }

        internal void RemovePlayer(ulong id)
        {
            if (this.players.ContainsKey(id))
            {
                this.players.Remove(id);
            }
        }

        internal bool ContainsPlayer(ulong id)
        {
            return this.players.ContainsKey(id);
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
            get { return this._ConsoleCommandCancelList; }
        }

        internal Dictionary<ulong, Player> DPlayers
        {
            get { return this.players; }
        } 

        /// <summary>
        /// Gets all the current sleepers on the server.
        /// </summary>
        public List<Sleeper> Sleepers
        {
            get
            {
                var query = from s in UnityEngine.Object.FindObjectsOfType<SleepingAvatar>()
                            select new Sleeper(s.GetComponent<DeployableObject>());
                return query.ToList<Sleeper>();
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
                if (File.Exists(path))
                {
                    return new IniParser(path);
                }
                return null;
            }
        }
    }
}