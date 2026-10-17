### Example plugin
`TDM` - round-based team deathmatch arena

### Original
[`TDM.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/TDM/TDM.py) by DreTaX. The
author's notes call this one "never finished", which matches the issue that inspired this page: a locked arena
with a GUI client (RustBuster) could drive sign-up/team-select instead of chat commands, turning this into a
proper lobby-based TDM.

### Concept
TDM runs as a single shared queue (not yet split into two teams in the original - that part was left
unfinished), gated by an `/tdm announce` admin command with a cooldown so it can't be spammed. The interesting
parts, independent of the unfinished team split:

- **Join flow**: `/tdm join` records the player's current position (`DataStore`), **saves and clears their
  entire inventory** (so dying in the arena can't cost them their real loot), restricts a configurable list of
  commands (`/home`, `/tpa`, `/shop`, ...) for the duration of the match, and teleports them to a lobby/arena
  spawn point.
- **Leave/end flow**: `/tdm leave` reverses all of the above - unrestricts commands, returns the original
  inventory, and teleports the player back to where they were when they joined.
- **Arena lockdown**: while in the match, `On_EntityDeployed` blocks placing anything (no building inside the
  arena) and `On_EntityHurt` makes every arena wall/spawn-point structure immortal (reset to full health,
  zero damage) so the map's layout can't be griefed mid-match.
- **Auto-start**: once enough players have queued (`MinimumPlayers`), a countdown timer force-starts the match
  even if the configured `MaxPlayers` cap hasn't been reached yet.

### C#

```csharp
using System;
using System.Collections.Generic;
using Fougerite;

namespace TeamDeathmatch
{
    public class TDM : Module
    {
        public override string Name { get { return "TDM"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Round-based team deathmatch arena."; } }
        public override Version Version { get { return new Version(1, 0); } }

        private readonly List<Player> _players = new List<Player>();
        private readonly string[] _restrictedCommands = { "home", "tpa", "shop" };
        private const string InventoryTable = "TDMInventory";
        private const string ReturnLocationTable = "TDMReturnLocation";
        private readonly UnityEngine.Vector3 _lobby = new UnityEngine.Vector3(0, 0, 0);

        public override void Initialize()
        {
            Hooks.OnCommand += OnCommand;
            Hooks.OnEntityDeployed += OnEntityDeployed;
            Hooks.OnEntityHurt += OnEntityHurt;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
            Hooks.OnEntityDeployed -= OnEntityDeployed;
            Hooks.OnEntityHurt -= OnEntityHurt;
        }

        private void SaveAndClearInventory(Player player)
        {
            DataStore.GetInstance().Add(InventoryTable, player.UID, player.Inventory.Items);
            player.Inventory.ClearAll();
        }

        private void RestoreInventory(Player player)
        {
            object saved = DataStore.GetInstance().Get(InventoryTable, player.UID);
            if (saved == null) return;
            // In a real plugin: iterate the saved item list and re-add each one via Inventory.AddItemTo.
            DataStore.GetInstance().Remove(InventoryTable, player.UID);
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd != "tdm" || args.Length == 0) return;

            if (args[0] == "join")
            {
                if (_players.Contains(player))
                {
                    player.MessageFrom(Name, "You're already queued.");
                    return;
                }
                DataStore.GetInstance().Add(ReturnLocationTable, player.UID, player.Location);
                SaveAndClearInventory(player);
                foreach (string restricted in _restrictedCommands)
                {
                    player.RestrictCommand(restricted);
                }
                _players.Add(player);
                player.TeleportTo(_lobby);
                player.MessageFrom(Name, "You joined the TDM queue!");
            }
            else if (args[0] == "leave")
            {
                if (!_players.Remove(player)) return;
                foreach (string restricted in _restrictedCommands)
                {
                    player.UnRestrictCommand(restricted);
                }
                RestoreInventory(player);
                object returnLoc = DataStore.GetInstance().Get(ReturnLocationTable, player.UID);
                if (returnLoc != null)
                {
                    player.TeleportTo((UnityEngine.Vector3) returnLoc);
                    DataStore.GetInstance().Remove(ReturnLocationTable, player.UID);
                }
                player.MessageFrom(Name, "You left TDM.");
            }
        }

        private void OnEntityDeployed(Player player, Entity entity, Player actualPlacer)
        {
            if (_players.Contains(actualPlacer))
            {
                actualPlacer.MessageFrom(Name, "You can't build inside the arena.");
                entity.Destroy();
            }
        }

        private void OnEntityHurt(HurtEvent he)
        {
            if (he.Attacker != null && _players.Contains(he.Attacker) && he.Entity != null)
            {
                // Arena structures (walls, spawn points) are immortal during a match.
                he.Entity.Health = he.Entity.MaxHealth;
                he.DamageAmount = 0f;
            }
        }
    }
}
```

### JavaScript

```javascript
var players = [];
var restrictedCommands = ["home", "tpa", "shop"];
var InventoryTable = "TDMInventory";
var ReturnLocationTable = "TDMReturnLocation";
var lobby = Util.CreateVector(0, 0, 0);

function SaveAndClearInventory(Player)
{
    DataStore.Add(InventoryTable, Player.UID, Player.Inventory.Items);
    Player.Inventory.ClearAll();
}

function RestoreInventory(Player)
{
    var saved = DataStore.Get(InventoryTable, Player.UID);
    if (saved === null) return;
    DataStore.Remove(InventoryTable, Player.UID);
}

function On_Command(Player, cmd, args)
{
    if (cmd !== "tdm" || args.length === 0) return;

    if (args[0] === "join")
    {
        if (players.indexOf(Player) !== -1)
        {
            Player.MessageFrom("TDM", "You're already queued.");
            return;
        }
        DataStore.Add(ReturnLocationTable, Player.UID, Player.Location);
        SaveAndClearInventory(Player);
        for (var i = 0; i < restrictedCommands.length; i++) Player.RestrictCommand(restrictedCommands[i]);
        players.push(Player);
        Player.TeleportTo(lobby, false);
        Player.MessageFrom("TDM", "You joined the TDM queue!");
    }
    else if (args[0] === "leave")
    {
        var idx = players.indexOf(Player);
        if (idx === -1) return;
        players.splice(idx, 1);
        for (var i = 0; i < restrictedCommands.length; i++) Player.UnRestrictCommand(restrictedCommands[i]);
        RestoreInventory(Player);
        var returnLoc = DataStore.Get(ReturnLocationTable, Player.UID);
        if (returnLoc !== null)
        {
            Player.TeleportTo(returnLoc, false);
            DataStore.Remove(ReturnLocationTable, Player.UID);
        }
        Player.MessageFrom("TDM", "You left TDM.");
    }
}

function On_EntityDeployed(Player, Entity, ActualPlacer)
{
    if (players.indexOf(ActualPlacer) !== -1)
    {
        ActualPlacer.MessageFrom("TDM", "You can't build inside the arena.");
        Entity.Destroy();
    }
}

function On_EntityHurt(HurtEvent)
{
    if (HurtEvent.Attacker !== null && players.indexOf(HurtEvent.Attacker) !== -1 && HurtEvent.Entity !== null)
    {
        HurtEvent.Entity.Health = HurtEvent.Entity.MaxHealth;
        HurtEvent.DamageAmount = 0.0;
    }
}
```

### Python

```python
players = []
restrictedCommands = ["home", "tpa", "shop"]
InventoryTable = "TDMInventory"
ReturnLocationTable = "TDMReturnLocation"
lobby = Util.CreateVector(0, 0, 0)


class TDM:

    def SaveAndClearInventory(self, Player):
        DataStore.Add(InventoryTable, Player.UID, Player.Inventory.Items)
        Player.Inventory.ClearAll()

    def RestoreInventory(self, Player):
        saved = DataStore.Get(InventoryTable, Player.UID)
        if saved is None:
            return
        DataStore.Remove(InventoryTable, Player.UID)

    def On_Command(self, Player, cmd, args):
        if cmd != "tdm" or len(args) == 0:
            return

        if args[0] == "join":
            if Player in players:
                Player.MessageFrom("TDM", "You're already queued.")
                return
            DataStore.Add(ReturnLocationTable, Player.UID, Player.Location)
            self.SaveAndClearInventory(Player)
            for restricted in restrictedCommands:
                Player.RestrictCommand(restricted)
            players.append(Player)
            Player.TeleportTo(lobby, False)
            Player.MessageFrom("TDM", "You joined the TDM queue!")
        elif args[0] == "leave":
            if Player not in players:
                return
            players.remove(Player)
            for restricted in restrictedCommands:
                Player.UnRestrictCommand(restricted)
            self.RestoreInventory(Player)
            returnLoc = DataStore.Get(ReturnLocationTable, Player.UID)
            if returnLoc is not None:
                Player.TeleportTo(returnLoc, False)
                DataStore.Remove(ReturnLocationTable, Player.UID)
            Player.MessageFrom("TDM", "You left TDM.")

    def On_EntityDeployed(self, Player, Entity, ActualPlacer):
        if ActualPlacer in players:
            ActualPlacer.MessageFrom("TDM", "You can't build inside the arena.")
            Entity.Destroy()

    def On_EntityHurt(self, HurtEvent):
        if HurtEvent.Attacker is not None and HurtEvent.Attacker in players and HurtEvent.Entity is not None:
            HurtEvent.Entity.Health = HurtEvent.Entity.MaxHealth
            HurtEvent.DamageAmount = 0.0
```

### Lua

```lua
local players = {}
local restrictedCommands = { "home", "tpa", "shop" }
local InventoryTable = "TDMInventory"
local ReturnLocationTable = "TDMReturnLocation"
local lobby = Util.CreateVector(0, 0, 0)

local function IndexOf(list, value)
    for i, v in ipairs(list) do if v == value then return i end end
    return nil
end

local function SaveAndClearInventory(player)
    DataStore:Add(InventoryTable, player.UID, player.Inventory.Items)
    player.Inventory:ClearAll()
end

local function RestoreInventory(player)
    local saved = DataStore:Get(InventoryTable, player.UID)
    if saved == nil then return end
    DataStore:Remove(InventoryTable, player.UID)
end

function On_Command(player, cmd, args)
    if cmd ~= "tdm" or #args == 0 then return end

    if args[1] == "join" then
        if IndexOf(players, player) ~= nil then
            player:MessageFrom("TDM", "You're already queued.")
            return
        end
        DataStore:Add(ReturnLocationTable, player.UID, player.Location)
        SaveAndClearInventory(player)
        for _, restricted in ipairs(restrictedCommands) do player:RestrictCommand(restricted) end
        table.insert(players, player)
        player:TeleportTo(lobby, false)
        player:MessageFrom("TDM", "You joined the TDM queue!")
    elseif args[1] == "leave" then
        local idx = IndexOf(players, player)
        if idx == nil then return end
        table.remove(players, idx)
        for _, restricted in ipairs(restrictedCommands) do player:UnRestrictCommand(restricted) end
        RestoreInventory(player)
        local returnLoc = DataStore:Get(ReturnLocationTable, player.UID)
        if returnLoc ~= nil then
            player:TeleportTo(returnLoc, false)
            DataStore:Remove(ReturnLocationTable, player.UID)
        end
        player:MessageFrom("TDM", "You left TDM.")
    end
end

function On_EntityDeployed(player, entity, actualPlacer)
    if IndexOf(players, actualPlacer) ~= nil then
        actualPlacer:MessageFrom("TDM", "You can't build inside the arena.")
        entity:Destroy()
    end
end

function On_EntityHurt(hurtEvent)
    if hurtEvent.Attacker ~= nil and IndexOf(players, hurtEvent.Attacker) ~= nil and hurtEvent.Entity ~= nil then
        hurtEvent.Entity.Health = hurtEvent.Entity.MaxHealth
        hurtEvent.DamageAmount = 0.0
    end
end
```

### Finishing the unfinished part: teams + a RustBuster GUI lobby
The original never got past a single shared queue. The natural next step the issue description points at:
- Split `_players` into `Team1`/`Team2` lists (alternate assignment on join, or let players pick with
  `/tdm team 1`/`/tdm team 2`), each with its own spawn point (`Team1Position`/`Team2Position` in the original's
  ini, already present but unused by the queue logic).
- Replace the chat-based `/tdm join`/`team` flow with a **RustBuster GUI** panel sent to the client (see
  RustBuster's client-side UI/RPC system) showing live queue size per team and a join/leave/team-select button,
  instead of players needing to know and type exact commands.
- Use a locked arena `Zone3D` (see [`Classes/World.md`](../Classes/World.md#zone3d)) covering the whole arena
  footprint so `Zone3D.Contains(player)` can double as a hard "are you actually still inside the arena"
  safety check, independent of the `_players` list (catches edge cases like a player glitching/falling out of
  bounds).
- Once both teams have at least one player, start a round timer and declare a winner by team kill-count when it
  expires (or when one team is fully eliminated) - exactly the "lobby -> locked arena -> GUI-driven TDM" concept
  from the issue.

### Notes / what was trimmed
- The original restores the exact inventory (slots, armor, belt) saved on join using the same ini-backed
  save/restore pattern as [`LevelSystem`](LevelSystem.md)'s database, guards joins behind a cooldown-gated
  `/tdm announce`, force-starts once `MinimumPlayers` queue (countdown via `Plugin.CreateTimer`), and tracks
  round wins/money rewards per team - none of the team split was finished in the source, which is exactly the
  gap highlighted above.

See also: [`Hooks/Entities/On_EntityDeployed.md`](../Hooks/Entities/On_EntityDeployed.md) ·
[`Hooks/Entities/On_EntityHurt.md`](../Hooks/Entities/On_EntityHurt.md) · [`Classes/World.md`](../Classes/World.md)
