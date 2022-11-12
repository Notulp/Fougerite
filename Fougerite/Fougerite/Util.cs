using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Facepunch.MeshBatch;
using Fougerite.Permissions;
using IronPython.Runtime.Types;
using UnityEngine;
using String = Facepunch.Utility.String;

namespace Fougerite
{
    /// <summary>
    /// This class contains some useful methods.
    /// </summary>
    public class Util
    {
        private readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static Util _util;

        /// <summary>
        /// All unstackable item names in rust legacy.
        /// </summary>
        public static readonly string[] UStackable =
        {
            "Spike Wall", "Large Spike Wall", "Wood Gate",
            "Wood Gateway", "Wood Shelter", "Bed", "Workbench", "Furnace", "Repair Bench",
            "Rock", "Stone Hatchet", "Hatchet", "Pick Axe", "Torch", "Flashlight Mod",
            "9mm Pistol", "M4", "Hand Cannon", "Pipe Shotgun", "Bolt Action Rifle",
            "P250", "Shotgun", "MP5A4", "Hunting Bow", "Revolver",
            "Holo sight", "Silencer", "Laser Sight",
            "Cloth Helmet", "Leather Helmet", "Rad Suit Helmet", "Kevlar Helmet", "Invisible Helmet",
            "Cloth Vest", "Leather Vest", "Rad Suit Vest", "Kevlar Vest", "Invisible Vest",
            "Cloth Pants", "Leather Pants", "Rad Suit Pants", "Kevlar Pants", "Invisible Pants",
            "Cloth Boots", "Leather Boots", "Rad Suit Boots", "Kevlar Boots", "Invisible Boots",
            "Blood Draw Kit", "Supply Signal", "Research Kit 1", "Uber Hatchet", "Uber Hunting Bow"
        };

        /// <summary>
        /// PlayerActions that I debugged myself. These are sent by the On_PlayerMove event.
        /// </summary>
        public enum PlayerActions
        {
            Standing = 4096,
            Moving = 4160,
            AimMoving = 4164,
            AimMovingShooting = 4172,
            Jumping = 4112,
            Running = 4162,
            RunJump = 4176,
            JumpChat = 144, // Possible Flyhack
            ESC = 128,
            TAB = 4224,
            Aiming = 4100,
            Shooting = 4104,
            MoveShoot = 4168,
            AimShoot = 4108,
            RightClickWhileReload = 4352,
            RightClickWhileGunTake = 4353,
            RightClickWhileGunTakeMove = 4416,
            Crouch = 4097,
            CrouchAim = 4101,
            CrouchMoveShoot = 4169,
            CrouchAimMove = 4165,
            CrouchAimMoveShoot = 4173,
            CrouchShoot = 4105,
            CrouchAimShoot = 4109
        }

        [DllImport("kernel32")]
        public static extern ulong GetTickCount64();

        /// <summary>
        /// Sends a console message to everyone.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="adminOnly"></param>
        public void ConsoleLog(string str, [Optional, DefaultParameterValue(false)] bool adminOnly)
        {
            foreach (Player player in Server.GetServer().Players)
            {
                if (!player.IsOnline)
                    continue;

                if (!adminOnly)
                {
                    ConsoleNetworker.singleton.networkView.RPC("CL_ConsoleMessage", player.PlayerClient.netPlayer, str);
                }
                else if (player.Admin || PermissionSystem.GetPermissionSystem().PlayerHasPermission(player, "fougerite.console"))
                {
                    ConsoleNetworker.singleton.networkView.RPC("CL_ConsoleMessage", player.PlayerClient.netPlayer, str);
                }
            }
        }

        /// <summary>
        /// Creates an array. Useful for JS plugins.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public object CreateArrayInstance(string name, int size)
        {
            Type type;
            if (!TryFindType(name.Replace('.', '+'), out type))
            {
                return null;
            }

            if (type.BaseType?.Name == "ScriptableObject")
            {
                return ScriptableObject.CreateInstance(name);
            }

            return Array.CreateInstance(type, size);
        }

        /// <summary>
        /// Tries to create an instance to the specified class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CreateInstance(string name, params object[] args)
        {
            Type type;
            if (!TryFindType(name.Replace('.', '+'), out type))
            {
                return null;
            }

            if (type.BaseType?.Name == "ScriptableObject")
            {
                return ScriptableObject.CreateInstance(name);
            }

            return Activator.CreateInstance(type, args);
        }

        /// <summary>
        /// Creates a Quaterion
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <returns></returns>
        public Quaternion CreateQuat(float x, float y, float z, float w)
        {
            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Creates a Vector3
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public Vector3 CreateVector(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Creates a Vector2
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Vector2 CreateVector2(float x, float y)
        {
            return new Vector2(x, y);
        }

        /// <summary>
        /// Tries to parse a string to Vector2.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Vector2 ConvertStringToVector2(string s)
        {
            try
            {
                s = s.Replace("(", "").Replace(")", "").Replace(" ", "");
                string[] spl = s.Split(',');
                float.TryParse(spl[0], out float f1);
                float.TryParse(spl[1], out float f2);
                return new Vector2(f1, f2);
            }
            catch
            {
                return Vector2.zero;
            }
        }

        /// <summary>
        /// Tries to parse a string to Vector3
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Vector3 ConvertStringToVector3(string s)
        {
            try
            {
                s = s.Replace("(", "").Replace(")", "").Replace(" ", "");
                string[] spl = s.Split(',');
                float.TryParse(spl[0], out float f1);
                float.TryParse(spl[1], out float f2);
                float.TryParse(spl[2], out float f3);
                return new Vector3(f1, f2, f3);
            }
            catch
            {
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Uses Netcull to Destroy a gameobject.
        /// </summary>
        /// <param name="go"></param>
        public void DestroyObject(GameObject go)
        {
            NetCull.Destroy(go);
        }

        /// <summary>
        /// Clears up the unneccessary \ signs.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalizePath(string path)
        {
            string normal = path.Replace(@"\\", @"\").Replace(@"//", @"/").Trim();
            return normal;
        }

        /// <summary>
        /// Gets the path of the file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetAbsoluteFilePath(string fileName)
        {
            return Path.Combine(Config.GetPublicFolder(), fileName);
        }

        /// <summary>
        /// Gets the root folder where rust server exe is located.
        /// </summary>
        /// <returns></returns>
        public static string GetRootFolder()
        {
            return Path.GetDirectoryName(
                Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));
        }

        /// <summary>
        /// Gets the filepath to rust_server_Data folder.
        /// </summary>
        /// <returns></returns>
        public static string GetServerFolder()
        {
            return Path.Combine(
                Path.GetDirectoryName(
                    Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))),
                "rust_server_Data");
        }

        /// <summary>
        /// Tries to get the arguments by quoting them using Facepunch api.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string[] GetQuotedArgs(string s)
        {
            return String.SplitQuotesStrings(s.Trim('\\'));
        }

        /// <summary>
        /// Tries to get the variable's value using reflection
        /// </summary>
        /// <param name="className"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public object GetStaticField(string className, string field)
        {
            Type type;
            if (TryFindType(className.Replace('.', '+'), out type))
            {
                FieldInfo info = type.GetField(field, BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                {
                    return info.GetValue(null);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the Util's instance.
        /// </summary>
        /// <returns></returns>
        public static Util GetUtil()
        {
            if (_util == null)
            {
                _util = new Util();
            }

            return _util;
        }

        /// <summary>
        /// Gets the distance between 2 Vector3s
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public float GetVectorsDistance(Vector3 v1, Vector3 v2)
        {
            return Vector3.Distance(v1, v2);
        }

        /// <summary>
        /// Gets the distance between 2 Vector2s
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public float GetVector2sDistance(Vector2 v1, Vector2 v2)
        {
            return Vector2.Distance(v1, v2);
        }

        /// <summary>
        /// Gets the eyerays of a player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Ray GetEyesRay(Player player)
        {
            if (player.Character == null)
            {
                return new Ray();
            }

            Vector3 position = player.Character.transform.position;
            Vector3 direction = player.Character.eyesRay.direction;
            position.y += player.Character.stateFlags.crouch ? 1f : 1.85f;
            return new Ray(position, direction);
        }

        /// <summary>
        /// Gets the last save file of the server.
        /// </summary>
        /// <returns></returns>
        public string GetLastSaveFile()
        {
            FileInfo info = null;
            string autoSavePath = ServerSaveManager.autoSavePath;
            if (File.Exists(autoSavePath))
            {
                info = new FileInfo(autoSavePath);
            }

            if ((info == null) || (info.Length == 0L))
            {
                for (int i = 0; i < 20; i++)
                {
                    autoSavePath = $"{ServerSaveManager.autoSavePath}.old.{i}";
                    if (File.Exists(autoSavePath) && (new FileInfo(autoSavePath).Length > 0L))
                    {
                        return autoSavePath;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the object that is in line between two vectors.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="point"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public GameObject GetLineObject(Vector3 start, Vector3 end, out Vector3 point, int layerMask = -1)
        {
            RaycastHit hit;
            bool flag;
            MeshBatchInstance instance;
            point = Vector3.zero;
            if (!Facepunch.MeshBatch.MeshBatchPhysics.Linecast(start, end, out hit, layerMask, out flag, out instance))
            {
                return null;
            }

            IDMain main = flag ? instance.idMain : IDBase.GetMain(hit.collider);
            point = hit.point;
            return ((main != null) ? main.gameObject : hit.collider.gameObject);
        }

        /// <summary>
        /// Gets the object that the player is looking at.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public GameObject GetLookObject(Player player, int layerMask = -1)
        {
            if (player.Character == null)
            {
                return null;
            }

            Vector3 position = player.Character.transform.position;
            Vector3 direction = player.Character.eyesRay.direction;
            position.y += player.Character.stateFlags.crouch ? 1f : 1.85f;
            return GetLookObject(new Ray(position, direction));
        }

        /// <summary>
        /// Gets the object where the ray is going at.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="distance"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public GameObject GetLookObject(Ray ray, float distance = 300f, int layerMask = -1)
        {
            Vector3 zero = Vector3.zero;
            return GetLookObject(ray, out zero, distance, layerMask);
        }

        /// <summary>
        /// Gets the object where the ray is going at.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="point"></param>
        /// <param name="distance"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public GameObject GetLookObject(Ray ray, out Vector3 point, float distance = 300f, int layerMask = -1)
        {
            RaycastHit hit;
            bool flag;
            MeshBatchInstance instance;
            point = Vector3.zero;
            if (!Facepunch.MeshBatch.MeshBatchPhysics.Raycast(ray, out hit, distance, layerMask, out flag,
                    out instance))
            {
                return null;
            }

            IDMain main = flag ? instance.idMain : IDBase.GetMain(hit.collider);
            point = hit.point;
            return ((main != null) ? main.gameObject : hit.collider.gameObject);
        }

        /// <summary>
        /// Gets the object where the player is looking at.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Ray GetLookRay(Player player)
        {
            if (player.Character == null)
            {
                return new Ray();
            }

            Vector3 position = player.Character.transform.position;
            Vector3 direction = player.Character.eyesRay.direction;
            position.y += player.Character.stateFlags.crouch ? 0.85f : 1.65f;
            return new Ray(position, direction);
        }

        public static Hashtable HashtableFromFile(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (Hashtable)formatter.Deserialize(stream);
                }
            }
            catch
            {
                return new Hashtable();
            }
        }

        public static void HashtableToFile(Hashtable ht, string path)
        {
            var storage = ht;
            List<object> keys = new List<object>();
            try
            {
                // Running Through Table Names
                foreach (object x in storage.Keys)
                {
                    // Getting the keys and values
                    if (storage[x] is Hashtable hashtable)
                    {
                        // Running through keys
                        foreach (object y in hashtable.Keys)
                        {
                            // Getting value
                            if (y != null)
                            {
                                Type z = y.GetType();
                                if (z == typeof(BuiltinFunction))
                                {
                                    if (!keys.Contains(y)) keys.Add(y);
                                    Logger.LogDebug(
                                        $"[DataStore] {x} - {y} is not serializable. Saving skipped for It.");
                                }
                                else if (!z.IsSerializable)
                                {
                                    Logger.LogDebug(
                                        $"[DataStore] {x} - {y} is not serializable. Saving skipped for It.");
                                    if (!keys.Contains(y)) keys.Add(y);
                                }

                                if (hashtable[y] != null)
                                {
                                    Type z2 = hashtable[y].GetType();
                                    if (z2 == typeof(BuiltinFunction))
                                    {
                                        if (!keys.Contains(y)) keys.Add(y);
                                        Logger.LogDebug(
                                            $"[DataStore] {x} - {y} is not serializable. (Table's key) Saving skipped for It.");
                                    }
                                    else if (!z2.IsSerializable)
                                    {
                                        Logger.LogDebug(
                                            $"[DataStore] {x} - {y} is not serializable. Saving skipped for It.");
                                        if (!keys.Contains(y)) keys.Add(y);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[DataStore] Failed to search for not serializable values!");
                Logger.LogDebug($"[DataStore] Error: {ex}");
            }

            try
            {
                // Running through table names
                foreach (object x in storage.Keys)
                {
                    // Getting Keys and Values
                    if (storage[x] is Hashtable hashtable)
                    {
                        foreach (object y in keys)
                        {
                            if (hashtable.ContainsKey(y))
                            {
                                Logger.LogDebug($"[DataStore] Key Ignored: {y} from table: {storage[x]}");
                                hashtable.Remove(y);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[DataStore] Failed to remove not serializable values!");
                Logger.LogDebug($"[DataStore] Error: {ex}");
            }

            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, storage);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("[DataStore] Failed to save datastore! ");
                Logger.LogDebug($"[DataStore] Error: {ex}");
            }
        }

        /// <summary>
        /// Gets a Vector3 x meters away from the player.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Vector3 Infront(Player p, float length)
        {
            Transform transform = p.PlayerClient.controllable.transform;
            return (transform.position + transform.forward * length);
        }

        public object InvokeStatic(string className, string method, object[] args)
        {
            Type type;
            if (!TryFindType(className.Replace('.', '+'), out type))
            {
                return null;
            }

            MethodInfo info = type.GetMethod(method, BindingFlags.Static);
            if (info == null)
            {
                return null;
            }

            if (info.ReturnType == typeof(void))
            {
                info.Invoke(null, args);
                return true;
            }

            return info.Invoke(null, args);
        }

        public bool IsNull(object obj)
        {
            return (obj == null);
        }

        /// <summary>
        /// Logs to the console and to the Fougerite logs.
        /// </summary>
        /// <param name="str"></param>
        public void Log(string str)
        {
            Logger.Log(str);
        }

        public Match Regex(string input, string match)
        {
            return new Regex(input).Match(match);
        }

        public Quaternion RotateX(Quaternion q, float angle)
        {
            return (q * Quaternion.Euler(angle, 0f, 0f));
        }

        public Quaternion RotateY(Quaternion q, float angle)
        {
            return (q * Quaternion.Euler(0f, angle, 0f));
        }

        public Quaternion RotateZ(Quaternion q, float angle)
        {
            return (q * Quaternion.Euler(0f, 0f, angle));
        }

        [Obsolete("Use the Player class's message system instead.", false)]
        public static void say(uLink.NetworkPlayer player, string playername, string arg)
        {
            Player pl = Player.FindByNetworkPlayer(player);
            if (pl == null) return;
            if (!pl.IsOnline) return;
            if (!string.IsNullOrEmpty(arg) && !string.IsNullOrEmpty(playername) && player != null)
                ConsoleNetworker.SendClientCommand(player, $"chat.add {playername} {arg}");
        }

        [Obsolete("Use the Server class's broadcast methods instead.", false)]
        public static void sayAll(string customName, string arg)
        {
            ConsoleNetworker.Broadcast($"chat.add {String.QuoteSafe(customName)} {String.QuoteSafe(arg)}");
        }

        [Obsolete("Use the Server class's broadcast methods instead.", false)]
        public static void sayAll(string arg)
        {
            if (!string.IsNullOrEmpty(arg))
                ConsoleNetworker.Broadcast(
                    $"chat.add {String.QuoteSafe(Server.GetServer().server_message_name)} {String.QuoteSafe(arg)}");
        }

        [Obsolete("Use the Player class's message system instead.", false)]
        public static void sayUser(uLink.NetworkPlayer player, string arg)
        {
            Player pl = Player.FindByNetworkPlayer(player);
            if (pl == null) return;
            if (!pl.IsOnline) return;
            if (!string.IsNullOrEmpty(arg) && player != null)
                ConsoleNetworker.SendClientCommand(player,
                    $"chat.add {String.QuoteSafe(Server.GetServer().server_message_name)} {String.QuoteSafe(arg)}");
        }

        [Obsolete("Use the Player class's message system instead.", false)]
        public static void sayUser(uLink.NetworkPlayer player, string customName, string arg)
        {
            Player pl = Player.FindByNetworkPlayer(player);
            if (pl == null) return;
            if (!pl.IsOnline) return;
            if (!string.IsNullOrEmpty(arg) && !string.IsNullOrEmpty(customName) && player != null)
                ConsoleNetworker.SendClientCommand(player,
                    $"chat.add {String.QuoteSafe(customName)} {String.QuoteSafe(arg)}");
        }

        public void SetStaticField(string className, string field, object val)
        {
            Type type;
            if (TryFindType(className.Replace('.', '+'), out type))
            {
                FieldInfo info = type.GetField(field, BindingFlags.Public | BindingFlags.Static);
                if (info != null)
                {
                    info.SetValue(null, Convert.ChangeType(val, info.FieldType));
                }
            }
        }

        /// <summary>
        /// Splits the string to the specified amount of parts.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="partLength"></param>
        /// <returns></returns>
        public IEnumerable<string> SplitInParts(string s, int partLength)
        {
            if (string.IsNullOrEmpty(s) || partLength <= 0) 
                yield return null;

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
        }
        
        /// <summary>
        /// Splits the string to the specified amount of parts.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="partLength"></param>
        /// <returns></returns>
        public List<string> SplitInPartsLs(string s, int partLength)
        {
            List<string> data = new List<string>();
            
            if (string.IsNullOrEmpty(s) || partLength <= 0) 
                return data;
            
            for (var i = 0; i < s.Length; i += partLength)
                data.Add(s.Substring(i, Math.Min(partLength, s.Length - i)));

            return data;
        }

        public TimeSpan ConvertToTime(long ticks)
        {
            TimeSpan ts = TimeSpan.FromTicks(ticks);
            return ts;
        }

        /// <summary>
        /// Tries to find the specified Class type in through the loaded assemblies. Useful for Scripting languages.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool TryFindType(string typeName, out Type t)
        {
            lock (_typeCache)
            {
                if (!_typeCache.TryGetValue(typeName, out t))
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        t = assembly.GetType(typeName);
                        if (t != null)
                        {
                            break;
                        }
                    }

                    _typeCache[typeName] = t;
                }
            }

            return (t != null);
        }

        /// <summary>
        /// Tries to find the specified Class type in through the loaded assemblies. Useful for Scripting languages.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Type TryFindReturnType(string typeName)
        {
            if (TryFindType(typeName, out Type t))
                return t;
            
            throw new Exception($"Type not found {typeName}");
        }

        /// <summary>
        /// Deep Clones the specified object.
        /// </summary>
        /// <param name="item"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T DeepCopy<T>(T item)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, item);
            stream.Seek(0, SeekOrigin.Begin);
            T result = (T)formatter.Deserialize(stream);
            stream.Close();
            return result;
        }

        public bool ContainsString(string str, string key)
        {
            if (str.Contains(key))
            {
                return true;
            }

            return false;
        }

        public ItemDataBlock ConvertNameToData(string name)
        {
            ItemDataBlock byName = DatablockDictionary.GetByName(name);
            if (byName != null)
            {
                return byName;
            }

            return null;
        }

        public BlueprintDataBlock BlueprintOfItem(ItemDataBlock item)
        {
            return DatablockDictionary.All.OfType<BlueprintDataBlock>().FirstOrDefault(obj => obj.resultItem == item);
        }

        [Obsolete("Use FindDeployableAt", false)]
        public Entity FindChestAt(Vector3 givenPosition, float dist = 1f, bool forceupdate = false)
        {
            return FindDeployableAt(givenPosition, dist, forceupdate);
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public Entity FindDeployableAt(Vector3 givenPosition, float dist = 1f, bool forceupdate = false)
        {
            foreach (var x in World.GetWorld().DeployableObjects(forceupdate))
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) return x;
            }

            return null;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public Entity FindDoorAt(Vector3 givenPosition, float dist = 2f, bool forceupdate = false)
        {
            foreach (var x in World.GetWorld().BasicDoors(forceupdate))
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) return x;
            }

            return null;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public Entity FindStructureAt(Vector3 givenPosition, float dist = 1f, bool forceupdate = false)
        {
            foreach (var x in World.GetWorld().StructureComponents(forceupdate))
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) return x;
            }

            return null;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public Entity FindLootableAt(Vector3 givenPosition, float dist = 1f)
        {
            foreach (var x in World.GetWorld().LootableObjects)
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) return x;
            }

            return null;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public Entity FindEntityAt(Vector3 givenPosition, float dist = 1f)
        {
            foreach (var x in World.GetWorld().Entities)
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) return x;
            }

            return null;
        }

        /// <summary>
        /// Finds the closest gameobject that is convertible to an Entity.
        /// </summary>
        /// <param name="givenPosition"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        public Entity FindClosestEntity(Vector3 givenPosition, float dist = 1f)
        {
            Collider[] array = Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(givenPosition, dist);
            if (array.Length == 0)
            {
                return null;
            }

            Collider closest = array[0];
            if (array.Length > 1)
            {
                for (int i = 1; i < array.Length; i++)
                {
                    if (Vector3.Distance(array[i].transform.position, givenPosition) <
                        Vector3.Distance(closest.transform.position, givenPosition))
                    {
                        closest = array[i];
                    }
                }
            }

            if (closest.gameObject.GetComponent<StructureMaster>())
            {
                return new Entity(closest.gameObject.GetComponent<StructureMaster>());
            }

            if (closest.gameObject.GetComponent<StructureComponent>())
            {
                return new Entity(closest.gameObject.GetComponent<StructureComponent>());
            }

            if (closest.gameObject.GetComponent<DeployableObject>())
            {
                return new Entity(closest.gameObject.GetComponent<DeployableObject>());
            }

            if (closest.gameObject.GetComponent<LootableObject>())
            {
                return new Entity(closest.gameObject.GetComponent<LootableObject>());
            }

            if (closest.gameObject.GetComponent<SupplyCrate>())
            {
                return new Entity(closest.gameObject.GetComponent<SupplyCrate>());
            }

            if (closest.gameObject.GetComponent<ResourceTarget>())
            {
                return new Entity(closest.gameObject.GetComponent<ResourceTarget>());
            }

            return null;
        }

        /// <summary>
        /// Finds the objects within a range that are convertable to the Entity class.
        /// </summary>
        /// <param name="givenPosition"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        public List<Entity> FindEntitysAroundFast(Vector3 givenPosition, float dist = 1f)
        {
            Collider[] array = Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(givenPosition, dist);
            List<Entity> list = new List<Entity>(array.Length);
            foreach (Collider x in array)
            {
                if (x.gameObject.GetComponent<StructureMaster>())
                {
                    list.Add(new Entity(x.gameObject.GetComponent<StructureMaster>()));
                }
                else if (x.gameObject.GetComponent<StructureComponent>())
                {
                    list.Add(new Entity(x.gameObject.GetComponent<StructureComponent>()));
                }
                else if (x.gameObject.GetComponent<DeployableObject>())
                {
                    list.Add(new Entity(x.gameObject.GetComponent<DeployableObject>()));
                }
                else if (x.gameObject.GetComponent<LootableObject>())
                {
                    list.Add(new Entity(x.gameObject.GetComponent<LootableObject>()));
                }
                else if (x.gameObject.GetComponent<SupplyCrate>())
                {
                    list.Add(new Entity(x.gameObject.GetComponent<SupplyCrate>()));
                }
                else if (x.gameObject.GetComponent<ResourceTarget>())
                {
                    list.Add(new Entity(x.gameObject.GetComponent<ResourceTarget>()));
                }
            }

            return list;
        }

        /// <summary>
        /// Finds the closest object to the given position.
        /// </summary>
        /// <param name="givenPosition"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        public GameObject FindClosestObject(Vector3 givenPosition, float dist = 1f)
        {
            Collider[] array = Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(givenPosition, dist);
            if (array.Length == 0)
            {
                return null;
            }

            Collider closest = array[0];
            if (array.Length > 1)
            {
                for (int i = 1; i < array.Length; i++)
                {
                    if (Vector3.Distance(array[i].transform.position, givenPosition) <
                        Vector3.Distance(closest.transform.position, givenPosition))
                    {
                        closest = array[i];
                    }
                }
            }

            return closest.gameObject; // Specific Entities can be converted to Entity, see the Entity class's constructor. (Example: It doesn't handle BasicDoor.)
        }

        /// <summary>
        /// Find the closest objects that are within the specified range.
        /// </summary>
        /// <param name="givenPosition"></param>
        /// <param name="dist"></param>
        /// <returns></returns>
        public List<GameObject> FindObjectsAroundFast(Vector3 givenPosition, float dist = 1f)
        {
            Collider[] array = Facepunch.MeshBatch.MeshBatchPhysics.OverlapSphere(givenPosition, dist);
            List<GameObject> list = new List<GameObject>(array.Length);
            list.AddRange(array.Select(x => x.gameObject));
            return list;
        }


        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public List<Entity> FindDeployablesAround(Vector3 givenPosition, float dist = 100f, bool forceupdate = false)
        {
            List<Entity> l = new List<Entity>();
            foreach (var x in World.GetWorld().DeployableObjects(forceupdate))
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) l.Add(x);
            }

            return l;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public List<Entity> FindDoorsAround(Vector3 givenPosition, float dist = 100f, bool forceupdate = false)
        {
            List<Entity> l = new List<Entity>();
            foreach (var x in World.GetWorld().BasicDoors(forceupdate))
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) l.Add(x);
            }

            return l;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public List<Entity> FindStructuresAround(Vector3 givenPosition, float dist = 100f, bool forceupdate = false)
        {
            List<Entity> l = new List<Entity>();
            foreach (var x in World.GetWorld().StructureComponents(forceupdate))
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) l.Add(x);
            }

            return l;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public List<Entity> FindLootablesAround(Vector3 givenPosition, float dist = 100f)
        {
            List<Entity> l = new List<Entity>();
            foreach (var x in World.GetWorld().LootableObjects)
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) l.Add(x);
            }

            return l;
        }

        [Obsolete("Use FindClosestEntity, and distinguish between the object types.", false)]
        public List<Entity> FindEntitiesAround(Vector3 givenPosition, float dist = 100f)
        {
            List<Entity> l = new List<Entity>();
            foreach (var x in World.GetWorld().Entities)
            {
                if (Vector3.Distance(x.Location, givenPosition) <= dist) l.Add(x);
            }

            return l;
        }

        [Obsolete("Use FindEntity", false)]
        public Entity GetEntityatCoords(Vector3 givenPosition)
        {
            return FindEntityAt(givenPosition);
        }

        [Obsolete("Use FindEntity", false)]
        public Entity GetEntityatCoords(float x, float y, float z)
        {
            return FindEntityAt(new Vector3(x, y, z));
        }

        [Obsolete("Use FindDoorAt", false)]
        public Entity GetDooratCoords(Vector3 givenPosition)
        {
            return FindDoorAt(givenPosition);
        }

        [Obsolete("Use FindDoorAt", false)]
        public Entity GetDooratCoords(float x, float y, float z)
        {
            return FindDoorAt(new Vector3(x, y, z));
        }

        /// <summary>
        /// Returns the integer hash of the byte array input using the
        /// 'superfasthash' algorithm.
        /// Check out: http://landman-code.blogspot.com/2009/02/c-superfasthash-and-murmurhash2.html
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public UInt32 SuperFastHash(Byte[] input)
        {
            return SuperFastHashUInt16Hack.Hash(input);
        }

        /// <summary>
        /// Returns the integer hash of the byte array input using the
        /// 'superfasthash' algorithm.
        /// Check out: http://landman-code.blogspot.com/2009/02/c-superfasthash-and-murmurhash2.html
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public UInt32 SuperFastHash(string input)
        {
            return SuperFastHash(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Returns the sha1 hash of the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string SHA1Hash(Byte[] input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                byte[] hash = sha1.ComputeHash(input);
                StringBuilder sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the sha1 hash of the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string SHA1Hash(string input)
        {
            return SHA1Hash(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Returns the sha256 hash of the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string SHA256Hash(Byte[] input)
        {
            using (SHA256Managed sha256 = new SHA256Managed())
            {
                byte[] hash = sha256.ComputeHash(input);
                StringBuilder sb = new StringBuilder();

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the sha256 hash of the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string SHA256Hash(string input)
        {
            return SHA256Hash(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Returns the md5 hash of the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string MD5Hash(Byte[] input)
        {
            using (MD5 md5 = MD5.Create())
            {
                StringBuilder sb = new StringBuilder();
                byte[] hash = md5.ComputeHash(input);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the md5 hash of the given input.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string MD5Hash(string input)
        {
            return MD5Hash(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Gets the specified variable's value from the instance using reflection.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            try
            {
                FieldInfo field = type.GetField(fieldName, bindFlags);
                if (field != null)
                {
                    object v = field.GetValue(instance);
                    return v;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Reflection] Failed to get value of {fieldName}! {ex}");
            }
            
            return null;
        }

        /// <summary>
        /// Sets the specified variable's value in the instance using reflection.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <param name="val"></param>
        public void SetInstanceField(Type type, object instance, string fieldName, object val)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            try
            {
                FieldInfo field = type.GetField(fieldName, bindFlags);
                if (field != null) 
                    field.SetValue(instance, val);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Reflection] Failed to set value of {fieldName}! {ex}");
            }
        }

        /// <summary>
        /// Determines if any invalid XML 1.0 characters exist within the string,
        /// and if so it returns a new string with the invalid chars removed, else 
        /// the same string is returned (with no wasted StringBuilder allocated, etc).
        /// </summary>
        /// <param name="s">Xml string.</param>
        /// <param name="startIndex">The index to begin checking at.</param>
        public string ToValidXmlCharactersString(string s, int startIndex = 0)
        {
            int firstInvalidChar = IndexOfFirstInvalidXMLChar(s, startIndex);
            if (firstInvalidChar < 0)
            {
                return s;
            }

            startIndex = firstInvalidChar;

            int len = s.Length;
            StringBuilder sb = new StringBuilder(len);

            if (startIndex > 0)
            {
                sb.Append(s, 0, startIndex);
            }

            for (int i = startIndex; i < len; i++)
            {
                if (IsLegalXmlChar(s[i]))
                {
                    sb.Append(s[i]);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the index of the first invalid XML 1.0 character in this string, else returns -1.
        /// </summary>
        /// <param name="s">Xml string.</param>
        /// <param name="startIndex">Start index.</param>
        public int IndexOfFirstInvalidXMLChar(string s, int startIndex = 0)
        {
            if (!string.IsNullOrEmpty(s) && startIndex < s.Length)
            {
                if (startIndex < 0)
                {
                    startIndex = 0;
                }

                int len = s.Length;

                for (int i = startIndex; i < len; i++)
                {
                    if (!IsLegalXmlChar(s[i]))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Indicates whether a given character is valid according to the XML 1.0 spec.
        /// </summary>
        public bool IsLegalXmlChar(char c)
        {
            if (c > 31 && c <= 55295)
            {
                return true;
            }

            if (c < 32)
            {
                return c == 9 || c == 10 || c == 13;
            }

            return (c >= 57344 && c <= 65533) || c > 65535;
            // final comparison is useful only for integral comparison, if char c -> int c, useful for utf-32 I suppose
            //c <= 1114111 */ // impossible to get a code point bigger than 1114111 because Char.ConvertToUtf32 would have thrown an exception
        }

        public ulong TimeInMillis
        {
            get { return NetCull.timeInMillis; }
        }

        public double TimeEpoch
        {
            get
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                return t.TotalSeconds;
            }
        }

        public int MainThreadID
        {
            get { return Bootstrap.CurrentThread.ManagedThreadId; }
        }

        public Thread MainThread
        {
            get { return Bootstrap.CurrentThread; }
        }

        public Thread CurrentWorkingThread
        {
            get { return Thread.CurrentThread; }
        }

        public int CurrentWorkingThreadID
        {
            get { return Thread.CurrentThread.ManagedThreadId; }
        }
    }
}