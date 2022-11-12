using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Fougerite
{
    public class Loom : MonoBehaviour
    {
        private static Loom _current;
        private static GameObject _gameObject;
        internal static int numThreads;
        internal static bool initialized = false;
        private readonly List<Action> _currentActions = new List<Action>();
        private readonly List<Action> _actions = new List<Action>();
        private readonly List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();
        private readonly List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();
        
        /// <summary>
        /// Struct for the action to be ran under a thread.
        /// </summary>
        public struct DelayedQueueItem
        {
            public float time;
            public Action action;
        }

        /// <summary>
        /// Maximum amount of threads that can be queued at a time. Do not set this to a large number or rust will die.
        /// </summary>
        public static int maxThreads = 30;

        /// <summary>
        /// Returns the current amount of queued threads.
        /// </summary>
        public int AmountOfThreads
        {
            get { return numThreads; }
        }

        /// <summary>
        /// Returns the instance of the Loom Class.
        /// </summary>
        public static Loom Current
        {
            get
            {
                Initialize();
                if (_current == null)
                {
                    _gameObject = new GameObject("Loom");
                    _current = _gameObject.AddComponent<Loom>();
                }
                return _current;
            }
        }

        public void Awake()
        {
            //_current = this;
            initialized = true;
        }

        private static void Initialize()
        {
            if (!initialized)
            {
                if (!Application.isPlaying)
                {
                    Logger.LogWarning("[Fougerite Loom] Server Is still loading, but a plugin already accessed Loom!");
                    return;
                }
                initialized = true;
                _gameObject = new GameObject("Loom");
                _current = _gameObject.AddComponent<Loom>();
            }
        }

        /// <summary>
        /// Runs the code on the MAIN thread.
        /// </summary>
        /// <param name="action"></param>
        public static void QueueOnMainThread(Action action)
        {
            QueueOnMainThread(action, 0f);
        }

        /// <summary>
        /// Runs the code on the MAIN thread.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="time"></param>
        public static void QueueOnMainThread(Action action, float time)
        {
            try
            {
                if (time != 0)
                {
                    lock (Current._delayed)
                    {
                        Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action });
                    }
                }
                else
                {
                    lock (Current._actions)
                    {
                        Current._actions.Add(action);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    $"[Fougerite Loom Error] {ex} - {Time.time} - {time} - {action} - {Current._actions} - {Current._delayed}");
            }
        }

        /// <summary>
        /// Runs the code on a sub thread.
        /// </summary>
        /// <param name="action"></param>
        public static void ExecuteInBiggerStackThread(Action action)
        {
            Thread bigStackThread = new Thread(() => action(), 1024 * 1024);
            bigStackThread.IsBackground = true;
            bigStackThread.Start();
        }

        public static Thread RunAsync(Action a)
        {
            Initialize();
            while (numThreads >= maxThreads)
            {
                Thread.Sleep(1);
            }
            Interlocked.Increment(ref numThreads);
            ThreadPool.QueueUserWorkItem(RunAction, a);
            return null;
        }

        private static void RunAction(object action)
        {
            try
            {
                ((Action)action)();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Loom RunAction] Error: {ex}");
            }
            finally
            {
                Interlocked.Decrement(ref numThreads);
            }
        }


        public void OnDisable()
        {
            if (_current == this)
            {
                _current = null;
            }
        }
        

        // Use this for initialization
        public void Start()
        {

        }

        // Update is called once per frame
        public void Update()
        {
            lock (_actions)
            {
                _currentActions.Clear();
                _currentActions.AddRange(_actions);
                _actions.Clear();
            }
            foreach (var a in _currentActions)
            {
                a();
            }
            lock (_delayed)
            {
                _currentDelayed.Clear();
                _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
                foreach (var item in _currentDelayed)
                    _delayed.Remove(item);
            }
            foreach (var delayed in _currentDelayed)
            {
                delayed.action();
            }
        }
    }
}