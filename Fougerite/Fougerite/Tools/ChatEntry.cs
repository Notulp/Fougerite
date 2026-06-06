using System;

namespace Fougerite.Tools
{
    /// <summary>
    /// Represents a chat message entry with associated user and timestamp information.
    /// </summary>
    public class ChatEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user in the Steam system.
        /// </summary>
        public ulong SteamID { get; set; }

        /// <summary>
        /// Gets or sets the username of the user associated with this chat entry.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the content of the chat message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the timestamp indicating when the chat entry was created.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}