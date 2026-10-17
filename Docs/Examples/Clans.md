### Example plugin
`Clans` - role-based clans with opt-in friendly fire

### Original
[`Clans.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/Clans/Clans.py) by DreTaX.

### Concept
A clan is a named group with four roles stored across separate ini sections per clan (`Owner`, `CoOwner`,
`Officer`, `Member`) plus a global reverse-lookup (`ClanMembers`/`ClanOfficers`/... keyed by SteamID -> clan
name) so "what clan is this player in" is an O(1) lookup instead of scanning every clan. Creating a clan can
cost money (`Cost` setting, meant to integrate with an economy plugin like [`iConomy`](iConomy.md)); joining
requires an invite/approval flow in the original (omitted below).

The interesting mechanic is **friendly fire**: each clan has a `FriendlyFire` flag (cached in memory, loaded
once at startup from `ClanInfo.ini`) that an owner can toggle. `On_PlayerHurt` checks whether attacker and
victim are in the *same* clan, and if so, zeroes the damage unless that clan has opted into friendly fire -
letting a clan practice/duel internally if they want to, while defaulting to "can't accidentally team-kill your
own clanmate".

### C#

```csharp
using System;
using System.Collections.Generic;
using Fougerite;

namespace Clans
{
    public class Clans : Module
    {
        public override string Name { get { return "Clans"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Role-based clans with opt-in friendly fire."; } }
        public override Version Version { get { return new Version(1, 2); } }

        // SteamID -> clan name
        private readonly Dictionary<string, string> _memberOf = new Dictionary<string, string>();
        // clan name -> allows friendly fire?
        private readonly Dictionary<string, bool> _friendlyFire = new Dictionary<string, bool>();

        public override void Initialize()
        {
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnCommand -= OnCommand;
        }

        private string GetClanOf(string steamId)
        {
            return _memberOf.ContainsKey(steamId) ? _memberOf[steamId] : null;
        }

        private void OnPlayerHurt(HurtEvent he)
        {
            if (!he.AttackerIsPlayer || he.Attacker == null || he.Victim == null) return;
            if (he.Attacker.UID == he.Victim.UID) return;

            string attackerClan = GetClanOf(he.Attacker.SteamID);
            string victimClan = GetClanOf(he.Victim.SteamID);
            if (attackerClan == null || attackerClan != victimClan) return;

            bool allowsFf = _friendlyFire.ContainsKey(attackerClan) && _friendlyFire[attackerClan];
            if (!allowsFf)
            {
                he.DamageAmount = 0f;
                he.Attacker.MessageFrom(Name, "Friendly fire is disabled for your clan.");
            }
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd == "clancreate" && args.Length == 1)
            {
                string clanName = args[0];
                if (_friendlyFire.ContainsKey(clanName))
                {
                    player.MessageFrom(Name, "A clan with that name already exists.");
                    return;
                }
                _friendlyFire[clanName] = false;
                _memberOf[player.SteamID] = clanName;
                player.MessageFrom(Name, "Clan '" + clanName + "' created! You are the owner.");
            }
            else if (cmd == "clanff" && args.Length == 0)
            {
                string clan = GetClanOf(player.SteamID);
                if (clan == null)
                {
                    player.MessageFrom(Name, "You are not in a clan.");
                    return;
                }
                _friendlyFire[clan] = !_friendlyFire[clan];
                player.MessageFrom(Name, "Friendly fire is now " + (_friendlyFire[clan] ? "ON" : "OFF") +
                    " for your clan.");
            }
        }
    }
}
```

### JavaScript

```javascript
var memberOf = {};       // SteamID -> clan name
var friendlyFire = {};   // clan name -> allows friendly fire?

function GetClanOf(steamId)
{
    return memberOf[steamId] || null;
}

function On_PlayerHurt(HurtEvent)
{
    if (!HurtEvent.AttackerIsPlayer || HurtEvent.Attacker === null || HurtEvent.Victim === null) return;
    if (HurtEvent.Attacker.UID === HurtEvent.Victim.UID) return;

    var attackerClan = GetClanOf(HurtEvent.Attacker.SteamID);
    var victimClan = GetClanOf(HurtEvent.Victim.SteamID);
    if (attackerClan === null || attackerClan !== victimClan) return;

    if (!friendlyFire[attackerClan])
    {
        HurtEvent.DamageAmount = 0.0;
        HurtEvent.Attacker.MessageFrom("Clans", "Friendly fire is disabled for your clan.");
    }
}

function On_Command(Player, cmd, args)
{
    if (cmd === "clancreate" && args.length === 1)
    {
        var clanName = args[0];
        if (clanName in friendlyFire)
        {
            Player.MessageFrom("Clans", "A clan with that name already exists.");
            return;
        }
        friendlyFire[clanName] = false;
        memberOf[Player.SteamID] = clanName;
        Player.MessageFrom("Clans", "Clan '" + clanName + "' created! You are the owner.");
    }
    else if (cmd === "clanff" && args.length === 0)
    {
        var clan = GetClanOf(Player.SteamID);
        if (clan === null)
        {
            Player.MessageFrom("Clans", "You are not in a clan.");
            return;
        }
        friendlyFire[clan] = !friendlyFire[clan];
        Player.MessageFrom("Clans", "Friendly fire is now " + (friendlyFire[clan] ? "ON" : "OFF") + " for your clan.");
    }
}
```

### Python

```python
memberOf = {}
friendlyFire = {}


class Clans:

    def GetClanOf(self, steamId):
        return memberOf.get(steamId)

    def On_PlayerHurt(self, HurtEvent):
        if not HurtEvent.AttackerIsPlayer or HurtEvent.Attacker is None or HurtEvent.Victim is None:
            return
        if HurtEvent.Attacker.UID == HurtEvent.Victim.UID:
            return

        attackerClan = self.GetClanOf(HurtEvent.Attacker.SteamID)
        victimClan = self.GetClanOf(HurtEvent.Victim.SteamID)
        if attackerClan is None or attackerClan != victimClan:
            return

        if not friendlyFire.get(attackerClan, False):
            HurtEvent.DamageAmount = 0.0
            HurtEvent.Attacker.MessageFrom("Clans", "Friendly fire is disabled for your clan.")

    def On_Command(self, Player, cmd, args):
        if cmd == "clancreate" and len(args) == 1:
            clanName = args[0]
            if clanName in friendlyFire:
                Player.MessageFrom("Clans", "A clan with that name already exists.")
                return
            friendlyFire[clanName] = False
            memberOf[Player.SteamID] = clanName
            Player.MessageFrom("Clans", "Clan '" + clanName + "' created! You are the owner.")
        elif cmd == "clanff" and len(args) == 0:
            clan = self.GetClanOf(Player.SteamID)
            if clan is None:
                Player.MessageFrom("Clans", "You are not in a clan.")
                return
            friendlyFire[clan] = not friendlyFire[clan]
            state = "ON" if friendlyFire[clan] else "OFF"
            Player.MessageFrom("Clans", "Friendly fire is now " + state + " for your clan.")
```

### Lua

```lua
local memberOf = {}
local friendlyFire = {}

local function GetClanOf(steamId)
    return memberOf[steamId]
end

function On_PlayerHurt(hurtEvent)
    if not hurtEvent.AttackerIsPlayer or hurtEvent.Attacker == nil or hurtEvent.Victim == nil then return end
    if hurtEvent.Attacker.UID == hurtEvent.Victim.UID then return end

    local attackerClan = GetClanOf(hurtEvent.Attacker.SteamID)
    local victimClan = GetClanOf(hurtEvent.Victim.SteamID)
    if attackerClan == nil or attackerClan ~= victimClan then return end

    if not friendlyFire[attackerClan] then
        hurtEvent.DamageAmount = 0.0
        hurtEvent.Attacker:MessageFrom("Clans", "Friendly fire is disabled for your clan.")
    end
end

function On_Command(player, cmd, args)
    if cmd == "clancreate" and #args == 1 then
        local clanName = args[1]
        if friendlyFire[clanName] ~= nil then
            player:MessageFrom("Clans", "A clan with that name already exists.")
            return
        end
        friendlyFire[clanName] = false
        memberOf[player.SteamID] = clanName
        player:MessageFrom("Clans", "Clan '" .. clanName .. "' created! You are the owner.")
    elseif cmd == "clanff" and #args == 0 then
        local clan = GetClanOf(player.SteamID)
        if clan == nil then
            player:MessageFrom("Clans", "You are not in a clan.")
            return
        end
        friendlyFire[clan] = not friendlyFire[clan]
        local state = friendlyFire[clan] and "ON" or "OFF"
        player:MessageFrom("Clans", "Friendly fire is now " .. state .. " for your clan.")
    end
end
```

### Notes / what was trimmed
- The original persists everything to ini files (`Clans.ini`/`ClanInfo.ini`) instead of plain in-memory
  dictionaries, has four distinct roles (Owner/CoOwner/Officer/Member) with different permission levels instead
  of a single flat membership, supports an invite/accept flow and a per-clan member limit, and can charge
  `Cost` money to create a clan. The friendly-fire gate shown above is the core, reusable mechanic; swapping the
  in-memory dictionaries for an `IniParser` (as in the [`Warps`](Warps.md) example) is all that's needed to
  persist it across restarts.

See also: [`Hooks/Player/On_PlayerHurt.md`](../Hooks/Player/On_PlayerHurt.md) · [`Classes/Config.md`](../Classes/Config.md)
