using System;
using System.Threading;
using Fougerite.Events;

namespace Fougerite.Tools
{
    /// <summary>
    /// Represents the result of a plugin messaging operation.
    /// Encapsulates the response status and the associated event data.
    /// </summary>
    public class PluginMessageResult
    {
        private readonly PluginMessageResponse _status;
        private readonly PluginMessageEvent _event;
        
        /// <summary>
        /// Represents the result of a plugin message operation, including the status of the message delivery
        /// and the associated event data from the communication process.
        /// </summary>
        public PluginMessageResult(PluginMessageResponse status, PluginMessageEvent e)
        {
            _status = status;
            _event = e;
        }

        /// <summary>
        /// Represents the status of a plugin message response.
        /// Determines the outcome of an inter-plugin communication attempt.
        /// Possible values include states such as Success, TargetNotFound, TargetDisabled, Error, and Rejected.
        /// </summary>
        public PluginMessageResponse Status 
        {
            get
            {
                return _status;
            }
        }

        /// <summary>
        /// Gets the associated <see cref="PluginMessageEvent"/> instance that contains
        /// the details of the plugin message communication event.
        /// </summary>
        public PluginMessageEvent Event
        {
            get
            {
                return _event;
            }
        }
    }
    
    /// <summary>
    /// API for Inter-Plugin Communication.
    /// Provides synchronous and asynchronous methods for cross-module messaging.
    /// </summary>
    public static class PluginMessaging
    {
        /// <summary>
        /// Sends a message to a specific plugin and returns the encapsulated result.
        /// </summary>
        /// <param name="sender">The name of the plugin sending the message.</param>
        /// <param name="targetName">The name of the target plugin.</param>
        /// <param name="message">The object payload.</param>
        /// <returns>A PluginMessageResult containing the delivery status and event data.</returns>
        public static PluginMessageResult Send(string sender, string targetName, object message)
        {
            PluginMessageEvent e = new PluginMessageEvent(sender, targetName, message);
            PluginMessageResponse response = Hooks.PluginMessage(e);
            return new PluginMessageResult(response, e);
        }

        /// <summary>
        /// Sends a message asynchronously. The callback provides the encapsulated result on the Unity Main Thread.
        /// </summary>
        /// <param name="sender">The name of the plugin sending the message.</param>
        /// <param name="targetName">The name of the target plugin.</param>
        /// <param name="message">The object payload.</param>
        /// <param name="callback">Action executed when finished (PluginMessageResult).</param>
        /// <param name="runInThreadPool">If true, the Hook dispatch occurs on a background thread.</param>
        public static void SendAsync(string sender, string targetName, object message, Action<PluginMessageResult> callback, bool runInThreadPool = true)
        {
            if (runInThreadPool)
            {
                ThreadPool.QueueUserWorkItem((state) => 
                {
                    ExecuteInternal(sender, targetName, message, callback);
                });
            }
            else
            {
                ExecuteInternal(sender, targetName, message, callback);
            }
        }

        private static void ExecuteInternal(string sender, string target, object msg, Action<PluginMessageResult> callback)
        {
            PluginMessageEvent e = new PluginMessageEvent(sender, target, msg);
            PluginMessageResponse result = Hooks.PluginMessage(e);

            if (callback != null)
            {
                Loom.QueueOnMainThread(() =>
                {
                    callback(new PluginMessageResult(result, e));
                });
            }
        }
    }
}