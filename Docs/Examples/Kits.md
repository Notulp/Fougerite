### Example plugin
`Kits` - config-driven item loadouts

### Original
[`Kits.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/Kits/Kits.py) by DreTaX.

### Concept
Each "kit" is its own ini file (`LoadOuts\<name>.ini`) describing what it gives and who can use it: an
`[Items]` section (`item = quantity[,slot]`), and flags like `AdminCanUse`/`ModeratorCanUse`/`NormalCanUse`,
`Cooldown` (ms), `MaxUses` (0 = unlimited), and `AutoGiveOnSpawn`. `On_PluginInit` scans the `LoadOuts` folder
and parses every kit file once into an in-memory store, so `/kit <name>` never touches disk on the hot path.

`/kit <name>` checks, in order: does the kit exist and is it enabled -> is this rank (`Normal`/`Moderator`/
`Admin`) allowed to use it -> has the per-player cooldown expired (tracked via `DataStore`, keyed per
kit+player) -> has the player hit `MaxUses` yet (also `DataStore`, incremented per use). If every check passes,
the items are handed out via `Inventory.AddItem`/`AddItemTo`. `On_PlayerSpawned` can additionally auto-give a
kit flagged `AutoGiveOnSpawn` (the original gates this on "does the player still have their default starter
items", to avoid re-granting a kit to someone who already has a full inventory).

### C#

```csharp
using System;
using System.Collections.Generic;
using Fougerite;

namespace Kits
{
    public class KitDefinition
    {
        public bool Enabled;
        public int CooldownMs;
        public int MaxUses;
        public bool AdminCanUse;
        public bool ModeratorCanUse;
        public bool NormalCanUse;
        public Dictionary<string, int> Items = new Dictionary<string, int>();
    }

    public class Kits : Module
    {
        public override string Name { get { return "Kits"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Config-driven item loadouts."; } }
        public override Version Version { get { return new Version(1, 3); } }

        private readonly Dictionary<string, KitDefinition> _kits = new Dictionary<string, KitDefinition>();

        public override void Initialize()
        {
            // In a real plugin: scan a LoadOuts\ subfolder and populate _kits per file, as the original does.
            KitDefinition starter = new KitDefinition { Enabled = true, CooldownMs = 3600000, MaxUses = 0,
                NormalCanUse = true, ModeratorCanUse = true, AdminCanUse = true };
            starter.Items["Rock"] = 1;
            starter.Items["Torch"] = 1;
            starter.Items["Bandage"] = 5;
            _kits["starter"] = starter;

            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
        }

        private bool CanUse(Player player, KitDefinition kit)
        {
            if (player.Admin) return kit.AdminCanUse;
            if (player.Moderator) return kit.ModeratorCanUse;
            return kit.NormalCanUse;
        }

        private void GiveKit(Player player, KitDefinition kit)
        {
            foreach (KeyValuePair<string, int> item in kit.Items)
            {
                player.Inventory.AddItem(item.Key, item.Value);
            }
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd != "kit" || args.Length != 1) return;
            string name = args[0].ToLower();
            if (!_kits.ContainsKey(name))
            {
                player.MessageFrom(Name, "Kit '" + name + "' not found.");
                return;
            }
            KitDefinition kit = _kits[name];
            if (!kit.Enabled)
            {
                player.MessageFrom(Name, "This kit has been disabled.");
                return;
            }
            if (!CanUse(player, kit))
            {
                player.MessageFrom(Name, "You can't use this kit.");
                return;
            }

            string cooldownKey = "KitCooldown" + name;
            object lastUse = DataStore.GetInstance().Get(cooldownKey, player.UID);
            if (kit.CooldownMs > 0 && lastUse != null &&
                Environment.TickCount - (int) lastUse < kit.CooldownMs)
            {
                int remaining = (kit.CooldownMs - (Environment.TickCount - (int) lastUse)) / 1000;
                player.MessageFrom(Name, "You have to wait " + remaining + " more second(s).");
                return;
            }

            if (kit.MaxUses > 0)
            {
                object uses = DataStore.GetInstance().Get("KitUses" + name, player.UID);
                int current = uses != null ? (int) uses : 0;
                if (current >= kit.MaxUses)
                {
                    player.MessageFrom(Name, "You've reached the maximum uses of this kit.");
                    return;
                }
                DataStore.GetInstance().Add("KitUses" + name, player.UID, current + 1);
            }

            DataStore.GetInstance().Add(cooldownKey, player.UID, Environment.TickCount);
            GiveKit(player, kit);
            player.MessageFrom(Name, "Kit '" + name + "' received!");
        }
    }
}
```

### JavaScript

```javascript
var kits = {
    "starter": {
        Enabled: true, CooldownMs: 3600000, MaxUses: 0,
        NormalCanUse: true, ModeratorCanUse: true, AdminCanUse: true,
        Items: { "Rock": 1, "Torch": 1, "Bandage": 5 }
    }
};

function CanUse(Player, kit)
{
    if (Player.Admin) return kit.AdminCanUse;
    if (Player.Moderator) return kit.ModeratorCanUse;
    return kit.NormalCanUse;
}

function GiveKit(Player, kit)
{
    for (var item in kit.Items)
    {
        Player.Inventory.AddItem(item, kit.Items[item]);
    }
}

function On_Command(Player, cmd, args)
{
    if (cmd !== "kit" || args.length !== 1) return;
    var name = args[0].toLowerCase();
    var kit = kits[name];
    if (!kit)
    {
        Player.MessageFrom("Kits", "Kit '" + name + "' not found.");
        return;
    }
    if (!kit.Enabled)
    {
        Player.MessageFrom("Kits", "This kit has been disabled.");
        return;
    }
    if (!CanUse(Player, kit))
    {
        Player.MessageFrom("Kits", "You can't use this kit.");
        return;
    }

    var cooldownKey = "KitCooldown" + name;
    var lastUse = DataStore.Get(cooldownKey, Player.UID);
    if (kit.CooldownMs > 0 && lastUse !== null && (System.Environment.TickCount - lastUse) < kit.CooldownMs)
    {
        var remaining = Math.floor((kit.CooldownMs - (System.Environment.TickCount - lastUse)) / 1000);
        Player.MessageFrom("Kits", "You have to wait " + remaining + " more second(s).");
        return;
    }

    if (kit.MaxUses > 0)
    {
        var uses = DataStore.Get("KitUses" + name, Player.UID) || 0;
        if (uses >= kit.MaxUses)
        {
            Player.MessageFrom("Kits", "You've reached the maximum uses of this kit.");
            return;
        }
        DataStore.Add("KitUses" + name, Player.UID, uses + 1);
    }

    DataStore.Add(cooldownKey, Player.UID, System.Environment.TickCount);
    GiveKit(Player, kit);
    Player.MessageFrom("Kits", "Kit '" + name + "' received!");
}
```

### Python

```python
import System

kits = {
    "starter": {
        "Enabled": True, "CooldownMs": 3600000, "MaxUses": 0,
        "NormalCanUse": True, "ModeratorCanUse": True, "AdminCanUse": True,
        "Items": {"Rock": 1, "Torch": 1, "Bandage": 5}
    }
}


class Kits:

    def CanUse(self, Player, kit):
        if Player.Admin:
            return kit["AdminCanUse"]
        if Player.Moderator:
            return kit["ModeratorCanUse"]
        return kit["NormalCanUse"]

    def GiveKit(self, Player, kit):
        for item, qty in kit["Items"].iteritems():
            Player.Inventory.AddItem(item, qty)

    def On_Command(self, Player, cmd, args):
        if cmd != "kit" or len(args) != 1:
            return
        name = args[0].lower()
        if name not in kits:
            Player.MessageFrom("Kits", "Kit '" + name + "' not found.")
            return
        kit = kits[name]
        if not kit["Enabled"]:
            Player.MessageFrom("Kits", "This kit has been disabled.")
            return
        if not self.CanUse(Player, kit):
            Player.MessageFrom("Kits", "You can't use this kit.")
            return

        cooldownKey = "KitCooldown" + name
        lastUse = DataStore.Get(cooldownKey, Player.UID)
        if kit["CooldownMs"] > 0 and lastUse is not None and \
                (System.Environment.TickCount - lastUse) < kit["CooldownMs"]:
            remaining = (kit["CooldownMs"] - (System.Environment.TickCount - lastUse)) / 1000
            Player.MessageFrom("Kits", "You have to wait " + str(remaining) + " more second(s).")
            return

        if kit["MaxUses"] > 0:
            uses = DataStore.Get("KitUses" + name, Player.UID) or 0
            if uses >= kit["MaxUses"]:
                Player.MessageFrom("Kits", "You've reached the maximum uses of this kit.")
                return
            DataStore.Add("KitUses" + name, Player.UID, uses + 1)

        DataStore.Add(cooldownKey, Player.UID, System.Environment.TickCount)
        self.GiveKit(Player, kit)
        Player.MessageFrom("Kits", "Kit '" + name + "' received!")
```

### Lua

```lua
local kits = {
    starter = {
        Enabled = true, CooldownMs = 3600000, MaxUses = 0,
        NormalCanUse = true, ModeratorCanUse = true, AdminCanUse = true,
        Items = { Rock = 1, Torch = 1, Bandage = 5 }
    }
}

local function CanUse(player, kit)
    if player.Admin then return kit.AdminCanUse end
    if player.Moderator then return kit.ModeratorCanUse end
    return kit.NormalCanUse
end

local function GiveKit(player, kit)
    for item, qty in pairs(kit.Items) do
        player.Inventory:AddItem(item, qty)
    end
end

function On_Command(player, cmd, args)
    if cmd ~= "kit" or #args ~= 1 then return end
    local name = args[1]:lower()
    local kit = kits[name]
    if kit == nil then
        player:MessageFrom("Kits", "Kit '" .. name .. "' not found.")
        return
    end
    if not kit.Enabled then
        player:MessageFrom("Kits", "This kit has been disabled.")
        return
    end
    if not CanUse(player, kit) then
        player:MessageFrom("Kits", "You can't use this kit.")
        return
    end

    local cooldownKey = "KitCooldown" .. name
    local lastUse = DataStore:Get(cooldownKey, player.UID)
    if kit.CooldownMs > 0 and lastUse ~= nil and (System.Environment.TickCount - lastUse) < kit.CooldownMs then
        local remaining = math.floor((kit.CooldownMs - (System.Environment.TickCount - lastUse)) / 1000)
        player:MessageFrom("Kits", "You have to wait " .. remaining .. " more second(s).")
        return
    end

    if kit.MaxUses > 0 then
        local uses = DataStore:Get("KitUses" .. name, player.UID) or 0
        if uses >= kit.MaxUses then
            player:MessageFrom("Kits", "You've reached the maximum uses of this kit.")
            return
        end
        DataStore:Add("KitUses" .. name, player.UID, uses + 1)
    end

    DataStore:Add(cooldownKey, player.UID, System.Environment.TickCount)
    GiveKit(player, kit)
    player:MessageFrom("Kits", "Kit '" .. name .. "' received!")
end
```

### Notes / what was trimmed
- The original stores each kit as its own ini file under `LoadOuts\`, supports placing an item into a specific
  inventory slot (`item = quantity,slot`), lets admins/moderators optionally bypass the cooldown/max-uses
  checks (`AdminCanBypassCooldown`, etc.), and supports `On_PlayerSpawned`-driven auto-give kits plus admin
  utility commands (`/reloadkits`, `/flushkit`, `/clearmaxuses`). The example above keeps a single hardcoded
  kit to demonstrate the enable/rank/cooldown/max-uses gating chain, which is the reusable part.

See also: [`Classes/Data.md`](../Classes/Data.md) · [`Classes/Inventory.md`](../Classes/Inventory.md) ·
[`Hooks/Player/On_PlayerSpawned.md`](../Hooks/Player/On_PlayerSpawned.md)
