using System;
using System.Collections;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

namespace Fougerite
{
    /// <summary>
    /// Local DataStore that is capable to store most of the objects, such as vectors, ulong, string, int, float, etc.
    /// </summary>
    public class DataStore
    {
        public Hashtable datastore = new Hashtable();
        private static DataStore _instance;
        public static string PATH = Path.Combine(Config.GetPublicFolder(), "FougeriteDatastore.ds");

        /// <summary>
        /// Returns the DataStore's instance.
        /// </summary>
        /// <returns></returns>
        public static DataStore GetInstance()
        {
            if (_instance == null)
            {
                _instance = new DataStore();
            }
            return _instance;
        }

        private object StringifyIfVector3(object keyorval)
        {
            if (keyorval == null)
                return null;
            
            try
            {
                if (keyorval is Vector3 vector3)
                {
                    return $"Vector3,{vector3.x:G9},{vector3.y:G9},{vector3.z:G9}";
                }
            }
            catch
            {
                //Logger.LogException(ex);
            }

            return null;
        }

        private object ParseIfVector3String(object keyorval)
        {
            if (keyorval == null)
                return null;

            try
            {
                if (keyorval is string s)
                {
                    if (s.StartsWith("Vector3,", StringComparison.Ordinal))
                    {
                        string[] v3array = s.Split(',');
                        Vector3 parse = new Vector3(Single.Parse(v3array[1]),
                                            Single.Parse(v3array[2]),
                                            Single.Parse(v3array[3]));
                        return parse;
                    }
                }
            }
            catch
            {
                //Logger.LogException(ex);
            }
            
            return keyorval;
        }

        /// <summary>
        /// Converts the DS table to an ini file.
        /// </summary>
        /// <param name="tablename">The name of the table to convert.</param>
        /// <param name="ini">The IniParser instance to use.</param>
        public void ToIni(string tablename, IniParser ini)
        {
            string nullref = "__NullReference__";
            Hashtable ht = (Hashtable)datastore[tablename];
            if (ht == null || ini == null)
                return;

            foreach (object key in ht.Keys)
            {
                string setting = key.ToString();
                string val = nullref;
                if (ht[setting] != null)
                {
                    float tryfloat;
                    if (float.TryParse((string) ht[setting], out tryfloat))
                    {
                        try
                        {
                            val = ((float) ht[setting]).ToString("G9");
                        }
                        catch
                        {
                            // Ignore
                        }
                    } 
                    var t = ht[setting].GetType();
                    if (t == typeof(Vector4) || t == typeof(Vector3) || t == typeof(Vector2) || t == typeof(Quaternion) || t == typeof(Bounds))
                    {
                        try
                        {
                            val = ((Vector3) ht[setting]).ToString("F5");
                        }
                        catch
                        {
                            // Ignore
                        }
                    } else
                    {
                        val = ht[setting].ToString();
                    }
                }
                ini.AddSetting(tablename, setting, val);
            }
            ini.Save();
        }

        /// <summary>
        /// Converts an Ini file to a DS Table.
        /// </summary>
        /// <param name="ini">The instance of the ini file.</param>
        public void FromIni(IniParser ini)
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
                        Add(section, key, valuef);
                    }
                    else if (int.TryParse(setting, out valuei))
                    {
                        Add(section, key, valuei);
                    }
                    else if (ini.GetBoolSetting(section, key))
                    {
                        Add(section, key, true);
                    }
                    else if (setting.Equals("False", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Add(section, key, false);
                    }
                    else if (setting == "__NullReference__")
                    {
                        Add(section, key, null);
                    }
                    else
                    {
                        Add(section, key, ini.GetSetting(section, key));
                    }
                }
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

            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
            {
                hashtable = new Hashtable();
                datastore.Add(tablename, hashtable);
            }
            //hashtable[key] = val;
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

            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
                return false;

            //return hashtable.ContainsKey(key);
            return hashtable.ContainsKey(StringifyIfVector3(key));
        }

        /// <summary>
        /// Checks if the specified value is found in the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <param name="val"></param>
        /// <returns>Returns true If It does, false otherwise.</returns>
        public bool ContainsValue(string tablename, object val)
        {
            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
                return false;

            //return hashtable.ContainsValue(val);
            return hashtable.ContainsValue(StringifyIfVector3(val));
        }

        /// <summary>
        /// Counts the elements in the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns>Returns the number of the elements.</returns>
        public int Count(string tablename)
        {
            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
            {
                return 0;
            }
            return hashtable.Count;
        }

        /// <summary>
        /// It deletes every key and value from the table.
        /// </summary>
        /// <param name="tablename"></param>
        public void Flush(string tablename)
        {
            if ((datastore[tablename] as Hashtable) != null)
            {
                datastore.Remove(tablename);
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

            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            //return hashtable[key];
            return ParseIfVector3String(hashtable[StringifyIfVector3(key)]);
        }

        /// <summary>
        /// Gets all the keys/values from the table.
        /// </summary>
        /// <param name="tablename">Returns a HashTable.</param>
        /// <returns></returns>
        public Hashtable GetTable(string tablename)
        {
            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            Hashtable parse = new Hashtable(hashtable.Count);
            foreach (DictionaryEntry entry in hashtable)
            {
                parse.Add(ParseIfVector3String(entry.Key), ParseIfVector3String(entry.Value));
            }
            //return hashtable;
            return parse;
        }

        /// <summary>
        /// Returns all the keys of the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns>Returns an object array.</returns>
        public object[] Keys(string tablename)
        {
            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            /*object[] array = new object[hashtable.Keys.Count];
            hashtable.Keys.CopyTo(array, 0);
            return array;*/
            List<object> parse = new List<object>(hashtable.Keys.Count);
            foreach (object key in hashtable.Keys)
            {
                parse.Add(ParseIfVector3String(key));
            }
            return parse.ToArray<object>();
        }

        public void Load()
        {
            if (File.Exists(PATH))
            {
                datastore = Util.HashtableFromFile(PATH);
                Util.GetUtil().ConsoleLog("Fougerite DataStore Loaded", true);
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

            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable != null)
            {
                //hashtable.Remove(key);
                hashtable.Remove(StringifyIfVector3(key));
            }
        }

        /// <summary>
        /// Saves the datastore.
        /// </summary>
        public void Save()
        {
            if (datastore.Count != 0)
            {
                Logger.Log("[DataStore] Saving...");
                Util.HashtableToFile(datastore, PATH);
                Util.GetUtil().ConsoleLog("Fougerite DataStore Saved", false);
                Logger.Log("[DataStore] Saved!");
            }
        }

        /// <summary>
        /// Gets all values of the table.
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns>Returns an object array of the table's values.</returns>
        public object[] Values(string tablename)
        {
            Hashtable hashtable = datastore[tablename] as Hashtable;
            if (hashtable == null)
                return null;

            /*object[] array = new object[hashtable.Values.Count];
            hashtable.Values.CopyTo(array, 0);
            return array;*/
            List<object> parse = new List<object>(hashtable.Values.Count);
            foreach (object val in hashtable.Values)
            {
                parse.Add(ParseIfVector3String(val));
            }
            return parse.ToArray();
        }
    }
}