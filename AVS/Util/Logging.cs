using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.Util
{
    /// <summary>
    /// Logging configuration, mostly used for material adaptation processes.
    /// </summary>
    public readonly struct Logging
    {
        /// <summary>
        /// If true material property changes will be logged.
        /// </summary>
        public bool LogMaterialChanges { get; }
        /// <summary>
        /// Logging prefix, used to identify the source of the log message.
        /// </summary>
        public string? Prefix { get; }

        /// <summary>
        /// If true, log messages will include a timestamp.
        /// </summary>
        public bool IncludeTimestamp { get; }
        /// <summary>
        /// If true, extra steps of the material adaptation process will be logged.
        /// </summary>
        public bool LogExtraSteps { get; }

        /// <summary>
        /// Creates a new logging definition.
        /// </summary>
        /// <param name="logMaterialChanges">If true material property changes will be logged.</param>
        /// <param name="prefix">Logging prefix, used to identify the source of the log message.</param>
        /// <param name="includeTimestamp">If true, log messages will include a timestamp.</param>
        /// <param name="logExtraSteps">If true, extra steps of the material adaptation process will be logged.</param>
        public Logging(bool logMaterialChanges, string? prefix, bool includeTimestamp, bool logExtraSteps)
        {
            LogMaterialChanges = logMaterialChanges;
            Prefix = prefix;
            IncludeTimestamp = includeTimestamp;
            LogExtraSteps = logExtraSteps;
        }

        /// <summary>
        /// Default logging prefix used when fixing materials.
        /// </summary>
        public const string DefaultPrefix = "Material Fix";

        /// <summary>
        /// Default logging configuration.
        /// </summary>
        public static Logging Default { get; } = new Logging(
            logMaterialChanges: false,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: true
            );

        /// <summary>
        /// Muted logging configuration.
        /// </summary>
        public static Logging Silent { get; } = new Logging(
            logMaterialChanges: false,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: false
            );

        /// <summary>
        /// Verbose logging configuration.
        /// </summary>
        public static Logging Verbose { get; } = new Logging(
            logMaterialChanges: true,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: true
            );

        /// <summary>
        /// Logs an extra step in the material adaptation process.
        /// If <see cref="LogExtraSteps"/> is false, this method does nothing.
        /// </summary>
        public void LogExtraStep(string msg)
        {
            if (!LogExtraSteps)
                return;
            Logger.Log(MakeMessage(msg));
        }

        private string MakeMessage(string msg)
        {
            if (!string.IsNullOrEmpty(Prefix))
            {
                if (IncludeTimestamp)
                    return $"{DateTime.Now:HH:mm:ss.fff} [Mod] {Prefix}: {msg}";
                return $"{Prefix}: {msg}";
            }
            else
            {
                if (IncludeTimestamp)
                    return $"{DateTime.Now:HH:mm:ss.fff} [Mod] {msg}";
                return msg;
            }
        }

        /// <summary>
        /// Logs a regular message.
        /// </summary>
        public void LogMessage(string msg)
        {
            Logger.Log(MakeMessage(msg));
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public void LogWarning(string msg)
        {
            Logger.Warn(MakeMessage(msg));
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public void LogError(string msg)
        {
            Logger.Error(MakeMessage(msg));
        }

        /// <summary>
        /// Logs a material change message.
        /// If <see cref="LogMaterialChanges"/> is false, this method does nothing.
        /// </summary>
        public void LogMaterialChange(string msg)
        {
            if (!LogMaterialChanges)
                return;
            Logger.Log(MakeMessage(msg));
        }
        /// <summary>
        /// Logs a material change message using a function to generate the message.
        /// If <see cref="LogMaterialChanges"/> is false, this method does nothing.
        /// </summary>
        public void LogMaterialChange(Func<string> msg)
        {
            if (!LogMaterialChanges)
                return;
            Logger.Log(MakeMessage(msg()));
        }

        private string? ValueToString<T>(T value)
        {
            if (value is float f0)
                return f0.ToString(CultureInfo.InvariantCulture);
            return value?.ToString();
        }

        /// <summary>
        /// Logs a material property set operation.
        /// </summary>
        /// <typeparam name="T">C# type being updated</typeparam>
        /// <param name="type">Unity type being updated</param>
        /// <param name="name">Field name being updated</param>
        /// <param name="old">Old value</param>
        /// <param name="value">New value</param>
        /// <param name="m">Material affected</param>
        public void LogMaterialVariableSet<T>(
            ShaderPropertyType type,
            string name,
            T old,
            T value,
            Material m)
        {
            if (LogMaterialChanges)
                Logger.Log(MakeMessage($"Setting {type} {name} ({ValueToString(old)} -> {ValueToString(value)}) on material {m.NiceName()}"));
        }
    }

}
