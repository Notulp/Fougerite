namespace Fougerite.Events
{
    public class ConsoleEvent
    {
        private bool _cancelled;
        
        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
        
        /// <summary>
        /// Gets if the console message was cancelled.
        /// </summary>
        public bool Cancelled
        {
            get
            {
                return _cancelled;
            }
        }
    }
}