### Method
`On_PluginMessage`

### Description
Runs when a plugin sends a message to another plugin, enabling simple inter-plugin communication without
needing direct references between plugins.

### C# Event
```csharp
public static event PluginMessageHandlerDelegate OnPluginMessage;
public delegate void PluginMessageHandlerDelegate(PluginMessageEvent e);
```

### Argument(s)
- `PluginMessageEvent PluginMessageEvent`

### Properties/Methods
- `PluginMessageEvent.SenderName` - The name of the sending plugin.
- `PluginMessageEvent.ReceiverName` - The name of the intended receiving plugin.
- `PluginMessageEvent.Message` - The message payload (any object).
- `PluginMessageEvent.Response` - Set this to return a response to the sender.
- `PluginMessageEvent.Cancelled` - Whether the message is cancelled.
- `PluginMessageEvent.Cancel()` - Cancels/blocks the message delivery.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPluginMessage += MessageHandler;
}

public override void DeInitialize()
{
    Hooks.OnPluginMessage -= MessageHandler;
}

public void MessageHandler(PluginMessageEvent e)
{
    if (e.ReceiverName == "MyPlugin")
    {
        Logger.Log("Received message from " + e.SenderName + ": " + e.Message);
        e.Response = "ACK";
    }
}
```

#### Python
```python
def On_PluginMessage(self, PluginMessageEvent):
    if PluginMessageEvent.ReceiverName == "MyPlugin":
        Util.Log("Received message from " + PluginMessageEvent.SenderName)
        PluginMessageEvent.Response = "ACK"
```

#### JavaScript
```javascript
function On_PluginMessage(PluginMessageEvent)
{
    if (PluginMessageEvent.ReceiverName === "MyPlugin")
    {
        Util.Log("Received message from " + PluginMessageEvent.SenderName);
        PluginMessageEvent.Response = "ACK";
    }
}
```

#### Lua
```lua
function On_PluginMessage(PluginMessageEvent)
    if PluginMessageEvent.ReceiverName == "MyPlugin" then
        Util.Log("Received message from " .. PluginMessageEvent.SenderName)
        PluginMessageEvent.Response = "ACK"
    end
end
```
