using System.Collections.Generic;
using System.Reflection;

namespace GlitchFix
{
    using Fougerite;
    using System;
    using System.IO;
    using System.Linq;
    using Fougerite.Events;
    using UnityEngine;

    public class GlitchFix : Fougerite.Module
    {
        private bool enabled;
        private bool GiveBack;
        private bool Ramp;
        private bool Struct;
        private bool RockGlitch;
        private bool RockGlitchKill;
        private bool CheckForRampLoot;
        private bool BarricadePillar;
        private IniParser Config;
        private Vector3 Vector3Down = new Vector3(0f, -1f, 0f);
        private Vector3 Vector3Up = new Vector3(0f, 1f, 0f);
        private int terrainLayer;
        //private FieldInfo Weight;

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
            get { return "Fixing multiply ramp spawning one over one"; }
        }

        public override Version Version
        {
            get { return new Version("1.4.8");}
        }

        public override uint Order
        {
            get { return 2; }
        }

        public override void Initialize()
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
            //Weight = typeof(StructureMaster).GetField("_weightOnMe", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            terrainLayer = UnityEngine.LayerMask.GetMask(new string[] { "Static", "Terrain" });
            if (enabled)
            {
                Fougerite.Hooks.OnEntityDeployedWithPlacer += EntityDeployed;
                Fougerite.Hooks.OnPlayerSpawned += OnPlayerSpawned;
                Fougerite.Hooks.OnPlayerTeleport += OnPlayerTeleport;
            }
        }

        public override void DeInitialize()
        {
            if (enabled)
            {
                Fougerite.Hooks.OnEntityDeployedWithPlacer -= EntityDeployed;
                Fougerite.Hooks.OnPlayerSpawned -= OnPlayerSpawned;
                Fougerite.Hooks.OnPlayerTeleport -= OnPlayerTeleport;
            }
        }
        
        public void OnPlayerTeleport(Fougerite.Player player, Vector3 from, Vector3 dest)
        {
            if (RockGlitch)
            {
                var loc = player.Location;
                Vector3 cachedPosition = loc;
                RaycastHit cachedRaycast;
                cachedPosition.y += 100f;
                try
                {
                    if (Physics.Raycast(loc, Vector3Up, out cachedRaycast, terrainLayer))
                    {
                        cachedPosition = cachedRaycast.point;
                    }
                    if (!Physics.Raycast(cachedPosition, Vector3Down, out cachedRaycast, terrainLayer)) return;
                }
                catch
                {
                    return;
                }
                if (!string.IsNullOrEmpty(cachedRaycast.collider.gameObject.name)) return;
                if (cachedRaycast.point.y < player.Y) return;
                Logger.LogDebug(player.Name + " tried to TELEPORT rock glitch at " + player.Location);
                Server.GetServer().Broadcast(player.Name + " don't try to rock glitch =)");
                foreach (Collider collider in Physics.OverlapSphere(player.Location, 3f))
                {
                    if (collider.gameObject.name == "SleepingBagA(Clone)")
                        TakeDamage.KillSelf(collider.GetComponent<IDMain>());
                }
                if (RockGlitchKill)
                {
                    if (player.Admin)
                    {
                        player.Message("You administrator rights allowed you to bug inside rocks.");
                        return;
                    }
                    player.Message("Glitching gets you killed.");
                    player.Kill();
                }
            }
        }

        public void OnPlayerSpawned(Fougerite.Player player, SpawnEvent se)
        {
            if (RockGlitch)
            {
                var loc = player.Location;
                Vector3 cachedPosition = loc;
                RaycastHit cachedRaycast;
                cachedPosition.y += 100f;
                try
                {
                    if (Physics.Raycast(loc, Vector3Up, out cachedRaycast, terrainLayer))
                    {
                        cachedPosition = cachedRaycast.point;
                    }
                    if (!Physics.Raycast(cachedPosition, Vector3Down, out cachedRaycast, terrainLayer)) return;
                }
                catch
                {
                    return;
                }
                if (cachedRaycast.collider.gameObject.name != "") return;
                if (cachedRaycast.point.y < player.Y) return;
                Logger.LogDebug(player.Name + " tried to rock glitch at " + player.Location);
                Server.GetServer().Broadcast(player.Name + " don't try to rock glitch =)");
                foreach (Collider collider in Physics.OverlapSphere(player.Location, 3f))
                {
                    if (collider.gameObject.name == "SleepingBagA(Clone)")
                        TakeDamage.KillSelf(collider.GetComponent<IDMain>());
                }
                if (RockGlitchKill)
                {
                    if (player.Admin)
                    {
                        player.Message("You administrator rights allowed you to bug inside rocks.");
                        return;
                    }
                    player.Message("Glitching gets you killed.");
                    player.Kill();
                }
            }
        }

        public void EntityDeployed(Fougerite.Player Player, Fougerite.Entity Entity, Fougerite.Player actualplacer)
        {
            try
            {
                if (Entity != null)
                {
                    if (Entity.Name.Contains("Foundation") || Entity.Name.Contains("Ramp")
                        || Entity.Name.Contains("Pillar") || Entity.Name == "WoodDoor" || Entity.Name == "MetalDoor")
                    {
                        string name = Entity.Name;
                        var location = Entity.Location;
                        if (Ramp && name.Contains("Ramp"))
                        {
                            RaycastHit cachedRaycast;
                            bool cachedBoolean;
                            Facepunch.MeshBatch.MeshBatchInstance cachedhitInstance;
                            if (Facepunch.MeshBatch.MeshBatchPhysics.Raycast(location + new Vector3(0f, 0.1f, 0f), Vector3Down, out cachedRaycast, out cachedBoolean, out cachedhitInstance))
                            {
                                if (cachedhitInstance != null)
                                {
                                    var cachedComponent = cachedhitInstance.physicalColliderReferenceOnly.GetComponent<StructureComponent>();
                                    if (cachedComponent.type == StructureComponent.StructureComponentType.Foundation || cachedComponent.type == StructureComponent.StructureComponentType.Ceiling)
                                    {
                                        var weight = cachedComponent._master._weightOnMe;
                                        int ramps = 0;
                                        if (weight != null && weight.ContainsKey(cachedComponent))
                                        {
                                            ramps += weight[cachedComponent].Count(structure => structure.type == StructureComponent.StructureComponentType.Ramp);
                                        }
                                        if (ramps > 1)
                                        {
                                            Entity.Destroy();
                                            if (GiveBack && actualplacer.IsOnline)
                                            {
                                                switch (name)
                                                {
                                                    case "WoodFoundation":
                                                        name = "Wood Foundation";
                                                        break;
                                                    case "MetalFoundation":
                                                        name = "Metal Foundation";
                                                        break;
                                                    case "WoodRamp":
                                                        name = "Wood Ramp";
                                                        break;
                                                    case "MetalRamp":
                                                        name = "Metal Ramp";
                                                        break;
                                                    case "WoodPillar":
                                                        name = "Wood Pillar";
                                                        break;
                                                    case "MetalPillar":
                                                        name = "Metal Pillar";
                                                        break;
                                                }
                                                actualplacer.Inventory.AddItem(name, 1);
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                        }
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
                            var x = Physics.OverlapSphere(location, d);
                            if (
                                x.Any(
                                    l =>
                                        l.name.ToLower().Contains("woodbox") || l.name.ToLower().Contains("smallstash") ||
                                        (l.name.ToLower().Contains("door") && !isdoor)))
                            {
                                Entity.Destroy();
                                if (actualplacer.IsOnline && GiveBack)
                                {
                                    switch (name)
                                    {
                                        case "WoodFoundation":
                                            name = "Wood Foundation";
                                            break;
                                        case "MetalFoundation":
                                            name = "Metal Foundation";
                                            break;
                                        case "WoodRamp":
                                            name = "Wood Ramp";
                                            break;
                                        case "MetalRamp":
                                            name = "Metal Ramp";
                                            break;
                                        case "WoodPillar":
                                            name = "Wood Pillar";
                                            break;
                                        case "MetalPillar":
                                            name = "Metal Pillar";
                                            break;
                                        case "WoodDoor":
                                            name = "Wood Door";
                                            break;
                                        case "MetalDoor":
                                            name = "Metal Door";
                                            break;
                                    }
                                    actualplacer.Inventory.AddItem(name, 1);
                                }
                                return;
                            }
                        }
                        if (BarricadePillar)
                        {
                            if (name.Contains("Pillar"))
                            {
                                if (Physics.OverlapSphere(location, 0.34f).Where(collider => collider.GetComponent<DeployableObject>() != null).Any(collider => collider.GetComponent<DeployableObject>().name.Contains("Barricade_Fence")))
                                {
                                    actualplacer.Message("Pillar Barricade glitching is not allowed!");
                                    Entity.Destroy();
                                    if (actualplacer.IsOnline && GiveBack)
                                    {
                                        switch (name)
                                        {
                                            case "WoodPillar":
                                                name = "Wood Pillar";
                                                break;
                                            case "MetalPillar":
                                                name = "Metal Pillar";
                                                break;
                                        }
                                        actualplacer.Inventory.AddItem(name, 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("[GlitchFix] Some error showed up. Report this. " + ex);
            }
        }
    }
}
