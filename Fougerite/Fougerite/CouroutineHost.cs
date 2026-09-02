using Fougerite.Concurrent;
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
        /// Lazily initialized singleton instance of the <see cref="CoroutineHost"/> class.
        /// Uses <see cref="Lazy{T}"/> to ensure thread-safe initialization.
        /// </summary>
        private static readonly Lazy<CoroutineHost> InstanceC = new Lazy<CoroutineHost>(() =>
        {
            GameObject go = new GameObject("Fougerite_CoroutineHost");
            Object.DontDestroyOnLoad(go);
            return go.AddComponent<CoroutineHost>();
        });

        /// <summary>
        /// Gets the singleton instance of the CoroutineHost class.
        /// This property ensures that only one instance of the CoroutineHost exists
        /// throughout the application's lifecycle. If the instance does not exist,
        /// it will initialize a new CoroutineHost object attached to a GameObject
        /// named "Fougerite_CoroutineHost", which is marked to persist across scenes.
        /// </summary>
        public static CoroutineHost Instance
        {
            get { return InstanceC.Value; }
        }
    }
}