using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AVS.Util;

/// <summary>
/// Represents a collection of unique elements that maintains the order in which items are added.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public class OrderedSet<T> : ICollection<T>, IReadOnlyCollection<T>, ISet<T>
{
    private Dictionary<T, int> Dict { get; }
    private List<T> List { get; } = [];


    /// <summary>
    /// Constructs an empty set.
    /// </summary>
    public OrderedSet()
        : this(EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    /// Constructs an empty set with a customer comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use</param>
    public OrderedSet(IEqualityComparer<T> comparer)
    {
        Dict = new(comparer);
    }

    /// <summary>
    /// Constructs a new set from a given set of items
    /// </summary>
    /// <param name="items">Items to fill into the new set</param>
    public OrderedSet(IEnumerable<T> items)
        : this(EqualityComparer<T>.Default)
    {
        AddRange(items);
    }


    /// <summary>
    /// Constructs a new set with a customer comparer from a given set of items
    /// </summary>
    /// <param name="items">Items to fill into the new set</param>
    /// <param name="comparer">The comparer to use</param>
    public OrderedSet(IEnumerable<T> items, IEqualityComparer<T> comparer)
        : this(comparer)
    {
        AddRange(items);
    }

    /// <summary>
    /// Converts the <see cref="OrderedSet{T}"/> to a <see cref="HashSet{T}"/> containing the same elements.
    /// </summary>
    /// <remarks>
    /// The order of elements will be lost in the process
    /// </remarks>
    /// <returns>A <see cref="HashSet{T}"/> containing all elements of the <see cref="OrderedSet{T}"/>.</returns>
    public HashSet<T> ToHashSet() => [..Dict.Keys];

    /// <summary>
    /// Converts the local set to a list
    /// </summary>
    /// <returns></returns>
    public List<T> ToList() => [.. List];

    /// <summary>
    /// Converts the elements of the set to an array.
    /// </summary>
    /// <returns>An array containing all elements in the set, preserving their order.</returns>
    public T[] ToArray() => List.ToArray();

    /// <summary>
    /// Gets the number of elements contained in the <see cref="OrderedSet{T}"/>.
    /// </summary>
    /// <value>The total count of elements in the set.</value>
    public int Count => Dict.Count;

    /// <inheritdoc />
    public virtual bool IsReadOnly => false;

    void ICollection<T>.Add(T item)
    {
        Add(item);
    }

    /// <summary>
    /// Adds a range of items to the <see cref="OrderedSet{T}"/>.
    /// </summary>
    /// <param name="items">The collection of elements to add to the set.</param>
    /// <returns>The updated <see cref="OrderedSet{T}"/> instance.</returns>
    public OrderedSet<T> AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            Add(item);
        return this;
    }

    /// <inheritdoc />
    public void UnionWith(IEnumerable<T> other)
    {
        AddRange(other);
    }

    /// <inheritdoc />
    public void IntersectWith(IEnumerable<T> other)
    {
        var keep = new HashSet<T>(other);
        foreach (var item in Dict.Keys.Where(x => !keep.Contains(x)))
            Remove(item);
    }

    /// <inheritdoc />
    public void ExceptWith(IEnumerable<T> other)
    {
        foreach (var item in other)
            Remove(item);
    }

    /// <inheritdoc />
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Determines whether the current set contains all elements from the specified collection.
    /// </summary>
    /// <param name="other">The collection of elements to check against the current set.</param>
    /// <returns>True if the current set contains all elements from the specified collection; otherwise, false.</returns>
    public bool ContainsAll(IEnumerable<T> other)
    {
        foreach (var item in other)
            if (!Contains(item))
                return false;
        return true;
    }

    /// <summary>
    /// Determines whether the current set contains any elements from the specified collection.
    /// </summary>
    /// <param name="other">The collection of elements to check against the current set.</param>
    /// <returns>True if the current set contains at least one element from the specified collection; otherwise, false.</returns>
    public bool ContainsAny(IEnumerable<T> other)
    {
        foreach (var item in other)
            if (Contains(item))
                return true;
        return false;
    }

    /// <inheritdoc />
    public bool IsSubsetOf(IEnumerable<T> other) => this.All(other.Contains);

    /// <inheritdoc />
    public bool IsSupersetOf(IEnumerable<T> other) => ContainsAll(other);

    /// <inheritdoc />
    public bool IsProperSupersetOf(IEnumerable<T> other) => IsSubsetOf(other) && !IsSupersetOf(other);

    /// <inheritdoc />
    public bool IsProperSubsetOf(IEnumerable<T> other) => !IsSubsetOf(other) && IsSupersetOf(other);

    /// <inheritdoc />
    public bool Overlaps(IEnumerable<T> other) => ContainsAny(other);

    /// <inheritdoc />
    public bool SetEquals(IEnumerable<T> other)
    {
        var cnt = 0;
        foreach (var item in other)
        {
            cnt++;
            if (!Contains(item))
                return false;
        }

        return cnt == Count;
    }

    /// <inheritdoc />
    public void Clear()
    {
        List.Clear();
        Dict.Clear();
    }

    /// <summary>
    /// Removes all elements from the set that match the given predicate.
    /// </summary>
    /// <param name="match">The predicate function that determines whether an element should be removed.</param>
    /// <returns>The current set with the elements removed.</returns>
    public OrderedSet<T> RemoveAll(Predicate<T> match)
    {
        List<int> removeAt = [];
        for (var i = 0; i < List.Count; i++)
            if (match(List[i]))
                removeAt.Add(i);
        for (var i = removeAt.Count - 1; i >= 0; i--)
        {
            var idx = removeAt[i];
            List.RemoveAt(idx);
            Dict.Remove(List[idx]);
            for (var j = idx; j < List.Count; j++)
                Dict[List[j]] = j;
        }

        return this;
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        var found = Dict.TryGetValue(item, out var idx);
        if (!found)
            return false;
        Dict.Remove(item);
        List.RemoveAt(idx);
        for (var i = idx; i < List.Count; i++)
            Dict[List[i]] = i;
        return true;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => List.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Contains(T item) => Dict.ContainsKey(item);

    /// <inheritdoc />
    public void CopyTo(T[] array, int arrayIndex)
    {
        List.CopyTo(array, arrayIndex);
    }

    /// <inheritdoc />
    public bool Add(T item)
    {
        if (Dict.ContainsKey(item))
            return false;
        var idx = List.Count;
        List.Add(item);
        Dict.Add(item, idx);
        return true;
    }
}