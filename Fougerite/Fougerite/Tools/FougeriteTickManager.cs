using UnityEngine;

namespace Fougerite.Tools
{
    public class FougeriteTickManager : MonoBehaviour
    {
        private static FougeriteTickManager _current;
        private static GameObject _gameObject;
        
        internal static void Initialize()
        {
            if (_gameObject == null)
            {
                if (!Application.isPlaying)
                {
                    Logger.LogWarning("[Fougerite TickManager] Server Is still loading, but accessed FougeriteTickManager!");
                    return;
                }
                _gameObject = new GameObject("FougeriteTickManager");
                DontDestroyOnLoad(_gameObject);
                _current = _gameObject.AddComponent<FougeriteTickManager>();
            }
        }
        
        void Update()
        {
            Hooks.OnServerTickHook(); 
        }
    }
}