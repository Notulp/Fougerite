namespace Fougerite.Events
{
    /// <summary>
    /// Result states for Inter-Plugin Communication attempts.
    /// </summary>
    public enum PluginMessageResponse
    {
        /// <summary> Message was delivered successfully to the target plugin. </summary>
        Success,
        /// <summary> The target plugin name does not exist in the PluginLoader. </summary>
        TargetNotFound,
        /// <summary> The target plugin is known but is currently not in a 'Loaded' state. </summary>
        TargetDisabled,
        /// <summary> An internal exception occurred while trying to dispatch the message. </summary>
        Error,
        /// <summary> The message was delivered, but the target plugin explicitly rejected it via Cancel(). </summary>
        Rejected
    }
    
    /// <summary>
    /// Event arguments for communication between two plugins. 
    /// This container carries the payload and allows the receiver to return data or cancel the request.
    /// </summary>
    public sealed class PluginMessageEvent
    {
        private readonly string _sender;
        private readonly string _receiver;
        private readonly object _message;
        private object _response;
        private bool _cancelled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginMessageEvent"/> class.
        /// </summary>
        /// <param name="sender">The name of the plugin sending the message.</param>
        /// <param name="receiver">The name of the plugin intended to receive the message.</param>
        /// <param name="message">The object payload.</param>
        public PluginMessageEvent(string sender, string receiver, object message)
        {
            _sender = sender;
            _receiver = receiver;
            _message = message;
        }

        /// <summary>
        /// Gets the name of the plugin that initiated this message.
        /// </summary>
        public string SenderName
        {
            get { return _sender; }
        }

        /// <summary>
        /// Gets the name of the plugin that this message is directed to.
        /// </summary>
        public string ReceiverName
        {
            get { return _receiver; }
        }

        /// <summary>
        /// Gets the primary payload sent by the sender.
        /// </summary>
        public object Message
        {
            get { return _message; }
        }

        /// <summary>
        /// Gets or sets the data returned by the receiving plugin.
        /// </summary>
        public object Response
        {
            get { return _response; }
            set { _response = value; }
        }

        /// <summary>
        /// Returns true if the message was rejected/cancelled by the receiver.
        /// </summary>
        public bool Cancelled
        {
            get { return _cancelled; }
        }

        /// <summary>
        /// Rejects the message. The sender will receive a 'Rejected' response status.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
    }
}