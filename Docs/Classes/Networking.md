### Class
`Fougerite.WinHttpClient` / `Fougerite.ScriptWebSocket` / `Fougerite.MySQLConnector` / `Fougerite.SQLiteConnector`

### Description
Four singleton/instance helpers for talking to the outside world or a database from a plugin without pulling
in extra dependencies. `WinHttpClient` is a lower-level, native-WinHTTP alternative to [`Web`](Web.md) (mostly
useful on hosts where `Web`'s managed `HttpWebRequest` misbehaves). `ScriptWebSocket` is a full WebSocket
client (also WinHTTP-based) with hook-driven events, primarily aimed at script plugins that can't easily keep
raw socket/thread state themselves. `MySQLConnector`/`SQLiteConnector` are thin wrappers around `MySql.Data`/
`System.Data.SQLite` so plugins (especially script plugins) get an easy way to persist to a real database
instead of (or alongside) [`DataStore`](Data.md).

### WinHttpClient
`WinHttpClient.GetInstance()` returns the singleton.
- `MakeRequest(string url, Action<int, string> callback, string method = "GET", string inputBody = null, Dictionary<string, string> additionalHeaders = null, string contentType = "application/x-www-form-urlencoded", float timeout = 0f)`
  - fire-and-forget request on a `ThreadPool` thread; `callback(statusCode, responseBody)` runs on that same
  background thread - use `Loom.QueueOnMainThread` inside it if you need to touch Unity/`World` state.
- `GetBlocking(string url, float timeout = 5f)` / `PostBlocking(string url, string inputBody, string contentType = "...", float timeout = 5f)`
  - synchronous helpers; only call these from a background thread/timer, never the main thread.
- `DownloadFileBlocking(string url, string destinationPath, float timeout = 30f)` (+ an `out string error`
  overload) / `DownloadFileAsync(string url, string destinationPath, Action<bool, string> onComplete, float timeout = 30f)`.
- `UploadFileBlocking(string url, string filePath, float timeout = 30f)` (+ `out string error` overload) /
  `UploadFileAsync(string url, string filePath, Action<bool, string> onComplete, float timeout = 30f)`.
- SSL certificate validation is disabled and the response size is capped (10 MB by default, adjustable via a
  class field) - treat this as a convenience client for trusted endpoints, not a hardened HTTP stack.

### ScriptWebSocket
A per-connection, disposable WebSocket client - create one instance per socket you want open.
`new ScriptWebSocket(string pluginName, string socketId, string url, int bufferSize = 32768)`.
- `Connect()` - connects asynchronously on a background thread; results arrive via hooks (see below).
- `Send(string message)` - sends a text frame. Safe to call from any thread.
- `Close()` / `Close(string errorMessage)` - closes the socket (optionally reporting an error to the plugin).
- `Dispose()` - releases the underlying WinHTTP handles; always dispose when done with a socket.
- `SocketId` / `PluginName` / `Url` / `BufferSize` / `IsConnected` - read-only properties.
- Not thread-safe as a whole: only call `Connect`/`Close`/`Dispose` from one thread (ideally the main thread);
  `Send` is the only member explicitly safe to call from any thread.
- Connection lifecycle and incoming messages are delivered through
  [`On_WebSocketConnected` / `On_WebSocketMessage` / `On_WebSocketClosed` / `On_WebSocketError`](../Hooks/Server/On_WebSocket.md)
  (all `WebSocketEvent`s carrying the matching `SocketId`/`PluginName` so you can route callbacks when running
  multiple sockets at once).

### MySQLConnector
`MySQLConnector.GetInstance` (property, not a method) returns the singleton.
- `Connect(string ip, string database, string username, string passwd, string extraarg = "")` - builds the
  `MySqlConnection` (check `Connection == null` before calling this again).
- `OpenConnection()` / `CloseConnection()` - open/close the connection; logs a friendly message for common
  errors (unreachable server, bad credentials).
- `ExecuteNonQuery(string query)` - runs a query with no result set (INSERT/UPDATE/DELETE/DDL), returns
  whether it succeeded.
- `ExecuteQuery(string query, Dictionary<string, object> parameters = null)` - runs a `SELECT`, returns a
  `Dictionary<string, object>` of column name -> value (note: only the **last** row read survives, since each
  row's columns are added with the same keys - fine for single-row lookups, roll your own reader loop via
  `Connection`/`CreateMysqlCommand()` if you need every row).
- `CreateMysqlCommand()` - a blank `MySqlCommand` for anything the helpers above don't cover.
- `Connection` - the raw `MySqlConnection`.
- There's no built-in async query API (the underlying driver doesn't support a usable one on this old Mono
  build) - run `MySQLConnector` calls on a background thread/timer yourself if you don't want to block the
  main thread.

### SQLiteConnector
`SQLiteConnector.GetInstance` (property) returns the singleton; the database file lives at
`Save/FougeriteSQL.sqlite` (`SQLitePath`), created automatically on first use.
- `Connect(string extraarguments = ";Version=3;New=False;Compress=True;Foreign Keys=True;")` - builds the
  `SQLiteConnection` (check `Connection == null` first).
- `Connection` - the raw `SQLiteConnection`, once connected.
- `CreateSQLiteCommand(string command)` / `CreateSQLiteCommand(string command, SQLiteConnection con)` -
  convenience factories for `SQLiteCommand`.

### Example - C# (polling a REST endpoint every 30 seconds)
```csharp
public override void Initialize()
{
    Util.GetUtil().CreateParallelSystemTimer("StatusCheck", 30000, null).Start();
}

public void StatusCheckCallback(ATimedEvent e)
{
    WinHttpClient.GetInstance().MakeRequest("https://example.com/status", (status, body) =>
    {
        Logger.Log($"Status endpoint returned {status}: {body}");
    });
}
```

### Example - C# (a simple WebSocket client)
```csharp
private ScriptWebSocket _socket;

public override void Initialize()
{
    Hooks.OnWebSocketConnected += OnSocketConnected;
    Hooks.OnWebSocketMessage += OnSocketMessage;

    _socket = new ScriptWebSocket(Name, "main", "wss://example.com/ws");
    _socket.Connect();
}

public override void DeInitialize()
{
    Hooks.OnWebSocketConnected -= OnSocketConnected;
    Hooks.OnWebSocketMessage -= OnSocketMessage;
    _socket?.Dispose();
}

public void OnSocketConnected(WebSocketEvent e)
{
    if (e.PluginName == Name) _socket.Send("{\"type\":\"hello\"}");
}

public void OnSocketMessage(WebSocketEvent e)
{
    if (e.PluginName == Name) Logger.Log("Received: " + e.Message);
}
```

### Example - C# (storing a kill count in SQLite)
```csharp
public override void Initialize()
{
    SQLiteConnector sql = SQLiteConnector.GetInstance;
    if (sql.Connection == null)
    {
        sql.Connect().Open();
        sql.CreateSQLiteCommand("CREATE TABLE IF NOT EXISTS Kills (SteamID TEXT PRIMARY KEY, Count INTEGER)",
            sql.Connection).ExecuteNonQuery();
    }
}
```

See also: [`Web`](Web.md) · [`Data`](Data.md) · [`Loom`](Loom.md) ·
[`On_WebSocket`](../Hooks/Server/On_WebSocket.md)
