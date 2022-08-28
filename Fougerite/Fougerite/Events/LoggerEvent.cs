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

        public LoggerEventType Type
        {
            get
            {
                return _type;
            }
        }

        public string Message
        {
            get
            {
                return _message;
            }
        }
    }
}