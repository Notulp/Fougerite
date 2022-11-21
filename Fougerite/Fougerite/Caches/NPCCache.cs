using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ReaderWriterLock = Fougerite.Concurrent.ReaderWriterLock;

namespace Fougerite.Caches
{
    public class NPCCache
    {
        private static NPCCache _npcCache;
        private readonly Dictionary<int, NPC> _allNpcs = new Dictionary<int, NPC>(300);
        private readonly ReaderWriterLock _lock = new ReaderWriterLock();

        private NPCCache()
        {
            
        }
        
        /// <summary>
        /// Returns the instance.
        /// </summary>
        /// <returns></returns>
        public static NPCCache GetInstance()
        {
            if (_npcCache == null)
            {
                _npcCache = new NPCCache();
            }

            return _npcCache;
        }
        
        /// <summary>
        /// This method is called by the Hooks class when an NPC is spawned.
        /// </summary>
        /// <param name="npc"></param>
        internal void Add(NPC npc)
        {
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                // Duplicates shouldn't happen, but It's better to handle it this way.
                _allNpcs[npc.GetHashCode()] = npc;
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// This method is called by the Hooks class when an NPC is killed/destroyed.
        /// </summary>
        /// <param name="instanceId"></param>
        internal bool Remove(int instanceId)
        {
            bool result = false;
            try
            {
                _lock.AcquireWriterLock(Timeout.Infinite);
                if (_allNpcs.ContainsKey(instanceId))
                {
                    _allNpcs.Remove(instanceId);
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
                ret = _allNpcs.ContainsKey(instanceId);
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return ret;
        }
        
        /// <summary>
        /// Returns a shallow copy of the npc list.
        /// </summary>
        /// <returns></returns>
        public List<NPC> GetNPCs()
        {
            List<NPC> entities;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                entities = _allNpcs.Values.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[NPCCache] Failed to copy the NPC list. Error: {ex}");
                entities = new List<NPC>();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return entities;
        }

        /// <summary>
        /// Returns an NPC by instance id.
        /// Null if doesn't exist.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public NPC GetEntityByInstanceId(int instanceId)
        {
            NPC npc = null;
            try
            {
                _lock.AcquireReaderLock(Timeout.Infinite);
                _allNpcs.TryGetValue(instanceId, out npc);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[NPCCache] Failed to get the NPC from list. Error: {ex}");
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            return npc;
        }
    }
}