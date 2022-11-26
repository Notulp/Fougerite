namespace Fougerite.Events
{
    public enum CommandRestrictionType
    {
        Command,
        ConsoleCommand
    }

    public enum CommandRestrictionScale
    {
        Global,
        SpecificPlayer
    }
    
    /// <summary>
    /// This is a non-optimal solution to solve the detection of command restriction changes in Fougerite.
    /// In Fougerite since 2014 all plugins use the OnCommand event, where we should have moved to registering the
    /// commands on an API level instead. Since that would require re-working all the plugins, and would take too much effort
    /// and also we are too late to do that I'm proposing this solution to atleast be able to track of the restriction changes
    /// in Fougerite.
    /// </summary>
    public class CommandRestrictionEvent
    {
        private readonly Player _player;
        private readonly string _command;
        private readonly CommandRestrictionType _commandRestrictionType;
        private readonly CommandRestrictionScale _commandRestrictionScale;
        private readonly bool _isBeingRestricted;
        private bool _cancelled;

        public CommandRestrictionEvent(Player player, string command, CommandRestrictionType commandRestrictionType,
            CommandRestrictionScale commandRestrictionScale, bool isBeingRestricted)
        {
            _player = player;
            _command = command;
            _commandRestrictionType = commandRestrictionType;
            _commandRestrictionScale = commandRestrictionScale;
            _isBeingRestricted = isBeingRestricted;
        }

        /// <summary>
        /// Gets the player who is being restricted/unrestricted.
        /// CommandRestrictionScale must be SpecificPlayer, if It's global
        /// the Player property will return null.
        /// </summary>
        public Player Player
        {
            get
            {
                return _player;
            }
        }

        /// <summary>
        /// The command that is being restricted/unrestricted.
        /// </summary>
        public string Command
        {
            get
            {
                return _command;
            }
        }

        /// <summary>
        /// Gets whether the command is being restricted or unrestricted.
        /// </summary>
        public bool IsBeingRestricted
        {
            get
            {
                return _isBeingRestricted;
            }
        }

        /// <summary>
        /// Gets the type of command console/regular chat command.
        /// </summary>
        public CommandRestrictionType CommandRestrictionType
        {
            get
            {
                return _commandRestrictionType;
            }
        }

        /// <summary>
        /// Gets the scale of the restriction
        /// If Global It means all players will have the effect applied.
        /// If SpecificPlayer then only the one who is in the event.
        /// </summary>
        public CommandRestrictionScale CommandRestrictionScale
        {
            get
            {
                return _commandRestrictionScale;
            }
        }

        /// <summary>
        /// Checks if the event was cancelled.
        /// </summary>
        public bool Cancelled
        {
            get
            {
                return _cancelled;
            }
        }

        /// <summary>
        /// Cancels the event.
        /// </summary>
        public void Cancel()
        {
            _cancelled = true;
        }
    }
}