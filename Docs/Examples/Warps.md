### Example plugin
`Warps` - named teleport points

### Original
[`Warps.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/Warps/Warps.py) by DreTaX.

### Concept
A simple admin tool for defining named teleport destinations:
- `/setwarp <name> <onlyAdmin:true/false> <canModsUse:true/false>` - captures the caller's current position as
  a new warp. Admins can always set warps; moderators can too if `AllowModsSetWarp` is enabled.
- `/warp <name>` - teleports the caller to that warp, after checking the admin/moderator gate for that specific
  warp.
- `/deletewarp <name>` - removes a warp (same admin/moderator gate as setting one).
- `/warps` - lists every known warp name.

Warps are stored in the plugin's own ini (`Warps.ini`, section `Warps`, `name = x,y,z`), loaded once into an
in-memory dictionary on `On_PluginInit` and kept in sync on every add/delete, so normal `/warp` lookups never
touch disk. The original also supports an optional teleport delay (`EnableTeleportDelay`/`TeleportDelay`) using
a parallel timer, so a player can be interrupted (e.g. by taking damage, if you hook that too) before the
teleport actually happens - omitted below for brevity, but it's exactly the same pattern as the loadout cooldown
timers used in the [`Kits`](Kits.md) example.

### C#

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Fougerite;

namespace Warps
{
    public class Warps : Module
    {
        public override string Name { get { return "Warps"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Named teleport points."; } }
        public override Version Version { get { return new Version(1, 0); } }

        private readonly Dictionary<string, Vector3> _warps = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, bool> _adminOnly = new Dictionary<string, bool>();
        private IniParser _ini;

        public override void Initialize()
        {
            string path = System.IO.Path.Combine(ModuleFolder, "Warps.ini");
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.Create(path).Dispose();
            }
            _ini = new IniParser(path);

            foreach (string key in _ini.EnumSection("Warps"))
            {
                string[] parts = _ini.GetSetting("Warps", key).Split(',');
                Vector3 pos = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                _warps[key.ToLower()] = pos;
                string adminOnly = _ini.GetSetting("AdminWarps", key);
                _adminOnly[key.ToLower()] = adminOnly != null && bool.Parse(adminOnly);
            }

            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd == "setwarp" && player.Admin)
            {
                if (args.Length != 2)
                {
                    player.MessageFrom(Name, "Usage: /setwarp <name> <onlyAdmin:true/false>");
                    return;
                }
                string key = args[0].ToLower();
                bool adminOnly = bool.Parse(args[1]);
                _warps[key] = player.Location;
                _adminOnly[key] = adminOnly;
                _ini.AddSetting("Warps", args[0], player.X + "," + player.Y + "," + player.Z);
                _ini.AddSetting("AdminWarps", args[0], adminOnly.ToString());
                _ini.Save();
                player.MessageFrom(Name, "Warp '" + args[0] + "' set!");
            }
            else if (cmd == "warp")
            {
                if (args.Length == 0)
                {
                    player.MessageFrom(Name, "Usage: /warp <name>");
                    return;
                }
                string key = args[0].ToLower();
                if (!_warps.ContainsKey(key))
                {
                    player.MessageFrom(Name, "Couldn't find warp '" + args[0] + "'.");
                    return;
                }
                if (_adminOnly[key] && !player.Admin)
                {
                    player.MessageFrom(Name, "'" + args[0] + "' is only for admins!");
                    return;
                }
                player.TeleportTo(_warps[key]);
                player.MessageFrom(Name, "Teleported to '" + args[0] + "'.");
            }
            else if (cmd == "warps")
            {
                player.MessageFrom(Name, "Warps: " + string.Join(", ", new List<string>(_warps.Keys).ToArray()));
            }
        }
    }
}
```

### JavaScript

```javascript
function Warps()
{
    if (!Plugin.IniExists("Warps"))
    {
        Plugin.CreateIni("Warps").Save();
    }
    return Plugin.GetIni("Warps");
}

function On_PluginInit()
{
    globalThis.warps = {};
    globalThis.adminOnly = {};
    var ini = Warps();
    var keys = ini.EnumSection("Warps");
    for (var i = 0; i < keys.Count; i++)
    {
        var key = keys[i];
        var parts = ini.GetSetting("Warps", key).split(",");
        warps[key.toLowerCase()] = Util.CreateVector(parseFloat(parts[0]), parseFloat(parts[1]), parseFloat(parts[2]));
        var flag = ini.GetSetting("AdminWarps", key);
        adminOnly[key.toLowerCase()] = flag !== null && flag.toLowerCase() === "true";
    }
}

function On_Command(Player, cmd, args)
{
    var ini = Warps();
    if (cmd === "setwarp" && Player.Admin)
    {
        if (args.length !== 2)
        {
            Player.MessageFrom("Warps", "Usage: /setwarp <name> <onlyAdmin:true/false>");
            return;
        }
        var key = args[0].toLowerCase();
        var flag = args[1].toLowerCase() === "true";
        warps[key] = Player.Location;
        adminOnly[key] = flag;
        ini.AddSetting("Warps", args[0], Player.X + "," + Player.Y + "," + Player.Z);
        ini.AddSetting("AdminWarps", args[0], flag.toString());
        ini.Save();
        Player.MessageFrom("Warps", "Warp '" + args[0] + "' set!");
    }
    else if (cmd === "warp")
    {
        if (args.length === 0)
        {
            Player.MessageFrom("Warps", "Usage: /warp <name>");
            return;
        }
        var key = args[0].toLowerCase();
        if (!(key in warps))
        {
            Player.MessageFrom("Warps", "Couldn't find warp '" + args[0] + "'.");
            return;
        }
        if (adminOnly[key] && !Player.Admin)
        {
            Player.MessageFrom("Warps", "'" + args[0] + "' is only for admins!");
            return;
        }
        Player.TeleportTo(warps[key], false);
        Player.MessageFrom("Warps", "Teleported to '" + args[0] + "'.");
    }
    else if (cmd === "warps")
    {
        Player.MessageFrom("Warps", "Warps: " + Object.keys(warps).join(", "));
    }
}
```

### Python

```python
class Warps:

    def Warps(self):
        if not Plugin.IniExists("Warps"):
            Plugin.CreateIni("Warps").Save()
        return Plugin.GetIni("Warps")

    def On_PluginInit(self):
        self.warps = {}
        self.adminOnly = {}
        ini = self.Warps()
        for key in ini.EnumSection("Warps"):
            parts = ini.GetSetting("Warps", key).split(',')
            self.warps[key.lower()] = Util.CreateVector(float(parts[0]), float(parts[1]), float(parts[2]))
            flag = ini.GetSetting("AdminWarps", key)
            self.adminOnly[key.lower()] = flag is not None and flag.lower() == "true"

    def On_Command(self, Player, cmd, args):
        ini = self.Warps()
        if cmd == "setwarp" and Player.Admin:
            if len(args) != 2:
                Player.MessageFrom("Warps", "Usage: /setwarp <name> <onlyAdmin:true/false>")
                return
            key = args[0].lower()
            flag = args[1].lower() == "true"
            self.warps[key] = Player.Location
            self.adminOnly[key] = flag
            ini.AddSetting("Warps", args[0], str(Player.X) + "," + str(Player.Y) + "," + str(Player.Z))
            ini.AddSetting("AdminWarps", args[0], str(flag))
            ini.Save()
            Player.MessageFrom("Warps", "Warp '" + args[0] + "' set!")
        elif cmd == "warp":
            if len(args) == 0:
                Player.MessageFrom("Warps", "Usage: /warp <name>")
                return
            key = args[0].lower()
            if key not in self.warps:
                Player.MessageFrom("Warps", "Couldn't find warp '" + args[0] + "'.")
                return
            if self.adminOnly[key] and not Player.Admin:
                Player.MessageFrom("Warps", "'" + args[0] + "' is only for admins!")
                return
            Player.TeleportTo(self.warps[key], False)
            Player.MessageFrom("Warps", "Teleported to '" + args[0] + "'.")
        elif cmd == "warps":
            Player.MessageFrom("Warps", "Warps: " + ", ".join(self.warps.keys()))
```

### Lua

```lua
local warps = {}
local adminOnly = {}

local function Warps()
    if not Plugin.IniExists("Warps") then
        Plugin.CreateIni("Warps"):Save()
    end
    return Plugin.GetIni("Warps")
end

function On_PluginInit()
    local ini = Warps()
    for _, key in ipairs(ini:EnumSection("Warps")) do
        local parts = {}
        for part in ini:GetSetting("Warps", key):gmatch("[^,]+") do table.insert(parts, tonumber(part)) end
        warps[key:lower()] = Util.CreateVector(parts[1], parts[2], parts[3])
        local flag = ini:GetSetting("AdminWarps", key)
        adminOnly[key:lower()] = flag ~= nil and flag:lower() == "true"
    end
end

function On_Command(player, cmd, args)
    local ini = Warps()
    if cmd == "setwarp" and player.Admin then
        if #args ~= 2 then
            player:MessageFrom("Warps", "Usage: /setwarp <name> <onlyAdmin:true/false>")
            return
        end
        local key = args[1]:lower()
        local flag = args[2]:lower() == "true"
        warps[key] = player.Location
        adminOnly[key] = flag
        ini:AddSetting("Warps", args[1], player.X .. "," .. player.Y .. "," .. player.Z)
        ini:AddSetting("AdminWarps", args[1], tostring(flag))
        ini:Save()
        player:MessageFrom("Warps", "Warp '" .. args[1] .. "' set!")
    elseif cmd == "warp" then
        if #args == 0 then
            player:MessageFrom("Warps", "Usage: /warp <name>")
            return
        end
        local key = args[1]:lower()
        if warps[key] == nil then
            player:MessageFrom("Warps", "Couldn't find warp '" .. args[1] .. "'.")
            return
        end
        if adminOnly[key] and not player.Admin then
            player:MessageFrom("Warps", "'" .. args[1] .. "' is only for admins!")
            return
        end
        player:TeleportTo(warps[key], false)
        player:MessageFrom("Warps", "Teleported to '" .. args[1] .. "'.")
    elseif cmd == "warps" then
        local names = {}
        for name, _ in pairs(warps) do table.insert(names, name) end
        player:MessageFrom("Warps", "Warps: " .. table.concat(names, ", "))
    end
end
```

### Notes / what was trimmed
- The original also supports `/deletewarp`, a `CanModsUse`/`AllowModsSetWarp`/`AllowModsDeleteWarp` set of
  moderator permissions in addition to the admin/public split shown above, and an optional
  `EnableTeleportDelay`/`TeleportDelay` using `Plugin.CreateParallelTimer` so the teleport fires a few seconds
  after the command instead of instantly (giving e.g. an anti-glitch/anti-combat-log check a window to cancel
  it) - all mechanically identical patterns to what's already shown elsewhere in these examples.

See also: [`Classes/Config.md`](../Classes/Config.md) (ini usage pattern) · [`Hooks/Player/On_Command.md`](../Hooks/Player/On_Command.md)
