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
        /// If true, input material variables will be logged.
        /// </summary>
        public bool LogMaterialVariables { get; }
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
        public Logging(bool logMaterialVariables, bool logMaterialChanges, string? prefix, bool includeTimestamp, bool logExtraSteps)
        {
            LogMaterialVariables = logMaterialVariables;
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
            logMaterialVariables: false,
            logMaterialChanges: false,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: true
            );

        /// <summary>
        /// Muted logging configuration.
        /// </summary>
        public static Logging Silent { get; } = new Logging(
            logMaterialVariables: false,
            logMaterialChanges: false,
            prefix: DefaultPrefix,
            includeTimestamp: true,
            logExtraSteps: false
            );

        /// <summary>
        /// Verbose logging configuration.
        /// </summary>
        public static Logging Verbose { get; } = new Logging(
            logMaterialVariables: true,
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
            if (value == null)
                return "<null>";
            return value.ToString();
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
                Logger.Log(MakeMessage($"Setting {type} {name} ({ValueToString(old)} -> {ValueToString(value)}) on {m.NiceName()}"));
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
        public void LogMaterialVariableData(
            ShaderPropertyType? type,
            string name,
            string dataAsString,
            Material m)
        {
            if (LogMaterialVariables)
                Logger.Log(MakeMessage($"{m.NiceName()} {type} variable {name} imported as {dataAsString}"));
        }

        /// <summary>
        /// Logs a material property set operation.
        /// </summary>
        /// <typeparam name="T">C# type being logged</typeparam>
        /// <param name="name">Field name being updated</param>
        /// <param name="data">Recognized data</param>
        /// <param name="m">Material being logged</param>
        public void LogMaterialVariableData<T>(
            string name,
            T data,
            Material m)
        {
            if (!LogMaterialVariables)
                return;
            switch (data)
            {
                case float f:
                    LogMaterialVariableData(ShaderPropertyType.Float, name, f.ToString(CultureInfo.InvariantCulture), m);
                    break;
                case int i:
                    LogMaterialVariableData(null, name, i.ToString(), m);
                    break;
                case Vector4 v:
                    LogMaterialVariableData(ShaderPropertyType.Vector, name, v.ToString(), m);
                    break;
                case Vector3 v:
                    LogMaterialVariableData(ShaderPropertyType.Vector, name, v.ToString(), m);
                    break;
                case Vector2 v:
                    LogMaterialVariableData(ShaderPropertyType.Vector, name, v.ToString(), m);
                    break;
                case Color c:
                    LogMaterialVariableData(ShaderPropertyType.Color, name, c.ToString(), m);
                    break;
                case Texture t:
                    LogMaterialVariableData(ShaderPropertyType.Texture, name, t.NiceName(), m);
                    break;
                default:
                    LogMaterialVariableData(null, name, "<unknown>", m);
                    break;
            }
        }
    }

}
