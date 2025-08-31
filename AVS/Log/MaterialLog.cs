using AVS.Util;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.Log;

/// <summary>
/// Material adaptation logging configuration.
/// </summary>
public readonly struct MaterialLog
{
    /// <summary>
    /// Constructs a new material adaptation logging configuration.
    /// </summary>
    /// <param name="tags">Optional logging tags, used to identify the source of the log message.</param>
    /// <param name="logMaterialVariables">If true, input material variables will be logged.</param>
    /// <param name="logMaterialChanges">If true material property changes will be logged.</param>
    /// <param name="logExtraSteps">If true, extra steps of the material adaptation process will be logged.</param>
    public MaterialLog(
        bool logMaterialVariables = true,
        bool logMaterialChanges = true,
        string[]? tags = null,
        bool logExtraSteps = false
    )
    {
        Tags = tags;
        LogMaterialVariables = logMaterialVariables;
        LogMaterialChanges = logMaterialChanges;
        LogExtraSteps = logExtraSteps;
    }


    /// <summary>
    /// Creates a new scopes smart log for material adaptation logging.
    /// </summary>
    /// <param name="rmc"></param>
    /// <returns></returns>
    public SmartLog NewLog(RootModController rmc)
        => new SmartLog(rmc, Domain.AVS, frameDelta: 1, tags: Tags, forceLazy: true);


    /// <summary>
    /// If true material property changes will be logged.
    /// </summary>
    public bool LogMaterialChanges { get; }

    /// <summary>
    /// Default tags associated with this logging configuration.
    /// </summary>
    public string[]? Tags { get; }

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
    public static MaterialLog Default { get; } = new(
        logMaterialVariables: false,
        logMaterialChanges: false,
        tags: [MaterialAdaptationTag],
        logExtraSteps: true
    );

    /// <summary>
    /// Muted logging configuration for material adaptation.
    /// </summary>
    public static MaterialLog Silent { get; } = new(
        logMaterialVariables: false,
        logMaterialChanges: false,
        tags: [MaterialAdaptationTag],
        logExtraSteps: false
    );


    /// <summary>
    /// Verbose logging configuration for material adaptation.
    /// </summary>
    public static MaterialLog Verbose { get; } = new(
        logMaterialVariables: true,
        logMaterialChanges: true,
        tags: [MaterialAdaptationTag],
        logExtraSteps: true
    );

    /// <summary>
    /// Logs an extra step in the material adaptation process.
    /// If <see cref="LogExtraSteps"/> is false, this method does nothing.
    /// </summary>
    public void LogExtraStep(SmartLog log, string msg)
    {
        if (!LogExtraSteps)
            return;
        log.Write(msg);
    }


    /// <summary>
    /// Logs a material change message.
    /// If <see cref="LogMaterialChanges"/> is false, this method does nothing.
    /// </summary>
    public void LogMaterialChange(SmartLog log, string msg)
    {
        if (!LogMaterialChanges)
            return;
        log.Write(msg);
    }

    /// <summary>
    /// Logs a material change message using a function to generate the message.
    /// If <see cref="LogMaterialChanges"/> is false, this method does nothing.
    /// </summary>
    public void LogMaterialChange(SmartLog log, Func<string> msg)
    {
        if (!LogMaterialChanges)
            return;
        log.Write(msg());
    }

    private string ValueToString<T>(T value)
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
    /// <param name="log">The log to write to</param>
    /// <param name="type">Unity type being updated</param>
    /// <param name="name">Field name being updated</param>
    /// <param name="old">Old value</param>
    /// <param name="value">New value</param>
    /// <param name="m">Material affected</param>
    /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
    public void LogMaterialVariableSet<T>(
        SmartLog log,
        ShaderPropertyType type,
        string name,
        T old,
        T value,
        Material m,
        string? materialName)
    {
        if (LogMaterialChanges)
            log.Write(
                $"Setting {type} {name} ({ValueToString(old)} -> {ValueToString(value)}) on {materialName ?? m.NiceName()}");
    }

    /// <summary>
    /// Logs a material property value.
    /// </summary>
    /// <param name="log">The log to write to</param>
    /// <param name="type">Unity type being updated</param>
    /// <param name="name">Field name being updated</param>
    /// <param name="dataAsString">The current value as string</param>
    /// <param name="m">Material affected</param>
    /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
    public void LogMaterialVariableData(
        SmartLog log,
        ShaderPropertyType? type,
        string name,
        string dataAsString,
        Material m,
        string? materialName)
    {
        if (LogMaterialVariables)
            log.Write($"{materialName ?? m.NiceName()} [{type}] {name} := {dataAsString}");
    }

    /// <summary>
    /// Logs a material property set operation.
    /// </summary>
    /// <typeparam name="T">C# type being logged</typeparam>
    /// <param name="log">The log to write to</param>
    /// <param name="name">Field name being updated</param>
    /// <param name="data">Recognized data</param>
    /// <param name="m">Material being logged</param>
    /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
    public void LogMaterialVariableData<T>(
        SmartLog log,
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
                LogMaterialVariableData(log, ShaderPropertyType.Float, name, f.ToString(CultureInfo.InvariantCulture), m,
                    materialName);
                break;
            case int i:
                LogMaterialVariableData(log, ShaderPropertyType.Float, name, i.ToString(), m, materialName);
                break;
            case Vector4 v:
                LogMaterialVariableData(log, ShaderPropertyType.Vector, name, v.ToString(), m, materialName);
                break;
            case Vector3 v:
                LogMaterialVariableData(log, ShaderPropertyType.Vector, name, v.ToString(), m, materialName);
                break;
            case Vector2 v:
                LogMaterialVariableData(log, ShaderPropertyType.Vector, name, v.ToString(), m, materialName);
                break;
            case Color c:
                LogMaterialVariableData(log, ShaderPropertyType.Color, name, c.ToString(), m, materialName);
                break;
            case Texture t:
                LogMaterialVariableData(log, ShaderPropertyType.Texture, name, t.NiceName(), m, materialName);
                break;
            case null:
                LogMaterialVariableData(log, DecodeType(typeof(T)), name, "<null>", m, materialName);
                break;
            default:
                LogMaterialVariableData(log, DecodeType(typeof(T)), name, "<unknown>", m, materialName);
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

}