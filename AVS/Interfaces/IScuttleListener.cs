namespace AVS
{
    /// <summary>
    /// Listeners for scuttling events.
    /// </summary>
    public interface IScuttleListener
    {
        /// <summary>
        /// Executed when the scuttle action is triggered.
        /// </summary>
        void OnScuttle();
        /// <summary>
        /// Executed when the unscuttle action is triggered.
        /// </summary>
        void OnUnscuttle();
    }
}
