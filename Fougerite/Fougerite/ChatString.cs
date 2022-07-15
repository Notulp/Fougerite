using System;

namespace Fougerite
{
    /// <summary>
    /// Basically On_Chat event's argument.
    /// </summary>
    public class ChatString
    {
        private string ntext;
        private string origText;
        private bool _cancelled = false;

        public ChatString(string str)
        {
            this.ntext = this.origText = str;
        }

        /// <summary>
        /// Returns the chat message the player originally typed.
        /// </summary>
        public string OriginalMessage
        {
            get
            {
                return origText;
            }
        }

        /// <summary>
        /// Returns the modified chat message or you may set the new chat message value.
        /// </summary>
        public string NewText
        {
            get
            {
                return ntext;
            }
            set
            {
                if (_cancelled)
                {
                    return;
                }
                // Rust doesn't like empty strings on chat
                if (string.IsNullOrEmpty(value))
                {
                    _cancelled = true;
                    ntext = "          ";
                }
                else
                {
                    ntext = value;
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
            return this.origText.Contains(str);
        }

        public static implicit operator string(ChatString cs)
        {
            return cs.origText;
        }

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
            return this.origText.Replace(find, replacement);
        }

        /// <summary>
        /// Cuts the string from the specified start character number.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns>Returns the new string.</returns>
        public string Substring(int start, int length)
        {
            return this.origText.Substring(start, length);
        }

        [Obsolete("Use OriginalMessage instead", false)]
        public override string ToString()
        {
            return this.origText;
        }
    }
}