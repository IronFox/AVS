using FMOD;
using System;

namespace AVS.Audio
{
    /// <summary>
    /// Caught fatal FMOD result.
    /// </summary>
    public class FModException : Exception
    {
        /// <summary>
        /// The result that caused the exception.
        /// </summary>
        public RESULT Result { get; }


        /// <summary>
        /// Represents an exception that occurs when an FMOD operation fails.
        /// </summary>
        /// <param name="message">The error message that describes the exception.</param>
        /// <param name="result">The FMOD result code associated with the exception.</param>
        public FModException(string message, RESULT result) : base(message)
        {
            this.Result = result;
        }
    }
}
