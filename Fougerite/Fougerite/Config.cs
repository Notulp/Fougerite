using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Fougerite
{
    public class Config
    {
        public static IniParser FougeriteDirectoryConfig;
        public static IniParser FougeriteConfig;

        public static void Init(string DirectoryConfigPath)
        {
            try
            {
                if (File.Exists(DirectoryConfigPath))
                {
                    FougeriteDirectoryConfig = new IniParser(DirectoryConfigPath);
                    Debug.Log($"DirectoryConfig {DirectoryConfigPath} loaded.");
                }
                else Debug.Log($"DirectoryConfig {DirectoryConfigPath} NOT LOADED.");

                string ConfigPath = Path.Combine(GetPublicFolder(), "Fougerite.cfg");

                if (File.Exists(ConfigPath))
                {
                    FougeriteConfig = new IniParser(ConfigPath);
                    Debug.Log($"Config {ConfigPath} loaded.");
                }
                else Debug.Log($"Config {ConfigPath} NOT LOADED.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Fougerite Config] Error, failed to read configs: {ex}");
            }
        }

        public static string GetValue(string Section, string Setting)
        {
            return FougeriteConfig.GetSetting(Section, Setting);
        }

        public static bool GetBoolValue(string Section, string Setting)
        {
            return FougeriteConfig.GetBoolSetting(Section, Setting);
        }

        public static string GetModulesFolder()
        {
            Regex root = new Regex(@"^%RootFolder%", RegexOptions.IgnoreCase);
            string path = $@"{root.Replace(FougeriteDirectoryConfig.GetSetting("Settings", "ModulesFolder"),
                Util.GetRootFolder())}\";
            return Util.NormalizePath(path);
        }

        public static string GetPublicFolder()
        {
            Regex root = new Regex(@"^%RootFolder%", RegexOptions.IgnoreCase);
            string path = $@"{root.Replace(FougeriteDirectoryConfig.GetSetting("Settings", "PublicFolder"),
                Util.GetRootFolder())}\";
            return Util.NormalizePath(path);
        }
    }
}