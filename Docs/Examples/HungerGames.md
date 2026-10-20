### Example plugin
`HungerGames` - battle royale with a shrinking safe zone

### Original
[`HungerGames.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/HungerGames/HungerGames.py)
by DreTaX.

### Concept
HungerGames reuses the same join/queue/inventory-swap/arena-lockdown skeleton as [`TDM`](TeamDeathmatch.md)
(same `On_EntityDeployed`/`On_EntityHurt` arena protections, same save-inventory-on-join), but it's
free-for-all instead of team-based, and adds the mechanic that actually makes it a "battle royale": a shrinking
**radiation zone**.

Once enough players have joined (`CRad`), a repeating timer starts, and every tick:
- It computes the current safe radius around a fixed center point, shrinking it by a configured amount
  (`RadME` meters) every interval, down to a configured minimum (`RadMDist`) - exactly a shrinking "circle"
  battle-royale zone.
- Every player further from the center than the current safe radius takes radiation damage (`RadR`), with a
  chance to also start bleeding (`BleedChance`) or take extra random damage (`DamageChance`/`DamageRandom`) the
  further outside the zone they are, and can even have their legs broken past a configured radiation threshold
  (`BreakAtXRad`) - all designed to make "just wait it out at the edge" actively punishing, forcing players
  toward the center and each other over time.
- A top-kill list (`TopListC`) is announced at the end, optionally with money/item rewards.

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

        private readonly List<Player> _players = new List<Player>();
        private readonly Vector3 _center = new Vector3(0, 0, 0);
        private float _safeRadius = 310f;
        private const float ShrinkPerTick = 40f;
        private const float MinRadius = 95f;
        private const float RadiationDamage = 65f;
        private bool _active;

        public override void Initialize()
        {
            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
            KillTimer("HGRadiation");
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd == "hg" && args.Length == 1 && args[0] == "start" && player.Admin && !_active)
            {
                _active = true;
                _safeRadius = 310f;
                // Fires every 6 seconds on the main thread - safe to touch World/Player directly.
                CreateTimer("HGRadiation", 6000, RadiationTickCallback, true);
                player.MessageFrom(Name, "HungerGames started! The safe zone will begin shrinking.");
            }
        }

        public void RadiationTickCallback(TimedEvent te)
        {
            if (_safeRadius > MinRadius)
            {
                _safeRadius -= ShrinkPerTick;
            }

            foreach (Player player in _players)
            {
                if (!player.IsOnline) continue;
                float distance = Vector3.Distance(player.Location, _center);
                if (distance > _safeRadius)
                {
                    player.Health -= RadiationDamage * ((distance - _safeRadius) / 100f + 1f);
                    player.MessageFrom(Name, "You are outside the safe zone! Move towards the center!");
                }
            }
        }
    }
}
```

### JavaScript

```javascript
var players = [];
var center = new UnityEngine.Vector3(0, 0, 0);
var safeRadius = 310.0;
var ShrinkPerTick = 40.0;
var MinRadius = 95.0;
var RadiationDamage = 65.0;
var active = false;

function On_Command(Player, cmd, args)
{
    if (cmd === "hg" && args.length === 1 && args[0] === "start" && Player.Admin && !active)
    {
        active = true;
        safeRadius = 310.0;
        Plugin.CreateTimer("HGRadiation", 6000, true).Start();
        Player.MessageFrom("HungerGames", "HungerGames started! The safe zone will begin shrinking.");
    }
}

function HGRadiationCallback(ATimedEvent)
{
    if (safeRadius > MinRadius)
    {
        safeRadius -= ShrinkPerTick;
    }

    for (var i = 0; i < players.length; i++)
    {
        var player = players[i];
        if (!player.IsOnline) continue;
        var distance = Util.GetVectorsDistance(player.Location, center);
        if (distance > safeRadius)
        {
            player.Health -= RadiationDamage * ((distance - safeRadius) / 100.0 + 1.0);
            player.MessageFrom("HungerGames", "You are outside the safe zone! Move towards the center!");
        }
    }
}
```

### Python

```python
players = []
center = UnityEngine.Vector3(0, 0, 0)
safeRadius = 310.0
ShrinkPerTick = 40.0
MinRadius = 95.0
RadiationDamage = 65.0
active = False


class HungerGames:

    def On_Command(self, Player, cmd, args):
        global active, safeRadius
        if cmd == "hg" and len(args) == 1 and args[0] == "start" and Player.Admin and not active:
            active = True
            safeRadius = 310.0
            Plugin.CreateTimer("HGRadiation", 6000, True).Start()
            Player.MessageFrom("HungerGames", "HungerGames started! The safe zone will begin shrinking.")

    def HGRadiationCallback(self, ATimedEvent):
        global safeRadius
        if safeRadius > MinRadius:
            safeRadius -= ShrinkPerTick

        for player in players:
            if not player.IsOnline:
                continue
            distance = Util.GetVectorsDistance(player.Location, center)
            if distance > safeRadius:
                player.Health -= RadiationDamage * ((distance - safeRadius) / 100.0 + 1.0)
                player.MessageFrom("HungerGames", "You are outside the safe zone! Move towards the center!")
```

### Lua

```lua
local players = {}
local center = UnityEngine.Vector3.__new(0, 0, 0)
local safeRadius = 310.0
local ShrinkPerTick = 40.0
local MinRadius = 95.0
local RadiationDamage = 65.0
local active = false

function On_Command(player, cmd, args)
    if cmd == "hg" and #args == 1 and args[1] == "start" and player.Admin and not active then
        active = true
        safeRadius = 310.0
        Plugin:CreateTimer("HGRadiation", 6000, true):Start()
        player:MessageFrom("HungerGames", "HungerGames started! The safe zone will begin shrinking.")
    end
end

function HGRadiationCallback(timedEvent)
    if safeRadius > MinRadius then
        safeRadius = safeRadius - ShrinkPerTick
    end

    for _, player in ipairs(players) do
        if player.IsOnline then
            local distance = Util.GetVectorsDistance(player.Location, center)
            if distance > safeRadius then
                player.Health = player.Health - RadiationDamage * ((distance - safeRadius) / 100.0 + 1.0)
                player:MessageFrom("HungerGames", "You are outside the safe zone! Move towards the center!")
            end
        end
    end
end
```

### Notes / what was trimmed
- This example only shows the shrinking-zone/radiation-damage mechanic; the join/leave/inventory-swap/arena
  lockdown machinery around it is identical to [`TeamDeathmatch.md`](TeamDeathmatch.md) (minus the team split -
  HungerGames is always free-for-all) and was omitted here to avoid duplicating it.
- The original additionally adds a chance to start bleeding or take extra random damage the further outside the
  zone a player is, can break a player's legs past a radiation threshold, tracks a kill leaderboard for an
  end-of-match top list with rewards, and uses a configurable player-count threshold (`CRad`) before radiation
  even starts, so early-game players aren't punished while the lobby is still filling up. The timer callback
  above runs on the **main thread** (`Plugin.CreateTimer`, not a System Timer) specifically because it touches
  `Player.Health`/`Location` directly - see [`Classes/Timers.md`](../Classes/Timers.md) for why that distinction
  matters.

See also: [`TeamDeathmatch.md`](TeamDeathmatch.md) · [`Classes/Timers.md`](../Classes/Timers.md) ·
[`Classes/Util.md`](../Classes/Util.md)
