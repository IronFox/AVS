namespace AVS
{
    internal enum PlayerStatus
    {
        OnPlayerEntry,
        OnPlayerExit,
        OnPilotBegin,
        OnPilotEnd
    }
    /// <summary>
    /// Listener interface for player-related events in the AVS vehicle.
    /// </summary>
    public interface IPlayerListener
    {
        /// <summary>
        /// The player has entered the vehicle.
        /// </summary>
        void OnPlayerEntry();
        /// <summary>
        /// The player has exited the vehicle.
        /// </summary>
        void OnPlayerExit();
        /// <summary>
        /// The player has begun piloting the vehicle.
        /// </summary>
        void OnPilotBegin();
        /// <summary>
        /// The player has ended piloting the vehicle.
        /// </summary>
        void OnPilotEnd();
    }
}
