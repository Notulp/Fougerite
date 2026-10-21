# GlitchFix

**GlitchFix** (`GlitchFix\GlitchFixModule.cs`) is a `Fougerite.Module` that blocks a handful of long-standing
Rust Legacy building/griefing exploits by listening to `Fougerite.Hooks.OnEntityDeployedWithPlacer`,
`OnPlayerSpawned`, `OnPlayerTeleport`, `OnCommand`, and (optionally) `OnNPCHurt`. Every check is independently
toggleable, and a trusted player/permission can be exempted from all of them.

## Configuration

`GlitchFix.cfg` (an `IniParser` file next to the module, see [`Classes/Config.md`](Classes/Config.md)),
`[Settings]` section unless noted:

| Key | Default | Meaning |
|---|---|---|
| `enabled` | - | Master on/off switch for the whole module. |
| `AdminBypass` | `false` | Admins (`Player.Admin`, see [`AdminAccess.md`](AdminAccess.md)) skip every check below. |
| `PermissionBypass` | `false` | Players holding the `glitchfix.bypass` permission (checked via [`PermissionManager`](PermissionManager.md)'s `PermissionSystem`) skip every check below. |
| `giveback` | - | Refund the item to the placer whenever a placement is blocked/destroyed. |
| `rampstackcheck` | - | Enables the ramp-stack-limit check (`RampStackMax`). |
| `structurecheck` | - | Enables the legacy raw-collider-name structure check (woodbox/door/stash). |
| `RockGlitch` | - | Enables the spawn/teleport-into-rock detection. |
| `RockGlitchKill` | - | Kills the player caught rock-glitching (on top of/instead of other responses). |
| `RockGlitchDestroySleepingBag` | - | Also destroys any sleeping bag within 3m of a caught rock-glitcher. |
| `PreventBuildInRock` | - | Blocks placing foundations/ramps/deployables inside a rock. |
| `ShouldTeleportPlayerOutOfRock` | - | Teleports the caught player to the nearest safe terrain point instead of/alongside killing them. |
| `CheckForRampLoot` | - | Enables the anti-ramp-on-object/character checks (`AntiRampObject`, legacy ramp branch of `structurecheck`). |
| `BarricadePillarGlitchDetection` (`BarricadePillar`) | - | Blocks barricades placed directly on top of a fence pillar near another pillar. |
| `AntiFoundationHide` | - | Blocks a foundation placed to hide a nearby deployable/player inside it (`FoundationHideRadius`). |
| `AntiPillarStash` | - | Blocks a pillar placed to hide a nearby deployable (`PillarStashRadius`). |
| `AntiRampObject` | - | Blocks a ramp placed over a nearby deployable/character (`RampObjectRadius`), only when `CheckForRampLoot` is also on. |
| `AntiDoorStash` | - | Blocks a `SmallStash` hidden directly in a doorway. |
| `AntiAnimalGlitch` | `false` | Destroys a dead animal's corpse after `AnimalHitsBeforeDestroy` further hits, so players can't glitch inside it. |
| `AntiShacking` | - | Blocks deployables (shelter/spike/gate/etc., see `[Shacking]` below) placed too close to a door to grief it. See "What is shacking?" below. |
| `AntiBarricadeOnPillar` | - | Blocks a barricade placed directly on top of a pillar. |
| `RampStackMax` | `1` | Max ramps allowed stacked against the same foundation/ceiling. |
| `AnimalHitsBeforeDestroy` | `5` | Hits (at &le;0 HP) before a dead animal's corpse is force-destroyed. |
| `FoundationHideRadius` | `4.5` | Radius (meters) for `AntiFoundationHide`. |
| `PillarStashRadius` | `0.40` | Radius (meters) for `AntiPillarStash`. |
| `RampObjectRadius` | `3.5` | Radius (meters) for `AntiRampObject`. |
| `FoundationHideMessage` | `"You cannot place structures on deployables here ({0})"` | Shown on most of the "hidden object" blocks; `{0}` is the name of what was found. |
| `RockBuildMessage` | `"You are not allowed to build in rocks"` | Shown by `PreventBuildInRock`. |
| `PillarBarricadeMessage` | `"Pillar Barricade glitching is not allowed!"` | Shown by `AntiBarricadeOnPillar`/`BarricadePillar`. |
| `ShackingMessage` | `"Griefing is not allowed on this server"` | Shown by `AntiShacking`. |

### What is "shacking"?

"Shacking" is slang for a griefing trick where a player places cheap deployables (a shelter,
spikes, a gate, a barricade...) directly against someone else's door, boxing it in so the owner
can no longer open/use it or get in/out of their own base. It's called "shacking" because the
classic version of this trick is wedging a small `Shelter` ("shack") right against a door.

`AntiShacking` stops this by checking, every time a player places one of the deployable types
listed in `[Shacking]`, whether there is a door collider within that deployable's configured
radius of the spot it's being placed at (`GlitchFixModule.cs`'s `SphereContainsNamed`/
`GetShackingRadius`, called from `EntityDeployed`). If a door is found that close, the placement
is rejected: the entity is destroyed, the item is refunded to the placer when `giveback` is on,
and the placer is shown `ShackingMessage`.

`[Shacking]` section - one `key=radius` per deployable name fragment (case-insensitive substring match against
the entity name), `0` disables the check for that entry. Defaults compiled in if the section is absent/empty:
`Shelter=5`, `Spike=2`, `Barricade=0`, `Gate=5`. You can add your own `key=radius` lines here to cover any
other deployable prefab name (e.g. `Campfire=3`).

**Worked example** - with the default config (`Shelter=5`, `Spike=2`, `Gate=5`, `Barricade=0`):

- A player tries to place a `Shelter` 3 meters from your base's front door. Since 3m is within the
  configured `Shelter=5` radius and a door is found nearby, the placement is blocked, the `Shelter`
  is destroyed, the item goes back to the placer's inventory (if `giveback=true`), and they see
  `ShackingMessage` ("Griefing is not allowed on this server" by default).
- The same player tries to place that `Shelter` 8 meters away from any door - that's outside the
  5m radius, so no door is found nearby and the placement succeeds normally.
- A player places `Spikes` 1.5 meters from a door - blocked (within the `Spike=2` radius).
  Placing those same `Spikes` 4 meters from a door - allowed (outside the `Spike=2` radius).
- `Barricade=0` means barricades are **never** checked by this rule (radius `0` disables the entry
  entirely) - they can be placed right next to a door without triggering `AntiShacking` (barricades
  next to pillars are instead covered separately by `AntiBarricadeOnPillar`).

## What each check actually does

- **Rock glitch (spawn/teleport)** - on `OnPlayerSpawned`/`OnPlayerTeleport`, raycasts straight down from above
  the player; if it hits an *unnamed* collider (a raw terrain/rock mesh instead of a game object) right at or
  below the player, they're considered "inside" the rock. Optionally broadcasts a warning, destroys a nearby
  sleeping bag, teleports them to the nearest safe terrain point, and/or kills them.
- **Build-in-rock prevention** - on entity placement, raycasts from well below the target position upward; if
  an unnamed collider's bounds contain both the placement point and the placer's feet, the placement is
  rejected, the entity destroyed, and (if `giveback` is on) the item refunded.
- **Anti-shacking** - "shacking" means boxing in someone's door with a cheap deployable (shelter/spikes/gate)
  so they can't use it; see "What is shacking?" above for the full explanation and a worked example. For
  deployables matching a `[Shacking]` entry, checks whether a door-named collider exists within that entry's
  configured radius; if so the placement is blocked as griefing.
- **Anti-barricade-on-pillar** - blocks a barricade placed directly above a pillar mesh (raycast straight down
  from slightly above the placement point).
- **Anti-foundation-hide / anti-pillar-stash / anti-ramp-on-object** - sphere-overlap checks around a newly
  placed foundation/pillar/ramp for any other deployable or player character; if one is found, the placement is
  rejected to stop players from hiding a deployable/person under/behind newly-placed structure pieces.
- **Legacy structure check (`structurecheck`)** - an independent, broader sphere-overlap check by raw collider
  name (not `DeployableObject` component) covering overlap with `woodbox`, `smallstash`, or `door` colliders.
  Deliberately kept alongside the more specific anti-*-hide checks above rather than merged into them, since the
  two don't cover identical ground (notably the door/stash-vs-woodbox cases below).
- **Anti-door-stash** - specifically blocks a `SmallStash` placed directly in a doorway (distinct from the
  general anti-shacking door-proximity check above, and from the legacy structure check's own door handling).
- **Ramp stack limit** - raycasts down from a newly placed ramp; if it lands on a foundation/ceiling already
  carrying more than `RampStackMax` other ramps, the new one is rejected.
- **Pillar barricade glitch (`BarricadePillar`)** - blocks a pillar placed within 0.34m of an existing
  `Barricade_Fence` deployable.
- **Anti-animal-glitch** - tracks hits-after-death per corpse position; once a dead NPC has absorbed
  `AnimalHitsBeforeDestroy` more hits, its `GameObject` is force-destroyed via `NetCull.Destroy` so players can't
  keep glitching inside the corpse. A 30-minute cleanup timer prunes stale tracking entries.

## Commands

`/glitchfix` - requires `Player.Admin` or the `glitchfix.reload` permission:

- `/glitchfix` - prints version/author/usage.
- `/glitchfix reload` - reloads `GlitchFix.cfg` without restarting the server.
