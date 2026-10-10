### Class
`Fougerite.World` (`SpawnEntity`/`Spawn`/`SpawnAtPlayer`)

### Description
This page lists known prefab path names that can be passed as the `prefab` argument to
[`World.SpawnEntity`/`World.Spawn`/`World.SpawnAtPlayer`](World.md). These are the same short prefab names used
by the in-game `spawn`/`entity.spawn` console commands and by other Rust Legacy plugin frameworks - Fougerite
does not maintain or validate this list itself, it is simply forwarded to the underlying engine's prefab
lookup, so names may vary slightly between server/client builds. If a name doesn't resolve, `SpawnEntity`
returns `null` (check the server console for a resolution warning).

> Prefixes are meaningful to the engine's own prefab categories: `;` denotes a structure/deployable prefab,
> `:` denotes a Character/NPC prefab, and a few loot container prefabs (`AmmoLootBox`, `MedicalLootBox`,
> `BoxLoot`, `WeaponLootBox`, `SupplyCrate`) have no prefix. Always pass the name exactly as listed, including
> the prefix.

### Structure - Wood
- `;struct_wood_foundation`
- `;struct_wood_windowframe`
- `;struct_wood_doorway`
- `;struct_wood_wall`
- `;struct_wood_ceiling`
- `;struct_wood_ramp`
- `;struct_wood_stairs`
- `;struct_wood_pillar`

### Structure - Metal
- `;struct_metal_foundation`
- `;struct_metal_wall`
- `;struct_metal_doorframe`
- `;struct_metal_ceiling`
- `;struct_metal_stairs`
- `;struct_metal_windowframe`
- `;struct_metal_ramp`
- `;struct_metal_pillar`

### Other Metal
- `;deploy_metalwindowbars`

### Doors
- `;deploy_wood_door`
- `;deploy_metal_door`

### Storage
- `;deploy_wood_box`
- `;deploy_wood_storage_large`
- `;deploy_small_stash`

### Attack and Protect
- `;deploy_largewoodspikewall`
- `;deploy_woodspikewall`
- `;deploy_wood_barricade`
- `;deploy_woodgateway`
- `;deploy_woodgate`

### Base
- `;deploy_camp_bonfire`
- `;deploy_wood_shelter`
- `;deploy_furnace`
- `;deploy_workbench`
- `;deploy_camp_sleepingbag`
- `;deploy_singlebed`

### Resources
- `;res_woodpile`
- `;res_ore_1`
- `;res_ore_2`
- `;res_ore_3`

### Animals (NPC)
- `:stag_prefab`
- `:chicken_prefab`
- `:rabbit_prefab_a`
- `:bear_prefab`
- `:mutant_bear`
- `:boar_prefab`
- `:wolf_prefab`
- `:mutant_wolf`

### Other
- `;drop_lootsack_zombie`
- `;drop_lootsack`
- `;sleeper_male`
- `;explosive_charge`
- `AmmoLootBox`
- `MedicalLootBox`
- `BoxLoot`
- `WeaponLootBox`
- `SupplyCrate`

### Example - C# (spawning a wood box above a player)
```csharp
public void GiveBox(Player player)
{
    Vector3 loc = player.Location + new Vector3(0f, 1f, 0f);
    Entity box = World.GetWorld().SpawnEntity(";deploy_wood_box", loc);
    if (box == null)
    {
        Logger.LogError("Failed to spawn ;deploy_wood_box - unknown prefab name?");
    }
}
```

### Example - Python (spawning an animal at a player)
```python
def SpawnWolf(self, Player):
    World.GetWorld().SpawnAtPlayer(":wolf_prefab", Player, 1)
```

See also: [`World`](World.md) · [`Entity`](Entity.md)
