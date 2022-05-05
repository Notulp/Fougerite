namespace Fougerite
{
    using Facepunch;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Mostly containing functions that can modify the environment.
    /// See below.
    /// </summary>
    public class World
    {
        private static World world;
        public Dictionary<string, Zone3D> zones;
        public readonly Dictionary<string, double> cache = new Dictionary<string, double>();
        private List<Entity> deployables = new List<Entity>();
        private List<Entity> doors = new List<Entity>();
        private List<Entity> structurems = new List<Entity>();
        private List<Entity> structures = new List<Entity>();
        public int CacheUpdateTime = 120;


        public World()
        {
            this.zones = new Dictionary<string, Zone3D>();
        }

        /// <summary>
        /// Returns the Fougerite ServerSaveHandler class.
        /// </summary>
        public ServerSaveHandler ServerSaveHandler
        {
            get;
            internal set;
        }

        /// <summary>
        /// Returns the instance of the class.
        /// </summary>
        /// <returns></returns>
        public static World GetWorld()
        {
            if (world == null)
            {
                world = new World();
            }
            return world;
        }

        /// <summary>
        /// Calls an airdrop to a random position.
        /// </summary>
        public void Airdrop()
        {
            this.Airdrop(1);
        }

        /// <summary>
        /// Calls an airdrop N times.
        /// </summary>
        /// <param name="rep"></param>
        public void Airdrop(int rep)
        {
            for (int i = 0; i < rep; i++)
            {
                Vector3 rpog = SupplyDropZone.GetRandomTargetPos();
                SupplyDropZone.CallAirDropAt(rpog);
            }
        }

        /*private void RandomPointOnGround(ref System.Random rand, out Vector3 onground)
        {
            onground = SupplyDropZone.GetRandomTargetPos();
            float z = (float)rand.Next(-6100, -1000);
            float x = (float)3600;
            if (z < -4900 && z >= -6100)
            {
                x = (float)rand.Next(3600, 6100);
            }
            if (z < 2400 && z >= -4900)
            {
                x = (float)rand.Next(3600, 7300);
            }
            if (z <= -1000 && z >= -2400)
            {
                x = (float)rand.Next(3600, 6700);
            }
            float y = Terrain.activeTerrain.SampleHeight(new Vector3(x, 500, z));
            onground = new Vector3(x, y, z);
        }*/

        [Obsolete("AirdropAt is deprecated, please use AirdropAtOriginal instead.", false)]
        public void AirdropAt(float x, float y, float z)
        {
            this.AirdropAt(x, y, z, 1);
        }

        [Obsolete("AirdropAt is deprecated, please use AirdropAtOriginal instead.", false)]
        public void AirdropAt(float x, float y, float z, int rep)
        {
            Vector3 target = new Vector3(x, y, z);
            this.AirdropAt(target, rep);
        }

        [Obsolete("AirdropAt is deprecated, please use AirdropAtOriginal instead.", false)]
        public void AirdropAtPlayer(Fougerite.Player p)
        {
            this.AirdropAt(p.X, p.Y, p.Z, 1);
        }

        [Obsolete("AirdropAt is deprecated, please use AirdropAtOriginal instead.", false)]
        public void AirdropAtPlayer(Fougerite.Player p, int rep)
        {
            this.AirdropAt(p.X, p.Y, p.Z, rep);
        }

        [Obsolete("AirdropAt is deprecated, please use AirdropAtOriginal instead.", false)]
        public void AirdropAt(Vector3 target, int rep = 1)
        {
            Vector3 original = target;
            System.Random rand = new System.Random();
            int r, reset;
            r = reset = 20;
            for (int i = 0; i < rep; i++)
            {
                r--;
                if (r == 0)
                {
                    r = reset;
                    target = original;
                }
                target.y = original.y + rand.Next(-5, 20) * 20;
                SupplyDropZone.CallAirDropAt(target);
                Hooks.Airdrop(target);
                Jitter(ref target);
            }
        }

        /// <summary>
        /// Calls an airdrop to the coordinates 'rep' times.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="rep"></param>
        public void AirdropAtOriginal(float x, float y, float z, int rep = 1)
        {
            this.AirdropAtOriginal(new Vector3(x, y, z), rep);
        }

        /// <summary>
        /// Calls an airdrop to the target player 'rep' times.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="rep"></param>
        public void AirdropAtOriginal(Fougerite.Player p, int rep = 1)
        {
            this.AirdropAtOriginal(p.Location, rep);
        }

        /// <summary>
        /// Calls an airdrop to the target vector 'rep' times.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="rep"></param>
        public void AirdropAtOriginal(Vector3 target, int rep = 1)
        {
            for (int i = 0; i < rep; i++)
            {
                SupplyDropZone.CallAirDropAt(target);
            }
        }

        private static void Jitter(ref Vector3 target)
        {
            Vector2 jitter = UnityEngine.Random.insideUnitCircle;
            target.x += jitter.x * 100;
            target.z += jitter.y * 100;
        }

        /// <summary>
        /// Puts out the current datablock values to a txt file.
        /// </summary>
        public void Blocks()
        {
            foreach (ItemDataBlock block in DatablockDictionary.All)
            {
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Name: " + block.name + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "ID: " + block.uniqueID + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Flags: " + block._itemFlags.ToString() + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Max Condition: " + block._maxCondition + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Loose Condition: " + block.doesLoseCondition + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Max Uses: " + block._maxUses + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Mins Uses (Display): " + block._minUsesForDisplay + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Spawn Uses Max: " + block._spawnUsesMax + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Spawn Uses Min: " + block._spawnUsesMin + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Splittable: " + block._splittable + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Category: " + block.category.ToString() + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Combinations:\n");
                foreach (ItemDataBlock.CombineRecipe recipe in block.Combinations)
                {
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "\t" + recipe.ToString() + "\n");
                }
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Icon: " + block.icon + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "IsRecycleable: " + block.isRecycleable + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "IsRepairable: " + block.isRepairable + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "IsResearchable: " + block.isResearchable + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Description: " + block.itemDescriptionOverride + "\n");
                if (block is BulletWeaponDataBlock)
                {
                    BulletWeaponDataBlock block2 = (BulletWeaponDataBlock)block;
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Min Damage: " + block2.damageMin + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Max Damage: " + block2.damageMax + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Ammo: " + block2.ammoType.ToString() + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Recoil Duration: " + block2.recoilDuration + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "RecoilPitch Min: " + block2.recoilPitchMin + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "RecoilPitch Max: " + block2.recoilPitchMax + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "RecoilYawn Min: " + block2.recoilYawMin + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "RecoilYawn Max: " + block2.recoilYawMax + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Bullet Range: " + block2.bulletRange + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Sway: " + block2.aimSway + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "SwaySpeed: " + block2.aimSwaySpeed + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Aim Sensitivity: " + block2.aimSensitivtyPercent + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "FireRate: " + block2.fireRate + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "FireRate Secondary: " + block2.fireRateSecondary + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Max Clip Ammo: " + block2.maxClipAmmo + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Reload Duration: " + block2.reloadDuration + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "Attachment Point: " + block2.attachmentPoint + "\n");
                }
                File.AppendAllText(Util.GetAbsoluteFilePath("BlocksData.txt"), "------------------------------------------------------------\n\n");
            }
        }

        /// <summary>
        /// Creates a structuremaster.
        /// A structuremaster in the game gets created when the first foundation is placed by a player.
        /// Therefore an owner of the structuremaster must be specified.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public StructureMaster CreateSM(Fougerite.Player p)
        {
            return this.CreateSM(p, p.X, p.Y, p.Z, p.PlayerClient.transform.rotation);
        }

        /// <summary>
        /// Creates a structuremaster.
        /// A structuremaster in the game gets created when the first foundation is placed by a player.
        /// Therefore an owner of the structuremaster must be specified.
        /// You may even specify the location, this is usually the foundation's position.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public StructureMaster CreateSM(Fougerite.Player p, float x, float y, float z)
        {
            return this.CreateSM(p, x, y, z, Quaternion.identity);
        }
        
        /// <summary>
        /// Creates a structuremaster.
        /// A structuremaster in the game gets created when the first foundation is placed by a player.
        /// Therefore an owner of the structuremaster must be specified.
        /// You may even specify the location, this is usually the foundation's position.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public StructureMaster CreateSM(Fougerite.Player p, float x, float y, float z, Quaternion rot)
        {
            StructureMaster master = NetCull.InstantiateClassic<StructureMaster>(Bundling.Load<StructureMaster>("content/structures/StructureMasterPrefab"), new Vector3(x, y, z), rot, 0);
            master.SetupCreator(p.PlayerClient.controllable);
            return master;
        }

        /// <summary>
        /// Creates a zone with a specific name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Zone3D CreateZone(string name)
        {
            return new Zone3D(name);
        }

        /// <summary>
        /// Gets the ground Y position at the given X, Z coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public float GetGround(float x, float z)
        {
            Vector3 above = new Vector3(x, 2000f, z);
            return (float)((RaycastHit)Physics.RaycastAll(above, Vector3.down, 2000f)[0]).point.y;
        }

        /// <summary>
        /// Gets the ground Y position at the given Vector.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public float GetGround(Vector3 target)
        {
            Vector3 above = new Vector3(target.x, 2000f, target.z);
            return (float)((RaycastHit)Physics.RaycastAll(above, Vector3.down, 2000f)[0]).point.y;
        }

        /// <summary>
        /// Gets the TerrainHight at the given Vector.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public float GetTerrainHeight(Vector3 target)
        {
            return Terrain.activeTerrain.SampleHeight(target);
        }

        /// <summary>
        /// Gets the TerrainHight at the given coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public float GetTerrainHeight(float x, float y, float z)
        {
            return GetTerrainHeight(new Vector3(x, y, z));
        }
        
        /// <summary>
        /// Gets the terrain steepness at the target vector.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public float GetTerrainSteepness(Vector3 target)
        {
            return Terrain.activeTerrain.terrainData.GetSteepness(target.x, target.z);
        }

        /// <summary>
        /// Gets the terrain steepness at the given X, Z coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public float GetTerrainSteepness(float x, float z)
        {
            return Terrain.activeTerrain.terrainData.GetSteepness(x, z);
        }

        /// <summary>
        /// Returns the Ground Distance from the specified coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public float GetGroundDist(float x, float y, float z)
        {
            float ground = GetGround(x, z);
            return y - ground;
        }

        /// <summary>
        /// Returns the Ground Distance from the specified Vector.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public float GetGroundDist(Vector3 target)
        {
            float ground = GetGround(target);
            return target.y - ground;
        }

        /// <summary>
        /// Lists all the lootspawnlist values to a txt file.
        /// </summary>
        public void Lists()
        {
            foreach (LootSpawnList list in DatablockDictionary._lootSpawnLists.Values)
            {
                File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Name: " + list.name + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Min Spawn: " + list.minPackagesToSpawn + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Max Spawn: " + list.maxPackagesToSpawn + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "NoDuplicate: " + list.noDuplicates + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "OneOfEach: " + list.spawnOneOfEach + "\n");
                File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Entries:\n");
                foreach (LootSpawnList.LootWeightedEntry entry in list.LootPackages)
                {
                    File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Amount Min: " + entry.amountMin + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Amount Max: " + entry.amountMax + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Weight: " + entry.weight + "\n");
                    File.AppendAllText(Util.GetAbsoluteFilePath("Lists.txt"), "Object: " + entry.obj.ToString() + "\n\n");
                }
            }
        }

        /// <summary>
        /// Lists all the prefabs to a txt file.
        /// </summary>
        public void Prefabs()
        {
            foreach (ItemDataBlock block in DatablockDictionary.All)
            {
                if (block is DeployableItemDataBlock)
                {
                    DeployableItemDataBlock block2 = block as DeployableItemDataBlock;
                    File.AppendAllText(Util.GetAbsoluteFilePath("Prefabs.txt"), "[\"" + block2.ObjectToPlace.name + "\", \"" + block2.DeployableObjectPrefabName + "\"],\n");
                }
                else if (block is StructureComponentDataBlock)
                {
                    StructureComponentDataBlock block3 = block as StructureComponentDataBlock;
                    File.AppendAllText(Util.GetAbsoluteFilePath("Prefabs.txt"),
                        "[\"" + block3.structureToPlacePrefab.name + "\", \"" + block3.structureToPlaceName + "\"],\n");
                }
            }
        }


        /// <summary>
        /// Loops through all the datablocks, and writes them to a txt file.
        /// </summary>
        public void DataBlocks()
        {
            foreach (ItemDataBlock block in DatablockDictionary.All)
            {
                File.AppendAllText(Util.GetAbsoluteFilePath("DataBlocks.txt"), string.Format("name={0} uniqueID={1}\n", block.name, block.uniqueID));
            }
        }

        /// <summary>
        /// Spawns a prefab at the vector position.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public object Spawn(string prefab, Vector3 location)
        {
            return this.Spawn(prefab, location, 1);
        }

        /// <summary>
        /// Spawns a prefab at the vector position N times.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="location"></param>
        /// <param name="rep"></param>
        /// <returns></returns>
        public object Spawn(string prefab, Vector3 location, int rep)
        {
            return this.Spawn(prefab, location, Quaternion.identity, rep);
        }

        /// <summary>
        /// Spawns a prefab at the coordinates.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public object Spawn(string prefab, float x, float y, float z)
        {
            return this.Spawn(prefab, new Vector3(x, y, z), 1);
        }

        /// <summary>
        /// Spawns a prefab at the given vector, rotation, N times.
        /// IMPORTANT: Returns the prefab as an Entity class.
        /// Entity class only supports specific types, like LootableObject
        /// SupplyCrate, ResourceTarget, DeployableObject, StructureComponent,
        /// StructureMaster.
        /// The other spawn methods are returning the gameobject instead.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="location"></param>
        /// <param name="rotation"></param>
        /// <param name="rep"></param>
        /// <returns></returns>
        public Entity SpawnEntity(string prefab, Vector3 location, Quaternion rotation, int rep = 1)
        {
            Entity obj2 = null;
            prefab = prefab.Trim();
            try 
            {
                for (int i = 0; i < rep; i++)
                {
                    GameObject obj3 = NetCull.InstantiateStatic(prefab, location, rotation);
                    StructureComponent build = obj3.GetComponent<StructureComponent>();
                    if (build != null)
                    {
                        
                        obj2 = new Entity(build);
                    } 
                    else if (obj3.GetComponent<LootableObject>())
                    {
                        obj2 = new Entity(obj3.GetComponent<LootableObject>());
                    }
                    else if (obj3.GetComponent<SupplyCrate>())
                    {
                        obj2 = new Entity(obj3.GetComponent<SupplyCrate>());
                    }
                    else if (obj3.GetComponent<ResourceTarget>())
                    {
                        obj2 = new Entity(obj3.GetComponent<ResourceTarget>());
                    }
                    else
                    {
                        DeployableObject obj4 = obj3.GetComponent<DeployableObject>();
                        if (obj4 != null)
                        {
                            obj4.ownerID = 0L;
                            obj4.creatorID = 0L;
                            obj4.CacheCreator();
                            obj4.CreatorSet();
                            obj2 = new Entity(obj4);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("SpawnEntity error: " + e.ToString());
            }
            return obj2;
        }

        private object Spawn(string prefab, Vector3 location, Quaternion rotation, int rep)
        {
            prefab = prefab.Trim();
            object obj2 = null;
            try 
            {
                for (int i = 0; i < rep; i++)
                {
                    if (prefab == ":player_soldier")
                    {
                        obj2 = NetCull.InstantiateDynamic(uLink.NetworkPlayer.server, prefab, location, rotation);
                    } 
                    else if (prefab.Contains("C130"))
                    {
                        obj2 = NetCull.InstantiateClassic(prefab, location, rotation, 0);
                    } 
                    else
                    {
                        GameObject obj3 = NetCull.InstantiateStatic(prefab, location, rotation);
                        obj2 = obj3;
                        StructureComponent component = obj3.GetComponent<StructureComponent>();
                        if (component != null)
                        {
                            obj2 = new Entity(component);
                        } 
                        else if (obj3.GetComponent<LootableObject>())
                        {
                            obj2 = new Entity(obj3.GetComponent<LootableObject>());
                        }
                        else if (obj3.GetComponent<SupplyCrate>())
                        {
                            obj2 = new Entity(obj3.GetComponent<SupplyCrate>());
                        }
                        else if (obj3.GetComponent<ResourceTarget>())
                        {
                            obj2 = new Entity(obj3.GetComponent<ResourceTarget>());
                        }
                        else
                        {
                            DeployableObject obj4 = obj3.GetComponent<DeployableObject>();
                            if (obj4 != null)
                            {
                                obj4.ownerID = 0L;
                                obj4.creatorID = 0L;
                                obj4.CacheCreator();
                                obj4.CreatorSet();
                                obj2 = new Entity(obj4);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Spawn error: " + e.ToString());
            }
            return obj2;
        }

        /// <summary>
        /// Spawns a prefab at the given coordinates, N times.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="rep"></param>
        /// <returns></returns>
        public object Spawn(string prefab, float x, float y, float z, int rep)
        {
            return this.Spawn(prefab, new Vector3(x, y, z), Quaternion.identity, rep);
        }

        /// <summary>
        /// Spawns a prefab at the given coordinates, rotation.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public object Spawn(string prefab, float x, float y, float z, Quaternion rot)
        {
            return this.Spawn(prefab, new Vector3(x, y, z), rot, 1);
        }

        /// <summary>
        /// Spawns a prefab at the given coordinates, rotation, N times.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="rot"></param>
        /// <param name="rep"></param>
        /// <returns></returns>
        public object Spawn(string prefab, float x, float y, float z, Quaternion rot, int rep)
        {
            return this.Spawn(prefab, new Vector3(x, y, z), rot, rep);
        }

        /// <summary>
        /// Spawns a prefab at the given player.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public object SpawnAtPlayer(string prefab, Fougerite.Player p)
        {
            return this.Spawn(prefab, p.Location, p.PlayerClient.transform.rotation, 1);
        }
        
        /// <summary>
        /// Spawns a prefab at the given player, N times.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="p"></param>
        /// <param name="rep"></param>
        /// <returns></returns>
        public object SpawnAtPlayer(string prefab, Fougerite.Player p, int rep)
        {
            return this.Spawn(prefab, p.Location, p.PlayerClient.transform.rotation, rep);
        }

        /// <summary>
        /// Gets or Sets the DayLength.
        /// </summary>
        public float DayLength
        {
            get { return env.daylength; }
            set { env.daylength = value; }
        }

        public IEnumerable<Entity> BasicDoors(bool forceupdate = false)
        {
            try
            {
                if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
                {
                    Logger.LogWarning("[Fougerite BasicDoors] Some plugin is calling World.BasicDoors in a Thread/Timer. This is dangerous, and may cause crashes.");
                }
                if (!this.cache.ContainsKey("BasicDoor") || forceupdate || this.doors.Count == 0)
                {
                    this.cache["BasicDoor"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                    IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<BasicDoor>()
                        select new Entity(s);
                    this.doors = source.ToList();
                }
                else if (this.cache.ContainsKey("BasicDoor"))
                {
                    double num = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds - this.cache["BasicDoor"];
                    if (num >= this.CacheUpdateTime || double.IsNaN(num) || num <= 0)
                    {
                        this.cache["BasicDoor"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                        IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<BasicDoor>()
                            select new Entity(s);
                        this.doors = source.ToList();
                    }
                }
            }
            catch
            {
                this.cache["BasicDoor"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<BasicDoor>()
                                             select new Entity(s);
                this.doors = source.ToList();
            }
            return this.doors;
        }

        public IEnumerable<Entity> DeployableObjects(bool forceupdate = false)
        {
            try
            {
                if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
                {
                    Logger.LogWarning("[Fougerite DeployableObjects] Some plugin is calling World.DeployableObjects in a Thread/Timer. This is dangerous, and may cause crashes.");
                }
                if (!this.cache.ContainsKey("DeployableObject") || forceupdate || this.deployables.Count == 0)
                {
                    this.cache["DeployableObject"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                    IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<DeployableObject>()
                        select new Entity(s);
                    this.deployables = source.ToList();
                }
                else if (this.cache.ContainsKey("DeployableObject"))
                {
                    double num = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds - this.cache["DeployableObject"];
                    if (num >= this.CacheUpdateTime || double.IsNaN(num) || num <= 0)
                    {
                        this.cache["DeployableObject"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                        IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<DeployableObject>()
                            select new Entity(s);
                        this.deployables = source.ToList();
                    }
                }
            }
            catch
            {
                this.cache["DeployableObject"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<DeployableObject>()
                                             select new Entity(s);
                this.deployables = source.ToList();
            }
            return this.deployables;
        }

        public IEnumerable<Entity> StructureComponents(bool forceupdate = false)
        {
            try
            {
                if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
                {
                    Logger.LogWarning("[Fougerite StructureComponents] Some plugin is calling World.StructureComponents in a Thread/Timer. This is dangerous, and may cause crashes.");
                }
                if (!this.cache.ContainsKey("StructureComponent") || forceupdate || this.structures.Count == 0)
                {
                    this.cache["StructureComponent"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                    IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<StructureComponent>()
                        select new Entity(s);
                    this.structures = source.ToList();
                }
                else if (this.cache.ContainsKey("StructureComponent"))
                {
                    double num = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds - this.cache["StructureComponent"];
                    if (num >= this.CacheUpdateTime || double.IsNaN(num) || num <= 0)
                    {
                        this.cache["StructureComponent"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                        IEnumerable<Entity> source =
                            from s in UnityEngine.Object.FindObjectsOfType<StructureComponent>()
                            select new Entity(s);
                        this.structures = source.ToList();
                    }
                }
            }
            catch
            {
                this.cache["StructureComponent"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                IEnumerable<Entity> source =
                    from s in UnityEngine.Object.FindObjectsOfType<StructureComponent>()
                    select new Entity(s);
                this.structures = source.ToList();
            }
            return this.structures;
        }

        public IEnumerable<Entity> StructureMasters(bool forceupdate = false)
        {
            try
            {
                if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
                {
                    Logger.LogWarning("[Fougerite StructureMasters] Some plugin is calling World.StructureMasters in a Thread/Timer. This is dangerous, and may cause crashes.");
                }
                if (!this.cache.ContainsKey("StructureMaster") || forceupdate || this.structurems.Count == 0)
                {
                    this.cache["StructureMaster"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                    IEnumerable<Entity> source = from s in StructureMaster.AllStructures
                        select new Entity(s);
                    this.structurems = source.ToList();
                }
                else if (this.cache.ContainsKey("StructureMaster"))
                {
                    double num = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds - this.cache["StructureMaster"];
                    if (num >= this.CacheUpdateTime || double.IsNaN(num) || num <= 0)
                    {
                        this.cache["StructureMaster"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                        IEnumerable<Entity> source = from s in StructureMaster.AllStructures
                            select new Entity(s);
                        this.structurems = source.ToList();
                    }
                }
            }
            catch
            {
                this.cache["StructureMaster"] = TimeSpan.FromTicks(DateTime.Now.Ticks).TotalSeconds;
                IEnumerable<Entity> source = from s in StructureMaster.AllStructures
                                             select new Entity(s);
                this.structurems = source.ToList();
            }
            return this.structurems;
        }

        /// <summary>
        /// Returns all the LootableObjects.
        /// THIS METHOD IS NOT SAFE TO CALL IN A SUBTHREAD DUE TO Object.FindObjectsOfType.
        /// CONSIDER USING Util.FindClosestEntity or Util.FindEntitysAroundFast or Util.FindClosestObject or Util.FindObjectsAroundFast
        /// </summary>
        public IEnumerable<Entity> LootableObjects
        {
            get
            {
                if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
                {
                    Logger.LogWarning("[Fougerite LootableObjects] Some plugin is calling World.LootableObjects in a Thread/Timer. This is dangerous, and may cause crashes.");
                }
                IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<LootableObject>()
                                             select new Entity(s);
                return source.ToList();
            }
        }

        /// <summary>
        /// Returns all the SupplyCrates.
        /// THIS METHOD IS NOT SAFE TO CALL IN A SUBTHREAD DUE TO Object.FindObjectsOfType.
        /// CONSIDER USING Util.FindClosestEntity or Util.FindEntitysAroundFast or Util.FindClosestObject or Util.FindObjectsAroundFast
        /// </summary>
        public IEnumerable<Entity> SupplyCrates
        {
            get
            {
                if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
                {
                    Logger.LogWarning("[Fougerite SupplyCrates] Some plugin is calling World.SupplyCrates in a Thread/Timer. This is dangerous, and may cause crashes.");
                }
                IEnumerable<Entity> source = from s in UnityEngine.Object.FindObjectsOfType<SupplyCrate>()
                                             select new Entity(s);
                return source.ToList();
            }
        }

        /// <summary>
        /// Returns all the Entities into a list.
        /// THIS METHOD IS NOT SAFE TO CALL IN A SUBTHREAD DUE TO Object.FindObjectsOfType.
        /// CONSIDER USING Util.FindClosestEntity or Util.FindEntitysAroundFast or Util.FindClosestObject or Util.FindObjectsAroundFast
        /// </summary>
        public List<Entity> Entities
        {
            get
            {
                try
                {
                    if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
                    {
                        Logger.LogWarning("[Fougerite Entities] Some plugin is calling World.Entities in a Thread/Timer. This is dangerous, and may cause crashes.");
                    }
                    var structs = UnityEngine.Object.FindObjectsOfType<StructureComponent>();
                    var deployables = UnityEngine.Object.FindObjectsOfType<DeployableObject>();
                    var crates = UnityEngine.Object.FindObjectsOfType<SupplyCrate>();
                    IEnumerable<Entity> component = structs.Select(x => new Entity(x)).ToList();
                    IEnumerable<Entity> deployable = deployables.Select(x => new Entity(x)).ToList();
                    IEnumerable<Entity> supplydrop = crates.Select(x => new Entity(x)).ToList();
                    // this is much faster than Concat
                    List<Entity> entities = new List<Entity>(component.Count() + deployable.Count() + supplydrop.Count());
                    entities.AddRange(component);
                    entities.AddRange(deployable);
                    if (supplydrop.Any())
                    {
                        entities.AddRange(supplydrop);
                    }
                    return entities;
                }
                catch
                {
                    return new List<Entity>();
                }
            }
        }

        /// <summary>
        /// Gets or Sets the NightLength
        /// </summary>
        public float NightLength
        {
            get { return env.nightlength; }
            set { env.nightlength = value; }
        }

        /// <summary>
        /// Gets or Sets the current time.
        /// </summary>
        public float Time
        {
            get
            {
                try
                {
                    float hour = EnvironmentControlCenter.Singleton.GetTime();
                    return hour;
                } catch (NullReferenceException)
                {
                    return 12f;
                }
            }
            set
            {
                float hour = value;
                if (hour < 0f || hour > 24f)
                    hour = 12f;

                try
                {
                    EnvironmentControlCenter.Singleton.SetTime(hour);
                } catch (Exception)
                {
                }
            }
        }
    }
}