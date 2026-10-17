using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
    private const int WriteBufferSize = 0x10000;
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    
    [StructLayout(LayoutKind.Sequential)]
    public struct SectionPair : IEquatable<SectionPair>
    {
        public string Section;
        public string Key;

        /// <summary>
        /// Without these overrides the struct falls back to <see cref="ValueType"/> equality, which
        /// compares the fields through reflection and hashes the first field only. That means every
        /// key of the same section lands in the same hash bucket, so lookups on a big ini degrade
        /// into a linear scan with a reflection call per step.
        /// </summary>
        public bool Equals(SectionPair other)
        {
            return string.Equals(Section, other.Section, StringComparison.Ordinal)
                   && string.Equals(Key, other.Key, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SectionPair pair && Equals(pair);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Section != null ? Section.GetHashCode() : 0;
                return (hash * 397) ^ (Key != null ? Key.GetHashCode() : 0);
            }
        }
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
    /// Adds a Section and a Key to the file with an empty value.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    public void AddSetting(string sectionName, string settingName)
    {
        AddSetting(sectionName, settingName, string.Empty, null);
    }

    /// <summary>
    /// Adds a Section, Key, and Value.
    /// </summary>
    /// <param name="sectionName"></param>
    /// <param name="settingName"></param>
    /// <param name="settingValue"></param>
    public void AddSetting(string sectionName, string settingName, string settingValue)
    {
        AddSetting(sectionName, settingName, settingValue, null);
    }

    /// <summary>
    /// Adds a Section, Key, Value, and an optional documentation comment.
    /// When <paramref name="document"/> is provided and the key does not already exist, one or more
    /// <c>;</c>-prefixed comment lines are inserted directly above the key in the file so server
    /// operators can understand the setting at a glance.
    /// Multi-line documentation is supported: embed <c>\n</c> in <paramref name="document"/> and each
    /// line will become a separate <c>;</c> comment line.
    /// If the key already exists its value is updated and no new comment lines are added.
    /// </summary>
    /// <param name="sectionName">The INI section that owns the key.</param>
    /// <param name="settingName">The key name.</param>
    /// <param name="settingValue">The value to store.</param>
    /// <param name="document">
    /// Optional human-readable description.  Each logical line (split on <c>\n</c>) is written as a
    /// separate <c>; text</c> line immediately above the key.
    /// </param>
    public void AddSetting(string sectionName, string settingName, string settingValue, string document)
    {
        try
        {
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            
            SectionPair pair;
            pair.Section = sectionName;
            pair.Key = settingName;
            if (settingValue == null)
                settingValue = string.Empty;

            bool isNew = !_keyPairs.ContainsKey(pair);

            if (!isNew)
            {
                _keyPairs.Remove(pair);
                _tmpList.Remove(pair);
            }

            // Only insert document comment lines when the key is brand-new.
            if (isNew && !string.IsNullOrEmpty(document))
            {
                string[] docLines = document.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string docLine in docLines)
                {
                    string commentKey = $"; {docLine.Trim()}";
                    SectionPair commentPair;
                    commentPair.Section = sectionName;
                    commentPair.Key = commentKey.Replace("=", "%eq%");

                    if (!_keyPairs.ContainsKey(commentPair))
                    {
                        _keyPairs.Add(commentPair, "%comment%");
                        _tmpList.Add(commentPair);
                    }
                }
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
    /// Runs synchronously; writing even a multi megabyte config only costs a few milliseconds.
    /// </summary>
    public void Save()
    {
        SaveSettings(_iniFilePath);
    }

    /// <summary>
    /// Saves the current state of the ini from the memory to a file.
    /// The file is written to a temporary file first and swapped in afterwards, so an error halfway
    /// through can never leave a half written config behind.
    /// </summary>
    /// <param name="newFilePath"></param>
    public void SaveSettings(string newFilePath)
    {
        try
        {
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            
            List<string> sectionOrder = new List<string>();
            Dictionary<string, List<SectionPair>> sections = new Dictionary<string, List<SectionPair>>(StringComparer.Ordinal);

            foreach (SectionPair pair in _tmpList)
            {
                List<SectionPair> keys;
                if (!sections.TryGetValue(pair.Section, out keys))
                {
                    keys = new List<SectionPair>();
                    sections.Add(pair.Section, keys);
                    sectionOrder.Add(pair.Section);
                }

                keys.Add(pair);
            }
            
            string tempFilePath = $"{newFilePath}.tmp";

            using (FileStream stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, WriteBufferSize))
            using (StreamWriter writer = new StreamWriter(stream, Utf8NoBom, WriteBufferSize))
            {
                writer.NewLine = "\r\n";

                foreach (string section in sectionOrder)
                {
                    writer.Write('[');
                    writer.Write(section);
                    writer.Write("]\r\n");

                    foreach (SectionPair pair in sections[section])
                    {
                        string key = pair.Key;
                        if (key.IndexOf("%eq%", StringComparison.Ordinal) != -1)
                        {
                            key = key.Replace("%eq%", "=");
                        }

                        writer.Write(key);

                        string value = (string)_keyPairs[pair];
                        if (value != null && value != "%comment%")
                        {
                            writer.Write('=');
                            writer.Write(value);
                        }

                        writer.Write("\r\n");
                    }

                    writer.Write("\r\n");
                }

                writer.Flush();
            }

            if (File.Exists(newFilePath))
            {
                File.Delete(newFilePath);
            }

            File.Move(tempFilePath, newFilePath);
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
    /// Adds a setting with the given default value only if the key does not already exist in the section.
    /// A document comment line (starting with <c>;</c>) is written immediately before the key to describe
    /// what the setting does.  Both the comment and the key are skipped when the key is already present,
    /// so existing user-configured values are never overwritten.
    /// Call <see cref="Save"/> after all <c>AddDefault</c> calls to persist any newly added entries.
    /// </summary>
    /// <param name="sectionName">The section that contains the setting (e.g. "Fougerite").</param>
    /// <param name="settingName">The key name of the setting.</param>
    /// <param name="defaultValue">The default value to write when the key is missing.</param>
    /// <param name="document">
    /// A human-readable description of the setting.  Written as a <c>;</c>-comment line directly
    /// above the key so server operators can understand its purpose without consulting external docs.
    /// </param>
    public void AddDefault(string sectionName, string settingName, string defaultValue, string document)
    {
        try
        {
            _readerWriterLock.AcquireWriterLock(Timeout.Infinite);

            SectionPair pair;
            pair.Section = sectionName;
            pair.Key = settingName;

            // Only add when the key is genuinely absent,never overwrite user values.
            if (_keyPairs.ContainsKey(pair))
                return;

            if (defaultValue == null)
                defaultValue = string.Empty;

            // Insert one comment line per logical line in document so the file stays readable.
            // Multi-line documentation is supported: split on newlines and write each as a separate ; entry.
            if (!string.IsNullOrEmpty(document))
            {
                string[] docLines = document.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string docLine in docLines)
                {
                    string commentKey = $"; {docLine.Trim()}";
                    SectionPair commentPair;
                    commentPair.Section = sectionName;
                    commentPair.Key = commentKey.Replace("=", "%eq%");

                    if (!_keyPairs.ContainsKey(commentPair))
                    {
                        _keyPairs.Add(commentPair, "%comment%");
                        _tmpList.Add(commentPair);
                    }
                }
            }

            _keyPairs.Add(pair, defaultValue);
            _tmpList.Add(pair);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[IniParser] AddDefault Error: {ex}");
        }
        finally
        {
            _readerWriterLock.ReleaseWriterLock();
        }
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