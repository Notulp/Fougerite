using System;
using Fougerite.Tools;

namespace Fougerite
{
    /// <summary>
    /// Basically On_Chat event's argument.
    /// </summary>
    public class ChatString
    {
        private string _ntext;
        private readonly string _origText;
        private bool _cancelled;

        public ChatString(string str)
        {
            _ntext = _origText = str;
        }

        /// <summary>
        /// Returns the chat message the player originally typed.
        /// </summary>
        public string OriginalMessage
        {
            get
            {
                return _origText;
            }
        }

        /// <summary>
        /// Returns the modified chat message or you may set the new chat message value.
        /// </summary>
        public string NewText
        {
            get
            {
                return _ntext;
            }
            set
            {
                if (_cancelled)
                {
                    return;
                }
                // Rust doesn't like empty strings on chat
                if (value == null || string.IsNullOrEmpty(value) || value.IsNullOrWhiteSpace())
                {
                    _cancelled = true;
                    _ntext = "          ";
                }
                else
                {
                    _ntext = value;
                }
            }
        }

        /// <summary>
        /// Checks if a specific string is in the original text.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Returns true / false based on the outcome.</returns>
        public bool Contains(string str)
        {
            return _origText.Contains(str);
        }

        /// <summary>
        /// Implicitly converts a ChatString to its original text.
        /// </summary>
        /// <param name="cs">The ChatString to convert.</param>
        public static implicit operator string(ChatString cs)
        {
            return cs._origText;
        }

        /// <summary>
        /// Implicitly wraps a string in a new ChatString.
        /// </summary>
        /// <param name="str">The string to wrap.</param>
        public static implicit operator ChatString(string str)
        {
            return new ChatString(str);
        }

        /// <summary>
        /// Can replace a chain of characters to the specified value.
        /// </summary>
        /// <param name="find"></param>
        /// <param name="replacement"></param>
        /// <returns>Returns the new string.</returns>
        public string Replace(string find, string replacement)
        {
            return _origText.Replace(find, replacement);
        }

        /// <summary>
        /// Cuts the string from the specified start character number.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns>Returns the new string.</returns>
        public string Substring(int start, int length)
        {
            return _origText.Substring(start, length);
        }

        /// <summary>
        /// Returns the original chat message (same as <see cref="OriginalMessage"/>).
        /// </summary>
        [Obsolete("Use OriginalMessage instead", false)]
        public override string ToString()
        {
            return _origText;
        }
    }
}