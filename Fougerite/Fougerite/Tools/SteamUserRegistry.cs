using System.Collections.Generic;
using Fougerite.Concurrent;

namespace Fougerite.Tools
{
    /// <summary>
    /// Provides a registry system for managing and verifying Steam user IDs.
    /// </summary>
    /// <remarks>
    /// This static class utilizes a thread-safe dictionary to maintain a list of verified Steam user IDs.
    /// It allows adding, removing, and checking the presence of user IDs in the registry.
    /// </remarks>
    public static class SteamUserRegistry
    {
        /// <summary>
        /// A thread-safe dictionary that keeps track of verified users.
        /// The key represents the user's unique Steam ID (ulong),
        /// and the value is a SteamAppId indicating the verification status for the app id.
        /// </summary>
        private static readonly ConcurrentDictionary<ulong, SteamAppId> VerifiedUsers = new ConcurrentDictionary<ulong, SteamAppId>();

        /// <summary>
        /// Steam App IDs.
        /// </summary>
        public enum SteamAppId
        {
            None = -1,
            SpaceWars = 440,
            Rust = 252490,
        }

        /// <summary>
        /// Adds a user ID and corresponding app ID to the registry of verified users.
        /// </summary>
        /// <param name="userID">The unique identifier of the user to add to the verified user registry.</param>
        /// <param name="appId">The Steam application ID associated with the user.</param>
        public static void Add(ulong userID, SteamAppId appId)
        {
            VerifiedUsers[userID] = appId;
        }

        /// <summary>
        /// Removes a user from the verified users registry using their user ID.
        /// </summary>
        /// <param name="userID">The unique identifier of the user to remove.</param>
        public static bool Remove(ulong userID)
        {
            return VerifiedUsers.TryRemove(userID);
        }

        /// <summary>
        /// Determines whether the specified user ID exists in the verified users registry.
        /// </summary>
        /// <param name="userID">The unique identifier of the user to locate in the registry.</param>
        /// <return>
        /// True if the user ID exists in the verified users registry; otherwise, false.
        /// </return>
        public static bool Contains(ulong userID)
        {
            return VerifiedUsers.ContainsKey(userID);
        }

        /// <summary>
        /// Retrieves the Steam application ID associated with the specified user ID,
        /// if the user exists in the registry.
        /// </summary>
        /// <param name="userID">The unique Steam ID of the user to check in the registry.</param>
        /// <returns>The SteamAppId associated with the user if they exist in the registry; otherwise, SteamAppId.None.</returns>
        public static SteamAppId GetType(ulong userID)
        {
            return VerifiedUsers.ContainsKey(userID) ? VerifiedUsers[userID] : SteamAppId.None;
        }

        /// <summary>
        /// Retrieves a shallow copy of the registry of verified Steam user IDs.
        /// </summary>
        /// <returns>A dictionary containing a snapshot of the currently verified Steam user IDs.</returns>
        public static Dictionary<ulong, SteamAppId> GetShallowCopy()
        {
            return VerifiedUsers.GetShallowCopy();
        }
    }
}