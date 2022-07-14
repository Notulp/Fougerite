using System.Text.RegularExpressions;
using System.Threading;

namespace Fougerite
{
    using Fougerite.Events;
    using System;
    using System.Linq;
    using System.Timers;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Represents an ONLINE player.
    /// </summary>
    public class Player
    {
        private long connectedAt;
        private readonly long connectedAt2;
        private readonly double connectedAtSeconds;
        private long disconnecttime = -1;
        private PlayerInv inv;
        internal bool justDied;
        private PlayerClient ourPlayer;
        private readonly ulong uid;
        private string name;
        private string ipaddr;
        private readonly List<string> _CommandCancelList;
        private readonly List<string> _ConsoleCommandCancelList;
        private bool disconnected;
        private Vector3 _lastpost;
        internal bool _adminoff = false;
        internal bool _modoff = false;
        internal uLink.NetworkPlayer _np;

        public Player()
        {
            this.justDied = true;
        }

        public Player(PlayerClient client)
        {
            this.disconnected = false;
            this.justDied = true;
            this.ourPlayer = client;
            this.connectedAt = DateTime.UtcNow.Ticks;
            this.connectedAt2 = System.Environment.TickCount;
            this.connectedAtSeconds = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
            this.uid = client.netUser.userID;
            this.name = client.netUser.displayName;
            this.ipaddr = client.netPlayer.externalIP;
            this._CommandCancelList = new List<string>();
            this._ConsoleCommandCancelList = new List<string>();
            this._lastpost = Vector3.zero;
            this._np = client.netUser.networkPlayer;
        }

        internal void UpdatePlayerClient(PlayerClient client)
        {
            this.ourPlayer = client;
            if (client.netUser != null)
            {
                this._np = client.netUser.networkPlayer;
            }
        }

        /// <summary>
        /// Returns if the player is Online.
        /// </summary>
        public bool IsOnline
        {
            get
            {
                if (this.ourPlayer != null)
                {
                    if (this.ourPlayer.netUser != null)
                    {
                        return Fougerite.Server.GetServer().ContainsPlayer(uid) && ourPlayer.netUser.connected &&
                               !IsDisconnecting;
                        //return (!this.ourPlayer.netUser.disposed && this.ourPlayer.netUser.connected && Fougerite.Server.GetServer().Players.Contains(this));
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns if the player is alive.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                float hp = 0;
                try
                {
                    if (this.IsOnline)
                    {
                        hp = this.Health;
                    }
                }
                catch
                {
                    // Ignore
                }
                return hp > 0;
            }
        }

        /// <summary>
        /// Returns if the player is disconnecting or disconnected. You may want to use IsOnline instead.
        /// </summary>
        public bool IsDisconnecting
        {
            get { return disconnected; }
            set { disconnected = value; }
        }

        /// <summary>
        /// Returns the Character of the player.
        /// </summary>
        public Character Character
        {
            get
            {
                if (this.IsOnline)
                {
                    Character c = this.PlayerClient.controllable.GetComponent<Character>();
                    return c;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the uLink.NetworkPlayer class of this player.
        /// </summary>
        public uLink.NetworkPlayer NetworkPlayer
        {
            get { return this._np; }
        }

        /// <summary>
        /// Returns the time when this player connected in DateTime.UtcNow.Ticks.
        /// </summary>
        public long ConnectedAt
        {
            get { return this.connectedAt; }
        }

        /// <summary>
        /// Returns the time when this player connected in System.Environment.Ticks
        /// </summary>
        public long ConnectedAt2
        {
            get { return this.connectedAt2; }
        }

        /// <summary>
        /// Returns the time when this player connected in TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds
        /// </summary>
        public double ConnectedAtSeconds
        {
            get { return this.connectedAtSeconds; }
        }

        /// <summary>
        /// Deals a specific amount of damage to the player.
        /// </summary>
        /// <param name="dmg"></param>
        public void Damage(float dmg)
        {
            if (this.IsOnline)
            {
                TakeDamage.HurtSelf(this.PlayerClient.controllable.character, dmg);
            }
        }

        public void OnConnect(NetUser user)
        {
            this.justDied = true;
            this.ourPlayer = user.playerClient;
            this.connectedAt = DateTime.UtcNow.Ticks;
            this.name = user.displayName;
            this.ipaddr = user.networkPlayer.externalIP;
        }

        public void OnDisconnect()
        {
            this.justDied = false;
        }

        /// <summary>
        /// Disconnects the player from the server.
        /// </summary>
        public void Disconnect()
        {
            if (this.IsOnline)
            {
                Disconnect(true, NetError.Facepunch_Kick_RCON);
            }
        }

        /// <summary>
        /// Disconnects the player from the server.
        /// </summary>
        /// <param name="SendNotification"></param>
        public void Disconnect(bool SendNotification = true, NetError NetErrorReason = NetError.Facepunch_Kick_RCON)
        {
            if (this.IsOnline)
            {
                //Logger.LogError("Same? " + Thread.CurrentThread.ManagedThreadId + " - " +  Bootstrap.CurrentThread.ManagedThreadId);
                if (Thread.CurrentThread.ManagedThreadId != Util.GetUtil().MainThreadID)
                {
                    //Logger.LogError("Nope, invoking");
                    Loom.QueueOnMainThread(() => { this.Disconnect(SendNotification, NetErrorReason); });
                    return;
                }

                Server.GetServer().RemovePlayer(uid);
                this.ourPlayer.netUser.Kick(NetErrorReason, SendNotification);
                IsDisconnecting = true;
            }
        }

        /// <summary>
        /// The specified console command cannot be used by this player.
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
        /// The specified console command will be unrestricted and the player will be able to use It again.
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
        /// Does what It says.
        /// </summary>
        public void CleanRestrictedConsoleCommands()
        {
            ConsoleCommandCancelList.Clear();
        }

        /// <summary>
        /// The specified command cannot be used by this player.
        /// </summary>
        /// <param name="cmd"></param>
        public void RestrictCommand(string cmd)
        {
            if (!CommandCancelList.Contains(cmd))
            {
                CommandCancelList.Add(cmd);
            }
        }

        /// <summary>
        /// The specified command will be unrestricted and the player will be able to use It again.
        /// </summary>
        /// <param name="cmd"></param>
        public void UnRestrictCommand(string cmd)
        {
            if (CommandCancelList.Contains(cmd))
            {
                CommandCancelList.Remove(cmd);
            }
        }

        /// <summary>
        /// Does what It says.
        /// </summary>
        public void CleanRestrictedCommands()
        {
            CommandCancelList.Clear();
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public Fougerite.Player Find(string search)
        {
            return Search(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Fougerite.Player Search(string search)
        {
            return Fougerite.Server.GetServer().FindPlayer(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Fougerite.Player FindBySteamID(string search)
        {
            return Fougerite.Server.GetServer().FindPlayer(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Fougerite.Player FindByGameID(string search)
        {
            return FindBySteamID(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Fougerite.Player FindByName(string search)
        {
            return Fougerite.Server.GetServer().FindPlayer(search);
        }

        /// <summary>
        /// Finds the player by stating uLink.NetworkPlayer
        /// </summary>
        /// <param name="np"></param>
        /// <returns></returns>
        public static Fougerite.Player FindByNetworkPlayer(uLink.NetworkPlayer np)
        {
            foreach (var x in Fougerite.Server.GetServer().Players)
            {
                if (x.PlayerClient.netPlayer == np) return x;
            }

            return null;
        }

        /// <summary>
        /// Finds the player by stating PlayerClient
        /// </summary>
        /// <param name="pc"></param>
        /// <returns></returns>
        public static Fougerite.Player FindByPlayerClient(PlayerClient pc)
        {
            foreach (var x in Fougerite.Server.GetServer().Players)
            {
                if (x.PlayerClient == pc) return x;
            }

            return null;
        }

        /// <summary>
        /// Creates an Inventory Notice message on the right.
        /// </summary>
        /// <param name="arg"></param>
        public void InventoryNotice(string arg)
        {
            if (this.IsOnline)
            {
                Rust.Notice.Inventory(this.ourPlayer.netPlayer, arg);
            }
        }

        /// <summary>
        /// Kills the player.
        /// </summary>
        public void Kill()
        {
            if (this.IsOnline)
            {
                TakeDamage.KillSelf(this.ourPlayer.controllable.character, null);
            }
        }

        /// <summary>
        /// Sends a message to the player.
        /// </summary>
        /// <param name="arg"></param>
        public void Message(string arg)
        {
            if (this.IsOnline)
            {
                if (string.IsNullOrEmpty(arg) || arg.Length == 0)
                {
                    return;
                }

                string s = Regex.Replace(arg, @"\[/?color\b.*?\]", string.Empty);
                if (string.IsNullOrEmpty(s) || s.Length == 0)
                {
                    return;
                }

                if (s.Length <= 100)
                {
                    if (Bootstrap.RustChat)
                    {
                        this.SendCommand("chat.add " +
                                         Facepunch.Utility.String.QuoteSafe(Fougerite.Server.GetServer()
                                             .server_message_name) + " " + Facepunch.Utility.String.QuoteSafe(arg));
                    }

                    if (Bootstrap.RPCChat)
                    {
                        string text =
                            Facepunch.Utility.String.QuoteSafe(Fougerite.Server.GetServer().server_message_name) + " " +
                            Facepunch.Utility.String.QuoteSafe(arg);
                        uLink.NetworkView.Get(this.PlayerClient.networkView)
                            .RPC(Bootstrap.RPCChatMethod, this.NetworkPlayer, text);
                    }
                }
                else
                {
                    var arr = Regex.Matches(arg, @"\[/?color\b.*?\]")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray();
                    string lastcolor = "";
                    if (arr.Length > 0)
                    {
                        lastcolor = arr[arr.Length - 1];
                    }

                    int i = 0;
                    foreach (var x in Util.GetUtil().SplitInParts(arg, 100))
                    {
                        if (i == 1)
                        {
                            if (Bootstrap.RustChat)
                            {
                                this.SendCommand("chat.add " +
                                                 Facepunch.Utility.String.QuoteSafe(Fougerite.Server.GetServer()
                                                     .server_message_name) + " " +
                                                 Facepunch.Utility.String.QuoteSafe(lastcolor + x));
                            }

                            if (Bootstrap.RPCChat)
                            {
                                string text =
                                    Facepunch.Utility.String.QuoteSafe(Fougerite.Server.GetServer()
                                        .server_message_name) + " " + Facepunch.Utility.String.QuoteSafe(lastcolor + x);
                                uLink.NetworkView.Get(this.PlayerClient.networkView)
                                    .RPC(Bootstrap.RPCChatMethod, this.NetworkPlayer, text);
                            }
                        }
                        else
                        {
                            if (Bootstrap.RustChat)
                            {
                                this.SendCommand("chat.add " +
                                                 Facepunch.Utility.String.QuoteSafe(Fougerite.Server.GetServer()
                                                     .server_message_name) + " " +
                                                 Facepunch.Utility.String.QuoteSafe(x));
                            }

                            if (Bootstrap.RPCChat)
                            {
                                string text =
                                    Facepunch.Utility.String.QuoteSafe(Fougerite.Server.GetServer()
                                        .server_message_name) + " " + Facepunch.Utility.String.QuoteSafe(x);
                                uLink.NetworkView.Get(this.PlayerClient.networkView)
                                    .RPC(Bootstrap.RPCChatMethod, this.NetworkPlayer, text);
                            }
                        }

                        i++;
                    }

                    //foreach (var x in Util.GetUtil().SplitInParts(arg, 100))
                    //    this.SendCommand("chat.add " + lastcolor + Facepunch.Utility.String.QuoteSafe(Fougerite.Server.GetServer().server_message_name) + " " + Facepunch.Utility.String.QuoteSafe(x));
                }
            }
        }

        /// <summary>
        /// Sends a message to the player with the specified name "sender".
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="arg"></param>
        public void MessageFrom(string playername, string arg)
        {
            if (this.IsOnline)
            {
                if (string.IsNullOrEmpty(arg) || arg.Length == 0)
                {
                    return;
                }

                string s = Regex.Replace(arg, @"\[/?color\b.*?\]", string.Empty);
                if (string.IsNullOrEmpty(s) || s.Length == 0)
                {
                    return;
                }

                if (s.Length <= 100)
                {
                    if (Bootstrap.RustChat)
                    {
                        this.SendCommand("chat.add " + Facepunch.Utility.String.QuoteSafe(playername) + " " +
                                         Facepunch.Utility.String.QuoteSafe(arg));
                    }

                    if (Bootstrap.RPCChat)
                    {
                        string text = Facepunch.Utility.String.QuoteSafe(playername) + " " +
                                      Facepunch.Utility.String.QuoteSafe(arg);
                        uLink.NetworkView.Get(this.PlayerClient.networkView)
                            .RPC(Bootstrap.RPCChatMethod, this.NetworkPlayer, text);
                    }
                }
                else
                {
                    var arr = Regex.Matches(arg, @"\[/?color\b.*?\]")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray();
                    string lastcolor = "";
                    if (arr.Length > 0)
                    {
                        lastcolor = arr[arr.Length - 1];
                    }

                    int i = 0;
                    foreach (var x in Util.GetUtil().SplitInParts(arg, 100))
                    {
                        if (i == 1)
                        {
                            if (Bootstrap.RustChat)
                            {
                                this.SendCommand("chat.add " + Facepunch.Utility.String.QuoteSafe(playername) + " " +
                                                 Facepunch.Utility.String.QuoteSafe(lastcolor + x));
                            }

                            if (Bootstrap.RPCChat)
                            {
                                string text = Facepunch.Utility.String.QuoteSafe(playername) + " " +
                                              Facepunch.Utility.String.QuoteSafe(lastcolor + x);
                                uLink.NetworkView.Get(this.PlayerClient.networkView)
                                    .RPC(Bootstrap.RPCChatMethod, this.NetworkPlayer, text);
                            }
                        }
                        else
                        {
                            if (Bootstrap.RustChat)
                            {
                                this.SendCommand("chat.add " + Facepunch.Utility.String.QuoteSafe(playername) + " " +
                                                 Facepunch.Utility.String.QuoteSafe(x));
                            }

                            if (Bootstrap.RPCChat)
                            {
                                string text = Facepunch.Utility.String.QuoteSafe(playername) + " " +
                                              Facepunch.Utility.String.QuoteSafe(x);
                                uLink.NetworkView.Get(this.PlayerClient.networkView)
                                    .RPC(Bootstrap.RPCChatMethod, this.NetworkPlayer, text);
                            }
                        }

                        i++;
                    }
                }
            }
        }

        /// <summary>
        /// Sends a notice message to the middle of the screen.
        /// </summary>
        /// <param name="arg"></param>
        public void Notice(string arg)
        {
            if (this.IsOnline)
            {
                Rust.Notice.Popup(this.ourPlayer.netPlayer, "!", arg, 4f);
            }
        }

        /// <summary>
        /// Sends a notice message to the middle of the screen with specified duration and icon.
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="text"></param>
        /// <param name="duration"></param>
        public void Notice(string icon, string text, float duration = 4f)
        {
            if (this.IsOnline)
            {
                Rust.Notice.Popup(this.ourPlayer.netPlayer, icon, text, duration);
            }
        }

        /// <summary>
        /// Sends a console message to the player.
        /// </summary>
        /// <param name="msg"></param>
        public void SendConsoleMessage(string msg)
        {
            if (this.IsOnline)
            {
                ConsoleNetworker.singleton.networkView.RPC<string>("CL_ConsoleMessage", PlayerClient.netPlayer, msg);
            }
        }

        /// <summary>
        /// Sends a console command to the player.
        /// </summary>
        /// <param name="cmd"></param>
        public void SendCommand(string cmd)
        {
            if (this.IsOnline)
            {
                ConsoleNetworker.SendClientCommand(this.ourPlayer.netPlayer, cmd);
            }
        }

        /// <summary>
        /// Teleports the player to another player. Distance is how far from the target player. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="distance"></param>
        /// <param name="callhook"></param>
        /// <returns></returns>
        public bool TeleportTo(Fougerite.Player p, float distance = 1.5f, bool callhook = true)
        {
            if (this.IsOnline)
            {
                if (this == p) // lol
                    return false;

                try
                {
                    Transform transform = p.PlayerClient.controllable.transform; // get the target player's transform
                    Vector3 target = transform.TransformPoint(new Vector3(0f, 0f, (this.Admin ? -distance : distance)));
                    // rcon admin teleports behind target player
                    return this.SafeTeleportTo(target, callhook);
                }
                catch
                {
                    if (p.Location == Vector3.zero) return false;
                    return TeleportTo(p.Location, callhook);
                }
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        public bool SafeTeleportTo(float x, float y, float z, bool callhook = true)
        {
            if (this.IsOnline)
            {
                return this.SafeTeleportTo(new Vector3(x, y, z), callhook);
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        public bool SafeTeleportTo(float x, float z, bool callhook = true)
        {
            if (this.IsOnline)
            {
                return this.SafeTeleportTo(new Vector3(x, 0f, z), callhook);
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call. You can also disable the safechecks made by Fougerite.
        /// </summary>
        public bool SafeTeleportTo(Vector3 target, bool callhook = true, bool dosafechecks = true)
        {
            if (this.IsOnline)
            {
                float maxSafeDistance = 360f;
                float seaLevel = 256f;
                double ms = 500d;
                string me = "SafeTeleport";

                float bumpConst = 0.75f;
                Vector3 bump = Vector3.up * bumpConst;
                Vector3 terrain = new Vector3(target.x, Terrain.activeTerrain.SampleHeight(target), target.z);
                RaycastHit hit;
                IEnumerable<StructureMaster> structures = from s in StructureMaster.AllStructures
                    where s.containedBounds.Contains(terrain)
                    select s;
                if (terrain.y > target.y)
                    target = terrain + bump * 2;

                var structureMasters = structures as StructureMaster[] ?? structures.ToArray();
                if (structureMasters.Count() == 1)
                {
                    if (Physics.Raycast(target, Vector3.down, out hit))
                    {
                        if (hit.collider.name == "HB Hit" && dosafechecks)
                        {
                            // this.Message("There you are.");
                            return false;
                        }
                    }

                    StructureMaster structure = structureMasters.FirstOrDefault<StructureMaster>();
                    if (structure != null && !structure.containedBounds.Contains(target) || hit.distance > 8f)
                        target = hit.point + bump;

                    float distance = Vector3.Distance(this.Location, target);

                    if (distance < maxSafeDistance)
                    {
                        return this.TeleportTo(target, callhook);
                    }
                    else
                    {
                        if (this.TeleportTo(terrain + bump * 2, callhook))
                        {
                            System.Timers.Timer timer = new System.Timers.Timer();
                            timer.Interval = ms;
                            timer.AutoReset = false;
                            timer.Elapsed += delegate(object x, ElapsedEventArgs y)
                            {
                                this.TeleportTo(target, callhook);
                            };
                            timer.Start();
                            return true;
                        }

                        return false;
                    }
                }
                if (!structureMasters.Any())
                {
                    if (terrain.y < seaLevel)
                    {
                        this.Message("That would put you in the ocean.");
                        return false;
                    }

                    if (Physics.Raycast(terrain + Vector3.up * 300f, Vector3.down, out hit))
                    {
                        if (hit.collider.name == "HB Hit" && dosafechecks)
                        {
                            this.Message("There you are.");
                            return false;
                        }

                        Vector3 worldPos = target - Terrain.activeTerrain.transform.position;
                        Vector3 tnPos =
                            new Vector3(Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.x, worldPos.x), 0,
                                Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.z, worldPos.z));
                        float gradient = Terrain.activeTerrain.terrainData.GetSteepness(tnPos.x, tnPos.z);
                        if (gradient > 50f && dosafechecks)
                        {
                            this.Message("It's too steep there.");
                            return false;
                        }

                        target = hit.point + bump * 2;
                    }

                    float distance = Vector3.Distance(this.Location, target);
                    Logger.LogDebug(string.Format("[{0}] player={1}({2}) from={3} to={4} distance={5} terrain={6}", me,
                        this.Name, this.GameID,
                        this.Location.ToString(), target.ToString(), distance.ToString("F2"), terrain.ToString()));

                    return this.TeleportTo(target, callhook);
                }

                Logger.LogDebug(string.Format("[{0}] structures.Count is {1}. Weird.", me,
                    structureMasters.Count().ToString()));
                Logger.LogDebug(string.Format("[{0}] target={1} terrain{2}", me, target.ToString(),
                    terrain.ToString()));
                this.Message("Cannot execute safely with the parameters supplied.");
                return false;
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to the closest rust spawnpoint that is the closest to the specified vector. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="callhook"></param>
        /// <returns></returns>
        public Vector3 TeleportToTheClosestSpawnpoint(Vector3 target, bool callhook = true)
        {
            Vector3 pos;
            Quaternion qt;
            SpawnManager.GetClosestSpawn(target, out pos, out qt);
            if (target != Vector3.zero)
            {
                TeleportTo(pos, callhook);
            }

            return pos;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        public bool TeleportTo(float x, float y, float z, bool callhook = true)
        {
            if (this.IsOnline)
            {
                return this.TeleportTo(new Vector3(x, y, z), callhook);
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        public bool TeleportTo(Vector3 target, bool callhook = true)
        {
            if (this.IsOnline)
            {
                try
                {
                    if (callhook)
                    {
                        Fougerite.Hooks.PlayerTeleport(this, this.Location, target);
                    }
                }
                catch
                {
                }

                return RustServerManagement.Get().TeleportPlayerToWorld(this.ourPlayer.netPlayer, target);
            }

            return false;
        }

        /// <summary>
        /// Enables/Disables the player's admin rights.
        /// </summary>
        /// <param name="state"></param>
        public void ForceAdminOff(bool state)
        {
            if (state && this.ourPlayer.netUser.admin)
            {
                ourPlayer.netUser.SetAdmin(false);
                ourPlayer.netUser.admin = false;
            }

            _adminoff = state;
        }

        /// <summary>
        /// Enables/Disables the player's moderator rights.
        /// </summary>
        /// <param name="state"></param>
        public void ForceModeratorOff(bool state)
        {
            _modoff = state;
        }

        /// <summary>
        /// Gets if the player is an Admin on the Server.
        /// </summary>
        public bool Admin
        {
            get
            {
                if (_adminoff)
                {
                    return false;
                }

                if (this.IsOnline)
                {
                    return this.ourPlayer.netUser.admin;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the Player is in the "Moderators" DataStore table or has the Moderator Rust++ permission.
        /// </summary>
        [ObsoleteAttribute("Most of the plugins should be using PermissionSystem. Refer implementing permissions instead.", false)]
        public bool Moderator
        {
            get
            {
                if (_modoff)
                {
                    return false;
                }

                if (Fougerite.Server.GetServer().HasRustPP)
                {
                    if (Fougerite.Server.GetServer().GetRustPPAPI().IsAdmin(this.UID))
                    {
                        return Server.GetServer().GetRustPPAPI().HasPermission(this.UID, "Moderator"); 
                    }
                }

                return DataStore.GetInstance().ContainsKey("Moderators", SteamID);
            }
        }

        /// <summary>
        /// Returns the SteamID of the player as ulong.
        /// </summary>
        public ulong UID
        {
            get { return this.uid; }
        }

        /// <summary>
        /// Returns the SteamID of the player as string.
        /// </summary>
        public string GameID
        {
            get { return this.uid.ToString(); }
        }

        /// <summary>
        /// Returns the SteamID of the player as string.
        /// </summary>
        public string SteamID
        {
            get { return this.uid.ToString(); }
        }

        /// <summary>
        /// Returns the list of the restricted commands of the player.
        /// </summary>
        public List<string> CommandCancelList
        {
            get { return this._CommandCancelList; }
        }

        /// <summary>
        /// Returns the list of the restricted console commands of the player.
        /// </summary>
        public List<string> ConsoleCommandCancelList
        {
            get
            {
                return _ConsoleCommandCancelList;
            }
        }

        /// <summary>
        /// Checks if the player has the specified blueprint.
        /// </summary>
        /// <param name="dataBlock"></param>
        /// <returns></returns>
        public bool HasBlueprint(BlueprintDataBlock dataBlock)
        {
            if (this.IsOnline)
            {
                PlayerInventory invent = this.Inventory.InternalInventory as PlayerInventory;
                if (invent != null && invent.KnowsBP(dataBlock))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the player has the specified blueprint by blueprint name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasBlueprint(string name)
        {
            if (this.IsOnline)
            {
                foreach (BlueprintDataBlock blueprintDataBlock in this.Blueprints())
                {
                    if (name.ToLower().Equals(blueprintDataBlock.name.ToLower()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets all the blueprints of the player in a list.
        /// </summary>
        /// <returns></returns>
        public ICollection<BlueprintDataBlock> Blueprints()
        {
            if (!this.IsOnline)
            {
                return null;
            }

            PlayerInventory invent = this.Inventory.InternalInventory as PlayerInventory;
            ICollection<BlueprintDataBlock> collection = new List<BlueprintDataBlock>();
            if (invent != null)
            {
                foreach (BlueprintDataBlock blueprintDataBlock in invent.GetBoundBPs())
                {
                    collection.Add(blueprintDataBlock);
                }
            }

            return collection;
        }

        /// <summary>
        /// Gets / Sets the Player's current health.
        /// </summary>
        public float Health
        {
            get
            {
                if (this.IsOnline)
                {
                    if (this.ourPlayer.controllable != null)
                    {
                        return this.ourPlayer.controllable.health;
                    }
                }

                return 0f;
            }
            set
            {
                if (!this.IsOnline)
                    return;

                if (this.ourPlayer.controllable != null)
                {

                    if (value < 0f)
                    {
                        this.ourPlayer.controllable.takeDamage.health = 0f;
                    }
                    else
                    {
                        this.ourPlayer.controllable.takeDamage.health = value;
                    }

                    this.ourPlayer.controllable.takeDamage.Heal(this.ourPlayer.controllable, 0f);
                }
            }
        }

        /// <summary>
        /// Returns the Player's inventory.
        /// </summary>
        public PlayerInv Inventory
        {
            get
            {
                if (this.IsOnline)
                {
                    if (this.justDied)
                    {
                        this.inv = new PlayerInv(this);
                        this.justDied = false;
                    }

                    return this.inv;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the player's network IP address.
        /// </summary>
        public string IP
        {
            get { return this.ipaddr; }
        }

        /// <summary>
        /// Gets if the player is bleeding.
        /// </summary>
        public bool IsBleeding
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return HumanBodyTakeDmg.IsBleeding();
                }

                return false;
            }
        }

        /// <summary>
        /// Returns the player's HumanBodyTakeDamage class if possible.
        /// </summary>
        public HumanBodyTakeDamage HumanBodyTakeDmg
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.ourPlayer.controllable.GetComponent<HumanBodyTakeDamage>();
                }

                return null;
            }
        }

        /// <summary>
        /// Gets if the player is cold.
        /// </summary>
        public bool IsCold
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.ourPlayer.controllable.GetComponent<Metabolism>().IsCold();
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the player is injured.
        /// </summary>
        public bool IsInjured
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return (this.ourPlayer.controllable.GetComponent<FallDamage>().GetLegInjury() != 0f);
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the player is radiation poisoned.
        /// </summary>
        public bool IsRadPoisoned
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().HasRadiationPoisoning();
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the player is warm.
        /// </summary>
        public bool IsWarm
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().IsWarm();
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the player is poisoned.
        /// </summary>
        public bool IsPoisoned
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().IsPoisoned();
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the player is starving.
        /// </summary>
        public bool IsStarving
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.CalorieLevel <= 0.0;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the player is hungry.
        /// </summary>
        public bool IsHungry
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.CalorieLevel < 500.0;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the player's bleeding level.
        /// </summary>
        public float BleedingLevel
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return HumanBodyTakeDmg._bleedingLevel;
                }

                return 0f;
            }
        }

        /// <summary>
        /// Gets the player's calorie level.
        /// </summary>
        public float CalorieLevel
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().GetCalorieLevel();
                }

                return 0f;
            }
        }

        /// <summary>
        /// Gets the player's temperature.
        /// </summary>
        public float CoreTemperature
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().coreTemperature;
                    //return (float) Util.GetUtil().GetInstanceField(typeof(Metabolism), m, "coreTemperature");
                }

                return 0f;
            }
            set
            {
                if (this.IsOnline && this.IsAlive)
                {
                    Metabolism m = this.PlayerClient.controllable.GetComponent<Metabolism>();
                    this.PlayerClient.controllable.GetComponent<Metabolism>().coreTemperature = value;
                    //Util.GetUtil().SetInstanceField(typeof(Metabolism), m, "coreTemperature", value);
                }
            }
        }

        /// <summary>
        /// Increases or decreases the player's calorie level based on the negative or positive value.
        /// </summary>
        /// <param name="amount"></param>
        public void AdjustCalorieLevel(float amount)
        {
            if (!this.IsOnline && !this.IsAlive)
            {
                return;
            }

            if (amount < 0)
            {
                this.PlayerClient.controllable.GetComponent<Metabolism>().SubtractCalories(Math.Abs(amount));
            }
            else if (amount > 0)
            {
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddCalories(amount);
            }
        }

        /// <summary>
        /// Gets or Sets the player's rad level
        /// </summary>
        public float RadLevel
        {
            get
            {
                if (this.IsOnline && this.IsAlive)
                {
                    return this.PlayerClient.controllable.GetComponent<Metabolism>().GetRadLevel();
                }

                return 0f;
            }
        }

        /// <summary>
        /// Adds radiation to the player.
        /// </summary>
        /// <param name="amount"></param>
        public void AddRads(float amount)
        {
            if (this.IsOnline && this.IsAlive)
            {
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddRads(amount);
            }
        }

        /// <summary>
        /// Adds anti radiation to the player.
        /// </summary>
        /// <param name="amount"></param>
        public void AddAntiRad(float amount)
        {
            if (this.IsOnline && this.IsAlive)
            {
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddAntiRad(amount);
            }
        }

        /// <summary>
        /// Adds water to the player.
        /// </summary>
        /// <param name="litres"></param>
        public void AddWater(float litres)
        {
            if (this.IsOnline && this.IsAlive)
            {
                this.PlayerClient.controllable.GetComponent<Metabolism>().AddWater(litres);
            }
        }

        /// <summary>
        /// Increases or decreases the player's poison level based on the negative or positive value. 
        /// </summary>
        /// <param name="amount"></param>
        public void AdjustPoisonLevel(float amount)
        {
            if (this.IsOnline && this.IsAlive)
            {
                if (amount < 0)
                    this.PlayerClient.controllable.GetComponent<Metabolism>().SubtractPosion(Math.Abs(amount));

                else if (amount > 0)
                    this.PlayerClient.controllable.GetComponent<Metabolism>().AddPoison(amount);
            }
        }

        /// <summary>
        /// Gets the player's disconnect location.
        /// </summary>
        public Vector3 DisconnectLocation
        {
            get { return this._lastpost; }
            set { this._lastpost = value; }
        }

        /// <summary>
        /// Gets / Sets the player's location.
        /// </summary>
        public Vector3 Location
        {
            get
            {
                if (this.IsOnline)
                {
                    return this.ourPlayer.lastKnownPosition;
                }

                return Vector3.zero;
            }
            set
            {
                if (this.IsOnline)
                {
                    this.ourPlayer.transform.position.Set(value.x, value.y, value.z);
                }
            }
        }

        /// <summary>
        /// Gets / Sets the player's name
        /// </summary>
        public string Name
        {
            get
            {
                return this.name; // displayName
            }
            set
            {
                if (this.IsOnline)
                {
                    this.name = value;
                    this.ourPlayer.netUser.user.displayname_ = value;
                    this.ourPlayer.userName = value; // displayName
                }
            }
        }

        /// <summary>
        /// Tries to find the player's sleeper if it exists and the player is offline.
        /// </summary>
        public Sleeper Sleeper
        {
            get
            {
                if (this.IsOnline)
                {
                    return null;
                }

                var query = from sleeper in UnityEngine.Object.FindObjectsOfType<SleepingAvatar>()
                    let deployable = sleeper.GetComponent<DeployableObject>()
                    where deployable.ownerID == this.uid
                    select new Sleeper(deployable);

                return query.ToList().FirstOrDefault();
            }
        }

        /// <summary>
        /// Checks if the player is inside a house.
        /// </summary>
        public bool AtHome
        {
            get
            {
                if (this.IsOnline)
                {
                    return this.Structures.Any(e => e.Object is StructureMaster master && master.containedBounds.Contains(this.Location));
                }

                if (this.Sleeper != null)
                {
                    return this.Structures.Any(e => e.Object is StructureMaster master && master.containedBounds.Contains(this.Sleeper.Location));
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the player's ping.
        /// </summary>
        public int Ping
        {
            get
            {
                if (this.IsOnline)
                {
                    return this.ourPlayer.netPlayer.averagePing;
                }

                return int.MaxValue;
            }
        }

        /// <summary>
        /// Returns the Rust PlayerClient class of this player.
        /// </summary>
        public PlayerClient PlayerClient
        {
            get
            {
                if (this.IsOnline)
                {
                    return this.ourPlayer;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the falldamage class of this player.
        /// </summary>
        public FallDamage FallDamage
        {
            get
            {
                if (this.IsOnline)
                {
                    return this.ourPlayer.controllable.GetComponent<FallDamage>();
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the online time of the player.
        /// </summary>
        public long TimeOnline
        {
            get
            {
                if (IsOnline)
                {
                    return ((DateTime.UtcNow.Ticks - this.connectedAt) / 0x2710L);
                }

                return ((disconnecttime - this.connectedAt) / 0x2710L);
            }
        }

        /// <summary>
        /// Gets the player's disconnect time in DateTime.UtcNow.Ticks. Returns -1 if the player is online.
        /// </summary>
        public long DisconnectTime
        {
            get { return disconnecttime; }
            internal set { disconnecttime = value; }
        }

        /// <summary>
        /// Gets the X coordinate of the Player
        /// </summary>
        public float X
        {
            get { return this.ourPlayer.lastKnownPosition.x; }
            set
            {
                if (this.IsOnline)
                {
                    this.ourPlayer.transform.position.Set(value, this.Y, this.Z);
                }
            }
        }

        /// <summary>
        /// Gets the Y coordinate of the Player
        /// </summary>
        public float Y
        {
            get { return this.ourPlayer.lastKnownPosition.y; }
            set
            {
                if (this.IsOnline)
                {
                    this.ourPlayer.transform.position.Set(this.X, value, this.Z);
                }
            }
        }

        /// <summary>
        /// Gets the Z coordinate of the Player
        /// </summary>
        public float Z
        {
            get { return this.ourPlayer.lastKnownPosition.z; }
            set
            {
                if (this.IsOnline)
                {
                    this.ourPlayer.transform.position.Set(this.X, this.Y, value);
                }
            }
        }

        private static Fougerite.Entity[] QueryToEntity<T>(IEnumerable<T> query)
        {
            var enumerable = query.ToList();
            Fougerite.Entity[] these = new Fougerite.Entity[enumerable.Count<T>()];
            for (int i = 0; i < these.Length; i++)
            {
                these[i] = new Fougerite.Entity((enumerable.ElementAtOrDefault<T>(i) as UnityEngine.Component)
                    ?.GetComponent<DeployableObject>());
            }

            return these;
        }

        /// <summary>
        /// Gets all Entities (Buildings) that the player owns.
        /// </summary>
        public Fougerite.Entity[] Structures
        {
            get
            {
                var query = from s in StructureMaster.AllStructures
                    where this.UID == s.ownerID
                    select s;
                var structureMasters = query.ToList();
                Fougerite.Entity[] these = new Fougerite.Entity[structureMasters.Count()];
                for (int i = 0; i < these.Length; i++)
                {
                    these[i] = new Fougerite.Entity(structureMasters.ElementAtOrDefault(i));
                }

                return these;
            }
        }

        /// <summary>
        /// Gets all Entities (Chests, Barricades, etc.) that the player owns.
        /// </summary>
        public Fougerite.Entity[] Deployables
        {
            get
            {
                var query =
                    from d in UnityEngine.Object.FindObjectsOfType(typeof(DeployableObject)) as DeployableObject[]
                    where this.UID == d.ownerID
                    select d;
                return QueryToEntity<DeployableObject>(query);
            }
        }

        /// <summary>
        /// Gets all Entities (Shelters) that the player owns.
        /// </summary>
        public Fougerite.Entity[] Shelters
        {
            get
            {
                var query =
                    from d in UnityEngine.Object.FindObjectsOfType(typeof(DeployableObject)) as DeployableObject[]
                    where d.name.Contains("Shelter") && this.UID == d.ownerID
                    select d;
                return QueryToEntity<DeployableObject>(query);
            }
        }

        /// <summary>
        /// Gets all Entities (Chests, Stashes) that the player owns.
        /// </summary>
        public Fougerite.Entity[] Storage
        {
            get
            {
                var query =
                    from s in UnityEngine.Object.FindObjectsOfType(typeof(SaveableInventory)) as SaveableInventory[]
                    where this.UID == (s.GetComponent<DeployableObject>() as DeployableObject).ownerID
                    select s;
                return QueryToEntity<SaveableInventory>(query);
            }
        }

        /// <summary>
        /// Gets all Entities (Camp Fires) that the player owns.
        /// </summary>
        public Fougerite.Entity[] Fires
        {
            get
            {
                var query = from f in UnityEngine.Object.FindObjectsOfType(typeof(FireBarrel)) as FireBarrel[]
                    where this.UID == (f.GetComponent<DeployableObject>() as DeployableObject).ownerID
                    select f;
                return QueryToEntity<FireBarrel>(query);
            }
        }

        /// <summary>
        /// Checks if the player standing on something.
        /// </summary>
        public bool IsOnGround
        {
            get
            {
                Vector3 lastPosition = Location;
                bool cachedBoolean;
                RaycastHit cachedRaycast;
                Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;

                if (lastPosition == Vector3.zero) return true;
                if (!Facepunch.MeshBatch.MeshBatchPhysics.Raycast(lastPosition + new Vector3(0f, -1.15f, 0f),
                    new Vector3(0f, -1f, 0f),
                    out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                {
                    return true;
                }

                if (cachedhitInstance == null)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(cachedhitInstance.graphicalModel.ToString()))
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if the player is near a building. (3.5m)
        /// </summary>
        public bool IsNearStructure
        {
            get
            {
                var x = Physics.OverlapSphere(Location, 3.5f);
                return x.Any(hit => hit.collider.gameObject.name.Contains("__MESHBATCH_PHYSICAL_OUTPUT"));
            }
        }

        /// <summary>
        /// Checks if the player is standing on a deployable object.
        /// </summary>
        public bool IsOnDeployable
        {
            get
            {
                Vector3 lastPosition = Location;
                bool cachedBoolean;
                RaycastHit cachedRaycast;
                Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
                DeployableObject cachedDeployable;
                if (lastPosition == Vector3.zero) return false;
                if (!Facepunch.MeshBatch.MeshBatchPhysics.Raycast(lastPosition + new Vector3(0f, -1.15f, 0f),
                    new Vector3(0f, -1f, 0f), out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                {
                    return false;
                }

                if (cachedhitInstance == null)
                {
                    cachedDeployable = cachedRaycast.collider.GetComponent<DeployableObject>();
                    if (cachedDeployable != null)
                    {
                        return true;
                    }

                    return false;
                }

                if (string.IsNullOrEmpty(cachedhitInstance.graphicalModel.ToString()))
                {
                    return false;
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if the player is inside a shelter.
        /// </summary>
        public bool IsInShelter
        {
            get
            {
                Vector3 lastPosition = Location;
                bool cachedBoolean;
                RaycastHit cachedRaycast;
                Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
                if (lastPosition == Vector3.zero) return false;
                if (!Facepunch.MeshBatch.MeshBatchPhysics.Raycast(lastPosition + new Vector3(0f, -1.15f, 0f),
                    new Vector3(0f, -1f, 0f),
                    out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                {
                    return false;
                }

                if (cachedhitInstance == null)
                {
                    var cachedsack = "Wood_Shelter(Clone)";
                    var cachedLootableObject = cachedRaycast.collider.gameObject.name;
                    if (cachedLootableObject == cachedsack)
                    {
                        return true;
                    }

                    return false;
                }

                var cachedsack2 = "Wood_Shelter(Clone)";
                if (cachedhitInstance.graphicalModel.ToString() == cachedsack2)
                    return true;
                if (cachedhitInstance.graphicalModel.ToString().Contains(cachedsack2)) return true;
                if (string.IsNullOrEmpty(cachedhitInstance.graphicalModel.ToString()))
                {
                    return false;
                }

                return false;
            }
        }
    }
}
