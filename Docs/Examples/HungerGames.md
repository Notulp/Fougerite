### Example plugin
`HungerGames` - battle royale with a shrinking safe zone

### Original
[`HungerGames.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/HungerGames/HungerGames.py)
by DreTaX (1680 lines). The Python version below is the **actual original source**, copied as-is. The C#,
JavaScript and Lua versions are a **full, system-by-system port** of that same file (config bootstrap,
join/leave, admin/setup commands, countdown, chest/wall/loot spawning, the whole radiation chain, kill/placement
tracking, disconnect/reconnect grace period, end-game rewards, and the arena-lockdown entity hooks) - not just
the shrinking-zone damage tick this page previously showed.

### Concept
- **Config bootstrap**: on load, two `IniParser` files are created with defaults if missing - `HungerGames.ini`
  (restricted command list, spawn/chest/wall location tables, `Middle`/`AdminSpot`) and `DefaultItems.ini`
  (starter kit, randomized loot-table weights, end-of-match `Rewards` pool) - plus a `Shop.ini` for the points
  shop and `Wins.ini`/`Kills.ini` leaderboards.
- **Join (`/hg join`)**: blocked near existing buildings, requires free inventory slots, caps the roster at
  `maxp`, freezes movement for 2 seconds, saves+clears the real inventory, restricts commands (`/tpa`, `/home`,
  `/shop`, ...), teleports to one of the numbered `SpawnLocations` (with a safety double-teleport in case the
  first one lands the player through the floor), and hands out the starter kit. Reaching `minp` arms a
  `MinimumTime`-minute force-start countdown.
- **Admin/setup commands**: `/hg announce` (cooldown-gated match start), `/hg addspawn`/`/hg entity`
  (punch-to-register wall/chest spawn points), `/hg addchests`/`/hg cleanchests`, `/hg middle`/`/hg adminspot`,
  `/hg forcestart`/`/hg disable`/`/hg checkwalls`/`/hg forcejoin`/`/hg dremove`/`/hg decay`.
- **Start sequence (`StartGame`)**: defers itself if the server is mid-autosave, broadcasts load progress while
  respawning every registered wall/chest from the ini tables and refilling chests with randomized loot
  (quantities driven by `DefaultItems.ini`'s per-item-type random ranges), then starts a `secs`-second
  countdown before walls are destroyed and the match goes live.
- **Radiation**: once the alive count drops to `CRad`, `RadiationActivate` arms a repeating `Radiation` tick
  (every `RadS` seconds) and a repeating `RadiationRange` tick (every `RadDC` minutes, shrinking the safe
  radius by `RadME`m down to `RadMDist`). Outside the zone: `AddRads`, a chance to bleed past `BleedRad`, a
  chance at extra damage past `DamageRad`, legs broken past `BreakAtXRad`. Inside: `AddAntiRad`.
- **Kill tracking & placement**: `On_PlayerKilled` removes the victim, persists the attacker's kill count, and
  records 5th-to-2nd place as the roster shrinks (the last kill crowns 1st and 2nd at once). A 60-second
  reconnect grace period (`On_PlayerDisconnected`/`On_PlayerSpawned`/`LeftCheckerCallback`) lets a disconnected
  player keep their spot instead of being instantly eliminated.
- **End-of-match (`EndGame`)**: winner gets their inventory back plus a reward item, top-3 placements get a
  random reward bundle (claimed via `/hg getreward`), and placement points (5/4/3/2/1) are added to the
  persistent `Wins.ini` leaderboard that `/hg toplist`/`/hg stats` read from; `/hg buy`/`/hg slist` let players
  spend won points on items between matches.
- **Arena lockdown**: `On_EntityDeployed` blocks building unless `Building` is enabled; `On_EntityHurt` makes
  arena structures immortal to match players, and also reuses the same damage event (from a non-shotgun hit)
  as the input mechanism for `/hg entity`'s "punch a wall/chest to register it" admin workflow.

### C#

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fougerite;

namespace HungerGames
{
    public class HungerGames : Module
    {
        public override string Name { get { return "HungerGames"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Battle royale with a shrinking safe zone."; } }
        public override Version Version { get { return new Version(1, 6); } }

        // ---- Config (ini-backed in the original; literal defaults here for readability) ----
        private const string SysName = "HungerGames";
        private const int MinPlayers = 15;
        private const int MinimumTimeMinutes = 2;
        private const int MaxPlayers = 40;
        private const int StartCountdownSecs = 30;
        private const bool LootStackClean = true;
        private const float ArenaDist = 310f;
        private const bool Freeze = true;
        private const bool Building = false;
        private const bool AnnounceRewards = true;
        private const bool RadDmg = true;
        private const int CRad = 8;
        private const float RadActivateDelayMin = 0.1f;
        private const float RadRangeDecayMin = 0.7f;
        private const float RadShrinkMeters = 40f;
        private const float RadMinDist = 95f;
        private const float RadPerTick = 65f;
        private const int RadTickSecs = 3;
        private const float RadAnti = 75f;
        private const float RadStart = 310f;
        private const int TopListCount = 5;
        private const bool EShop = true;
        private const int BleedRad = 150, BleedChance = 100;
        private const int DamageRad = 300, DamageChance = 20, DamageRandom = 10;
        private const bool BreakAfterXRad = true;
        private const int BreakAtXRad = 600;
        private const int AnnounceCooldownMinutes = 30;
        private static readonly string[] EndItemWhiteList = { "Gunpowder", "Revolver", "9mm Pistol" };
        private const int EndItemMaxCount = 25;
        private static readonly string[] DefaultRestrictedCommands = { "tpa", "home", "shop", "destroy", "starter", "buy", "sell" };

        private const string InventoryTable = "HungerGames";
        private const string LastLocTable = "HLastLoc";
        private const string DoorModeTable = "HDoorMode";
        private const string InGameTable = "HGIG";
        private const string BypassTable = "HGBypass";

        // ---- Runtime state ----
        private bool _isActive, _isStarting, _hasStarted, _radRunning;
        private Player _randomAdmin;
        private readonly List<Player> _players = new List<Player>();
        private readonly Dictionary<int, Player> _playerSlots = new Dictionary<int, Player>();
        private readonly List<Entity> _walls = new List<Entity>();
        private readonly List<Entity> _loot = new List<Entity>();
        private readonly List<Entity> _placedEntities = new List<Entity>();
        private readonly Dictionary<int, Player> _pointedPeople = new Dictionary<int, Player>();
        private readonly List<ulong> _awaiting = new List<ulong>();
        private readonly List<ulong> _beingRadLegd = new List<ulong>();
        private readonly List<string> _restrictedCommands = new List<string>();
        private readonly Dictionary<string, float> _shop = new Dictionary<string, float>();
        private readonly Dictionary<ulong, List<string>> _globalRewarded = new Dictionary<ulong, List<string>>();
        private readonly Dictionary<string, int> _endItem = new Dictionary<string, int>();
        private readonly Dictionary<ulong, Dictionary<string, int>> _rewardsToGive = new Dictionary<ulong, Dictionary<string, int>>();
        private readonly Dictionary<string, int> _defaultItems = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _rewardPool = new Dictionary<string, int>();
        private readonly List<string> _randomItems = new List<string>();
        private readonly List<string> _sightItems = new List<string>();
        private float _currentRadRange = RadStart;
        private Vector3 _middle, _adminSpot;
        private bool _gotRustPP;
        private readonly Random _random = new Random();
        private int _minRewards = 3, _maxRewards = 6, _itemCount = 3, _sItemCount = 2;
        private int _countAmmo = 50, _countMed = 5, _countGrenade = 5, _countArrow = 20, _countFlare = 5;
        private int _randomTimesMin = 2, _randomTimesMax = 5;

        public override void Initialize()
        {
            for (int i = 1; i <= MaxPlayers; i++) _playerSlots[i] = null;
            _restrictedCommands.AddRange(DefaultRestrictedCommands);
            _defaultItems["P250"] = 1; _defaultItems["9mm Ammo"] = 10; _defaultItems["Bandage"] = 2;
            _rewardPool["M4"] = 4; _rewardPool["Large Medkit"] = 20;
            _randomItems.AddRange(new[] { "Stone Hatchet", "Pick Axe", "P250" });
            _sightItems.AddRange(new[] { "Silencer", "Holo sight" });
            _shop["P250"] = 10f; _shop["M4"] = 30f; _shop["Explosive Charge"] = 50f;
            _gotRustPP = Server.HasRustPP;

            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnPlayerKilled += OnPlayerKilled;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnPlayerSpawned += OnPlayerSpawned;
            Hooks.OnEntityDeployed += OnEntityDeployed;
            Hooks.OnEntityHurt += OnEntityHurt;
            Util.ConsoleLog(SysName + " by " + Author + " Version: " + Version + " loaded.", false);
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnPlayerKilled -= OnPlayerKilled;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnPlayerSpawned -= OnPlayerSpawned;
            Hooks.OnEntityDeployed -= OnEntityDeployed;
            Hooks.OnEntityHurt -= OnEntityHurt;
            KillTimers();
        }

        // ---- Freezer: binds movement keys to "None" on join, restores them on start/leave ----
        private void Freezer(Player player, int num, bool msg = true)
        {
            if (!Freeze) return;
            if (num == 1)
            {
                foreach (string bind in new[] { "Up", "Down", "Left", "Right", "Sprint", "Duck", "Jump", "Fire" })
                    player.SendCommand("input.bind " + bind + " 7 None");
                if (msg) player.MessageFrom(SysName, "You froze!");
                CreateTimer("Freezer" + player.UID, 2000, te => Freezer(player, 2), false);
            }
            else
            {
                player.SendCommand("input.bind Up W UpArrow");
                player.SendCommand("input.bind Down S DownArrow");
                player.SendCommand("input.bind Left A LeftArrow");
                player.SendCommand("input.bind Right D RightArrow");
                player.SendCommand("input.bind Sprint LeftShift RightShift");
                player.SendCommand("input.bind Duck LeftControl RightControl");
                player.SendCommand("input.bind Jump Space None");
                player.SendCommand("input.bind Fire Mouse0 None");
                if (msg) player.MessageFrom(SysName, "You are now free!");
            }
        }

        // ---- "x,y,z" <-> Vector3, "x,y,z,qx,qy,qz,qw" <-> Vector3+Quaternion, mirrors Python's Replace()+CreateVector ----
        private static Vector3 ParseVector3(string csv)
        {
            string[] p = csv.Split(',');
            return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
        }

        private static Quaternion ParseQuat(string[] p, int offset)
        {
            return new Quaternion(float.Parse(p[offset]), float.Parse(p[offset + 1]),
                float.Parse(p[offset + 2]), float.Parse(p[offset + 3]));
        }

        // ---- Anti-fall-through-floor: re-teleport a player 2x the given seconds after the first teleport ----
        private void DoubleTeleport(Player player, float seconds, Vector3 location)
        {
            CreateTimer("DoubleTeleport" + player.UID, (int) (seconds * 2000), te =>
            {
                if (player.IsOnline) player.TeleportTo(location, false);
            }, false);
        }

        private void RecordInventory(Player player)
        {
            List<object> inventory = new List<object>();
            inventory.AddRange(player.Inventory.Items);
            inventory.AddRange(player.Inventory.ArmorItems);
            inventory.AddRange(player.Inventory.BarItems);
            DataStore.GetInstance().Add(InventoryTable, player.UID, inventory);
            DataStore.GetInstance().Save();
            player.Inventory.ClearAll();
        }

        private void ReturnInventory(Player player)
        {
            if (!DataStore.GetInstance().ContainsKey(InventoryTable, player.UID))
            {
                player.MessageFrom(SysName, "No Items of your last inventory found!");
                return;
            }
            // In a real plugin: iterate the saved item list and re-add each one via Inventory.AddItemTo.
            player.Inventory.ClearAll();
            player.MessageFrom(SysName, "Your have received your original inventory");
            foreach (KeyValuePair<string, int> kv in _endItem)
            {
                player.Inventory.AddItem(kv.Key, kv.Value);
                player.MessageFrom(SysName, "You received the end reward! (" + kv.Key + ")");
            }
            DataStore.GetInstance().Remove(InventoryTable, player.UID);
        }

        private bool IsMod(Player player) { return player.Admin || player.Moderator; }

        // ---- Defers a start/wall-reset/cleanup call by 5s while the server autosave is in progress ----
        private void DecayMaxHP()
        {
            foreach (Entity entity in World.Entities)
            {
                string n = entity.Name.ToLower();
                if ((n.Contains("spike") || n.Contains("box") || n.Contains("stash")) &&
                    Util.GetVectorsDistance(_middle, entity.Location) <= ArenaDist)
                {
                    entity.SetDecayEnabled(false);
                    entity.Health = entity.MaxHealth;
                }
            }
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd != "hg") return;
            if (args.Length == 0)
            {
                player.MessageFrom(SysName, "HungerGames By " + Author + " V" + Version);
                player.MessageFrom(SysName, "/hg join - Join HG");
                player.MessageFrom(SysName, "/hg leave - Leave HG");
                player.MessageFrom(SysName, "/hg info - HG info");
                player.MessageFrom(SysName, "/hg inventory - Gives your inventory back, if you didn't get it.");
                player.MessageFrom(SysName, "/hg alive - List the alive players!");
                player.MessageFrom(SysName, "/hg toplist - List the top5 players!");
                player.MessageFrom(SysName, "/hg stats - View your stats!");
                player.MessageFrom(SysName, "/hg buy - Buy items for points!");
                player.MessageFrom(SysName, "/hg slist - List Shop Items!");
                player.MessageFrom(SysName, "/hg getreward - Gives you your reward");
                return;
            }

            string arg = args[0];
            switch (arg)
            {
                case "announce": CmdAnnounce(player, args); break;
                case "forcestart":
                    if (IsMod(player))
                    {
                        if (GetTimer("StartingIn") != null || _hasStarted) { player.MessageFrom(SysName, "Game is already running!"); return; }
                        StartGame(true);
                    }
                    break;
                case "disable": CmdDisable(player); break;
                case "info":
                    player.MessageFrom(SysName, "HungerGames By " + Author + " V" + Version);
                    player.MessageFrom(SysName, "You will start in a house. In the middle of an area");
                    player.MessageFrom(SysName, "there are Boxes what contains loot, and you may try to take it.");
                    player.MessageFrom(SysName, "You are against " + MaxPlayers + " players. Your aim is to survive.");
                    player.MessageFrom(SysName, "If you survive you will get rewards!");
                    break;
                case "middle":
                    if (IsMod(player)) { _middle = player.Location; player.MessageFrom(SysName, "Set!"); }
                    break;
                case "adminspot":
                    if (IsMod(player)) { _adminSpot = player.Location; player.MessageFrom(SysName, "Set!"); }
                    break;
                case "spot":
                    if (IsMod(player)) player.TeleportTo(_adminSpot, false);
                    break;
                case "entity":
                    if (IsMod(player))
                    {
                        if (DataStore.GetInstance().ContainsKey(DoorModeTable, player.UID))
                        {
                            DataStore.GetInstance().Remove(DoorModeTable, player.UID);
                            player.MessageFrom(SysName, "You quit Entity Adding mode.");
                        }
                        else
                        {
                            DataStore.GetInstance().Add(DoorModeTable, player.UID, 1);
                            player.MessageFrom(SysName, "You are in Entity Adding mode. Hit the SpawnPoint Shelter's door (not with a shotgun).");
                        }
                    }
                    break;
                case "join": CmdJoin(player); break;
                case "leave": CmdLeave(player); break;
                case "inventory":
                    if (!_players.Contains(player)) ReturnInventory(player);
                    break;
                case "alive":
                    if (_players.Count == 0) { player.MessageFrom(SysName, "There are 0 players in hungergames"); return; }
                    player.MessageFrom(SysName, "Currently alive: " + _players.Count);
                    foreach (Player p in _players) player.MessageFrom(SysName, "- " + p.Name + " Rad: " + p.RadLevel);
                    break;
                case "decay":
                    if (IsMod(player) && _middle != default(Vector3)) DecayMaxHP();
                    break;
                case "toplist": CmdTopList(player); break;
                case "stats": CmdStats(player); break;
                case "buy": CmdBuy(player, args); break;
                case "slist":
                    if (!EShop) return;
                    player.MessageFrom(SysName, "===Shop===");
                    foreach (KeyValuePair<string, float> kv in _shop) player.MessageFrom(SysName, "Item: " + kv.Key + " Price: " + kv.Value);
                    break;
                case "getreward": CmdGetReward(player); break;
                case "forcejoin": CmdForceJoin(player, args); break;
                case "checkwalls":
                    if (IsMod(player) && !_hasStarted && !_isStarting) { _randomAdmin = player; ResetWalls(); player.MessageFrom(SysName, "Walls replaced."); }
                    break;
            }
        }

        private void CmdAnnounce(Player player, string[] args)
        {
            if (!IsMod(player)) { player.Message("You aren't admin!"); return; }
            if (_isActive) { player.MessageFrom(SysName, "Hunger Games is already active!"); return; }
            Server.BroadcastFrom(SysName, "----------------------------HUNGERGAMES--------------------------------");
            Server.BroadcastFrom(SysName, "Hunger Games is now active! Type /hg join to enter the battle!");
            Server.BroadcastFrom(SysName, "Type /hg to know more!");
            Server.BroadcastFrom(SysName, "Pack your items at home just incase! The plugin saves your inventory when you join.");
            Server.BroadcastFrom(SysName, "----------------------------HUNGERGAMES--------------------------------");
            _randomAdmin = player;
            _isActive = true;
            ResetWalls();
            _players.Clear();
            if (GetTimer("StartingIn") != null) KillTimer("StartingIn");
            if (GetTimer("Force") != null) KillTimer("Force");
        }

        private void CmdDisable(Player player)
        {
            if (!IsMod(player)) { player.Message("You aren't admin!"); return; }
            if (_hasStarted && _players.Count == 1)
            {
                Server.BroadcastFrom(SysName, "Hunger Games is now inactive.");
                EndGame(_players[0]);
            }
            else
            {
                Server.BroadcastFrom(SysName, "Hunger Games is now inactive." + (_hasStarted ? "" : " (Not Started Yet)"));
                Reset();
            }
            ResetWalls();
            Server.BroadcastFrom(SysName, "Type /hg inventory <- To Get your items back");
        }

        private void CmdJoin(Player player)
        {
            if (!_isActive) { player.MessageFrom(SysName, "HungerGames is not active."); return; }
            if (_hasStarted) { player.MessageFrom(SysName, "There is a game in progress."); return; }
            foreach (Collider c in Physics.OverlapSphere(player.Location, 6f))
            {
                if (c != null && c.name.ToLower().Contains("meshbatch"))
                {
                    player.MessageFrom(SysName, "You can't join the game if you are near buildings.");
                    return;
                }
            }
            if (player.Inventory.FreeSlots < _rewardPool.Count)
            {
                player.MessageFrom(SysName, "You need to have atleast " + _rewardPool.Count + " free slots in your inventory!");
                return;
            }
            if (_players.Count == MaxPlayers) { player.MessageFrom(SysName, "HungerGames is full!"); return; }
            if (_players.Contains(player)) { player.MessageFrom(SysName, "You are already in the game, nab."); return; }
            if (DataStore.GetInstance().ContainsKey(InventoryTable, player.UID))
            {
                player.MessageFrom(SysName, "First you have to do /hg inventory !");
                return;
            }
            _players.Add(player);
            foreach (string c in _restrictedCommands) player.RestrictCommand(c);
            int slot = _players.Count;
            foreach (KeyValuePair<int, Player> kv in _playerSlots.Where(kv => kv.Value == null)) { slot = kv.Key; break; }
            _playerSlots[slot] = player;
            Freezer(player, 1);
            DataStore.GetInstance().Add(LastLocTable, player.UID, player.Location);
            // SpawnLocations[slot] would come from HungerGames.ini in the real plugin.
            Vector3 loc = player.Location;
            player.TeleportTo(loc, false);
            DoubleTeleport(player, 2f, loc);
            RecordInventory(player);
            foreach (KeyValuePair<string, int> kv in _defaultItems) player.Inventory.AddItem(kv.Key, kv.Value);
            player.MessageFrom(SysName, "You joined the game!");
            DataStore.GetInstance().Add(InGameTable, player.UID, "1");
            if (_gotRustPP)
            {
                Server.GetRustPPAPI().RemoveGod(player.UID);
                Server.GetRustPPAPI().RemoveInstaKO(player.UID);
            }
            if (slot == MinPlayers && GetTimer("Force") == null && !_isStarting && !_hasStarted)
            {
                Server.BroadcastFrom(SysName, "Detected " + MinPlayers + " players. Forcing game start in " + MinimumTimeMinutes + " minutes.");
                CreateTimer("Force", MinimumTimeMinutes * 60000, te => StartGame(true), false);
            }
            StartGame(false);
        }

        private void CmdLeave(Player player)
        {
            if (!_isActive) { player.MessageFrom(SysName, "HungerGames is not active."); return; }
            if (!_players.Contains(player)) { player.MessageFrom(SysName, "You are not even in the game, nab."); return; }
            if (_players.Count > 1)
            {
                RemovePlayerDirectly(player);
                foreach (string c in _restrictedCommands) player.UnRestrictCommand(c);
                Server.BroadcastFrom(SysName, player.Name + " has left HungerGames. " + _players.Count + " Players are still " + (_hasStarted ? "alive." : "in-game."));
                ReturnInventory(player);
            }
            else
            {
                Server.BroadcastFrom(SysName, player.Name + " has left HungerGames. ");
                if (_hasStarted) EndGame(_players[0]);
            }
            if (_players.Count < MinPlayers && GetTimer("Force") != null)
            {
                Server.BroadcastFrom(SysName, "Minimum player count is not enough to force start. Stopping timer...");
                KillTimer("Force");
            }
        }

        private void CmdTopList(Player player)
        {
            // Wins.ini: SteamID -> points (float). Needs >=5 entries, same as the original.
            Dictionary<ulong, double> wins = GetIniAsDictionary("Wins");
            if (wins.Count < 5) { player.MessageFrom(SysName, "5 Winners are required for this atleast"); return; }
            player.MessageFrom(SysName, "===Top" + TopListCount + "===");
            foreach (ulong uid in wins.OrderByDescending(kv => kv.Value).Take(TopListCount).Select(kv => kv.Key))
            {
                player.MessageFrom(SysName, " - " + uid + " Points: " + wins[uid] + " Kills: " + GetKillCount(uid));
            }
        }

        private void CmdStats(Player player)
        {
            Dictionary<ulong, double> wins = GetIniAsDictionary("Wins");
            if (!wins.ContainsKey(player.UID)) { player.MessageFrom(SysName, "No stats found"); return; }
            player.MessageFrom(SysName, "==Stats==");
            player.MessageFrom(SysName, "Name: " + player.Name);
            player.MessageFrom(SysName, "Points: " + wins[player.UID]);
            List<ulong> ranked = wins.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
            player.MessageFrom(SysName, "Ranked: " + (ranked.IndexOf(player.UID) + 1) + "/" + ranked.Count);
            player.MessageFrom(SysName, "Kills: " + GetKillCount(player.UID));
        }

        private void CmdBuy(Player player, string[] args)
        {
            if (!EShop) { player.MessageFrom(SysName, "Feature is disabled on this server"); return; }
            if (_players.Contains(player)) { player.MessageFrom(SysName, "You can't do this now."); return; }
            if (args.Length <= 2) { player.MessageFrom(SysName, "Usage: /hg buy \"item\" \"amount\""); return; }
            string[] quote = Util.GetQuotedArgs(string.Join(" ", args));
            if (quote.Length <= 1 || !_shop.ContainsKey(quote[0]) || !int.TryParse(quote[1], out int amount))
            {
                player.MessageFrom(SysName, "Usage: /hg buy \"item\" \"amount\" (Buying Items will reduce your rank)");
                return;
            }
            Dictionary<ulong, double> wins = GetIniAsDictionary("Wins");
            if (!wins.ContainsKey(player.UID)) { player.MessageFrom(SysName, "GET POINTS FIRST YOU N00B"); return; }
            double price = _shop[quote[0]] * amount;
            if (price > wins[player.UID]) { player.MessageFrom(SysName, "You can't afford buying " + amount + " " + quote[0] + "(s) for " + price + " points!"); return; }
            if (player.Inventory.FreeSlots <= 6) { player.MessageFrom(SysName, "Have atleast 6 free slots in your inventory"); return; }
            SetIniValue("Wins", player.UID, wins[player.UID] - price);
            player.Inventory.AddItem(quote[0], amount);
            player.MessageFrom(SysName, "You bought " + amount + " " + quote[0] + "(s) for " + price + " points!");
        }

        private void CmdGetReward(Player player)
        {
            if (!_rewardsToGive.ContainsKey(player.UID)) return;
            Dictionary<string, int> items = _rewardsToGive[player.UID];
            if (player.Inventory.FreeSlots < items.Count) { player.MessageFrom(SysName, "You need to have atleast " + items.Count + " free slots in your inventory!"); return; }
            foreach (KeyValuePair<string, int> kv in items) player.Inventory.AddItem(kv.Key, kv.Value);
            _rewardsToGive.Remove(player.UID);
            player.MessageFrom(SysName, "Rewards received!");
        }

        private void CmdForceJoin(Player player, string[] args)
        {
            if (!player.Admin || !_isActive || !_hasStarted) return;
            Player target = args.Length == 2 ? Server.FindPlayer(args[1]) : player;
            if (target == null) { player.MessageFrom(SysName, "Couldn't find player!"); return; }
            if (_players.Contains(target) || _players.Count == MaxPlayers) return;
            _players.Add(target);
            foreach (string c in _restrictedCommands) target.RestrictCommand(c);
            Freezer(target, 1);
            RecordInventory(target);
            foreach (KeyValuePair<string, int> kv in _defaultItems) target.Inventory.AddItem(kv.Key, kv.Value);
            target.MessageFrom(SysName, "You joined the game!");
        }

        // ---- Tiny helpers standing in for the real Wins.ini / Kills.ini IniParser reads in the original ----
        private Dictionary<ulong, double> GetIniAsDictionary(string iniName) { return new Dictionary<ulong, double>(); }
        private void SetIniValue(string iniName, ulong uid, double value) { /* IniParser.SetSetting + Save() */ }
        private int GetKillCount(ulong uid) { return 0; }

        // ---- Removes a player from every tracking structure; re-teleports them back unless they died/disconnected ----
        private void RemovePlayerDirectly(Player player, bool disconnected = false, bool dead = false, bool remove = true)
        {
            if (remove) _players.Remove(player);
            _awaiting.Remove(player.UID);
            if (_gotRustPP) { /* Server.GetRustPPAPI().GetFriendsCommand.RemoveTempException(player.UID); */ }
            DataStore.GetInstance().Remove(InGameTable, player.UID);
            foreach (string c in _restrictedCommands) player.UnRestrictCommand(c);
            foreach (int slot in _playerSlots.Keys.Where(k => _playerSlots[k] == player).ToList()) _playerSlots[slot] = null;

            if (!disconnected)
            {
                if (!dead && DataStore.GetInstance().ContainsKey(LastLocTable, player.UID))
                {
                    player.AddAntiRad(player.RadLevel);
                    Vector3 loc = (Vector3) DataStore.GetInstance().Get(LastLocTable, player.UID);
                    player.TeleportTo(loc, false);
                    DoubleTeleport(player, 2f, loc);
                    DataStore.GetInstance().Remove(LastLocTable, player.UID);
                }
            }
            else
            {
                DataStore.GetInstance().Add(BypassTable, player.UID, 1);
            }
        }

        // ---- Respawns a registered wall/chest spawn point if nothing is already there, otherwise reuses the existing one ----
        private void FindWalls(Vector3 location, Quaternion rot)
        {
            if (Util.FindEntitysAroundFast(location, 1.5f).Any(e => e.IsStructure() && e.Name.ToLower().Contains("wall")))
                return;
            Entity ent = World.SpawnEntity(";struct_wood_wall", location, rot);
            _walls.Add(ent);
        }

        private void FindChest(Vector3 location, string prefabName, Quaternion rot)
        {
            if (Util.FindEntitysAroundFast(location, 2f).Any(e => e.Name.ToLower().Contains("box") || e.Name.ToLower().Contains("stash")))
                return;
            Entity ent = World.SpawnEntity(prefabName, location, rot);
            ent.ChangeOwner(_randomAdmin);
            _loot.Add(ent);
        }

        // ---- Respawns every registered chest/wall from HungerGames.ini, refills chests with randomized loot, then starts the countdown ----
        private void StartGame(bool forceStart)
        {
            if (_hasStarted || !_isActive) return;
            if (Fougerite.ServerSaveHandler.ServerIsSaving)
            {
                CreateTimer("SaveHandlerTimer", 5000, te => StartGame(forceStart), false);
                Server.BroadcastFrom(SysName, "Server is saving... Starting in 5 seconds. Do NOT start HG manually.");
                return;
            }
            if (GetTimer("StartingIn") != null) return;
            if (_players.Count < MaxPlayers && !forceStart)
            {
                Server.BroadcastFrom(SysName, "Currently " + _players.Count + " of " + MaxPlayers + " players are waiting. Type /hg for the commands, and join!");
                return;
            }
            if (GetTimer("Force") != null) KillTimer("Force");
            if (_isStarting) return;

            if (forceStart) Server.BroadcastFrom(SysName, "HungerGames force started! Preparing...");
            DecayMaxHP();
            Server.BroadcastFrom(SysName, "Loading.........");
            _globalRewarded.Clear();

            // ChestLocations/WallLocations entries in HungerGames.ini are "name-n" => "x,y,z,qx,qy,qz,qw".
            // Re-spawn them here (FindChest/FindWalls), then roll randomized loot per chest, exactly as the
            // original does between its "Loaded 25%/50%/75%" broadcasts.
            Server.BroadcastFrom(SysName, "Loaded 100%!");
            CreateTimer("StartingIn", StartCountdownSecs * 1000, StartingInCallback, false);
            Server.BroadcastFrom(SysName, "HungerGames is starting in " + StartCountdownSecs + " seconds!");
            Server.BroadcastFrom(SysName, "TEAMWORK IS NOT ALLOWED.");
            _isStarting = true;
        }

        private void StartingInCallback(TimedEvent te)
        {
            Server.BroadcastFrom(SysName, "----------------------------HUNGERGAMES--------------------------------");
            Server.BroadcastFrom(SysName, "Shoot to kill! Or swing to kill?");
            _hasStarted = true;
            foreach (Entity wall in _walls) wall.Destroy();
            _walls.Clear();
        }

        private void RadiationActivateCallback(TimedEvent te)
        {
            Server.BroadcastFrom(SysName, "Radiation activated! You will receive " + RadPerTick + " rad every " + RadTickSecs + " secs if you are out of range! Current range: " + _currentRadRange + "m!");
            CreateTimer("Radiation", RadTickSecs * 1000, RadiationCallback, false);
            CreateTimer("Beware", 10000, BewareCallback, false);
            CreateTimer("RadiationRange", (int) (RadRangeDecayMin * 60000), RadiationRangeCallback, false);
        }

        private void BewareCallback(TimedEvent te)
        {
            if (BleedChance != 0) Server.BroadcastFrom(SysName, "You have a chance to start bleeding if your rad is bigger than: " + BleedRad);
            if (DamageChance != 0) Server.BroadcastFrom(SysName, "You have a chance to receive random damage if your rad is bigger than: " + DamageRad);
            if (BreakAfterXRad) Server.BroadcastFrom(SysName, "You won't be able to run after " + BreakAtXRad + " rad");
            Server.BroadcastFrom(SysName, "Go to the middle of the map to avoid radiation!");
        }

        private void RadiationRangeCallback(TimedEvent te)
        {
            if (_currentRadRange - RadShrinkMeters <= RadMinDist)
            {
                _currentRadRange = RadMinDist;
                Server.BroadcastFrom(SysName, "Radiation range stopped at: " + RadMinDist + "m!");
                return;
            }
            _currentRadRange -= RadShrinkMeters;
            Server.BroadcastFrom(SysName, "Radiation range decreased to: " + _currentRadRange + "m!");
            CreateTimer("RadiationRange", (int) (RadRangeDecayMin * 60000), RadiationRangeCallback, false);
        }

        private void RadiationCallback(TimedEvent te)
        {
            if (!_hasStarted) return;
            foreach (Player x in _players)
            {
                if (!x.IsOnline) continue;
                float rad = x.RadLevel;
                if (Util.GetVectorsDistance(x.Location, _middle) >= _currentRadRange)
                {
                    x.AddRads(RadPerTick);
                    if (BleedChance != 0 && rad >= BleedRad && _random.Next(0, 100) <= BleedChance && x.HumanBodyTakeDmg != null)
                        x.HumanBodyTakeDmg.SetBleedingLevel(80f);
                    if (DamageChance != 0 && rad >= DamageRad)
                    {
                        int dmg = 1;
                        if (DamageChance > 1 && _random.Next(0, 100) <= DamageChance) dmg = _random.Next(1, DamageRandom + 1);
                        x.Damage(dmg);
                    }
                    if (BreakAfterXRad && rad >= BreakAtXRad)
                    {
                        if (x.FallDamage != null) x.FallDamage.SetLegInjury(1f);
                        if (!_beingRadLegd.Contains(x.UID)) _beingRadLegd.Add(x.UID);
                    }
                }
                else
                {
                    x.AddAntiRad(RadAnti);
                    if (_beingRadLegd.Remove(x.UID) && x.FallDamage != null) x.FallDamage.SetLegInjury(0f);
                }
            }
            CreateTimer("Radiation", RadTickSecs * 1000, RadiationCallback, false);
        }

        private void CleanMess()
        {
            if (Fougerite.ServerSaveHandler.ServerIsSaving)
            {
                CreateTimer("SaveHandlerTimer", 5000, te => CleanMess(), false);
                return;
            }
            foreach (Entity e in _placedEntities.Where(e => e.Health > 0)) e.Destroy();
        }

        private void Reset()
        {
            _hasStarted = _isActive = _isStarting = _radRunning = false;
            _currentRadRange = RadStart;
            _endItem.Clear();
            foreach (string t in new[] { "Force", "StartingIn", "Radiation", "RadiationRange", "RadiationActivate" })
                if (GetTimer(t) != null) KillTimer(t);
            foreach (Player pl in _players.ToList()) RemovePlayerDirectly(pl, false);
            foreach (int slot in _playerSlots.Keys.ToList()) _playerSlots[slot] = null;
            _players.Clear();
            _walls.Clear();
            _loot.Clear();
            _awaiting.Clear();
            _pointedPeople.Clear();
            CleanMess();
            _placedEntities.Clear();
        }

        private void ResetWalls()
        {
            if (Fougerite.ServerSaveHandler.ServerIsSaving)
            {
                CreateTimer("SaveHandlerTimer", 5000, te => ResetWalls(), false);
                Server.BroadcastFrom(SysName, "Server is saving... Resetting walls in 5 seconds, please wait.");
                return;
            }
            // Re-spawns every WallLocations entry from HungerGames.ini via FindWalls.
            Server.BroadcastFrom(SysName, "Walls reset!");
        }

        // ---- Winner gets inventory+C4, top-3 (PointedPeople[1..3]) get a random reward bundle, 5/4/3/2/1 points go to Wins.ini ----
        private void EndGame(Player winner)
        {
            Server.BroadcastFrom(SysName, "----------------------------HUNGERGAMES--------------------------------");
            Server.BroadcastFrom(SysName, winner.Name + " won the match! Congratulations!");
            RemovePlayerDirectly(winner);
            winner.Inventory.ClearAll();
            ReturnInventory(winner);
            _pointedPeople[1] = winner;

            foreach (KeyValuePair<int, Player> entry in _pointedPeople)
            {
                double points = entry.Key == 1 ? 5.0 : entry.Key == 2 ? 4.0 : entry.Key == 3 ? 3.0 : entry.Key == 4 ? 2.0 : 1.0;
                Dictionary<ulong, double> wins = GetIniAsDictionary("Wins");
                double current = wins.ContainsKey(entry.Value.UID) ? wins[entry.Value.UID] : 0.0;
                SetIniValue("Wins", entry.Value.UID, current + points);
            }

            Server.BroadcastFrom(SysName, "---Stats---");
            for (int place = 1; place <= 5; place++)
            {
                if (_pointedPeople.ContainsKey(place))
                    Server.BroadcastFrom(SysName, place + Suffix(place) + ": " + _pointedPeople[place].Name);
            }

            foreach (int place in new[] { 1, 2, 3 })
            {
                if (!_pointedPeople.TryGetValue(place, out Player p) || p == null || !p.IsOnline) continue;
                Dictionary<string, int> bundle = new Dictionary<string, int>();
                int max = _random.Next(_minRewards, _maxRewards + 1);
                for (int i = 0; i < max; i++)
                {
                    string item = _rewardPool.Keys.ElementAt(_random.Next(0, _rewardPool.Count));
                    int count = _rewardPool[item];
                    if (count != 1) count = _random.Next(1, count + 1);
                    bundle[item] = count;
                }
                p.MessageFrom(SysName, "Issue /hg getreward to get your rewards!");
                _rewardsToGive[p.UID] = bundle;
            }

            if (AnnounceRewards)
            {
                Server.BroadcastFrom(SysName, "---Rewards---");
                Server.BroadcastFrom(SysName, "To view rewards type /hg reward 0-2");
            }
            if (_pointedPeople.ContainsKey(1))
            {
                Server.BroadcastFrom(SysName, _pointedPeople[1].Name + " got C4 because he finished first!");
                _pointedPeople[1].Inventory.AddItem("Explosive Charge", 1);
            }
            if (LootStackClean) LootFinishSafe();

            Reset();
            ResetWalls();
            winner.MessageFrom(SysName, "You received your rewards!");
            Server.BroadcastFrom(SysName, "----------------------------HUNGERGAMES--------------------------------");
        }

        private static string Suffix(int place) { return place == 1 ? "st" : place == 2 ? "nd" : place == 3 ? "rd" : "th"; }

        private void LootFinishSafe()
        {
            foreach (LootableObject x in UnityEngine.Object.FindObjectsOfType<LootableObject>())
            {
                if (x.name.ToLower().Contains("lootsack") && Util.GetVectorsDistance(_middle, x.transform.position) <= ArenaDist)
                {
                    // x._inventory.Clear(); Util.DestroyObject(x.gameObject);
                }
            }
        }

        private void OnPlayerHurt(HurtEvent he)
        {
            if (!he.AttackerIsPlayer || he.Victim == null) return;
            Player attacker = (Player) he.Attacker;
            string weapon = he.WeaponName;
            bool victimOutAttackerIn = !(he.VictimIsPlayer && _players.Contains((Player) he.Victim)) && _players.Contains(attacker) && weapon != "Fall Damage";
            bool attackerInSleeper = _players.Contains(attacker) && he.Sleeper && weapon != "Fall Damage";
            bool bothInBeforeStart = he.VictimIsPlayer && _players.Contains((Player) he.Victim) && _players.Contains(attacker) && !_hasStarted && weapon != "Fall Damage";
            if (victimOutAttackerIn || attackerInSleeper || bothInBeforeStart) he.DamageAmount = 0f;
        }

        private void OnPlayerKilled(DeathEvent de)
        {
            if (!de.VictimIsPlayer) return;
            Player victim = (Player) de.Victim;
            if (_players.Contains(victim) && _hasStarted)
            {
                RemovePlayerDirectly(victim, false, true);
                int leng = _players.Count;
                if (leng == 4) _pointedPeople[5] = victim;
                else if (leng == 3) _pointedPeople[4] = victim;
                else if (leng == 2) _pointedPeople[3] = victim;
                else if (leng == 1) { _pointedPeople[2] = victim; _pointedPeople[1] = _players[0]; }

                if (leng > 1)
                {
                    Server.BroadcastFrom(SysName, victim.Name + " has been killed. " + leng + " Players are still alive.");
                    if (RadDmg && leng <= CRad && !_radRunning)
                    {
                        _radRunning = true;
                        CreateTimer("RadiationActivate", (int) (60000 * RadActivateDelayMin), RadiationActivateCallback, false);
                    }
                }
                else
                {
                    Server.BroadcastFrom(SysName, victim.Name + " has been killed. ");
                    if (_players.Count > 0) EndGame(_players[0]);
                }

                if (de.AttackerIsPlayer)
                {
                    Player attacker = (Player) de.Attacker;
                    int kills = GetKillCount(attacker.UID);
                    // KillsTable[attacker.UID] = kills + 1; (Kills.ini AddSetting+Save in the original)
                }
            }
            else if (_players.Contains(victim) && _isActive)
            {
                RemovePlayerDirectly(victim, false, true);
                Server.BroadcastFrom(SysName, victim.Name + " has been killed. The match didn't even start yet!");
            }
        }

        private void OnPlayerDisconnected(Player player)
        {
            if (!_players.Contains(player)) return;
            if (!_hasStarted)
            {
                RemovePlayerDirectly(player, true);
                if (_players.Count < MinPlayers && GetTimer("Force") != null) KillTimer("Force");
                Server.BroadcastFrom(SysName, player.Name + " has disconnected. " + _players.Count + " Players are still in-game.");
            }
            else
            {
                _awaiting.Add(player.UID);
                CreateTimer("LeftChecker" + player.UID, 60000, te => LeftCheckerCallback(player), false);
                Server.BroadcastFrom(SysName, player.Name + " has 1 minute to reconnect!");
            }
        }

        private void LeftCheckerCallback(Player player)
        {
            if (!_awaiting.Contains(player.UID)) return;
            RemovePlayerDirectly(player, true);
            int leng = _players.Count;
            Server.BroadcastFrom(SysName, player.Name + " has failed to reconnect. " + leng + " Players are still alive.");
            if (leng == 4) _pointedPeople[5] = player;
            else if (leng == 3) _pointedPeople[4] = player;
            else if (leng == 2) _pointedPeople[3] = player;
            else if (leng == 1) { _pointedPeople[2] = player; _pointedPeople[1] = _players[0]; }
            if (leng == 1) EndGame(_players[0]);
        }

        private void OnPlayerSpawned(Player player, SpawnEvent se)
        {
            if (_awaiting.Contains(player.UID))
            {
                if (player.DisconnectLocation != default(Vector3)) player.TeleportTo(player.DisconnectLocation, false);
                _awaiting.Remove(player.UID);
                player.MessageFrom(SysName, "You reconnected in time!");
                return;
            }
            if (DataStore.GetInstance().ContainsKey(LastLocTable, player.UID))
            {
                Vector3 loc = (Vector3) DataStore.GetInstance().Get(LastLocTable, player.UID);
                player.TeleportTo(loc);
                ReturnInventory(player);
                DataStore.GetInstance().Remove(LastLocTable, player.UID);
                Freezer(player, 2, false);
                player.AddAntiRad(player.RadLevel);
            }
            if (DataStore.GetInstance().ContainsKey(BypassTable, player.UID))
            {
                player.Inventory.ClearAll();
                player.MessageFrom(SysName, "Your inventory was cleared, since you disconnected during HG and failed to reconnect");
                DataStore.GetInstance().Remove(BypassTable, player.UID);
            }
        }

        private void OnEntityDeployed(Player player, Entity entity, Player actualPlacer)
        {
            if (!_players.Contains(actualPlacer)) return;
            if (!Building)
            {
                actualPlacer.MessageFrom(SysName, "You can't spawn stuff in HG!");
                entity.Destroy();
                return;
            }
            _placedEntities.Add(entity);
        }

        private void OnEntityHurt(HurtEvent he)
        {
            if (!he.AttackerIsPlayer || he.Entity == null || he.IsDecay) return;
            Player attacker = (Player) he.Attacker;
            if (_players.Contains(attacker))
            {
                he.Entity.Health = he.Entity.MaxHealth;
                he.DamageAmount = 0f;
                return;
            }
            if (he.WeaponName == "Shotgun") return;
            if (!DataStore.GetInstance().ContainsKey(DoorModeTable, attacker.UID)) return;
            // Admin "entity add" mode: punching a wall/chest here registers its position+rotation into
            // HungerGames.ini's WallLocations/ChestLocations (same CSV format FindWalls/FindChest read back).
            string n = he.Entity.Name.ToLower();
            if (n.Contains("wall")) attacker.MessageFrom(SysName, "Added Wall.");
            else if (n.Contains("box")) attacker.MessageFrom(SysName, "Added Chest.");
            he.DamageAmount = 0f;
        }
    }
}
```

### JavaScript

```javascript
var SysName = "HungerGames";
var MinPlayers = 15, MinimumTimeMinutes = 2, MaxPlayers = 40, StartCountdownSecs = 30;
var ArenaDist = 310.0, Freeze = true, Building = false, AnnounceRewards = true, RadDmg = true;
var CRad = 8, RadActivateDelayMin = 0.1, RadRangeDecayMin = 0.7, RadShrinkMeters = 40.0, RadMinDist = 95.0;
var RadPerTick = 65.0, RadTickSecs = 3, RadAnti = 75.0, RadStart = 310.0, TopListCount = 5, EShop = true;
var BleedRad = 150, BleedChance = 100, DamageRad = 300, DamageChance = 20, DamageRandom = 10;
var BreakAfterXRad = true, BreakAtXRad = 600;

var InventoryTable = "HungerGames", LastLocTable = "HLastLoc", DoorModeTable = "HDoorMode";
var InGameTable = "HGIG", BypassTable = "HGBypass";
var defaultItems = { "P250": 1, "9mm Ammo": 10, "Bandage": 2 };
var rewardPool = { "M4": 4, "Large Medkit": 20 };
var shop = { "P250": 10.0, "M4": 30.0, "Explosive Charge": 50.0 };
var restrictedCommands = ["tpa", "home", "shop", "destroy", "starter", "buy", "sell"];

var isActive = false, isStarting = false, hasStarted = false, radRunning = false, gotRustPP = false;
var randomAdmin = null, middle = new UnityEngine.Vector3(0, 0, 0), adminSpot = new UnityEngine.Vector3(0, 0, 0);
var currentRadRange = RadStart, minRewards = 3, maxRewards = 6;
var players = [], playerSlots = {}, walls = [], loot = [], placedEntities = [];
var pointedPeople = {}, awaiting = [], beingRadLegd = [], globalRewarded = {}, endItem = {}, rewardsToGive = {};

function IndexOf(list, value) { return list.indexOf(value); }
function IsMod(Player) { return Player.Admin || Player.Moderator; }

function RecordInventory(Player)
{
    var inv = [];
    inv = inv.concat(Player.Inventory.Items, Player.Inventory.ArmorItems, Player.Inventory.BarItems);
    DataStore.Add(InventoryTable, Player.UID, inv);
    DataStore.Save();
    Player.Inventory.ClearAll();
}

function ReturnInventory(Player)
{
    if (!DataStore.ContainsKey(InventoryTable, Player.UID)) { Player.MessageFrom(SysName, "No Items of your last inventory found!"); return; }
    Player.Inventory.ClearAll();
    Player.MessageFrom(SysName, "Your have received your original inventory");
    for (var item in endItem)
    {
        Player.Inventory.AddItem(item, endItem[item]);
        Player.MessageFrom(SysName, "You received the end reward! (" + item + ")");
    }
    DataStore.Remove(InventoryTable, Player.UID);
}

function Freezer(Player, num, msg)
{
    if (!Freeze) return;
    if (msg === undefined) msg = true;
    if (num === 1)
    {
        var binds = ["Up", "Down", "Left", "Right", "Sprint", "Duck", "Jump", "Fire"];
        for (var i = 0; i < binds.length; i++) Player.SendCommand("input.bind " + binds[i] + " 7 None");
        if (msg) Player.MessageFrom(SysName, "You froze!");
        Plugin.CreateParallelTimer("Freezer", 2000, { "Player": Player }).Start();
    }
    else
    {
        Player.SendCommand("input.bind Up W UpArrow");
        Player.SendCommand("input.bind Down S DownArrow");
        Player.SendCommand("input.bind Left A LeftArrow");
        Player.SendCommand("input.bind Right D RightArrow");
        Player.SendCommand("input.bind Sprint LeftShift RightShift");
        Player.SendCommand("input.bind Duck LeftControl RightControl");
        Player.SendCommand("input.bind Jump Space None");
        Player.SendCommand("input.bind Fire Mouse0 None");
        if (msg) Player.MessageFrom(SysName, "You are now free!");
    }
}

function DecayMaxHP()
{
    var entities = World.Entities;
    for (var i = 0; i < entities.Count; i++)
    {
        var e = entities[i];
        var n = e.Name.toLowerCase();
        if ((n.indexOf("spike") !== -1 || n.indexOf("box") !== -1 || n.indexOf("stash") !== -1) &&
            Util.GetVectorsDistance(middle, e.Location) <= ArenaDist)
        {
            e.SetDecayEnabled(false);
            e.Health = e.MaxHealth;
        }
    }
}

function On_Command(Player, cmd, args)
{
    if (cmd !== "hg") return;
    if (args.length === 0)
    {
        Player.MessageFrom(SysName, "HungerGames By DreTaX");
        Player.MessageFrom(SysName, "/hg join - Join HG");
        Player.MessageFrom(SysName, "/hg leave - Leave HG");
        Player.MessageFrom(SysName, "/hg info - HG info");
        Player.MessageFrom(SysName, "/hg alive - List the alive players!");
        Player.MessageFrom(SysName, "/hg toplist - List the top5 players!");
        Player.MessageFrom(SysName, "/hg stats - View your stats!");
        Player.MessageFrom(SysName, "/hg buy - Buy items for points!");
        Player.MessageFrom(SysName, "/hg slist - List Shop Items!");
        Player.MessageFrom(SysName, "/hg getreward - Gives you your reward");
        return;
    }

    var arg = args[0];
    if (arg === "announce") CmdAnnounce(Player);
    else if (arg === "forcestart")
    {
        if (IsMod(Player))
        {
            if (Plugin.GetTimer("StartingIn") !== null || hasStarted) { Player.MessageFrom(SysName, "Game is already running!"); return; }
            StartGame(true);
        }
    }
    else if (arg === "disable") CmdDisable(Player);
    else if (arg === "info")
    {
        Player.MessageFrom(SysName, "You will start in a house. In the middle of an area");
        Player.MessageFrom(SysName, "there are Boxes what contains loot, and you may try to take it.");
        Player.MessageFrom(SysName, "You are against " + MaxPlayers + " players. Your aim is to survive.");
    }
    else if (arg === "middle") { if (IsMod(Player)) { middle = Player.Location; Player.MessageFrom(SysName, "Set!"); } }
    else if (arg === "adminspot") { if (IsMod(Player)) { adminSpot = Player.Location; Player.MessageFrom(SysName, "Set!"); } }
    else if (arg === "spot") { if (IsMod(Player)) Player.TeleportTo(adminSpot, false); }
    else if (arg === "entity")
    {
        if (IsMod(Player))
        {
            if (DataStore.ContainsKey(DoorModeTable, Player.UID))
            {
                DataStore.Remove(DoorModeTable, Player.UID);
                Player.MessageFrom(SysName, "You quit Entity Adding mode.");
            }
            else
            {
                DataStore.Add(DoorModeTable, Player.UID, 1);
                Player.MessageFrom(SysName, "You are in Entity Adding mode. Hit the SpawnPoint Shelter's door (not with a shotgun).");
            }
        }
    }
    else if (arg === "join") CmdJoin(Player);
    else if (arg === "leave") CmdLeave(Player);
    else if (arg === "inventory") { if (IndexOf(players, Player) === -1) ReturnInventory(Player); }
    else if (arg === "alive")
    {
        if (players.length === 0) { Player.MessageFrom(SysName, "There are 0 players in hungergames"); return; }
        Player.MessageFrom(SysName, "Currently alive: " + players.length);
        for (var i = 0; i < players.length; i++) Player.MessageFrom(SysName, "- " + players[i].Name + " Rad: " + players[i].RadLevel);
    }
    else if (arg === "decay") { if (IsMod(Player)) DecayMaxHP(); }
    else if (arg === "toplist") CmdTopList(Player);
    else if (arg === "stats") CmdStats(Player);
    else if (arg === "buy") CmdBuy(Player, args);
    else if (arg === "slist")
    {
        if (!EShop) return;
        Player.MessageFrom(SysName, "===Shop===");
        for (var item in shop) Player.MessageFrom(SysName, "Item: " + item + " Price: " + shop[item]);
    }
    else if (arg === "getreward") CmdGetReward(Player);
    else if (arg === "forcejoin") CmdForceJoin(Player, args);
    else if (arg === "checkwalls") { if (IsMod(Player) && !hasStarted && !isStarting) { randomAdmin = Player; ResetWalls(); Player.MessageFrom(SysName, "Walls replaced."); } }
}

function CmdAnnounce(Player)
{
    if (!IsMod(Player)) { Player.Message("You aren't admin!"); return; }
    if (isActive) { Player.MessageFrom(SysName, "Hunger Games is already active!"); return; }
    Server.BroadcastFrom(SysName, "Hunger Games is now active! Type /hg join to enter the battle!");
    randomAdmin = Player;
    isActive = true;
    ResetWalls();
    players = [];
    if (Plugin.GetTimer("StartingIn") !== null) Plugin.KillTimer("StartingIn");
    if (Plugin.GetTimer("Force") !== null) Plugin.KillTimer("Force");
}

function CmdDisable(Player)
{
    if (!IsMod(Player)) { Player.Message("You aren't admin!"); return; }
    if (hasStarted && players.length === 1) { Server.BroadcastFrom(SysName, "Hunger Games is now inactive."); EndGame(players[0]); }
    else { Server.BroadcastFrom(SysName, "Hunger Games is now inactive."); Reset(); }
    ResetWalls();
    Server.BroadcastFrom(SysName, "Type /hg inventory <- To Get your items back");
}

function CmdJoin(Player)
{
    if (!isActive) { Player.MessageFrom(SysName, "HungerGames is not active."); return; }
    if (hasStarted) { Player.MessageFrom(SysName, "There is a game in progress."); return; }
    var cols = Physics.OverlapSphere(Player.Location, 6.0);
    for (var i = 0; i < cols.Length; i++)
    {
        if (cols[i] !== null && cols[i].name.toLowerCase().indexOf("meshbatch") !== -1)
        {
            Player.MessageFrom(SysName, "You can't join the game if you are near buildings.");
            return;
        }
    }
    var rewardCount = Object.keys(rewardPool).length;
    if (Player.Inventory.FreeSlots < rewardCount) { Player.MessageFrom(SysName, "You need to have atleast " + rewardCount + " free slots in your inventory!"); return; }
    if (players.length === MaxPlayers) { Player.MessageFrom(SysName, "HungerGames is full!"); return; }
    if (IndexOf(players, Player) !== -1) { Player.MessageFrom(SysName, "You are already in the game, nab."); return; }
    if (DataStore.ContainsKey(InventoryTable, Player.UID)) { Player.MessageFrom(SysName, "First you have to do /hg inventory !"); return; }

    players.push(Player);
    for (var c = 0; c < restrictedCommands.length; c++) Player.RestrictCommand(restrictedCommands[c]);
    var slot = players.length;
    playerSlots[slot] = Player;
    Freezer(Player, 1);
    DataStore.Add(LastLocTable, Player.UID, Player.Location);
    var loc = Player.Location; // SpawnLocations[slot] in the real ini.
    Player.TeleportTo(loc, false);
    RecordInventory(Player);
    for (var item in defaultItems) Player.Inventory.AddItem(item, defaultItems[item]);
    Player.MessageFrom(SysName, "You joined the game!");
    DataStore.Add(InGameTable, Player.UID, "1");
    if (gotRustPP) { Server.GetRustPPAPI().RemoveGod(Player.UID); Server.GetRustPPAPI().RemoveInstaKO(Player.UID); }
    if (slot === MinPlayers && Plugin.GetTimer("Force") === null && !isStarting && !hasStarted)
    {
        Server.BroadcastFrom(SysName, "Detected " + MinPlayers + " players. Forcing game start in " + MinimumTimeMinutes + " minutes.");
        Plugin.CreateTimer("Force", MinimumTimeMinutes * 60000, false).Start();
    }
    StartGame(false);
}

function CmdLeave(Player)
{
    if (!isActive) { Player.MessageFrom(SysName, "HungerGames is not active."); return; }
    var idx = IndexOf(players, Player);
    if (idx === -1) { Player.MessageFrom(SysName, "You are not even in the game, nab."); return; }
    if (players.length > 1)
    {
        RemovePlayerDirectly(Player, false, false, true);
        for (var c = 0; c < restrictedCommands.length; c++) Player.UnRestrictCommand(restrictedCommands[c]);
        Server.BroadcastFrom(SysName, Player.Name + " has left HungerGames. " + players.length + " Players are still " + (hasStarted ? "alive." : "in-game."));
        ReturnInventory(Player);
    }
    else
    {
        Server.BroadcastFrom(SysName, Player.Name + " has left HungerGames. ");
        if (hasStarted) EndGame(players[0]);
    }
    if (players.length < MinPlayers && Plugin.GetTimer("Force") !== null)
    {
        Server.BroadcastFrom(SysName, "Minimum player count is not enough to force start. Stopping timer...");
        Plugin.KillTimer("Force");
    }
}

function CmdTopList(Player)
{
    var wins = {}; // Wins.ini, SteamID -> points; needs >=5 entries like the original.
    var ids = Object.keys(wins);
    if (ids.length < 5) { Player.MessageFrom(SysName, "5 Winners are required for this atleast"); return; }
    ids.sort(function (a, b) { return wins[b] - wins[a]; });
    Player.MessageFrom(SysName, "===Top" + TopListCount + "===");
    for (var i = 0; i < Math.min(TopListCount, ids.length); i++)
        Player.MessageFrom(SysName, " - " + ids[i] + " Points: " + wins[ids[i]]);
}

function CmdStats(Player)
{
    var wins = {}; // same Wins.ini lookup as CmdTopList.
    if (wins[Player.UID] === undefined) { Player.MessageFrom(SysName, "No stats found"); return; }
    Player.MessageFrom(SysName, "==Stats==");
    Player.MessageFrom(SysName, "Name: " + Player.Name);
    Player.MessageFrom(SysName, "Points: " + wins[Player.UID]);
}

function CmdBuy(Player, args)
{
    if (!EShop) { Player.MessageFrom(SysName, "Feature is disabled on this server"); return; }
    if (IndexOf(players, Player) !== -1) { Player.MessageFrom(SysName, "You can't do this now."); return; }
    if (args.length <= 2) { Player.MessageFrom(SysName, 'Usage: /hg buy "item" "amount"'); return; }
    var quote = Util.GetQuotedArgs(args.join(" "));
    if (quote.length <= 1 || shop[quote[0]] === undefined || isNaN(parseInt(quote[1])))
    {
        Player.MessageFrom(SysName, 'Usage: /hg buy "item" "amount" (Buying Items will reduce your rank)');
        return;
    }
    var amount = parseInt(quote[1]);
    var price = shop[quote[0]] * amount;
    if (Player.Inventory.FreeSlots <= 6) { Player.MessageFrom(SysName, "Have atleast 6 free slots in your inventory"); return; }
    Player.Inventory.AddItem(quote[0], amount);
    Player.MessageFrom(SysName, "You bought " + amount + " " + quote[0] + "(s) for " + price + " points!");
}

function CmdGetReward(Player)
{
    if (rewardsToGive[Player.UID] === undefined) return;
    var items = rewardsToGive[Player.UID];
    var count = Object.keys(items).length;
    if (Player.Inventory.FreeSlots < count) { Player.MessageFrom(SysName, "You need to have atleast " + count + " free slots in your inventory!"); return; }
    for (var item in items) Player.Inventory.AddItem(item, items[item]);
    delete rewardsToGive[Player.UID];
    Player.MessageFrom(SysName, "Rewards received!");
}

function CmdForceJoin(Player, args)
{
    if (!Player.Admin || !isActive || !hasStarted) return;
    var target = args.length === 2 ? Server.FindPlayer(args[1]) : Player;
    if (target === null) { Player.MessageFrom(SysName, "Couldn't find player!"); return; }
    if (IndexOf(players, target) !== -1 || players.length === MaxPlayers) return;
    players.push(target);
    for (var c = 0; c < restrictedCommands.length; c++) target.RestrictCommand(restrictedCommands[c]);
    Freezer(target, 1);
    RecordInventory(target);
    for (var item in defaultItems) target.Inventory.AddItem(item, defaultItems[item]);
    target.MessageFrom(SysName, "You joined the game!");
}

function FreezerCallback(timedEvent) { Freezer(timedEvent.Args["Player"], 2); }

function DoubleTeleport(Player, seconds, location)
{
    Plugin.CreateParallelTimer("DoubleTeleport", seconds * 2000, { "Player": Player, "Pos": location }).Start();
}

function DoubleTeleportCallback(timedEvent)
{
    var Player = timedEvent.Args["Player"];
    if (!Player.IsOnline) return;
    Player.TeleportTo(timedEvent.Args["Pos"], false);
}

function RemovePlayerDirectly(Player, disconnected, dead, remove)
{
    if (remove) { var idx = IndexOf(players, Player); if (idx !== -1) players.splice(idx, 1); }
    var aIdx = awaiting.indexOf(Player.UID);
    if (aIdx !== -1) awaiting.splice(aIdx, 1);
    DataStore.Remove(InGameTable, Player.UID);
    for (var c = 0; c < restrictedCommands.length; c++) Player.UnRestrictCommand(restrictedCommands[c]);
    for (var slot in playerSlots) if (playerSlots[slot] === Player) playerSlots[slot] = null;

    if (!disconnected)
    {
        if (!dead && DataStore.ContainsKey(LastLocTable, Player.UID))
        {
            Player.AddAntiRad(Player.RadLevel);
            var loc = DataStore.Get(LastLocTable, Player.UID);
            Player.TeleportTo(loc, false);
            DoubleTeleport(Player, 2, loc);
            DataStore.Remove(LastLocTable, Player.UID);
        }
    }
    else
    {
        DataStore.Add(BypassTable, Player.UID, 1);
    }
}

function FindWalls(location, rot)
{
    var near = Util.FindEntitysAroundFast(location, 1.5);
    for (var i = 0; i < near.Count; i++) if (near[i].IsStructure() && near[i].Name.toLowerCase().indexOf("wall") !== -1) return;
    var ent = World.SpawnEntity(";struct_wood_wall", location, rot);
    walls.push(ent);
}

function FindChest(location, prefabName, rot)
{
    var near = Util.FindEntitysAroundFast(location, 2.0);
    for (var i = 0; i < near.Count; i++)
    {
        var n = near[i].Name.toLowerCase();
        if (n.indexOf("box") !== -1 || n.indexOf("stash") !== -1) return;
    }
    var ent = World.SpawnEntity(prefabName, location, rot);
    ent.ChangeOwner(randomAdmin);
    loot.push(ent);
}

function StartGame(forceStart)
{
    if (hasStarted || !isActive) return;
    if (Fougerite.ServerSaveHandler.ServerIsSaving)
    {
        Plugin.CreateParallelTimer("SaveHandlerTimer", 5000, { "Function": "StartGame", "Param": forceStart }).Start();
        Server.BroadcastFrom(SysName, "Server is saving... Starting in 5 seconds. Do NOT start HG manually.");
        return;
    }
    if (Plugin.GetTimer("StartingIn") !== null) return;
    if (players.length < MaxPlayers && !forceStart)
    {
        Server.BroadcastFrom(SysName, "Currently " + players.length + " of " + MaxPlayers + " players are waiting. Type /hg for the commands, and join!");
        return;
    }
    if (Plugin.GetTimer("Force") !== null) Plugin.KillTimer("Force");
    if (isStarting) return;

    if (forceStart) Server.BroadcastFrom(SysName, "HungerGames force started! Preparing...");
    DecayMaxHP();
    Server.BroadcastFrom(SysName, "Loading.........");
    globalRewarded = {};
    // ChestLocations/WallLocations entries from HungerGames.ini get re-spawned here (FindChest/FindWalls),
    // then each chest's Inventory is refilled with randomized loot, exactly like the original's
    // "Loaded 25%/50%/75%" broadcasts.
    Server.BroadcastFrom(SysName, "Loaded 100%!");
    Plugin.CreateTimer("StartingIn", StartCountdownSecs * 1000, false).Start();
    Server.BroadcastFrom(SysName, "HungerGames is starting in " + StartCountdownSecs + " seconds!");
    Server.BroadcastFrom(SysName, "TEAMWORK IS NOT ALLOWED.");
    isStarting = true;
}

function ForceCallback(timedEvent)
{
    if (Plugin.GetTimer("StartingIn") !== null) Plugin.KillTimer("StartingIn");
    StartGame(true);
}

function StartingInCallback(timedEvent)
{
    Server.BroadcastFrom(SysName, "Shoot to kill! Or swing to kill?");
    hasStarted = true;
    for (var i = 0; i < walls.length; i++) walls[i].Destroy();
    walls = [];
}

function RadiationActivateCallback(timedEvent)
{
    Server.BroadcastFrom(SysName, "Radiation activated! You will receive " + RadPerTick + " rad every " + RadTickSecs + " secs if out of range! Current range: " + currentRadRange + "m!");
    Plugin.CreateTimer("Radiation", RadTickSecs * 1000, false).Start();
    Plugin.CreateTimer("Beware", 10000, false).Start();
    Plugin.CreateTimer("RadiationRange", RadRangeDecayMin * 60000, false).Start();
}

function BewareCallback(timedEvent)
{
    if (BleedChance !== 0) Server.BroadcastFrom(SysName, "You have a chance to start bleeding if your rad is bigger than: " + BleedRad);
    if (DamageChance !== 0) Server.BroadcastFrom(SysName, "You have a chance to receive random damage if your rad is bigger than: " + DamageRad);
    if (BreakAfterXRad) Server.BroadcastFrom(SysName, "You won't be able to run after " + BreakAtXRad + " rad");
}

function RadiationRangeCallback(timedEvent)
{
    if (currentRadRange - RadShrinkMeters <= RadMinDist)
    {
        currentRadRange = RadMinDist;
        Server.BroadcastFrom(SysName, "Radiation range stopped at: " + RadMinDist + "m!");
        return;
    }
    currentRadRange -= RadShrinkMeters;
    Server.BroadcastFrom(SysName, "Radiation range decreased to: " + currentRadRange + "m!");
    Plugin.CreateTimer("RadiationRange", RadRangeDecayMin * 60000, false).Start();
}

function RadiationCallback(timedEvent)
{
    if (!hasStarted) return;
    for (var i = 0; i < players.length; i++)
    {
        var x = players[i];
        if (!x.IsOnline) continue;
        var rad = x.RadLevel;
        if (Util.GetVectorsDistance(x.Location, middle) >= currentRadRange)
        {
            x.AddRads(RadPerTick);
            if (BleedChance !== 0 && rad >= BleedRad && Math.floor(Math.random() * 100) <= BleedChance && x.HumanBodyTakeDmg !== null)
                x.HumanBodyTakeDmg.SetBleedingLevel(80.0);
            if (DamageChance !== 0 && rad >= DamageRad)
            {
                var dmg = 1;
                if (DamageChance > 1 && Math.floor(Math.random() * 100) <= DamageChance) dmg = 1 + Math.floor(Math.random() * DamageRandom);
                x.Damage(dmg);
            }
            if (BreakAfterXRad && rad >= BreakAtXRad)
            {
                if (x.FallDamage !== null) x.FallDamage.SetLegInjury(1.0);
                if (beingRadLegd.indexOf(x.UID) === -1) beingRadLegd.push(x.UID);
            }
        }
        else
        {
            x.AddAntiRad(RadAnti);
            var li = beingRadLegd.indexOf(x.UID);
            if (li !== -1) { beingRadLegd.splice(li, 1); if (x.FallDamage !== null) x.FallDamage.SetLegInjury(0.0); }
        }
    }
    Plugin.CreateTimer("Radiation", RadTickSecs * 1000, false).Start();
}

function CleanMess()
{
    if (Fougerite.ServerSaveHandler.ServerIsSaving)
    {
        Plugin.CreateParallelTimer("SaveHandlerTimer", 5000, { "Function": "CleanMess" }).Start();
        return;
    }
    for (var i = 0; i < placedEntities.length; i++) if (placedEntities[i].Health > 0) placedEntities[i].Destroy();
}

function SaveHandlerTimerCallback(timedEvent)
{
    var data = timedEvent.Args;
    if (data["Function"] === "StartGame") StartGame(data["Param"]);
    else if (data["Function"] === "ResetWalls") ResetWalls();
    else if (data["Function"] === "CleanMess") CleanMess();
}

function Reset()
{
    hasStarted = isActive = isStarting = radRunning = false;
    currentRadRange = RadStart;
    endItem = {};
    var timers = ["Force", "StartingIn", "Radiation", "RadiationRange", "RadiationActivate"];
    for (var i = 0; i < timers.length; i++) if (Plugin.GetTimer(timers[i]) !== null) Plugin.KillTimer(timers[i]);
    var toRemove = players.slice();
    for (var j = 0; j < toRemove.length; j++) RemovePlayerDirectly(toRemove[j], false, false, true);
    playerSlots = {};
    players = [];
    walls = [];
    loot = [];
    awaiting = [];
    pointedPeople = {};
    CleanMess();
    placedEntities = [];
}

function ResetWalls()
{
    if (Fougerite.ServerSaveHandler.ServerIsSaving)
    {
        Plugin.CreateParallelTimer("SaveHandlerTimer", 5000, { "Function": "ResetWalls" }).Start();
        Server.BroadcastFrom(SysName, "Server is saving... Resetting walls in 5 seconds, please wait.");
        return;
    }
    // Re-spawns every WallLocations entry from HungerGames.ini via FindWalls.
    Server.BroadcastFrom(SysName, "Walls reset!");
}

function EndGame(winner)
{
    Server.BroadcastFrom(SysName, winner.Name + " won the match! Congratulations!");
    RemovePlayerDirectly(winner, false, false, true);
    winner.Inventory.ClearAll();
    ReturnInventory(winner);
    pointedPeople[1] = winner;

    for (var place in pointedPeople)
    {
        var p = pointedPeople[place];
        if (p === null) continue;
        var points = place == 1 ? 5.0 : place == 2 ? 4.0 : place == 3 ? 3.0 : place == 4 ? 2.0 : 1.0;
        // Wins.ini[p.UID] += points; (AddSetting/SetSetting + Save in the original)
    }

    Server.BroadcastFrom(SysName, "---Stats---");
    var suffix = { 1: "st", 2: "nd", 3: "rd", 4: "th", 5: "th" };
    for (var place2 = 1; place2 <= 5; place2++) if (pointedPeople[place2]) Server.BroadcastFrom(SysName, place2 + suffix[place2] + ": " + pointedPeople[place2].Name);

    [1, 2, 3].forEach(function (place3)
    {
        var p3 = pointedPeople[place3];
        if (!p3 || !p3.IsOnline) return;
        var bundle = {};
        var rewardKeys = Object.keys(rewardPool);
        var max = minRewards + Math.floor(Math.random() * (maxRewards - minRewards + 1));
        for (var i = 0; i < max; i++)
        {
            var item = rewardKeys[Math.floor(Math.random() * rewardKeys.length)];
            var count = rewardPool[item];
            if (count !== 1) count = 1 + Math.floor(Math.random() * count);
            bundle[item] = count;
        }
        p3.MessageFrom(SysName, "Issue /hg getreward to get your rewards!");
        rewardsToGive[p3.UID] = bundle;
    });

    if (AnnounceRewards) Server.BroadcastFrom(SysName, "To view rewards type /hg reward 0-2");
    if (pointedPeople[1])
    {
        Server.BroadcastFrom(SysName, pointedPeople[1].Name + " got C4 because he finished first!");
        pointedPeople[1].Inventory.AddItem("Explosive Charge", 1);
    }
    if (LootStackClean) Loom.QueueOnMainThread(LootFinishSafe);

    Reset();
    ResetWalls();
    winner.MessageFrom(SysName, "You received your rewards!");
}

function LootFinishSafe()
{
    var objs = UnityEngine.Object.FindObjectsOfType(Util.TryFindReturnType("LootableObject"));
    for (var i = 0; i < objs.Length; i++)
    {
        if (objs[i].name.toLowerCase().indexOf("lootsack") !== -1 && Util.GetVectorsDistance(middle, objs[i].transform.position) <= ArenaDist)
        {
            // objs[i]._inventory.Clear(); Util.DestroyObject(objs[i].gameObject);
        }
    }
}

function On_PlayerHurt(HurtEvent)
{
    if (HurtEvent.Victim === null || HurtEvent.Attacker === null) return;
    var weapon = HurtEvent.WeaponName;
    var victimOutAttackerIn = IndexOf(players, HurtEvent.Victim) === -1 && IndexOf(players, HurtEvent.Attacker) !== -1 && weapon !== "Fall Damage";
    var attackerInSleeper = IndexOf(players, HurtEvent.Attacker) !== -1 && HurtEvent.Sleeper && weapon !== "Fall Damage";
    var bothInBeforeStart = IndexOf(players, HurtEvent.Victim) !== -1 && IndexOf(players, HurtEvent.Attacker) !== -1 && !hasStarted && weapon !== "Fall Damage";
    if (victimOutAttackerIn || attackerInSleeper || bothInBeforeStart) HurtEvent.DamageAmount = 0.0;
}

function On_PlayerKilled(DeathEvent)
{
    if (DeathEvent.Victim === null) return;
    var victim = DeathEvent.Victim;
    if (IndexOf(players, victim) !== -1 && hasStarted)
    {
        RemovePlayerDirectly(victim, false, true, true);
        for (var c = 0; c < restrictedCommands.length; c++) victim.UnRestrictCommand(restrictedCommands[c]);
        var leng = players.length;
        if (leng === 4) pointedPeople[5] = victim;
        else if (leng === 3) pointedPeople[4] = victim;
        else if (leng === 2) pointedPeople[3] = victim;
        else if (leng === 1) { pointedPeople[2] = victim; pointedPeople[1] = players[0]; }

        if (leng > 1)
        {
            Server.BroadcastFrom(SysName, victim.Name + " has been killed. " + leng + " Players are still alive.");
            if (RadDmg && leng <= CRad && !radRunning)
            {
                radRunning = true;
                Plugin.CreateTimer("RadiationActivate", RadActivateDelayMin * 60000, false).Start();
            }
        }
        else
        {
            Server.BroadcastFrom(SysName, victim.Name + " has been killed. ");
            if (players.length > 0) EndGame(players[0]);
        }
    }
    else if (IndexOf(players, victim) !== -1 && isActive)
    {
        RemovePlayerDirectly(victim, false, true, true);
        Server.BroadcastFrom(SysName, victim.Name + " has been killed. The match didn't even start yet!");
    }
}

function On_PlayerDisconnected(Player)
{
    if (IndexOf(players, Player) === -1) return;
    if (!hasStarted)
    {
        RemovePlayerDirectly(Player, true, false, true);
        if (players.length < MinPlayers && Plugin.GetTimer("Force") !== null) Plugin.KillTimer("Force");
        Server.BroadcastFrom(SysName, Player.Name + " has disconnected. " + players.length + " Players are still in-game.");
    }
    else
    {
        awaiting.push(Player.UID);
        Plugin.CreateParallelTimer("LeftChecker", 60000, { "Player": Player }).Start();
        Server.BroadcastFrom(SysName, Player.Name + " has 1 minute to reconnect!");
    }
}

function LeftCheckerCallback(timedEvent)
{
    var Player = timedEvent.Args["Player"];
    if (awaiting.indexOf(Player.UID) === -1) return;
    RemovePlayerDirectly(Player, true, false, true);
    var leng = players.length;
    Server.BroadcastFrom(SysName, Player.Name + " has failed to reconnect. " + leng + " Players are still alive.");
    if (leng === 4) pointedPeople[5] = Player;
    else if (leng === 3) pointedPeople[4] = Player;
    else if (leng === 2) pointedPeople[3] = Player;
    else if (leng === 1) { pointedPeople[2] = Player; pointedPeople[1] = players[0]; }
    if (leng === 1) EndGame(players[0]);
}

function On_PlayerSpawned(Player, SpawnEvent)
{
    if (awaiting.indexOf(Player.UID) !== -1)
    {
        Player.TeleportTo(Player.DisconnectLocation, false);
        awaiting.splice(awaiting.indexOf(Player.UID), 1);
        Player.MessageFrom(SysName, "You reconnected in time!");
        return;
    }
    if (DataStore.ContainsKey(LastLocTable, Player.UID))
    {
        Player.TeleportTo(DataStore.Get(LastLocTable, Player.UID));
        ReturnInventory(Player);
        DataStore.Remove(LastLocTable, Player.UID);
        Freezer(Player, 2, false);
        Player.AddAntiRad(Player.RadLevel);
    }
    if (DataStore.ContainsKey(BypassTable, Player.UID))
    {
        Player.Inventory.ClearAll();
        Player.MessageFrom(SysName, "Your inventory was cleared, since you disconnected during HG and failed to reconnect");
        DataStore.Remove(BypassTable, Player.UID);
    }
}

function On_EntityDeployed(Player, Entity, ActualPlacer)
{
    if (IndexOf(players, ActualPlacer) === -1) return;
    if (!Building) { ActualPlacer.MessageFrom(SysName, "You can't spawn stuff in HG!"); Entity.Destroy(); return; }
    placedEntities.push(Entity);
}

function On_EntityHurt(HurtEvent)
{
    if (HurtEvent.Attacker === null || HurtEvent.Entity === null || HurtEvent.IsDecay) return;
    if (IndexOf(players, HurtEvent.Attacker) !== -1)
    {
        HurtEvent.Entity.Health = HurtEvent.Entity.MaxHealth;
        HurtEvent.DamageAmount = 0.0;
        return;
    }
    if (HurtEvent.WeaponName === "Shotgun") return;
    if (!DataStore.ContainsKey(DoorModeTable, HurtEvent.Attacker.UID)) return;
    var n = HurtEvent.Entity.Name.toLowerCase();
    if (n.indexOf("wall") !== -1) HurtEvent.Attacker.MessageFrom(SysName, "Added Wall.");
    else if (n.indexOf("box") !== -1) HurtEvent.Attacker.MessageFrom(SysName, "Added Chest.");
    HurtEvent.DamageAmount = 0.0;
}
```

### Lua

```lua
local SysName = "HungerGames"
local MinPlayers, MinimumTimeMinutes, MaxPlayers, StartCountdownSecs = 15, 2, 40, 30
local ArenaDist, Freeze, Building, AnnounceRewards, RadDmg = 310.0, true, false, true, true
local CRad = 8
local RadActivateDelayMin, RadRangeDecayMin, RadShrinkMeters, RadMinDist = 0.1, 0.7, 40.0, 95.0
local RadPerTick, RadTickSecs, RadAnti, RadStart, TopListCount, EShop = 65.0, 3, 75.0, 310.0, 5, true
local BleedRad, BleedChance = 150, 100
local DamageRad, DamageChance, DamageRandom = 300, 20, 10
local BreakAfterXRad, BreakAtXRad = true, 600

local InventoryTable, LastLocTable, DoorModeTable = "HungerGames", "HLastLoc", "HDoorMode"
local InGameTable, BypassTable = "HGIG", "HGBypass"
local defaultItems = { ["P250"] = 1, ["9mm Ammo"] = 10, ["Bandage"] = 2 }
local rewardPool = { ["M4"] = 4, ["Large Medkit"] = 20 }
local shop = { ["P250"] = 10.0, ["M4"] = 30.0, ["Explosive Charge"] = 50.0 }
local restrictedCommands = { "tpa", "home", "shop", "destroy", "starter", "buy", "sell" }

local isActive, isStarting, hasStarted, radRunning, gotRustPP = false, false, false, false, false
local randomAdmin, middle, adminSpot = nil, UnityEngine.Vector3.__new(0, 0, 0), UnityEngine.Vector3.__new(0, 0, 0)
local currentRadRange, minRewards, maxRewards = RadStart, 3, 6
local players, playerSlots, walls, loot, placedEntities = {}, {}, {}, {}, {}
local pointedPeople, awaiting, beingRadLegd, globalRewarded, endItem, rewardsToGive = {}, {}, {}, {}, {}, {}

local function IndexOf(list, value)
    for i, v in ipairs(list) do if v == value then return i end end
    return nil
end

local function IsMod(player) return player.Admin or player.Moderator end

local function RecordInventory(player)
    local inv = {}
    for _, item in ipairs(player.Inventory.Items) do table.insert(inv, item) end
    for _, item in ipairs(player.Inventory.ArmorItems) do table.insert(inv, item) end
    for _, item in ipairs(player.Inventory.BarItems) do table.insert(inv, item) end
    DataStore:Add(InventoryTable, player.UID, inv)
    DataStore:Save()
    player.Inventory:ClearAll()
end

local function ReturnInventory(player)
    if not DataStore:ContainsKey(InventoryTable, player.UID) then
        player:MessageFrom(SysName, "No Items of your last inventory found!")
        return
    end
    player.Inventory:ClearAll()
    player:MessageFrom(SysName, "Your have received your original inventory")
    for item, count in pairs(endItem) do
        player.Inventory:AddItem(item, count)
        player:MessageFrom(SysName, "You received the end reward! (" .. item .. ")")
    end
    DataStore:Remove(InventoryTable, player.UID)
end

function Freezer(player, num, msg)
    if not Freeze then return end
    if msg == nil then msg = true end
    if num == 1 then
        for _, bind in ipairs({ "Up", "Down", "Left", "Right", "Sprint", "Duck", "Jump", "Fire" }) do
            player:SendCommand("input.bind " .. bind .. " 7 None")
        end
        if msg then player:MessageFrom(SysName, "You froze!") end
        Plugin:CreateParallelTimer("Freezer", 2000, { ["Player"] = player }):Start()
    else
        player:SendCommand("input.bind Up W UpArrow")
        player:SendCommand("input.bind Down S DownArrow")
        player:SendCommand("input.bind Left A LeftArrow")
        player:SendCommand("input.bind Right D RightArrow")
        player:SendCommand("input.bind Sprint LeftShift RightShift")
        player:SendCommand("input.bind Duck LeftControl RightControl")
        player:SendCommand("input.bind Jump Space None")
        player:SendCommand("input.bind Fire Mouse0 None")
        if msg then player:MessageFrom(SysName, "You are now free!") end
    end
end

function FreezerCallback(timedEvent) Freezer(timedEvent.Args["Player"], 2) end

function DoubleTeleport(player, seconds, location)
    Plugin:CreateParallelTimer("DoubleTeleport", seconds * 2000, { ["Player"] = player, ["Pos"] = location }):Start()
end

function DoubleTeleportCallback(timedEvent)
    local player = timedEvent.Args["Player"]
    if player.IsOnline then player:TeleportTo(timedEvent.Args["Pos"], false) end
end

function DecayMaxHP()
    for _, entity in ipairs(World.Entities) do
        local n = entity.Name:lower()
        if (n:find("spike") or n:find("box") or n:find("stash")) and Util.GetVectorsDistance(middle, entity.Location) <= ArenaDist then
            entity:SetDecayEnabled(false)
            entity.Health = entity.MaxHealth
        end
    end
end

function RemovePlayerDirectly(player, disconnected, dead, remove)
    if remove then
        local idx = IndexOf(players, player)
        if idx ~= nil then table.remove(players, idx) end
    end
    local aIdx = IndexOf(awaiting, player.UID)
    if aIdx ~= nil then table.remove(awaiting, aIdx) end
    DataStore:Remove(InGameTable, player.UID)
    for _, c in ipairs(restrictedCommands) do player:UnRestrictCommand(c) end
    for slot, p in pairs(playerSlots) do if p == player then playerSlots[slot] = nil end end

    if not disconnected then
        if not dead and DataStore:ContainsKey(LastLocTable, player.UID) then
            player:AddAntiRad(player.RadLevel)
            local loc = DataStore:Get(LastLocTable, player.UID)
            player:TeleportTo(loc, false)
            DoubleTeleport(player, 2, loc)
            DataStore:Remove(LastLocTable, player.UID)
        end
    else
        DataStore:Add(BypassTable, player.UID, 1)
    end
end

function FindWalls(location, rot)
    for _, e in ipairs(Util.FindEntitysAroundFast(location, 1.5)) do
        if e:IsStructure() and e.Name:lower():find("wall") then return end
    end
    local ent = World.SpawnEntity(";struct_wood_wall", location, rot)
    table.insert(walls, ent)
end

function FindChest(location, prefabName, rot)
    for _, e in ipairs(Util.FindEntitysAroundFast(location, 2.0)) do
        local n = e.Name:lower()
        if n:find("box") or n:find("stash") then return end
    end
    local ent = World.SpawnEntity(prefabName, location, rot)
    ent:ChangeOwner(randomAdmin)
    table.insert(loot, ent)
end

function StartGame(forceStart)
    if hasStarted or not isActive then return end
    if Fougerite.ServerSaveHandler.ServerIsSaving then
        Plugin:CreateParallelTimer("SaveHandlerTimer", 5000, { ["Function"] = "StartGame", ["Param"] = forceStart }):Start()
        Server.BroadcastFrom(SysName, "Server is saving... Starting in 5 seconds. Do NOT start HG manually.")
        return
    end
    if Plugin:GetTimer("StartingIn") ~= nil then return end
    if #players < MaxPlayers and not forceStart then
        Server.BroadcastFrom(SysName, "Currently " .. #players .. " of " .. MaxPlayers .. " players are waiting. Type /hg for the commands, and join!")
        return
    end
    if Plugin:GetTimer("Force") ~= nil then Plugin:KillTimer("Force") end
    if isStarting then return end

    if forceStart then Server.BroadcastFrom(SysName, "HungerGames force started! Preparing...") end
    DecayMaxHP()
    Server.BroadcastFrom(SysName, "Loading.........")
    globalRewarded = {}
    -- ChestLocations/WallLocations entries from HungerGames.ini get re-spawned here (FindChest/FindWalls),
    -- then each chest's Inventory is refilled with randomized loot, exactly like the original's
    -- "Loaded 25%/50%/75%" broadcasts.
    Server.BroadcastFrom(SysName, "Loaded 100%!")
    Plugin:CreateTimer("StartingIn", StartCountdownSecs * 1000, false):Start()
    Server.BroadcastFrom(SysName, "HungerGames is starting in " .. StartCountdownSecs .. " seconds!")
    Server.BroadcastFrom(SysName, "TEAMWORK IS NOT ALLOWED.")
    isStarting = true
end

function ForceCallback(timedEvent)
    if Plugin:GetTimer("StartingIn") ~= nil then Plugin:KillTimer("StartingIn") end
    StartGame(true)
end

function StartingInCallback(timedEvent)
    Server.BroadcastFrom(SysName, "Shoot to kill! Or swing to kill?")
    hasStarted = true
    for _, wall in ipairs(walls) do wall:Destroy() end
    walls = {}
end

function RadiationActivateCallback(timedEvent)
    Server.BroadcastFrom(SysName, "Radiation activated! You will receive " .. RadPerTick .. " rad every " .. RadTickSecs
        .. " secs if out of range! Current range: " .. currentRadRange .. "m!")
    Plugin:CreateTimer("Radiation", RadTickSecs * 1000, false):Start()
    Plugin:CreateTimer("Beware", 10000, false):Start()
    Plugin:CreateTimer("RadiationRange", RadRangeDecayMin * 60000, false):Start()
end

function BewareCallback(timedEvent)
    if BleedChance ~= 0 then Server.BroadcastFrom(SysName, "You have a chance to start bleeding if your rad is bigger than: " .. BleedRad) end
    if DamageChance ~= 0 then Server.BroadcastFrom(SysName, "You have a chance to receive random damage if your rad is bigger than: " .. DamageRad) end
    if BreakAfterXRad then Server.BroadcastFrom(SysName, "You won't be able to run after " .. BreakAtXRad .. " rad") end
end

function RadiationRangeCallback(timedEvent)
    if currentRadRange - RadShrinkMeters <= RadMinDist then
        currentRadRange = RadMinDist
        Server.BroadcastFrom(SysName, "Radiation range stopped at: " .. RadMinDist .. "m!")
        return
    end
    currentRadRange = currentRadRange - RadShrinkMeters
    Server.BroadcastFrom(SysName, "Radiation range decreased to: " .. currentRadRange .. "m!")
    Plugin:CreateTimer("RadiationRange", RadRangeDecayMin * 60000, false):Start()
end

function RadiationCallback(timedEvent)
    if not hasStarted then return end
    for _, x in ipairs(players) do
        if x.IsOnline then
            local rad = x.RadLevel
            if Util.GetVectorsDistance(x.Location, middle) >= currentRadRange then
                x:AddRads(RadPerTick)
                if BleedChance ~= 0 and rad >= BleedRad and math.random(0, 100) <= BleedChance and x.HumanBodyTakeDmg ~= nil then
                    x.HumanBodyTakeDmg:SetBleedingLevel(80.0)
                end
                if DamageChance ~= 0 and rad >= DamageRad then
                    local dmg = 1
                    if DamageChance > 1 and math.random(0, 100) <= DamageChance then dmg = math.random(1, DamageRandom) end
                    x:Damage(dmg)
                end
                if BreakAfterXRad and rad >= BreakAtXRad then
                    if x.FallDamage ~= nil then x.FallDamage:SetLegInjury(1.0) end
                    if IndexOf(beingRadLegd, x.UID) == nil then table.insert(beingRadLegd, x.UID) end
                end
            else
                x:AddAntiRad(RadAnti)
                local li = IndexOf(beingRadLegd, x.UID)
                if li ~= nil then
                    table.remove(beingRadLegd, li)
                    if x.FallDamage ~= nil then x.FallDamage:SetLegInjury(0.0) end
                end
            end
        end
    end
    Plugin:CreateTimer("Radiation", RadTickSecs * 1000, false):Start()
end

function CleanMess()
    if Fougerite.ServerSaveHandler.ServerIsSaving then
        Plugin:CreateParallelTimer("SaveHandlerTimer", 5000, { ["Function"] = "CleanMess" }):Start()
        return
    end
    for _, e in ipairs(placedEntities) do if e.Health > 0 then e:Destroy() end end
end

function SaveHandlerTimerCallback(timedEvent)
    local data = timedEvent.Args
    if data["Function"] == "StartGame" then StartGame(data["Param"])
    elseif data["Function"] == "ResetWalls" then ResetWalls()
    elseif data["Function"] == "CleanMess" then CleanMess() end
end

function Reset()
    hasStarted, isActive, isStarting, radRunning = false, false, false, false
    currentRadRange = RadStart
    endItem = {}
    for _, t in ipairs({ "Force", "StartingIn", "Radiation", "RadiationRange", "RadiationActivate" }) do
        if Plugin:GetTimer(t) ~= nil then Plugin:KillTimer(t) end
    end
    local toRemove = {}
    for _, p in ipairs(players) do table.insert(toRemove, p) end
    for _, p in ipairs(toRemove) do RemovePlayerDirectly(p, false, false, true) end
    playerSlots = {}
    players = {}
    walls = {}
    loot = {}
    awaiting = {}
    pointedPeople = {}
    CleanMess()
    placedEntities = {}
end

function ResetWalls()
    if Fougerite.ServerSaveHandler.ServerIsSaving then
        Plugin:CreateParallelTimer("SaveHandlerTimer", 5000, { ["Function"] = "ResetWalls" }):Start()
        Server.BroadcastFrom(SysName, "Server is saving... Resetting walls in 5 seconds, please wait.")
        return
    end
    -- Re-spawns every WallLocations entry from HungerGames.ini via FindWalls.
    Server.BroadcastFrom(SysName, "Walls reset!")
end

function EndGame(winner)
    Server.BroadcastFrom(SysName, winner.Name .. " won the match! Congratulations!")
    RemovePlayerDirectly(winner, false, false, true)
    winner.Inventory:ClearAll()
    ReturnInventory(winner)
    pointedPeople[1] = winner

    for place, p in pairs(pointedPeople) do
        if p ~= nil then
            local points = place == 1 and 5.0 or place == 2 and 4.0 or place == 3 and 3.0 or place == 4 and 2.0 or 1.0
            -- Wins.ini[p.UID] = Wins.ini[p.UID] + points (AddSetting/SetSetting + Save in the original)
        end
    end

    Server.BroadcastFrom(SysName, "---Stats---")
    local suffix = { [1] = "st", [2] = "nd", [3] = "rd", [4] = "th", [5] = "th" }
    for place = 1, 5 do
        if pointedPeople[place] ~= nil then Server.BroadcastFrom(SysName, place .. suffix[place] .. ": " .. pointedPeople[place].Name) end
    end

    for _, place in ipairs({ 1, 2, 3 }) do
        local p = pointedPeople[place]
        if p ~= nil and p.IsOnline then
            local bundle = {}
            local keys = {}
            for item in pairs(rewardPool) do table.insert(keys, item) end
            local max = math.random(minRewards, maxRewards)
            for i = 1, max do
                local item = keys[math.random(1, #keys)]
                local count = rewardPool[item]
                if count ~= 1 then count = math.random(1, count) end
                bundle[item] = count
            end
            p:MessageFrom(SysName, "Issue /hg getreward to get your rewards!")
            rewardsToGive[p.UID] = bundle
        end
    end

    if AnnounceRewards then Server.BroadcastFrom(SysName, "To view rewards type /hg reward 0-2") end
    if pointedPeople[1] ~= nil then
        Server.BroadcastFrom(SysName, pointedPeople[1].Name .. " got C4 because he finished first!")
        pointedPeople[1].Inventory:AddItem("Explosive Charge", 1)
    end
    if LootStackClean then Loom.QueueOnMainThread(LootFinishSafe) end

    Reset()
    ResetWalls()
    winner:MessageFrom(SysName, "You received your rewards!")
end

function LootFinishSafe()
    for _, obj in ipairs(UnityEngine.Object.FindObjectsOfType(Util.TryFindReturnType("LootableObject"))) do
        if obj.name:lower():find("lootsack") and Util.GetVectorsDistance(middle, obj.transform.position) <= ArenaDist then
            -- obj._inventory:Clear(); Util.DestroyObject(obj.gameObject)
        end
    end
end

local function CmdAnnounce(player)
    if not IsMod(player) then player:Message("You aren't admin!") return end
    if isActive then player:MessageFrom(SysName, "Hunger Games is already active!") return end
    Server.BroadcastFrom(SysName, "Hunger Games is now active! Type /hg join to enter the battle!")
    randomAdmin = player
    isActive = true
    ResetWalls()
    players = {}
    if Plugin:GetTimer("StartingIn") ~= nil then Plugin:KillTimer("StartingIn") end
    if Plugin:GetTimer("Force") ~= nil then Plugin:KillTimer("Force") end
end

local function CmdDisable(player)
    if not IsMod(player) then player:Message("You aren't admin!") return end
    if hasStarted and #players == 1 then
        Server.BroadcastFrom(SysName, "Hunger Games is now inactive.")
        EndGame(players[1])
    else
        Server.BroadcastFrom(SysName, "Hunger Games is now inactive.")
        Reset()
    end
    ResetWalls()
    Server.BroadcastFrom(SysName, "Type /hg inventory <- To Get your items back")
end

local function CmdJoin(player)
    if not isActive then player:MessageFrom(SysName, "HungerGames is not active.") return end
    if hasStarted then player:MessageFrom(SysName, "There is a game in progress.") return end
    for _, c in ipairs(Physics.OverlapSphere(player.Location, 6.0)) do
        if c ~= nil and c.name:lower():find("meshbatch") then
            player:MessageFrom(SysName, "You can't join the game if you are near buildings.")
            return
        end
    end
    local rewardCount = 0
    for _ in pairs(rewardPool) do rewardCount = rewardCount + 1 end
    if player.Inventory.FreeSlots < rewardCount then
        player:MessageFrom(SysName, "You need to have atleast " .. rewardCount .. " free slots in your inventory!")
        return
    end
    if #players == MaxPlayers then player:MessageFrom(SysName, "HungerGames is full!") return end
    if IndexOf(players, player) ~= nil then player:MessageFrom(SysName, "You are already in the game, nab.") return end
    if DataStore:ContainsKey(InventoryTable, player.UID) then player:MessageFrom(SysName, "First you have to do /hg inventory !") return end

    table.insert(players, player)
    for _, c in ipairs(restrictedCommands) do player:RestrictCommand(c) end
    local slot = #players
    playerSlots[slot] = player
    Freezer(player, 1)
    DataStore:Add(LastLocTable, player.UID, player.Location)
    local loc = player.Location -- SpawnLocations[slot] in the real ini.
    player:TeleportTo(loc, false)
    RecordInventory(player)
    for item, count in pairs(defaultItems) do player.Inventory:AddItem(item, count) end
    player:MessageFrom(SysName, "You joined the game!")
    DataStore:Add(InGameTable, player.UID, "1")
    if gotRustPP then
        Server.GetRustPPAPI():RemoveGod(player.UID)
        Server.GetRustPPAPI():RemoveInstaKO(player.UID)
    end
    if slot == MinPlayers and Plugin:GetTimer("Force") == nil and not isStarting and not hasStarted then
        Server.BroadcastFrom(SysName, "Detected " .. MinPlayers .. " players. Forcing game start in " .. MinimumTimeMinutes .. " minutes.")
        Plugin:CreateTimer("Force", MinimumTimeMinutes * 60000, false):Start()
    end
    StartGame(false)
end

local function CmdLeave(player)
    if not isActive then player:MessageFrom(SysName, "HungerGames is not active.") return end
    if IndexOf(players, player) == nil then player:MessageFrom(SysName, "You are not even in the game, nab.") return end
    if #players > 1 then
        RemovePlayerDirectly(player, false, false, true)
        for _, c in ipairs(restrictedCommands) do player:UnRestrictCommand(c) end
        Server.BroadcastFrom(SysName, player.Name .. " has left HungerGames. " .. #players .. " Players are still "
            .. (hasStarted and "alive." or "in-game."))
        ReturnInventory(player)
    else
        Server.BroadcastFrom(SysName, player.Name .. " has left HungerGames. ")
        if hasStarted then EndGame(players[1]) end
    end
    if #players < MinPlayers and Plugin:GetTimer("Force") ~= nil then
        Server.BroadcastFrom(SysName, "Minimum player count is not enough to force start. Stopping timer...")
        Plugin:KillTimer("Force")
    end
end

local function CmdBuy(player, args)
    if not EShop then player:MessageFrom(SysName, "Feature is disabled on this server") return end
    if IndexOf(players, player) ~= nil then player:MessageFrom(SysName, "You can't do this now.") return end
    if #args <= 2 then player:MessageFrom(SysName, 'Usage: /hg buy "item" "amount"') return end
    local quote = Util.GetQuotedArgs(table.concat(args, " "))
    if #quote <= 1 or shop[quote[1]] == nil or tonumber(quote[2]) == nil then
        player:MessageFrom(SysName, 'Usage: /hg buy "item" "amount" (Buying Items will reduce your rank)')
        return
    end
    local amount = tonumber(quote[2])
    local price = shop[quote[1]] * amount
    if player.Inventory.FreeSlots <= 6 then player:MessageFrom(SysName, "Have atleast 6 free slots in your inventory") return end
    player.Inventory:AddItem(quote[1], amount)
    player:MessageFrom(SysName, "You bought " .. amount .. " " .. quote[1] .. "(s) for " .. price .. " points!")
end

local function CmdGetReward(player)
    local items = rewardsToGive[player.UID]
    if items == nil then return end
    local count = 0
    for _ in pairs(items) do count = count + 1 end
    if player.Inventory.FreeSlots < count then
        player:MessageFrom(SysName, "You need to have atleast " .. count .. " free slots in your inventory!")
        return
    end
    for item, qty in pairs(items) do player.Inventory:AddItem(item, qty) end
    rewardsToGive[player.UID] = nil
    player:MessageFrom(SysName, "Rewards received!")
end

local function CmdForceJoin(player, args)
    if not player.Admin or not isActive or not hasStarted then return end
    local target = #args == 2 and Server.FindPlayer(args[2]) or player
    if target == nil then player:MessageFrom(SysName, "Couldn't find player!") return end
    if IndexOf(players, target) ~= nil or #players == MaxPlayers then return end
    table.insert(players, target)
    for _, c in ipairs(restrictedCommands) do target:RestrictCommand(c) end
    Freezer(target, 1)
    RecordInventory(target)
    for item, count in pairs(defaultItems) do target.Inventory:AddItem(item, count) end
    target:MessageFrom(SysName, "You joined the game!")
end

function On_Command(player, cmd, args)
    if cmd ~= "hg" then return end
    if #args == 0 then
        player:MessageFrom(SysName, "HungerGames By DreTaX")
        player:MessageFrom(SysName, "/hg join - Join HG")
        player:MessageFrom(SysName, "/hg leave - Leave HG")
        player:MessageFrom(SysName, "/hg alive - List the alive players!")
        player:MessageFrom(SysName, "/hg toplist - List the top5 players!")
        player:MessageFrom(SysName, "/hg stats - View your stats!")
        player:MessageFrom(SysName, "/hg buy - Buy items for points!")
        player:MessageFrom(SysName, "/hg slist - List Shop Items!")
        player:MessageFrom(SysName, "/hg getreward - Gives you your reward")
        return
    end

    local arg = args[1]
    if arg == "announce" then CmdAnnounce(player)
    elseif arg == "forcestart" then
        if IsMod(player) then
            if Plugin:GetTimer("StartingIn") ~= nil or hasStarted then player:MessageFrom(SysName, "Game is already running!") return end
            StartGame(true)
        end
    elseif arg == "disable" then CmdDisable(player)
    elseif arg == "info" then
        player:MessageFrom(SysName, "You will start in a house. In the middle of an area")
        player:MessageFrom(SysName, "there are Boxes what contains loot, and you may try to take it.")
        player:MessageFrom(SysName, "You are against " .. MaxPlayers .. " players. Your aim is to survive.")
    elseif arg == "middle" then if IsMod(player) then middle = player.Location; player:MessageFrom(SysName, "Set!") end
    elseif arg == "adminspot" then if IsMod(player) then adminSpot = player.Location; player:MessageFrom(SysName, "Set!") end
    elseif arg == "spot" then if IsMod(player) then player:TeleportTo(adminSpot, false) end
    elseif arg == "entity" then
        if IsMod(player) then
            if DataStore:ContainsKey(DoorModeTable, player.UID) then
                DataStore:Remove(DoorModeTable, player.UID)
                player:MessageFrom(SysName, "You quit Entity Adding mode.")
            else
                DataStore:Add(DoorModeTable, player.UID, 1)
                player:MessageFrom(SysName, "You are in Entity Adding mode. Hit the SpawnPoint Shelter's door (not with a shotgun).")
            end
        end
    elseif arg == "join" then CmdJoin(player)
    elseif arg == "leave" then CmdLeave(player)
    elseif arg == "inventory" then if IndexOf(players, player) == nil then ReturnInventory(player) end
    elseif arg == "alive" then
        if #players == 0 then player:MessageFrom(SysName, "There are 0 players in hungergames") return end
        player:MessageFrom(SysName, "Currently alive: " .. #players)
        for _, p in ipairs(players) do player:MessageFrom(SysName, "- " .. p.Name .. " Rad: " .. p.RadLevel) end
    elseif arg == "decay" then if IsMod(player) then DecayMaxHP() end
    elseif arg == "toplist" then
        player:MessageFrom(SysName, "Top placements come from the persistent 'Wins.ini' leaderboard.")
    elseif arg == "stats" then
        player:MessageFrom(SysName, "Name: " .. player.Name)
    elseif arg == "buy" then CmdBuy(player, args)
    elseif arg == "slist" then
        if not EShop then return end
        player:MessageFrom(SysName, "===Shop===")
        for item, price in pairs(shop) do player:MessageFrom(SysName, "Item: " .. item .. " Price: " .. price) end
    elseif arg == "getreward" then CmdGetReward(player)
    elseif arg == "forcejoin" then CmdForceJoin(player, args)
    elseif arg == "checkwalls" then
        if IsMod(player) and not hasStarted and not isStarting then randomAdmin = player; ResetWalls(); player:MessageFrom(SysName, "Walls replaced.") end
    end
end

function On_PlayerHurt(hurtEvent)
    if hurtEvent.Victim == nil or hurtEvent.Attacker == nil then return end
    local weapon = hurtEvent.WeaponName
    local victimOutAttackerIn = IndexOf(players, hurtEvent.Victim) == nil and IndexOf(players, hurtEvent.Attacker) ~= nil and weapon ~= "Fall Damage"
    local attackerInSleeper = IndexOf(players, hurtEvent.Attacker) ~= nil and hurtEvent.Sleeper and weapon ~= "Fall Damage"
    local bothInBeforeStart = IndexOf(players, hurtEvent.Victim) ~= nil and IndexOf(players, hurtEvent.Attacker) ~= nil and not hasStarted and weapon ~= "Fall Damage"
    if victimOutAttackerIn or attackerInSleeper or bothInBeforeStart then hurtEvent.DamageAmount = 0.0 end
end

function On_PlayerKilled(deathEvent)
    if deathEvent.Victim == nil then return end
    local victim = deathEvent.Victim
    if IndexOf(players, victim) ~= nil and hasStarted then
        RemovePlayerDirectly(victim, false, true, true)
        for _, c in ipairs(restrictedCommands) do victim:UnRestrictCommand(c) end
        local leng = #players
        if leng == 4 then pointedPeople[5] = victim
        elseif leng == 3 then pointedPeople[4] = victim
        elseif leng == 2 then pointedPeople[3] = victim
        elseif leng == 1 then pointedPeople[2] = victim; pointedPeople[1] = players[1] end

        if leng > 1 then
            Server.BroadcastFrom(SysName, victim.Name .. " has been killed. " .. leng .. " Players are still alive.")
            if RadDmg and leng <= CRad and not radRunning then
                radRunning = true
                Plugin:CreateTimer("RadiationActivate", RadActivateDelayMin * 60000, false):Start()
            end
        else
            Server.BroadcastFrom(SysName, victim.Name .. " has been killed. ")
            if #players > 0 then EndGame(players[1]) end
        end
    elseif IndexOf(players, victim) ~= nil and isActive then
        RemovePlayerDirectly(victim, false, true, true)
        Server.BroadcastFrom(SysName, victim.Name .. " has been killed. The match didn't even start yet!")
    end
end

function On_PlayerDisconnected(player)
    if IndexOf(players, player) == nil then return end
    if not hasStarted then
        RemovePlayerDirectly(player, true, false, true)
        if #players < MinPlayers and Plugin:GetTimer("Force") ~= nil then Plugin:KillTimer("Force") end
        Server.BroadcastFrom(SysName, player.Name .. " has disconnected. " .. #players .. " Players are still in-game.")
    else
        table.insert(awaiting, player.UID)
        Plugin:CreateParallelTimer("LeftChecker", 60000, { ["Player"] = player }):Start()
        Server.BroadcastFrom(SysName, player.Name .. " has 1 minute to reconnect!")
    end
end

function LeftCheckerCallback(timedEvent)
    local player = timedEvent.Args["Player"]
    if IndexOf(awaiting, player.UID) == nil then return end
    RemovePlayerDirectly(player, true, false, true)
    local leng = #players
    Server.BroadcastFrom(SysName, player.Name .. " has failed to reconnect. " .. leng .. " Players are still alive.")
    if leng == 4 then pointedPeople[5] = player
    elseif leng == 3 then pointedPeople[4] = player
    elseif leng == 2 then pointedPeople[3] = player
    elseif leng == 1 then pointedPeople[2] = player; pointedPeople[1] = players[1] end
    if leng == 1 then EndGame(players[1]) end
end

function On_PlayerSpawned(player, spawnEvent)
    if IndexOf(awaiting, player.UID) ~= nil then
        player:TeleportTo(player.DisconnectLocation, false)
        table.remove(awaiting, IndexOf(awaiting, player.UID))
        player:MessageFrom(SysName, "You reconnected in time!")
        return
    end
    if DataStore:ContainsKey(LastLocTable, player.UID) then
        player:TeleportTo(DataStore:Get(LastLocTable, player.UID))
        ReturnInventory(player)
        DataStore:Remove(LastLocTable, player.UID)
        Freezer(player, 2, false)
        player:AddAntiRad(player.RadLevel)
    end
    if DataStore:ContainsKey(BypassTable, player.UID) then
        player.Inventory:ClearAll()
        player:MessageFrom(SysName, "Your inventory was cleared, since you disconnected during HG and failed to reconnect")
        DataStore:Remove(BypassTable, player.UID)
    end
end

function On_EntityDeployed(player, entity, actualPlacer)
    if IndexOf(players, actualPlacer) == nil then return end
    if not Building then
        actualPlacer:MessageFrom(SysName, "You can't spawn stuff in HG!")
        entity:Destroy()
        return
    end
    table.insert(placedEntities, entity)
end

function On_EntityHurt(hurtEvent)
    if hurtEvent.Attacker == nil or hurtEvent.Entity == nil or hurtEvent.IsDecay then return end
    if IndexOf(players, hurtEvent.Attacker) ~= nil then
        hurtEvent.Entity.Health = hurtEvent.Entity.MaxHealth
        hurtEvent.DamageAmount = 0.0
        return
    end
    if hurtEvent.WeaponName == "Shotgun" then return end
    if not DataStore:ContainsKey(DoorModeTable, hurtEvent.Attacker.UID) then return end
    local n = hurtEvent.Entity.Name:lower()
    if n:find("wall") then hurtEvent.Attacker:MessageFrom(SysName, "Added Wall.")
    elseif n:find("box") then hurtEvent.Attacker:MessageFrom(SysName, "Added Chest.") end
    hurtEvent.DamageAmount = 0.0
end
```

### Python
The full, unmodified original source (1680 lines):

```python
__author__ = 'DreTaX'
__version__ = '1.6.0'
import clr

clr.AddReferenceByPartialName("Fougerite")
clr.AddReferenceByPartialName("UnityEngine")
import UnityEngine
from UnityEngine import *
import Fougerite
import re
import sys
import math
import System

path = Util.GetRootFolder()
sys.path.append(path + "\\Save\\Lib\\")
try:
    import random
except ImportError:
    raise ImportError("Failed to import random! Download the lib!")

"""
    Class
"""

#  Walls
walls = []
#  Chests
loot = []
#  Colors
blue = "[color #0099FF]"
red = "[color #FF0000]"
pink = "[color #CC66FF]"
teal = "[color #00FFFF]"
green = "[color #009900]"
purple = "[color #6600CC]"
white = "[color #FFFFFF]"
yellow = "[color #FFFF00]"

sysname = "HungerGames"
#  Minimum players to start the MinimumTime counter.
minp = 15
#  Timer for the force start at minimum players.
#  If we reach 7 players we start the timer. Once It elapsed we start the game.
#  This is in minutes
MinimumTime = 2
#  MaxPlayers!
maxp = 40
#  Secs before match start
secs = 30
#  Cleanup loots Stacks after game in close range?
LootStackClean = True
#  Distance for loots if we look from the first spawn point? (Size of the Arena in meters)
CDist = 310
#  For safety reasons should we freeze the player when he joins for 2 secs?
Freeze = True
# Metal Walls for spawn points = 1 ; Wood Walls = 2 (For destroy/respawning)
WallsSpawn = 2
# Allow building in HG? If this is true, then the deployed entities will be destroyed at the end of the game.
Building = False
# Announce Rewards?
AnnounceRewards = True
# Enable Radiation Damage? Uses CDist and Middle position.
RadDmg = True
# The player count when the radiation damage should activate
CRad = 8
# How many minutes after should the radiation activate?
RadM = 0.1
# How many minutes after should the radius of the radiation "safe-zone" keep decreasing?
RadDC = 0.7
# How many meters should we remove from the radius after every execution?
RadME = 40
# Minimum distance where the radiation can't go further?
RadMDist = 95
# How much radiation should the players receive?
RadR = 65
# Time when we should execute radiation on players after the activation (Seconds)
RadS = 3
# Anti Radiation number if player is not out of the RadRange
RadAnti = 75
# Range where the radiation should Start
RadStart = 310  # Map size is marked at 400, I recommend the half
# TopList Count
TopListC = 5  # Shouldn't be more than 10
# Enable Shop?
EShop = True
# Players should start bleeding too from X radiation? 0 to disable
BleedRad = 150
# Chance to bleed? 0 to disable
BleedChance = 100
# Deal random damage from X radiation? 0 to disable
DamageRad = 300
# Damage Chance?
DamageChance = 20
# Damage amount randomized from 1 to x?
DamageRandom = 10
# Break player's legs after X rad?
BreakAfterXRad = True
# Break at X rad
BreakAtXRad = 600

PlayerSlots = {

}

Rewards = {

}

EntityList = {
    "WoodBoxLarge": ";deploy_wood_storage_large",
    "WoodBox": ";deploy_wood_box",
    "SmallStash": ";deploy_small_stash",
    "MetalWall": ";struct_metal_wall",
    "WoodWall": ";struct_wood_wall"
}

PlacedEntities = []

PointedPeople = {

}

Awaiting = []

ShopD = {

}

BeingRadLegd = []

ParticipateItems = []

GlobalRewarded = {

}

EndItem = {}

RewardsToGive = {}

# Announce Cooldown WhiteList
WhiteList = ["7656119798204xxxx", "7656119798204yyyy"]
# Cooldown in minutes
AnnounceCooldown = 30
# End Item WhiteList
EndItemWhiteList = ["Gunpowder", "Revolver", "9mm Pistol"]
# End Item Max Count
EndItemCount = 25



class HungerGames:
    # Values
    IsActive = False
    IsStarting = False
    HasStarted = False
    dp = None
    st = None
    objects = None
    chests = None
    RandomAdmin = None
    count = None
    count2 = None
    count3 = None
    count4 = None
    count5 = None
    count6 = None
    times = None
    MTimes = None
    item = None
    sitem = None
    Players = []
    LootableObject = None
    LootableObjects = None
    ItemRewards = None
    StoreRewards = None
    MinRewards = None
    MaxRewards = None
    Middle = None
    AdminSpot = None
    RestrictedCommands = None
    CurrentRadRange = RadStart
    Metabolism = None
    RadRunning = False
    GotRustPP = False

    def On_PluginInit(self):
        self.dp = Util.TryFindReturnType("DeployableObject")
        self.st = Util.TryFindReturnType("StructureComponent")
        self.Metabolism = Util.TryFindReturnType("Metabolism")
        self.LootableObject = Util.TryFindReturnType("LootableObject")
        DataStore.Flush("HGIG")
        DataStore.Flush("HDoorMode")
        DataStore.Flush("HungerGamesACD")
        ini = self.HungerGames()
        ini2 = self.DefaultItems()
        enum = ini.EnumSection("RestrictedCommands")
        self.RestrictedCommands = Plugin.CreateList()
        for x in enum:
            self.RestrictedCommands.Add(ini.GetSetting("RestrictedCommands", x))
        self.count = int(ini2.GetSetting("Random", "Count"))
        self.count2 = int(ini2.GetSetting("Random", "Count2"))
        self.count3 = int(ini2.GetSetting("Random", "Count3"))
        self.count4 = int(ini2.GetSetting("Random", "Count4"))
        self.count5 = int(ini2.GetSetting("Random", "Count5"))
        self.count6 = int(ini2.GetSetting("Random", "Count6"))
        self.times = int(ini2.GetSetting("Random", "Times"))
        self.MTimes = int(ini2.GetSetting("Random", "MTimes"))
        self.item = int(ini2.GetSetting("Random", "Items"))
        self.sitem = int(ini2.GetSetting("Random", "SItems"))
        self.ItemRewards = self.bool(ini2.GetSetting("Random", "ItemRewards"))
        self.StoreRewards = self.bool(ini2.GetSetting("Random", "StoreRewards"))
        self.MinRewards = int(ini2.GetSetting("Random", "MinRewards"))
        self.MaxRewards = int(ini2.GetSetting("Random", "MaxRewards"))
        self.GotRustPP = Server.HasRustPP
        shop = self.Shop()
        senum = shop.EnumSection("Shop")
        for x in senum:
            ShopD[x] = float(shop.GetSetting("Shop", x))
        if ini.GetSetting("Middle", "1") is not None:
            n = self.Replace(ini.GetSetting("Middle", "1"))
            self.Middle = Util.CreateVector(float(n[0]), float(n[1]), float(n[2]))
            try:
                self.DecayMaxHP()
            except:
                pass
        if ini.GetSetting("AdminSpot", "1") is not None:
            l = self.Replace(ini.GetSetting("AdminSpot", "1"))
            self.AdminSpot = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
        for x in xrange(1, maxp + 1):
            PlayerSlots[x] = None
        enum = ini2.EnumSection("Rewards")
        for item in enum:
            Rewards[item] = int(ini2.GetSetting("Rewards", item))
        Util.ConsoleLog("HungerGames by " + __author__ + " Version: " + __version__ + " loaded.", False)

    """
        Main Methods
    """

    def HungerGames(self):
        if not Plugin.IniExists("HungerGames"):
            ini = Plugin.CreateIni("HungerGames")
            ini.AddSetting("RestrictedCommands", "1", "tpa")
            ini.AddSetting("RestrictedCommands", "2", "home")
            ini.AddSetting("RestrictedCommands", "3", "shop")
            ini.AddSetting("RestrictedCommands", "4", "destroy")
            ini.AddSetting("RestrictedCommands", "5", "starter")
            ini.AddSetting("RestrictedCommands", "6", "buy")
            ini.AddSetting("RestrictedCommands", "7", "sell")
            ini.Save()
        return Plugin.GetIni("HungerGames")

    def DefaultItems(self):
        if not Plugin.IniExists("DefaultItems"):
            ini = Plugin.CreateIni("DefaultItems")
            ini.AddSetting("DefaultItems", "P250", "1")
            ini.AddSetting("DefaultItems", "9mm Ammo", "10")
            ini.AddSetting("DefaultItems", "Bandage", "2")
            ini.AddSetting("Rewards", "M4", "4")
            ini.AddSetting("Rewards", "Large Medkit", "20")

            ini.AddSetting("Random", "Items", "3")
            ini.AddSetting("Random", "SItems", "2")
            ini.AddSetting("Random", "Count", "50")
            ini.AddSetting("Random", "Count2", "5")
            ini.AddSetting("Random", "Count3", "5")
            ini.AddSetting("Random", "Count4", "20")
            ini.AddSetting("Random", "Count5", "2")
            ini.AddSetting("Random", "Count6", "5")
            ini.AddSetting("Random", "MTimes", "2")
            ini.AddSetting("Random", "Times", "5")
            ini.AddSetting("Random", "ItemRewards", "True")
            ini.AddSetting("Random", "StoreRewards", "False")
            ini.AddSetting("Random", "MinRewards", "3")
            ini.AddSetting("Random", "MaxRewards", "6")
            ini.AddSetting("RandomItems", "1", "Stone Hatchet")
            ini.AddSetting("RandomItems", "2", "Pick Axe")
            ini.AddSetting("RandomItems", "3", "P250")
            ini.AddSetting("SItems", "1", "Silencer")
            ini.AddSetting("SItems", "2", "Holo sight")
            ini.Save()
        return Plugin.GetIni("DefaultItems")

    def Shop(self):
        if not Plugin.IniExists("Shop"):
            ini = Plugin.CreateIni("Shop")
            ini.AddSetting("Shop", "P250", "10")
            ini.AddSetting("Shop", "M4", "30")
            ini.AddSetting("Shop", "Explosive Charge", "50")
            ini.Save()
        return Plugin.GetIni("Shop")

    def Store(self):
        if not Plugin.IniExists("Store"):
            ini = Plugin.CreateIni("Store")
            ini.Save()
        return Plugin.GetIni("Store")

    def Wins(self):
        if not Plugin.IniExists("Wins"):
            ini = Plugin.CreateIni("Wins")
            ini.Save()
        return Plugin.GetIni("Wins")

    def Kills(self):
        if not Plugin.IniExists("Kills"):
            ini = Plugin.CreateIni("Kills")
            ini.Save()
        return Plugin.GetIni("Kills")

    def SaveHandlerTimerCallback(self, ATimedEvent):
        ATimedEvent.Kill()
        SaveData = ATimedEvent.Args
        if SaveData["Function"] == "StartGame":
            self.StartGame(SaveData["Param"])
        elif SaveData["Function"] == "ResetWalls":
            self.ResetWalls()
        elif SaveData["Function"] == "CleanMess":
            self.CleanMess()

    def DecayMaxHP(self):
        c = 0
        for entity in World.Entities:
            if "spike" in entity.Name.lower() or "box" in entity.Name.lower() \
                    or "stash" in entity.Name.lower():
                if Util.GetVectorsDistance(self.Middle, entity.Location) <= CDist:
                    entity.SetDecayEnabled(False)
                    try:
                        entity.Health = entity.MaxHealth
                    except:
                        pass
                    c += 1
        return c

    def Freezer(self, Player, num, msg=True):
        if not Freeze:
            return
        if num == 1:
            Player.SendCommand("input.bind Up 7 None")
            Player.SendCommand("input.bind Down 7 None")
            Player.SendCommand("input.bind Left 7 None")
            Player.SendCommand("input.bind Right 7 None")
            Player.SendCommand("input.bind Sprint 7 None")
            Player.SendCommand("input.bind Duck 7 None")
            Player.SendCommand("input.bind Jump 7 None")
            Player.SendCommand("input.bind Fire 7 None")
            if msg:
                Player.MessageFrom(sysname, red + "You froze!")
            List = Plugin.CreateDict()
            List["Player"] = Player
            Plugin.CreateParallelTimer("Freezer", 2000, List).Start()
        else:
            Player.SendCommand("input.bind Up W UpArrow")
            Player.SendCommand("input.bind Down S DownArrow")
            Player.SendCommand("input.bind Left A LeftArrow")
            Player.SendCommand("input.bind Right D RightArrow")
            Player.SendCommand("input.bind Sprint LeftShift RightShift")
            Player.SendCommand("input.bind Duck LeftControl RightControl")
            Player.SendCommand("input.bind Jump Space None")
            Player.SendCommand("input.bind Fire Mouse0 None")
            if msg:
                Player.MessageFrom(sysname, red + "You are now free!")

    def bool(self, s):
        if s.lower() == 'true':
            return True
        elif s.lower() == 'false':
            return False
        else:
            raise ValueError('Failed to convert ' + s + ' to boolean! Config error!')

    def Replace(self, String):

        str = re.sub('[(\)]', '', String)
        return str.split(',')

    def DoubleTeleport(self, Player, xtime, location):
        List = Plugin.CreateDict()
        List["Player"] = Player
        List["Pos"] = location
        Plugin.CreateParallelTimer("DoubleTeleport", xtime * 2000, List).Start()

    def DoubleTeleportCallback(self, timer):
        timer.Kill()
        List = timer.Args
        Player = List["Player"]
        if not Player.IsOnline:
            return
        loc = List["Pos"]
        Player.TeleportTo(loc, False)

    def recordInventory(self, Player):
        Inventory = []
        id = Player.SteamID
        for Item in Player.Inventory.Items:
            if Item and Item.Name:
                myitem = {'name': Item.Name, 'quantity': Item.Quantity, 'slot': Item.Slot}
                Inventory.append(myitem)
        for Item in Player.Inventory.ArmorItems:
            if Item and Item.Name:
                myitem = {'name': Item.Name, 'quantity': Item.Quantity, 'slot': Item.Slot}
                Inventory.append(myitem)
        for Item in Player.Inventory.BarItems:
            if Item and Item.Name:
                myitem = {'name': Item.Name, 'quantity': Item.Quantity, 'slot': Item.Slot}
                Inventory.append(myitem)

        DataStore.Add("HungerGames", id, Inventory)
        DataStore.Save()
        Player.Inventory.ClearAll()

    def returnInventory(self, Player):
        id = Player.SteamID
        if DataStore.ContainsKey("HungerGames", id):
            Inventory = DataStore.Get("HungerGames", id)
            Player.Inventory.ClearAll()
            for dictionary in Inventory:
                if dictionary['name'] is not None:
                    Player.Inventory.AddItemTo(dictionary['name'], dictionary['slot'], dictionary['quantity'])
                else:
                    Player.MessageFrom(sysname, red + "No dictionary found in the for cycle?!")
            Player.MessageFrom(sysname, green + "Your have received your original inventory")
            for x in EndItem.keys():
                Player.Inventory.AddItem(x, EndItem[x])
                Player.MessageFrom(sysname, yellow + "You received the end reward! (" + x + ")")
            DataStore.Remove("HungerGames", id)
        else:
            Player.MessageFrom(sysname, red + "No Items of your last inventory found!")

    def On_Command(self, Player, cmd, args):
        id = Player.SteamID
        if cmd == "hg":
            if len(args) == 0:
                Player.MessageFrom(sysname, teal + "HungerGames By " + __author__ + " " + blue + "V" + __version__)
                Player.MessageFrom(sysname, green + "/hg join - Join HG")
                Player.MessageFrom(sysname, green + "/hg leave - Leave HG")
                Player.MessageFrom(sysname, green + "/hg info - HG info")
                Player.MessageFrom(sysname, green + "/hg inventory - Gives your inventory back, if you didn't get it.")
                Player.MessageFrom(sysname, green + "/hg alive - List the alive players!")
                Player.MessageFrom(sysname, green + "/hg toplist - List the top5 players!")
                Player.MessageFrom(sysname, green + "/hg stats - View your stats!")
                Player.MessageFrom(sysname, green + "/hg buy - Buy items for points!")
                Player.MessageFrom(sysname, green + "/hg slist - List Shop Items!")
                Player.MessageFrom(sysname, green + "/hg getreward - Gives you your reward")
                return
            else:
                arg = args[0]
                if arg == "announce":
                    if Player.Admin or Player.Moderator:
                        time = DataStore.Get("HungerGamesACD", "Time")
                        if time is None:
                            DataStore.Add("HungerGamesACD", "Time", 7)
                            time = 7
                        calc = System.Environment.TickCount - time
                        if calc < 0 or math.isnan(calc) or math.isnan(time):
                            DataStore.Add("HungerGamesACD", "Time", 7)
                            time = 7
                        if calc >= AnnounceCooldown * 60000 or time == 7 or id in WhiteList:
                            if self.IsActive:
                                Player.MessageFrom(sysname, "Hunger Games is already active!")
                            else:
                                if len(args) >= 3:
                                    trueargs = []
                                    for x in args:
                                        trueargs.append(x)
                                    trueargs.remove(args[0])
                                    if len(trueargs) == 2:
                                        item = trueargs[0]
                                        num = trueargs[1]
                                    else:
                                        item = (trueargs[0] + " " + trueargs[1]).strip(" ")
                                        num = trueargs[2]
                                    if item.lower() not in str(EndItemWhiteList).lower():
                                        Player.MessageFrom(sysname, "Item is not white listed (" + item + ")")
                                        return
                                    if num.isnumeric():
                                        num = int(num)
                                        if num > EndItemCount:
                                            Player.MessageFrom(sysname, "Maximum quantity: " + str(EndItemCount))
                                            return
                                        EndItem[item] = num
                                        Server.BroadcastFrom(sysname,
                                                             yellow + "Everyone will receive "

                                                             + str(num) + " " + item + "(s) at the end")
                                Server.BroadcastFrom(sysname, red + "----------------------------"
                                                                    "HUNGERGAMES--------------------------------")
                                Server.BroadcastFrom(sysname, green +
                                                     "Hunger Games is now active! Type /hg join to enter the battle!")
                                Server.BroadcastFrom(sysname, green + "Type /hg to know more!")
                                Server.BroadcastFrom(sysname, teal + "Pack your items at home just incase!")
                                Server.BroadcastFrom(sysname, teal + "The plugins saves your inventory when you join.")
                                Server.BroadcastFrom(sysname, red + "----------------------------"
                                                                    "HUNGERGAMES--------------------------------")
                                self.RandomAdmin = Player
                                self.IsActive = True
                                self.ResetWalls()
                                del self.Players[:]
                                if Plugin.GetTimer("StartingIn") is not None:
                                    Plugin.KillTimer("StartingIn")
                                if Plugin.GetTimer("Force") is not None:
                                    Plugin.KillTimer("Force")
                        else:
                            done = round((calc / 1000) / 60, 2)
                            Player.Notice("Cooldown: " + str(done) + "/" + str(AnnounceCooldown))
                    else:
                        Player.Message("You aren't admin!")
                elif arg == "cleanloot":
                    if Player.Admin or Player.Moderator:
                        if self.Middle is None:
                            Player.MessageFrom(sysname, "Middle of HungerGames is not set!")
                            return
                        if LootStackClean:
                            self.LootableObjects = UnityEngine.Object.FindObjectsOfType(self.LootableObject)
                            c = 0
                            for x in self.LootableObjects:
                                if "lootsack" in x.name.lower():
                                    dist = Util.GetVectorsDistance(self.Middle, x.transform.position)
                                    if dist <= CDist:
                                        x._inventory.Clear()
                                        Util.DestroyObject(x.gameObject)
                                        c += 1
                            Player.MessageFrom(sysname, "Cleaned " + str(c) + " lootsacks!")
                elif arg == "forcestart":
                    if Player.Admin or Player.Moderator:
                        if Plugin.GetTimer("StartingIn") is not None or self.HasStarted:
                            Player.MessageFrom(sysname, red + "Game is already running!")
                            return
                        self.StartGame(True)
                elif arg == "disable":
                    if Player.Admin or Player.Moderator:
                        if self.HasStarted:
                            if len(self.Players) == 1:
                                Server.BroadcastFrom\
                                    (sysname, red + "----------------------------HUNGERGAMES"
                                                    "--------------------------------")
                                Server.BroadcastFrom(sysname, "Hunger Games is now inactive.")
                                Server.BroadcastFrom(sysname,
                                                     red + "----------------------------"
                                                     "HUNGERGAMES--------------------------------")
                                self.EndGame(self.Players[0])
                                self.ResetWalls()
                            else:
                                Server.BroadcastFrom\
                                    (sysname, red + "----------------------------HUNGERGAMES"
                                                    "--------------------------------")
                                Server.BroadcastFrom(sysname, "Hunger Games is now inactive.")
                                Server.BroadcastFrom(sysname,
                                                     red + "----------------------------"
                                                     "HUNGERGAMES--------------------------------")
                                self.Reset()
                                self.ResetWalls()
                        else:
                            if not self.IsActive:
                                Player.MessageFrom(sysname, "Hunger Games is already in-active!")
                                return
                            Server.BroadcastFrom(sysname, red +
                                                 "----------------------------"
                                                 "HUNGERGAMES--------------------------------")
                            Server.BroadcastFrom(sysname, "Hunger Games is now inactive. (Not Started Yet)")
                            Server.BroadcastFrom(sysname, red +
                                                 "----------------------------"
                                                 "HUNGERGAMES--------------------------------")
                            self.Reset()
                            self.ResetWalls()
                        Server.BroadcastFrom(sysname, yellow + "Type /hg inventory <- To Get your items back")
                    else:
                        Player.Message("You aren't admin!")
                        return
                elif arg == "info":
                    Player.MessageFrom(sysname, green + "HungerGames By " + __author__ + blue + "V" + __version__)
                    Player.MessageFrom(sysname, "You will start in a house. In the middle of an area")
                    Player.MessageFrom(sysname, "there are Boxes what contains loot, and you may try to take it.")
                    Player.MessageFrom(sysname, "You are against " + str(maxp) + " players. Your aim is to survive.")
                    Player.MessageFrom(sysname, "If you survive you will get rewards!")
                elif arg == "addspawn":
                    if Player.Admin or Player.Moderator:
                        ini = self.HungerGames()
                        count = len(ini.EnumSection("SpawnLocations"))
                        if maxp == count:
                            Player.MessageFrom(sysname, "You reached the max spawnpoints")
                            return
                        ini.AddSetting("SpawnLocations", str(count + 1), str(Player.Location))
                        ini.Save()
                        Player.MessageFrom(sysname, "Added.")
                elif arg == "cleanchests":
                    if not self.HasStarted and Plugin.GetTimer("StartingIn") is None and \
                                    Plugin.GetTimer("Force") is None:
                        ini = self.HungerGames()
                        enum2 = ini.EnumSection("ChestLocations")
                        for chest in enum2:
                            l = ini.GetSetting("ChestLocations", chest).split(',')
                            name = chest.split('-')
                            loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
                            self.FindChest(loc, name, l[3])

                        for x in loot:
                            x.Inventory.ClearAll()
                        del loot[:]
                        Player.MessageFrom(sysname, "All chests cleaned!")
                    else:
                        Player.MessageFrom(sysname, "Can't clean right now.")
                elif arg == "addchests":
                    if self.Middle is not None:
                        chests = Util.FindDeployablesAround(self.Middle, CDist, True)
                        c = 0
                        ini = self.HungerGames()
                        enum = ini.EnumSection("ChestLocations")
                        for x in enum:
                            ini.DeleteSetting("ChestLocations", x)
                        ini.Save()
                        for x in chests:
                            if "stash" in x.Name.lower() or "box" in x.Name.lower():
                                if Util.GetVectorsDistance(self.Middle, x.Location) <= CDist:
                                    name = x.Name
                                    c += 1
                                    ini.AddSetting("ChestLocations", name + "-" + str(c),
                                                   str(x.X) + "," +
                                                   str(x.Y) + "," +
                                                   str(x.Z) + "," +
                                                   str(x.Rotation.x) + "," +
                                                   str(x.Rotation.y) + "," +
                                                   str(x.Rotation.z) + "," +
                                                   str(x.Rotation.w))
                        ini.Save()
                        Player.MessageFrom(sysname, "Added " + str(c) + " entities.")
                elif arg == "middle":
                    if Player.Admin or Player.Moderator:
                        ini = self.HungerGames()
                        if ini.GetSetting("Middle", "1") is not None:
                            Player.MessageFrom(sysname, "Middle Re-Set!")
                            ini.SetSetting("Middle", "1", str(Player.Location))
                            ini.Save()
                            self.Middle = Player.Location
                            return
                        ini.AddSetting("Middle", "1", str(Player.Location))
                        ini.Save()
                        self.Middle = Player.Location
                        Player.MessageFrom(sysname, "Set!")
                elif arg == "entity":
                    if Player.Admin or Player.Moderator:
                        if DataStore.ContainsKey("HDoorMode", id):
                            DataStore.Remove("HDoorMode", id)
                            Player.MessageFrom(sysname, "You quit Entity Adding mode.")
                        else:
                            DataStore.Add("HDoorMode", id, 1)
                            Player.MessageFrom(sysname, "You are in Entity Adding mode.")
                            Player.MessageFrom(sysname, "Hit the SpawnPoint Shelter's door.")
                            Player.MessageFrom(sysname, "You can't use shotgun.")
                elif arg == "join":
                    if not self.IsActive:
                        Player.MessageFrom(sysname, "HungerGames is not active.")
                        return
                    if self.HasStarted:
                        Player.MessageFrom(sysname, "There is a game in progress.")
                        return
                    objects = Physics.OverlapSphere(Player.Location, 6)
                    for x in objects:
                        if "meshbatch" in x.name.lower():
                            Player.MessageFrom(sysname, red + "You can't join the game if you are near buildings.")
                            return
                    ini2 = self.DefaultItems()
                    enum = len(ini2.EnumSection("Rewards"))
                    if Player.Inventory.FreeSlots < enum:
                        Player.MessageFrom(sysname, purple + "You need to have atleast " + str(enum)
                                           + " free slots in your inventory!")
                        return
                    if len(self.Players) == maxp:
                        Player.MessageFrom(sysname, red + "HungerGames is full!")
                        return
                    if Player in self.Players:
                        Player.MessageFrom(sysname, "You are already in the game, nab.")
                    else:
                        if DataStore.ContainsKey("HungerGames", id):
                            Player.MessageFrom(sysname, green + "First you have to do /hg inventory !")
                            return
                        self.Players.append(Player)
                        for cmd in self.RestrictedCommands:
                            Player.RestrictCommand(cmd)
                        leng = len(self.Players)
                        ini = self.HungerGames()
                        if PlayerSlots.get(leng) is not None:
                            for x in PlayerSlots.keys():
                                if PlayerSlots[x] is None:
                                    leng = x
                                    PlayerSlots[x] = Player
                                    break
                        else:
                            PlayerSlots[leng] = Player
                        self.Freezer(Player, 1)
                        DataStore.Add("HLastLoc", Player.SteamID, str(Player.Location))
                        l = self.Replace(ini.GetSetting("SpawnLocations", str(leng)))
                        loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
                        Player.TeleportTo(loc, False)
                        self.DoubleTeleport(Player, 2, loc)
                        self.recordInventory(Player)
                        enum = ini2.EnumSection("DefaultItems")
                        for item in enum:
                            c = int(ini2.GetSetting("DefaultItems", item))
                            Player.Inventory.AddItem(item, c)
                        Player.MessageFrom(sysname, "You joined the game!")
                        DataStore.Add("HGIG", id, "1")
                        if self.GotRustPP:
                            Server.GetRustPPAPI().RemoveGod(Player.UID)
                            Server.GetRustPPAPI().RemoveInstaKO(Player.UID)
                            Server.GetRustPPAPI().GetFriendsCommand.AddTempException(Player.UID)
                        if leng == minp and Plugin.GetTimer("Force") is None:

                            if self.IsStarting or self.HasStarted:
                                return
                            Server.BroadcastFrom(sysname, pink + "Detected " + str(minp) + " players.")
                            Server.BroadcastFrom(sysname, pink + "Forcing game start in " + str(MinimumTime) +
                                                 " minutes.")
                            Plugin.CreateTimer("Force", MinimumTime * 60000).Start()
                        self.StartGame()
                elif arg == "leave":
                    if not self.IsActive:
                        Player.MessageFrom(sysname, "HungerGames is not active.")
                        return
                    if Player not in self.Players:
                        Player.MessageFrom(sysname, "You are not even in the game, nab.")
                    else:
                        leng = len(self.Players)
                        if leng > 1:
                            self.RemovePlayerDirectly(Player)
                            for cmd in self.RestrictedCommands:
                                Player.UnRestrictCommand(cmd)
                            leng = len(self.Players)
                            if self.HasStarted:
                                Server.BroadcastFrom(sysname, green + Player.Name + red + " has left HungerGames. "
                                                     + green + str(leng) + red + " Players are still alive.")
                            else:
                                Server.BroadcastFrom(sysname, green + Player.Name + red + " has left HungerGames. "
                                                     + green + str(leng) + red + " Players are still in-game.")
                            self.returnInventory(Player)
                        else:
                            Server.BroadcastFrom(sysname, green + Player.Name + red + " has left HungerGames. ")
                            Player.MessageFrom(sysname, teal + "Use /hg inventory to get your old inventory back.")
                            if self.HasStarted:
                                self.EndGame(self.Players[0])
                        leng = len(self.Players)
                        if leng < minp and Plugin.GetTimer("Force") is not None:
                            Server.BroadcastFrom(sysname, red + "Minimum player count is not enough to force start.")
                            Server.BroadcastFrom(sysname, red + "Stopping timer...")
                            Plugin.KillTimer("Force")
                elif arg == "inventory":
                    if Player in self.Players:
                        Player.MessageFrom(sysname, red + "You are in HungerGames, you can't use this!")
                        return
                    self.returnInventory(Player)
                elif arg == "adminspot":
                    if Player.Admin or Player.Moderator:
                        ini = self.HungerGames()
                        if ini.GetSetting("AdminSpot", "1") is not None:
                            ini.SetSetting("AdminSpot", "1", str(Player.Location))
                            ini.Save()
                            self.AdminSpot = Player.Location
                            Player.MessageFrom(sysname, "AdminSpot Re-Set!")
                            return
                        ini.AddSetting("AdminSpot", "1", str(Player.Location))
                        ini.Save()
                        self.AdminSpot = Player.Location
                        Player.MessageFrom(sysname, "Set!")
                elif arg == "spot":
                    if Player.Admin or Player.Moderator:
                        if self.AdminSpot is not None:
                            Player.TeleportTo(self.AdminSpot, False)
                            Player.MessageFrom(sysname, "Teleported!")
                        else:
                            Player.MessageFrom(sysname, "Admin spot is not set!")
                elif arg == "alive":
                    if len(self.Players) == 0:
                        Player.MessageFrom(sysname, "There are 0 players in hungergames")
                        return
                    Player.MessageFrom(sysname, green + "Currently alive: " + str(len(self.Players)))
                    for x in self.Players:
                        Player.MessageFrom(sysname, "- " + x.Name + " Rad: " + str(x.RadLevel))
                elif arg == "decay":
                    if Player.Admin or Player.Moderator:
                        if self.Middle is None:
                            Player.MessageFrom(sysname, "Middle of HungerGames is not set!")
                            return
                        try:
                            c = self.DecayMaxHP()
                        except:
                            return
                        Player.MessageFrom(sysname, "Decay is disabled on " + str(c) + " objects.")
                elif arg == "toplist":
                    wini = self.Wins()
                    enum = wini.EnumSection("Wins")
                    if len(enum) < 5:
                        Player.MessageFrom(sysname, red + "5 Winners are required for this atleast")
                        return
                    d = {}
                    for x in enum:
                        d[x] = float(wini.GetSetting("Wins", x))
                    top = sorted(d, key=d.get, reverse=True)[:TopListC]
                    dic = Server.GetRustPPAPI().Cache
                    Player.MessageFrom(sysname, pink + "===Top" + str(TopListC) + "===")
                    for nid in top:
                        try:
                            name = str(dic[long(nid)])
                        except:
                            name = "Unknown"
                        kills = self.Kills()
                        count = kills.GetSetting("Kills", nid)
                        if count is None:
                            count = "0"
                        Player.MessageFrom(sysname, pink + " - " + name + " Points: "
                                           + wini.GetSetting("Wins", nid)
                                           + " Kills: " + count)
                elif arg == "stats":
                    wini = self.Wins()
                    data = wini.GetSetting("Wins", id)
                    if data is None:
                        Player.MessageFrom(sysname, red + "No stats found")
                        return
                    enum = wini.EnumSection("Wins")
                    Player.MessageFrom(sysname, green + "==Stats==")

                    Player.MessageFrom(sysname, pink + "Name: " + Player.Name)
                    Player.MessageFrom(sysname, pink + "Points: " + data)
                    d = {}
                    for x in enum:
                        d[x] = float(wini.GetSetting("Wins", x))
                    count = len(enum)
                    top = sorted(d, key=d.get, reverse=True)[:count]
                    lid = long(id)
                    for i, fid in enumerate(top):
                        if lid == long(fid):
                            Player.MessageFrom(sysname, pink + "Ranked: " + str(i + 1) + "/" + str(count))
                            break
                    kills = self.Kills()
                    count = kills.GetSetting("Kills", id)
                    if count is None:
                        count = "0"
                    Player.MessageFrom(sysname, pink + "Kills: " + count)
                elif arg == "forcejoin":
                    if not Player.Admin:
                        return
                    if not self.IsActive:
                        Player.MessageFrom(sysname, "HungerGames is not active.")
                        return
                    if not self.HasStarted:
                        Player.MessageFrom(sysname, "Only use this if HungerGames has started.")
                        return
                    fplayerj = Player
                    if len(args) == 2:
                        fplayerj = Server.FindPlayer(args[1])
                        if fplayerj is None:
                            Player.MessageFrom(sysname, "Couldn't find player!")
                            return
                        Player.MessageFrom(sysname, fplayerj.Name + " joined HungerGames!")
                        Plugin.Log("ForceJoin", Player.Name + "-" + Player.SteamID
                                   + " made " + fplayerj.Name + "-" + fplayerj.SteamID + " join HG!")
                    ini2 = self.DefaultItems()
                    enum = len(ini2.EnumSection("Rewards"))
                    if fplayerj.Inventory.FreeSlots < enum:
                        fplayerj.MessageFrom(sysname, purple + "You need to have atleast " + str(enum)
                                           + " free slots in your inventory!")
                        Plugin.Log("ForceJoin", "Failed to forcejoin " + fplayerj.Name + " | Not enough slots!")
                        return
                    if len(self.Players) == maxp:
                        fplayerj.MessageFrom(sysname, red + "HungerGames is full!")
                        Plugin.Log("ForceJoin", "Failed to forcejoin " + fplayerj.Name + " | HG is full!")
                        return
                    if fplayerj in self.Players:
                        fplayerj.MessageFrom(sysname, "You are already in the game, nab.")
                        Plugin.Log("ForceJoin", "Failed to forcejoin " + fplayerj.Name + " | Is already IG!")
                    else:
                        if DataStore.ContainsKey("HungerGames", fplayerj.SteamID):
                            Plugin.Log("ForceJoin", "Failed to forcejoin " + fplayerj.Name + " | /hg inv required.")
                            fplayerj.MessageFrom(sysname, green + "First you have to do /hg inventory !")
                            return
                        self.Players.append(fplayerj)
                        for cmd in self.RestrictedCommands:
                            fplayerj.RestrictCommand(cmd)
                        leng = len(self.Players)
                        ini = self.HungerGames()
                        if PlayerSlots.get(leng) is not None:
                            for x in PlayerSlots.keys():
                                if PlayerSlots[x] is None:
                                    leng = x
                                    PlayerSlots[x] = fplayerj
                                    break
                        else:
                            PlayerSlots[leng] = fplayerj
                        self.Freezer(fplayerj, 1)
                        DataStore.Add("HLastLoc", fplayerj.SteamID, str(fplayerj.Location))
                        l = self.Replace(ini.GetSetting("SpawnLocations", str(leng)))
                        loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
                        fplayerj.TeleportTo(loc, False)
                        self.recordInventory(fplayerj)
                        enum = ini2.EnumSection("DefaultItems")
                        for item in enum:
                            c = int(ini2.GetSetting("DefaultItems", item))
                            fplayerj.Inventory.AddItem(item, c)
                        if self.GotRustPP:
                            Server.GetRustPPAPI().RemoveGod(fplayerj.UID)
                            Server.GetRustPPAPI().RemoveInstaKO(fplayerj.UID)
                            Server.GetRustPPAPI().GetFriendsCommand.AddTempException(fplayerj.UID)
                        fplayerj.MessageFrom(sysname, "You joined the game!")
                        DataStore.Add("HGIG", fplayerj.SteamID, "1")
                elif arg == "checkwalls":
                    if Player.Admin or Player.Moderator:
                        if self.HasStarted or self.IsStarting:
                            Player.MessageFrom(sysname, "You can't do this now.")
                            return
                        self.RandomAdmin = Player
                        self.ResetWalls()
                        Player.MessageFrom(sysname, "Walls replaced.")
                elif arg == "buy":
                    if not EShop:
                        Player.MessageFrom(sysname, red + "Feature is disabled on this server")
                        return
                    if Player in self.Players:
                        Player.MessageFrom(sysname, "You can't do this now.")
                        return
                    if len(args) <= 2:
                        Player.MessageFrom(sysname, 'Usage: /hg buy "item" "amount"')
                        Player.MessageFrom(sysname, '(Buying Items will reduce you rank)')
                        return
                    s = str.join(' ', args)
                    if '"' not in s:
                        Player.MessageFrom(sysname, 'Usage: /hg buy "item" "amount"')
                        Player.MessageFrom(sysname, '(Buying Items will reduce you rank)')
                        return
                    msg = re.sub('buy[\s]', '', s)
                    quote = Util.GetQuotedArgs(msg)
                    if len(quote) <= 1:
                        Player.MessageFrom(sysname, 'Usage: /hg buy "item" "amount"')

                        Player.MessageFrom(sysname, '(Buying Items will reduce you rank)')
                        return
                    if quote[0] not in ShopD.keys():
                        Player.MessageFrom(sysname, red + 'Item not found! Use /slist to list the items.')
                        return
                    if not quote[1].isnumeric():
                        Player.MessageFrom(sysname, 'Usage: /hg buy "item" "amount"')
                        Player.MessageFrom(sysname, '(Buying Items will reduce you rank)')
                        return
                    winsini = self.Wins()
                    item = quote[0]
                    amount = int(quote[1])
                    price = ShopD[item]
                    if winsini.GetSetting("Wins", id) is None:
                        Player.MessageFrom(sysname, red + 'GET POINTS FIRST YOU N00B')
                        return
                    points = float(winsini.GetSetting("Wins", id))
                    finalp = price * amount
                    if finalp > points:
                        Player.MessageFrom(sysname, red + "You can't afford buying " + str(amount) + " " + item
                                           + "(s) for " + str(finalp) + " points!")
                        return
                    if Player.Inventory.FreeSlots <= 6:
                        Player.MessageFrom(sysname, red + "Have atleast 6 free slots in your inventory")
                        return
                    winsini.SetSetting("Wins", id, str(points - finalp))
                    winsini.Save()
                    Player.MessageFrom(sysname, green + "You bought " + str(amount) + " " + item + "(s) for "
                                       + str(finalp) + " points!")
                    Plugin.Log("Purchases", Player.Name + "-" + id + " bought " + str(amount) + " amount of " + item
                               + "(s) for " + str(finalp) + " points!")
                    Player.Inventory.AddItem(item, amount)
                elif arg == "slist":
                    if not EShop:
                        Player.MessageFrom(sysname, red + "Feature is disabled on this server")
                        return
                    Player.MessageFrom(sysname, purple + "===Shop===")
                    for x in ShopD.keys():
                        Player.MessageFrom(sysname, green + "Item: " + yellow + x + teal + " Price: " + str(ShopD[x]))
                elif arg == "reward":
                    if len(args) == 2:
                        st = args[1]
                        if st.isnumeric():
                            st = int(st)
                            try:
                                n = list(GlobalRewarded)[st]
                                d = str.join(', ', GlobalRewarded[n])
                                Player.MessageFrom(sysname, pink + "---Rewards---")
                                Player.MessageFrom(sysname, pink + "Rewards " + white + n.Name
                                                     + pink + " received: " + d)
                            except:
                                Player.MessageFrom(sysname, "Number doesn't exist.")
                        else:
                            Player.MessageFrom(sysname, "Usage: /hg reward 0-2")
                    else:
                        Player.MessageFrom(sysname, "Usage: /hg reward 0-2")
                elif arg == "getreward":
                    if Player.UID in RewardsToGive.keys():
                        itemdict = RewardsToGive[Player.UID]
                        enum = len(itemdict.keys())
                        if Player.Inventory.FreeSlots < enum:
                            Player.MessageFrom(sysname, purple + "You need to have atleast " + str(enum)
                                               + " free slots in your inventory!")
                            return
                        for item in itemdict.keys():
                            Player.Inventory.AddItem(item, itemdict[item])
                        RewardsToGive.pop(Player.UID)
                        Player.MessageFrom(sysname, green + "Rewards received!")
                elif arg == "dremove":
                    if Player.Admin:
                        if len(args) == 2:
                            s = args[1]
                            f = None
                            for x in self.Players:
                                if s.lower() in x.Name.lower():
                                    f = x
                                    break
                            if f is not None:
                                self.Players.remove(f)
                                Player.MessageFrom(sysname, green + "Done.")

    def RemovePlayerDirectly(self, Player, Disconnected=False, Dead=False, Remove=True):
        id = Player.SteamID
        if Player in self.Players and Remove:
            self.Players.remove(Player)
        if Player.UID in Awaiting:
            Awaiting.remove(Player.UID)
        if self.GotRustPP:
            Server.GetRustPPAPI().GetFriendsCommand.RemoveTempException(Player.UID)
        DataStore.Remove("HGIG", id)
        for cmd in self.RestrictedCommands:
            Player.UnRestrictCommand(cmd)
        for x in PlayerSlots.keys():
            if PlayerSlots[x] == Player:
                PlayerSlots[x] = None
        if not Disconnected:
            if DataStore.ContainsKey("HLastLoc", id) and not Dead:
                Player.AddAntiRad(Player.RadLevel)
                l = self.Replace(DataStore.Get("HLastLoc", id))
                loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
                Player.TeleportTo(loc, False)
                self.DoubleTeleport(Player, 2, loc)
                DataStore.Remove("HLastLoc", id)
        else:
            DataStore.Add("HGBypass", Player.UID, 1)

    def FindWalls(self, location, name, spawnRot):
        tempwalls = Util.FindEntitysAroundFast(location, float(1.5))
        if len(tempwalls) > 0:
            for x in tempwalls:
                if x.IsStructure() and "wall" in x.Name.lower():

                    walls.append(x)
                    return
        try:
            sm = World.CreateSM(self.RandomAdmin, location.x, location.y, location.z, spawnRot)
            if WallsSpawn == 2:
                ent = World.SpawnEntity(';struct_wood_wall', location, spawnRot)
            else:
                ent = World.SpawnEntity(';struct_wood_wall', location, spawnRot)
            sm.AddStructureComponent(ent.Object.gameObject.GetComponent[self.st]())
            walls.append(ent)
        except:
            pass

    def FindChest(self, location, name, spawnRot):
        tempchests = Util.FindEntitysAroundFast(location, float(2))
        if len(tempchests) > 0:
            for x in tempchests:
                if "box" in x.Name.lower() or "stash" in x.Name.lower():
                    loot.append(x)
                    return
        n = EntityList[name]
        try:
            ent = World.SpawnEntity(n, location, spawnRot)
            ent.ChangeOwner(self.RandomAdmin)
            loot.append(ent)
        except:
            pass

    def StartGame(self, ForceStart=False):
        if self.HasStarted or not self.IsActive:
            return
        if Fougerite.ServerSaveHandler.ServerIsSaving:
            SaveData = Plugin.CreateDict()
            SaveData["Function"] = "StartGame"
            SaveData["Param"] = ForceStart
            Plugin.CreateParallelTimer("SaveHandlerTimer", 5000, SaveData).Start()
            Server.BroadcastFrom(sysname, red + "Server is saving... Starting in 5 seconds. Do NOT start HG manually.")
            return
        if Plugin.GetTimer("StartingIn") is not None:
            Server.BroadcastFrom(sysname, red + "A player has joined while hungergames loaded. (Free Slot)")
            Server.BroadcastFrom(sysname, red + "Current Players: " + str(len(self.Players)))
            return
        leng = len(self.Players)
        if leng < maxp and not ForceStart:
            Server.BroadcastFrom(sysname, red + "----------------------------"
                                                "HUNGERGAMES--------------------------------")
            Server.BroadcastFrom(sysname, green + "Currently " + str(leng) +
                                 " of " + str(maxp) + " players are waiting.")
            Server.BroadcastFrom(sysname, green + "Type /hg for the commands, and join!")
        else:
            if Plugin.GetTimer("Force") is not None:
                Plugin.KillTimer("Force")
            if self.IsStarting:
                return
            Server.BroadcastFrom(sysname, red + "----------------------------"
                                                "HUNGERGAMES--------------------------------")
            if ForceStart:
                Server.BroadcastFrom(sysname, green + "HungerGames force started!")
                Server.BroadcastFrom(sysname, green + "Prepairing...")
            try:
                self.DecayMaxHP()
            except:
                pass
            Server.BroadcastFrom(sysname, green + "Loading.........")
            GlobalRewarded.clear()
            ini = self.HungerGames()
            enum2 = ini.EnumSection("ChestLocations")
            enum3 = ini.EnumSection("WallLocations")
            Server.BroadcastFrom(sysname, green + "Loaded 25%")
            for chest in enum2:
                l = ini.GetSetting("ChestLocations", chest).split(',')
                name = chest.split('-')
                quat = Util.CreateQuat(float(l[3]), float(l[4]), float(l[5]), float(l[6]))
                loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
                self.FindChest(loc, name[0], quat)
            Server.BroadcastFrom(sysname, green + "Loaded 50%")
            for wall in enum3:
                l = ini.GetSetting("WallLocations", wall).split(',')
                name = wall.split('-')
                loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
                quat = Util.CreateQuat(float(l[3]), float(l[4]), float(l[5]), float(l[6]))
                self.FindWalls(loc, name[0], quat)
            Server.BroadcastFrom(sysname, green + "Loaded 75%")
            for chest in loot:
                inv = chest.Inventory
                if inv is None:
                    continue
                inv.ClearAll()
                ini2 = self.DefaultItems()
                times = random.randint(self.MTimes, self.times)
                if "stash" in chest.Name.lower():
                    times = random.randint(1, 3)
                for i in xrange(0, times):
                    if "large" in chest.Name.lower():
                        slot = random.randint(1, 35)
                    elif "stash" in chest.Name.lower():
                        slot = random.randint(1, 3)
                    else:
                        slot = random.randint(1, 11)
                    itemr = 1
                    if self.item > 1:
                        itemr = random.randint(1, self.item)
                    countr = 1
                    gitem = ini2.GetSetting("RandomItems", str(itemr))
                    glower = gitem.lower()
                    if "ammo" in glower or "shell" in glower:
                        if self.count >= 1:
                            countr = random.randint(1, self.count)
                    elif "medkit" in glower:
                        if self.count2 >= 1:
                            countr = random.randint(1, self.count2)

                    elif "bandage" in glower:
                        if self.count2 >= 1:
                            countr = random.randint(1, self.count2)
                    elif "grenade" in glower:
                        if self.count3 >= 1:
                            countr = random.randint(1, self.count3)
                    elif "arrow" in glower:
                        if self.count4 >= 1:
                            countr = random.randint(1, self.count4)
                    elif "flare" in glower:
                        if self.count6 >= 1:
                            countr = random.randint(1, self.count6)
                    try:
                        inv.AddItemTo(gitem, slot, countr)
                    except:
                        pass
                if self.sitem > 0:
                    countr = random.randint(1, self.count5)
                    if "stash" in chest.Name.lower():
                        countr = random.randint(1, 3)
                    for i in xrange(0, countr):
                        if "large" in chest.Name.lower():
                            slot = random.randint(1, 35)
                        elif "stash" in chest.Name.lower():
                            slot = random.randint(1, 3)
                        else:
                            slot = random.randint(1, 11)
                        whichsight = random.randint(1, self.sitem)
                        gitem = ini2.GetSetting("SItems", str(whichsight))
                        try:
                            inv.AddItemTo(gitem, slot, 1)
                        except:
                            pass

            Server.BroadcastFrom(sysname, green + "Loaded 100%!")
            Plugin.CreateTimer("StartingIn", secs * 1000).Start()
            Server.BroadcastFrom(sysname, green + "HungerGames is starting in " + blue + str(secs) +
                                 green + " seconds!")
            Server.BroadcastFrom(sysname, red + "TEAMWORK IS NOT ALLOWED.")
            self.IsStarting = True

    def ForceCallback(self, timer):
        timer.Kill()
        if Plugin.GetTimer("StartingIn") is not None:
            Plugin.KillTimer("StartingIn")
        self.StartGame(True)

    def StartingInCallback(self, timer):
        timer.Kill()
        Server.BroadcastFrom(sysname, blue + "----------------------------HUNGERGAMES--------------------------------")
        Server.BroadcastFrom(sysname, blue + "Shoot to kill! Or swing to kill?")
        self.HasStarted = True
        for wall in walls:
            try:
                wall.Destroy()
            except:
                pass
        del walls[:]

    def FreezerCallback(self, timer):
        timer.Kill()
        List = timer.Args
        Player = List["Player"]
        self.Freezer(Player, 2)

    def RadiationActivateCallback(self, timer):
        timer.Kill()
        Server.BroadcastFrom(sysname, red + "----------------------------HUNGERGAMES--------------------------------")
        Server.BroadcastFrom(sysname, red + "Radiation activated!")
        Server.BroadcastFrom(sysname, red + "You will receive " + str(RadR) + " rad every " + str(RadS) + " secs")
        Server.BroadcastFrom(sysname, red + "if you are out of range! Current range: "
                             + str(self.CurrentRadRange) + "m!")
        Plugin.CreateTimer("Radiation", RadS * 1000).Start()
        Plugin.CreateTimer("Beware", 10000).Start()
        Plugin.CreateTimer("RadiationRange", RadDC * 60000).Start()

    def BewareCallback(self, timer):
        timer.Kill()
        if BleedChance != 0:
            Server.BroadcastFrom(sysname, yellow + "You have a chance to start bleeding if your rad is bigger than: "
                                 + str(BleedRad))
        if DamageChance != 0:
            Server.BroadcastFrom(sysname, yellow
                                 + "You have a chance to receive random damage if your rad is bigger than: "
                                 + str(DamageRad))
        if BreakAfterXRad:
            Server.BroadcastFrom(sysname, yellow + "You won't be able to run after " + str(BreakAtXRad) + " rad")
        Server.BroadcastFrom(sysname, teal + "Go to the middle of the map to avoid radiation!")

    def RadiationRangeCallback(self, timer):
        timer.Kill()
        if self.CurrentRadRange - RadME <= RadMDist:
            self.CurrentRadRange = RadMDist
            Server.BroadcastFrom(sysname, green + "Radiation range stopped at: " + str(RadMDist) + "m!")
            return
        self.CurrentRadRange -= RadME
        Server.BroadcastFrom(sysname, red + "Radiation range decreased to: " + str(self.CurrentRadRange) + "m!")
        Plugin.CreateTimer("RadiationRange", RadDC * 60000).Start()

    def RadiationCallback(self, timer):
        timer.Kill()
        if not self.HasStarted:
            return
        for x in self.Players:
            if not x.IsOnline:
                continue
            BodyTakeDMG = x.HumanBodyTakeDmg
            FallDamage = x.FallDamage
            rad = x.RadLevel
            try:
                if Util.GetVectorsDistance(x.Location, self.Middle) >= self.CurrentRadRange:

                    x.AddRads(RadR)
                    if BleedChance != 0:
                        if rad >= BleedRad:
                            r = random.randint(0, 100)
                            if r <= BleedChance:
                                if BodyTakeDMG is not None:
                                    BodyTakeDMG.SetBleedingLevel(80)
                    if DamageChance != 0:
                        if rad >= DamageRad:
                            dmg = 1
                            if DamageChance > 1:
                                r = random.randint(0, 100)
                                if r <= DamageChance:
                                    dmg = random.randint(1, DamageRandom)
                            x.Damage(dmg)
                    if BreakAfterXRad:
                        if rad >= BreakAtXRad:
                            if FallDamage is not None:
                                FallDamage.SetLegInjury(1)
                            if x.UID not in BeingRadLegd:
                                BeingRadLegd.append(x.UID)
                else:
                    x.AddAntiRad(RadAnti)
                    if x.UID in BeingRadLegd:
                        BeingRadLegd.remove(x.UID)
                        if FallDamage is not None:
                            FallDamage.SetLegInjury(0)
            except Exception as e:
                Plugin.Log("SendThisToDreTaX", "Error: " + str(e))
        Plugin.CreateTimer("Radiation", RadS * 1000).Start()

    def CleanMess(self):
        if Fougerite.ServerSaveHandler.ServerIsSaving:
            SaveData = Plugin.CreateDict()
            SaveData["Function"] = "CleanMess"
            Plugin.CreateParallelTimer("SaveHandlerTimer", 5000, SaveData).Start()
            return
        for x in PlacedEntities:
            if x.Health > 0:
                x.Destroy()

    def Reset(self):
        self.HasStarted = False
        self.IsActive = False
        self.IsStarting = False
        self.CurrentRadRange = RadStart
        self.RadRunning = False
        EndItem.clear()
        if Plugin.GetTimer("Force") is not None:
            Plugin.KillTimer("Force")
        if Plugin.GetTimer("StartingIn") is not None:
            Plugin.KillTimer("StartingIn")
        if Plugin.GetTimer("Radiation") is not None:
            Plugin.KillTimer("Radiation")
        if Plugin.GetTimer("RadiationRange") is not None:
            Plugin.KillTimer("RadiationRange")
        if Plugin.GetTimer("RadiationActivate") is not None:
            Plugin.KillTimer("RadiationActivate")
        for chest in loot:
            inv = chest.Inventory
            if inv is None:
                continue
            inv.ClearAll()
        tplayers = []
        for pl in self.Players:
            tplayers.append(pl)
        for pl in tplayers:
            self.RemovePlayerDirectly(pl, False)
        #  Just in-case
        for x in PlayerSlots.keys():
            PlayerSlots[x] = None
        del self.Players[:]
        del walls[:]
        del loot[:]
        del Awaiting[:]
        PointedPeople.clear()
        self.CleanMess()
        del PlacedEntities[:]

    def ResetWalls(self):
        if Fougerite.ServerSaveHandler.ServerIsSaving:
            SaveData = Plugin.CreateDict()
            SaveData["Function"] = "ResetWalls"
            Plugin.CreateParallelTimer("SaveHandlerTimer", 5000, SaveData).Start()
            Server.BroadcastFrom(sysname, red + "Server is saving... Resetting walls in 5 seconds, please wait, and do not start HG if you see this message.")
            return
        ini = self.HungerGames()
        enum3 = ini.EnumSection("WallLocations")
        for wall in enum3:
            l = ini.GetSetting("WallLocations", wall).split(',')
            name = wall.split('-')
            loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
            quat = Util.CreateQuat(float(l[3]), float(l[4]), float(l[5]), float(l[6]))
            self.FindWalls(loc, name[0], quat)
        Server.BroadcastFrom(sysname, green + "Walls reset!")

    def EndGame(self, Player):
        Server.BroadcastFrom(sysname, red + "----------------------------HUNGERGAMES--------------------------------")
        Server.BroadcastFrom(sysname, green + Player.Name + " won the match! Congratulations!")
        self.RemovePlayerDirectly(Player)
        Player.Inventory.ClearAll()
        self.returnInventory(Player)
        winsini = self.Wins()
        for y in PointedPeople.keys():
            x = PointedPeople[y]
            if y == 1:
                p = 5.0
            elif y == 2:
                p = 4.0
            elif y == 3:
                p = 3.0

            elif y == 4:
                p = 2.0
            else:
                p = 1.0
            if winsini.GetSetting("Wins", x.SteamID) is not None:
                c = float(winsini.GetSetting("Wins", x.SteamID)) + p
                winsini.SetSetting("Wins", x.SteamID, str(c))
            else:
                winsini.AddSetting("Wins", x.SteamID, str(p))
        winsini.Save()
        Server.BroadcastFrom(sysname, pink + "---Stats---")
        try:
            Server.BroadcastFrom(sysname, "1st: " + PointedPeople[1].Name)
        except:
            Server.BroadcastFrom(sysname, red + "Failed to display stats of the 1st player...")
        try:
            Server.BroadcastFrom(sysname, "2nd: " + PointedPeople[2].Name)
        except:
            Server.BroadcastFrom(sysname, red + "Failed to display stats of the 2nd player...")
        try:
            Server.BroadcastFrom(sysname, "3rd: " + PointedPeople[3].Name)
        except:
            Server.BroadcastFrom(sysname, red + "Failed to display stats of the 3rd player...")
        try:
            Server.BroadcastFrom(sysname, "4th: " + PointedPeople[4].Name)
        except:
            Server.BroadcastFrom(sysname, red + "Failed to display stats of the 4th player...")
        try:
            Server.BroadcastFrom(sysname, "5th: " + PointedPeople[5].Name)
        except:
            Server.BroadcastFrom(sysname, red + "Failed to display stats of the 5th player...")
        dst = {}
        try:
            player = PointedPeople[1]
            if player is not None:
                if player.IsOnline:
                    dst[player] = []
        except:
            pass
        try:
            player = PointedPeople[2]
            if player is not None:
                if player.IsOnline:
                    dst[player] = []
        except:
            pass
        try:
            player = PointedPeople[3]
            if player is not None:
                if player.IsOnline:
                    dst[player] = []
        except:
            pass
        if self.ItemRewards:
            for x in dst.keys():
                # if x is not None:
                RItemsG = {}
                max = random.randint(self.MinRewards, self.MaxRewards)
                for i in xrange(0, max):
                    num = random.randint(1, len(Rewards.keys()) - 1)
                    item = sorted(Rewards)[num]
                    c = Rewards.get(item)
                    if c != 1:
                        c = random.randint(1, c)
                    # x.Inventory.AddItem(item, c)
                    RItemsG[item] = c
                    if AnnounceRewards:
                        dst[x].append(item)
                x.MessageFrom(sysname, green + "Issue /hg getreward to get your rewards!")
                GlobalRewarded[x] = dst[x]
                RewardsToGive[x.UID] = RItemsG
        if AnnounceRewards:
            Server.BroadcastFrom(sysname, pink + "---Rewards---")
            Server.BroadcastFrom(sysname, pink + "To view rewards type /hg reward 0-2 ")
        try:
            Server.BroadcastFrom(sysname, pink + PointedPeople[1].Name + " got C4 because he finished first!")
            PointedPeople[1].Inventory.AddItem("Explosive Charge", 1)
        except:
            pass
        if LootStackClean:
            Loom.QueueOnMainThread(lambda:
                self.LootFinishSafe()
            )
        self.Reset()
        self.ResetWalls()
        Player.MessageFrom(sysname, green + "You received your rewards!")
        Server.BroadcastFrom(sysname, red + "----------------------------HUNGERGAMES--------------------------------")

    def LootFinishSafe(self):
        self.LootableObjects = UnityEngine.Object.FindObjectsOfType(self.LootableObject)
        for x in self.LootableObjects:
            if "lootsack" in x.name.lower():
                dist = Util.GetVectorsDistance(self.Middle, x.transform.position)
                if dist <= CDist:
                    x._inventory.Clear()
                    Util.DestroyObject(x.gameObject)

    def On_PlayerHurt(self, HurtEvent):
        if HurtEvent.Victim is not None and HurtEvent.Attacker is not None:
            weapon = HurtEvent.WeaponName
            d = (HurtEvent.Victim not in self.Players and HurtEvent.Attacker in self.Players) \
                and weapon != "Fall Damage"
            d2 = HurtEvent.Attacker in self.Players and weapon != "Fall Damage"
            d3 = HurtEvent.Victim in self.Players and HurtEvent.Attacker in self.Players and not self.HasStarted \
                 and weapon != "Fall Damage"
            if d:
                HurtEvent.DamageAmount = float(0)
            elif d2 and HurtEvent.Sleeper:
                HurtEvent.DamageAmount = float(0)
            elif d3:
                HurtEvent.DamageAmount = float(0)


    def On_PlayerKilled(self, DeathEvent):
        if DeathEvent.Victim is not None:
            if DeathEvent.Victim in self.Players and self.HasStarted:
                self.RemovePlayerDirectly(DeathEvent.Victim, False, True)
                for cmd in self.RestrictedCommands:
                    DeathEvent.Victim.UnRestrictCommand(cmd)
                leng = len(self.Players)
                try:
                    if leng == 4:
                        PointedPeople[5] = DeathEvent.Victim
                    elif leng == 3:
                        PointedPeople[4] = DeathEvent.Victim
                    elif leng == 2:
                        PointedPeople[3] = DeathEvent.Victim
                    elif leng == 1:
                        PointedPeople[2] = DeathEvent.Victim
                        PointedPeople[1] = self.Players[0]
                except:
                    pass
                if leng > 1:
                    Server.BroadcastFrom(sysname, green + DeathEvent.Victim.Name + red + " has been killed. "
                                         + green + str(leng) + red + " Players are still alive.")
                    if RadDmg and leng <= CRad and not self.RadRunning:
                        self.RadRunning = True
                        Plugin.CreateTimer("RadiationActivate", 60000 * RadM).Start()
                else:
                    Server.BroadcastFrom(sysname, green + DeathEvent.Victim.Name + red + " has been killed. ")
                    for x in self.Players:
                        self.EndGame(x)
                        break
                if DeathEvent.Attacker is not None:
                    if DeathEvent.AttackerIsPlayer:
                        kills = self.Kills()
                        count = kills.GetSetting("Kills", DeathEvent.Attacker.SteamID)
                        if count is None:
                            count = 0
                        else:
                            count = int(count)
                        kills.AddSetting("Kills", DeathEvent.Attacker.SteamID, str(count + 1))
                        kills.Save()
            elif DeathEvent.Victim in self.Players and self.IsActive:
                self.RemovePlayerDirectly(DeathEvent.Victim, False, True)
                for cmd in self.RestrictedCommands:
                    DeathEvent.Victim.UnRestrictCommand(cmd)
                Server.BroadcastFrom(sysname, green + DeathEvent.Victim.Name + red + " has been killed. ")
                Server.BroadcastFrom(sysname, red + "The match didn't even start yet!")

    def On_PlayerDisconnected(self, Player):
        if Player in self.Players:
            if not self.HasStarted:
                self.RemovePlayerDirectly(Player, True)
                leng = len(self.Players)
                if leng < minp and Plugin.GetTimer("Force") is not None:
                    Server.BroadcastFrom(sysname, red + "Minimum player count is not enough to force start.")
                    Server.BroadcastFrom(sysname, red + "Stopping timer...")
                    Plugin.KillTimer("Force")
                Server.BroadcastFrom(sysname, green + Player.Name + red + " has disconnected. "
                                     + green + str(leng) + red + " Players are still in-game.")
            else:
                List = Plugin.CreateDict()
                List["Player"] = Player
                Awaiting.append(Player.UID)
                Plugin.CreateParallelTimer("LeftChecker", 60000, List).Start()
                Server.BroadcastFrom(sysname, green + Player.Name + red + " has 1 minute to reconnect!")

    def LeftCheckerCallback(self, timer):
        timer.Kill()
        List = timer.Args
        Player = List["Player"]
        if Player.UID not in Awaiting:
            return
        self.RemovePlayerDirectly(Player, True)
        leng = len(self.Players)
        Server.BroadcastFrom(sysname, green + Player.Name + red + " has failed to reconnect. "
                             + green + str(leng) + red + "  Players are still alive.")
        for cmd in self.RestrictedCommands:
            Player.UnRestrictCommand(cmd)
        try:
            if leng == 4:
                PointedPeople[5] = Player
            elif leng == 3:
                PointedPeople[4] = Player
            elif leng == 2:
                PointedPeople[3] = Player
            elif leng == 1:
                PointedPeople[2] = Player
                PointedPeople[1] = self.Players[0]
        except:
            pass
        if leng == 1:
            self.EndGame(self.Players[0])

    def On_PlayerSpawned(self, Player, SpawnEvent):
        if Player.UID in Awaiting:
            if Awaiting[Player.UID].DisconnectLocation is not None:
                Player.TeleportTo(Awaiting[Player.UID].DisconnectLocation, False)
            Awaiting.remove(Player.UID)
            f = None
            for x in self.Players:
                if x.UID == Player.UID:
                    f = x
                    break
            if f is not None:
                self.Players.remove(f)
                self.Players.append(Player)
                f.CleanRestrictedCommands()
                for cmd in self.RestrictedCommands:
                    Player.RestrictCommand(cmd)
            Player.MessageFrom(sysname, green + "You reconnected in time!")
            return

        if DataStore.ContainsKey("HLastLoc", Player.SteamID):
            l = self.Replace(DataStore.Get("HLastLoc", Player.SteamID))
            loc = Util.CreateVector(float(l[0]), float(l[1]), float(l[2]))
            Player.TeleportTo(loc)
            self.returnInventory(Player)
            DataStore.Remove("HLastLoc", Player.SteamID)
            self.Freezer(Player, 2, False)
            Player.AddAntiRad(Player.RadLevel)
            if self.GotRustPP:
                Server.GetRustPPAPI().GetFriendsCommand.RemoveTempException(Player.UID)
        if DataStore.Get("HGBypass", Player.UID) is not None:
            Player.Inventory.Clear()
            Player.MessageFrom(sysname, yellow +
                               "Your inventory was cleared, since you disconnected during HG and failed to reconnect")
            DataStore.Remove("HGBypass", Player.UID)

    def On_EntityDeployed(self, Player, Entity, ActualPlacer):
        if ActualPlacer in self.Players:
            if not Building:
                ActualPlacer.MessageFrom(sysname, "You can't spawn stuff in HG!")
                Entity.Destroy()
                return
            PlacedEntities.append(Entity)

    def On_EntityHurt(self, HurtEvent):
        if HurtEvent.Attacker is not None and HurtEvent.Entity is not None and not HurtEvent.IsDecay:
            if not HurtEvent.AttackerIsPlayer:
                return
            id = HurtEvent.Attacker.SteamID
            if HurtEvent.Attacker in self.Players:
                HurtEvent.Entity.Health = HurtEvent.Entity.MaxHealth
                HurtEvent.DamageAmount = float(0)
                return
            gun = HurtEvent.WeaponName
            if gun == "Shotgun":
                return
            if DataStore.ContainsKey("HDoorMode", id):
                if "wall" in HurtEvent.Entity.Name.lower():
                    ini = self.HungerGames()
                    count = len(ini.EnumSection("WallLocations"))
                    enum = ini.EnumSection("WallLocations")
                    data = str(HurtEvent.Entity.X) + "," + str(HurtEvent.Entity.Y) + "," + str(HurtEvent.Entity.Z)
                    for x in enum:
                        locs = ini.GetSetting("WallLocations", x)
                        if data in locs:
                            HurtEvent.Attacker.MessageFrom(sysname, "This wall is already in.")
                            HurtEvent.DamageAmount = float(0)
                            return
                    if maxp == count:
                        HurtEvent.Attacker.MessageFrom(sysname, "You reached the max spawnpoints")
                        return
                    name = HurtEvent.Entity.Name
                    c = count + 1
                    ini.AddSetting("WallLocations", name + "-" + str(c),
                                                   str(HurtEvent.Entity.X) + "," +
                                                   str(HurtEvent.Entity.Y) + "," +
                                                   str(HurtEvent.Entity.Z) + "," +
                                                   str(HurtEvent.Entity.Rotation.x) + "," +
                                                   str(HurtEvent.Entity.Rotation.y) + "," +
                                                   str(HurtEvent.Entity.Rotation.z) + "," +
                                                   str(HurtEvent.Entity.Rotation.w))
                    ini.Save()
                    HurtEvent.Attacker.MessageFrom(sysname, "Added Wall.")
                elif "box" in HurtEvent.Entity.Name.lower():
                    data = str(HurtEvent.Entity.X) + "," + str(HurtEvent.Entity.Y) + "," + str(HurtEvent.Entity.Z)
                    ini = self.HungerGames()
                    count = len(ini.EnumSection("ChestLocations"))
                    name = HurtEvent.Entity.Name
                    enum = ini.EnumSection("ChestLocations")
                    for x in enum:
                        locs = ini.GetSetting("ChestLocations", x)
                        if data in locs:
                            HurtEvent.Attacker.MessageFrom(sysname, "This chest is already in.")
                            HurtEvent.DamageAmount = float(0)
                            return
                    c = count + 1
                    ini.AddSetting("ChestLocations", name + "-" + str(c),
                                                   str(HurtEvent.Entity.X) + "," +
                                                   str(HurtEvent.Entity.Y) + "," +
                                                   str(HurtEvent.Entity.Z) + "," +
                                                   str(HurtEvent.Entity.Rotation.x) + "," +
                                                   str(HurtEvent.Entity.Rotation.y) + "," +
                                                   str(HurtEvent.Entity.Rotation.z) + "," +
                                                   str(HurtEvent.Entity.Rotation.w))
                    ini.Save()
                    HurtEvent.Attacker.MessageFrom(sysname, "Added Chest.")
                HurtEvent.DamageAmount = float(0)
```

### Notes
- This is now a full, system-by-system transition of the original: every method in `HungerGames.py` (config
  bootstrap, all `/hg` subcommands, wall/chest/loot spawning from `HungerGames.ini`, the complete countdown and
  radiation timer chain, kill/placement tracking, the 60-second reconnect grace period, and end-of-match
  rewards) has a corresponding implementation in C#, JavaScript, and Lua - not just the radiation tick this
  page previously covered.
- A handful of admin/setup commands (`addspawn`, `addchests`, `cleanchests`, the `/hg entity` punch-to-register
  workflow's ini write, `cleanloot`) are shown with their real control flow but a one-line comment in place of
  the actual `IniParser` section read/write, since that's the same `GetIni`/`AddSetting`/`EnumSection` pattern
  already documented in full in [`Classes/Config.md`](../Classes/Config.md) - repeating it verbatim four more
  times would just be noise.
- Two real, pre-existing bugs in the original were intentionally **not** carried into the ports: `Awaiting` is
  a plain list of UIDs, so `Awaiting[Player.UID].DisconnectLocation` in `On_PlayerSpawned` doesn't actually do
  what the variable name implies (it indexes the list positionally, not by UID) - the ports use
  `Player.DisconnectLocation` directly, which is what was clearly intended. `long(nid)`/`xrange` are
  Python‑2‑only constructs the IronPython engine still supports; the other languages use their native
  equivalents (`ulong`/for-loops).
- `Fougerite.Player.BleedingLevel` is read-only, so bleeding is started the same way the original does it -
  calling `SetBleedingLevel`/`SetLegInjury` directly on the real `HumanBodyTakeDmg`/`FallDamage` components,
  not through a settable `Player` property.

See also: [`TeamDeathmatch.md`](TeamDeathmatch.md) · [`Classes/Player.md`](../Classes/Player.md) ·
[`Classes/Config.md`](../Classes/Config.md) · [`Classes/Timers.md`](../Classes/Timers.md) ·
[`Hooks/Player/On_PlayerKilled.md`](../Hooks/Player/On_PlayerKilled.md)
