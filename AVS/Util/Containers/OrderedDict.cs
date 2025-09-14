using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AVS.Util.Containers
{
    /// <summary>
    /// A dictionary that maintains insertion order of keys.
    /// </summary>
    /// <typeparam name="K">Key type</typeparam>
    /// <typeparam name="V">Value type</typeparam>
    public class OrderedDict<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V>
    {

        private readonly Dictionary<K, V> _dict;
        private readonly List<K> _keys;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDict{K, V}"/> class.
        /// </summary>
        /// <remarks>This constructor creates an empty ordered dictionary, which maintains the insertion
        /// order of keys. Keys and values can be added after initialization using the appropriate methods or
        /// properties.</remarks>
        public OrderedDict()
        {
            _dict = new Dictionary<K, V>();
            _keys = new List<K>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderedDict{K, V}"/> class with the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the dictionary can contain. Must be greater than or equal to 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is less than 0.</exception>
        public OrderedDict(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _dict = new Dictionary<K, V>(capacity);
            _keys = new List<K>(capacity);
        }

        /// <inheritdoc/>
        public int Count => _keys.Count;
        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public V this[K key]
        {
            get => _dict[key];
            set
            {
                if (_dict.ContainsKey(key))
                {
                    _keys.Remove(key);
                    _dict[key] = value;
                    _keys.Add(key);
                }
                else
                {
                    _dict[key] = value;
                    _keys.Add(key);
                }
            }
        }

        /// <inheritdoc/>
        public ICollection<K> Keys => new ReadOnlyCollection<K>(_keys);
        IEnumerable<K> IReadOnlyDictionary<K, V>.Keys => _keys;

        /// <inheritdoc/>
        public ICollection<V> Values
        {
            get
            {
                var list = new List<V>(_keys.Count);
                for (int i = 0; i < _keys.Count; i++)
                    list.Add(_dict[_keys[i]]);
                return new ReadOnlyCollection<V>(list);
            }
        }
        /// <inheritdoc/>
        IEnumerable<V> IReadOnlyDictionary<K, V>.Values
        {
            get
            {
                for (int i = 0; i < _keys.Count; i++)
                    yield return _dict[_keys[i]];
            }
        }

        /// <inheritdoc/>
        public void Add(K key, V value)
        {
            if (_dict.ContainsKey(key))
                throw new ArgumentException("An element with the same key already exists in the OrderedDict.", nameof(key));
            _dict.Add(key, value);
            _keys.Add(key);
        }

        /// <inheritdoc/>
        public bool TryAdd(K key, V value)
        {
            if (_dict.ContainsKey(key)) return false;
            _dict.Add(key, value);
            _keys.Add(key);
            return true;
        }

        /// <inheritdoc/>
        public bool ContainsKey(K key) => _dict.ContainsKey(key);

        /// <inheritdoc/>
        public bool TryGetValue(K key, out V value) => _dict.TryGetValue(key, out value);

        /// <inheritdoc/>
        public bool Remove(K key)
        {
            if (!_dict.Remove(key)) return false;
            int idx = _keys.IndexOf(key);
            if (idx >= 0) _keys.RemoveAt(idx);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _dict.Clear();
            _keys.Clear();
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                var k = _keys[i];
                yield return new KeyValuePair<K, V>(k, _dict[k]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ICollection<KeyValuePair<K,V>> explicit implementations

        void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item)
        {
            V value;
            if (!_dict.TryGetValue(item.Key, out value)) return false;
            return EqualityComparer<V>.Default.Equals(value, item.Value);
        }

        void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < Count) throw new ArgumentException("Insufficient space in target array.");

            for (int i = 0; i < _keys.Count; i++)
            {
                var k = _keys[i];
                array[arrayIndex + i] = new KeyValuePair<K, V>(k, _dict[k]);
            }
        }

        bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
        {
            V value;
            if (!_dict.TryGetValue(item.Key, out value)) return false;
            if (!EqualityComparer<V>.Default.Equals(value, item.Value)) return false;
            return Remove(item.Key);
        }

        /// <summary>
        /// Determines whether the value associated with the specified key is of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to check against the value associated with the key. Must derive from <typeparamref name="V"/>.</typeparam>
        /// <param name="key">The key whose associated value is to be checked.</param>
        /// <returns><see langword="true"/> if the value associated with the specified key exists and is of type <typeparamref
        /// name="T"/>; otherwise, <see langword="false"/>.</returns>
        public bool ValueIs<T>(K key) where T : V
        {
            if (!_dict.TryGetValue(key, out var value))
                return false;
            return value is T;
        }

        /// <summary>
        /// Determines whether the specified key exists in the dictionary and is associated with the specified value.
        /// </summary>
        /// <remarks>This method uses the default equality comparer for the value type to determine
        /// equality.</remarks>
        /// <param name="key">The key to locate in the dictionary.</param>
        /// <param name="value">The value to compare against the value associated with the specified key.</param>
        /// <returns><see langword="true"/> if the dictionary contains the specified key and the associated value equals the
        /// specified value;  otherwise, <see langword="false"/>.</returns>
        public bool ValueEquals(K key, V value)
        {
            if (!_dict.TryGetValue(key, out var v))
                return false;
            return EqualityComparer<V>.Default.Equals(v, value);
        }
    }
}
