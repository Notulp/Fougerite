using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Fougerite;
using Fougerite.Events;
using Fougerite.Permissions;
using UnityEngine;

namespace GlitchFix
{
    public class GlitchFix : Fougerite.Module
    {
        private bool enabled = true;
        private bool GiveBack = true;
        private bool Ramp = true;
        private bool Struct = true;
        private bool RockGlitch = true;
        private bool RockGlitchKill = true;
        private bool CheckForRampLoot = true;
        private bool BarricadePillar = true;
        private bool AdminBypass = false;
        private bool PermissionBypass = false;
        private bool AntiFoundHide = true;
        private bool AntiPillarStash = true;
        private bool AntiRampObject = true;
        private bool BlockDoorStash = false;
        private bool AntiAnimalGlitch = false;
        private bool RockGlitchDestroySleepingBag = true;
        private bool PreventBuildInRock = true;
        private int RampStackMax = 1;
        private int AnimalHitsBeforeDestroy = 5;
        private float FoundHideRadius = 4.5f;
        private float PillarRadius = 0.40f;
        private float RampRadius = 3.5f;
        
        private string SystemName = "Server";
        private string FoundationHideMessage = "You cannot place structures on deployables here ({0})";
        private string RockBuildMessage = "You are not allowed to build in rocks";
        private string PillarBarricadeMessage = "Pillar Barricade glitching is not allowed!";

        private const string BypassPermission = "glitchfix.bypass";
        private const string ReloadPermission = "glitchfix.reload";
        private IniParser Config;
        private readonly Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        private readonly Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
        private int terrainLayer;

        // animal-glitch hit tracking, keyed by victim position
        private readonly Dictionary<Vector3, int> AnimalHits = new Dictionary<Vector3, int>();

        public override string Name
        {
            get { return "GlitchFix"; }
        }

        public override string Author
        {
            get { return "DreTaX"; }
        }

        public override string Description
        {
            get { return "Fix various glitching issues in Legacy."; }
        }

        public override Version Version
        {
            get { return new Version("2.0.0"); }
        }

        public override uint Order
        {
            get { return 2; }
        }

        public override void Initialize()
        {
            ReloadConfig();
            terrainLayer = UnityEngine.LayerMask.GetMask(new string[] { "Static", "Terrain" });
            if (enabled)
            {
                Fougerite.Hooks.OnEntityDeployedWithPlacer += EntityDeployed;
                Fougerite.Hooks.OnPlayerSpawned += OnPlayerSpawned;
                Fougerite.Hooks.OnPlayerTeleport += OnPlayerTeleport;
                Fougerite.Hooks.OnCommand += OnCommand;
                if (AntiAnimalGlitch)
                    Fougerite.Hooks.OnNPCHurt += OnNPCHurt;
            }
        }

        public override void DeInitialize()
        {
            if (enabled)
            {
                Fougerite.Hooks.OnEntityDeployedWithPlacer -= EntityDeployed;
                Fougerite.Hooks.OnPlayerSpawned -= OnPlayerSpawned;
                Fougerite.Hooks.OnPlayerTeleport -= OnPlayerTeleport;
                Fougerite.Hooks.OnCommand -= OnCommand;
                if (AntiAnimalGlitch)
                    Fougerite.Hooks.OnNPCHurt -= OnNPCHurt;
            }
        }

        public void ReloadConfig()
        {
            try
            {
                Config = new IniParser(Path.Combine(ModuleFolder, "GlitchFix.cfg"));
                enabled = Config.GetBoolSetting("Settings", "enabled");
                GiveBack = Config.GetBoolSetting("Settings", "giveback");
                Ramp = Config.GetBoolSetting("Settings", "rampstackcheck");
                Struct = Config.GetBoolSetting("Settings", "structurecheck");
                RockGlitch = Config.GetBoolSetting("Settings", "RockGlitch");
                RockGlitchKill = Config.GetBoolSetting("Settings", "RockGlitchKill");
                CheckForRampLoot = Config.GetBoolSetting("Settings", "CheckForRampLoot");
                BarricadePillar = Config.GetBoolSetting("Settings", "BarricadePillarGlitchDetection");

                AdminBypass = Config.GetBoolSetting("Settings", "AdminBypass");
                PermissionBypass = Config.GetBoolSetting("Settings", "PermissionBypass");
                AntiFoundHide = Config.GetBoolSetting("Settings", "AntiFoundationHide");
                AntiPillarStash = Config.GetBoolSetting("Settings", "AntiPillarStash");
                AntiRampObject = Config.GetBoolSetting("Settings", "AntiRampObject");
                BlockDoorStash = Config.GetBoolSetting("Settings", "AntiDoorStash");
                AntiAnimalGlitch = Config.GetBoolSetting("Settings", "AntiAnimalGlitch");
                RockGlitchDestroySleepingBag = Config.GetBoolSetting("Settings", "RockGlitchDestroySleepingBag");
                PreventBuildInRock = Config.GetBoolSetting("Settings", "PreventBuildInRock");

                RampStackMax = GetIntSetting("RampStackMax", 1);
                AnimalHitsBeforeDestroy = GetIntSetting("AnimalHitsBeforeDestroy", 5);
                FoundHideRadius = GetFloatSetting("FoundationHideRadius", 4.5f);
                PillarRadius = GetFloatSetting("PillarStashRadius", 0.40f);
                RampRadius = GetFloatSetting("RampObjectRadius", 3.5f);

                SystemName = GetStringSetting("SystemName", "Server");
                FoundationHideMessage = GetStringSetting("FoundationHideMessage",
                    "You cannot place structures on deployables here ({0})");
                RockBuildMessage = GetStringSetting("RockBuildMessage", "You are not allowed to build in rocks");
                PillarBarricadeMessage =
                    GetStringSetting("PillarBarricadeMessage", "Pillar Barricade glitching is not allowed!");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to load GlitchFix config: {e.Message}");
            }
        }

        private int GetIntSetting(string key, int def)
        {
            string v = Config.GetSetting("Settings", key);
            if (string.IsNullOrEmpty(v)) return def;
            return int.Parse(v);
        }

        private float GetFloatSetting(string key, float def)
        {
            string v = Config.GetSetting("Settings", key);
            if (string.IsNullOrEmpty(v)) return def;
            return float.Parse(v);
        }

        private string GetStringSetting(string key, string def)
        {
            string v = Config.GetSetting("Settings", key);
            return string.IsNullOrEmpty(v) ? def : v;
        }

        // A player is exempt from all checks if AdminBypass is on and they are an
        // admin, or PermissionBypass is on and they hold the bypass permission.
        private bool HasBypass(Fougerite.Player player)
        {
            if (player == null)
                return false;
            if (AdminBypass && player.Admin)
                return true;
            if (PermissionBypass &&
                PermissionSystem.GetPermissionSystem().PlayerHasPermission(player, BypassPermission))
                return true;
            return false;
        }

        public void OnCommand(Fougerite.Player player, string cmd, string[] args)
        {
            if (cmd == "glitchfix" && (player.Admin ||
                                       PermissionSystem.GetPermissionSystem()
                                           .PlayerHasPermission(player, ReloadPermission)))
            {
                player.Message("GlitchFix v" + Version);
                player.Message("By " + Author);
                player.Message("Reload config: /glitchfix reload");
                if (args.Length > 0 && args[0] == "reload")
                {
                    ReloadConfig();
                    player.Message("Config reloaded.");
                }
            }
        }

        // ANTI ANIMAL GLITCH
        // Destroyed the dead animal after N bullet hits while it was
        // already at <=0 health to stop players from glitching inside corpses.
        public void OnNPCHurt(HurtEvent he)
        {
            if (!AntiAnimalGlitch)
                return;
            if (!he.AttackerIsPlayer || he.Attacker == null)
                return;
            if (he.Entity == null)
                return;
            if (HasBypass(he.Attacker as Fougerite.Player))
                return;

            var td = he.Entity.GetTakeDamage();
            if (td == null || td.health > 0f)
                return;

            Vector3 pos = he.Entity.Location;
            if (!AnimalHits.ContainsKey(pos))
                AnimalHits[pos] = 1;
            else
                AnimalHits[pos]++;

            if (AnimalHits[pos] >= AnimalHitsBeforeDestroy)
            {
                he.Entity.Destroy();
                AnimalHits.Remove(pos);
            }
        }

        public void OnPlayerTeleport(Fougerite.Player player, Vector3 from, Vector3 dest)
        {
            if (!RockGlitch)
                return;
            if (HasBypass(player))
                return;

            var loc = player.Location;
            Vector3 cachedPosition = loc;
            RaycastHit cachedRaycast;
            cachedPosition.y += 100f;
            if (Physics.Raycast(loc, Vector3Up, out cachedRaycast, terrainLayer))
            {
                cachedPosition = cachedRaycast.point;
            }

            if (!Physics.Raycast(cachedPosition, Vector3Down, out cachedRaycast, terrainLayer)) return;
            if (!string.IsNullOrEmpty(cachedRaycast.collider.gameObject.name)) return;
            if (cachedRaycast.point.y < player.Y) return;
            Logger.LogDebug($"{player.Name} tried to TELEPORT rock glitch at {player.Location}");
            Server.GetServer().Broadcast($"{player.Name} don't try to rock glitch =)");
            if (RockGlitchDestroySleepingBag)
            {
                foreach (Collider collider in Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(player.Location, 3f))
                {
                    if (collider.gameObject.name == "SleepingBagA(Clone)")
                        TakeDamage.KillSelf(collider.GetComponent<IDMain>());
                }
            }

            if (RockGlitchKill)
            {
                player.Message("Glitching gets you killed.");
                player.Kill();
            }
        }

        public void OnPlayerSpawned(Fougerite.Player player, SpawnEvent se)
        {
            if (!RockGlitch)
                return;
            if (HasBypass(player))
                return;

            var loc = player.Location;
            Vector3 cachedPosition = loc;
            RaycastHit cachedRaycast;
            cachedPosition.y += 100f;
            if (Physics.Raycast(loc, Vector3Up, out cachedRaycast, terrainLayer))
            {
                cachedPosition = cachedRaycast.point;
            }

            if (!Physics.Raycast(cachedPosition, Vector3Down, out cachedRaycast, terrainLayer)) return;
            if (cachedRaycast.collider.gameObject.name != "") return;
            if (cachedRaycast.point.y < player.Y) return;
            Logger.LogDebug($"{player.Name} tried to rock glitch at {player.Location}");
            Server.GetServer().Broadcast($"{player.Name} don't try to rock glitch =)");
            if (RockGlitchDestroySleepingBag)
            {
                foreach (Collider collider in Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(player.Location, 3f))
                {
                    if (collider.gameObject.name == "SleepingBagA(Clone)")
                        TakeDamage.KillSelf(collider.GetComponent<IDMain>());
                }
            }

            if (RockGlitchKill)
            {
                player.Message("Glitching gets you killed.");
                player.Kill();
            }
        }

        // give the placed item back, mapping internal name to readable item name
        private void RefundItem(Fougerite.Player actualplacer, string name)
        {
            if (!GiveBack || actualplacer == null || !actualplacer.IsOnline)
                return;
            switch (name)
            {
                case "WoodFoundation": name = "Wood Foundation"; break;
                case "MetalFoundation": name = "Metal Foundation"; break;
                case "WoodRamp": name = "Wood Ramp"; break;
                case "MetalRamp": name = "Metal Ramp"; break;
                case "WoodPillar": name = "Wood Pillar"; break;
                case "MetalPillar": name = "Metal Pillar"; break;
                case "WoodDoor": name = "Wood Door"; break;
                case "MetalDoor": name = "Metal Door"; break;
                case "SmallStash": name = "Small Stash"; break;
            }

            actualplacer.Inventory.AddItem(name, 1);
        }

        // sphere check: is there ANY deployable / character near the placed entity
        private bool SphereContains(Vector3 location, float radius, bool wantCharacter, ref string foundName)
        {
            foreach (Collider collider in Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(location, radius))
            {
                if (collider == null)
                    continue;
                if (wantCharacter)
                {
                    var ch = collider.GetComponent<Character>();
                    if (ch != null && ch.playerClient != null)
                    {
                        foundName = ch.playerClient.userName;
                        return true;
                    }
                }
                else
                {
                    var dep = collider.GetComponent<DeployableObject>();
                    if (dep != null)
                    {
                        foundName = dep.name;
                        return true;
                    }
                }
            }

            return false;
        }

        // sphere check: is there a deployable matching a specific name nearby.
        // Unlike SphereContains, this doesn't stop at the first deployable found,
        // so it can't miss a match because some other deployable happened to be
        // closer/earlier in the collider list.
        private bool SphereContainsNamed(Vector3 location, float radius, string nameContains, ref string foundName)
        {
            foreach (Collider collider in Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(location, radius))
            {
                if (collider == null)
                    continue;
                var dep = collider.GetComponent<DeployableObject>();
                if (dep != null && dep.name.ToLower().Contains(nameContains))
                {
                    foundName = dep.name;
                    return true;
                }
            }

            return false;
        }

        public void EntityDeployed(Fougerite.Player Player, Fougerite.Entity Entity, Fougerite.Player actualplacer)
        {
            try
            {
                if (Entity == null)
                    return;
                if (HasBypass(actualplacer))
                    return;

                // Foundation/Ramp/Pillar/Door are needed by the checks below them.
                // SmallStash is admitted too, purely so the legacy Struct check
                // further down (which blocks a stash placed next to a woodbox/
                // another stash/a door) can actually run on it.
                if (!(Entity.Name.Contains("Foundation") || Entity.Name.Contains("Ramp")
                                                         || Entity.Name.Contains("Pillar") ||
                                                         Entity.Name == "WoodDoor" || Entity.Name == "MetalDoor" ||
                                                         Entity.Name.ToLower().Contains("smallstash")))
                    return;

                string name = Entity.Name;
                var location = Entity.Location;
                string foundName = "Unknown";

                // ROCK GLITCH BUILD PREVENTION
                // block foundations / ramps placed inside a rock (empty-name collider above)
                if (PreventBuildInRock && (name.Contains("Foundation") || name.Contains("Ramp")))
                {
                    if (InRock(location, actualplacer))
                    {
                        actualplacer.Message(RockBuildMessage);
                        Logger.LogDebug($"{actualplacer.Name} tried to rock glitch build at {location}");
                        Entity.Destroy();
                        RefundItem(actualplacer, name);
                        return;
                    }
                }

                // ANTI DOOR STASH
                // only block a SmallStash hidden in a doorway, not the whole base.
                // uses SphereContainsNamed so it can't miss the stash behind some
                // other deployable in the same radius.
                if (BlockDoorStash && name.ToLower().Contains("door"))
                {
                    if (SphereContainsNamed(location, 1f, "smallstash", ref foundName))
                    {
                        actualplacer.Message(string.Format(FoundationHideMessage, foundName));
                        Entity.Destroy();
                        RefundItem(actualplacer, name);
                        return;
                    }
                }

                // RAMP STACK LIMIT
                if (Ramp && name.Contains("Ramp"))
                {
                    RaycastHit cachedRaycast;
                    bool cachedBoolean;
                    Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
                    if (Facepunch.MeshBatch.MeshBatchPhysics.Raycast(location + new Vector3(0f, 0.1f, 0f), Vector3Down,
                            out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                    {
                        if (cachedhitInstance != null)
                        {
                            var cachedComponent = cachedhitInstance.physicalColliderReferenceOnly
                                .GetComponent<StructureComponent>();
                            if (cachedComponent.type == StructureComponent.StructureComponentType.Foundation ||
                                cachedComponent.type == StructureComponent.StructureComponentType.Ceiling)
                            {
                                var weight = cachedComponent._master._weightOnMe;
                                int ramps = 0;
                                if (weight != null && weight.ContainsKey(cachedComponent))
                                {
                                    ramps += weight[cachedComponent].Count(structure =>
                                        structure.type == StructureComponent.StructureComponentType.Ramp);
                                }

                                if (ramps > RampStackMax)
                                {
                                    Entity.Destroy();
                                    RefundItem(actualplacer, name);
                                    return;
                                }
                            }
                        }
                    }
                }

                // ANTI FOUNDATION HIDE (deployable + character)
                if (AntiFoundHide && name.Contains("Foundation"))
                {
                    if (SphereContains(location, FoundHideRadius, false, ref foundName) ||
                        SphereContains(location, FoundHideRadius, true, ref foundName))
                    {
                        actualplacer.Message(string.Format(FoundationHideMessage, foundName));
                        Entity.Destroy();
                        RefundItem(actualplacer, name);
                        return;
                    }
                }

                // ANTI PILLAR STASH
                if (AntiPillarStash && name.Contains("Pillar"))
                {
                    if (SphereContains(location, PillarRadius, false, ref foundName))
                    {
                        actualplacer.Message(string.Format(FoundationHideMessage, foundName));
                        Entity.Destroy();
                        RefundItem(actualplacer, name);
                        return;
                    }
                }

                // ANTI RAMP ON OBJECT (deployable + character)
                if (AntiRampObject && CheckForRampLoot && name.Contains("Ramp"))
                {
                    if (SphereContains(location, RampRadius, false, ref foundName) ||
                        SphereContains(location, RampRadius, true, ref foundName))
                    {
                        actualplacer.Message(string.Format(FoundationHideMessage, foundName));
                        Entity.Destroy();
                        RefundItem(actualplacer, name);
                        return;
                    }
                }

                // LEGACY STRUCTURE CHECK (woodbox/smallstash/door, by raw collider name)
                // Pillar/Foundation/Ramp here overlap with AntiPillarStash/
                // AntiFoundationHide/AntiRampObject above (raw name match vs
                // DeployableObject component match - not a guaranteed subset,
                // kept independent on purpose). The Door and SmallStash branches
                // are NOT covered anywhere else: Door catches a woodbox blocking
                // a door, SmallStash catches a stash placed next to a woodbox,
                // another stash, or a door to disguise it.
                if (Struct)
                {
                    bool isdoor = false;
                    float d = 4.5f;
                    if (name.Contains("Pillar"))
                    {
                        d = 0.40f;
                    }
                    else if (name.Contains("Door"))
                    {
                        isdoor = true;
                        d = 0.40f;
                    }
                    else if (name.ToLower().Contains("smallstash"))
                    {
                        d = 0.40f;
                    }
                    else if (name.Contains("Foundation"))
                    {
                        d = 4.5f;
                    }
                    else if (name.Contains("Ramp"))
                    {
                        if (!CheckForRampLoot)
                        {
                            return;
                        }

                        d = 3.5f;
                    }

                    var x = Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(location, d);
                    if (
                        x.Any(l =>
                            l.name.ToLower().Contains("woodbox") || l.name.ToLower().Contains("smallstash") ||
                            (l.name.ToLower().Contains("door") && !isdoor)))
                    {
                        Entity.Destroy();
                        RefundItem(actualplacer, name);
                        return;
                    }
                }

                // ANTI PILLAR BARRICADE
                if (BarricadePillar && name.Contains("Pillar"))
                {
                    if (Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(location, 0.34f)
                        .Where(collider => collider.GetComponent<DeployableObject>() != null)
                        .Any(collider => collider.GetComponent<DeployableObject>().name.Contains("Barricade_Fence")))
                    {
                        actualplacer.Message(PillarBarricadeMessage);
                        Entity.Destroy();
                        RefundItem(actualplacer, name);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[GlitchFix] Some error showed up. Report this. {ex}");
            }
        }

        // a build pos is "in rock" when an unnamed
        // (terrain mesh) collider above the pos contains both the pos and the placer.
        private bool InRock(Vector3 pos, Fougerite.Player player)
        {
            if (player == null)
                return false;
            Vector3 playerPos = player.Location;
            bool punish = false;
            var hits = Physics.RaycastAll(pos + (Vector3.down * 20f), Vector3.up, 5000f);
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;
                if (hit.collider.gameObject.name.Length >= 1)
                    continue;
                if (!hit.collider.bounds.Contains(playerPos))
                    break;
                if (hit.collider.bounds.Contains(pos))
                {
                    Vector3 feet = playerPos + new Vector3(0f, -2.5f, 0f);
                    if (hit.collider.bounds.Contains(feet))
                        punish = true;
                    break;
                }
            }

            return punish;
        }
    }
}