### Guide
Writing your first C# plugin (Module) for Fougerite

### Description
This is a beginner-friendly, step-by-step tutorial for writing a compiled C# plugin ("Module") for
Fougerite. Unlike Python/JS/Lua script plugins, C# modules are compiled into a `.dll` beforehand and are
loaded directly into the server process. If you're looking for the full technical reference instead of a
walkthrough, see [`Classes/BasePlugin.md`](Classes/BasePlugin.md) (base class + reload behavior) and the
[Hooks reference](Hooks/README.md).

### 1. Requirements
- **Microsoft Visual Studio** (2013 or newer - Community Edition works fine) **or JetBrains Rider** - both
  are full-featured .NET IDEs and either works fine for writing a Fougerite module; use whichever you're
  more comfortable with. The rest of this guide shows Visual Studio's menu names, but the equivalent
  actions (add reference, set target framework, build) exist in Rider too.
- **`Fougerite.dll`** - required by every C# module, contains all Fougerite types (`Module`, `Hooks`,
  `Player`, `Server`, `World`, `Entity`, ...).
- Extra DLLs (optional, only needed for more advanced plugins), both found in your Rust Legacy server's
  `rust_server_Data\Managed` folder:
  - **`UnityEngine.dll`** - needed if you work with `Vector3`/`Quaternion`/rotations or anything else that's
    part of the Unity engine.
  - **`Assembly-CSharp.dll`** - contains all of Rust's own game code (items, structures, NPCs, etc). Needed
    for more advanced plugins that go beyond what `Fougerite.dll` already wraps for you.

### 2. Creating the project
1. In Visual Studio, create a new project of type **Class Library**.
2. In the **Solution Explorer**, right-click **References** -> **Add Reference...** and add:
   - `Fougerite.dll` (always).
   - `UnityEngine.dll` and `Assembly-CSharp.dll` (only if you need them, see above).
3. **Important:** Fougerite runs on a custom Mono build that targets **.NET Framework 3.5**. Right-click
   your project -> **Properties** -> under **Application**, set **Target framework** to **.NET Framework
   3.5**. If you build against a newer framework, your plugin's assembly may fail to load or behave
   unexpectedly on the server.

### 3. The Module skeleton
Every C# module must inherit from `Fougerite.Module` (which itself derives from
[`BasePlugin`](Classes/BasePlugin.md), so you also get timers, logging, dictionaries, etc. for free - see
that page for the full list of inherited members).

Add `using Fougerite;` at the top of your file so you can use its types without fully qualifying them.

```csharp
using Fougerite;

namespace MyFirstPlugin
{
    public class MyFirstPlugin : Module
    {
        public override string Name
        {
            get { return "MyFirstPlugin"; }
        }

        public override string Author
        {
            get { return "YourName"; }
        }

        public override string Description
        {
            get { return "My very first Fougerite plugin."; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        public override void Initialize()
        {
            // Code that runs once, when the plugin is loaded.
            // This is the right place to open files (IniParsers, StreamWriters, ...) and to subscribe
            // to Hooks.
        }

        public override void DeInitialize()
        {
            // Code that runs once, when the plugin is unloaded/reloaded.
            // This is the right place to close files and unsubscribe from every Hook you subscribed to
            // in Initialize(), otherwise a reload will leave "ghost" handlers behind (see BasePlugin.md).
        }
    }
}
```

`Name`, `Author`, `Description` and `Version` are just metadata, they don't need any further explanation.
`Initialize()` and `DeInitialize()` are where the actual logic of your plugin begins and ends.

### 4. Hooks: reacting to events
Hooks let your plugin run code whenever something happens in-game - a player connecting, placing a
structure, writing something in chat, executing a command, etc. The full list (with every argument and
property documented) lives in the [Hooks reference](Hooks/README.md); this section only covers the basics.

**The pattern is always the same:**
- In `Initialize()`, subscribe your method to the hook with `+=`.
- In `DeInitialize()`, unsubscribe the *same* method with `-=`.

```csharp
public override void Initialize()
{
    Hooks.OnCommand += HandleCommand;
}

public override void DeInitialize()
{
    Hooks.OnCommand -= HandleCommand;
}
```

Notice that `Initialize` uses `+=` and `DeInitialize` uses `-=` - this is important, if you forget to
unsubscribe your plugin may keep running old handlers after being reloaded.

#### Example: `OnCommand`
`OnCommand` is called every time a player (or the console) types something in chat starting with `/`, e.g.
`/help` or `/starterkit`. Full reference: [`On_Command`](Hooks/Player/On_Command.md).

```csharp
public void HandleCommand(Fougerite.Player player, string cmd, string[] args)
{
    // "/spawn battlekit sniper" -> cmd = "spawn", args = ["battlekit", "sniper"]
    if (cmd == "test")
    {
        player.Message("It works!");
    }
}
```
- `player` - the `Player` who ran the command.
- `cmd` - the command name, without the leading `/`.
- `args` - everything typed after the command name, split by spaces.

#### Example: `OnBlueprintUse`
Called when a player tries to learn a blueprint - whether or not they already know it. Full reference:
[`On_BlueprintUse`](Hooks/Items/On_BlueprintUse.md).

```csharp
public override void Initialize()
{
    Hooks.OnBlueprintUse += HandleBlueprintUse;
}

public override void DeInitialize()
{
    Hooks.OnBlueprintUse -= HandleBlueprintUse;
}

public void HandleBlueprintUse(Fougerite.Player player, BPUseEvent bpUseEvent)
{
    if (bpUseEvent.ItemName == "Explosive Charge")
    {
        player.Message("You are not allowed to learn the Explosive Charge blueprint!");
        bpUseEvent.Cancel = true;
    }
}
```
- `bpUseEvent.ItemName` - the name of the blueprint item being learned.
- `bpUseEvent.Cancel` - set to `true` to cancel learning the blueprint.
- `bpUseEvent.DataBlock` - advanced info about the event (rarely needed for simple plugins).

#### Example: `OnChat`
Called when a player writes a normal chat message (i.e. one that does **not** start with `/`). Full
reference: [`On_Chat`](Hooks/Player/On_Chat.md).

```csharp
public override void Initialize()
{
    Hooks.OnChat += ChatHandler;
}

public override void DeInitialize()
{
    Hooks.OnChat -= ChatHandler;
}

public void ChatHandler(Fougerite.Player player, ref ChatString chatString)
{
    if (chatString.ToString().Contains("noob"))
    {
        chatString.NewText = "   "; // empties the message, so it won't show up in chat
        player.Message("Please don't use that word!");
    }
}
```
- `chatString.ToString()` (or `chatString.OriginalMessage`) - the original text the player typed.
- `chatString.NewText` - set this to replace/blank out what actually gets sent/broadcast.

> These three hooks are only a small sample. See the [full Hooks reference](Hooks/README.md) for every
> available event, grouped by category (Player, NPC, Entities, Items, Combat, World, Server), each with the
> exact delegate signature and working examples in C#/Python/JS/Lua.

### 5. Building and installing your plugin
1. Build your project (`Build` -> `Build Solution`). This produces a `.dll` in your project's `bin\Debug`
   (or `bin\Release`) folder.
2. On the server, create a folder for your plugin under the server's `Modules` folder, and the folder name
   **must match the DLL's file name** (without extension). For example, for the `MyFirstPlugin` project
   above:
   ```
   Modules\MyFirstPlugin\MyFirstPlugin.dll
   ```
3. Open `Fougerite.cfg` and register your module under the `[Modules]` section, in the form
   `PluginClassName=FolderName`:
   ```ini
   [Modules]
   MyFirstPlugin=MyFirstPlugin
   ```
   The value after `=` is passed to your plugin as `ModuleFolder` (relative to the public/server folder),
   handy if your plugin needs its own subfolder for config/data files.
4. Restart the server (or use the server's reload command, if available) to load your module. Check the
   console/log for a line like:
   ```
   [Modules] Module MyFirstPlugin v1.0 (by YourName) initiated.
   ```
   If something goes wrong, Fougerite will log the exception with your plugin's name so you can pinpoint
   the failing line.

### 6. Where to go next
- [Hooks reference](Hooks/README.md) - every hook/event Fougerite exposes.
- [`Classes/BasePlugin.md`](Classes/BasePlugin.md) - everything your `Module` inherits (timers, logging,
  dictionaries, cross-plugin messaging), plus how the custom single-domain Mono build reloads C# modules.
- [`Classes/Player.md`](Classes/Player.md), [`Classes/Server.md`](Classes/Server.md),
  [`Classes/Entity.md`](Classes/Entity.md), [`Classes/Sleeper.md`](Classes/Sleeper.md) - the core game
  object wrappers you'll use the most.
- [`Classes/Timers.md`](Classes/Timers.md) - scheduling delayed/repeated logic, and the crucial difference
  between the main-thread Normal Timer and the background-thread System Timer.
- [`Classes/Web.md`](Classes/Web.md) / [`Classes/Loom.md`](Classes/Loom.md) - HTTP requests and safely
  jumping between threads.
