### Method
`On_Chat`

### Description
Runs when a Player types something in the chat.

### C# Event
```csharp
public static event ChatHandlerDelegate OnChat;
public delegate void ChatHandlerDelegate(Player player, ref ChatString text);
```

### Argument(s)
- `Player Player` - The player who sent the message.
- `ChatString ChatEvent` - The chat message wrapper (passed by `ref` in C#).

### Properties/Methods
- `ChatString.OriginalMessage` - The original text the player typed.
- `ChatString.NewText` - Set this to change/replace the outgoing message. Set it to a blank/space string
  to make the message disappear.
- `ChatString.ToString()` - Returns the original text.
- `ChatString.Contains(string str)` - Checks if the original message contains a substring.
- `ChatString.Replace(string find, string replacement)` - Returns the original message with a replacement.
- `ChatString.Substring(int start, int length)` - Returns a substring of the original message.

No `Name`, `Attacker`, or `Victim` properties on this event.

### Examples

#### C#
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
    string Text = chatString.OriginalMessage;
    chatString.NewText = "   ";
    Server.GetServer().Broadcast(Text);
}
```

#### Python
```python
def On_Chat(self, Player, ChatEvent):
    Text = ChatEvent.OriginalMessage

    ChatEvent.NewText = "    "  # How to make the chat message disappear

    Server.Broadcast(Text)
```

#### JavaScript
```javascript
function On_Chat(Player, ChatEvent)
{
    var Text = ChatEvent.OriginalMessage;

    ChatEvent.NewText = "    "; // How to make the chat message disappear

    Server.Broadcast(Text);
}
```

#### Lua
```lua
function On_Chat(Player, ChatEvent)
    local Text = ChatEvent.OriginalMessage

    ChatEvent.NewText = "    " -- How to make the chat message disappear

    Server.Broadcast(Text)
end
```
