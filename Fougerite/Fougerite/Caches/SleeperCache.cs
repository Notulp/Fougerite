using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Fougerite.Caches
{
    public class SleeperCache
    {
        private static SleeperCache _sleeperCache;
        private readonly Dictionary<int, Sleeper> _allSleepers = new Dictionary<int, Sleeper>(100);
        private readonly ReaderWriterLock _lock = new ReaderWriterLock();

        private SleeperCache()
        {
            
        }
        
        /// <summary>
        /// Returns the instance.
        /// </summary>
        /// <returns></returns>
        public static SleeperCache GetInstance()
        {
            if (_sleeperCache == null)
            {
                _sleeperCache = new SleeperCache();
            }

            return _sleeperCache;
        }
        
        /// <summary>
        /// This method is called by the Hooks class when an Sleeper is spawned.
        /// </summary>
        /// <param name="sleeper"></param>
        internal void Add(Sleeper sleeper)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                // Duplicates shouldn't happen, but It's better to handle it this way.
                _allSleepers[sleeper.InstanceID] = sleeper;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{nameof(SleeperCache)}] Failed to add to the entity list. Error: {ex}");
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// This method is called by the Hooks class when an Sleeper is killed/destroyed.
        /// </summary>
        /// <param name="instanceId"></param>
        internal bool Remove(int instanceId)
        {
            bool result = false;
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                if (_allSleepers.ContainsKey(instanceId))
                {
                    _allSleepers.Remove(instanceId);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{nameof(SleeperCache)}] Failed to remove from the entity list. Error: {ex}");
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }

            return result;
        }

        /// <summary>
        /// Checks if the instance id is in the dictionary.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        internal bool Contains(int instanceId)
        {
            bool result = false;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                result = _allSleepers.ContainsKey(instanceId);
            }
            catch (Exception)
            {
                // Ignore...
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return result;
        }
        
        /// <summary>
        /// Returns a shallow copy of the Sleeper list.
        /// </summary>
        /// <returns></returns>
        public List<Sleeper> GetSleepers()
        {
            List<Sleeper> entities;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                entities = _allSleepers.Values.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{nameof(SleeperCache)}] Failed to copy the Sleeper list. Error: {ex}");
                entities = new List<Sleeper>();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return entities;
        }

        /// <summary>
        /// Returns an Sleeper by instance id.
        /// Null if doesn't exist.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public Sleeper GetEntityByInstanceId(int instanceId)
        {
            Sleeper sleeper = null;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                _allSleepers.TryGetValue(instanceId, out sleeper);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[{nameof(SleeperCache)}] Failed to get the Sleeper from list. Error: {ex}");
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return sleeper;
        }
    }
}