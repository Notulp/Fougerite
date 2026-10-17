### Example plugin
`BambeeProtector` - new-player kill protection

### Original
[`BambeeProtector.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/BambeeProtector/BambeeProtector.py)
by DreTaX.

### Concept
When a player connects, they're put into a temporary "protected" state for a configurable number of minutes
(`Cooldown` in the plugin's own config). While protected:
- Nobody can deal PvP damage to them, and they can't deal PvP damage to anyone else either (both directions are
  blocked, so protection can't be used to farm free kills).
- Optionally (`ProtectFromNpcs`), NPC/animal damage against them is blocked too.
- Damage from explosives/grenades while protected is blocked outright, regardless of attacker.

The protection timer is tracked per-SteamID with `DataStore` + `System.Environment.TickCount`, so it survives
reconnects within the same server session (the clock keeps running, it doesn't reset on relog) - and a player
can opt out early with `/protectionoff`. An admin/moderator can wipe everyone's protection state with
`/resetprotection`.

The real plugin hooks `On_PlayerConnected`, `On_PlayerDisconnected`, `On_PlayerHurt`, `On_EntityHurt` (only for
the explosive-damage check) and `On_Command`. The example below reimplements all of that except the NPC-damage
toggle and the explosive-on-entity check, to keep the snippet focused on the PvP protection mechanic itself.

### C#

```csharp
using System;
using Fougerite;

namespace BambeeProtector
{
    public class BambeeProtector : Module
    {
        public override string Name { get { return "BambeeProtector"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Temporary PvP protection for new players."; } }
        public override Version Version { get { return new Version(1, 0); } }

        private const string Table = "Bambee";
        private const int CooldownMs = 60 * 60000; // 60 minutes

        public override void Initialize()
        {
            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnCommand -= OnCommand;
        }

        private bool IsProtected(ulong uid)
        {
            object raw = DataStore.GetInstance().Get(Table, uid);
            if (raw == null)
            {
                // First time we see this player this session: grant a fresh window.
                DataStore.GetInstance().Add(Table, uid, Environment.TickCount);
                return true;
            }
            int startedAt = (int) raw;
            return Environment.TickCount - startedAt < CooldownMs;
        }

        private void OnPlayerConnected(Player player)
        {
            if (IsProtected(player.UID))
            {
                player.MessageFrom(Name, "You are a new player! You have kill protection for a while.");
                player.MessageFrom(Name, "You can disable this early with /protectionoff.");
            }
        }

        private void OnPlayerHurt(HurtEvent he)
        {
            if (he.Attacker == null || he.Victim == null || !he.AttackerIsPlayer)
            {
                return;
            }
            if (IsProtected(he.Victim.UID))
            {
                he.DamageAmount = 0f;
                he.Attacker.MessageFrom(Name, he.Victim.Name + " is under new-player protection.");
                return;
            }
            if (IsProtected(he.Attacker.UID))
            {
                he.DamageAmount = 0f;
                he.Attacker.MessageFrom(Name, "You can't attack anyone while you are under protection.");
            }
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd == "protectionoff")
            {
                // Writing anything other than "still within the window" effectively ends it early;
                // simplest is to push the start time back far enough that the cooldown is already over.
                DataStore.GetInstance().Add(Table, player.UID, Environment.TickCount - CooldownMs);
                player.MessageFrom(Name, "Protection disabled.");
            }
            else if (cmd == "resetprotection" && player.Admin)
            {
                DataStore.GetInstance().Flush(Table);
                player.MessageFrom(Name, "Flushed every player's protection state.");
            }
        }
    }
}
```

### JavaScript

```javascript
var Table = "Bambee";
var CooldownMs = 60 * 60000;

function IsProtected(uid)
{
    var raw = DataStore.Get(Table, uid);
    if (raw === null)
    {
        DataStore.Add(Table, uid, System.Environment.TickCount);
        return true;
    }
    return (System.Environment.TickCount - raw) < CooldownMs;
}

function On_PlayerConnected(Player)
{
    if (IsProtected(Player.UID))
    {
        Player.MessageFrom("BambeeProtector", "You are a new player! You have kill protection for a while.");
        Player.MessageFrom("BambeeProtector", "You can disable this early with /protectionoff.");
    }
}

function On_PlayerHurt(HurtEvent)
{
    if (HurtEvent.Attacker === null || HurtEvent.Victim === null || !HurtEvent.AttackerIsPlayer) return;
    if (IsProtected(HurtEvent.Victim.UID))
    {
        HurtEvent.DamageAmount = 0.0;
        HurtEvent.Attacker.MessageFrom("BambeeProtector", HurtEvent.Victim.Name + " is under new-player protection.");
        return;
    }
    if (IsProtected(HurtEvent.Attacker.UID))
    {
        HurtEvent.DamageAmount = 0.0;
        HurtEvent.Attacker.MessageFrom("BambeeProtector", "You can't attack anyone while you are under protection.");
    }
}

function On_Command(Player, cmd, args)
{
    if (cmd === "protectionoff")
    {
        DataStore.Add(Table, Player.UID, System.Environment.TickCount - CooldownMs);
        Player.MessageFrom("BambeeProtector", "Protection disabled.");
    }
    else if (cmd === "resetprotection" && Player.Admin)
    {
        DataStore.Flush(Table);
        Player.MessageFrom("BambeeProtector", "Flushed every player's protection state.");
    }
}
```

### Python

```python
import System

Table = "Bambee"
CooldownMs = 60 * 60000


class BambeeProtector:

    def IsProtected(self, uid):
        raw = DataStore.Get(Table, uid)
        if raw is None:
            DataStore.Add(Table, uid, System.Environment.TickCount)
            return True
        return (System.Environment.TickCount - raw) < CooldownMs

    def On_PlayerConnected(self, Player):
        if self.IsProtected(Player.UID):
            Player.MessageFrom("BambeeProtector", "You are a new player! You have kill protection for a while.")
            Player.MessageFrom("BambeeProtector", "You can disable this early with /protectionoff.")

    def On_PlayerHurt(self, HurtEvent):
        if HurtEvent.Attacker is None or HurtEvent.Victim is None or not HurtEvent.AttackerIsPlayer:
            return
        if self.IsProtected(HurtEvent.Victim.UID):
            HurtEvent.DamageAmount = 0.0
            HurtEvent.Attacker.MessageFrom("BambeeProtector", HurtEvent.Victim.Name + " is under new-player protection.")
            return
        if self.IsProtected(HurtEvent.Attacker.UID):
            HurtEvent.DamageAmount = 0.0
            HurtEvent.Attacker.MessageFrom("BambeeProtector", "You can't attack anyone while you are under protection.")

    def On_Command(self, Player, cmd, args):
        if cmd == "protectionoff":
            DataStore.Add(Table, Player.UID, System.Environment.TickCount - CooldownMs)
            Player.MessageFrom("BambeeProtector", "Protection disabled.")
        elif cmd == "resetprotection" and Player.Admin:
            DataStore.Flush(Table)
            Player.MessageFrom("BambeeProtector", "Flushed every player's protection state.")
```

### Lua

```lua
local Table = "Bambee"
local CooldownMs = 60 * 60000

local function IsProtected(uid)
    local raw = DataStore:Get(Table, uid)
    if raw == nil then
        DataStore:Add(Table, uid, System.Environment.TickCount)
        return true
    end
    return (System.Environment.TickCount - raw) < CooldownMs
end

function On_PlayerConnected(player)
    if IsProtected(player.UID) then
        player:MessageFrom("BambeeProtector", "You are a new player! You have kill protection for a while.")
        player:MessageFrom("BambeeProtector", "You can disable this early with /protectionoff.")
    end
end

function On_PlayerHurt(hurtEvent)
    if hurtEvent.Attacker == nil or hurtEvent.Victim == nil or not hurtEvent.AttackerIsPlayer then return end
    if IsProtected(hurtEvent.Victim.UID) then
        hurtEvent.DamageAmount = 0.0
        hurtEvent.Attacker:MessageFrom("BambeeProtector", hurtEvent.Victim.Name .. " is under new-player protection.")
        return
    end
    if IsProtected(hurtEvent.Attacker.UID) then
        hurtEvent.DamageAmount = 0.0
        hurtEvent.Attacker:MessageFrom("BambeeProtector", "You can't attack anyone while you are under protection.")
    end
end

function On_Command(player, cmd, args)
    if cmd == "protectionoff" then
        DataStore:Add(Table, player.UID, System.Environment.TickCount - CooldownMs)
        player:MessageFrom("BambeeProtector", "Protection disabled.")
    elseif cmd == "resetprotection" and player.Admin then
        DataStore:Flush(Table)
        player:MessageFrom("BambeeProtector", "Flushed every player's protection state.")
    end
end
```

### Notes / what was trimmed
- The original also blocks damage from NPCs/animals (config toggle `ProtectFromNpcs`) and specifically zeroes
  out explosive/grenade damage dealt *by* a protected player against entities (`On_EntityHurt`) - omitted here
  to keep the example to the core PvP-protection mechanic; both would hook the same `IsProtected` check.
- The original keeps an in-memory `ProtectedPlayers` list in addition to `DataStore`, purely as a fast-path
  membership check; `DataStore` alone (as above) is simpler and is already an O(1) dictionary lookup.

See also: [`Classes/Data.md`](../Classes/Data.md) · [`Hooks/Player/On_PlayerHurt.md`](../Hooks/Player/On_PlayerHurt.md) ·
[`Hooks/Player/On_Command.md`](../Hooks/Player/On_Command.md)
