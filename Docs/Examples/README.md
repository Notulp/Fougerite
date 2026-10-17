# Plugin Examples

This folder is a collection of worked examples, one per page, each reimplementing the **core mechanic** of one
of DreTaX's original Python/IronPython plugins for Fougerite (originally written for Magma/Fougerite between
~2013-2015) in all four supported languages: **C#**, **JavaScript**, **Python**, and **Lua**.

The originals live at
[`FougeritePlugins`](https://github.com/dretax/Python-Plugins/tree/master/FougeritePlugins) in the
[`dretax/Python-Plugins`](https://github.com/dretax/Python-Plugins) GitHub repository - dozens of plugins
covering everything from kill-protection to full game modes. These pages don't try to reproduce every single
plugin (many are small admin-chat/log utilities), nor do they transcribe every line of the originals (some are
900-1700 lines long, with years of feature creep, config options, and RustPP-specific integration). Instead,
each page explains **the interesting mechanic** behind one plugin and shows a smaller, self-contained version
of it, built directly against the Fougerite API documented under [`Docs/`](../README.md) - good as a learning
reference or a starting point for your own plugin, not a drop-in replacement for the original.

## Picking a language

Before reading any of these, make sure you've gone through the tutorial for whichever language you intend to
write in - they cover the setup/boilerplate (plugin skeleton, hook naming convention, how `Plugin`/`DataStore`/
`Util` get injected) that these example pages assume you already know:
- [`CSharpPluginTutorial.md`](../CSharpPluginTutorial.md)
- [`JavaScriptPluginTutorial.md`](../JavaScriptPluginTutorial.md)
- [`PythonPluginTutorial.md`](../PythonPluginTutorial.md)
- [`LuaPluginTutorial.md`](../LuaPluginTutorial.md)

## The examples

Roughly ordered from simplest to most involved:

| Page | Original plugin | Mechanic |
|---|---|---|
| [`BambeeProtector.md`](BambeeProtector.md) | `BambeeProtector` | Temporary new-player PvP/explosive kill-protection with a cooldown-based grace period. |
| [`Warps.md`](Warps.md) | `Warps` | Named teleport points stored in their own ini, with admin-only warps and an optional teleport delay. |
| [`LevelSystem.md`](LevelSystem.md) | `LevelSystem` | Per-player level/XP progression that scales resource-gathering yield. |
| [`Kits.md`](Kits.md) | `Kits` | Config-driven item loadouts ("kits") with per-rank access, cooldowns, and max-use limits. |
| [`iConomy.md`](iConomy.md) | `iConomy` | A full player-balance economy: PvP/NPC kill rewards and an ini-driven buy/sell shop. |
| [`Clans.md`](Clans.md) | `Clans` | Role-based clans (owner/officer/member) with opt-in friendly-fire between members. |
| [`DestroySystem.md`](DestroySystem.md) | `DestroySystem` | An admin/owner "destroy mode" that lets you remove your own structures by hitting them, with a raid-cooldown lockout. |
| [`Recorder.md`](Recorder.md) | `Recorder` | Recording a placed structure as a reusable blueprint and respawning it elsewhere. |
| [`TeamDeathmatch.md`](TeamDeathmatch.md) | `TDM` | A full round-based arena game mode: lobby/queue, inventory swap, and an in-arena "no building" lockdown. |
| [`HungerGames.md`](HungerGames.md) | `HungerGames` | A battle-royale variant of the above: a shrinking "radiation" safe zone that forces players together over time. |

Each page follows the same structure: a short explanation of how the original plugin behaves (and which real
hooks it uses, cited directly from the source), followed by **C#**, **JavaScript**, **Python**, and **Lua**
versions of a trimmed-down, working implementation of that mechanic.

See also: [`Docs/README.md`](../README.md) for the full class/hook reference these examples build on.
