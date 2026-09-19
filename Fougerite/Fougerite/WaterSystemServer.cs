using System;
using System.Collections.Generic;
using Fougerite.Events;
using UnityEngine;

namespace Fougerite
{
    /// <summary>
    /// Server side swimming and drowning.
    ///
    /// WHY IT EXISTS
    /// -------------
    /// Vanilla HumanController.ServerFrame ended with an instant kill:
    ///
    ///     if (alive &amp;&amp; WaterLine.Height != 0f &amp;&amp; transform.position.y &lt;= WaterLine.Height)
    ///         TakeDamage.Hurt(idMain, idMain, health * 2f, null);
    ///
    /// Touch the water plane, take 200% health. That is the entire drowning mechanic, and it
    /// is why nobody can swim. This class replaces it with an oxygen budget.
    ///
    /// WHY POSITION AND NOT THE SWIM FLAG
    /// ----------------------------------
    /// The client publishes a swim state on stateFlags bit 32768, which survives the
    /// "&amp; -24577" mask and reaches the server. It is NOT used to decide drowning, because
    /// the client sets it: a modified client would simply never set it and never drown.
    /// Drowning is driven by the player's position against WaterLine.Height, which is the
    /// same condition vanilla used and which TruthDetector already validates. The flag is
    /// only carried through to plugins as advisory information.
    ///
    /// STATE
    /// -----
    /// Oxygen is kept per character instance rather than on Metabolism, so no game field has
    /// to be added. Entries prune themselves once they go stale, which covers disconnects,
    /// deaths and level changes without needing a hook for any of them.
    /// </summary>
    public static class WaterSystemServer
    {
        /// <summary>stateFlags bit the client sets while swimming. Advisory only.</summary>
        public const ushort SwimFlag = 32768;

        // ------------------------------------------------------------------
        // Config
        // ------------------------------------------------------------------

        /// <summary>
        /// When false, the vanilla behaviour is restored and touching the waterline kills
        /// outright. Here so the change can be turned off without repatching.
        /// </summary>
        public static bool AllowSwimming = false;

        /// <summary>Seconds a player can stay under before they start taking damage.</summary>
        public static float OxygenSeconds = 60f;

        /// <summary>How much faster air refills than it drains once back above the surface.</summary>
        public static float OxygenRefillMultiplier = 3f;

        /// <summary>Damage per minute once the air is gone. Mirrors starvingDamagePerMin.</summary>
        public static float DrownDamagePerMinute = 60f;

        /// <summary>
        /// How far below the waterline the eyes have to be before air starts draining.
        /// Keeps a player bobbing at the surface from slowly suffocating.
        /// </summary>
        public static float SubmergedDepth = 0.4f;

        /// <summary>
        /// Height of the head above the character origin, in metres.
        ///
        /// Character.eyesOrigin does not line up with the visible head on this model, which
        /// is why the client stopped using it too. A fixed height is predictable and, more
        /// importantly, is the same rule both sides follow.
        /// </summary>
        public static float HeadHeight = 1.5f;

        /// <summary>
        /// Use Character.eyesOrigin instead of HeadHeight. Off by default, matching the
        /// client's WaterSystem.UseCameraAsFace behaviour of not trusting it.
        /// </summary>
        public static bool UseEyesOrigin = false;

        /// <summary>
        /// Shifts the point at which the head counts as under, in metres. Must match the
        /// client's WaterSystem.SubmergeTriggerOffset or the two disagree about when someone
        /// is drowning: POSITIVE counts them as under sooner.
        /// </summary>
        public static float SubmergeTriggerOffset = -0.15f;

        /// <summary>Suppress the HearFootstep broadcast while a player is in water.</summary>
        public static bool SilenceFootstepsInWater = true;

        /// <summary>Seconds without an update before a player's oxygen state is dropped.</summary>
        public static float StateTimeout = 60f;

        /// <summary>
        /// Push the server's oxygen value to the owning client through the injected
        /// Metabolism.rbOxy(float) RPC.
        ///
        /// The client predicts locally between updates, because a bar driven at the send
        /// rate looks broken, but the server value is the truth and the client snaps to it
        /// when the two drift apart.
        /// </summary>
        public static bool SyncOxygenToClient = true;

        /// <summary>
        /// Seconds between oxygen pushes. Fast enough that the client never drifts far,
        /// slow enough not to matter on the wire.
        /// </summary>
        public static float OxygenSyncInterval = 0.4f;

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        private sealed class OxygenState
        {
            public float Oxygen = 1f;
            public float SubmergedSince = -1f;
            public float LastSeen;
            public float NextSync;

            /// <summary>
            /// Set once an rbOxy send throws, so a client whose build lacks the injected RPC
            /// is not spammed with a failing call several times a second for the rest of the
            /// session. One failure is enough to know.
            /// </summary>
            public bool RpcUnsupported;

            /// <summary>Cached so the send path does not GetComponent several times a second.</summary>
            public Metabolism Metabolism;

            /// <summary>
            /// Cached Player lookup. FindPlayer walks the player list, and the drowning path
            /// hit it on every server frame for every drowning player.
            /// </summary>
            public Player Player;
        }

        private static readonly Dictionary<int, OxygenState> States = new Dictionary<int, OxygenState>();
        private static float _nextPrune;

        // ------------------------------------------------------------------
        // Dry regions
        //
        // Registered volumes that read as dry no matter how far below the waterline they
        // are, so a sealed structure does not drown whoever is inside it.
        //
        // Bucketed into a flat grid rather than kept as one list. A populated server can
        // easily have thousands of these once people build underwater, and this test runs
        // per player per server frame, so a linear scan would not hold up. Each region is
        // filed under every cell it touches, and a lookup only ever examines one cell.
        // ------------------------------------------------------------------
        private sealed class DryRegion
        {
            public string Owner;
            public string Key;
            public Bounds Bounds;
        }

        /// <summary>Size of a spatial bucket in metres. Larger means fewer, fuller buckets.</summary>
        public static float ExclusionCellSize = 32f;

        private static readonly Dictionary<long, List<DryRegion>> Grid = new Dictionary<long, List<DryRegion>>();
        private static readonly Dictionary<string, DryRegion> ByKey = new Dictionary<string, DryRegion>();

        /// <summary>How many dry regions are registered.</summary>
        public static int ExclusionCount
        {
            get { return ByKey.Count; }
        }

        /// <summary>
        /// Registers a dry region. Re-registering the same key replaces the old one, so a
        /// structure that grows can simply be re-registered rather than removed first.
        /// </summary>
        /// <param name="owner">Plugin name, so everything can be dropped on unload.</param>
        /// <param name="key">Stable identity for this region, for example a structure id.</param>
        /// <param name="bounds">The dry volume in world space.</param>
        public static void AddExclusion(string owner, string key, Bounds bounds)
        {
            if (string.IsNullOrEmpty(key)) return;

            RemoveExclusion(key);

            DryRegion region = new DryRegion
            {
                Owner = string.IsNullOrEmpty(owner) ? "Unknown" : owner,
                Key = key,
                Bounds = bounds
            };

            ByKey[key] = region;

            foreach (long cell in CellsFor(bounds))
            {
                List<DryRegion> bucket;
                if (!Grid.TryGetValue(cell, out bucket))
                {
                    bucket = new List<DryRegion>();
                    Grid[cell] = bucket;
                }
                bucket.Add(region);
            }
        }

        /// <summary>Removes a dry region by key. Returns false if it was not registered.</summary>
        public static bool RemoveExclusion(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            DryRegion region;
            if (!ByKey.TryGetValue(key, out region)) return false;

            ByKey.Remove(key);

            foreach (long cell in CellsFor(region.Bounds))
            {
                List<DryRegion> bucket;
                if (!Grid.TryGetValue(cell, out bucket)) continue;

                bucket.Remove(region);
                if (bucket.Count == 0) Grid.Remove(cell);
            }

            return true;
        }

        /// <summary>Drops every dry region belonging to a plugin. Call this on plugin unload.</summary>
        public static int RemoveExclusionsFor(string owner)
        {
            if (string.IsNullOrEmpty(owner)) return 0;

            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, DryRegion> entry in ByKey)
            {
                if (string.Equals(entry.Value.Owner, owner, StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(entry.Key);
                }
            }

            for (int i = 0; i < keys.Count; i++)
            {
                RemoveExclusion(keys[i]);
            }

            return keys.Count;
        }

        /// <summary>Drops every dry region.</summary>
        public static void ClearExclusions()
        {
            Grid.Clear();
            ByKey.Clear();
        }

        /// <summary>True when the point sits inside a registered dry region.</summary>
        public static bool IsPointExcluded(Vector3 point)
        {
            if (ByKey.Count == 0) return false;

            List<DryRegion> bucket;
            if (!Grid.TryGetValue(CellKey(point.x, point.z), out bucket)) return false;

            for (int i = 0; i < bucket.Count; i++)
            {
                if (bucket[i].Bounds.Contains(point)) return true;
            }

            return false;
        }

        private static long CellKey(float x, float z)
        {
            float size = ExclusionCellSize <= 0f ? 32f : ExclusionCellSize;
            long cx = (long)Mathf.Floor(x / size);
            long cz = (long)Mathf.Floor(z / size);
            return (cx << 32) ^ (cz & 0xFFFFFFFFL);
        }

        private static IEnumerable<long> CellsFor(Bounds bounds)
        {
            float size = ExclusionCellSize <= 0f ? 32f : ExclusionCellSize;

            long minX = (long)Mathf.Floor(bounds.min.x / size);
            long maxX = (long)Mathf.Floor(bounds.max.x / size);
            long minZ = (long)Mathf.Floor(bounds.min.z / size);
            long maxZ = (long)Mathf.Floor(bounds.max.z / size);

            for (long cx = minX; cx <= maxX; cx++)
            {
                for (long cz = minZ; cz <= maxZ; cz++)
                {
                    yield return (cx << 32) ^ (cz & 0xFFFFFFFFL);
                }
            }
        }

        /// <summary>How many players currently have tracked oxygen state.</summary>
        public static int TrackedCount
        {
            get { return States.Count; }
        }

        /// <summary>
        /// Remaining air from 0 to 1 for a character, or 1 when it is not being tracked.
        /// Returns -1 on wrong character input.
        /// </summary>
        public static float GetOxygen(Character character)
        {
            if (!character) return -1f;

            OxygenState state;
            return States.TryGetValue(character.GetInstanceID(), out state) ? state.Oxygen : 1f;
        }

        /// <summary>Drops a character's tracked state, for example on respawn.</summary>
        public static void ResetOxygen(Character character)
        {
            if (!character) return;
            States.Remove(character.GetInstanceID());
        }

        /// <summary>Drops all tracked state.</summary>
        public static void Clear()
        {
            States.Clear();
        }

        // ------------------------------------------------------------------
        // Queries
        // ------------------------------------------------------------------

        /// <summary>
        /// The global waterline, or float.MinValue when this level has no water. A height of
        /// 0 is the "no water" case vanilla tested for.
        /// </summary>
        public static float WaterLineHeight
        {
            get
            {
                try
                {
                    float h = WaterLine.Height;
                    return h == 0f ? float.MinValue : h;
                }
                catch
                {
                    return float.MinValue;
                }
            }
        }

        /// <summary>
        /// How far the point is below the waterline. Zero or less means dry, and a point
        /// inside a registered dry region always reads as dry however deep it is.
        /// </summary>
        public static float DepthBelowWater(Vector3 point)
        {
            float line = WaterLineHeight;
            if (line == float.MinValue) return 0f;
            if (point.y >= line) return 0f;
            if (IsPointExcluded(point)) return 0f;
            return line - point.y;
        }

        /// <summary>True while any part of the character is in water.</summary>
        public static bool IsInWater(Character character)
        {
            if (!character) return false;
            return DepthBelowWater(character.transform.position) > 0f;
        }

        /// <summary>
        /// The character's eye position, which is what decides whether the head is under.
        ///
        /// Drowning has to be measured at the head, not at the feet. Using transform.position
        /// means a player standing chest deep with their head clearly in the air is treated
        /// as submerged, which is both wrong and out of step with the client, where the same
        /// player is Wading and breathing normally.
        /// </summary>
        public static Vector3 EyePosition(Character character)
        {
            if (!character) return Vector3.zero;

            if (UseEyesOrigin)
            {
                try
                {
                    Vector3 eyes = character.eyesOrigin;
                    if (eyes.sqrMagnitude > 0.0001f) return eyes;
                }
                catch
                {
                }
            }

            float height = HeadHeight;
            if (character.stateFlags.crouch) height *= 0.7f;

            return character.transform.position + Vector3.up * height;
        }

        /// <summary>
        /// How far the character's head is below the waterline. Zero or less means they can
        /// breathe. This is the value drowning is computed from.
        /// </summary>
        public static float HeadDepthBelowWater(Character character)
        {
            if (!character) return 0f;

            // The offset shifts the sample rather than the waterline, so it composes the same
            // way the client's does and both sides agree on the moment someone goes under.
            return DepthBelowWater(EyePosition(character) + Vector3.up * SubmergeTriggerOffset);
        }

        // ------------------------------------------------------------------
        // Driver
        // ------------------------------------------------------------------

        /// <summary>
        /// Runs the drowning check for one player, never throwing.
        ///
        /// This is called from the managed replacement for HumanController.ServerFrame, which
        /// runs for every player on every server frame. An exception escaping here would
        /// abort the rest of that player's frame, so it is contained and counted instead, and
        /// the whole feature switches off if it keeps failing.
        /// </summary>
        public static void ServerFrameWaterCheck(HumanController hc)
        {
            if (!AllowSwimmingInternalGuard) return;

            try
            {
                ServerFrameWaterCheckInternal(hc);
                _consecutiveFailures = 0;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                Logger.LogError($"[WaterSystem] ServerFrameWaterCheck failed ({_consecutiveFailures}/{MaxConsecutiveFailures}): {ex}");

                if (_consecutiveFailures < MaxConsecutiveFailures) return;

                Logger.LogError("[WaterSystem] Too many consecutive failures, disabling the server water system.");
                AllowSwimmingInternalGuard = false;
            }
        }

        /// <summary>
        /// False once the system has failed too many times in a row. Vanilla drowning is NOT
        /// restored when this trips: killing everyone who touches water because a check threw
        /// is worse than nobody drowning.
        /// </summary>
        public static bool AllowSwimmingInternalGuard = true;

        /// <summary>Consecutive failures before the server side switches itself off.</summary>
        public static int MaxConsecutiveFailures = 10;

        private static int _consecutiveFailures;

        private static void ServerFrameWaterCheckInternal(HumanController hc)
        {
            if (!hc || !hc.alive) return;

            // Cheapest possible early out. This runs for every player on every server frame,
            // and on a map with no water configured there is nothing to do at all.
            if (WaterLineHeight == float.MinValue) return;

            Character character = hc.GetComponent<Character>();
            if (!character) return;

            // Measured at the head, matching the client. Feet below the line with the head
            // in the air is wading, not drowning.
            float depth = HeadDepthBelowWater(character);

            if (!AllowSwimming)
            {
                // Vanilla behaviour, kept reachable so this can be switched off without
                // repatching the assembly.
                if (depth > 0f)
                {
                    TakeDamage.Hurt(hc.idMain, hc.idMain, hc.health * 2f, null);
                }
                return;
            }

            Prune();

            if (depth <= 0f)
            {
                float refillElapsed = Time.time - hc.lastServerFrameTime;
                if (refillElapsed <= 0f || refillElapsed > 5f) refillElapsed = 0f;

                Refill(character, refillElapsed);
                return;
            }

            OxygenState state = GetOrCreate(character);
            state.LastSeen = Time.time;

            // Bobbing at the surface does not drain air, only being properly under does.
            bool submerged = depth >= SubmergedDepth;
            if (!submerged)
            {
                state.SubmergedSince = -1f;
                return;
            }

            if (state.SubmergedSince < 0f) state.SubmergedSince = Time.time;

            float elapsed = Time.time - hc.lastServerFrameTime;
            if (elapsed <= 0f || elapsed > 5f) elapsed = 0f;

            if (OxygenSeconds > 0f)
            {
                state.Oxygen = Mathf.Max(0f, state.Oxygen - elapsed / OxygenSeconds);
            }
            else
            {
                state.Oxygen = 0f;
            }

            SendOxygen(hc, state, state.Oxygen);

            if (state.Oxygen > 0f) return;

            float damage = DrownDamagePerMinute * (elapsed / 60f);
            if (damage <= 0f) return;

            Player player = state.Player;
            if (player == null)
            {
                try
                {
                    if (hc.playerClient != null)
                    {
                        player = Server.GetServer().FindPlayer(hc.playerClient.userID);
                        state.Player = player;
                    }
                }
                catch
                {
                }
            }

            bool swimFlag = (character.stateFlags.flags & SwimFlag) != 0;
            float submergedSeconds = Time.time - state.SubmergedSince;

            WaterDamageEvent e = new WaterDamageEvent(player, depth, state.Oxygen,
                submergedSeconds, swimFlag, damage);

            Hooks.WaterDamage(e);

            if (e.Cancelled || e.DamageAmount <= 0f) return;

            TakeDamage.HurtSelf(hc.idMain, e.DamageAmount, null);
        }

        /// <summary>
        /// Whether the footstep broadcast should be suppressed for this player. A swimmer
        /// makes no footsteps, so other players should not hear any.
        /// </summary>
        public static bool ShouldSilenceFootsteps(HumanController hc)
        {
            if (!SilenceFootstepsInWater) return false;
            if (!hc) return false;

            Character character = hc.GetComponent<Character>();
            if (!character) return false;

            if (DepthBelowWater(character.transform.position) > 0f) return true;

            // Also honour the client's own swim state, so a player swimming in water the
            // server's single global waterline does not cover is still quiet.
            return (character.stateFlags.flags & SwimFlag) != 0;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Pushes an oxygen value to the owning client, throttled to OxygenSyncInterval.
        ///
        /// Sent on the injected Metabolism.rbOxy(float) RPC rather than by extending
        /// RecieveNetwork, because changing that signature would break every unpatched
        /// client and server. A new method is additive: a server that never calls it costs
        /// nothing, and a client without it is simply never sent one.
        /// </summary>
        private static void SendOxygen(HumanController hc, OxygenState state, float oxygen)
        {
            if (!SyncOxygenToClient) return;
            if (state == null || state.RpcUnsupported) return;
            if (Time.time < state.NextSync) return;

            state.NextSync = Time.time + OxygenSyncInterval;

            try
            {
                Metabolism metabolism = state.Metabolism;
                if (!metabolism && hc) metabolism = hc.GetComponent<Metabolism>();
                if (!metabolism) return;

                state.Metabolism = metabolism;

                uLink.NetworkView view = metabolism.networkView;
                if (view == null) return;

                view.RPC("rbOxy", view.owner, oxygen);
            }
            catch (Exception ex)
            {
                state.RpcUnsupported = true;
                Logger.LogWarning($"[WaterSystem] rbOxy push failed, disabling oxygen sync for this player: {ex.Message}");
            }
        }

        private static OxygenState GetOrCreate(Character character)
        {
            int key = character.GetInstanceID();

            OxygenState state;
            if (!States.TryGetValue(key, out state))
            {
                state = new OxygenState();
                state.LastSeen = Time.time;
                States[key] = state;
            }

            return state;
        }

        /// <summary>
        /// Refills a player's air. The elapsed time is passed in rather than read from
        /// Time.deltaTime so it matches the value the drain uses, otherwise the two run on
        /// different clocks and the server recovers at a different rate from the client.
        /// </summary>
        private static void Refill(Character character, float elapsed)
        {
            int key = character.GetInstanceID();

            OxygenState state;
            if (!States.TryGetValue(key, out state)) return;

            state.LastSeen = Time.time;
            state.SubmergedSince = -1f;

            if (OxygenSeconds <= 0f)
            {
                state.Oxygen = 1f;
            }
            else
            {
                state.Oxygen = Mathf.Min(1f, state.Oxygen + elapsed * OxygenRefillMultiplier / OxygenSeconds);
            }

            // One last push on the way back up so the client bar finishes refilling in step
            // with the server rather than only on its own prediction.
            SendOxygen(null, state, state.Oxygen);

            // Fully recovered, so stop tracking until they go under again.
            if (state.Oxygen >= 1f) States.Remove(key);
        }

        /// <summary>
        /// Drops entries nothing has touched recently. Covers disconnect, death and level
        /// change in one place, so none of them needs its own hook.
        /// </summary>
        private static void Prune()
        {
            if (Time.time < _nextPrune) return;
            _nextPrune = Time.time + 30f;

            if (States.Count == 0) return;

            List<int> stale = null;
            float cutoff = Time.time - StateTimeout;

            foreach (KeyValuePair<int, OxygenState> entry in States)
            {
                if (entry.Value.LastSeen >= cutoff) continue;
                if (stale == null) stale = new List<int>();
                stale.Add(entry.Key);
            }

            if (stale == null) return;
            for (int i = 0; i < stale.Count; i++)
            {
                States.Remove(stale[i]);
            }
        }
    }
}