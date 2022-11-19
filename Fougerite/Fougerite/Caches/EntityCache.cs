using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fougerite.Tools;
using ReaderWriterLock = Fougerite.Concurrent.ReaderWriterLock;

namespace Fougerite.Caches
{
    /// <summary>
    /// This class provides a thread-safe implementation to the entity list.
    /// Unity.Object.FindObjectsOfType (which is not thread-safe) will result in server
    /// crashes after multiple uses in a timer or thread.
    /// This class gets called when a new Entity is spawned, or destroyed (Thus slowing server start slightly)
    /// but allowing plugins to safely iterate Entities on another thread while not interfering with the main thread
    /// of the game and cause lagging.
    /// </summary>
    public class EntityCache
    {
        private static EntityCache _entityCache;
        // https://forum.unity.com/threads/getinstanceid-v-gethashcode.1005546/
        // Although in Unity 4.5.5f this doesn't seem to be the case yet to check for threads, although I'm not sure of the native
        // implementation
        private readonly Dictionary<int, Entity> _allEntities = new Dictionary<int, Entity>();
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
        /// <param name="entity"></param>
        internal void Remove(Entity entity)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                if (_allEntities.ContainsKey(entity.InstanceID))
                {
                    _allEntities.Remove(entity.InstanceID);
                }
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
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
                entities = Cloner<List<Entity>>.Clone(_allEntities.Values.ToList());
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
    }
}