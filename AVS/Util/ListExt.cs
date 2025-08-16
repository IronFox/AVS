using System;
using System.Collections.Generic;
using System.Linq;

namespace AVS.Util
{
    /// <summary>
    /// Provides a set of extension methods for operations on collections and lists.
    /// </summary>
    public static class ListExt
    {
        /// <summary>
        /// Finds the index of the first element in the source list that matches the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source list.</typeparam>
        /// <param name="source">The source list to search.</param>
        /// <param name="predicate">A function that defines the condition to match the elements.</param>
        /// <returns>The zero-based index of the first element that matches the predicate, or -1 if no such element is found.</returns>
        public static int FindIndexOf<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the last element in the source list.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source list.</typeparam>
        /// <param name="source">The source list to retrieve the last element from.</param>
        /// <returns>The last element in the source list.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the source list is empty.</exception>
        public static TSource Last<TSource>(this IReadOnlyList<TSource> source)
        {
            if (source.Count == 0)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }
            return source[source.Count - 1];
        }

        /// <summary>
        /// Converts the specified single object to a read-only list containing that object as its single element.
        /// </summary>
        /// <typeparam name="TSource">The type of the object to convert.</typeparam>
        /// <param name="source">The object to convert into a read-only list.</param>
        /// <returns>A read-only list containing the specified object as its only element.</returns>
        public static IReadOnlyList<TSource> ToRoList<TSource>(this TSource source)
        {
            return [ source ];
        }

        /// <summary>
        /// Determines whether two collections are equal by comparing their elements in sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the collections.</typeparam>
        /// <param name="first">The first collection to compare.</param>
        /// <param name="second">The second collection to compare.</param>
        /// <returns>True if the two collections are equal in length and their corresponding elements are equal; otherwise, false.</returns>
        public static bool CollectionsEqual<TSource>(this IReadOnlyCollection<TSource> first,
            IReadOnlyCollection<TSource> second)
            => first.Count == second.Count && first.SequenceEqual(second);

        /// <summary>
        /// Converts an <see cref="IReadOnlyList{T}"/> to an array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source list.</typeparam>
        /// <param name="source">The source list to convert to an array.</param>
        /// <returns>An array containing all the elements of the source list, in the same order.</returns>
        public static T[] ToArray<T>(this IReadOnlyList<T> source)
        {
            if (source.Count == 0)
            {
                return [];
            }
            if (source is T[] array)
            {
                return array;
            }
            var result = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                result[i] = source[i];
            }
            return result;
        }

        /// <summary>
        /// Adds a range of items to the specified HashSet and returns the count of items that were successfully added.
        /// </summary>
        /// <typeparam name="T">The type of elements in the set and the collection.</typeparam>
        /// <param name="set">The HashSet to add items to.</param>
        /// <param name="items">The collection of items to add to the HashSet.</param>
        /// <returns>The number of items successfully added to the HashSet.</returns>
        public static int AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
        {
            int count = 0;
            foreach (T item in items)
            {
                if (set.Add(item))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Executes the specified action for each element in the source collection.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source collection.</typeparam>
        /// <param name="source">The collection of elements to process.</param>
        /// <param name="action">The action to perform on each element in the collection.</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// Converts the elements of the given enumerable to a HashSet.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source enumerable.</typeparam>
        /// <param name="source">The source enumerable to convert.</param>
        /// <returns>A HashSet containing the elements of the source enumerable.</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return [..source];
        }

        /// <summary>
        /// Finds the element in the source sequence that has the least value as determined by the specified selector function.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source sequence.</typeparam>
        /// <param name="source">The source sequence to search.</param>
        /// <param name="selector">A function to extract a float value from each element for comparison.</param>
        /// <returns>The element in the source sequence with the least value as determined by the selector function.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the source sequence contains no elements.</exception>
        public static T Least<T>(this IEnumerable<T> source, Func<T, float> selector)
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            var current = enumerator.Current;
            var num = selector(current);
            while (enumerator.MoveNext())
            {
                var num2 = selector(enumerator.Current);
                if (num2 < num)
                {
                    num = num2;
                    current = enumerator.Current;
                }
            }

            return current;
        }

    }
}
