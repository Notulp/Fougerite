### Class
`Fougerite.ChatString` / `Fougerite.Flood` / `Fougerite.JsonAPI` / `Fougerite.ReflectionExtensions` /
`Fougerite.SuperFastHashUInt16Hack` / `Fougerite.GlobalPluginCollector` / `Fougerite.Stopper` /
`Fougerite.CoroutineHost` / `Fougerite.Icalls` / `Fougerite.ItemsBlocks` / `Fougerite.AssetBundleLoader` /
`Fougerite.RustPPExtension`

### Description
A grab-bag of smaller, single-purpose helper classes that don't fit anywhere else: the `On_Chat` message
wrapper, Fougerite's internal connection-flood guard, a JSON convenience API for script plugins, reflection
extension methods, a fast non-cryptographic hash, a serialized asset-bundle download queue, a tiny perf-timer,
and a couple of legacy/internal pieces kept for completeness.

### ChatString
The argument type for [`On_Chat`](../Hooks/Player/On_Chat.md) - wraps the message a player typed and lets a
plugin rewrite or suppress it.
- `OriginalMessage` - the text the player originally typed (read-only).
- `NewText` (get/set) - the (possibly already-modified-by-another-plugin) outgoing text. Setting it to
  `null`/empty/whitespace doesn't actually clear the message (Rust rejects empty chat strings) - it instead
  replaces it with a blob of spaces and marks the chat line as effectively cancelled.
- `Contains(string str)` - checks the **original** message.
- `Replace(string find, string replacement)` / `Substring(int start, int length)` - convenience wrappers
  around the original message.
- Implicitly convertible to/from `string` (so `ChatString cs = "test";` and `string s = cs;` both work).

### Flood
Internal per-IP connection-attempt counter backing `Bootstrap.FloodConnections` (max connections/IP/second);
you'll see it referenced in logs/`Hooks.FloodChecks` but wouldn't normally instantiate it from a plugin.
- `new Flood(string ip)` - starts tracking an IP, with a 3-second auto-clear window.
- `Increase()` - bumps the counter.
- `Amount` - current attempt count.
- `Reset()` - restarts the 3-second window.
- `Stop()` - stops tracking (kills the internal timer).

### JsonAPI
`JsonAPI.GetInstance` (property) returns the singleton; a thin, script-plugin-friendly wrapper around
Newtonsoft.Json (C# plugins can usually just use `Newtonsoft.Json` directly).
- `SerializeObjectToJson(object target)` / `DeSerializeJsonToObject(string target)` /
  `DeSerializeJsonToObject<T>(string target)`.
- `CreateJsonObject()` / `CreateJsonObject(object[] objects)` / `CreateJsonArray()` / `CreateJProperty(...)` -
  build `JObject`/`JArray`/`JProperty` (Newtonsoft's `JToken` tree) without importing the namespace yourself.
- `CreateJsonSerializer()` - a `JsonSerializer` pre-configured with UTC/ISO date handling.
- `CreateJsonTextReader(string s)` / `CreateJsonWriter(StringBuilder sb, bool idented = false)` /
  `CreateStringReader(string json)` / `CreateStringBD()` - low-level reader/writer factories.
- `GenerateJSchema(Type specifiedclasstype)` / `CreateJSchema(string json)` / `CreateJSchemaGenerator()` /
  `CreateJSchemaVReader(JsonReader reader)` - JSON Schema generation/validation helpers.
- `SerializeXmlNode(XmlNode target)` / `DeserializeXmlNode(string target)` - XML<->JSON conversion.

### ReflectionExtensions
Static extension methods (on `object`/`Type`) that power most of the dynamic member access used by script
plugin engines - handy directly for any plugin that needs to read/call something not exposed as public API:
- `obj.CallMethod(string methodName, params object[] args)` - invokes an instance method (public or
  non-public) by name via reflection.
- `obj.GetFieldValue(string fieldName)` / `obj.SetFieldValue(string fieldName, object newValue)` - gets/sets a
  field **or** property by name (checked in that order), searching up the base-type chain.
- `obj.GetFieldValueChain(params string[] args)` - chains `GetFieldValue` calls, e.g.
  `obj.GetFieldValueChain("a", "b", "c")` is `obj.a.b.c`.
- `classType.CallStaticMethod(string methodName, params object[] args)` /
  `classType.GetStaticFieldValue(string fieldName)` / `classType.SetFieldValueValue(string fieldName, object newValue)`
  - the static-member equivalents.
- `obj.CallMethodOnBase(...)` - calls a method resolved from a *base* type, even if it's hidden/overridden on
  the instance's actual runtime type (built using `DynamicMethod`/`ILGenerator`); an advanced, rarely-needed
  escape hatch.

### SuperFastHashUInt16Hack
`SuperFastHashUInt16Hack.Hash(byte[] dataToHash)` - a fast, non-cryptographic 32-bit hash (Paul Hsieh's
"SuperFastHash"), useful for cheap content fingerprints/dedup keys where `MD5`/`SHA` would be overkill.

### Stopper
A tiny `IDisposable` perf-timer: wrap a block in `using (new Stopper("MyPlugin", "DoWork", 0.05f)) { ... }`
and a warning is logged automatically if the block took longer than the threshold (defaults to 100ms).

### CoroutineHost
`CoroutineHost.Instance` - a singleton `MonoBehaviour` on its own persistent `GameObject`
(`Fougerite_CoroutineHost`), used internally (e.g. by [`AssetBundleLoader`](#assetbundleloader) below) to run
Unity coroutines that shouldn't be tied to any particular plugin's/Fougerite's own lifecycle. Mostly relevant
if you need `StartCoroutine` from code that isn't itself a `MonoBehaviour`.

### Icalls
`public sealed class Icalls` - the managed side of Fougerite's custom `mono.dll`'s internal calls, used to
load/unload a plugin assembly into its own isolated space within the single AppDomain the custom Mono build
runs (`mono_fg_load_plugin`/`mono_fg_unload_plugin`). This is what makes true hot-reload possible without a
full AppDomain per plugin. Internal plumbing - not meant to be called directly from a plugin.

### ItemsBlocks
`public class ItemsBlocks : List<ItemDataBlock>` - a small `List<ItemDataBlock>` subclass adding
`Find(string str)`, a case-insensitive-by-name lookup (e.g. `Find("Large Medkit")`), used internally wherever
Fougerite needs to search the game's item dictionary by name.

### AssetBundleLoader
`AssetBundleLoader.GetAssetBundleLoader()` returns the singleton. Solves a RustBuster-specific problem: if
several plugins each call `WWW.LoadFromCacheOrDownload` independently right as the loading screen ends, they
all spike the managed heap at once. Instead:
- `LoadBundle(string path, int version, Action<AssetBundle> done)` - queues a bundle load; Fougerite processes
  the queue one bundle at a time (on the [`CoroutineHost`](#coroutinehost)/`Loom` coroutine runner) and invokes
  `done` on the main thread with the loaded `AssetBundle` (or `null` on failure) once it's this request's turn.

### RustPPExtension *(obsolete)*
A compatibility bridge into the bundled `RustPP` plugin's admin/permission/social systems (friends, mutes,
god mode, InstaKO, admin flags) for plugins that were written depending on RustPP directly. The author's own
docs comment calls its existence a mistake kept only for backwards compatibility - **new plugins should build
their own permission/social data (e.g. via [`DataStore`](Data.md)) instead** of depending on this class.

### Example - C# (rewriting chat with a Stopper around it)
```csharp
public void ChatHandler(Player player, ref ChatString message)
{
    using (new Stopper(Name, "OnChat", 0.02f))
    {
        if (message.Contains("shop"))
        {
            message.NewText = message.OriginalMessage + " [Try /shop for items!]";
        }
    }
}
```

### Example - C# (loading a custom asset bundle without blocking other plugins)
```csharp
public override void Initialize()
{
    AssetBundleLoader.GetAssetBundleLoader().LoadBundle("file://" + ModuleFolder + "/mymap.unity3d", 1, bundle =>
    {
        if (bundle == null)
        {
            Logger.LogError("Failed to load asset bundle.");
            return;
        }

        Logger.Log("Asset bundle loaded.");
    });
}
```

See also: [`BasePlugin`](BasePlugin.md) · [`Util`](Util.md) · [`Loom`](Loom.md) ·
[`On_Chat`](../Hooks/Player/On_Chat.md)
