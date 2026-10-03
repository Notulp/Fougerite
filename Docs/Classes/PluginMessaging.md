### Class
`Fougerite.Tools.PluginMessaging` / `Fougerite.Tools.PluginMessageResult` / `Fougerite.Events.PluginMessageEvent` /
`Fougerite.Events.PluginMessageResponse`

### Description
`PluginMessaging` is Fougerite's API for Inter-Plugin Communication (IPC): it lets one plugin send an arbitrary
object to another plugin by name, without either plugin needing a reference/dependency on the other (they only
need to agree on a message contract). It's internally just a thin, convenient wrapper around the
`On_PluginMessage` hook (see [`On_PluginMessage`](../Hooks/Server/On_PluginMessage.md)) - the receiving plugin
implements `On_PluginMessage(PluginMessageEvent)`, reads `Message`, optionally sets `Response` and/or calls
`Cancel()`, and the sender gets a `PluginMessageResult` back describing what happened.

### PluginMessaging
Static class with two ways to send a message:
- `Send(string sender, string targetName, object message)` - synchronous; dispatches `Hooks.PluginMessage`
  immediately on the calling thread and returns the `PluginMessageResult` once the target plugin's handler
  (if any) has run. Only call this from the main/Unity thread, since it will run the target's handler inline.
- `SendAsync(string sender, string targetName, object message, Action<PluginMessageResult> callback, bool runInThreadPool = true)`
  - asynchronous; by default (`runInThreadPool = true`) the dispatch happens on a `ThreadPool` thread so the
  caller never blocks, and `callback` is always invoked back on the Unity main thread (via `Loom`), so it's
  safe to touch `World`/Unity state inside the callback. Pass `runInThreadPool: false` to dispatch inline on
  the calling thread instead (callback is still marshalled to the main thread).

### PluginMessageResult
The outcome returned by both `Send` and `SendAsync`'s callback:
- `Status` - a `PluginMessageResponse` describing what happened.
- `Event` - the underlying `PluginMessageEvent` that was dispatched, so you can read `Response`/`Cancelled`.

### PluginMessageResponse (enum)
- `Success` - the message was delivered and not rejected.
- `TargetNotFound` - no plugin with that name is known to the `PluginLoader`.
- `TargetDisabled` - the target plugin exists but isn't currently `Loaded`.
- `Error` - an exception was thrown while dispatching the message (check the log for details).
- `Rejected` - the target explicitly called `PluginMessageEvent.Cancel()`.

### PluginMessageEvent
The payload passed to both the sender (wrapped in `PluginMessageResult.Event`) and the receiver's
`On_PluginMessage` handler:
- `SenderName` - the name the sender passed in.
- `ReceiverName` - the name the message was addressed to.
- `Message` - the raw payload object. Cast/inspect it according to whatever contract sender and receiver agreed
  on (commonly a small DTO class or a `Dictionary<string, object>`).
- `Response` - get/set; the receiver sets this to hand data back to the sender.
- `Cancelled` - `true` once the receiver has called `Cancel()`.
- `Cancel()` - called by the receiver to reject the message; the sender then sees
  `PluginMessageResponse.Rejected`.

### Example - C# (receiving plugin)
```csharp
public override void Initialize()
{
    Hooks.OnPluginMessage += OnPluginMessage;
}

public override void DeInitialize()
{
    Hooks.OnPluginMessage -= OnPluginMessage;
}

public void OnPluginMessage(PluginMessageEvent e)
{
    if (e.ReceiverName != Name)
    {
        return;
    }

    if (e.Message is string && (string)e.Message == "ping")
    {
        e.Response = "pong";
    }
    else
    {
        e.Cancel(); // Reject anything we don't understand.
    }
}
```

### Example - C# (sending plugin, synchronous)
```csharp
PluginMessageResult result = PluginMessaging.Send(Name, "OtherPlugin", "ping");
if (result.Status == PluginMessageResponse.Success)
{
    Logger.Log("OtherPlugin replied: " + result.Event.Response);
}
else
{
    Logger.Log("Message to OtherPlugin failed: " + result.Status);
}
```

### Example - C# (sending plugin, asynchronous)
```csharp
PluginMessaging.SendAsync(Name, "OtherPlugin", "ping", result =>
{
    // This callback always runs on the Unity main thread, so World/Unity state is safe to use here.
    if (result.Status == PluginMessageResponse.Success)
    {
        Logger.Log("OtherPlugin replied: " + result.Event.Response);
    }
});
```

### Example - Python (receiving plugin)
```python
def On_PluginMessage(self, PluginMessageEvent):
    if PluginMessageEvent.ReceiverName != self.Name:
        return
    if PluginMessageEvent.Message == "ping":
        PluginMessageEvent.Response = "pong"
    else:
        PluginMessageEvent.Cancel()
```

See also: [`On_PluginMessage`](../Hooks/Server/On_PluginMessage.md) · [`PluginLoaders`](PluginLoaders.md) ·
[`BasePlugin`](BasePlugin.md)
