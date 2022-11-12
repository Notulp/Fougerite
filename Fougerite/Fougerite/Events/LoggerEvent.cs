using System;

namespace Fougerite.Events
{
    public enum LoggerEventType
    {
        Log,
        LogRPC,
        [Obsolete("LogSpeed is no longer used in this event.", false)]
        LogSpeed,
        LogError,
        LogException,
        LogDebug,
        LogWarning,
        ChatLog
    }
    
    public class LoggerEvent
    {
        private readonly LoggerEventType _type;
        private readonly string _message;
        
        public LoggerEvent(LoggerEventType type, string message)
        {
            _message = message;
            _type = type;
        }

        /// <summary>
        /// Returns the type of the log event.
        /// </summary>
        public LoggerEventType Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Returns the message of the log event.
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
        }
    }
}