using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Defines a filter for controlling whether debug-level log messages are processed.
    /// </summary>
    /// <remarks>Implementations of this interface can be used to determine whether debug-level log messages
    /// should be included in the logging output. This is typically used to enable or disable verbose logging
    /// dynamically based on application settings or runtime conditions.</remarks>
    public interface ILogFilter
    {
        /// <summary>
        /// Gets a value indicating whether debug-level logging is enabled.
        /// </summary>
        bool LogDebug { get; }
    }


    /// <summary>
    /// Provides logging, notification, and main menu alert utilities for the AVS mod.
    /// Integrates with BepInEx logging and Subnautica's in-game messaging systems.
    /// </summary>
    public static class Logger
    {
        #region BepInExLog
        /// <summary>
        /// The BepInEx log source used for outputting log messages.
        /// </summary>
        private static ManualLogSource? OutLog { get; set; }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void Log(string message)
        {
            OutLog?.LogInfo(message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void Warn(string message)
        {
            OutLog?.LogWarning(message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void Error(string message)
        {
            OutLog?.LogError(message);
        }

        /// <summary>
        /// Logs an exception with a prefix.
        /// </summary>
        public static void Exception(string prefix, System.Exception e)
        {
            OutLog?.LogError(prefix + e.Message);
            OutLog?.LogError(e.StackTrace);
        }

        /// <summary>
        /// Logs a debug message if the owner allows debug logging.
        /// </summary>
        public static void DebugLog(ILogFilter owner, string message)
        {
            if (owner?.LogDebug == true)
            {
                OutLog?.LogInfo($"[DebugLog]: {message}");
            }
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public static void DebugLog(string message)
        {
            OutLog?.LogInfo($"[DebugLog]: {message}");
        }

        /// <summary>
        /// Logs a warning and exception details, optionally outputs to screen.
        /// </summary>
        public static void WarnException(string message, System.Exception e, bool outputToScreen = false)
        {
            Warn(message);
            Warn(e.Message);
            Warn(e.StackTrace);
            if (outputToScreen)
            {
                ErrorMessage.AddWarning(message);
            }
        }

        /// <summary>
        /// Logs an error and exception details, optionally outputs to screen.
        /// </summary>
        public static void LogException(string message, System.Exception e, bool outputToScreen = false)
        {
            Error(message);
            Error(e.Message);
            Error(e.StackTrace);
            if (outputToScreen)
            {
                ErrorMessage.AddError(message);
            }
        }
        #endregion

        #region PdaNotifications
        /// <summary>
        /// Counter for generating unique notification IDs.
        /// </summary>
        private static int IDCounter = 65536;

        /// <summary>
        /// Stores message-to-ID mappings for notifications.
        /// </summary>
        private static readonly Dictionary<string, int> NoteIDsMemory = new Dictionary<string, int>();

        /// <summary>
        /// Gets a fresh, unused notification ID.
        /// </summary>
        public static int GetFreshID()
        {
            int returnID = IDCounter++;
            while (Subtitles.main.queue.messages.Select(x => x.id).Contains(returnID))
            {
                returnID = IDCounter++;
            }
            return returnID;
        }

        /// <summary>
        /// Shows a PDA notification with the specified message, duration, and delay.
        /// </summary>
        public static void PDANote(string msg, float duration = 1.4f, float delay = 0)
        {
            int id;
            if (NoteIDsMemory.ContainsKey(msg))
            {
                id = NoteIDsMemory[msg];
            }
            else
            {
                id = GetFreshID();
                NoteIDsMemory.Add(msg, id);
            }
            if (Subtitles.main.queue.messages.Select(x => x.id).Contains(id))
            {
                // don't replicate the message
            }
            else
            {
                Subtitles.main.queue.Add(id, new StringBuilder(msg), delay, duration);
            }
        }

        /// <summary>
        /// Outputs a warning message to the in-game screen.
        /// </summary>
        public static void Output(string msg, float time = 4f, int x = 500, int y = 0)
        {
            ErrorMessage.AddWarning(msg);
        }
        #endregion

        #region MainMenuLoopingMessages
        /// <summary>
        /// Stores main menu notifications to be displayed in a loop.
        /// </summary>
        private static readonly List<string> Notifications = new List<string>();

        /// <summary>
        /// Adds an error notification to the main menu loop and logs it.
        /// </summary>
        public static void LoopMainMenuError(string message, string prefix = "")
        {
            string result = $"<color=#FF0000>{prefix} Error: </color><color=#FFFF00>{message}</color>";
            Notifications.Add(result);
            Logger.Error(message);
        }

        /// <summary>
        /// Adds a warning notification to the main menu loop and logs it.
        /// </summary>
        public static void LoopMainMenuWarning(string message, string prefix = "")
        {
            string result = $"<color=#FF0000>{prefix} Warning: </color><color=#FFFF00>{message}</color>";
            Notifications.Add(result);
            Logger.Error(message);
        }

        /// <summary>
        /// Coroutine that displays main menu notifications in a loop until the player is loaded.
        /// </summary>
        internal static IEnumerator MakeAlerts()
        {
            yield return new WaitUntil(() => ErrorMessage.main != null);
            yield return new WaitForSeconds(1);
            yield return new WaitUntil(() => ErrorMessage.main != null);
            ErrorMessage eMain = ErrorMessage.main;
            float messageDuration = eMain.timeFlyIn + eMain.timeDelay + eMain.timeFadeOut + eMain.timeInvisible + 0.1f;
            while (Player.main == null)
            {
                foreach (string notif in Notifications)
                {
                    ErrorMessage.AddMessage(notif);
                    yield return new WaitForSeconds(1);
                }
                yield return new WaitForSeconds(messageDuration);
            }
        }

        internal static void Init(ManualLogSource logger)
        {
            OutLog = logger;
        }
        #endregion
    }
}
