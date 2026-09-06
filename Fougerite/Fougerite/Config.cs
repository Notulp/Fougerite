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
            if (FougeriteConfig == null)
            {
                Debug.LogError("Fougerite.cfg failed to load. " +
                               "Either you are missing a FougeriteDirectory.cfg, " +
                               "missing the content of the file, " +
                               "or you are starting rust_server.exe wrong in a batch file.");
                return null;
            }

            return FougeriteConfig.GetSetting(Section, Setting);
        }

        public static bool GetBoolValue(string Section, string Setting)
        {
            if (FougeriteConfig == null)
            {
                Debug.LogError("Fougerite.cfg failed to load. " +
                               "Either you are missing a FougeriteDirectory.cfg, " +
                               "missing the content of the file, " +
                               "or you are starting rust_server.exe wrong in a batch file.");
                return false;
            }

            return FougeriteConfig.GetBoolSetting(Section, Setting);
        }

        /// <summary>
        /// Adds a Section, Key, Value, and an optional documentation comment to <c>Fougerite.cfg</c>.
        /// When <paramref name="Document"/> is provided and the key does not already exist, one or more
        /// <c>;</c>-prefixed comment lines are inserted directly above the key.
        /// Multi-line documentation is supported: embed <c>\n</c> in <paramref name="Document"/> and each
        /// line will become a separate <c>;</c> comment line.
        /// If the key already exists its value is updated and no comment lines are added.
        /// </summary>
        /// <param name="Section">The INI section (e.g. <c>"Fougerite"</c>).</param>
        /// <param name="Setting">The key name of the setting.</param>
        /// <param name="Value">The value to store.</param>
        /// <param name="Document">
        /// Optional human-readable description.  Each logical line (split on <c>\n</c>) is written as a
        /// separate <c>; text</c> line immediately above the key.
        /// </param>
        public static void AddSetting(string Section, string Setting, string Value, string Document)
        {
            if (FougeriteConfig == null)
            {
                Debug.LogError("Fougerite.cfg failed to load. " +
                               "Either you are missing a FougeriteDirectory.cfg, " +
                               "missing the content of the file, " +
                               "or you are starting rust_server.exe wrong in a batch file.");
                return;
            }

            FougeriteConfig.AddSetting(Section, Setting, Value, Document);
        }

        /// <summary>
        /// Adds a setting with its default value to <c>Fougerite.cfg</c> only when the key is not already
        /// present.  A <c>;</c>-prefixed comment line (<paramref name="document"/>) is inserted directly
        /// above the key so server operators can understand the setting without consulting external docs.
        /// This method is a no-op when the key exists, so user-edited values are never overwritten.
        /// Call <see cref="Save"/> after all <c>AddDefault</c> calls to flush new entries to disk.
        /// </summary>
        /// <param name="Section">The INI section (e.g. <c>"Fougerite"</c>).</param>
        /// <param name="Setting">The key name of the setting.</param>
        /// <param name="DefaultValue">Value to write when the key is absent.</param>
        /// <param name="Document">
        /// Human-readable description written as a <c>;</c> comment line above the key.
        /// </param>
        public static void AddDefault(string Section, string Setting, string DefaultValue, string Document)
        {
            if (FougeriteConfig == null)
            {
                Debug.LogError("Fougerite.cfg failed to load. " +
                               "Either you are missing a FougeriteDirectory.cfg, " +
                               "missing the content of the file, " +
                               "or you are starting rust_server.exe wrong in a batch file.");
                return;
            }

            FougeriteConfig.AddDefault(Section, Setting, DefaultValue, Document);
        }

        /// <summary>
        /// Saves the current in-memory state of <c>Fougerite.cfg</c> to disk.
        /// Any defaults registered via <see cref="AddDefault"/> that were not already present in the file
        /// will be persisted by this call.
        /// </summary>
        public static void Save()
        {
            if (FougeriteConfig == null)
            {
                Debug.LogError("Fougerite.cfg failed to load. " +
                               "Either you are missing a FougeriteDirectory.cfg, " +
                               "missing the content of the file, " +
                               "or you are starting rust_server.exe wrong in a batch file.");
                return;
            }

            FougeriteConfig.Save();
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
            if (FougeriteDirectoryConfig == null)
            {
                Debug.LogError("FougeriteDirectory.cfg failed to load. " +
                               "Either you are missing a FougeriteDirectory.cfg, " +
                               "missing the content of the file, " +
                               "or you are starting rust_server.exe wrong in a batch file.");
                return Util.NormalizePath(Path.Combine(Util.GetRootFolder(), "Modules"));
            }

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
            if (FougeriteDirectoryConfig == null)
            {
                Debug.LogError("FougeriteDirectory.cfg failed to load. " +
                               "Either you are missing a FougeriteDirectory.cfg, " +
                               "missing the content of the file, " +
                               "or you are starting rust_server.exe wrong in a batch file.");
                return Util.NormalizePath(Path.Combine(Util.GetRootFolder(), "Save"));
            }

            Regex root = new Regex(@"^%RootFolder%", RegexOptions.IgnoreCase);
            string path = $@"{root.Replace(FougeriteDirectoryConfig.GetSetting("Settings", "PublicFolder"),
                Util.GetRootFolder())}\";
            return Util.NormalizePath(path);
        }
    }
}