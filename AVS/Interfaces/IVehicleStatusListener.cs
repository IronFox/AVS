namespace AVS
{
    /// <summary>
    /// Listener interface for vehicle status changes.
    /// </summary>
    public interface IVehicleStatusListener
    {
        /// <summary>
        /// The vehicle has taken damage
        /// </summary>
        void OnTakeDamage();
        /// <summary>
        /// The vehicle has detected a nearby leviathan.
        /// </summary>
        void OnNearbyLeviathan();
    }
}
