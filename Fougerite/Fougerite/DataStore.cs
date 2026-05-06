using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Fougerite.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using ReaderWriterLock = Fougerite.Concurrent.ReaderWriterLock;

namespace Fougerite
{
    /// <summary>
    /// Local DataStore that is capable to store most of the objects, such as vectors, ulong, string, int, float, etc.
    /// </summary>
    public class DataStore
    {
        private Hashtable _datastore = new Hashtable();
        private static readonly Lazy<DataStore> Instance = new Lazy<DataStore>(() => new DataStore());
        private readonly ReaderWriterLock _rwLock = new ReaderWriterLock();
        public static string PATH = Path.Combine(Config.GetPublicFolder(), "FougeriteDatastore.ds");
        private readonly JsonSerializerSettings _jsonSettings;

        private DataStore()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                // https://stackoverflow.com/questions/24025350/xamarin-android-json-net-serilization-fails-on-4-2-2-device-only-timezonenotfoun
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Include,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            _jsonSettings.Converters.Add(new UnityTypeConverter());
        }

        /// <summary>
        /// Returns the DataStore's instance.
        /// </summary>
        /// <returns></returns>
        public static DataStore GetInstance()
        {
            return Instance.Value;
        }

        /// <summary>
        /// Returns the hashtable instance.
        /// </summary>
        [Obsolete("Old API. Use the provided methods instead for thread safety.", false)]
        public Hashtable Hashtable
        {
            get
            {
                // Note: Accessing this directly bypasses thread safety provided by the Wrapper methods.
                return _datastore;
            }
        }

        /// <summary>
        /// Converts the DS table to an ini file.
        /// </summary>
        /// <param name="tablename">The name of the table to convert.</param>
        /// <param name="ini">The IniParser instance to use.</param>
        public void ToIni(string tablename, IniParser ini)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                const string nullref = "__NullReference__";
                Hashtable ht = (Hashtable)_datastore[tablename];
                if (ht == null || ini == null)
                    return;

                foreach (object key in ht.Keys)
                {
                    string setting = key.ToString();
                    string val = nullref;
                    if (ht[setting] != null)
                    {
                        float tryfloat;
                        if (ht[setting] is string s && float.TryParse(s, out tryfloat))
                        {
                            try
                            {
                                val = tryfloat.ToString("G9");
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                        else if (ht[setting] is float f)
                        {
                            val = f.ToString("G9");
                        }

                        Type t = ht[setting].GetType();
                        if (t == typeof(Vector4) || t == typeof(Vector3) || t == typeof(Vector2) ||
                            t == typeof(Quaternion) || t == typeof(Bounds))
                        {
                            try
                            {
                                val = ((Vector3)ht[setting]).ToString("F5");
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                        else
                        {
                            val = ht[setting].ToString();
                        }
                    }

                    ini.AddSetting(tablename, setting, val);
                }

                ini.Save();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] ToIni error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Converts an Ini file to a DS Table.
        /// </summary>
        /// <param name="ini">The instance of the ini file.</param>
        public void FromIni(IniParser ini)
        {
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                foreach (string section in ini.Sections)
                {
                    foreach (string key in ini.EnumSection(section))
                    {
                        string setting = ini.GetSetting(section, key);
                        float valuef;
                        int valuei;
                        if (float.TryParse(setting, out valuef))
                        {
                            AddInternal(section, key, valuef);
                        }
                        else if (int.TryParse(setting, out valuei))
                        {
                            AddInternal(section, key, valuei);
                        }
                        else if (ini.GetBoolSetting(section, key))
                        {
                            AddInternal(section, key, true);
                        }
                        else if (setting.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                        {
                            AddInternal(section, key, false);
                        }
                        else if (setting == "__NullReference__")
                        {
                            AddInternal(section, key, null);
                        }
                        else
                        {
                            AddInternal(section, key, setting);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] FromIni error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Adds a key / value to the table.
        /// </summary>
        /// <param name="tablename">Name of the table</param>
        /// <param name="key">Key object</param>
        /// <param name="val">Value object</param>
        public void Add(string tablename, object key, object val)
        {
            if (key == null)
                return;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                AddInternal(tablename, key, val);
            }
            catch (Exception)
            {
                // Ignore.
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Shared internal logic to avoid lock recursion issues
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        private void AddInternal(string tablename, object key, object val)
        {
            if (!(_datastore[tablename] is Hashtable hashtable))
            {
                hashtable = new Hashtable();
                _datastore.Add(tablename, hashtable);
            }

            hashtable[StringifyIfVector3(key)] = StringifyIfVector3(val);
        }

        /// <summary>
        /// Checks if the specified key is found in the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="key"></param>
        /// <returns>Returns true If It does, false otherwise.</returns>
        public bool ContainsKey(string tablename, object key)
        {
            if (key == null)
                return false;

            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!(_datastore[tablename] is Hashtable hashtable))
                    return false;

                return hashtable.ContainsKey(StringifyIfVector3(key));
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] ContainsKey error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified value is found in the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="val"></param>
        /// <returns>Returns true If It does, false otherwise.</returns>
        public bool ContainsValue(string tablename, object val)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!(_datastore[tablename] is Hashtable hashtable))
                    return false;

                return hashtable.ContainsValue(StringifyIfVector3(val));
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] ContainsValue error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return false;
        }

        /// <summary>
        /// Counts the elements in the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns>Returns the number of the elements.</returns>
        public int Count(string tablename)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!(_datastore[tablename] is Hashtable hashtable))
                {
                    return 0;
                }

                return hashtable.Count;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Count error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return 0;
        }

        /// <summary>
        /// It deletes every key and value from the table.
        /// </summary>
        /// <param name="tablename"></param>
        public void Flush(string tablename)
        {
            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_datastore[tablename] is Hashtable)
                {
                    _datastore.Remove(tablename);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Flush error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// It gets the value of the specified key in the specified table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="key"></param>
        /// <returns>Returns the value or null If It doesn't exist.</returns>
        public object Get(string tablename, object key)
        {
            if (key == null)
                return null;

            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!(_datastore[tablename] is Hashtable hashtable))
                    return null;

                return ParseIfVector3String(hashtable[StringifyIfVector3(key)]);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Get error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return null;
        }

        /// <summary>
        /// Gets all the keys/values from the table.
        /// </summary>
        /// <param name="tablename">Returns a HashTable.</param>
        /// <returns></returns>
        public Hashtable GetTable(string tablename)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!(_datastore[tablename] is Hashtable hashtable))
                    return null;

                Hashtable parse = new Hashtable(hashtable.Count);
                foreach (DictionaryEntry entry in hashtable)
                {
                    parse.Add(ParseIfVector3String(entry.Key), ParseIfVector3String(entry.Value));
                }

                return parse;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] GetTable error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            return null;
        }

        /// <summary>
        /// Returns all the keys of the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns>Returns an object array.</returns>
        public object[] Keys(string tablename)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!(_datastore[tablename] is Hashtable hashtable))
                    return null;

                List<object> parse = new List<object>(hashtable.Keys.Count);
                parse.AddRange(hashtable.Keys.Cast<object>().Select(ParseIfVector3String));
                return parse.ToArray<object>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Keys error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            object[] keys = new object[0];
            return keys;
        }

        public void Load()
        {
            if (!File.Exists(PATH)) return;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                string content = File.ReadAllText(PATH);
                bool isJson = false;
                try
                {
                    JObject.Parse(content);
                    isJson = true;
                }
                catch (Exception)
                {
                    // Ignore.
                }

                if (isJson)
                {
                    _datastore = JsonConvert.DeserializeObject<Hashtable>(content, _jsonSettings) ?? new Hashtable();
                    Logger.Log("Fougerite DataStore Loaded (JSON)");
                }
                else
                {
                    _datastore = Util.HashtableFromFile(PATH) ?? new Hashtable();
                    Logger.Log("Fougerite DataStore Loaded (Legacy Binary Format)");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Load error: {ex}");
                // Fallback to legacy binary just in case
                _datastore = Util.HashtableFromFile(PATH) ?? new Hashtable();
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Deletes the key/value from the table by specifying the key / table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="key"></param>
        public void Remove(string tablename, object key)
        {
            if (key == null)
                return;

            _rwLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_datastore[tablename] is Hashtable hashtable)
                {
                    hashtable.Remove(StringifyIfVector3(key));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Remove error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Saves the datastore.
        /// </summary>
        public void Save()
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (_datastore.Count != 0)
                {
                    Logger.Log("[DataStore] Saving to JSON...");
                    string json = JsonConvert.SerializeObject(_datastore, _jsonSettings);
                    File.WriteAllText(PATH, json);
                    Logger.Log("[DataStore] Saved!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Save error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Gets all values of the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns>Returns an object array of the table's values.</returns>
        public object[] Values(string tablename)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (!(_datastore[tablename] is Hashtable hashtable))
                    return null;

                List<object> parse = new List<object>(hashtable.Values.Count);
                parse.AddRange(hashtable.Values.Cast<object>().Select(ParseIfVector3String));
                return parse.ToArray();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Values error: {ex}");
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }

            object[] values = new object[0];
            return values;
        }

        /// <summary>
        /// Returns an array of all table names currently stored in the DataStore.
        /// </summary>
        /// <returns>A string array containing all table names.</returns>
        public string[] GetTableNames()
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                // Extract the keys from the main datastore hashtable.
                // These keys represent the "Tablenames" created by plugins.
                return _datastore.Keys.Cast<string>().ToArray();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[DataStore] Failed to get table names: {ex}");
                return new string[0];
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Registers a custom JsonConverter to the serialization engine.
        /// Use this to support custom classes or structures that Newtonsoft cannot handle natively.
        /// </summary>
        /// <param name="jsonConverter">The custom converter instance to add.</param>
        public void AddJsonConverter(JsonConverter jsonConverter)
        {
            _jsonSettings.Converters.Add(jsonConverter);
        }

        /// <summary>
        /// Removes a specific JsonConverter from the serialization engine.
        /// </summary>
        /// <param name="jsonConverter">The converter instance to remove.</param>
        public void RemoveJsonConverter(JsonConverter jsonConverter)
        {
            if (_jsonSettings.Converters.Contains(jsonConverter))
            {
                _jsonSettings.Converters.Remove(jsonConverter);
            }
        }

        /// <summary>
        /// Removes all registered JsonConverters, including default Unity type support.
        /// Use with caution as this will break Vector, Quaternion, and Color serialization.
        /// </summary>
        public void ClearJsonConverters()
        {
            _jsonSettings.Converters.Clear();
        }

        /// <summary>
        /// Returns a copy of all currently active JsonConverters.
        /// Useful for debugging or checking if a specific converter is already registered.
        /// </summary>
        /// <returns>A list containing the active JsonConverters.</returns>
        public List<JsonConverter> GetJsonConverters()
        {
            return _jsonSettings.Converters.ToList();
        }

        /// <summary>
        /// Provides direct access to the underlying Newtonsoft JsonSerializerSettings.
        /// Advanced users can use this to modify Formatting, Null Handling, or TypeNameHandling.
        /// </summary>
        public JsonSerializerSettings JsonSerializerSettings
        {
            get { return _jsonSettings; }
        }

        /// <summary>
        /// Converts Unity math types (Vector2, Vector3, Quaternion, Color, Rect) into a high-precision string format.
        /// Uses 'G9' formatting to ensure "huge ass floats" are preserved without precision loss.
        /// Stringification is used for Keys to ensure reliable Hashtable indexing and HashCode stability.
        /// </summary>
        /// <param name="keyorval">The object to check and potentially convert to string.</param>
        /// <returns>A string representation of the Unity type, or the original object if no conversion is needed.</returns>
        private object StringifyIfVector3(object keyorval)
        {
            switch (keyorval)
            {
                case null: return null;
                case Vector3 v3: return $"V3({v3.x:G9}|{v3.y:G9}|{v3.z:G9})";
                case Vector2 v2: return $"V2({v2.x:G9}|{v2.y:G9})";
                case Quaternion q: return $"Q({q.x:G9}|{q.y:G9}|{q.z:G9}|{q.w:G9})";
                case Color c: return $"C({c.r:G9}|{c.g:G9}|{c.b:G9}|{c.a:G9})";
                case Rect r: return $"R({r.x:G9}|{r.y:G9}|{r.width:G9}|{r.height:G9})";
                default: return keyorval;
            }
        }

        /// <summary>
        /// Parses string representations of Unity types back into their original object forms.
        /// Supports both the new bracket/pipe format (like, V3(x|y|z)) and the legacy 2013... comma format (like Vector3,x,y,z).
        /// Uses simple stripping and splitting logic to remain resilient against variable float lengths and trailing spaces.
        /// </summary>
        /// <param name="keyorval">The object (potentially a string) to parse.</param>
        /// <returns>The reconstructed Unity object if parsing succeeds, otherwise the original input.</returns>
        private object ParseIfVector3String(object keyorval)
        {
            if (keyorval is string s)
            {
                string t = s.Trim();
                try
                {
                    if (t.StartsWith("V3(", StringComparison.Ordinal))
                    {
                        string[] p = t.Replace("V3(", "").Replace(")", "").Split('|');
                        return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
                    }

                    if (t.StartsWith("V2(", StringComparison.Ordinal))
                    {
                        string[] p = t.Replace("V2(", "").Replace(")", "").Split('|');
                        return new Vector2(float.Parse(p[0]), float.Parse(p[1]));
                    }

                    if (t.StartsWith("Q(", StringComparison.Ordinal))
                    {
                        string[] p = t.Replace("Q(", "").Replace(")", "").Split('|');
                        return new Quaternion(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]),
                            float.Parse(p[3]));
                    }

                    if (t.StartsWith("C(", StringComparison.Ordinal))
                    {
                        string[] p = t.Replace("C(", "").Replace(")", "").Split('|');
                        return new Color(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
                    }

                    if (t.StartsWith("R(", StringComparison.Ordinal))
                    {
                        string[] p = t.Replace("R(", "").Replace(")", "").Split('|');
                        return new Rect(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
                    }

                    // Old format.
                    if (t.StartsWith("Vector3,", StringComparison.Ordinal))
                    {
                        string[] p = t.Split(',');
                        return new Vector3(float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
                    }
                }
                catch (Exception)
                {
                    // Ignore.
                }
            }

            return keyorval;
        }
    }

    /// <summary>
    /// Prevents Newtonsoft from deep-serializing internal Unity properties that cause circular loops.
    /// </summary>
    public class UnityTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(Vector3) || t == typeof(Vector2) || t == typeof(Quaternion) ||
                   t == typeof(Color) || t == typeof(Rect);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            switch (value)
            {
                case Vector3 v3:
                {
                    writer.WritePropertyName("x");
                    writer.WriteValue(v3.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(v3.y);
                    writer.WritePropertyName("z");
                    writer.WriteValue(v3.z);
                    break;
                }
                case Vector2 v2:
                {
                    writer.WritePropertyName("x");
                    writer.WriteValue(v2.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(v2.y);
                    break;
                }
                case Quaternion q:
                {
                    writer.WritePropertyName("x");
                    writer.WriteValue(q.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(q.y);
                    writer.WritePropertyName("z");
                    writer.WriteValue(q.z);
                    writer.WritePropertyName("w");
                    writer.WriteValue(q.w);
                    break;
                }
                case Color c:
                {
                    writer.WritePropertyName("r");
                    writer.WriteValue(c.r);
                    writer.WritePropertyName("g");
                    writer.WriteValue(c.g);
                    writer.WritePropertyName("b");
                    writer.WriteValue(c.b);
                    writer.WritePropertyName("a");
                    writer.WriteValue(c.a);
                    break;
                }
                case Rect r:
                {
                    writer.WritePropertyName("x");
                    writer.WriteValue(r.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(r.y);
                    writer.WritePropertyName("w");
                    writer.WriteValue(r.width);
                    writer.WritePropertyName("h");
                    writer.WriteValue(r.height);
                    break;
                }
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            if (objectType == typeof(Vector3)) return new Vector3((float)obj["x"], (float)obj["y"], (float)obj["z"]);
            if (objectType == typeof(Vector2)) return new Vector2((float)obj["x"], (float)obj["y"]);
            if (objectType == typeof(Quaternion))
                return new Quaternion((float)obj["x"], (float)obj["y"], (float)obj["z"], (float)obj["w"]);
            if (objectType == typeof(Color))
                return new Color((float)obj["r"], (float)obj["g"], (float)obj["b"], (float)obj["a"]);
            if (objectType == typeof(Rect))
                return new Rect((float)obj["x"], (float)obj["y"], (float)obj["w"], (float)obj["h"]);
            return null;
        }
    }
}