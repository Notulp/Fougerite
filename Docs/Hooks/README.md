# Hooks Reference

Each file below documents a single Fougerite hook: what it does, what argument(s) it passes, the relevant
properties/methods on those arguments, and usage examples in C#, Python, JavaScript and Lua.

> Naming convention: The C# event on `Fougerite.Hooks` is named `OnXxx` (e.g. `OnChat`).
> The matching script method (Python/JS/Lua) is named `On_Xxx` (e.g. `On_Chat`), as defined in
> `Fougerite.PluginLoaders.PluginLoaderEvents`.

## Player

| Hook | Script Method | Description |
|---|---|---|
| [OnChat](Player/On_Chat.md) | `On_Chat` | A player sends a chat message. |
| [OnCommand](Player/On_Command.md) | `On_Command` | A player/console executes a `/command`. |
| [OnCommandRestriction](Player/On_CommandRestriction.md) | `On_CommandRestriction` | A command gets restricted/unrestricted for a player or globally. |
| [OnPlayerConnected](Player/On_PlayerConnected.md) | `On_PlayerConnected` | A player finished connecting. |
| [OnPlayerDisconnected](Player/On_PlayerDisconnected.md) | `On_PlayerDisconnected` | A player disconnected. |
| [OnPlayerApproval](Player/On_PlayerApproval.md) | `On_PlayerApproval` | A connecting player is being approved/denied. |
| [OnSteamDeny](Player/On_SteamDeny.md) | `On_SteamDeny` | A player got kicked/denied by Steam. |
| [OnPlayerBan](Player/On_PlayerBan.md) | `On_PlayerBan` | A player is being banned. |
| [OnPlayerSpawning](Player/On_PlayerSpawning.md) | `On_PlayerSpawning` | A player is about to spawn. |
| [OnPlayerSpawned](Player/On_PlayerSpawned.md) | `On_PlayerSpawned` | A player has spawned. |
| [OnPlayerTeleport](Player/On_PlayerTeleport.md) | `On_PlayerTeleport` | A player was teleported via the Fougerite API. |
| [OnPlayerMove](Player/On_PlayerMove.md) | `On_PlayerMove` | A player moves (every movement packet). |
| [OnPlayerGathering](Player/On_PlayerGathering.md) | `On_PlayerGathering` | A player gathers a resource/animal. |
| [OnPlayerHurt](Player/On_PlayerHurt.md) | `On_PlayerHurt` | A player takes damage. |
| [OnPlayerKilled](Player/On_PlayerKilled.md) | `On_PlayerKilled` | A player has died. |
| [OnShowTalker (VoiceChat)](Player/On_VoiceChat.md) | `On_VoiceChat` | A player is talking on the microphone. |
| [OnFallDamage](Player/On_FallDamage.md) | `On_FallDamage` | A player receives fall damage. |
| [OnWaterDamage](Player/On_WaterDamage.md) | `On_WaterDamage` | A player is damaged by water/drowning. |
| [OnAudibleSound](Player/On_AudibleSound.md) | `On_AudibleSound` | A player's movement sound is about to be broadcast. |
| [OnMetabolismUpdate](Player/On_MetabolismUpdate.md) | `On_MetabolismUpdate` | A player's metabolism (calories/water/etc) updates. |

## NPC

| Hook | Script Method | Description |
|---|---|---|
| [OnNPCHurt](NPC/On_NPCHurt.md) | `On_NPCHurt` | An NPC/animal takes damage. |
| [OnNPCKilled](NPC/On_NPCKilled.md) | `On_NPCKilled` | An NPC/animal has died. |
| [OnNPCSpawned](NPC/On_NPCSpawned.md) | `On_NPCSpawned` | An NPC/animal has spawned. |
| [OnAnimalMovement](NPC/On_AnimalMovement.md) | `On_AnimalMovement` | An animal's AI movement is updated. |

## Entities & World Objects

| Hook | Script Method | Description |
|---|---|---|
| [OnEntityHurt](Entities/On_EntityHurt.md) | `On_EntityHurt` | A structure/deployable entity takes damage. |
| [OnEntityDecay](Entities/On_EntityDecay.md) | `On_EntityDecay` | An entity is damaged by decay. |
| [OnEntityDeployed](Entities/On_EntityDeployed.md) | `On_EntityDeployed` | An entity is placed on the ground. |
| [OnEntityDestroyed](Entities/On_EntityDestroyed.md) | `On_EntityDestroyed` | An entity is destroyed. |
| [OnDoorUse](Entities/On_DoorUse.md) | `On_DoorUse` | A door is opened/closed. |
| [OnFireBarrelToggle](Entities/On_FireBarrelToggle.md) | `On_FireBarrelToggle` | A fire barrel is turned on/off. |
| [OnTimedExplosiveSpawned](Entities/On_TimedExplosiveSpawned.md) | `On_TimedExplosiveSpawned` | A C4/timed explosive is placed. |
| [OnSleeperSpawned](Entities/On_SleeperSpawned.md) | `On_SleeperSpawned` | A sleeping player (bag/bed) is registered. |
| [OnHeatZoneEnter](Entities/On_HeatZoneEnter.md) | `On_HeatZoneEnter` | A player stands in a campfire/furnace's heat zone. |
| [OnWorkZoneEnter](Entities/On_WorkZoneEnter.md) | `On_WorkZoneEnter` | A player stands in a workbench's work zone. |

## Items, Crafting & Loot

| Hook | Script Method | Description |
|---|---|---|
| [OnItemPickup](Items/On_ItemPickup.md) | `On_ItemPickup` | A player picks an item off the ground. |
| [OnItemAdded](Items/On_ItemAdded.md) | `On_ItemAdded` | An item is added to an inventory. |
| [OnItemRemoved](Items/On_ItemRemoved.md) | `On_ItemRemoved` | An item is removed from an inventory. |
| [OnItemMove](Items/On_ItemMove.md) | `On_ItemMove` | An item is moved between inventory slots. |
| [OnItemModInstall](Items/On_ItemModInstall.md) | `On_ItemModInstall` | A weapon attachment/mod is installed. |
| [OnBlueprintUse](Items/On_BlueprintUse.md) | `On_BlueprintUse` | A player learns a blueprint. |
| [OnCrafting](Items/On_Crafting.md) | `On_Crafting` | A player starts crafting an item. |
| [OnCraftCancel](Items/On_CraftingCancel.md) | `On_CraftingCancel` | A crafting queue entry is cancelled. |
| [OnCraftComplete](Items/On_CraftingComplete.md) | `On_CraftingComplete` | A crafting queue entry completes. |
| [OnResearch](Items/On_Research.md) | `On_Research` | A player researches an item. |
| [OnRepairBench](Items/On_RepairBench.md) | `On_RepairBench` | A player repairs an item at a repair bench. |
| [OnLootUse](Items/On_LootUse.md) | `On_LootUse` | A player starts looting a container/corpse. |
| [OnBeltUse](Items/On_BeltUse.md) | `On_BeltUse` | A player selects/uses a belt (hotbar) slot. |
| [OnConsumableUse](Items/On_ConsumableUse.md) | `On_ConsumableUse` | A player eats/drinks a consumable. |
| [OnMedikitUse](Items/On_MedikitUse.md) | `On_MedikitUse` | A player uses a medkit/bandage. |
| [OnBloodDraw](Items/On_BloodDraw.md) | `On_BloodDraw` | A player uses a Blood Draw Kit. |
| [OnArmorEquip / OnArmorUnEquip](Items/On_ArmorEquip.md) | `On_ArmorEquip` / `On_ArmorUnEquip` | Armor is equipped/unequipped. |
| [OnFlareThrow](Items/On_FlareThrow.md) | `On_FlareThrow` | A player throws a flare. |
| [OnFlareIgnite / OnTorchIgnite](Items/On_FlareIgnite.md) | `On_FlareIgnite` / `On_TorchIgnite` | A flare/torch is ignited. |

## Combat / Weapons

| Hook | Script Method | Description |
|---|---|---|
| [OnShoot](Combat/On_Shoot.md) | `On_Shoot` | A player fires a bullet weapon. |
| [OnShotgunShoot](Combat/On_ShotgunShoot.md) | `On_ShotgunShoot` | A player fires a shotgun. |
| [OnBowShoot](Combat/On_BowShoot.md) | `On_BowShoot` | A player fires a bow. |
| [OnGrenadeThrow](Combat/On_GrenadeThrow.md) | `On_GrenadeThrow` | A player throws a grenade. |

## World / Server Events

| Hook | Script Method | Description |
|---|---|---|
| [OnResourceSpawned](World/On_ResourceSpawn.md) | `On_ResourceSpawn` | A resource node (tree/ore) spawns. |
| [OnGenericSpawnerLoad](World/On_GenericSpawnLoad.md) | `On_GenericSpawnLoad` | A generic resource spawner has loaded. |
| [OnAirdropCalled](World/On_Airdrop.md) | `On_Airdrop` | An airdrop is called in. |
| [OnSupplyDropPlaneCreated](World/On_SupplyDropPlaneCreated.md) | `On_SupplyDropPlaneCreated` | The supply drop plane spawns. |
| [OnAirdropCrateDropped](World/On_AirdropCrateDropped.md) | `On_AirdropCrateDropped` | The supply crate is dropped from the plane. |
| [OnSupplySignalExpode](World/On_SupplySignalExploded.md) | `On_SupplySignalExploded` | A supply signal grenade explodes. |
| [OnDayCycleChanged](World/On_DayCycleChanged.md) | `On_DayCycleChanged` | Day/night cycle changes. |
| [OnAllPluginsLoaded](Server/On_AllPluginsLoaded.md) | `On_AllPluginsLoaded` | All plugins finished loading (first time). |
| [OnPluginInit](Server/On_PluginInit.md) | `On_PluginInit` | This plugin has finished initializing. |
| [OnPluginLoaded](Server/On_PluginLoaded.md) | `On_PluginLoaded` | Any plugin (C#/CSScript/Python/JS/Lua) finished loading. |
| [OnPluginUnloaded](Server/On_PluginUnloaded.md) | `On_PluginUnloaded` | Any plugin (C#/CSScript/Python/JS/Lua) was unloaded. |
| [OnModulesLoaded](Server/On_ModulesLoaded.md) | *(C# modules only)* | All C# modules finished loading. |
| [OnServerInit](Server/On_ServerInit.md) | `On_ServerInit` | The server started loading. |
| [OnServerLoaded](Server/On_ServerLoaded.md) | `On_ServerLoaded` | The server finished loading. |
| [OnServerShutdown](Server/On_ServerShutdown.md) | `On_ServerShutdown` | The server is stopping. |
| [OnServerSaved](Server/On_ServerSaved.md) | `On_ServerSaved` | The server finished saving. |
| [OnServerTick](Server/On_ServerTick.md) | `On_ServerTick` | Runs on every server tick. |
| [OnItemsLoaded](Server/On_ItemsLoaded.md) | `On_ItemsLoaded` | Item datablocks have been loaded. |
| [OnTablesLoaded](Server/On_TablesLoaded.md) | `On_TablesLoaded` | Loot tables have been loaded. |
| [OnPluginMessage](Server/On_PluginMessage.md) | `On_PluginMessage` | A plugin sends a message to another plugin. |
| [OnLogger](Server/On_Logger.md) | `On_Logger` | Internal Fougerite log events. |
| [OnConsoleReceivedWithCancel](Server/On_ConsoleWithCancel.md) | `On_ConsoleWithCancel` | A console command is received. |
| [OnPermissionChange](Server/On_PermissionChange.md) | `On_PermissionChange` | A permission group/permission is being changed. |
| [OnWebSocketMessage / Connected / Closed / Error](Server/On_WebSocket.md) | `On_WebSocketMessage` etc. | WebSocket client events. |

---

Also see: [Web class](../Classes/Web.md) · [Loom class](../Classes/Loom.md) · [Timers](../Classes/Timers.md)
