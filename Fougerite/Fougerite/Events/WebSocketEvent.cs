namespace Fougerite.Events
{
    /// <summary>
    /// Represents an event triggered during a WebSocket interaction in the context of a plugin.
    /// </summary>
    /// <remarks>
    /// The WebSocketEvent provides information about a particular WebSocket message, such as the
    /// originating plugin, the socket identifier, and the message content. This class is primarily
    /// used in the event system to pass data to relevant event handlers and plugin operations.
    /// </remarks>
    public class WebSocketEvent
    {
        /// <summary>
        /// Represents the name of the plugin associated with the WebSocket event.
        /// </summary>
        private readonly string _pluginName;

        /// <summary>
        /// Represents the unique identifier for the WebSocket connection associated with the event.
        /// </summary>
        private readonly string _socketId;

        /// <summary>
        /// Represents the content of the message received or sent through the WebSocket event.
        /// </summary>
        private readonly string _message;

        /// <summary>
        /// Represents the error message if the WebSocket event is an error or a dirty close.
        /// </summary>
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the WebSocketEvent class.
        /// </summary>
        /// <param name="pluginName">The name of the plugin associated with the event.</param>
        /// <param name="socketId">The unique identifier of the WebSocket connection.</param>
        /// <param name="message">The message payload received or transmitted.</param>
        /// <param name="errorMessage">The error message if an error occurred.</param>
        public WebSocketEvent(string pluginName, string socketId, string message, string errorMessage = null)
        {
            _pluginName = pluginName;
            _socketId = socketId;
            _message = message;
            _errorMessage = errorMessage;
        }

        /// <summary>
        /// Gets the name of the plugin associated with the WebSocket event.
        /// </summary>
        public string PluginName
        {
            get { return _pluginName; }
        }

        /// <summary>
        /// Gets the unique identifier associated with the WebSocket connection.
        /// </summary>
        public string SocketId
        {
            get { return _socketId; }
        }

        /// <summary>
        /// Gets the message data associated with the WebSocket event.
        /// </summary>
        public string Message
        {
            get { return _message; }
        }

        /// <summary>
        /// Gets the error message associated with a failed WebSocket operation or connection close.
        /// Might be null.
        /// </summary>
        public string ErrorMessage
        {
            get { return _errorMessage; }
        }
    }
}