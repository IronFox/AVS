namespace AVS
{
    public enum LightsStatus
    {
        OnHeadlightsOn,
        OnHeadlightsOff,
        OnInteriorLightsOn,
        OnInteriorLightsOff,
        OnFloodlightsOn,
        OnFloodlightsOff,
        OnNavLightsOn,
        OnNavLightsOff,
    }
    /// <summary>
    /// Listener for changes in vehicle lights status.
    /// </summary>
    public interface ILightsStatusListener
    {
        /// <summary>
        /// Triggered when the AVS controlled headlights are turned on.
        /// Called only if the vehicle has AVS controlled, declared headlights.
        /// </summary>
        void OnHeadlightsOn();
        /// <summary>
        /// Triggered when the AVS controlled headlights are turned off.
        /// Called only if the vehicle has AVS controlled, declared headlights.
        /// </summary>
        void OnHeadlightsOff();
        /// <summary>
        /// Triggered when the AVS controlled interior lights are turned on.
        /// Called only if the vehicle has AVS controlled, declared interior lights.
        /// </summary>
        void OnInteriorLightsOn();
        /// <summary>
        /// Triggered when the AVS controlled interior lights are turned off.
        /// Called only if the vehicle has AVS controlled, declared interior lights.
        /// </summary>
        void OnInteriorLightsOff();
        /// <summary>
        /// Triggered when the AVS controlled navigation lights are turned on.
        /// Called only if the vehicle has AVS controlled, declared navigation lights.
        /// </summary>
        void OnNavLightsOn();
        /// <summary>
        /// Triggered when the AVS controlled navigation lights are turned off.
        /// Called only if the vehicle has AVS controlled, declared navigation lights.
        /// </summary>
        void OnNavLightsOff();
        /// <summary>
        /// Triggered when the AVS controlled floodlights are turned on.
        /// Called only if the vehicle has AVS controlled, declared floodlights.
        /// </summary>
        void OnFloodlightsOn();
        /// <summary>
        /// Triggered when the AVS controlled floodlights are turned off.
        /// Called only if the vehicle has AVS controlled, declared floodlights.
        /// </summary>
        void OnFloodlightsOff();
    }
}
