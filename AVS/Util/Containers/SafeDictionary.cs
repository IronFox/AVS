using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AVS.Util.Containers
{
    /// <summary>
    /// A dictionary that allows adding and removing items while iterating over it.
    /// Count may return stale values. Items added during the iteration may or may not be part of the iteration itself.
    /// Items removed during the iteration that have not yet been iterated over will not be part of the iteration.
    /// Pending values are applied after the last iterator was disposed. Iterators must be disposed of properly to ensure this.
    /// </summary>
    /// <typeparam name="TKey">The key type used in the dictionary</typeparam>
    /// <typeparam name="TValue">The value type used in the dictionary</typeparam>
    public class SafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _dict;
        private readonly Dictionary<TKey, TValue> _pendingAdds;
        private readonly Dictionary<TKey, TValue> _pendingUpdates;
        private readonly HashSet<TKey> _pendingRemovals;
        private int _iterationDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class with the default capacity
        /// and comparer.
        /// </summary>
        /// <remarks>This constructor creates an empty dictionary with the default initial capacity and
        /// uses the default equality comparer for the key type.</remarks>
        public SafeDictionary()
            : this(0, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class with the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the dictionary can contain. Must be greater than or equal to 0.</param>
        public SafeDictionary(int capacity)
            : this(capacity, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class with the specified
        /// comparer.
        /// </summary>
        /// <remarks>This constructor allows you to specify a custom equality comparer for key
        /// comparisons, which can be useful for scenarios requiring case-insensitive or culture-specific
        /// comparisons.</remarks>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing keys, or <see langword="null"/>
        /// to use the default equality comparer for the type of the key.</param>
        public SafeDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class with the specified
        /// initial capacity and an optional key comparer.
        /// </summary>
        /// <remarks>This constructor initializes the internal data structures with the specified capacity
        /// and comparer.  It ensures that pending additions, updates, and removals are tracked separately to support
        /// safe concurrent operations.</remarks>
        /// <param name="capacity">The initial number of elements that the dictionary can contain before resizing is required. Must be greater
        /// than or equal to 0.</param>
        /// <param name="comparer">An optional <see cref="IEqualityComparer{T}"/> implementation to use for comparing keys.  If <see
        /// langword="null"/>, the default equality comparer for the type of the key is used.</param>
        public SafeDictionary(int capacity, IEqualityComparer<TKey>? comparer)
        {
            _dict = new Dictionary<TKey, TValue>(capacity, comparer);
            _pendingAdds = new Dictionary<TKey, TValue>(comparer);
            _pendingUpdates = new Dictionary<TKey, TValue>(comparer);
            _pendingRemovals = new HashSet<TKey>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class  using the specified
        /// source dictionary.
        /// </summary>
        /// <remarks>If the <paramref name="source"/> dictionary contains duplicate keys, an exception may
        /// be thrown  depending on the implementation of the <see cref="SafeDictionary{TKey, TValue}"/>. Ensure that 
        /// the source dictionary does not contain null keys or values if the implementation does not support
        /// them.</remarks>
        /// <param name="source">The dictionary whose elements are copied to the new <see cref="SafeDictionary{TKey, TValue}"/>.  The keys
        /// and values from the source dictionary are added to the new instance.</param>
        public SafeDictionary(IDictionary<TKey, TValue> source)
            : this(source, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeDictionary{TKey, TValue}"/> class with the specified source
        /// dictionary and an optional key comparer.
        /// </summary>
        /// <param name="source">The dictionary whose elements are copied to initialize the <see cref="SafeDictionary{TKey, TValue}"/>.
        /// Cannot be <see langword="null"/>.</param>
        /// <param name="comparer">An optional equality comparer to use for comparing keys. If <see langword="null"/>, the default equality
        /// comparer for the key type is used.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public SafeDictionary(IDictionary<TKey, TValue> source, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            _dict = new Dictionary<TKey, TValue>(source, comparer);
            _pendingAdds = new Dictionary<TKey, TValue>(comparer);
            _pendingUpdates = new Dictionary<TKey, TValue>(comparer);
            _pendingRemovals = new HashSet<TKey>(comparer);
        }

        private bool IsIterating => _iterationDepth > 0;

        private void ApplyPendingIfPossible()
        {
            if (IsIterating) return;

            // Apply removals first
            if (_pendingRemovals.Count > 0)
            {
                foreach (var k in _pendingRemovals)
                {
                    _dict.Remove(k);
                }
                _pendingRemovals.Clear();
            }

            // Apply updates to existing keys
            if (_pendingUpdates.Count > 0)
            {
                foreach (var kv in _pendingUpdates)
                {
                    if (_dict.ContainsKey(kv.Key))
                        _dict[kv.Key] = kv.Value;
                }
                _pendingUpdates.Clear();
            }

            // Apply adds
            if (_pendingAdds.Count > 0)
            {
                foreach (var kv in _pendingAdds)
                {
                    _dict.Add(kv.Key, kv.Value);
                }
                _pendingAdds.Clear();
            }
        }

        private bool ExistsLogical(TKey key) =>
            !_pendingRemovals.Contains(key) &&
            (_pendingAdds.ContainsKey(key) || _dict.ContainsKey(key));

        private TValue GetLogicalValue(TKey key)
        {
            if (_pendingRemovals.Contains(key))
                throw new KeyNotFoundException();
            if (_pendingAdds.TryGetValue(key, out var vAdd))
                return vAdd;
            if (_pendingUpdates.TryGetValue(key, out var vUpd) && _dict.ContainsKey(key))
                return vUpd;
            return _dict[key];
        }


        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get
            {
                if (!ExistsLogical(key))
                    throw new KeyNotFoundException();
                return GetLogicalValue(key);
            }
            set
            {
                if (ExistsLogical(key))
                {
                    // Update existing
                    if (IsIterating)
                    {
                        if (_pendingAdds.ContainsKey(key))
                        {
                            _pendingAdds[key] = value;
                        }
                        else
                        {
                            // Only store update if key truly exists in underlying
                            _pendingUpdates[key] = value;
                        }
                    }
                    else
                    {
                        _dict[key] = value;
                    }
                }
                else
                {
                    // Treat as Add
                    Add(key, value);
                }
            }
        }


        private KeyCollection? _cachedKeys;
        private ValueCollection? _cachedValues;

        /// <inheritdoc/>
        public ICollection<TKey> Keys => _cachedKeys ??= new KeyCollection(this);
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Select(kv => kv.Key);

        /// <inheritdoc/>
        public ICollection<TValue> Values => _cachedValues ??= new ValueCollection(this);
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Select(kv => kv.Value);

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                // "May return stale values" – we compute an approximate logical count.
                int count = _dict.Count;
                count += _pendingAdds.Count;
                count -= _pendingRemovals.Count;
                // Pending updates do not affect count.
                return count;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (ExistsLogical(key))
                throw new ArgumentException("An item with the same key has already been added.", nameof(key));

            if (IsIterating)
            {
                // If it was scheduled for removal, cancel that removal.
                _pendingRemovals.Remove(key);
                _pendingAdds.Add(key, value);
            }
            else
            {
                _dict.Add(key, value);
            }
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return ExistsLogical(key);
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (!ExistsLogical(key)) return false;

            if (IsIterating)
            {
                if (_pendingAdds.Remove(key))
                {
                    // Added & removed within iteration => net no-op
                    _pendingUpdates.Remove(key);
                    return true;
                }

                _pendingUpdates.Remove(key);
                _pendingRemovals.Add(key);
            }
            else
            {
                if (_dict.Remove(key))
                    return true;
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!ExistsLogical(key))
            {
                value = default!;
                return false;
            }
            value = GetLogicalValue(key);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            if (IsIterating)
            {
                // Mark everything for removal
                foreach (var k in _dict.Keys)
                {
                    if (!_pendingRemovals.Contains(k))
                        _pendingRemovals.Add(k);
                }
                _pendingAdds.Clear();
                _pendingUpdates.Clear();
            }
            else
            {
                _dict.Clear();
                _pendingAdds.Clear();
                _pendingUpdates.Clear();
                _pendingRemovals.Clear();
            }
        }

        /// <inheritdoc/>
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (!ExistsLogical(item.Key)) return false;
            var val = GetLogicalValue(item.Key);
            return EqualityComparer<TValue>.Default.Equals(val, item.Value);
        }

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Destination array is not large enough.");

            int i = arrayIndex;
            foreach (var kv in this)
                array[i++] = kv;
        }

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Contains(item)) return false;
            return Remove(item.Key);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void EnterIteration() => _iterationDepth++;
        private void ExitIteration()
        {
            _iterationDepth--;
            if (_iterationDepth == 0)
            {
                ApplyPendingIfPossible();
            }
        }

        private sealed class Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly SafeDictionary<TKey, TValue> _owner;
            private Dictionary<TKey, TValue>.Enumerator _inner;
            private KeyValuePair<TKey, TValue> _current;
            private bool _disposed;

            public Enumerator(SafeDictionary<TKey, TValue> owner)
            {
                _owner = owner;
                _owner.EnterIteration();
                _inner = owner._dict.GetEnumerator();
                _current = default;
            }

            public KeyValuePair<TKey, TValue> Current => _current;
            object IEnumerator.Current => _current;

            public bool MoveNext()
            {
                while (_inner.MoveNext())
                {
                    var kv = _inner.Current;

                    // Skip if pending removal
                    if (_owner._pendingRemovals.Contains(kv.Key))
                        continue;

                    TValue value = kv.Value;
                    if (_owner._pendingUpdates.TryGetValue(kv.Key, out var updated))
                        value = updated;

                    _current = new KeyValuePair<TKey, TValue>(kv.Key, value);
                    return true;
                }
                _current = default;
                return false;
            }

            public void Reset()
            {
                Dispose();
                throw new NotSupportedException("Reset is not supported for SafeDictionary enumerator.");
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _inner.Dispose();
                _owner.ExitIteration();
            }
        }

        private sealed class KeyCollection : ICollection<TKey>
        {
            private readonly SafeDictionary<TKey, TValue> _owner;
            public KeyCollection(SafeDictionary<TKey, TValue> owner) => _owner = owner;

            public int Count => _owner.Count;
            public bool IsReadOnly => true;

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array == null) throw new ArgumentNullException(nameof(array));
                foreach (var k in this)
                {
                    if (arrayIndex >= array.Length) throw new ArgumentException("Destination array not large enough.");
                    array[arrayIndex++] = k;
                }
            }

            public bool Contains(TKey item) => _owner.ContainsKey(item);

            public IEnumerator<TKey> GetEnumerator()
            {
                foreach (var kv in _owner)
                    yield return kv.Key;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            #region Unsupported
            void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();
            void ICollection<TKey>.Clear() => throw new NotSupportedException();
            bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();
            #endregion
        }

        private sealed class ValueCollection : ICollection<TValue>
        {
            private readonly SafeDictionary<TKey, TValue> _owner;
            public ValueCollection(SafeDictionary<TKey, TValue> owner) => _owner = owner;

            public int Count => _owner.Count;
            public bool IsReadOnly => true;

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array == null) throw new ArgumentNullException(nameof(array));
                foreach (var v in this)
                {
                    if (arrayIndex >= array.Length) throw new ArgumentException("Destination array not large enough.");
                    array[arrayIndex++] = v;
                }
            }

            public bool Contains(TValue item)
            {
                var cmp = EqualityComparer<TValue>.Default;
                foreach (var v in this)
                {
                    if (cmp.Equals(v, item)) return true;
                }
                return false;
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                foreach (var kv in _owner)
                    yield return kv.Value;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            #region Unsupported
            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();
            void ICollection<TValue>.Clear() => throw new NotSupportedException();
            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();
            #endregion
        }
    }
}
