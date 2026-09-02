using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// A singleton-class responsible for hosting and managing coroutines in a Unity environment.
    /// This class ensures that coroutines can be executed independently of other MonoBehaviour lifecycles,
    /// providing a central point for coroutine execution.
    /// </summary>
    public class CoroutineHost : MonoBehaviour
    {
        /// <summary>
        /// Represents the singleton instance of the <see cref="CoroutineHost"/> class.
        /// This variable ensures there is only one active instance of <see cref="CoroutineHost"/>,
        /// which serves as a host for running Unity coroutines outside of a specific MonoBehaviour context.
        /// </summary>
        private static volatile CoroutineHost _instance;

        /// <summary>
        /// A private static object used as a thread synchronization lock to ensure safe initialization
        /// of the singleton <see cref="CoroutineHost"/> instance in a multithreaded environment.
        /// </summary>
        private static object _lockObject = new object();

        /// <summary>
        /// Gets the singleton instance of the CoroutineHost class.
        /// This property ensures that only one instance of the CoroutineHost exists
        /// throughout the application's lifecycle. If the instance does not exist,
        /// it will initialize a new CoroutineHost object attached to a GameObject
        /// named "Fougerite_CoroutineHost", which is marked to persist across scenes.
        /// </summary>
        public static CoroutineHost Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                
                lock (_lockObject)
                {
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("Fougerite_CoroutineHost");
                        UnityEngine.Object.DontDestroyOnLoad(go);
                        _instance = go.AddComponent<CoroutineHost>();
                    }
                }

                return _instance;
            }
        }
    }
}