using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Facepunch.MeshBatch;
using Fougerite.Caches;
using Fougerite.Concurrent;
using Fougerite.Events;
using UnityEngine;
using String = Facepunch.Utility.String;
using Timer = System.Timers.Timer;

namespace Fougerite
{
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
        private readonly ConcurrentList<string> _CommandCancelList;
        private readonly ConcurrentList<string> _ConsoleCommandCancelList;
        private bool disconnected;
        private Vector3 _lastpost;
        private readonly int _instanceId;
        internal bool _adminoff;
        internal bool _modoff;
        internal uLink.NetworkPlayer _np;

        public Player()
        {
            justDied = true;
        }

        public Player(PlayerClient client)
        {
            _instanceId = client.GetInstanceID();
            disconnected = false;
            justDied = true;
            ourPlayer = client;
            connectedAt = DateTime.UtcNow.Ticks;
            connectedAt2 = Environment.TickCount;
            connectedAtSeconds = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
            uid = client.netUser.userID;
            name = client.netUser.displayName;
            ipaddr = client.netPlayer.externalIP;
            _CommandCancelList = new ConcurrentList<string>();
            _ConsoleCommandCancelList = new ConcurrentList<string>();
            _lastpost = Vector3.zero;
            _np = client.netUser.networkPlayer;
        }

        internal void UpdatePlayerClient(PlayerClient client)
        {
            ourPlayer = client;
            NetUser netUser = ourPlayer.netUser;
            if (netUser != null)
            {
                _np = netUser.networkPlayer;
            }
        }

        /// <summary>
        /// Returns if the player is Online.
        /// </summary>
        public bool IsOnline
        {
            get
            {
                if (ourPlayer != null)
                {
                    NetUser netUser = ourPlayer.netUser;
                    if (netUser != null)
                    {
                        return Server.GetServer().ContainsPlayer(uid) && !netUser.disposed && netUser.connected && !IsDisconnecting;
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
                if (IsOnline)
                { 
                    hp = Health;
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
                if (IsOnline)
                {
                    Character c = PlayerClient.controllable.GetComponent<Character>();
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
            get { return _np; }
        }

        /// <summary>
        /// Returns the time when this player connected in DateTime.UtcNow.Ticks.
        /// </summary>
        public long ConnectedAt
        {
            get { return connectedAt; }
        }

        /// <summary>
        /// Returns the time when this player connected in System.Environment.Ticks
        /// </summary>
        public long ConnectedAt2
        {
            get { return connectedAt2; }
        }

        /// <summary>
        /// Returns the time when this player connected in TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds
        /// </summary>
        public double ConnectedAtSeconds
        {
            get { return connectedAtSeconds; }
        }

        /// <summary>
        /// Deals a specific amount of damage to the player.
        /// </summary>
        /// <param name="dmg"></param>
        public void Damage(float dmg)
        {
            if (IsOnline)
            {
                TakeDamage.HurtSelf(PlayerClient.controllable.character, dmg);
            }
        }

        [Obsolete("Don't use this.", false)]
        public void OnConnect(NetUser user)
        {
            justDied = true;
            ourPlayer = user.playerClient;
            connectedAt = DateTime.UtcNow.Ticks;
            name = user.displayName;
            ipaddr = user.networkPlayer.externalIP;
        }

        [Obsolete("Don't use this.", false)]
        public void OnDisconnect()
        {
            justDied = false;
        }

        /// <summary>
        /// Disconnects the player from the server.
        /// </summary>
        public void Disconnect()
        {
            if (IsOnline)
            {
                Disconnect(true);
            }
        }

        /// <summary>
        /// Disconnects the player from the server.
        /// </summary>
        /// <param name="SendNotification"></param>
        public void Disconnect(bool SendNotification = true, NetError NetErrorReason = NetError.Facepunch_Kick_RCON)
        {
            if (IsOnline)
            {
                if (Thread.CurrentThread.ManagedThreadId != Util.GetUtil().MainThreadID)
                {
                    Loom.QueueOnMainThread(() => { Disconnect(SendNotification, NetErrorReason); });
                    return;
                }

                Server.GetServer().RemovePlayer(uid);
                NetUser netUser = ourPlayer.netUser;
                // Sanity check
                if (netUser != null)
                    ourPlayer.netUser.Kick(NetErrorReason, SendNotification);
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
                bool result = Hooks.RestrictionChange(this, CommandRestrictionType.ConsoleCommand, 
                    CommandRestrictionScale.SpecificPlayer, cmd, true);
                
                if (!result)
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
                bool result = Hooks.RestrictionChange(this, CommandRestrictionType.ConsoleCommand, 
                    CommandRestrictionScale.SpecificPlayer, cmd, false);
                
                if (!result)
                    ConsoleCommandCancelList.Remove(cmd);
            }
        }
        
        /// <summary>
        /// Does what It says.
        /// </summary>
        public void CleanRestrictedConsoleCommands()
        {
            foreach (string x in ConsoleCommandCancelList)
            {
                UnRestrictConsoleCommand(x);
            }
        }

        /// <summary>
        /// The specified command cannot be used by this player.
        /// </summary>
        /// <param name="cmd"></param>
        public void RestrictCommand(string cmd)
        {
            if (!CommandCancelList.Contains(cmd))
            {
                bool result = Hooks.RestrictionChange(this, CommandRestrictionType.Command, 
                    CommandRestrictionScale.SpecificPlayer, cmd, true);
                
                if (!result)
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
                bool result = Hooks.RestrictionChange(this, CommandRestrictionType.Command, 
                    CommandRestrictionScale.SpecificPlayer, cmd, false);
                
                if (!result)
                    CommandCancelList.Remove(cmd);
            }
        }

        /// <summary>
        /// Does what It says.
        /// </summary>
        public void CleanRestrictedCommands()
        {
            foreach (string x in CommandCancelList)
            {
                UnRestrictCommand(x);
            }
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public Player Find(string search)
        {
            return Search(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Player Search(string search)
        {
            return Server.GetServer().FindPlayer(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Player FindBySteamID(string search)
        {
            return Server.GetServer().FindPlayer(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Player FindByGameID(string search)
        {
            return FindBySteamID(search);
        }

        /// <summary>
        /// Finds a specific player by your argument. Can state name or ID.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static Player FindByName(string search)
        {
            return Server.GetServer().FindPlayer(search);
        }

        /// <summary>
        /// Finds the player by stating uLink.NetworkPlayer
        /// </summary>
        /// <param name="np"></param>
        /// <returns></returns>
        public static Player FindByNetworkPlayer(uLink.NetworkPlayer np)
        {
            foreach (var x in Server.GetServer().Players)
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
        public static Player FindByPlayerClient(PlayerClient pc)
        {
            foreach (var x in Server.GetServer().Players)
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
            if (IsOnline)
            {
                Rust.Notice.Inventory(ourPlayer.netPlayer, arg);
            }
        }

        /// <summary>
        /// Kills the player.
        /// </summary>
        public void Kill()
        {
            if (IsOnline)
            {
                TakeDamage.KillSelf(ourPlayer.controllable.character);
            }
        }

        /// <summary>
        /// Sends a message to the player.
        /// </summary>
        /// <param name="arg"></param>
        public void Message(string arg)
        {
            HandleMessage(Server.GetServer().server_message_name, arg);
        }

        /// <summary>
        /// Sends a message to the player with the specified name "sender".
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="arg"></param>
        public void MessageFrom(string playername, string arg)
        {
            HandleMessage(playername, arg);
        }

        /// <summary>
        /// Sends a message to the player with the specified name "sender".
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="arg"></param>
        private void HandleMessage(string playername, string arg)
        {
            if (string.IsNullOrEmpty(arg) || arg.Length == 0)
            {
                return;
            }

            if (!IsOnline)
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
                    SendCommand($"chat.add {String.QuoteSafe(playername)} {String.QuoteSafe(arg)}");
                }

                if (Bootstrap.RPCChat)
                {
                    string text = $"{String.QuoteSafe(playername)} {String.QuoteSafe(arg)}";
                    uLink.NetworkView.Get(PlayerClient.networkView).RPC(Bootstrap.RPCChatMethod, NetworkPlayer, text);
                }
            }
            else
            {
                string[] arr = Regex.Matches(arg, @"\[/?color\b.*?\]")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .ToArray();
                string lastcolor = "";
                if (arr.Length > 0)
                {
                    lastcolor = arr[arr.Length - 1];
                }

                int i = 0;
                foreach (string x in Util.GetUtil().SplitInParts(arg, 100))
                {
                    if (i == 1)
                    {
                        if (Bootstrap.RustChat)
                        {
                            SendCommand($"chat.add {String.QuoteSafe(playername)} {String.QuoteSafe(lastcolor + x)}");
                        }

                        if (Bootstrap.RPCChat)
                        {
                            string text = $"{String.QuoteSafe(playername)} {String.QuoteSafe(lastcolor + x)}";
                            uLink.NetworkView.Get(PlayerClient.networkView).RPC(Bootstrap.RPCChatMethod, NetworkPlayer, text);
                        }
                    }
                    else
                    {
                        if (Bootstrap.RustChat)
                        {
                            SendCommand($"chat.add {String.QuoteSafe(playername)} {String.QuoteSafe(x)}");
                        }

                        if (Bootstrap.RPCChat)
                        {
                            string text = $"{String.QuoteSafe(playername)} {String.QuoteSafe(x)}";
                            uLink.NetworkView.Get(PlayerClient.networkView).RPC(Bootstrap.RPCChatMethod, NetworkPlayer, text);
                        }
                    }

                    i++;
                }
            }
        }

        /// <summary>
        /// Sends a notice message to the middle of the screen.
        /// </summary>
        /// <param name="arg"></param>
        public void Notice(string arg)
        {
            if (IsOnline)
            {
                Rust.Notice.Popup(ourPlayer.netPlayer, "!", arg);
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
            if (IsOnline)
            {
                Rust.Notice.Popup(ourPlayer.netPlayer, icon, text, duration);
            }
        }

        /// <summary>
        /// Sends a console message to the player.
        /// </summary>
        /// <param name="msg"></param>
        public void SendConsoleMessage(string msg)
        {
            if (IsOnline)
            {
                ConsoleNetworker.singleton.networkView.RPC("CL_ConsoleMessage", PlayerClient.netPlayer, msg);
            }
        }

        /// <summary>
        /// Sends a console command to the player.
        /// </summary>
        /// <param name="cmd"></param>
        public void SendCommand(string cmd)
        {
            if (IsOnline)
            {
                ConsoleNetworker.SendClientCommand(ourPlayer.netPlayer, cmd);
            }
        }

        /// <summary>
        /// Teleports the player to another player. Distance is how far from the target player. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="distance"></param>
        /// <param name="callhook"></param>
        /// <returns></returns>
        public bool TeleportTo(Player p, float distance = 1.5f, bool callhook = true)
        {
            if (IsOnline)
            {
                if (this == p) // lol
                    return false;

                try
                {
                    Transform transform = p.PlayerClient.controllable.transform; // get the target player's transform
                    Vector3 target = transform.TransformPoint(new Vector3(0f, 0f, (Admin ? -distance : distance)));
                    // rcon admin teleports behind target player
                    return SafeTeleportTo(target, callhook);
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
            if (IsOnline)
            {
                return SafeTeleportTo(new Vector3(x, y, z), callhook);
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        public bool SafeTeleportTo(float x, float z, bool callhook = true)
        {
            if (IsOnline)
            {
                return SafeTeleportTo(new Vector3(x, 0f, z), callhook);
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call. You can also disable the safechecks made by Fougerite.
        /// </summary>
        public bool SafeTeleportTo(Vector3 target, bool callhook = true, bool dosafechecks = true)
        {
            if (IsOnline)
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

                    StructureMaster structure = structureMasters.FirstOrDefault();
                    if (structure != null && !structure.containedBounds.Contains(target) || hit.distance > 8f)
                        target = hit.point + bump;

                    float distance = Vector3.Distance(Location, target);

                    if (distance < maxSafeDistance)
                    {
                        return TeleportTo(target, callhook);
                    }

                    if (TeleportTo(terrain + bump * 2, callhook))
                    {
                        Timer timer = new Timer();
                        timer.Interval = ms;
                        timer.AutoReset = false;
                        timer.Elapsed += delegate
                        {
                            TeleportTo(target, callhook);
                        };
                        timer.Start();
                        return true;
                    }

                    return false;
                }
                if (!structureMasters.Any())
                {
                    if (terrain.y < seaLevel)
                    {
                        Message("That would put you in the ocean.");
                        return false;
                    }

                    if (Physics.Raycast(terrain + Vector3.up * 300f, Vector3.down, out hit))
                    {
                        if (hit.collider.name == "HB Hit" && dosafechecks)
                        {
                            Message("There you are.");
                            return false;
                        }

                        Vector3 worldPos = target - Terrain.activeTerrain.transform.position;
                        Vector3 tnPos =
                            new Vector3(Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.x, worldPos.x), 0,
                                Mathf.InverseLerp(0, Terrain.activeTerrain.terrainData.size.z, worldPos.z));
                        float gradient = Terrain.activeTerrain.terrainData.GetSteepness(tnPos.x, tnPos.z);
                        if (gradient > 50f && dosafechecks)
                        {
                            Message("It's too steep there.");
                            return false;
                        }

                        target = hit.point + bump * 2;
                    }

                    float distance = Vector3.Distance(Location, target);
                    Logger.LogDebug(
                        $"[{me}] player={Name}({GameID}) from={Location.ToString()} to={target.ToString()} distance={distance:F2} terrain={terrain.ToString()}");

                    return TeleportTo(target, callhook);
                }

                Logger.LogDebug($"[{me}] structures.Count is {structureMasters.Count().ToString()}. Weird.");
                Logger.LogDebug($"[{me}] target={target.ToString()} terrain{terrain.ToString()}");
                Message("Cannot execute safely with the parameters supplied.");
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
            if (IsOnline)
            {
                return TeleportTo(new Vector3(x, y, z), callhook);
            }

            return false;
        }

        /// <summary>
        /// Teleports the player to a position. You may also disable the OnPlayerTeleport hook call.
        /// </summary>
        public bool TeleportTo(Vector3 target, bool callhook = true)
        {
            if (IsOnline)
            {
                try
                {
                    if (callhook)
                    {
                        Hooks.PlayerTeleport(this, Location, target);
                    }
                }
                catch
                {
                    // Ignore.
                }

                return RustServerManagement.Get().TeleportPlayerToWorld(ourPlayer.netPlayer, target);
            }

            return false;
        }

        /// <summary>
        /// Enables/Disables the player's admin rights.
        /// </summary>
        /// <param name="state"></param>
        public void ForceAdminOff(bool state)
        {
            NetUser netUser = ourPlayer.netUser;
            if (state && netUser != null && netUser.admin)
            {
                netUser.SetAdmin(false);
                netUser.admin = false;
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

                if (IsOnline)
                {
                    NetUser netUser = ourPlayer.netUser;
                    return netUser != null && netUser.admin;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets if the Player is in the "Moderators" DataStore table or has the Moderator Rust++ permission.
        /// </summary>
        [Obsolete("Most of the plugins should be using PermissionSystem. Refer implementing permissions instead.", false)]
        public bool Moderator
        {
            get
            {
                if (_modoff)
                {
                    return false;
                }

                if (Server.GetServer().HasRustPP)
                {
                    if (Server.GetServer().GetRustPPAPI().IsAdmin(UID))
                    {
                        return Server.GetServer().GetRustPPAPI().HasPermission(UID, "Moderator"); 
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
            get { return uid; }
        }

        /// <summary>
        /// Returns the SteamID of the player as string.
        /// </summary>
        public string GameID
        {
            get { return uid.ToString(); }
        }

        /// <summary>
        /// Returns the SteamID of the player as string.
        /// </summary>
        public string SteamID
        {
            get { return uid.ToString(); }
        }

        /// <summary>
        /// Returns the list of the restricted commands of the player.
        /// </summary>
        public List<string> CommandCancelList
        {
            get { return _CommandCancelList.GetShallowCopy(); }
        }

        /// <summary>
        /// Returns the list of the restricted console commands of the player.
        /// </summary>
        public List<string> ConsoleCommandCancelList
        {
            get
            {
                return _ConsoleCommandCancelList.GetShallowCopy();
            }
        }

        /// <summary>
        /// Checks if the player has the specified blueprint.
        /// </summary>
        /// <param name="dataBlock"></param>
        /// <returns></returns>
        public bool HasBlueprint(BlueprintDataBlock dataBlock)
        {
            if (IsOnline)
            {
                PlayerInventory invent = Inventory.InternalInventory as PlayerInventory;
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
            if (IsOnline)
            {
                foreach (BlueprintDataBlock blueprintDataBlock in Blueprints())
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
            if (!IsOnline)
            {
                return null;
            }

            PlayerInventory invent = Inventory.InternalInventory as PlayerInventory;
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
                if (IsOnline)
                {
                    if (ourPlayer.controllable != null)
                    {
                        return ourPlayer.controllable.health;
                    }
                }

                return 0f;
            }
            set
            {
                if (!IsOnline)
                    return;

                if (ourPlayer.controllable != null)
                {

                    if (value < 0f)
                    {
                        ourPlayer.controllable.takeDamage.health = 0f;
                    }
                    else
                    {
                        ourPlayer.controllable.takeDamage.health = value;
                    }

                    ourPlayer.controllable.takeDamage.Heal(ourPlayer.controllable, 0f);
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
                if (IsOnline)
                {
                    if (justDied)
                    {
                        inv = new PlayerInv(this);
                        justDied = false;
                    }

                    return inv;
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the player's network IP address.
        /// </summary>
        public string IP
        {
            get { return ipaddr; }
        }

        /// <summary>
        /// Gets if the player is bleeding.
        /// </summary>
        public bool IsBleeding
        {
            get
            {
                if (IsOnline && IsAlive)
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
                if (IsOnline && IsAlive)
                {
                    return ourPlayer.controllable.GetComponent<HumanBodyTakeDamage>();
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
                if (IsOnline && IsAlive)
                {
                    return ourPlayer.controllable.GetComponent<Metabolism>().IsCold();
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
                if (IsOnline && IsAlive)
                {
                    return (ourPlayer.controllable.GetComponent<FallDamage>().GetLegInjury() != 0f);
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
                if (IsOnline && IsAlive)
                {
                    return PlayerClient.controllable.GetComponent<Metabolism>().HasRadiationPoisoning();
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
                if (IsOnline && IsAlive)
                {
                    return PlayerClient.controllable.GetComponent<Metabolism>().IsWarm();
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
                if (IsOnline && IsAlive)
                {
                    return PlayerClient.controllable.GetComponent<Metabolism>().IsPoisoned();
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
                if (IsOnline && IsAlive)
                {
                    return CalorieLevel <= 0.0;
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
                if (IsOnline && IsAlive)
                {
                    return CalorieLevel < 500.0;
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
                if (IsOnline && IsAlive)
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
                if (IsOnline && IsAlive)
                {
                    return PlayerClient.controllable.GetComponent<Metabolism>().GetCalorieLevel();
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
                if (IsOnline && IsAlive)
                {
                    return PlayerClient.controllable.GetComponent<Metabolism>().coreTemperature;
                    //return (float) Util.GetUtil().GetInstanceField(typeof(Metabolism), m, "coreTemperature");
                }

                return 0f;
            }
            set
            {
                if (IsOnline && IsAlive)
                {
                    Metabolism m = PlayerClient.controllable.GetComponent<Metabolism>();
                    PlayerClient.controllable.GetComponent<Metabolism>().coreTemperature = value;
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
            if (!IsOnline && !IsAlive)
            {
                return;
            }

            if (amount < 0)
            {
                PlayerClient.controllable.GetComponent<Metabolism>().SubtractCalories(Math.Abs(amount));
            }
            else if (amount > 0)
            {
                PlayerClient.controllable.GetComponent<Metabolism>().AddCalories(amount);
            }
        }

        /// <summary>
        /// Gets or Sets the player's rad level
        /// </summary>
        public float RadLevel
        {
            get
            {
                if (IsOnline && IsAlive)
                {
                    return PlayerClient.controllable.GetComponent<Metabolism>().GetRadLevel();
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
            if (IsOnline && IsAlive)
            {
                PlayerClient.controllable.GetComponent<Metabolism>().AddRads(amount);
            }
        }

        /// <summary>
        /// Adds anti radiation to the player.
        /// </summary>
        /// <param name="amount"></param>
        public void AddAntiRad(float amount)
        {
            if (IsOnline && IsAlive)
            {
                PlayerClient.controllable.GetComponent<Metabolism>().AddAntiRad(amount);
            }
        }

        /// <summary>
        /// Adds water to the player.
        /// </summary>
        /// <param name="litres"></param>
        public void AddWater(float litres)
        {
            if (IsOnline && IsAlive)
            {
                PlayerClient.controllable.GetComponent<Metabolism>().AddWater(litres);
            }
        }

        /// <summary>
        /// Increases or decreases the player's poison level based on the negative or positive value. 
        /// </summary>
        /// <param name="amount"></param>
        public void AdjustPoisonLevel(float amount)
        {
            if (IsOnline && IsAlive)
            {
                if (amount < 0)
                    PlayerClient.controllable.GetComponent<Metabolism>().SubtractPosion(Math.Abs(amount));

                else if (amount > 0)
                    PlayerClient.controllable.GetComponent<Metabolism>().AddPoison(amount);
            }
        }

        /// <summary>
        /// Gets the player's disconnect location.
        /// </summary>
        public Vector3 DisconnectLocation
        {
            get { return _lastpost; }
            set { _lastpost = value; }
        }

        /// <summary>
        /// Gets / Sets the player's location.
        /// </summary>
        public Vector3 Location
        {
            get
            {
                if (IsOnline)
                {
                    return ourPlayer.lastKnownPosition;
                }

                return Vector3.zero;
            }
            set
            {
                if (IsOnline)
                {
                    ourPlayer.transform.position.Set(value.x, value.y, value.z);
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
                return name; // displayName
            }
            set
            {
                if (IsOnline)
                {
                    name = value;
                    NetUser netUser = ourPlayer.netUser;
                    if (netUser != null)
                        netUser.user.displayname_ = value;
                    ourPlayer.userName = value; // displayName
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
                if (IsOnline)
                {
                    return null;
                }

                Sleeper firstSleeper = Server.GetServer().Sleepers.FirstOrDefault(x => x.UID == uid);
                return firstSleeper;
            }
        }

        /// <summary>
        /// Checks if the player is inside a house.
        /// </summary>
        public bool AtHome
        {
            get
            {
                if (IsOnline)
                {
                    return Structures.Any(e => e.Object is StructureMaster master && master.containedBounds.Contains(Location));
                }

                if (Sleeper != null)
                {
                    return Structures.Any(e => e.Object is StructureMaster master && master.containedBounds.Contains(Sleeper.Location));
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
                if (IsOnline)
                {
                    return ourPlayer.netPlayer.averagePing;
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
                if (IsOnline)
                {
                    return ourPlayer;
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
                if (IsOnline)
                {
                    return ourPlayer.controllable.GetComponent<FallDamage>();
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
                    return ((DateTime.UtcNow.Ticks - connectedAt) / 0x2710L);
                }

                return ((disconnecttime - connectedAt) / 0x2710L);
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
            get { return ourPlayer.lastKnownPosition.x; }
            set
            {
                if (IsOnline)
                {
                    ourPlayer.transform.position.Set(value, Y, Z);
                }
            }
        }

        /// <summary>
        /// Gets the Y coordinate of the Player
        /// </summary>
        public float Y
        {
            get { return ourPlayer.lastKnownPosition.y; }
            set
            {
                if (IsOnline)
                {
                    ourPlayer.transform.position.Set(X, value, Z);
                }
            }
        }

        /// <summary>
        /// Gets the Z coordinate of the Player
        /// </summary>
        public float Z
        {
            get { return ourPlayer.lastKnownPosition.z; }
            set
            {
                if (IsOnline)
                {
                    ourPlayer.transform.position.Set(X, Y, value);
                }
            }
        }

        /// <summary>
        /// Gets all Entities (Buildings) that the player owns.
        /// This array is a shallow copy & thread safe.
        /// </summary>
        public Entity[] Structures
        {
            get
            {
                Entity[] structureMasters = EntityCache.GetInstance().GetEntities().Where(s => s.IsStructureMaster() && UID == s.UOwnerID).ToArray();
                return structureMasters;
            }
        }

        /// <summary>
        /// Gets all Entities (Chests, Barricades, etc.) that the player owns.
        /// This array is a shallow copy & thread safe.
        /// </summary>
        public Entity[] Deployables
        {
            get
            {
                Entity[] deployableObjects = EntityCache.GetInstance().GetEntities().Where(s => s.IsDeployableObject() && UID == s.UOwnerID).ToArray();
                return deployableObjects;
            }
        }

        /// <summary>
        /// Gets all Entities (Shelters) that the player owns.
        /// This array is a shallow copy & thread safe.
        /// </summary>
        public Entity[] Shelters
        {
            get
            {
                Entity[] deployableObjects = EntityCache.GetInstance().GetEntities().Where(s => s.IsDeployableObject() 
                    && UID == s.UOwnerID && s.Name.ToLower().Contains("shelter")).ToArray();
                return deployableObjects;
            }
        }

        /// <summary>
        /// Gets all Entities (Chests, Stashes) that the player owns.
        /// This array is a shallow copy & thread safe.
        /// </summary>
        public Entity[] Storage
        {
            get
            {
                Entity[] deployableObjects = EntityCache.GetInstance().GetEntities().Where(s => s.IsStorage() && UID == s.UOwnerID).ToArray();
                return deployableObjects;
            }
        }

        /// <summary>
        /// Gets all Entities (Camp Fires) that the player owns.
        /// This array is a shallow copy & thread safe.
        /// </summary>
        public Entity[] Fires
        {
            get
            {
                Entity[] deployableObjects = EntityCache.GetInstance().GetEntities().Where(s => s.IsFireBarrel() && UID == s.UOwnerID).ToArray();
                return deployableObjects;
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
                MeshBatchInstance cachedhitInstance;

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
                Collider[] x = Physics.OverlapSphere(Location, 3.5f);
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
                MeshBatchInstance cachedhitInstance;
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
                MeshBatchInstance cachedhitInstance;
                if (lastPosition == Vector3.zero) return false;
                if (!Facepunch.MeshBatch.MeshBatchPhysics.Raycast(lastPosition + new Vector3(0f, -1.15f, 0f),
                    new Vector3(0f, -1f, 0f),
                    out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                {
                    return false;
                }

                if (cachedhitInstance == null)
                {
                    const string cachedsack = "Wood_Shelter(Clone)";
                    string cachedLootableObject = cachedRaycast.collider.gameObject.name;
                    if (cachedLootableObject == cachedsack)
                    {
                        return true;
                    }

                    return false;
                }

                const string cachedsack2 = "Wood_Shelter(Clone)";
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
        
        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static bool operator ==(Player b1, Player b2)
        {
            if (ReferenceEquals(b1, b2)) 
                return true;
            if (ReferenceEquals(b1, null)) 
                return false;
            if (ReferenceEquals(b2, null))
                return false;

            return b1.Equals(b2);
        }

        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns></returns>
        public static bool operator !=(Player b1, Player b2)
        {
            return !(b1 == b2);
        }

        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            Player b2 = obj as Player;
            return b2 != null && UID == b2.UID && b2._instanceId == _instanceId;
        }

        /// <summary>
        /// For easier comparism.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _instanceId;
        }
    }
}
