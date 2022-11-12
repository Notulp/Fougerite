using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Fougerite;

public class IniParser
{
    private string iniFilePath;
    private Hashtable keyPairs = new Hashtable();
    public string Name;
    private List<SectionPair> tmpList = new List<SectionPair>();
    private Thread _t;

    public IniParser(string iniPath)
    {
        string str2 = null;
        iniFilePath = iniPath;
        Name = Path.GetFileNameWithoutExtension(iniPath);

        if (!File.Exists(iniPath)) throw new FileNotFoundException($"Unable to locate {iniPath}");

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
                            keyPairs.Add(pair, str3);
                            tmpList.Add(pair);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Failed adding{pair}|{str3} at {iniFilePath} Exception: {ex}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error at {iniFilePath} Exception: {ex}");
        }
    }

    public string IniPath
    {
        get { return iniFilePath; }
    }

    public void AddSetting(string sectionName, string settingName)
    {
        AddSetting(sectionName, settingName, string.Empty);
    }

    public void AddSetting(string sectionName, string settingName, string settingValue)
    {
        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        if (settingValue == null)
            settingValue = string.Empty;

        if (keyPairs.ContainsKey(pair))
        {
            keyPairs.Remove(pair);
        }
        if (tmpList.Contains(pair))
        {
            tmpList.Remove(pair);
        }
        keyPairs.Add(pair, settingValue);
        tmpList.Add(pair);
    }

    public int Count()
    {
        return Sections.Length;
    }

    public void DeleteSetting(string sectionName, string settingName)
    {
        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        if (keyPairs.ContainsKey(pair))
        {
            keyPairs.Remove(pair);
            tmpList.Remove(pair);
        }
    }

    public string[] EnumSection(string sectionName)
    {
        List<string> list = new List<string>();
        foreach (SectionPair pair in tmpList)
        {
            if (pair.Key.StartsWith(";"))
                continue;

            if (pair.Section == sectionName)
            {
                list.Add(pair.Key);
            }
        }
        return list.ToArray();
    }

    public string[] Sections
    {
        get
        {
            List<string> list = new List<string>();
            foreach (SectionPair pair in tmpList)
            {
                if (!list.Contains(pair.Section))
                {
                    list.Add(pair.Section);
                }
            }
            return list.ToArray();
        }
    }

    public string GetSetting(string sectionName, string settingName)
    {
        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        return (string)keyPairs[pair];
    }

    public bool GetBoolSetting(string sectionName, string settingName)
    {
        bool val;
        bool.TryParse(GetSetting(sectionName, settingName), out val);
        return val == true;
    }

    public bool isCommandOn(string cmdName)
    {
        return GetBoolSetting("Commands", cmdName);
    }

    public void Save()
    {
        var fi = new FileInfo(iniFilePath);
        float mega = (fi.Length / 1024f) / 1024f;
        if (mega <= 0.6)
        {
            SaveSettings(iniFilePath);
            return;
        }
        _t = new Thread(() => SaveSettings(iniFilePath));
        _t.Start();
    }

    public void SaveSettings(string newFilePath)
    {
        ArrayList list = new ArrayList();
        string str = "";
        string str2 = "";
        foreach (SectionPair pair in tmpList)
        {
            if (!list.Contains(pair.Section))
            {
                list.Add(pair.Section);
            }
        }
        foreach (string str3 in list)
        {
            str2 = $"{str2}[{str3}]\r\n";
            foreach (SectionPair pair2 in tmpList)
            {
                if (pair2.Section == str3)
                {
                    str = (string)keyPairs[pair2];
                    if (str != null) {
                        if (str == "%comment%") {
                            str = "";
                        } else {
                            str = $"={str}";
                        }
                    }
                    str2 = $"{str2}{pair2.Key.Replace("%eq%", "=")}{str}\r\n";
                }
            }
            str2 = $"{str2}\r\n";
        }

        using (TextWriter writer = new StreamWriter(newFilePath))
            writer.Write(str2);
    }

    public void SetSetting(string sectionName, string settingName, string value)
    {
        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        if (string.IsNullOrEmpty(value))
            value = string.Empty;

        if (keyPairs.ContainsKey(pair))
        {
            keyPairs[pair] = value;
        }
    }

    public bool ContainsSetting(string sectionName, string settingName)
    {
        SectionPair pair;
        pair.Section = sectionName;
        pair.Key = settingName;
        return keyPairs.Contains(pair);
    }

    public bool ContainsValue(string valueName)
    {
        return keyPairs.ContainsValue(valueName);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SectionPair
    {
        public string Section;
        public string Key;
    }
}