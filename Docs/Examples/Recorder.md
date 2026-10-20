### Example plugin
`Recorder` - record a structure as a reusable blueprint

### Original
[`Recorder.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/Recorder/Recorder.py) by
DreTaX.

### Concept
`/record <name>` puts an admin into recording mode; every entity they place afterwards is appended as a
`[Position]`-style section to `Buildings\<name>.ini`, storing the relative offset from the *first* recorded
piece plus its rotation and its prefab name (looked up from a hardcoded `prefabs` dictionary mapping the
in-game entity name, e.g. `WoodWall`, to its spawnable prefab string, e.g. `;struct_wood_wall`). `/stop` ends
the recording. The result is a tiny, named "blueprint" file independent of where it was originally built.

`/spawn <name> <distance>` reads that blueprint back and re-creates every recorded piece at `distance` meters
in front of the caller (`Util.Infront` + `World.GetGround` to drop it onto the terrain), re-applying each
piece's stored offset and rotation relative to that new anchor point - instantly rebuilding a saved base
design anywhere on the map.

### C#

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Fougerite;

namespace Recorder
{
    public class RecordedPiece
    {
        public string Prefab;
        public Vector3 Offset; // relative to the first recorded piece
        public Quaternion Rotation;
    }

    public class Recorder : Module
    {
        public override string Name { get { return "Recorder"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "Record and respawn structures as blueprints."; } }
        public override Version Version { get { return new Version(1, 2); } }

        private readonly Dictionary<ulong, string> _recording = new Dictionary<ulong, string>();
        private readonly Dictionary<string, List<RecordedPiece>> _blueprints = new Dictionary<string, List<RecordedPiece>>();
        private readonly Dictionary<ulong, Vector3> _recordOrigin = new Dictionary<ulong, Vector3>();

        public override void Initialize()
        {
            Hooks.OnCommand += OnCommand;
            Hooks.OnEntityDeployed += OnEntityDeployed;
        }

        public override void DeInitialize()
        {
            Hooks.OnCommand -= OnCommand;
            Hooks.OnEntityDeployed -= OnEntityDeployed;
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd == "record" && player.Admin && args.Length == 1)
            {
                _recording[player.UID] = args[0];
                _blueprints[args[0]] = new List<RecordedPiece>();
                player.MessageFrom(Name, "Recording '" + args[0] + "'. Place pieces, then use /stoprecord.");
            }
            else if (cmd == "stoprecord" && player.Admin)
            {
                if (_recording.ContainsKey(player.UID))
                {
                    string recName = _recording[player.UID];
                    _recording.Remove(player.UID);
                    _recordOrigin.Remove(player.UID);
                    player.MessageFrom(Name, "Stopped recording '" + recName + "'.");
                }
            }
            else if (cmd == "spawn" && player.Admin && args.Length == 2)
            {
                SpawnBlueprint(player, args[0], float.Parse(args[1]));
            }
        }

        private void OnEntityDeployed(Player player, Entity entity, Player actualPlacer)
        {
            if (!_recording.ContainsKey(player.UID)) return;
            string recName = _recording[player.UID];

            if (!_recordOrigin.ContainsKey(player.UID))
            {
                _recordOrigin[player.UID] = entity.Location;
            }
            Vector3 offset = entity.Location - _recordOrigin[player.UID];
            _blueprints[recName].Add(new RecordedPiece
            {
                Prefab = entity.Name,          // in a real plugin: look this up in a name -> prefab table
                Offset = offset,
                Rotation = entity.Object.transform.rotation
            });
            player.MessageFrom(Name, "Recorded " + entity.Name + " (" + _blueprints[recName].Count + " pieces).");
        }

        private void SpawnBlueprint(Player player, string recName, float distance)
        {
            if (!_blueprints.ContainsKey(recName))
            {
                player.MessageFrom(Name, "Blueprint '" + recName + "' not found.");
                return;
            }
            Vector3 anchor = Util.GetUtil().Infront(player, distance);
            anchor.y = World.GetWorld().GetGround(anchor.x, anchor.z);

            foreach (RecordedPiece piece in _blueprints[recName])
            {
                Vector3 spawnPos = anchor + piece.Offset;
                World.GetWorld().SpawnEntity(piece.Prefab, spawnPos, piece.Rotation);
            }
            player.MessageFrom(Name, "Spawned '" + recName + "' (" + _blueprints[recName].Count + " pieces).");
        }
    }
}
```

### JavaScript

```javascript
var recording = {};    // playerUID -> blueprint name
var blueprints = {};   // blueprint name -> list of {Prefab, Offset, Rotation}
var recordOrigin = {}; // playerUID -> Vector3

function On_Command(Player, cmd, args)
{
    if (cmd === "record" && Player.Admin && args.length === 1)
    {
        recording[Player.UID] = args[0];
        blueprints[args[0]] = [];
        Player.MessageFrom("Recorder", "Recording '" + args[0] + "'. Place pieces, then use /stoprecord.");
    }
    else if (cmd === "stoprecord" && Player.Admin)
    {
        if (Player.UID in recording)
        {
            var recName = recording[Player.UID];
            delete recording[Player.UID];
            delete recordOrigin[Player.UID];
            Player.MessageFrom("Recorder", "Stopped recording '" + recName + "'.");
        }
    }
    else if (cmd === "spawn" && Player.Admin && args.length === 2)
    {
        SpawnBlueprint(Player, args[0], parseFloat(args[1]));
    }
}

function On_EntityDeployed(Player, Entity, ActualPlacer)
{
    if (!(Player.UID in recording)) return;
    var recName = recording[Player.UID];

    if (!(Player.UID in recordOrigin))
    {
        recordOrigin[Player.UID] = Entity.Location;
    }
    var origin = recordOrigin[Player.UID];
    var offset = new UnityEngine.Vector3(Entity.Location.x - origin.x, Entity.Location.y - origin.y, Entity.Location.z - origin.z);
    blueprints[recName].push({
        Prefab: Entity.Name,
        Offset: offset,
        Rotation: Entity.Object.transform.rotation
    });
    Player.MessageFrom("Recorder", "Recorded " + Entity.Name + " (" + blueprints[recName].length + " pieces).");
}

function SpawnBlueprint(Player, recName, distance)
{
    if (!(recName in blueprints))
    {
        Player.MessageFrom("Recorder", "Blueprint '" + recName + "' not found.");
        return;
    }
    var anchor = Util.Infront(Player, distance);
    anchor.y = World.GetGround(anchor.x, anchor.z);

    var pieces = blueprints[recName];
    for (var i = 0; i < pieces.length; i++)
    {
        var off = pieces[i].Offset;
        var spawnPos = new UnityEngine.Vector3(anchor.x + off.x, anchor.y + off.y, anchor.z + off.z);
        World.SpawnEntity(pieces[i].Prefab, spawnPos, pieces[i].Rotation);
    }
    Player.MessageFrom("Recorder", "Spawned '" + recName + "' (" + pieces.length + " pieces).");
}
```

### Python

```python
recording = {}
blueprints = {}
recordOrigin = {}


class Recorder:

    def On_Command(self, Player, cmd, args):
        if cmd == "record" and Player.Admin and len(args) == 1:
            recording[Player.UID] = args[0]
            blueprints[args[0]] = []
            Player.MessageFrom("Recorder", "Recording '" + args[0] + "'. Place pieces, then use /stoprecord.")
        elif cmd == "stoprecord" and Player.Admin:
            if Player.UID in recording:
                recName = recording.pop(Player.UID)
                recordOrigin.pop(Player.UID, None)
                Player.MessageFrom("Recorder", "Stopped recording '" + recName + "'.")
        elif cmd == "spawn" and Player.Admin and len(args) == 2:
            self.SpawnBlueprint(Player, args[0], float(args[1]))

    def On_EntityDeployed(self, Player, Entity, ActualPlacer):
        if Player.UID not in recording:
            return
        recName = recording[Player.UID]

        if Player.UID not in recordOrigin:
            recordOrigin[Player.UID] = Entity.Location
        offset = Entity.Location - recordOrigin[Player.UID]
        blueprints[recName].append({
            "Prefab": Entity.Name,
            "Offset": offset,
            "Rotation": Entity.Object.transform.rotation
        })
        Player.MessageFrom("Recorder", "Recorded " + Entity.Name + " (" + str(len(blueprints[recName])) + " pieces).")

    def SpawnBlueprint(self, Player, recName, distance):
        if recName not in blueprints:
            Player.MessageFrom("Recorder", "Blueprint '" + recName + "' not found.")
            return
        anchor = Util.Infront(Player, distance)
        anchor.y = World.GetGround(anchor.x, anchor.z)

        for piece in blueprints[recName]:
            spawnPos = anchor + piece["Offset"]
            World.SpawnEntity(piece["Prefab"], spawnPos, piece["Rotation"])
        Player.MessageFrom("Recorder", "Spawned '" + recName + "' (" + str(len(blueprints[recName])) + " pieces).")
```

### Lua

```lua
local recording = {}
local blueprints = {}
local recordOrigin = {}

function On_Command(player, cmd, args)
    if cmd == "record" and player.Admin and #args == 1 then
        recording[player.UID] = args[1]
        blueprints[args[1]] = {}
        player:MessageFrom("Recorder", "Recording '" .. args[1] .. "'. Place pieces, then use /stoprecord.")
    elseif cmd == "stoprecord" and player.Admin then
        if recording[player.UID] ~= nil then
            local recName = recording[player.UID]
            recording[player.UID] = nil
            recordOrigin[player.UID] = nil
            player:MessageFrom("Recorder", "Stopped recording '" .. recName .. "'.")
        end
    elseif cmd == "spawn" and player.Admin and #args == 2 then
        SpawnBlueprint(player, args[1], tonumber(args[2]))
    end
end

function On_EntityDeployed(player, entity, actualPlacer)
    local recName = recording[player.UID]
    if recName == nil then return end

    if recordOrigin[player.UID] == nil then
        recordOrigin[player.UID] = entity.Location
    end
    local origin = recordOrigin[player.UID]
    local offset = UnityEngine.Vector3.__new(entity.Location.x - origin.x, entity.Location.y - origin.y, entity.Location.z - origin.z)
    table.insert(blueprints[recName], {
        Prefab = entity.Name,
        Offset = offset,
        Rotation = entity.Object.transform.rotation
    })
    player:MessageFrom("Recorder", "Recorded " .. entity.Name .. " (" .. #blueprints[recName] .. " pieces).")
end

function SpawnBlueprint(player, recName, distance)
    local pieces = blueprints[recName]
    if pieces == nil then
        player:MessageFrom("Recorder", "Blueprint '" .. recName .. "' not found.")
        return
    end
    local anchor = Util.Infront(player, distance)
    anchor.y = World.GetGround(anchor.x, anchor.z)

    for _, piece in ipairs(pieces) do
        local off = piece.Offset
        local spawnPos = UnityEngine.Vector3.__new(anchor.x + off.x, anchor.y + off.y, anchor.z + off.z)
        World.SpawnEntity(piece.Prefab, spawnPos, piece.Rotation)
    end
    player:MessageFrom("Recorder", "Spawned '" .. recName .. "' (" .. #pieces .. " pieces).")
end
```

### Notes / what was trimmed
- The original persists each blueprint as its own ini file under `Buildings\` (one section per recorded piece)
  instead of an in-memory dictionary, so blueprints survive a restart - swap `blueprints`/`recordOrigin` for an
  `IniParser` per name using the same pattern as the [`Warps`](Warps.md) example. It also resolves each
  in-game entity name to its spawnable prefab string through a hardcoded lookup table (`WoodWall` ->
  `;struct_wood_wall`, etc. - see [`Prefabs`](../Classes/Prefabs.md) for the full reference) rather than
  assuming `Entity.Name` is directly spawnable, and attaches spawned structure pieces to a `StructureMaster`
  via `World.CreateSM` so decay/weight-linking works correctly on the rebuilt copy (see the ownership note in
  [`Classes/World.md`](../Classes/World.md)).

See also: [`Classes/World.md`](../Classes/World.md) · [`Classes/Prefabs.md`](../Classes/Prefabs.md) ·
[`Hooks/Entities/On_EntityDeployed.md`](../Hooks/Entities/On_EntityDeployed.md)
