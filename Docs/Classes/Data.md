### Class
`Fougerite.Data` / `Fougerite.DataStore` / `Fougerite.SerializableDictionary<KT, VT>`

### Description
`DataStore` is Fougerite's built-in, thread-safe persistent key/value store - the recommended way for a plugin
to save arbitrary data across server restarts without rolling its own file I/O. `Data` is an older
singleton of mostly string/number parsing helpers (many of its original responsibilities - shared tables,
plugin config files - have since been superseded by `DataStore` and the module system, and are marked
`[Obsolete]`). `SerializableDictionary<KT, VT>` is a legacy XML-serializable dictionary, obsolete since
everything moved to JSON.

### DataStore
`DataStore.GetInstance()` returns the singleton. Data is organized into named **tables**, each an independent
key/value map (think "one `Hashtable` per table name"); everything is guarded by a `ReaderWriterLock` so it's
safe to use from multiple threads/timers at once.
- `Add(string tablename, object key, object val)` - inserts/overwrites a key, creating the table if needed.
- `Get(string tablename, object key)` - returns the value, or `null` if the table/key doesn't exist.
- `ContainsKey(string tablename, object key)` / `ContainsValue(string tablename, object val)`.
- `Remove(string tablename, object key)` - deletes a single key.
- `Flush(string tablename)` - deletes an entire table.
- `Wipe(List<string> tablesToSkip = null, bool cleanBans = false)` - deletes every table except the ones
  listed in `tablesToSkip` (and, unless `cleanBans` is `true`, always preserves the `"Ips"`/`"Ids"` ban
  tables); returns the list of table names that were actually removed.
- `Count(string tablename)` - number of keys in a table.
- `Keys(string tablename)` / `Values(string tablename)` - snapshot arrays of a table's keys/values.
- `GetTable(string tablename)` - a copied `Hashtable` of a whole table.
- `GetTableNames()` - every table name currently in use.
- `Save()` / `Load()` - persist/restore the entire store to/from `FougeriteDatastore.ds` as JSON (called
  automatically by Fougerite; `Vector2`/`Vector3`/`Quaternion`/`Color`/`Rect` values are supported natively and
  round-trip correctly, even as dictionary keys).
- `AddJsonConverter(JsonConverter)` / `RemoveJsonConverter(JsonConverter)` / `ClearJsonConverters()` /
  `GetJsonConverters()` / `JsonSerializerSettings` - register custom Newtonsoft `JsonConverter`s (or tweak the
  settings directly) if you store a custom type `DataStore` can't serialize out of the box.
- `ToIni(string tablename, IniParser ini)` / `FromIni(IniParser ini)` - converts a table to/from an `.ini`
  file, for interop with older config-file-based workflows.
- `Hashtable` *(obsolete)* - raw, lock-free access to the underlying `Hashtable`; prefer the methods above for
  thread safety.

> Values are looked up by key equality after an internal "stringify" step for Unity math types - plain
> `string`/`int`/`ulong`/etc. keys work exactly like a normal `Hashtable`, you don't need to do anything special.

### Data
`Data.GetData()` returns the singleton. Mostly small, null-safe string/number helpers useful from script
plugins (Python/JS/Lua) that don't have the full .NET `string`/`Convert` API as conveniently:
- Parsing: `ToInt`/`ToFloat`/`ToDouble`/`ToUlong`/`Tolong`/`ToBool`/`ToString`.
- Checks: `IsInt`/`IsFloat`/`IsNumeric`/`IsAlpha`/`IsAlphaNumeric`/`IsStringNullOrEmpty`/`IsStringNullOrWhiteSpace`.
- String manipulation: `ToLower`/`ToUpper`/`Trim`/`Replace`/`StringContains`/`StripColors`/`Substring`/`StrLen`/
  `SplitQuoteStrings` (splits on spaces, keeping `"quoted substrings"` together).
- Arrays: `Split`/`Join`/`CleanArray` (drops empty/trims)/`SliceArray`/`ArrayContains` (case-insensitive).
- Math: `RoundUp`/`RoundDown`/`Round(value, even)`.
- `AddTableValue`/`GetTableValue` *(obsolete)* - thin forwards to `DataStore.Add`/`DataStore.Get`; call
  `DataStore` directly instead.
- `chat_history`/`chat_history_username`/`Fougerite_shared_data`/`inifiles` *(obsolete)* - use
  `Util.ChatHistory` + [`PlayerCache`](Caches.md) and `DataStore` instead.

### SerializableDictionary\<KT, VT\> *(obsolete)*
A `Dictionary<KT, VT>` implementing `IXmlSerializable` so it can round-trip through `XmlSerializer`. Predates
`DataStore`'s JSON persistence; there is no reason to use this in new code.

### Example - C# (per-player kill counter that survives restarts)
```csharp
public void OnPlayerKilled(Player victim, HurtEvent e)
{
    if (!e.AttackerIsPlayer) return;

    Player killer = (Player)e.Attacker;
    DataStore ds = DataStore.GetInstance();
    int kills = (int)(ds.Get("KillCounts", killer.UID) ?? 0);
    ds.Add("KillCounts", killer.UID, kills + 1);
}

public void OnServerShutdown()
{
    DataStore.GetInstance().Save();
}
```

### Example - Python (storing a Vector3 home location)
```python
def On_Chat(self, Player, Message):
    if Message == "/sethome":
        DataStore.GetInstance().Add("Homes", Player.UID, Player.Location)
        Player.Message("Home set!")
```

See also: [`Caches`](Caches.md) · [`Util`](Util.md)
