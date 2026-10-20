### Example plugin
`LevelSystem` - gathering XP/level progression

### Original
[`LevelSystem.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/LevelSystem/LevelSystem.py)
by BogdanWDK, tuned by DreTaX.

### Concept
Every player has a `Level` (1-25) and `Experience` counter, persisted in the plugin's own ini (acting as a tiny
database, one `Level`/`Experience` pair per SteamID). Each level requires a configured XP threshold
(`[Level] 1 = 150`, `2 = 300`, ...). Two things drive the system:
- `On_PlayerGathering` multiplies the amount of resource gathered by the player's current level (so a level-5
  player gathers 5x as much wood/stone per hit as a level-1 player) and adds a flat `XpRate` amount of XP on
  every gather.
- `/level stats` shows current level/XP/progress to the next level; `/level rankup` manually promotes the
  player once they've accumulated enough XP for the next level (it's an explicit action, not automatic, so
  players can choose *when* to bank their progress).

State is loaded into memory on `On_PlayerConnected` and flushed back to the ini on `On_PlayerDisconnected`/
`On_ServerSaved`/`On_PluginShutdown`, the same read-on-connect/write-on-disconnect pattern used by `TDM`'s
inventory swap and `Clans`' membership cache.

### C#

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Fougerite;

namespace LevelSystem
{
    public class LevelSystem : Module
    {
        public override string Name { get { return "LevelSystem"; } }
        public override string Author { get { return "BogdanWDK / DreTaX"; } }
        public override string Description { get { return "Gathering XP and level progression."; } }
        public override Version Version { get { return new Version(1, 1); } }

        private const int XpRate = 5;
        private readonly Dictionary<int, int> _xpForLevel = new Dictionary<int, int> { { 2, 300 }, { 3, 450 } };
        private readonly Dictionary<ulong, int> _levels = new Dictionary<ulong, int>();
        private readonly Dictionary<ulong, int> _xp = new Dictionary<ulong, int>();
        private IniParser _db;

        public override void Initialize()
        {
            string path = Path.Combine(ModuleFolder, "Database.ini");
            if (!File.Exists(path)) File.Create(path).Dispose();
            _db = new IniParser(path);

            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnPlayerGathering += OnPlayerGathering;
            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnPlayerGathering -= OnPlayerGathering;
            Hooks.OnCommand -= OnCommand;
        }

        private void OnPlayerConnected(Player player)
        {
            string lvl = _db.GetSetting("Level", player.SteamID);
            if (lvl != null)
            {
                _levels[player.UID] = int.Parse(lvl);
                _xp[player.UID] = int.Parse(_db.GetSetting("Experience", player.SteamID));
            }
            else
            {
                _levels[player.UID] = 1;
                _xp[player.UID] = 0;
                _db.AddSetting("Level", player.SteamID, "1");
                _db.AddSetting("Experience", player.SteamID, "0");
            }
        }

        private void OnPlayerDisconnected(Player player)
        {
            if (!_levels.ContainsKey(player.UID)) return;
            _db.SetSetting("Level", player.SteamID, _levels[player.UID].ToString());
            _db.SetSetting("Experience", player.SteamID, _xp[player.UID].ToString());
            _db.Save();
            _levels.Remove(player.UID);
            _xp.Remove(player.UID);
        }

        private void OnPlayerGathering(Player player, GatherEvent ge)
        {
            int level = _levels[player.UID];
            ge.Quantity = ge.Quantity * level;
            _xp[player.UID] += XpRate;
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd != "level") return;
            if (args.Length == 1 && args[0] == "stats")
            {
                int level = _levels[player.UID];
                int xp = _xp[player.UID];
                int next = _xpForLevel.ContainsKey(level + 1) ? _xpForLevel[level + 1] : -1;
                player.MessageFrom(Name, "Level " + level + " - " + xp + " XP" +
                    (next > 0 ? " (" + (next - xp) + " XP to next level)" : " (max level)"));
            }
            else if (args.Length == 1 && args[0] == "rankup")
            {
                int level = _levels[player.UID];
                if (!_xpForLevel.ContainsKey(level + 1))
                {
                    player.MessageFrom(Name, "You are already at the maximum level!");
                    return;
                }
                int required = _xpForLevel[level + 1];
                if (_xp[player.UID] < required)
                {
                    player.MessageFrom(Name, "Not enough XP to rank up yet.");
                    return;
                }
                _levels[player.UID] = level + 1;
                player.MessageFrom(Name, "Ranked up to level " + (level + 1) + "!");
            }
        }
    }
}
```

### JavaScript

```javascript
var XpRate = 5;
var xpForLevel = { 2: 300, 3: 450 };
var levels = {};
var xp = {};
var db;

function On_PluginInit()
{
    if (!Plugin.IniExists("Database")) Plugin.CreateIni("Database").Save();
    db = Plugin.GetIni("Database");
}

function On_PlayerConnected(Player)
{
    var lvl = db.GetSetting("Level", Player.SteamID);
    if (lvl !== null)
    {
        levels[Player.UID] = parseInt(lvl);
        xp[Player.UID] = parseInt(db.GetSetting("Experience", Player.SteamID));
    }
    else
    {
        levels[Player.UID] = 1;
        xp[Player.UID] = 0;
        db.AddSetting("Level", Player.SteamID, "1");
        db.AddSetting("Experience", Player.SteamID, "0");
    }
}

function On_PlayerDisconnected(Player)
{
    if (!(Player.UID in levels)) return;
    db.SetSetting("Level", Player.SteamID, levels[Player.UID].toString());
    db.SetSetting("Experience", Player.SteamID, xp[Player.UID].toString());
    db.Save();
    delete levels[Player.UID];
    delete xp[Player.UID];
}

function On_PlayerGathering(Player, GatherEvent)
{
    var level = levels[Player.UID];
    GatherEvent.Quantity = GatherEvent.Quantity * level;
    xp[Player.UID] += XpRate;
}

function On_Command(Player, cmd, args)
{
    if (cmd !== "level") return;
    if (args.length === 1 && args[0] === "stats")
    {
        var level = levels[Player.UID];
        var playerXp = xp[Player.UID];
        var next = xpForLevel[level + 1] || -1;
        Player.MessageFrom("LevelSystem", "Level " + level + " - " + playerXp + " XP" +
            (next > 0 ? " (" + (next - playerXp) + " XP to next level)" : " (max level)"));
    }
    else if (args.length === 1 && args[0] === "rankup")
    {
        var level = levels[Player.UID];
        if (!(level + 1 in xpForLevel))
        {
            Player.MessageFrom("LevelSystem", "You are already at the maximum level!");
            return;
        }
        var required = xpForLevel[level + 1];
        if (xp[Player.UID] < required)
        {
            Player.MessageFrom("LevelSystem", "Not enough XP to rank up yet.");
            return;
        }
        levels[Player.UID] = level + 1;
        Player.MessageFrom("LevelSystem", "Ranked up to level " + (level + 1) + "!");
    }
}
```

### Python

```python
XpRate = 5
xpForLevel = {2: 300, 3: 450}


class LevelSystem:

    levels = {}
    xp = {}
    db = None

    def On_PluginInit(self):
        if not Plugin.IniExists("Database"):
            Plugin.CreateIni("Database").Save()
        self.db = Plugin.GetIni("Database")

    def On_PlayerConnected(self, Player):
        lvl = self.db.GetSetting("Level", Player.SteamID)
        if lvl is not None:
            self.levels[Player.UID] = int(lvl)
            self.xp[Player.UID] = int(self.db.GetSetting("Experience", Player.SteamID))
        else:
            self.levels[Player.UID] = 1
            self.xp[Player.UID] = 0
            self.db.AddSetting("Level", Player.SteamID, "1")
            self.db.AddSetting("Experience", Player.SteamID, "0")

    def On_PlayerDisconnected(self, Player):
        if Player.UID not in self.levels:
            return
        self.db.SetSetting("Level", Player.SteamID, str(self.levels[Player.UID]))
        self.db.SetSetting("Experience", Player.SteamID, str(self.xp[Player.UID]))
        self.db.Save()
        self.levels.pop(Player.UID)
        self.xp.pop(Player.UID)

    def On_PlayerGathering(self, Player, GatherEvent):
        level = self.levels[Player.UID]
        GatherEvent.Quantity = GatherEvent.Quantity * level
        self.xp[Player.UID] += XpRate

    def On_Command(self, Player, cmd, args):
        if cmd != "level":
            return
        if len(args) == 1 and args[0] == "stats":
            level = self.levels[Player.UID]
            exp = self.xp[Player.UID]
            next_level = xpForLevel.get(level + 1, -1)
            if next_level > 0:
                Player.MessageFrom("LevelSystem", "Level " + str(level) + " - " + str(exp) +
                                   " XP (" + str(next_level - exp) + " XP to next level)")
            else:
                Player.MessageFrom("LevelSystem", "Level " + str(level) + " - " + str(exp) + " XP (max level)")
        elif len(args) == 1 and args[0] == "rankup":
            level = self.levels[Player.UID]
            if level + 1 not in xpForLevel:
                Player.MessageFrom("LevelSystem", "You are already at the maximum level!")
                return
            required = xpForLevel[level + 1]
            if self.xp[Player.UID] < required:
                Player.MessageFrom("LevelSystem", "Not enough XP to rank up yet.")
                return
            self.levels[Player.UID] = level + 1
            Player.MessageFrom("LevelSystem", "Ranked up to level " + str(level + 1) + "!")
```

### Lua

```lua
local XpRate = 5
local xpForLevel = { [2] = 300, [3] = 450 }
local levels = {}
local xp = {}
local db

function On_PluginInit()
    if not Plugin:IniExists("Database") then
        Plugin:CreateIni("Database"):Save()
    end
    db = Plugin:GetIni("Database")
end

function On_PlayerConnected(player)
    local lvl = db:GetSetting("Level", player.SteamID)
    if lvl ~= nil then
        levels[player.UID] = tonumber(lvl)
        xp[player.UID] = tonumber(db:GetSetting("Experience", player.SteamID))
    else
        levels[player.UID] = 1
        xp[player.UID] = 0
        db:AddSetting("Level", player.SteamID, "1")
        db:AddSetting("Experience", player.SteamID, "0")
    end
end

function On_PlayerDisconnected(player)
    if levels[player.UID] == nil then return end
    db:SetSetting("Level", player.SteamID, tostring(levels[player.UID]))
    db:SetSetting("Experience", player.SteamID, tostring(xp[player.UID]))
    db:Save()
    levels[player.UID] = nil
    xp[player.UID] = nil
end

function On_PlayerGathering(player, gatherEvent)
    local level = levels[player.UID]
    gatherEvent.Quantity = gatherEvent.Quantity * level
    xp[player.UID] = xp[player.UID] + XpRate
end

function On_Command(player, cmd, args)
    if cmd ~= "level" then return end
    if #args == 1 and args[1] == "stats" then
        local level = levels[player.UID]
        local playerXp = xp[player.UID]
        local nextLevel = xpForLevel[level + 1] or -1
        if nextLevel > 0 then
            player:MessageFrom("LevelSystem", "Level " .. level .. " - " .. playerXp ..
                " XP (" .. (nextLevel - playerXp) .. " XP to next level)")
        else
            player:MessageFrom("LevelSystem", "Level " .. level .. " - " .. playerXp .. " XP (max level)")
        end
    elseif #args == 1 and args[1] == "rankup" then
        local level = levels[player.UID]
        if xpForLevel[level + 1] == nil then
            player:MessageFrom("LevelSystem", "You are already at the maximum level!")
            return
        end
        local required = xpForLevel[level + 1]
        if xp[player.UID] < required then
            player:MessageFrom("LevelSystem", "Not enough XP to rank up yet.")
            return
        end
        levels[player.UID] = level + 1
        player:MessageFrom("LevelSystem", "Ranked up to level " .. (level + 1) .. "!")
    end
end
```

### Notes / what was trimmed
- The original supports 25 levels with a full XP curve and separate `MaxWoodLevel`/`MaxMetalLevel` caps (so the
  gather-rate multiplier stops scaling past a configured level for each resource type) - the table above is cut
  down to 3 levels to keep the example short; extending `_xpForLevel`/`xpForLevel` is all that's needed.
- The original also persists on `On_ServerSaved`/`On_PluginShutdown` in addition to disconnect, so progress
  isn't lost on a crash between disconnects - worth adding in a real implementation.

See also: [`Hooks/Player/On_PlayerGathering.md`](../Hooks/Player/On_PlayerGathering.md) ·
[`Classes/Config.md`](../Classes/Config.md)
