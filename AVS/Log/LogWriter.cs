using AVS.Util;
using System;
using System.Collections.Generic;
using System.Linq;


namespace AVS.Log;

/// <summary>
/// Logging configuration, mostly used for material adaptation processes.
/// </summary>
public readonly struct LogWriter
{
    /// <summary>
    /// Logging prefix, used to identify the source of the log message.
    /// </summary>
    public string? Prefix { get; }

    /// <summary>
    /// Logging tags, used to identify the source of the log message.
    /// </summary>
    public string[]? Tags { get; }

    /// <summary>
    /// If true, log messages will include a timestamp.
    /// </summary>
    public bool IncludeTimestamp { get; }


    /// <summary>
    /// Creates a new logging definition.
    /// </summary>
    /// <param name="prefix">Logging prefix, used to identify the source of the log message.</param>
    /// <param name="tags">Logging tags, used to identify the source of the log message.</param>
    /// <param name="includeTimestamp">If true, log messages will include a timestamp.</param>
    public LogWriter(
        string? prefix,
        string[]? tags,
        bool includeTimestamp = true)
    {
        Prefix = prefix;
        Tags = tags;
        IncludeTimestamp = includeTimestamp;
    }

    /// <summary>
    /// Creates a new logging definition.
    /// </summary>
    /// <param name="prefix">Logging prefix, used to identify the source of the log message.</param>
    /// <param name="tags">Logging tags, used to identify the source of the log message.</param>
    public LogWriter(
        string? prefix,
        params string[]? tags)
        : this(prefix, tags, true)
    {
    }


    /// <summary>
    /// Default tags used for logging by AVS itself.
    /// </summary>
    internal static readonly string[] DefaultTags = new string[] { "AVS" };

    /// <summary>
    /// Default log writer instance for AVS itself.
    /// </summary>
    internal static readonly LogWriter Default = new(
        null,
        DefaultTags,
        true);


    private string MakeMessage(string msg, string? extraTag = null)
    {
        IEnumerable<string> tags = Tags ?? Array.Empty<string>();
        if (extraTag.IsNotNull() && extraTag != "")
            tags = tags.Append(extraTag);
        var tag = tags.Any() ? $"[{string.Join("] [", tags)}] " : string.Empty;
        var prefix = string.IsNullOrEmpty(Prefix) ? "" : $"{Prefix}: ";

        var dt = IncludeTimestamp ? DateTime.Now.ToString("HH:mm:ss.fff ") : "";

        return $"{dt}{tag}{prefix}{msg}";
    }



    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="msg">Message to log</param>
    public void Debug(string msg)
    {
        Logger.Log(MakeMessage(msg, "Debug"));
    }

    /// <summary>
    /// Logs a regular message.
    /// </summary>
    public void Write(string msg)
    {
        Logger.Log(MakeMessage(msg));
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void Warn(string msg)
    {
        Logger.Warn(MakeMessage(msg));
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public void Error(string msg)
    {
        Logger.Error(MakeMessage(msg));
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    public void Error(string msg, Exception? ex)
    {
        if (ex.IsNull())
        {
            Logger.Error(MakeMessage(msg));
            return;
        }

        Logger.LogException(MakeMessage(msg), ex);
    }

    /// <summary>
    /// Creates a new log writer with an additional prefix appended to end of the current prefix.
    /// If the current prefix is null, the new prefix will be used as the only prefix.
    /// Otherwise, the new prefix will be appended to the current prefix with a dot separator.
    /// </summary>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public LogWriter Prefixed(string prefix)
        => new(Prefix is null ? prefix : $"{Prefix}.{prefix}",
            Tags,
            IncludeTimestamp);

    /// <summary>
    /// Returns a new <see cref="LogWriter"/> instance with the specified tag added to the existing tags.
    /// </summary>
    /// <param name="tag">The tag to add to the <see cref="LogWriter"/>. Cannot be null.</param>
    /// <returns>A <see cref="LogWriter"/> instance with the specified tag included.  If the tag already exists, returns the
    /// current instance.</returns>
    public LogWriter Tag(string tag)
        => Tags.IsNotNull() && Array.IndexOf(Tags, tag) >= 0
            ? this
            : new LogWriter(Prefix,
                (Tags ?? Array.Empty<string>())
                .Append(tag)
                .ToArray(), IncludeTimestamp);
}