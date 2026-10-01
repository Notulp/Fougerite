using UnityEngine;

namespace Fougerite.Events
{
    /// <summary>
    /// Fired once the server's level and its save have loaded, right before OnServerLoaded.
    /// Fires for the stock map too, so check IsCustomMap.
    /// </summary>
    public sealed class CustomMapLoadedEvent
    {
        private readonly string _sceneName;
        private readonly string _serverLevelName;
        private readonly string _owner;
        private readonly AssetBundle _bundle;
        private readonly bool _isCustom;

        public CustomMapLoadedEvent(string sceneName, string serverLevelName, string owner, AssetBundle bundle, bool isCustom)
        {
            _sceneName = sceneName;
            _serverLevelName = serverLevelName;
            _owner = owner;
            _bundle = bundle;
            _isCustom = isCustom;
        }

        /// <summary>The scene that was actually loaded.</summary>
        public string SceneName
        {
            get { return _sceneName; }
        }

        /// <summary>The level given with -map. Differs from SceneName when a plugin replaced the map.</summary>
        public string ServerLevelName
        {
            get { return _serverLevelName; }
        }

        /// <summary>The plugin that supplied the map, or null for the stock map.</summary>
        public string Owner
        {
            get { return _owner; }
        }

        /// <summary>The bundle the scene came from, or null for the stock map. Do not Unload it.</summary>
        public AssetBundle Bundle
        {
            get { return _bundle; }
        }

        /// <summary>True if the loaded level came from a plugin rather than the game.</summary>
        public bool IsCustomMap
        {
            get { return _isCustom; }
        }
    }
}