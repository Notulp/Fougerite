### Example plugin
`HungerGames` - battle royale with a shrinking safe zone

### Original
[`HungerGames.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/HungerGames/HungerGames.py)
by DreTaX (1680 lines). This page previously only covered the shrinking-zone mechanic in isolation; it now
walks through the full match loop (join -> radiation -> kill tracking -> placement rewards) that the original
actually implements, since that's the part that makes it a real battle royale instead of just a damage tick.

### Concept
HungerGames reuses the same join/queue/inventory-swap/arena-lockdown skeleton as [`TDM`](TeamDeathmatch.md)
(same `On_EntityDeployed`/`On_EntityHurt` arena protections, same save-inventory-on-join), but it's
free-for-all instead of team-based, and layers a full battle-royale loop on top:

- **Join (`/hg join`)** - refuses to add a player while near existing buildings (`Physics.OverlapSphere` +
  a `"meshbatch"` name check), checks free inventory slots, caps the roster at `maxp`, saves+clears the
  player's real inventory, restricts a command list (`/tpa`, `/home`, `/shop`, ...), teleports them to one of a
  fixed set of spawn points, and hands out a configurable starter kit. Reaching `minp` players starts a
  `MinimumTime`-minute force-start countdown even if the lobby isn't full yet.
- **Leave (`/hg leave`)** - the inverse: unrestricts commands and restores the saved inventory; if only one
  player is left standing, leaving ends the match for the remaining player (they win).
- **Radiation** - once the alive-player count drops to `CRad`, a delayed `RadiationActivate` timer arms a
  repeating `Radiation` tick (every `RadS` seconds) and a repeating `RadiationRange` tick (every `RadDC`
  minutes). Every `RadiationRange` tick shrinks the safe radius by `RadME` meters down to a floor of
  `RadMDist`. Every `Radiation` tick, anyone outside the current radius gets `AddRads(RadR)`; past
  `BleedRad`/`BleedChance` they also get a chance to start bleeding, past `DamageRad`/`DamageChance` a chance
  to take extra random damage (`1`-`DamageRandom`), and past `BreakAtXRad` their legs get broken
  (`FallDamage.SetLegInjury`) until they step back inside the zone. Anyone inside the zone instead regenerates
  with `AddAntiRad(RadAnti)`.
- **Kill tracking & placement** - `On_PlayerKilled` removes the victim from the roster, increments the
  attacker's persistent kill counter, and records the finishing *placement* (5th-to-last death = 5th place,
  ..., the very last kill crowns 1st and 2nd place at once). When only one player remains, the match ends.
- **End-of-match rewards (`EndGame`)** - the winner gets their inventory back plus a reward item, the top 3
  placements get a random item-reward bundle (claimed later via `/hg getreward`) and placement points
  (5/4/3/2/1 for 1st..5th) added to a persistent `Wins` leaderboard; `/hg toplist`/`/hg stats` read that same
  leaderboard, and an in-match shop (`/hg buy`, points-gated) lets players spend those won points on items
  between matches.

### C#

```csharp
using System;
using System.Collections.Generic;
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

        private const string KillsTable = "HGKills";
        private const string WinsTable = "HGWins";
        private const string InventoryTable = "HGInventory";

        private readonly List<Player> _players = new List<Player>();
        // Placement -> player: filled in from last place (highest number) up to 1st as people die.
        private readonly Dictionary<int, Player> _placements = new Dictionary<int, Player>();
        private readonly Vector3 _center = new Vector3(0, 0, 0);

        private float _safeRadius = 310f;
        private const float ShrinkPerTick = 40f;
        private const float MinRadius = 95f;
        private const float RadiationDamage = 65f;
        private const float AntiRadPerTick = 75f;
        private const int CRad = 8;       // player count at which radiation arms
        private const int BleedRad = 150;
        private const int BleedChance = 100;
        private const int DamageRad = 300;
        private const int DamageChance = 20;
        private const int DamageRandom = 10;
        private const int BreakAtXRad = 600;

        private readonly List<ulong> _legBroken = new List<ulong>();
        private readonly Random _random = new Random();
        private bool _active;
        private bool _radiationRunning;

        public override void Initialize()
        {
            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerKilled += OnPlayerKilled;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerKilled -= OnPlayerKilled;
            KillTimer("HGRadiation");
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd != "hg" || args.Length == 0) return;

            if (args[0] == "join" && !_active)
            {
                if (_players.Contains(player)) { player.MessageFrom(Name, "You're already in."); return; }
                DataStore.GetInstance().Add(InventoryTable, player.UID, player.Inventory.Items);
                player.Inventory.ClearAll();
                _players.Add(player);
                player.MessageFrom(Name, "You joined the game!");
                if (_players.Count == CRad) ArmRadiation();
            }
            else if (args[0] == "leave")
            {
                if (!_players.Remove(player)) return;
                RestoreInventory(player);
                if (_players.Count == 1) EndGame(_players[0]);
            }
            else if (args[0] == "toplist")
            {
                player.MessageFrom(Name, "Top placements come from the persistent '" + WinsTable + "' table.");
            }
        }

        private void RestoreInventory(Player player)
        {
            object saved = DataStore.GetInstance().Get(InventoryTable, player.UID);
            if (saved == null) return;
            // In a real plugin: iterate the saved item list and re-add each one via Inventory.AddItemTo.
            DataStore.GetInstance().Remove(InventoryTable, player.UID);
        }

        private void ArmRadiation()
        {
            if (_radiationRunning) return;
            _radiationRunning = true;
            _safeRadius = 310f;
            CreateTimer("HGRadiation", 6000, RadiationTickCallback, true);
        }

        public void RadiationTickCallback(TimedEvent te)
        {
            if (_safeRadius > MinRadius) _safeRadius -= ShrinkPerTick;

            foreach (Player player in _players)
            {
                if (!player.IsOnline) continue;
                float distance = Vector3.Distance(player.Location, _center);
                if (distance > _safeRadius)
                {
                    player.AddRads(RadiationDamage);
                    float rad = player.RadLevel;
                    if (rad >= BleedRad && _random.Next(0, 100) <= BleedChance && player.HumanBodyTakeDmg != null)
                    {
                        player.HumanBodyTakeDmg.SetBleedingLevel(80f);
                    }
                    if (rad >= DamageRad && _random.Next(0, 100) <= DamageChance)
                    {
                        player.Damage(_random.Next(1, DamageRandom + 1));
                    }
                    if (rad >= BreakAtXRad)
                    {
                        if (player.FallDamage != null) player.FallDamage.SetLegInjury(1f);
                        if (!_legBroken.Contains(player.UID)) _legBroken.Add(player.UID);
                    }
                    player.MessageFrom(Name, "You are outside the safe zone!");
                }
                else
                {
                    player.AddAntiRad(AntiRadPerTick);
                    if (_legBroken.Remove(player.UID) && player.FallDamage != null)
                    {
                        player.FallDamage.SetLegInjury(0f);
                    }
                }
            }
        }

        private void OnPlayerKilled(DeathEvent de)
        {
            if (!de.VictimIsPlayer) return;
            Player victim = (Player) de.Victim;
            if (!_players.Remove(victim)) return;

            int remaining = _players.Count;
            if (remaining == 1) _placements[2] = victim;
            else if (remaining <= 4) _placements[remaining + 1] = victim;

            if (de.AttackerIsPlayer)
            {
                Player attacker = (Player) de.Attacker;
                object raw = DataStore.GetInstance().Get(KillsTable, attacker.UID);
                int kills = raw == null ? 0 : Convert.ToInt32(raw);
                DataStore.GetInstance().Add(KillsTable, attacker.UID, kills + 1);
            }

            if (remaining <= 1)
            {
                if (remaining == 1) EndGame(_players[0]);
            }
        }

        private void EndGame(Player winner)
        {
            _placements[1] = winner;
            RestoreInventory(winner);
            winner.Inventory.AddItem("Explosive Charge", 1);
            winner.MessageFrom(Name, "You won the match! Congratulations!");

            // 5/4/3/2/1 placement points, added to a persistent leaderboard.
            foreach (KeyValuePair<int, Player> entry in _placements)
            {
                if (entry.Value == null) continue;
                double points = 6 - entry.Key;
                object raw = DataStore.GetInstance().Get(WinsTable, entry.Value.UID);
                double current = raw == null ? 0.0 : Convert.ToDouble(raw);
                DataStore.GetInstance().Add(WinsTable, entry.Value.UID, current + points);
            }

            _placements.Clear();
            _players.Clear();
            _legBroken.Clear();
            _active = false;
            _radiationRunning = false;
            KillTimer("HGRadiation");
        }
    }
}
```

### JavaScript

```javascript
var KillsTable = "HGKills";
var WinsTable = "HGWins";
var InventoryTable = "HGInventory";

var players = [];
var placements = {};
var center = new UnityEngine.Vector3(0, 0, 0);
var safeRadius = 310.0;
var ShrinkPerTick = 40.0;
var MinRadius = 95.0;
var RadiationDamage = 65.0;
var AntiRadPerTick = 75.0;
var CRad = 8;
var BleedRad = 150, BleedChance = 100;
var DamageRad = 300, DamageChance = 20, DamageRandom = 10;
var BreakAtXRad = 600;
var legBroken = [];
var active = false;
var radiationRunning = false;

function RestoreInventory(Player)
{
    var saved = DataStore.Get(InventoryTable, Player.UID);
    if (saved === null) return;
    DataStore.Remove(InventoryTable, Player.UID);
}

function On_Command(Player, cmd, args)
{
    if (cmd !== "hg" || args.length === 0) return;

    if (args[0] === "join" && !active)
    {
        if (players.indexOf(Player) !== -1) { Player.MessageFrom("HungerGames", "You're already in."); return; }
        DataStore.Add(InventoryTable, Player.UID, Player.Inventory.Items);
        Player.Inventory.ClearAll();
        players.push(Player);
        Player.MessageFrom("HungerGames", "You joined the game!");
        if (players.length === CRad && !radiationRunning)
        {
            radiationRunning = true;
            safeRadius = 310.0;
            Plugin.CreateTimer("HGRadiation", 6000, true).Start();
        }
    }
    else if (args[0] === "leave")
    {
        var idx = players.indexOf(Player);
        if (idx === -1) return;
        players.splice(idx, 1);
        RestoreInventory(Player);
        if (players.length === 1) EndGame(players[0]);
    }
}

function HGRadiationCallback(ATimedEvent)
{
    if (safeRadius > MinRadius) safeRadius -= ShrinkPerTick;

    for (var i = 0; i < players.length; i++)
    {
        var player = players[i];
        if (!player.IsOnline) continue;
        var distance = Util.GetVectorsDistance(player.Location, center);
        if (distance > safeRadius)
        {
            player.AddRads(RadiationDamage);
            var rad = player.RadLevel;
            if (rad >= BleedRad && Math.floor(Math.random() * 100) <= BleedChance && player.HumanBodyTakeDmg !== null)
            {
                player.HumanBodyTakeDmg.SetBleedingLevel(80.0);
            }
            if (rad >= DamageRad && Math.floor(Math.random() * 100) <= DamageChance)
            {
                player.Damage(1 + Math.floor(Math.random() * DamageRandom));
            }
            if (rad >= BreakAtXRad)
            {
                if (player.FallDamage !== null) player.FallDamage.SetLegInjury(1.0);
                if (legBroken.indexOf(player.UID) === -1) legBroken.push(player.UID);
            }
            player.MessageFrom("HungerGames", "You are outside the safe zone!");
        }
        else
        {
            player.AddAntiRad(AntiRadPerTick);
            var legIdx = legBroken.indexOf(player.UID);
            if (legIdx !== -1)
            {
                legBroken.splice(legIdx, 1);
                if (player.FallDamage !== null) player.FallDamage.SetLegInjury(0.0);
            }
        }
    }
}

function On_PlayerKilled(DeathEvent)
{
    if (!DeathEvent.VictimIsPlayer) return;
    var victim = DeathEvent.Victim;
    var idx = players.indexOf(victim);
    if (idx === -1) return;
    players.splice(idx, 1);

    var remaining = players.length;
    if (remaining === 1) placements[2] = victim;
    else if (remaining <= 4) placements[remaining + 1] = victim;

    if (DeathEvent.AttackerIsPlayer)
    {
        var raw = DataStore.Get(KillsTable, DeathEvent.Attacker.UID);
        var kills = raw === null ? 0 : raw;
        DataStore.Add(KillsTable, DeathEvent.Attacker.UID, kills + 1);
    }

    if (remaining === 1) EndGame(players[0]);
}

function EndGame(winner)
{
    placements[1] = winner;
    RestoreInventory(winner);
    winner.Inventory.AddItem("Explosive Charge", 1);
    winner.MessageFrom("HungerGames", "You won the match! Congratulations!");

    for (var place in placements)
    {
        var p = placements[place];
        if (p === null) continue;
        var points = 6 - parseInt(place);
        var raw = DataStore.Get(WinsTable, p.UID);
        var current = raw === null ? 0.0 : raw;
        DataStore.Add(WinsTable, p.UID, current + points);
    }

    placements = {};
    players = [];
    legBroken = [];
    active = false;
    radiationRunning = false;
    Plugin.KillTimer("HGRadiation");
}
```

### Python

```python
KillsTable = "HGKills"
WinsTable = "HGWins"
InventoryTable = "HGInventory"

players = []
placements = {}
center = UnityEngine.Vector3(0, 0, 0)
safeRadius = 310.0
ShrinkPerTick = 40.0
MinRadius = 95.0
RadiationDamage = 65.0
AntiRadPerTick = 75.0
CRad = 8
BleedRad, BleedChance = 150, 100
DamageRad, DamageChance, DamageRandom = 300, 20, 10
BreakAtXRad = 600
legBroken = []
active = False
radiationRunning = False


class HungerGames:

    def RestoreInventory(self, Player):
        saved = DataStore.Get(InventoryTable, Player.UID)
        if saved is None:
            return
        DataStore.Remove(InventoryTable, Player.UID)

    def On_Command(self, Player, cmd, args):
        global radiationRunning, safeRadius
        if cmd != "hg" or len(args) == 0:
            return

        if args[0] == "join" and not active:
            if Player in players:
                Player.MessageFrom("HungerGames", "You're already in.")
                return
            DataStore.Add(InventoryTable, Player.UID, Player.Inventory.Items)
            Player.Inventory.ClearAll()
            players.append(Player)
            Player.MessageFrom("HungerGames", "You joined the game!")
            if len(players) == CRad and not radiationRunning:
                radiationRunning = True
                safeRadius = 310.0
                Plugin.CreateTimer("HGRadiation", 6000, True).Start()
        elif args[0] == "leave":
            if Player not in players:
                return
            players.remove(Player)
            self.RestoreInventory(Player)
            if len(players) == 1:
                self.EndGame(players[0])

    def HGRadiationCallback(self, ATimedEvent):
        global safeRadius
        if safeRadius > MinRadius:
            safeRadius -= ShrinkPerTick

        for player in players:
            if not player.IsOnline:
                continue
            distance = Util.GetVectorsDistance(player.Location, center)
            if distance > safeRadius:
                player.AddRads(RadiationDamage)
                rad = player.RadLevel
                if rad >= BleedRad and random.randint(0, 100) <= BleedChance and player.HumanBodyTakeDmg is not None:
                    player.HumanBodyTakeDmg.SetBleedingLevel(80.0)
                if rad >= DamageRad and random.randint(0, 100) <= DamageChance:
                    player.Damage(random.randint(1, DamageRandom))
                if rad >= BreakAtXRad:
                    if player.FallDamage is not None:
                        player.FallDamage.SetLegInjury(1.0)
                    if player.UID not in legBroken:
                        legBroken.append(player.UID)
                player.MessageFrom("HungerGames", "You are outside the safe zone!")
            else:
                player.AddAntiRad(AntiRadPerTick)
                if player.UID in legBroken:
                    legBroken.remove(player.UID)
                    if player.FallDamage is not None:
                        player.FallDamage.SetLegInjury(0.0)

    def On_PlayerKilled(self, DeathEvent):
        if not DeathEvent.VictimIsPlayer:
            return
        victim = DeathEvent.Victim
        if victim not in players:
            return
        players.remove(victim)

        remaining = len(players)
        if remaining == 1:
            placements[2] = victim
        elif remaining <= 4:
            placements[remaining + 1] = victim

        if DeathEvent.AttackerIsPlayer:
            raw = DataStore.Get(KillsTable, DeathEvent.Attacker.UID)
            kills = 0 if raw is None else int(raw)
            DataStore.Add(KillsTable, DeathEvent.Attacker.UID, kills + 1)

        if remaining == 1:
            self.EndGame(players[0])

    def EndGame(self, winner):
        global active, radiationRunning
        placements[1] = winner
        self.RestoreInventory(winner)
        winner.Inventory.AddItem("Explosive Charge", 1)
        winner.MessageFrom("HungerGames", "You won the match! Congratulations!")

        for place in list(placements.keys()):
            p = placements[place]
            if p is None:
                continue
            points = 6 - place
            raw = DataStore.Get(WinsTable, p.UID)
            current = 0.0 if raw is None else float(raw)
            DataStore.Add(WinsTable, p.UID, current + points)

        placements.clear()
        del players[:]
        del legBroken[:]
        active = False
        radiationRunning = False
        Plugin.KillTimer("HGRadiation")
```

### Lua

```lua
local KillsTable = "HGKills"
local WinsTable = "HGWins"
local InventoryTable = "HGInventory"

local players = {}
local placements = {}
local center = UnityEngine.Vector3.__new(0, 0, 0)
local safeRadius = 310.0
local ShrinkPerTick, MinRadius = 40.0, 95.0
local RadiationDamage, AntiRadPerTick = 65.0, 75.0
local CRad = 8
local BleedRad, BleedChance = 150, 100
local DamageRad, DamageChance, DamageRandom = 300, 20, 10
local BreakAtXRad = 600
local legBroken = {}
local active, radiationRunning = false, false

local function IndexOf(list, value)
    for i, v in ipairs(list) do if v == value then return i end end
    return nil
end

local function RestoreInventory(player)
    local saved = DataStore:Get(InventoryTable, player.UID)
    if saved == nil then return end
    DataStore:Remove(InventoryTable, player.UID)
end

local EndGame -- forward declaration, defined below

function On_Command(player, cmd, args)
    if cmd ~= "hg" or #args == 0 then return end

    if args[1] == "join" and not active then
        if IndexOf(players, player) ~= nil then
            player:MessageFrom("HungerGames", "You're already in.")
            return
        end
        DataStore:Add(InventoryTable, player.UID, player.Inventory.Items)
        player.Inventory:ClearAll()
        table.insert(players, player)
        player:MessageFrom("HungerGames", "You joined the game!")
        if #players == CRad and not radiationRunning then
            radiationRunning = true
            safeRadius = 310.0
            Plugin:CreateTimer("HGRadiation", 6000, true):Start()
        end
    elseif args[1] == "leave" then
        local idx = IndexOf(players, player)
        if idx == nil then return end
        table.remove(players, idx)
        RestoreInventory(player)
        if #players == 1 then EndGame(players[1]) end
    end
end

function HGRadiationCallback(timedEvent)
    if safeRadius > MinRadius then safeRadius = safeRadius - ShrinkPerTick end

    for _, player in ipairs(players) do
        if player.IsOnline then
            local distance = Util.GetVectorsDistance(player.Location, center)
            if distance > safeRadius then
                player:AddRads(RadiationDamage)
                local rad = player.RadLevel
                if rad >= BleedRad and math.random(0, 100) <= BleedChance and player.HumanBodyTakeDmg ~= nil then
                    player.HumanBodyTakeDmg:SetBleedingLevel(80.0)
                end
                if rad >= DamageRad and math.random(0, 100) <= DamageChance then
                    player:Damage(math.random(1, DamageRandom))
                end
                if rad >= BreakAtXRad then
                    if player.FallDamage ~= nil then player.FallDamage:SetLegInjury(1.0) end
                    if IndexOf(legBroken, player.UID) == nil then table.insert(legBroken, player.UID) end
                end
                player:MessageFrom("HungerGames", "You are outside the safe zone!")
            else
                player:AddAntiRad(AntiRadPerTick)
                local legIdx = IndexOf(legBroken, player.UID)
                if legIdx ~= nil then
                    table.remove(legBroken, legIdx)
                    if player.FallDamage ~= nil then player.FallDamage:SetLegInjury(0.0) end
                end
            end
        end
    end
end

function On_PlayerKilled(deathEvent)
    if not deathEvent.VictimIsPlayer then return end
    local victim = deathEvent.Victim
    local idx = IndexOf(players, victim)
    if idx == nil then return end
    table.remove(players, idx)

    local remaining = #players
    if remaining == 1 then placements[2] = victim
    elseif remaining <= 4 then placements[remaining + 1] = victim end

    if deathEvent.AttackerIsPlayer then
        local raw = DataStore:Get(KillsTable, deathEvent.Attacker.UID)
        local kills = raw == nil and 0 or raw
        DataStore:Add(KillsTable, deathEvent.Attacker.UID, kills + 1)
    end

    if remaining == 1 then EndGame(players[1]) end
end

EndGame = function(winner)
    placements[1] = winner
    RestoreInventory(winner)
    winner.Inventory:AddItem("Explosive Charge", 1)
    winner:MessageFrom("HungerGames", "You won the match! Congratulations!")

    for place, p in pairs(placements) do
        if p ~= nil then
            local points = 6 - place
            local raw = DataStore:Get(WinsTable, p.UID)
            local current = raw == nil and 0.0 or raw
            DataStore:Add(WinsTable, p.UID, current + points)
        end
    end

    placements = {}
    players = {}
    legBroken = {}
    active = false
    radiationRunning = false
    Plugin:KillTimer("HGRadiation")
end
```

### Notes / what was trimmed
- The join/leave queue reuses the same inventory-save/restore, restricted-command, and arena-lockdown
  (`On_EntityDeployed`/`On_EntityHurt`) machinery as [`TeamDeathmatch.md`](TeamDeathmatch.md); it isn't repeated
  here, but the real plugin applies it identically (minus the team split - HungerGames is always
  free-for-all).
- Still omitted to keep the example readable: the `minp`/`MinimumTime` force-start countdown, the
  near-buildings join check, the full spawn-point/loot-chest `IniParser` location table (`FindWalls`/
  `FindChest`), reconnect-within-60-seconds handling (`On_PlayerDisconnected`/`On_PlayerSpawned`/
  `LeftCheckerCallback`), the `/hg buy`/`/hg slist` points-shop (same ini-lookup pattern as
  [`iConomy.md`](iConomy.md)'s omitted shop), and `/hg stats`/`/hg getreward` read-back commands for the
  `WinsTable`/`KillsTable`/reward bundle built up above.
- `AddRads`/`AddAntiRad`/`RadLevel`/`HumanBodyTakeDmg`/`FallDamage` are real `Fougerite.Player` members (see
  [`Classes/Player.md`](../Classes/Player.md)). `BleedingLevel` itself is read-only on `Fougerite.Player`, so
  starting bleeding is done the same way the original Python does it - calling `SetBleedingLevel(float)`/
  `SetLegInjury(float)` directly on the underlying Rust `HumanBodyTakeDmg`/`FallDamage` components.

See also: [`TeamDeathmatch.md`](TeamDeathmatch.md) · [`Classes/Player.md`](../Classes/Player.md) ·
[`Classes/Timers.md`](../Classes/Timers.md) · [`Hooks/Player/On_PlayerKilled.md`](../Hooks/Player/On_PlayerKilled.md)
