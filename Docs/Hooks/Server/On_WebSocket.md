### Method
`On_WebSocketMessage` / `On_WebSocketConnected` / `On_WebSocketClosed` / `On_WebSocketError`

### Description
These four hooks run for a WebSocket client connection managed by Fougerite: when a message is received,
when the connection is established, when it's closed, and when an error occurs, respectively.

### C# Event
```csharp
public static event WebSocketEventHandlerDelegate OnWebSocketMessage;
public static event WebSocketEventHandlerDelegate OnWebSocketConnected;
public static event WebSocketEventHandlerDelegate OnWebSocketClosed;
public static event WebSocketEventHandlerDelegate OnWebSocketError;
public delegate void WebSocketEventHandlerDelegate(WebSocketEvent e);
```

### Argument(s)
- `WebSocketEvent WebSocketEvent`

### Properties/Methods
- `WebSocketEvent.PluginName` - The name of the plugin that owns the socket.
- `WebSocketEvent.SocketId` - The identifier of the socket.
- `WebSocketEvent.Message` - The received message (for `On_WebSocketMessage`).
- `WebSocketEvent.ErrorMessage` - The error message (for `On_WebSocketError`).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnWebSocketMessage += MessageHandler;
    Hooks.OnWebSocketConnected += ConnectedHandler;
    Hooks.OnWebSocketClosed += ClosedHandler;
    Hooks.OnWebSocketError += ErrorHandler;
}

public void MessageHandler(WebSocketEvent e)
{
    Logger.Log("WebSocket message: " + e.Message);
}

public void ConnectedHandler(WebSocketEvent e)
{
    Logger.Log("WebSocket connected: " + e.SocketId);
}

public void ClosedHandler(WebSocketEvent e)
{
    Logger.Log("WebSocket closed: " + e.SocketId);
}

public void ErrorHandler(WebSocketEvent e)
{
    Logger.Log("WebSocket error: " + e.ErrorMessage);
}
```

#### Python
```python
def On_WebSocketMessage(self, WebSocketEvent):
    Util.Log("WebSocket message: " + WebSocketEvent.Message)

def On_WebSocketConnected(self, WebSocketEvent):
    Util.Log("WebSocket connected: " + WebSocketEvent.SocketId)

def On_WebSocketClosed(self, WebSocketEvent):
    Util.Log("WebSocket closed: " + WebSocketEvent.SocketId)

def On_WebSocketError(self, WebSocketEvent):
    Util.Log("WebSocket error: " + WebSocketEvent.ErrorMessage)
```

#### JavaScript
```javascript
function On_WebSocketMessage(WebSocketEvent)
{
    Util.Log("WebSocket message: " + WebSocketEvent.Message);
}

function On_WebSocketConnected(WebSocketEvent)
{
    Util.Log("WebSocket connected: " + WebSocketEvent.SocketId);
}

function On_WebSocketClosed(WebSocketEvent)
{
    Util.Log("WebSocket closed: " + WebSocketEvent.SocketId);
}

function On_WebSocketError(WebSocketEvent)
{
    Util.Log("WebSocket error: " + WebSocketEvent.ErrorMessage);
}
```

#### Lua
```lua
function On_WebSocketMessage(WebSocketEvent)
    Util.Log("WebSocket message: " .. WebSocketEvent.Message)
end

function On_WebSocketConnected(WebSocketEvent)
    Util.Log("WebSocket connected: " .. WebSocketEvent.SocketId)
end

function On_WebSocketClosed(WebSocketEvent)
    Util.Log("WebSocket closed: " .. WebSocketEvent.SocketId)
end

function On_WebSocketError(WebSocketEvent)
    Util.Log("WebSocket error: " .. WebSocketEvent.ErrorMessage)
end
```
