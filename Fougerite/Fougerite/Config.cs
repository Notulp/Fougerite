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

        /// <summary>
        /// Retrieves the absolute path to the modules folder by replacing
        /// placeholders within the configuration string and normalizing the path
        /// for use within the application.
        /// </summary>
        /// <returns>
        /// A fully resolved and normalized string representing the modules folder
        /// path as defined in the <c>FougeriteDirectoryConfig</c>.
        /// </returns>
        public static string GetModulesFolder()
        {
            Regex root = new Regex(@"^%RootFolder%", RegexOptions.IgnoreCase);
            string path = $@"{root.Replace(FougeriteDirectoryConfig.GetSetting("Settings", "ModulesFolder"),
                Util.GetRootFolder())}\";
            return Util.NormalizePath(path);
        }

        /// <summary>
        /// Retrieves the absolute path to the public folder, replacing
        /// placeholders if necessary, and prepares the path for use.
        /// </summary>
        /// <returns>
        /// A normalized string representing the full path to the public folder.
        /// The value is derived from the configuration specified in
        /// <c>FougeriteDirectoryConfig</c>.
        /// </returns>
        public static string GetPublicFolder()
        {
            Regex root = new Regex(@"^%RootFolder%", RegexOptions.IgnoreCase);
            string path = $@"{root.Replace(FougeriteDirectoryConfig.GetSetting("Settings", "PublicFolder"),
                Util.GetRootFolder())}\";
            return Util.NormalizePath(path);
        }
    }
}