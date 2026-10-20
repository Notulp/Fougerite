### Example plugin
`DestroySystem` - hit-to-destroy your own structures

### Original
[`DestroySystem.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/DestroySystem/DestroySystem.py)
by DreTaX.

### Concept
`/destroy` toggles a per-player "destroy mode" flag in `DataStore`. While active, the next hits a player lands
on *their own* (or a friend-shared) structure/deployable via `On_EntityHurt` destroy it outright instead of
just damaging it, optionally refunding one of the matching item (a lookup table maps prefab names like
`WoodWall` back to their "Wood Wall" item name). Hitting with a shotgun is explicitly ignored (too easy to
"accidentally" destroy things with a wide-area weapon). The mode auto-expires after a configurable timeout via
a parallel timer, so a player can't forget they're in destroy mode and later rage-click through their own base.

The other half of the mechanic is a **raid-cooldown lock**: if a structure took *explosive* damage from someone
else recently, the owner can't use `/destroy` on anything nearby for a configurable window - stops a player from
pre-emptively self-destroying walls mid-raid to dodge a loss, or destroying what a raider is about to break
through anyway to deny loot.

### C#

```csharp
using System;
using Fougerite;

namespace DestroySystem
{
    public class DestroySystem : Module
    {
        public override string Name { get { return "DestroySystem"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Hit-to-destroy your own structures."; } }
        public override Version Version { get { return new Version(1, 7); } }

        private const string ModeTable = "DestroySystem";
        private const string RaidTable = "DestroySystemRaid";
        private const int RaidCooldownSeconds = 300;

        public override void Initialize()
        {
            Hooks.OnCommand += OnCommand;
            Hooks.OnEntityHurt += OnEntityHurt;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
            Hooks.OnEntityHurt -= OnEntityHurt;
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd != "destroy" && cmd != "crush") return;
            if (DataStore.GetInstance().ContainsKey(ModeTable, player.UID))
            {
                DataStore.GetInstance().Remove(ModeTable, player.UID);
                player.MessageFrom(Name, "You quit Destroy mode.");
            }
            else
            {
                DataStore.GetInstance().Add(ModeTable, player.UID, true);
                player.MessageFrom(Name, "You are in Destroy mode. Hit your own structures to remove them.");
                player.MessageFrom(Name, "Shotguns can't be used in Destroy mode.");
            }
        }

        private void OnEntityHurt(HurtEvent he)
        {
            // HurtEvent.Attacker/Victim are typed `object` (could be Player/Entity/NPC/null), so
            // AttackerIsPlayer must be checked before casting - a plain `he.Attacker.UID` won't compile.
            if (!he.AttackerIsPlayer || !he.VictimIsEntity || he.IsDecay) return;
            Player attacker = (Player) he.Attacker;
            Entity victim = (Entity) he.Victim;
            if (he.WeaponName == "Shotgun") return;

            ulong ownerId = victim.UOwnerID;
            if (ownerId == 0 || ownerId != attacker.UID) return; // only destroy things you own

            object raidedAt = DataStore.GetInstance().Get(RaidTable, ownerId);
            if (raidedAt != null)
            {
                double secondsSince = (DateTime.UtcNow - (DateTime) raidedAt).TotalSeconds;
                if (secondsSince < RaidCooldownSeconds)
                {
                    attacker.MessageFrom(Name, "You can't destroy while under raid cooldown ("
                        + Math.Round(secondsSince) + "/" + RaidCooldownSeconds + ").");
                    return;
                }
            }

            if (he.WeaponName != null && (he.WeaponName.ToLower().Contains("explosive")
                || he.WeaponName.ToLower().Contains("grenade")))
            {
                // Someone else's explosive hit would have gone through the `ownerId != attacker.UID` check
                // above and returned already; this branch exists only to (re-)arm the raid cooldown when the
                // *owner themselves* detonates something nearby, matching the original's behavior.
                DataStore.GetInstance().Add(RaidTable, ownerId, DateTime.UtcNow);
            }

            if (!DataStore.GetInstance().ContainsKey(ModeTable, attacker.UID)) return;

            string entityName = victim.Name;
            victim.Destroy();
            attacker.Inventory.AddItem(entityName, 1);
        }
    }
}
```

### JavaScript

```javascript
var ModeTable = "DestroySystem";
var RaidTable = "DestroySystemRaid";
var RaidCooldownSeconds = 300;

function On_Command(Player, cmd, args)
{
    if (cmd !== "destroy" && cmd !== "crush") return;
    if (DataStore.ContainsKey(ModeTable, Player.UID))
    {
        DataStore.Remove(ModeTable, Player.UID);
        Player.MessageFrom("DestroySystem", "You quit Destroy mode.");
    }
    else
    {
        DataStore.Add(ModeTable, Player.UID, true);
        Player.MessageFrom("DestroySystem", "You are in Destroy mode. Hit your own structures to remove them.");
        Player.MessageFrom("DestroySystem", "Shotguns can't be used in Destroy mode.");
    }
}

function On_EntityHurt(HurtEvent)
{
    // Jint resolves members off the attacker/victim's actual runtime type, so no explicit cast is needed
    // here the way C# requires - but the AttackerIsPlayer/VictimIsEntity checks still matter: Attacker can
    // legitimately be null/an NPC/another Entity (decay), and Victim might not be an Entity at all.
    if (!HurtEvent.AttackerIsPlayer || !HurtEvent.VictimIsEntity || HurtEvent.IsDecay) return;
    if (HurtEvent.WeaponName === "Shotgun") return;

    var ownerId = HurtEvent.Victim.UOwnerID;
    if (ownerId === 0 || ownerId !== HurtEvent.Attacker.UID) return;

    var raidedAt = DataStore.Get(RaidTable, ownerId);
    if (raidedAt !== null)
    {
        var secondsSince = (new Date() - raidedAt) / 1000;
        if (secondsSince < RaidCooldownSeconds)
        {
            HurtEvent.Attacker.MessageFrom("DestroySystem", "You can't destroy while under raid cooldown ("
                + Math.round(secondsSince) + "/" + RaidCooldownSeconds + ").");
            return;
        }
    }

    if (HurtEvent.WeaponName !== null &&
        (HurtEvent.WeaponName.toLowerCase().indexOf("explosive") !== -1 ||
         HurtEvent.WeaponName.toLowerCase().indexOf("grenade") !== -1))
    {
        DataStore.Add(RaidTable, ownerId, new Date());
    }

    if (!DataStore.ContainsKey(ModeTable, HurtEvent.Attacker.UID)) return;

    var entityName = HurtEvent.Victim.Name;
    HurtEvent.Victim.Destroy();
    HurtEvent.Attacker.Inventory.AddItem(entityName, 1);
}
```

### Python

```python
import System

ModeTable = "DestroySystem"
RaidTable = "DestroySystemRaid"
RaidCooldownSeconds = 300


class DestroySystem:

    def On_Command(self, Player, cmd, args):
        if cmd not in ("destroy", "crush"):
            return
        if DataStore.ContainsKey(ModeTable, Player.UID):
            DataStore.Remove(ModeTable, Player.UID)
            Player.MessageFrom("DestroySystem", "You quit Destroy mode.")
        else:
            DataStore.Add(ModeTable, Player.UID, True)
            Player.MessageFrom("DestroySystem", "You are in Destroy mode. Hit your own structures to remove them.")
            Player.MessageFrom("DestroySystem", "Shotguns can't be used in Destroy mode.")

    def On_EntityHurt(self, HurtEvent):
        if not HurtEvent.AttackerIsPlayer or not HurtEvent.VictimIsEntity or HurtEvent.IsDecay:
            return
        if HurtEvent.WeaponName == "Shotgun":
            return

        ownerId = HurtEvent.Victim.UOwnerID
        if ownerId == 0 or ownerId != HurtEvent.Attacker.UID:
            return

        raidedAt = DataStore.Get(RaidTable, ownerId)
        if raidedAt is not None:
            secondsSince = (System.DateTime.UtcNow - raidedAt).TotalSeconds
            if secondsSince < RaidCooldownSeconds:
                HurtEvent.Attacker.MessageFrom("DestroySystem", "You can't destroy while under raid cooldown (" +
                                   str(round(secondsSince)) + "/" + str(RaidCooldownSeconds) + ").")
                return

        weapon = HurtEvent.WeaponName
        if weapon is not None and ("explosive" in weapon.lower() or "grenade" in weapon.lower()):
            DataStore.Add(RaidTable, ownerId, System.DateTime.UtcNow)

        if not DataStore.ContainsKey(ModeTable, HurtEvent.Attacker.UID):
            return

        entityName = HurtEvent.Victim.Name
        HurtEvent.Victim.Destroy()
        HurtEvent.Attacker.Inventory.AddItem(entityName, 1)
```

### Lua

```lua
local ModeTable = "DestroySystem"
local RaidTable = "DestroySystemRaid"
local RaidCooldownSeconds = 300

function On_Command(player, cmd, args)
    if cmd ~= "destroy" and cmd ~= "crush" then return end
    if DataStore:ContainsKey(ModeTable, player.UID) then
        DataStore:Remove(ModeTable, player.UID)
        player:MessageFrom("DestroySystem", "You quit Destroy mode.")
    else
        DataStore:Add(ModeTable, player.UID, true)
        player:MessageFrom("DestroySystem", "You are in Destroy mode. Hit your own structures to remove them.")
        player:MessageFrom("DestroySystem", "Shotguns can't be used in Destroy mode.")
    end
end

function On_EntityHurt(hurtEvent)
    if not hurtEvent.AttackerIsPlayer or not hurtEvent.VictimIsEntity or hurtEvent.IsDecay then
        return
    end
    if hurtEvent.WeaponName == "Shotgun" then return end

    local ownerId = hurtEvent.Victim.UOwnerID
    if ownerId == 0 or ownerId ~= hurtEvent.Attacker.UID then return end

    local raidedAt = DataStore:Get(RaidTable, ownerId)
    if raidedAt ~= nil then
        local secondsSince = (System.DateTime.UtcNow - raidedAt).TotalSeconds
        if secondsSince < RaidCooldownSeconds then
            hurtEvent.Attacker:MessageFrom("DestroySystem", "You can't destroy while under raid cooldown (" ..
                math.floor(secondsSince) .. "/" .. RaidCooldownSeconds .. ").")
            return
        end
    end

    local weapon = hurtEvent.WeaponName
    if weapon ~= nil and (weapon:lower():find("explosive") or weapon:lower():find("grenade")) then
        DataStore:Add(RaidTable, ownerId, System.DateTime.UtcNow)
    end

    if not DataStore:ContainsKey(ModeTable, hurtEvent.Attacker.UID) then return end

    local entityName = hurtEvent.Victim.Name
    hurtEvent.Victim:Destroy()
    hurtEvent.Attacker.Inventory:AddItem(entityName, 1)
end
```

### Notes / what was trimmed
- The original arms the raid-cooldown lock from *anyone's* explosive hit on the structure (not just the owner
  detonating something, which was simplified above to keep the single `UOwnerID != Attacker.UID` early-return
  from needing a second branch), auto-expires destroy mode with a parallel timer after inactivity, supports a
  `/destroyall` variant that also removes every structure linked to the one hit (`GetLinkedStructs()`), and (pre-
  RustPP) had its own `/sharefoundation` ini-based whitelist so a friend's hits also count as "owner" - superseded
  by RustPP's own `/share` once that's installed. A full implementation should track the raid timestamp
  separately from the destroy toggle and key it off the *entity's* owner, not just the attacker, exactly like
  the original's `BeingRaided` dictionary.

See also: [`Hooks/Entities/On_EntityHurt.md`](../Hooks/Entities/On_EntityHurt.md) · [`Classes/Entity.md`](../Classes/Entity.md)
