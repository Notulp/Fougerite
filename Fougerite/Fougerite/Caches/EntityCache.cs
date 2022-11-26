using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ReaderWriterLock = Fougerite.Concurrent.ReaderWriterLock;

namespace Fougerite.Caches
{
    /// <summary>
    /// This class provides a thread-safe implementation to the entity list.
    /// Unity.Object.FindObjectsOfType (which is not thread-safe) will result in server
    /// crashes after multiple uses in a timer or thread, and It is also super slow, taking 0.8 seconds to collect & iterate
    /// when your map has 80k objects on an Intel i7-9700k.
    /// This class gets called when a new Entity is spawned, or destroyed (Thus slowing server start slightly)
    /// but allowing plugins to safely iterate Entities on another thread while not interfering with the main thread
    /// of the game and returns the list under 10ms (Tested with 80k objects again).
    /// </summary>
    public class EntityCache
    {
        private static EntityCache _entityCache;
        /// <summary>
        /// https://forum.unity.com/threads/getinstanceid-v-gethashcode.1005546/
        /// Although in Unity 4.5.5f this doesn't seem to be the case yet to check for threads, although I'm not sure of the native
        /// implementation. Pre-allocating 100003 slots (prime number), It will increase when needed. I didn't see significant increase
        /// in memory (100-150mb, this is worth for this implementation imo) when running a server with 80007 objects.
        /// </summary>
        private readonly Dictionary<int, Entity> _allEntities = new Dictionary<int, Entity>(100003);
        private readonly ReaderWriterLock _lock = new ReaderWriterLock();
        
        private EntityCache()
        {
            
        }

        /// <summary>
        /// Returns the instance.
        /// </summary>
        /// <returns></returns>
        public static EntityCache GetInstance()
        {
            if (_entityCache == null)
            {
                _entityCache = new EntityCache();
            }

            return _entityCache;
        }

        /// <summary>
        /// This method is called by the Hooks class when an entity is spawned.
        /// </summary>
        /// <param name="entity"></param>
        internal void Add(Entity entity)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                // Duplicates shouldn't happen, but It's better to handle it this way.
                _allEntities[entity.InstanceID] = entity;
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// This method is called by the Hooks class when an entity is destroyed.
        /// </summary>
        /// <param name="instanceId"></param>
        internal bool Remove(int instanceId)
        {
            bool result = false;
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                if (_allEntities.ContainsKey(instanceId))
                {
                    _allEntities.Remove(instanceId);
                    result = true;
                }
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
            bool ret;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                ret = _allEntities.ContainsKey(instanceId);
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return ret;
        }

        /// <summary>
        /// Returns a shallow copy of the entity list.
        /// </summary>
        /// <returns></returns>
        public List<Entity> GetEntities()
        {
            List<Entity> entities;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                entities = _allEntities.Values.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[EntityCache] Failed to copy the entity list. Error: {ex}");
                entities = new List<Entity>();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return entities;
        }

        /// <summary>
        /// Returns an Entity by instance id.
        /// Null if doesn't exist.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public Entity GetEntityByInstanceId(int instanceId)
        {
            Entity entity = null;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                _allEntities.TryGetValue(instanceId, out entity);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[EntityCache] Failed to get the entity from list. Error: {ex}");
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return entity;
        }
        
        /// <summary>
        /// Tries to find an Entity class by the instance id.
        /// If It doesn't exist It will allocate, but this shouldn't happen as the Instantiate events
        /// already handled that before somebody would even use the component objects.
        /// Other than that you can easily grab the class without instantiating new Entity(obj)
        /// all the time. I shall use this in the Fougerite core to pass entities to delegates.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="component"></param>
        /// <returns></returns>
        internal Entity GrabOrAllocate(int instanceId, object component)
        {
            Entity entity = null;
            Fougerite.Concurrent.LockCookie cookie = new Fougerite.Concurrent.LockCookie(int.MinValue);
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                // This shouldn't probably happen (a compatible entity missing from the list), but I still handle it just in case
                if (!_allEntities.TryGetValue(instanceId, out entity))
                {
                    cookie = _lock.UpgradeToWriterLock(Timeout.Infinite);
                    
                    // Try to safely create the entity (This should never throw errors though, but just in case)
                    try
                    {
                        entity = new Entity(component);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[EntityCache] Failed to allocate entity. Error: {ex}");
                    }

                    _allEntities[instanceId] = entity;
                    _lock.DowngradeFromWriterLock(ref cookie);
                }
            }
            catch (Exception ex)
            {
                // Sanity check, thread id should never be negative.
                // If this is a negative number, we had an upgrade to the lock which we need to downgrade.
                if (cookie.ThreadId != int.MinValue)
                    _lock.DowngradeFromWriterLock(ref cookie);
                Logger.LogError($"[EntityCache] Failed to get the entity from list. Error: {ex}");
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return entity;
        }
    }
}