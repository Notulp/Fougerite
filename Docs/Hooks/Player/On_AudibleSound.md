### Method
`On_AudibleSound`

### Description
Runs before the server broadcasts a movement sound (footstep, swim, etc.) for a player, so nearby players
can hear them. Allows swapping the sound, changing its range, or cancelling it entirely. Carries the water
state, which makes a swimming sound possible.

### C# Event
```csharp
public static event AudibleSoundDelegate OnAudibleSound;
public delegate void AudibleSoundDelegate(AudibleSoundEvent e);
```

### Argument(s)
- `AudibleSoundEvent AudibleSoundEvent`

### Properties/Methods
- `AudibleSoundEvent.Player` - The player emitting the sound.
- `AudibleSoundEvent.SoundName` - The sound name being broadcast. Can be changed.
- `AudibleSoundEvent.Range` - The audible range of the sound. Can be changed.
- `AudibleSoundEvent.InWater` / `Swimming` / `Sprinting` / `Crouching` - Movement state flags.
- `AudibleSoundEvent.Cancelled` - Whether the sound is cancelled.
- `AudibleSoundEvent.Cancel()` - Cancels the sound broadcast (makes the player silent to others).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnAudibleSound += SoundHandler;
}

public override void DeInitialize()
{
    Hooks.OnAudibleSound -= SoundHandler;
}

public void SoundHandler(AudibleSoundEvent e)
{
    if (e.Sprinting)
    {
        e.Cancel(); // Silent sprinting
    }
}
```

#### Python
```python
def On_AudibleSound(self, AudibleSoundEvent):
    if AudibleSoundEvent.Sprinting:
        AudibleSoundEvent.Cancel()
```

#### JavaScript
```javascript
function On_AudibleSound(AudibleSoundEvent)
{
    if (AudibleSoundEvent.Sprinting)
    {
        AudibleSoundEvent.Cancel();
    }
}
```

#### Lua
```lua
function On_AudibleSound(AudibleSoundEvent)
    if AudibleSoundEvent.Sprinting then
        AudibleSoundEvent.Cancel()
    end
end
```
