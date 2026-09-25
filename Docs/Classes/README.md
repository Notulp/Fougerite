# Classes Reference

This section documents important helper classes available to Fougerite plugins, beyond the event hooks.

- [`BasePlugin`](BasePlugin.md) - The base class every plugin (C#, Python, JS, Lua) is built on: fields,
  helper methods, the script global variables (`Plugin`, `Server`, `Util`, `World`, ...), and how the
  custom Mono build's single-AppDomain hot-reload works (see `Icalls`).
- [`Player`](Player.md) - The connected/known Rust client wrapper: properties, messaging, teleporting,
  metabolism, command restriction.
- [`Server`](Server.md) - The running server singleton: player list, chat broadcasts, bans, command
  restriction.
- [`Entity`](Entity.md) - Structures, deployables, storage, fire barrels, resource nodes and supply crates.
- [`Sleeper`](Sleeper.md) - Sleeping bags/beds and their sleeping avatars.
- [`Util`](Util.md) - Thread-info helpers, the System Timer API, world/entity lookups, reflection/hash
  helpers.
- [`Web`](Web.md) - Making synchronous and asynchronous HTTP requests from a plugin.
- [`Loom`](Loom.md) - Avoiding "Oops! Crashed" popups by running code on the main thread, and running heavy
  CPU-bound work on background threads.
- [`Timers`](Timers.md) - Normal Timers vs System Timers (`Plugin.CreateTimer`/`CreateParallelTimer` vs
  `Util.CreateSystemTimer`/`CreateParallelSystemTimer`): what thread each runs on, and when to use which.
