using AVS.Interfaces;

namespace AVS.Util
{
    /// <summary>
    /// Represents a handle for managing the lifecycle of a coroutine.
    /// </summary>
    /// <remarks>This interface provides functionality to check the running state of a coroutine  and to stop
    /// it when necessary. It is typically used to control and monitor  coroutines in systems that support asynchronous
    /// or time-based operations.</remarks>
    public interface ICoroutineHandle : INullTestableType
    {
        /// <summary>
        /// Gets a value indicating whether the process or operation is currently running.
        /// The result will turn false if the coroutine is either manually stopped or has completed its execution.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Stops the current operation or process, if it is running.
        /// </summary>
        /// <remarks>This method halts the operation gracefully. Ensure that any necessary cleanup or
        /// state-saving  is performed before calling this method, as the operation will not resume after being
        /// stopped.</remarks>
        void Stop();

        /// <summary>
        /// Returns a yieldable object that can be used to wait until the coroutine has completed its execution.
        /// </summary>
        /// <returns>Yieldable object</returns>
        object? WaitUntilDone();
    }
}
