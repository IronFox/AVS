namespace AVS.Interfaces
{
    /// <summary>
    /// Signals that an autopilot status change has occurred.
    /// </summary>
    public readonly struct AutopilotStatusChange
    {
        /// <summary>
        /// The previous autopilot status.
        /// </summary>
        public AutopilotStatus PreviousStatus { get; }
        /// <summary>
        /// The new autopilot status.
        /// </summary>
        public AutopilotStatus NewStatus { get; }

        /// <summary>
        /// Constructs a new autopilot status change.
        /// </summary>
        /// <param name="previousStatus">Previous status</param>
        /// <param name="newStatus">New status</param>
        public AutopilotStatusChange(AutopilotStatus previousStatus, AutopilotStatus newStatus)
        {
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
        }
    }

    /// <summary>
    /// Plain autopilot event
    /// </summary>
    public enum AutopilotEvent
    {
        /// <summary>
        /// Engine is powering up.
        /// </summary>
        PowerUp,
        /// <summary>
        /// Engine is powering down.
        /// </summary>
        PowerDown,
        /// <summary>
        /// The player has entered the vehicle.
        /// </summary>
        PlayerEntry,
        /// <summary>
        /// The player has exited the vehicle.
        /// </summary>
        PlayerExit,
    }


    /// <summary>
    /// Autopilot events that can be detected by the autopilot system.
    /// More severe statuses are generally greater than less severe statuses.
    /// </summary>
    public enum AutopilotStatus
    {
        /// <summary>
        /// Health status is now within safe limits.
        /// </summary>
        HealthSafe,
        /// <summary>
        /// Health is low, but not critical.
        /// </summary>
        HealthLow,
        /// <summary>
        /// Health is critical and requires immediate attention.
        /// </summary>
        HealthCritical,
        /// <summary>
        /// Power is within safe limits.
        /// </summary>
        PowerSafe,
        /// <summary>
        /// Power is low, but not critical.
        /// </summary>
        PowerLow,
        /// <summary>
        /// Power is critical and requires immediate attention.
        /// </summary>
        PowerCritical,
        /// <summary>
        /// Battery/ies is/are dead and require(s) replacement.
        /// </summary>
        PowerDead,
        /// <summary>
        /// Depth is within safe limits.
        /// </summary>
        DepthSafe,
        /// <summary>
        /// Depth is near crush depth, but not critical.
        /// </summary>
        DepthNearCrush,
        /// <summary>
        /// Depth is beyond crush depth and will cause damage.
        /// </summary>
        DepthBeyondCrush,
        /// <summary>
        /// No leviathan is nearby.
        /// </summary>
        LeviathanSafe,
        /// <summary>
        /// Leviathan is nearby.
        /// </summary>
        LeviathanNearby,

    }

    /// <summary>
    /// Listener for events detected by the autopilot system.
    /// </summary>
    public interface IAutopilotEventListener
    {
        /// <summary>
        /// Signals that the specified event was detected by the autopilot system.
        /// </summary>
        /// <param name="autopilotEvent">Event that was detected</param>
        void Signal(AutopilotEvent autopilotEvent);
        /// <summary>
        /// Signals that a status change has occurred in the autopilot system.
        /// </summary>
        /// <param name="statusChange">Change that was detected</param>
        void Signal(AutopilotStatusChange statusChange);
    }
}
