### Guide
Writing your first Python (IronPython) plugin for Fougerite

### Description
This is a beginner-friendly, step-by-step tutorial for writing a **Python** script plugin for Fougerite.
Python plugins run on **IronPython**, hosted directly inside the server process via the DLR (`Microsoft.Scripting`) -
no external Python install is required, and (per the engine speed write-up in
[`Classes/PluginLoaders.md`](Classes/PluginLoaders.md#engine-speed--performance)) it's also the fastest of the
three script engines. If you're looking for the full technical reference instead of a walkthrough, see
[`Classes/BasePlugin.md`](Classes/BasePlugin.md) (everything every plugin inherits) and the
[Hooks reference](Hooks/README.md).

### 1. Requirements
- Nothing to install - IronPython ships embedded inside `Fougerite.dll`. Just a text editor.
- Python plugins are **IronPython 2.7**-compatible, so: Python 2 syntax (`print "x"`, not `print("x")`),
  `xrange` is available, and there's no `f"..."` f-strings.
- Extra third-party `.py` libraries (e.g. `random`, as used further below) go in the server's
  `Save\Lib\` folder - it's automatically on every Python plugin's search path (alongside
  `rust_server_Data\Managed\` for `clr.AddReferenceByPartialName`), so `import whatever` finds them without
  any manual `sys.path` changes.

### 2. Folder layout
A Python plugin lives under `Save\PyPlugins\<Name>\`, and the **file name must match the folder name**:
```
Save\
└── PyPlugins\
    └── MyFirstPlugin\
        └── MyFirstPlugin.py     <-- required, same name as the folder
```
Unlike a CSScript plugin, only this single `.py` file is read - there's no multi-file compilation step.
Make sure `Save\Fougerite.cfg` has the engine enabled:
```ini
[Engines]
EnablePython=true
```
The plugin is picked up automatically on server start, and `PluginWatcher` hot-reloads it automatically
whenever you save the file again - no server restart needed.

### 3. The plugin skeleton
A Python plugin is a single class whose **name must match the plugin's name** (the file/folder name) - this
is how the loader finds your "main" class after executing the script
(`Class = Engine.Operations.Invoke(Scope.GetVariable(Name))` under the hood). Metadata (`Author`/`About`/
`Version`, shown in Fougerite's `[PluginLoader] Module ... initiated.` startup log line) comes from three
**module-level** (not class-level) dunder variables, read once right after your script executes:

```python
__author__ = 'YourName'
__version__ = '1.0'
__about__ = 'My very first Fougerite Python plugin.'


class MyFirstPlugin:

    def On_PluginInit(self):
        # Code that runs once, when the plugin is loaded. This is the right place to load configs
        # and do first-time setup - there's no separate "subscribe to a hook" step like in C#, see
        # section 4 below.
        Util.Log("MyFirstPlugin initialized.")

    def On_PluginDeinit(self):
        # Code that runs once, when the plugin is unloaded/reloaded (optional - only called if defined).
        Util.Log("MyFirstPlugin unloaded.")
```
A few important differences from C#:
- There's no `Initialize()`/`DeInitialize()` override pair - use the `On_PluginInit`/`On_PluginDeinit` hooks
  instead (see [`On_PluginInit`](Hooks/Server/On_PluginInit.md)), both are plain methods on your class, not
  hook subscriptions.
- Every method takes `self` as the first parameter, like any Python instance method.
- You don't need `import Fougerite`/`clr.AddReferenceByPartialName("Fougerite")` to access the API - `Plugin`,
  `Server`, `Util`, `World`, `DataStore`, `Data`, and the rest of the table in
  [`Classes/BasePlugin.md`](Classes/BasePlugin.md#script-plugin-global-variables) are already injected as
  globals into your script's scope before it even runs. `import Fougerite`/`clr.AddReferenceByPartialName` is
  only needed for types that *aren't* already one of those globals (e.g. `Fougerite.ServerSaveHandler`
  directly, or constructing a raw `Fougerite.XYZ` object by hand) - real-world plugins often add it as a
  matter of habit, but a plugin that only uses the injected globals doesn't need it at all.

### 4. Hooks: reacting to events
Unlike C#, there's no `+=`/`-=` subscription step - **just define a method named `On_<HookName>`** on your
class and Fougerite calls it automatically whenever that event fires (via `BasePlugin.Invoke`, looked up by
name). The full list of hooks (with every argument documented, and Python examples for most of them) lives
in the [Hooks reference](Hooks/README.md); this section only covers the basics.

#### Example: `On_Command`
Called every time a player (or the console) types something starting with `/`. Full reference:
[`On_Command`](Hooks/Player/On_Command.md).
```python
def On_Command(self, Player, cmd, args):
    # "/spawn battlekit sniper" -> cmd = "spawn", args = ["battlekit", "sniper"]
    if cmd == "test":
        Player.Message("It works!")
```
- `Player` - the `Fougerite.Player` who ran the command.
- `cmd` - the command name, without the leading `/`.
- `args` - a list of everything typed after the command name, split by spaces.

#### Example: `On_Chat`
Called when a player writes a normal chat message (one that does **not** start with `/`). Full reference:
[`On_Chat`](Hooks/Player/On_Chat.md).
```python
def On_Chat(self, Player, ChatString):
    if "noob" in ChatString.ToString().lower():
        ChatString.NewText = "   "  # empties the message, so it won't show up in chat
        Player.MessageFrom("Server", "Please don't use that word!")
```
- `ChatString.ToString()` (or `ChatString.OriginalMessage`) - the original text the player typed.
- `ChatString.NewText` - set this to replace/blank out what actually gets sent/broadcast.

#### Example: `On_PlayerHurt`
Called whenever a player takes damage, from any source. Full reference:
[`On_PlayerHurt`](Hooks/Player/On_PlayerHurt.md).
```python
def On_PlayerHurt(self, HurtEvent):
    if HurtEvent.VictimIsPlayer and HurtEvent.AttackerIsPlayer:
        if HurtEvent.Attacker.UID == HurtEvent.Victim.UID:
            return  # Self damage, ignore.
        HurtEvent.DamageAmount = 0.0  # Cancel all PvP damage.
```
- `HurtEvent.VictimIsPlayer` / `HurtEvent.AttackerIsPlayer` - whether each side of the event is a player.
- `HurtEvent.DamageAmount` - set to `0.0` (a Python `float`, not an `int`) to cancel/reduce the damage.

> These three hooks are only a small sample. See the [full Hooks reference](Hooks/README.md) for every
> available event, grouped by category (Player, NPC, Entities, Items, Combat, World, Server), each with the
> exact Python example, alongside C#/JS/Lua.

### 5. Config files: never use `Config`/`Fougerite.cfg`
Just like every other plugin, a Python plugin must own **its own** config file, never the shared
`Fougerite.cfg` - see the warning and full Python/JS/Lua example (`IniExists`/`CreateIni`/`GetIni`, plus
safely parsing values back out with `try/except`) in [`Classes/Config.md`](Classes/Config.md). The short
version:
```python
def MyConfig(self):
    if not self.IniExists("MySettings"):
        ini = self.CreateIni("MySettings")
        ini.AddSetting("Settings", "WelcomeMessage", "Welcome to the server!")
        ini.Save()
    return self.GetIni("MySettings")
```
`MySettings.ini` is created inside this plugin's own folder (`Save\PyPlugins\MyFirstPlugin\MySettings.ini`),
fully separate from `Fougerite.cfg` and from every other plugin's settings.

### 6. Full example: a minimal clan-tag plugin
A small, self-contained plugin putting the previous sections together - lets players create a two-person
"clan" that prefixes their name in chat, backed by its own `.ini` file (trimmed down from a much larger
real-world `Clans` plugin, keeping just the config + command + hook pattern):
```python
__author__ = 'YourName'
__version__ = '1.0'
__about__ = 'Minimal clan-tag example plugin.'

Clans = {}  # SteamID -> clan tag, module-level cache shared by every instance/reload.


class MyClans:

    def ClansConfig(self):
        if not self.IniExists("Clans"):
            ini = self.CreateIni("Clans")
            ini.Save()
        return self.GetIni("Clans")

    def On_PluginInit(self):
        ini = self.ClansConfig()
        for steamid in ini.EnumSection("Members"):
            Clans[steamid] = ini.GetSetting("Members", steamid)
        Util.Log("MyClans initialized, " + str(len(Clans)) + " members loaded.")

    def On_Command(self, Player, cmd, args):
        if cmd == "setclan":
            if len(args) == 0:
                Player.Message("Usage: /setclan tagname")
                return
            tag = str.join(" ", args)
            ini = self.ClansConfig()
            ini.AddSetting("Members", Player.SteamID, tag)
            ini.Save()
            Clans[Player.SteamID] = tag
            Player.Message("Your clan tag is now [" + tag + "]")

    def On_Chat(self, Player, ChatString):
        if Player.SteamID in Clans:
            ChatString.NewText = "[" + Clans[Player.SteamID] + "] " + ChatString.ToString()
```
Drop this in `Save\PyPlugins\MyClans\MyClans.py`, make sure `EnablePython=true` in `Fougerite.cfg`, and
start/reload the server. You should see something like:
```
[PluginLoader] Module MyClans<Python> v1.0 (by YourName) initiated.
MyClans initialized, 0 members loaded.
```

### 7. Where to go next
- [Hooks reference](Hooks/README.md) - every hook/event Fougerite exposes, most with a Python example.
- [`Classes/BasePlugin.md`](Classes/BasePlugin.md) - everything a Python plugin inherits (timers, logging,
  dictionaries, cross-plugin messaging, the full list of injected globals).
- [`Classes/Config.md`](Classes/Config.md) - the full `IniParser`/`GetIni`/`CreateIni` config story, including
  how to safely parse values back out (`try/except`, fallback defaults).
- [`Classes/Player.md`](Classes/Player.md), [`Classes/Server.md`](Classes/Server.md),
  [`Classes/Entity.md`](Classes/Entity.md) - the core game object wrappers you'll use the most.
- [`Classes/Timers.md`](Classes/Timers.md), [`Classes/Web.md`](Classes/Web.md),
  [`Classes/Loom.md`](Classes/Loom.md) - scheduling, HTTP requests, and safely jumping between threads.
- [`LuaPluginTutorial.md`](LuaPluginTutorial.md) / [`JavaScriptPluginTutorial.md`](JavaScriptPluginTutorial.md) /
  [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md) /
  [`CSScriptPluginTutorial.md`](CSScriptPluginTutorial.md) - the same walkthrough for the other engines.
