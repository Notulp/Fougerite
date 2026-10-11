### Guide
Writing your first Lua (MoonSharp) plugin for Fougerite

### Description
This is a beginner-friendly, step-by-step tutorial for writing a **Lua** script plugin for Fougerite. Lua
plugins run on **MoonSharp**, a pure-.NET Lua interpreter hosted directly inside the server process - no
external Lua install is required. Per the engine speed write-up in
[`Classes/PluginLoaders.md`](Classes/PluginLoaders.md#engine-speed--performance), MoonSharp is the second
fastest of the three script engines, behind IronPython but ahead of Jint (JS). If you're looking for the full
technical reference instead of a walkthrough, see [`Classes/BasePlugin.md`](Classes/BasePlugin.md) (everything
every plugin inherits) and the [Hooks reference](Hooks/README.md).

### 1. Requirements
- Nothing to install - MoonSharp ships embedded inside `Fougerite.dll`. Just a text editor.
- Standard Lua 5.x syntax/standard library (MoonSharp targets Lua 5.2) - `pcall`, `tonumber`, `string.format`,
  etc. all work as expected.
- Fougerite's own patched MoonSharp build auto-registers the entire `UnityEngine`/`Assembly-CSharp`/
  `Fougerite` namespaces for interop, so any public .NET type/member is reachable from Lua using normal `:`/`.`
  syntax - you don't need a `require`/import step for Fougerite's own API (see the global table in section 3).

### 2. Folder layout
A Lua plugin lives under `Save\LuaPlugins\<Name>\`, and the **file name must match the folder name**:
```
Save\
└── LuaPlugins\
    └── MyFirstPlugin\
        └── MyFirstPlugin.lua     <-- required, same name as the folder
```
Only this single `.lua` file is read - there's no multi-file compilation step. Make sure
`Save\Fougerite.cfg` has the engine enabled:
```ini
[Engines]
EnableLua=true
```
The plugin is picked up automatically on server start, and `PluginWatcher` hot-reloads it automatically
whenever you save the file again - no server restart needed.

### 3. The plugin skeleton
Unlike Python/C#, a Lua plugin isn't a class - it's just a flat script of global functions/variables (Lua
doesn't need a "main class" matching the file name). Metadata (`Author`/`About`/`Version`, shown in
Fougerite's `[PluginLoader] Module ... initiated.` startup log line) comes from three **global** variables,
read once right after your script executes:

```lua
Author = "YourName"
Version = "1.0"
About = "My very first Fougerite Lua plugin."

function On_PluginInit()
    -- Code that runs once, when the plugin is loaded. This is the right place to load configs
    -- and do first-time setup - there's no separate "subscribe to a hook" step like in C#, see
    -- section 4 below.
    Util:Log("MyFirstPlugin initialized.")
end

function On_PluginDeinit()
    -- Code that runs once, when the plugin is unloaded/reloaded (optional - only called if defined).
    Util:Log("MyFirstPlugin unloaded.")
end
```
A few important things to notice:
- There's no `Initialize()`/`DeInitialize()` override pair - use the `On_PluginInit`/`On_PluginDeinit` hooks
  instead (see [`On_PluginInit`](Hooks/Server/On_PluginInit.md)), both are plain global functions, not hook
  subscriptions.
- **Both `obj:Method(...)` (colon) and `obj.Method(...)` (dot) work for calling a method on a CLR/UserData
  object** (a `Player`, `Timer`, `Util`, `Plugin`, ...) - MoonSharp's automatic interop returns an
  already-bound callable either way, so `object.Method(obj, args)` (plain Lua rule: dot never auto-passes
  `self`) still "just works" here without the extra first argument. You'll see both styles used throughout
  this documentation and in real plugins (e.g. `Util.Log(...)` but `timer:Kill()`) - pick whichever reads
  cleaner, it's purely a style choice for these objects, not a correctness one.
- `Plugin`, `Server`, `Util`, `World`, `DataStore`, `Data`, and the rest of the table in
  [`Classes/BasePlugin.md`](Classes/BasePlugin.md#script-plugin-global-variables) are already injected as
  globals before your code runs - no `require`/import needed to use them.

### 4. Hooks: reacting to events
Unlike C#, there's no `+=`/`-=` subscription step - **just define a global function named
`On_<HookName>`** and Fougerite calls it automatically whenever that event fires (via `BasePlugin.Invoke`,
looked up by name). The full list of hooks (with every argument documented) lives in the
[Hooks reference](Hooks/README.md); this section only covers the basics.

#### Example: `On_Command`
Called every time a player (or the console) types something starting with `/`. Full reference:
[`On_Command`](Hooks/Player/On_Command.md).
```lua
function On_Command(player, cmd, args)
    -- "/spawn battlekit sniper" -> cmd = "spawn", args = {"battlekit", "sniper"}
    if cmd == "test" then
        player:Message("It works!")
    end
end
```
- `player` - the `Fougerite.Player` who ran the command.
- `cmd` - the command name, without the leading `/`.
- `args` - a Lua table (1-indexed) of everything typed after the command name, split by spaces.

#### Example: `On_Crafting`
Called when a player starts crafting an item. Full reference: [`On_Crafting`](Hooks/Items/On_Crafting.md).
```lua
function On_Crafting(CraftingEvent)
    CraftingEvent.Player:Message("Crafting executed!")
    -- CraftingEvent.Cancelled = true would block the craft entirely.
end
```
- `CraftingEvent.Player` - the `Fougerite.Player` who's crafting.
- `CraftingEvent.Cancelled` - set to `true` to cancel the craft.

#### Example: `On_DoorUse` (timer + table args, a practical "auto-close door" plugin)
Full reference: [`On_DoorUse`](Hooks/Entities/On_DoorUse.md). This combines a hook, a per-player
[Timer](Classes/Timers.md), and `Util:TryFindReturnType` (looking up a Rust/Unity type by name, useful
whenever Fougerite's own API doesn't already wrap something):
```lua
function On_ServerInit()
    local StructureComponent = Util:TryFindReturnType("StructureComponent")
    Plugin:Log("Test", "Found type: " .. tostring(StructureComponent))
end

function On_Command(player, cmd, args)
    if cmd ~= "doorcloser" then return end
    if #args == 0 then
        player:Message("Usage: /doorcloser number (1-10)")
        return
    end

    local seconds = tonumber(args[1])
    if seconds == nil or seconds < 1 or seconds > 10 then
        player:Message("Usage: /doorcloser number (1-10)")
        return
    end

    DataStore:Add("AutoCloser", player.UID, seconds)
    player:Message("Closing door after " .. seconds .. " seconds")
end

function On_DoorUse(player, DoorUseEvent)
    if DataStore:Get("AutoCloser", player.UID) == nil then return end

    local door = DoorUseEvent.BasicDoor
    local seconds = DataStore:Get("AutoCloser", player.UID)

    local dict = Plugin:CreateDict()
    dict:Add("Door", door)
    Plugin:CreateTimer("AutoCloseDoor", seconds * 1000, dict, false):Start()
end

function AutoCloseDoorCallback(timer)
    timer:Kill()
    local door = timer.Args["Door"]
    if door.state:ToString() ~= "Closing" and door.state:ToString() ~= "Closed" then
        door:ToggleStateServer(door.transform.position, Util.TimeInMillis, nil)
    end
end
```
Note the naming convention for timer callbacks: a timer named `"AutoCloseDoor"` (the first argument to
`Plugin:CreateTimer`) fires a global function named `AutoCloseDoorCallback` - see
[`Classes/Timers.md`](Classes/Timers.md) for the full Timer API (Normal vs. Parallel/System timers, `Args`,
repeating timers, etc).

> These examples are only a small sample. See the [full Hooks reference](Hooks/README.md) for every available
> event, grouped by category (Player, NPC, Entities, Items, Combat, World, Server), with examples in
> C#/Python/JS/Lua.

### 5. Config files: never use `Config`/`Fougerite.cfg`
Just like every other plugin, a Lua plugin must own **its own** config file, never the shared
`Fougerite.cfg` - see the warning and full Python/JS/Lua example (`IniExists`/`CreateIni`/`GetIni`, plus
safely parsing values back out with `pcall`/`tonumber`) in [`Classes/Config.md`](Classes/Config.md). The short
version:
```lua
function MyConfig()
    if not Plugin:IniExists("MySettings") then
        local ini = Plugin:CreateIni("MySettings")
        ini:AddSetting("Settings", "WelcomeMessage", "Welcome to the server!")
        ini:Save()
    end
    return Plugin:GetIni("MySettings")
end
```
`MySettings.ini` is created inside this plugin's own folder (`Save\LuaPlugins\MyFirstPlugin\MySettings.ini`),
fully separate from `Fougerite.cfg` and from every other plugin's settings.

### 6. Full example: a minimal mod-removal command
A small, self-contained plugin putting the previous sections together - lets a player strip the attachments
(mods) off their currently-held weapon with a command, safely checking everything beforehand:
```lua
Author = "YourName"
Version = "1.0"
About = "Minimal weapon mod removal example plugin."

function On_PluginInit()
    Util:Log("ModRemover initialized.")
end

function On_Command(player, cmd, args)
    if cmd ~= "unmod" then return end

    local inv = player.Inventory
    if inv == nil then
        player:Message("Inventory error")
        return
    end

    local item = inv.ActiveItem
    if item == nil then
        player:Message("No active item")
        return
    end

    if not item.ClearMods then
        player:Message("Mods not supported on this item")
        return
    end

    local used = item.UsedModSlots or 0
    if used == 0 then
        player:Message("No mods on this weapon")
        return
    end

    item:ClearMods(true)
    player:Message("Removed " .. used .. " mods")
end
```
Drop this in `Save\LuaPlugins\ModRemover\ModRemover.lua`, make sure `EnableLua=true` in `Fougerite.cfg`, and
start/reload the server. You should see something like:
```
[PluginLoader] Module ModRemover<Lua> v1.0 (by YourName) initiated.
ModRemover initialized.
```

### 7. Where to go next
- [Hooks reference](Hooks/README.md) - every hook/event Fougerite exposes.
- [`Classes/BasePlugin.md`](Classes/BasePlugin.md) - everything a Lua plugin inherits (timers, logging,
  dictionaries, cross-plugin messaging, the full list of injected globals).
- [`Classes/Config.md`](Classes/Config.md) - the full `IniParser`/`GetIni`/`CreateIni` config story, including
  how to safely parse values back out (`pcall`, `tonumber`, fallback defaults).
- [`Classes/Timers.md`](Classes/Timers.md) - the Normal Timer/`<Name>Callback` pattern used in section 4.
- [`Classes/Player.md`](Classes/Player.md), [`Classes/Server.md`](Classes/Server.md),
  [`Classes/Entity.md`](Classes/Entity.md) - the core game object wrappers you'll use the most.
- [`PythonPluginTutorial.md`](PythonPluginTutorial.md) / [`JavaScriptPluginTutorial.md`](JavaScriptPluginTutorial.md) /
  [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md) /
  [`CSScriptPluginTutorial.md`](CSScriptPluginTutorial.md) - the same walkthrough for the other engines.
