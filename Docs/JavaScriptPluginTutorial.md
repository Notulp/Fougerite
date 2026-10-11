### Guide
Writing your first JavaScript (Jint) plugin for Fougerite

### Description
This is a beginner-friendly, step-by-step tutorial for writing a **JavaScript** script plugin for Fougerite.
JS plugins run on **Jint**, a pure-.NET ECMAScript 5 interpreter hosted directly inside the server process -
no Node.js/external JS install is required. Per the engine speed write-up in
[`Classes/PluginLoaders.md`](Classes/PluginLoaders.md#engine-speed--performance), Jint is unfortunately the
**slowest** of the three script engines - avoid heavy per-tick/per-frame math in a JS plugin if you can help
it. If you're looking for the full technical reference instead of a walkthrough, see
[`Classes/BasePlugin.md`](Classes/BasePlugin.md) (everything every plugin inherits) and the
[Hooks reference](Hooks/README.md).

### 1. Requirements
- Nothing to install - Jint ships embedded inside `Fougerite.dll`. Just a text editor.
- Jint targets **ECMAScript 5**: no `let`/`const`, no arrow functions, no template literals, no `class`.
  Always declare variables with `var` and write old-style `function` expressions, exactly like the examples
  below.
- Fougerite's Jint build is CLR-aware (`cfg.AllowClr(...)`): any public .NET type already referenced by the
  server is reachable, and `importClass("Namespace.TypeName")` lets you pull in a type by its full name and
  use it as a constructor/static-member holder if you ever need one that isn't already injected as a global
  (see section 3).

### 2. Folder layout
A JS plugin lives under `Save\JsPlugins\<Name>\`, and the **file name must match the folder name**:
```
Save\
└── JsPlugins\
    └── MyFirstPlugin\
        └── MyFirstPlugin.js     <-- required, same name as the folder
```
Only this single `.js` file is read - there's no multi-file compilation step. Make sure
`Save\Fougerite.cfg` has the engine enabled:
```ini
[Engines]
EnableJavaScript=true
```
The plugin is picked up automatically on server start, and `PluginWatcher` hot-reloads it automatically
whenever you save the file again - no server restart needed.

### 3. The plugin skeleton
Just like Lua, a JS plugin isn't a class - it's a flat script of global functions/variables. Metadata
(`Author`/`About`/`Version`, shown in Fougerite's `[PluginLoader] Module ... initiated.` startup log line)
comes from three **global** `var`s, read once right after your script executes:

```javascript
var Author = "YourName";
var About = "My very first Fougerite JavaScript plugin.";
var Version = "1.0";

function On_PluginInit() {
    // Code that runs once, when the plugin is loaded. This is the right place to load configs
    // and do first-time setup - there's no separate "subscribe to a hook" step like in C#, see
    // section 4 below.
    Util.Log("MyFirstPlugin initialized.");
}

function On_PluginDeinit() {
    // Code that runs once, when the plugin is unloaded/reloaded (optional - only called if defined).
    Util.Log("MyFirstPlugin unloaded.");
}
```
A few important things to notice:
- There's no `Initialize()`/`DeInitialize()` override pair - use the `On_PluginInit`/`On_PluginDeinit` hooks
  instead (see [`On_PluginInit`](Hooks/Server/On_PluginInit.md)), both are plain global functions, not hook
  subscriptions. If `Author`/`About`/`Version` aren't declared at all, Jint falls back to `"Unknown"`/`""`/
  `"1.0"` respectively, instead of failing to load.
- `Plugin`, `Server`, `Util`, `World`, `DataStore`, `Data`, and the rest of the table in
  [`Classes/BasePlugin.md`](Classes/BasePlugin.md#script-plugin-global-variables) are already injected as
  globals before your code runs - no `require`/import needed to use them.
- Jint is a strict ES5 interpreter wrapping .NET objects, so a few things behave differently than in a
  browser/Node:
  - `.Length`/`.Count` are patched onto `Array`/`String` so interop code that expects a .NET-style property
    (instead of JS's own `.length`) still works, but prefer the normal JS `.length` in your own code.
  - `for (var x in collection)` over an injected .NET collection yields **values** (items/entries), not
    numeric/string keys like a plain JS object - see the `PlayerCache`/`World.Entities` example below.
  - There's no `console.log` - use `Util.Log(...)` or `Plugin.Log("Category", ...)` instead.

### 4. Hooks: reacting to events
Unlike C#, there's no `+=`/`-=` subscription step - **just define a global function named
`On_<HookName>`** and Fougerite calls it automatically whenever that event fires (via `BasePlugin.Invoke`,
looked up by name, matched against every callable found on the script's global scope). The full list of
hooks (with every argument documented) lives in the [Hooks reference](Hooks/README.md); this section only
covers the basics.

#### Example: `On_Command`
Called every time a player (or the console) types something starting with `/`. Full reference:
[`On_Command`](Hooks/Player/On_Command.md).
```javascript
function On_Command(Player, cmd, args) {
    // "/spawn battlekit sniper" -> cmd = "spawn", args = ["battlekit", "sniper"]
    if (cmd == "test") {
        Player.Message("It works!");
    }
}
```
- `Player` - the `Fougerite.Player` who ran the command.
- `cmd` - the command name, without the leading `/`.
- `args` - a .NET array of everything typed after the command name, split by spaces. Use `args.length` (or
  `args.Length`) and index with `args[0]`, like a normal JS array - don't use Python-style `len(args)`.

#### Example: `On_Chat`
Called when a player writes a normal chat message (one that does **not** start with `/`). Full reference:
[`On_Chat`](Hooks/Player/On_Chat.md). This is adapted from a real-world "sanitize and broadcast" plugin:
```javascript
function On_Chat(Player, ChatString) {
    var text = ChatString.OriginalMessage;
    // Strip anything that isn't a safe, printable character before re-broadcasting.
    var clean = text.replace(/[^a-zA-Z0-9 :,.\-\/!$%()=?]/g, '');

    Server.BroadcastFrom(Player.Name, clean);
    ChatString.NewText = " "; // Blank out the original message, we already broadcast our own version.
    return false;
}
```
- `ChatString.OriginalMessage` - the original text the player typed.
- `ChatString.NewText` - set this to replace/blank out what actually gets sent/broadcast.

#### Example: looping over injected .NET collections, and `Data.ToInt`
A couple of patterns that trip people up the first time, combined into one `On_Command` handler (adapted
from a real plugin's debugging code):
```javascript
function On_Command(Player, cmd, args) {
    if (cmd != "doorcloser") return;

    if (args.length == 0) {
        Player.Message("Usage: /doorcloser number (1-10)");
        return;
    }

    // Data.ToInt safely parses a string to an int, without throwing on garbage input - see Classes/Config.md.
    var seconds = Data.ToInt(args[0]);
    if (isNaN(seconds) || seconds > 10 || seconds < 1) {
        Player.Message("Usage: /doorcloser number (1-10)");
        return;
    }

    DataStore.Add("AutoCloser", Player.UID, seconds);
    Player.Message("Closing door after " + seconds + " seconds");
}

function On_DoorUse(Player, DoorUseEvent) {
    if (DataStore.Get("AutoCloser", Player.UID) == null) return;

    var door = DoorUseEvent.BasicDoor;
    var seconds = DataStore.Get("AutoCloser", Player.UID);

    var dict = Plugin.CreateDict();
    dict.Add("Door", door);
    Plugin.CreateTimer("AutoCloseDoor", seconds * 1000, dict, false).Start();
}

function AutoCloseDoorCallback(timer) {
    timer.Kill();
    var door = timer.Args["Door"];
    if (door.state.ToString() != "Closing" && door.state.ToString() != "Closed") {
        door.ToggleStateServer(door.transform.position, Util.TimeInMillis, null);
    }
}
```
Note the naming convention for timer callbacks: a timer named `"AutoCloseDoor"` (the first argument to
`Plugin.CreateTimer`) fires a global function named `AutoCloseDoorCallback` - see
[`Classes/Timers.md`](Classes/Timers.md) for the full Timer API (Normal vs. Parallel/System timers, `Args`,
repeating timers, etc). Full reference for the hook itself: [`On_DoorUse`](Hooks/Entities/On_DoorUse.md).

> These examples are only a small sample. See the [full Hooks reference](Hooks/README.md) for every available
> event, grouped by category (Player, NPC, Entities, Items, Combat, World, Server), each with Python/Lua/C#
> examples alongside JS.

### 5. Config files: never use `Config`/`Fougerite.cfg`
Just like every other plugin, a JS plugin must own **its own** config file, never the shared `Fougerite.cfg`
- see the warning and full Python/JS/Lua example (`IniExists`/`CreateIni`/`GetIni`, plus safely parsing
values back out) in [`Classes/Config.md`](Classes/Config.md). The short version:
```javascript
function MyConfig() {
    if (!Plugin.IniExists("MySettings")) {
        var ini = Plugin.CreateIni("MySettings");
        ini.AddSetting("Settings", "WelcomeMessage", "Welcome to the server!");
        ini.Save();
    }
    return Plugin.GetIni("MySettings");
}
```
`MySettings.ini` is created inside this plugin's own folder (`Save\JsPlugins\MyFirstPlugin\MySettings.ini`),
fully separate from `Fougerite.cfg` and from every other plugin's settings. When reading numeric settings
back, remember `parseInt`/`Number` don't throw on bad input - they return `NaN` - so always check with
`isNaN(...)` rather than relying on a `try/catch` alone (see the JS section of
[`Classes/Config.md`](Classes/Config.md#example---pythonjslua-safely-parsing-config-values-on-read)).

### 6. Full example: a minimal clan-tag plugin
A small, self-contained plugin putting the previous sections together - lets players create a "clan" that
prefixes their name in chat, backed by its own `.ini` file:
```javascript
var Author = "YourName";
var About = "Minimal clan-tag example plugin.";
var Version = "1.0";

var Clans = {}; // SteamID -> clan tag, in-memory cache.

function ClansConfig() {
    if (!Plugin.IniExists("Clans")) {
        var ini = Plugin.CreateIni("Clans");
        ini.Save();
    }
    return Plugin.GetIni("Clans");
}

function On_PluginInit() {
    var ini = ClansConfig();
    var members = ini.EnumSection("Members");
    for (var i = 0; i < members.Count; i++) {
        var steamid = members[i];
        Clans[steamid] = ini.GetSetting("Members", steamid);
    }
    Util.Log("MyClans initialized.");
}

function On_Command(Player, cmd, args) {
    if (cmd == "setclan") {
        if (args.length == 0) {
            Player.Message("Usage: /setclan tagname");
            return;
        }
        var tag = args.join(" ");
        var ini = ClansConfig();
        ini.AddSetting("Members", Player.SteamID, tag);
        ini.Save();
        Clans[Player.SteamID] = tag;
        Player.Message("Your clan tag is now [" + tag + "]");
    }
}

function On_Chat(Player, ChatString) {
    if (Clans[Player.SteamID] != null) {
        ChatString.NewText = "[" + Clans[Player.SteamID] + "] " + ChatString.OriginalMessage;
    }
}
```
Drop this in `Save\JsPlugins\MyClans\MyClans.js`, make sure `EnableJavaScript=true` in `Fougerite.cfg`, and
start/reload the server. You should see something like:
```
[PluginLoader] Module MyClans<JavaScript> v1.0 (by YourName) initiated.
MyClans initialized.
```

### 7. Where to go next
- [Hooks reference](Hooks/README.md) - every hook/event Fougerite exposes.
- [`Classes/BasePlugin.md`](Classes/BasePlugin.md) - everything a JS plugin inherits (timers, logging,
  dictionaries, cross-plugin messaging, the full list of injected globals).
- [`Classes/Config.md`](Classes/Config.md) - the full `IniParser`/`GetIni`/`CreateIni` config story, including
  how to safely parse values back out.
- [`Classes/Player.md`](Classes/Player.md), [`Classes/Server.md`](Classes/Server.md),
  [`Classes/Entity.md`](Classes/Entity.md) - the core game object wrappers you'll use the most.
- [`Classes/Timers.md`](Classes/Timers.md), [`Classes/Web.md`](Classes/Web.md),
  [`Classes/Loom.md`](Classes/Loom.md) - scheduling, HTTP requests, and safely jumping between threads.
- [`PythonPluginTutorial.md`](PythonPluginTutorial.md) / [`LuaPluginTutorial.md`](LuaPluginTutorial.md) /
  [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md) / [`CSScriptPluginTutorial.md`](CSScriptPluginTutorial.md) -
  the same walkthrough for the other engines. If per-tick performance matters, prefer those over JS (see
  [`Classes/PluginLoaders.md`](Classes/PluginLoaders.md#engine-speed--performance)).
