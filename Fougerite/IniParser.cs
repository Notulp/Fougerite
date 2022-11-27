using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Fougerite;

/// <summary>
/// Standard IniParser.
/// Supports thread safe reading / writing.
/// </summary>
public class IniParser
{
    private readonly string _iniFilePath;
    private readonly Hashtable _keyPairs = new Hashtable();
    public string Name;
    private readonly List<SectionPair> _tmpList = new List<SectionPair>();
    private readonly Fougerite.Concurrent.ReaderWriterLock _readerWriterLock = new Fougerite.Concurrent.ReaderWriterLock();
    private Thread _t;
    
    [StructLayout(LayoutKind.Sequential)]
    public struct SectionPair
    {
        public string Section;
        public string Key;
    }

    public IniParser(string iniPath)
    {
        string str2 = null;
        _iniFilePath = iniPath;
        Name = Path.GetFileNameWithoutExtension(iniPath);

        if (!File.Exists(iniPath)) 
            throw new FileNotFoundException($"Unable to locate {iniPath}");

        try
        {
            using (TextReader reader = new StreamReader(iniPath))
            {
                for (string str = reader.ReadLine(); str != null; str = reader.ReadLine())
                {
                    str = str.Trim();
                    if (str == "") continue;

                    if (str.StartsWith("[") && str.EndsWith("]"))
                        str2 = str.Substring(1, str.Length - 2);
                    else
                    {
                        SectionPair pair;

                        if (str.StartsWith(";"))
                            str = $@"{str.Replace("=", "%eq%")}=%comment%";

                        string[] strArray = str.Split(new char[] {'='}, 2);
                        string str3 = null;
                        if (str2 == null)
                        {
                            str2 = "ROOT";
                        }
                        pair.Section = str2;
                        pair.Key = strArray[0];
                        if (strArray.Length > 1)
                        {
                            str3 = strArray[1];
                        }
                        try
                        {
                            _keyPairs.Add(pair, str3);
                            _tmpList.Add(pair);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed adding{pair}|{str3} at {_iniFilePath} Exception: {ex}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error at {_iniFilePath} Exception: {ex}");
        }
    }

    /// <summary>
    /// The path of the file.
    /// </summary>
    public string IniPath
    {
        get { return _iniFilePath; }
    }

    /// <summary>
    /// Adds a Section, and a Key to the file.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    public void AddSetting(string sectionName, string settingName)
    {
        AddSetting(sectionName, settingName, string.Empty);
    }

    /// <summary>
    /// Adds a Section, Key, and Value.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    /// <param name="settingValue"></param>
    public void AddSetting(string sectionName, string settingName, string settingValue)
    {
        try
        {
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            
            SectionPair pair;
            pair.Section = sectionName;
            pair.Key = settingName;
            if (settingValue == null)
                settingValue = string.Empty;

            if (_keyPairs.ContainsKey(pair))
            {
                _keyPairs.Remove(pair);
            }

            if (_tmpList.Contains(pair))
            {
                _tmpList.Remove(pair);
            }

            _keyPairs.Add(pair, settingValue);
            _tmpList.Add(pair);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] AddSettings Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseWriterLock();
        }
    }

    /// <summary>
    /// Counts all Sections.
    /// </summary>
    /// <returns></returns>
    public int Count()
    {
        int count = 0;
        try
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            count = Sections.Length;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] Count Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseReaderLock();
        }

        return count;
    }

    /// <summary>
    /// Deletes a value + key by key.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    public void DeleteSetting(string sectionName, string settingName)
    {
        try
        {
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            SectionPair pair;
            pair.Section = sectionName;
            pair.Key = settingName;
            if (_keyPairs.ContainsKey(pair))
            {
                _keyPairs.Remove(pair);
                _tmpList.Remove(pair);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] DeleteSetting Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseWriterLock();
        }
    }

    /// <summary>
    /// Enumerates a Section without the commented lines.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <returns></returns>
    public string[] EnumSection(string sectionName)
    {
        List<string> list = new List<string>();
        try
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            foreach (SectionPair pair in _tmpList)
            {
                if (pair.Key.StartsWith(";"))
                    continue;

                if (pair.Section == sectionName)
                {
                    list.Add(pair.Key);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] EnumSection Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseReaderLock();
        }

        return list.ToArray();
    }

    /// <summary>
    /// Gets all Sections.
    /// </summary>
    public string[] Sections
    {
        get
        {
            List<string> list = new List<string>();
            try
            {
                _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
                foreach (SectionPair pair in _tmpList)
                {
                    if (!list.Contains(pair.Section))
                    {
                        list.Add(pair.Section);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[IniParser] Sections Error: {ex}");
            }
            finally
            {
                _readerWriterLock.ReleaseReaderLock();
            }
            
            return list.ToArray();
        }
    }
    
    /// <summary>
    /// Gets a value by Section + Key
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    /// <returns></returns>
    public string GetSetting(string sectionName, string settingName)
    {
        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;

        string ret = null;
        try
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            ret = (string) _keyPairs[pair];
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] GetSetting Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseReaderLock();
        }
        
        return ret;
    }

    /// <summary>
    /// Gets a Value by Section + Key, and tries to convert it to a boolean.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    /// <returns></returns>
    public bool GetBoolSetting(string sectionName, string settingName)
    {
        bool.TryParse(GetSetting(sectionName, settingName), out bool val);
        return val;
    }

    /// <summary>
    /// Gets a Value by Section (where the section is Commands) + Key, and tries to convert it to a boolean.
    /// </summary>
    /// <param name="cmdName"></param>
    /// <returns></returns>
    public bool isCommandOn(string cmdName)
    {
        return GetBoolSetting("Commands", cmdName);
    }

    /// <summary>
    /// Saves the current state of the ini from the memory to a file.
    /// </summary>
    public void Save()
    {
        FileInfo fi = new FileInfo(_iniFilePath);
        float mega = (fi.Length / 1024f) / 1024f;
        if (mega <= 0.6)
        {
            SaveSettings(_iniFilePath);
            return;
        }
        _t = new Thread(() => SaveSettings(_iniFilePath));
        _t.Start();
    }

    /// <summary>
    /// Saves the current state of the ini from the memory to a file.
    /// </summary>
    /// <param name="newFilePath"></param>
    public void SaveSettings(string newFilePath)
    {
        try
        {
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            
            ArrayList list = new ArrayList();
            string str = string.Empty;
            string str2 = string.Empty;
            foreach (SectionPair pair in _tmpList)
            {
                if (!list.Contains(pair.Section))
                {
                    list.Add(pair.Section);
                }
            }

            foreach (string str3 in list)
            {
                str2 = $"{str2}[{str3}]\r\n";
                foreach (SectionPair pair2 in _tmpList)
                {
                    if (pair2.Section == str3)
                    {
                        str = (string)_keyPairs[pair2];
                        if (str != null)
                        {
                            str = str == "%comment%" ? "" : $"={str}";
                        }

                        str2 = $"{str2}{pair2.Key.Replace("%eq%", "=")}{str}\r\n";
                    }
                }

                str2 = $"{str2}\r\n";
            }

            using (TextWriter writer = new StreamWriter(newFilePath))
                writer.Write(str2);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] SaveSettings Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseWriterLock();
        }
    }

    /// <summary>
    /// Sets an existing value to the new one by Section + Key
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    /// <param name="value"></param>
    public void SetSetting(string sectionName, string settingName, string value)
    {
        try
        {
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            
            SectionPair pair;
            pair.Section = sectionName;
            pair.Key = settingName;
            if (string.IsNullOrEmpty(value))
                value = string.Empty;

            if (_keyPairs.ContainsKey(pair))
            {
                _keyPairs[pair] = value;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] SetSetting Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseWriterLock();
        }
    }

    /// <summary>
    /// Checks if Section + Key exists
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    /// <returns></returns>
    public bool ContainsSetting(string sectionName, string settingName)
    {
        bool ret = false;
        try
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            
            SectionPair pair;
            pair.Section = sectionName;
            pair.Key = settingName;
            ret = _keyPairs.Contains(pair);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] GetSetting Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseReaderLock();
        }

        return ret;
    }

    /// <summary>
    /// Checks if a value exists.
    /// </summary>
    /// <param name="valueName"></param>
    /// <returns></returns>
    public bool ContainsValue(string valueName)
    {
        bool ret = false;
        try
        {
            _readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            ret = _keyPairs.ContainsValue(valueName);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] GetSetting Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseReaderLock();
        }

        return ret;
    }
}