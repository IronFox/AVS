using AVS.Interfaces;
using AVS.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AVS.Log
{
    /// <summary>
    /// Represents a hierarchical, disposable logging context that supports tagging, verbosity control,  and
    /// interruptible operations. This class is designed to facilitate structured logging within  nested or asynchronous
    /// operations.
    /// </summary>
    public class SmartLog : IDisposable, INullTestableType
    {
        private SmartLog? Previous { get; set; }
        private SmartLog? Parent { get; }
        private static SmartLog? Current { get; set; }
        /// <summary>
        /// The owning root mod controller instance.
        /// </summary>
        public RootModController RMC { get; }
        private bool IsInterruptable { get; }

        /// <summary>
        /// The recursive depth of this log context. Root context has depth 0.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// The active logging domain. Inherited only if null
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// The name of this log context, derived from the calling method.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// True if this instance has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        private bool HasStarted { get; set; }

        private OrderedSet<string> TagSet { get; } = [];

        /// <summary>
        /// The tags associated with this log context.
        /// </summary>
        public IReadOnlyList<string> Tags => TagSet.ToList();

        /// <summary>
        /// Gets the date and time when the context was created.
        /// </summary>
        public DateTime StartTime { get; } = DateTime.Now;

        private static Dictionary<int, SmartLog> InterruptableIndexes { get; } = [];
        private int InterruptableIndex { get; }

        //private bool IsInterruptableSelfOrChild { get; }
        private SmartLog? InterruptableAncestor { get; }

        /// <summary>
        /// Creates a new disposable log context. The new context becomes the current context.
        /// </summary>
        /// <param name="rmc">The owning root mod controller instance.</param>
        /// <param name="frameDelta">Optional additional stack frame delta to apply when determining the name of this context.</param>
        /// <param name="isInterruptable">If true, this context can be interrupted by asynchronous operations and later resumed.</param>
        /// <param name="tags">Optional additional tags to associate with this context to be set at creation time.</param>
        /// <param name="nameOverride">Optional name to use instead of the calling method's name.</param>
        /// <param name="domain">The domain name associated with the log. Typically AVS or Mod.</param>
        /// <param name="forceLazy">If true, logging of the start message is always deferred until the first actual log message.</param>
        public SmartLog(RootModController rmc, string? domain, int frameDelta = 0, bool isInterruptable = false, IReadOnlyList<string>? tags = null, string? nameOverride = null, bool forceLazy = false)
        {
            Previous = Current;
            Parent = Current;
            Depth = (Parent.IsNotNull() ? Parent.Depth + 1 : 0);
            if (Parent.IsNotNull())
                TagSet.AddRange(Parent.Tags);

            var sf = new StackFrame(1 + frameDelta, false);
            var m = sf.GetMethod();
            //m.GetParameters();
            Domain = domain ?? Parent?.Domain ?? "";
            Name = nameOverride ?? (m.DeclaringType?.Name + "." + m.Name);
            if (isInterruptable)
            {
                Depth = 0;
                //Tags.Add(rmc.ModName);
                //TagSet.Add("CR");
                //Name = $"{Name} (coroutine exec)";
            }
            if (tags.IsNotNull())
                TagSet.AddRange(tags);
            RMC = rmc;
            Current = this;
            IsInterruptable = isInterruptable;
            InterruptableAncestor = isInterruptable ? this : Parent?.InterruptableAncestor;
            //IsInterruptableSelfOrChild = isInterruptable || (Parent?.IsInterruptableSelfOrChild ?? false);

            if (isInterruptable)
            {
                int idx = 0;
                while (InterruptableIndexes.ContainsKey(idx))
                    idx++;
                InterruptableIndexes[idx] = this;
                InterruptableIndex = idx;
            }
            else
            {
                InterruptableIndex = Parent?.InterruptableIndex ?? 0;
            }

            if (rmc.LogVerbosity == Verbosity.Verbose && !forceLazy)
            {
                SignalLog();
            }
        }

        internal void SignalLog()
        {
            if (HasStarted)
                return;
            HasStarted = true;

            try
            {
                string prefix = "> ";
                if (IsInterruptable)
                    prefix = ">>";
                Logger.Log(MakeMessage(Name, depthOverride: Depth - 1, dtOverride: StartTime, isStart: true, prefix: prefix));
            }
            catch (Exception ex)
            {
                Logger.Exception("SmartLog.SignalLog failed", ex);
            }
        }

        private string MakeMessage(string message, int? depthOverride = null, string? extraTag = null, DateTime? dtOverride = null, bool isStart = false, string? prefix = null)
        {
            var depth = depthOverride ?? Depth;

            IEnumerable<string> tags = Tags;
            if (!string.IsNullOrEmpty(extraTag))
                tags = tags.Append(extraTag!);
            var tag = tags.Any() ? $"[{string.Join("] [", tags)}] " : string.Empty;

            var dt = (dtOverride ?? DateTime.Now).ToString("HH:mm:ss.fff ");

            int tagLen = tag.Length;
            string padding = new(' ', Math.Max(0, 24 - tagLen));


            var coroutineChannel = " ".Repeat(8);

            foreach (var pair in InterruptableIndexes)
            {
                if (pair.Value.HasStarted)
                    if (coroutineChannel.Length > pair.Key)
                        coroutineChannel[pair.Key] = '│';
            }

            if (InterruptableAncestor.IsNotNull())
            {
                if (coroutineChannel.Length > InterruptableAncestor.InterruptableIndex)
                {
                    coroutineChannel[InterruptableAncestor.InterruptableIndex] = '├';
                    if (depthOverride is not null && InterruptableAncestor == this)
                        if (isStart)
                            coroutineChannel[InterruptableAncestor.InterruptableIndex] = '┌';
                        else
                            coroutineChannel[InterruptableAncestor.InterruptableIndex] = '└';
                }
            }

            var indent = "  ".Repeat(depth + 1);
            return $"[{Domain}] {dt}{new string(coroutineChannel)} {new string(indent)}{prefix}{tag}{message}";

        }

        /// <summary>
        /// Checks if this log context is a child of the specified other context.
        /// </summary>
        /// <param name="other">Potential parent (or this)</param>
        /// <returns>True if child of or identical to <paramref name="other"/></returns>
        public bool IsChildOf(SmartLog? other)
        {
            if (other.IsNull())
                return false;
            var p = this;
            while (p.IsNotNull())
            {
                if (p == other)
                    return true;
                p = p.Parent;
            }
            return false;
        }

        internal SmartLog? FirstNonDisposedParent
        {
            get
            {
                var p = this;
                while (p.IsNotNull() && p.IsDisposed)
                    p = p.Parent;
                return p;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Name} (Depth {Depth}, Tags: [{string.Join(", ", Tags)}], IsDisposed: {IsDisposed}, IsInterruptable: {IsInterruptable})";

        private void Dbg(string message)
        {
            Logger.Log($"<{message}>");
        }

        internal void Interrupt()
        {
            if (!IsInterruptable)
                throw new InvalidOperationException("This SmartLog is not interruptable.");
            if (Current.IsNotNull() && Current.IsChildOf(this))
            {
                var p = Previous?.FirstNonDisposedParent;
                //Dbg($"Interrupting {this} from {Current} to {p}");
                Current = p;
            }
            else
                throw new InvalidOperationException("Cannot interrupt a SmartLog that is not current or a parent of current.");
        }

        internal void Resume()
        {
            //Dbg($"Resuming {this} from {Current}");
            Previous = Current;
            Current = this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            if (IsInterruptable)
            {
                InterruptableIndexes.Remove(InterruptableIndex);
            }
            if (HasStarted)
            {
                string prefix = "< ";
                if (IsInterruptable)
                    prefix = "<<";

                Logger.Log(MakeMessage(Name, Depth - 1, prefix: prefix));
            }
            if (Current?.IsChildOf(this) == true)
                Current = Previous?.FirstNonDisposedParent;
        }


        /// <summary>
        /// Writes a message to the log.
        /// </summary>
        /// <remarks>This method formats the provided message before logging it. Ensure that the message
        /// is meaningful and adheres to any logging conventions used in your application.</remarks>
        /// <param name="message">The message to be logged. Cannot be null or empty.</param>
        public void Write(string message)
        {
            if (RMC.LogVerbosity == Verbosity.WarningsAndErrorsOnly)
                return;
            try
            {
                SignalLog();
                Logger.Log(MakeMessage(message));
            }
            catch (Exception ex)
            {
                Logger.Exception("SmartLog.SignalLog failed", ex);
            }

        }

        /// <summary>
        /// Logs a warning message to the configured logging system.
        /// </summary>
        /// <remarks>This method signals the logging system before logging the warning message. Ensure
        /// that the logging system is properly configured to handle warning-level messages.</remarks>
        /// <param name="message">The warning message to log. Cannot be null or empty.</param>
        public void Warn(string message)
        {
            try
            {
                SignalLog();
                Logger.Warn(MakeMessage(message));
            }
            catch (Exception ex)
            {
                Logger.Exception("SmartLog.SignalLog failed", ex);
            }

        }

        /// <summary>
        /// Logs an error message, optionally including details of an exception.
        /// </summary>
        /// <remarks>This method logs the error message and, if provided, the exception details. It is
        /// intended for use in scenarios where error information needs to be recorded for diagnostics.</remarks>
        /// <param name="message">The error message to log. This value cannot be <see langword="null"/> or empty.</param>
        /// <param name="ex">An optional exception containing additional details about the error. If <see langword="null"/>, only the
        /// message is logged.</param>
        public void Error(string message, Exception? ex = null)
        {
            try
            {
                SignalLog();
                if (ex.IsNotNull())
                    Logger.Exception(MakeMessage(message), ex);
                else
                    Logger.Error(MakeMessage(message));
            }
            catch (Exception ex2)
            {
                Logger.Exception("SmartLog.SignalLog failed", ex2);
            }
        }

        /// <summary>
        /// Logs a debug message if the current log verbosity level is set to <see cref="Verbosity.Verbose"/>.
        /// </summary>
        /// <remarks>The method will not log the message if the log verbosity level is not set to <see
        /// cref="Verbosity.Verbose"/>.</remarks>
        /// <param name="message">The debug message to log. This should provide detailed information useful for debugging purposes.</param>
        public void Debug(string message)
        {
            if (RMC.LogVerbosity != Verbosity.Verbose)
                return;
            try
            {
                SignalLog();
                Logger.Log(MakeMessage(message, extraTag: "DBG"));
            }
            catch (Exception ex)
            {
                Logger.Exception("SmartLog.SignalLog failed", ex);
            }

        }

        internal static SmartLog ForAVS(RootModController mainPatcher, params string[] tags)
            => new SmartLog(mainPatcher, "AVS", 1, tags: tags);


        /// <summary>
        /// Creates a new instance of <see cref="SmartLog"/> configured for the specified domain.
        /// </summary>
        /// <param name="mainPatcher">The root mod controller instance used to initialize the log.</param>
        /// <param name="tags">Optional additional tags to associate with this log context.</param>
        /// <param name="domain">The domain name associated with the log. Defaults to "Mod" if not specified.</param>
        /// <returns>A <see cref="SmartLog"/> instance configured with the specified domain.</returns>
        public static SmartLog For(RootModController mainPatcher, string domain = "Mod", params string[] tags)
            => new SmartLog(mainPatcher, domain: domain, 1, tags: tags);
    }
}
