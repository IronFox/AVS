using AVS.Util;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.Log
{
    /// <summary>
    /// Material adaptation logging configuration.
    /// </summary>
    public readonly struct MaterialLog
    {
        /// <summary>
        /// Constructs a new material adaptation logging configuration.
        /// </summary>
        /// <param name="prefix">Optional logging prefix, used to identify the source of the log message.</param>
        /// <param name="tags">Optional logging tags, used to identify the source of the log message.</param>
        /// <param name="includeTimestamp">If true, log messages will include a timestamp.</param>
        /// <param name="logMaterialVariables">If true, input material variables will be logged.</param>
        /// <param name="logMaterialChanges">If true material property changes will be logged.</param>
        /// <param name="logExtraSteps">If true, extra steps of the material adaptation process will be logged.</param>
        public MaterialLog(
            bool logMaterialVariables = true,
            bool logMaterialChanges = true,
            string? prefix = null,
            string[]? tags = null,
            bool includeTimestamp = true,
            bool logExtraSteps = false
        )
        {
            Writer = new LogWriter(
                prefix: prefix,
                tags: tags,
                includeTimestamp: includeTimestamp
            );
            LogMaterialVariables = logMaterialVariables;
            LogMaterialChanges = logMaterialChanges;
            LogExtraSteps = logExtraSteps;
        }

        /// <summary>
        /// Internal log writer used for material adaptation logging.
        /// </summary>
        public LogWriter Writer { get; }


        /// <summary>
        /// If true material property changes will be logged.
        /// </summary>
        public bool LogMaterialChanges { get; }
        /// <summary>
        /// If true, input material variables will be logged.
        /// </summary>
        public bool LogMaterialVariables { get; }
        /// <summary>
        /// If true, extra steps of the material adaptation process will be logged.
        /// </summary>
        public bool LogExtraSteps { get; }


        /// <summary>
        /// Default logging prefix used when fixing materials.
        /// </summary>
        public const string MaterialAdaptationTag = "Material Fix";


        /// <summary>
        /// Default logging configuration for material adaptation.
        /// </summary>
        public static MaterialLog Default { get; } = new MaterialLog(
            logMaterialVariables: false,
            logMaterialChanges: false,
            prefix: null,
            tags: new string[] { MaterialAdaptationTag },
            includeTimestamp: true,
            logExtraSteps: true
            );

        /// <summary>
        /// Muted logging configuration for material adaptation.
        /// </summary>
        public static MaterialLog Silent { get; } = new MaterialLog(
            logMaterialVariables: false,
            logMaterialChanges: false,
            prefix: null,
            tags: new string[] { MaterialAdaptationTag },
            includeTimestamp: true,
            logExtraSteps: false
            );


        /// <summary>
        /// Verbose logging configuration for material adaptation.
        /// </summary>
        public static MaterialLog Verbose { get; } = new MaterialLog(
            logMaterialVariables: true,
            logMaterialChanges: true,
            prefix: null,
            tags: new string[] { MaterialAdaptationTag },
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
            Writer.Write(msg);
        }


        /// <summary>
        /// Logs a material change message.
        /// If <see cref="LogMaterialChanges"/> is false, this method does nothing.
        /// </summary>
        public void LogMaterialChange(string msg)
        {
            if (!LogMaterialChanges)
                return;
            Writer.Write(msg);
        }
        /// <summary>
        /// Logs a material change message using a function to generate the message.
        /// If <see cref="LogMaterialChanges"/> is false, this method does nothing.
        /// </summary>
        public void LogMaterialChange(Func<string> msg)
        {
            if (!LogMaterialChanges)
                return;
            Writer.Write(msg());
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
        /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
        public void LogMaterialVariableSet<T>(
            ShaderPropertyType type,
            string name,
            T old,
            T value,
            Material m,
            string? materialName)
        {
            if (LogMaterialChanges)
                Writer.Write($"Setting {type} {name} ({ValueToString(old)} -> {ValueToString(value)}) on {materialName ?? m.NiceName()}");
        }

        /// <summary>
        /// Logs a material property value.
        /// </summary>
        /// <param name="type">Unity type being updated</param>
        /// <param name="name">Field name being updated</param>
        /// <param name="dataAsString">The current value as string</param>
        /// <param name="m">Material affected</param>
        /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
        public void LogMaterialVariableData(
            ShaderPropertyType? type,
            string name,
            string dataAsString,
            Material m,
            string? materialName)
        {
            if (LogMaterialVariables)
                Writer.Write($"{materialName ?? m.NiceName()} [{type}] {name} := {dataAsString}");
        }

        /// <summary>
        /// Logs a material property set operation.
        /// </summary>
        /// <typeparam name="T">C# type being logged</typeparam>
        /// <param name="name">Field name being updated</param>
        /// <param name="data">Recognized data</param>
        /// <param name="m">Material being logged</param>
        /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
        public void LogMaterialVariableData<T>(
            string name,
            T data,
            Material m,
            string? materialName)
        {
            if (!LogMaterialVariables)
                return;

            switch (data)
            {
                case float f:
                    LogMaterialVariableData(ShaderPropertyType.Float, name, f.ToString(CultureInfo.InvariantCulture), m, materialName);
                    break;
                case int i:
                    LogMaterialVariableData(ShaderPropertyType.Float, name, i.ToString(), m, materialName);
                    break;
                case Vector4 v:
                    LogMaterialVariableData(ShaderPropertyType.Vector, name, v.ToString(), m, materialName);
                    break;
                case Vector3 v:
                    LogMaterialVariableData(ShaderPropertyType.Vector, name, v.ToString(), m, materialName);
                    break;
                case Vector2 v:
                    LogMaterialVariableData(ShaderPropertyType.Vector, name, v.ToString(), m, materialName);
                    break;
                case Color c:
                    LogMaterialVariableData(ShaderPropertyType.Color, name, c.ToString(), m, materialName);
                    break;
                case Texture t:
                    LogMaterialVariableData(ShaderPropertyType.Texture, name, t.NiceName(), m, materialName);
                    break;
                case null:
                    LogMaterialVariableData(DecodeType(typeof(T)), name, "<null>", m, materialName);
                    break;
                default:
                    LogMaterialVariableData(DecodeType(typeof(T)), name, "<unknown>", m, materialName);
                    break;
            }
        }

        private static ShaderPropertyType? DecodeType(Type type)
        {
            if (type == typeof(float)
                || type == typeof(double)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                )
                return ShaderPropertyType.Float;
            if (type == typeof(Vector4)
                || type == typeof(Vector3)
                || type == typeof(Vector2))
                return ShaderPropertyType.Vector;
            if (type == typeof(Color))
                return ShaderPropertyType.Color;
            if (type == typeof(Texture)
                || type == typeof(Texture2D)
                || type == typeof(Texture3D)
                || type == typeof(Cubemap))
                return ShaderPropertyType.Texture;
            return null;
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public void Warn(string v)
            => Writer.Warn(v);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public void Error(string v)
            => Writer.Error(v);
    }
}
