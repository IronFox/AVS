using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AVS.Crafting
{
    /// <summary>
    /// Path to a tab or module in the crafting interface.
    /// </summary>
    /// <typeparam name="T">The type of element in the path. Typically <see cref="string"/> or <see cref="CraftingNode"/></typeparam>
    public readonly struct Path<T> : IEquatable<Path<T>>, IEnumerable<T> where T : notnull
    {
        private readonly T[]? segments;

        /// <summary>
        /// The segments of the crafting path.
        /// </summary>
        public T[] Segments => segments ?? Array.Empty<T>();

        /// <summary>
        /// Gets a value indicating whether the collection of segments is empty.
        /// </summary>
        public bool IsEmpty => segments == null || segments.Length == 0;

        /// <summary>
        /// Gets the number of segments in the collection.
        /// </summary>
        public int Length => segments?.Length ?? 0;

        /// <summary>
        /// An empty crafting path, representing the root or no specific path.
        /// </summary>
        public static Path<T> Empty { get; } = default;
        /// <summary>
        /// Gets the last segment of the path. An empty path returns an empty string.
        /// </summary>
        [MaybeNull]
        public T Last => IsEmpty ? default : segments![segments.Length - 1];

        /// <summary>
        /// Gets the segment preceding the last segment in the collection.
        /// </summary>
        [MaybeNull]
        public T Previous => Length < 1 ? default : segments![segments.Length - 2];

        /// <summary>
        /// Gets the parent path, which is the path without the last segment.
        /// </summary>
        public Path<T> Parent => IsEmpty ? Empty : new Path<T>(segments.Take(Length - 1));

        /// <summary>
        /// Gets an enumerable collection of <see cref="Path{T}"/> objects representing the ancestors of the
        /// current path.
        /// </summary>
        public IEnumerable<Path<T>> Ancestors
        {
            get
            {
                if (IsEmpty)
                {
                    yield break;
                }
                for (int i = 0; i < Length; i++)
                {
                    yield return new Path<T>(segments.Take(i + 1));
                }
            }
        }

        /// <summary>
        /// Constructs a new <see cref="Path{T}"/> with the specified segments.
        /// </summary>
        public Path(IEnumerable<T> segments)
        {
            if (segments.Any())
                this.segments = segments.ToArray();
            else
                this.segments = null;
        }
        /// <summary>
        /// Constructs a new <see cref="Path{T}"/> with the specified segments.
        /// </summary>
        public Path(params T[] segments)
        {
            if (segments.Length == 0)
            {
                this.segments = null;
                return;
            }
            this.segments = segments;
        }

        /// <summary>
        /// Appends a new segment to the crafting path.
        /// </summary>
        /// <param name="segment">Segment to append. Must not be empty (after trim) and must not contain / characters</param>
        /// <returns>New crafting path with the given segment appended</returns>
        public Path<T> Append(T segment)
        {
            if (Equals(segment, default(T)))
            {
                throw new ArgumentException("Segment cannot be empty.", nameof(segment));
            }

            if (IsEmpty)
            {
                return new Path<T>(segment);
            }
            else
            {
                var newSegments = new T[segments!.Length + 1];
                Array.Copy(segments, newSegments, segments.Length);
                newSegments[segments.Length] = segment;
                return new Path<T>(newSegments);
            }
        }


        /// <inheritdoc/>
        public override string ToString() => string.Join("/", Segments);

        /// <inheritdoc/>
        public bool Equals(Path<T> other)
        {
            if (IsEmpty && other.IsEmpty)
            {
                return true;
            }
            if (IsEmpty || other.IsEmpty)
            {
                return false;
            }
            if (segments!.Length != other.segments!.Length)
            {
                return false;
            }
            for (int i = 0; i < segments.Length; i++)
            {
                if (!segments[i].Equals(other.segments[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <inheritdoc/>
        public static bool operator ==(Path<T> left, Path<T> right)
        {
            return left.Equals(right);
        }
        /// <inheritdoc/>
        public static bool operator !=(Path<T> left, Path<T> right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Path<T> other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }
            int hash = 17;
            foreach (var segment in Segments)
            {
                hash = hash * 31 + (segment?.GetHashCode() ?? 0);
            }
            return hash;
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            if (IsEmpty)
            {
                yield break;
            }
            foreach (var segment in segments!)
            {
                yield return segment;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Concatenates a specified segment to the end of the given <see cref="Path{T}"/>.
        /// </summary>
        /// <param name="path">The <see cref="Path{T}"/> to which the segment will be appended.</param>
        /// <param name="segment">The segment to append to the <see cref="Path{T}"/>. Cannot be null or empty.</param>
        /// <returns>A new <see cref="Path{T}"/> instance with the segment appended.</returns>
        public static Path<T> operator +(Path<T> path, T segment)
        {
            return path.Append(segment);
        }

    }
}
