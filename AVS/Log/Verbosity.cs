namespace AVS.Log
{
    /// <summary>
    /// Logging verbosity levels.
    /// </summary>
    public enum Verbosity
    {
        /// <summary>
        /// Everything is logged
        /// </summary>
        Verbose,
        /// <summary>
        /// Non-debug logs are logged.
        /// Smart log contexts are shown only if contained messages are logged
        /// </summary>
        Regular,
        /// <summary>
        /// Only warnings and errors are logged
        /// </summary>
        WarningsAndErrorsOnly,
    }
}
