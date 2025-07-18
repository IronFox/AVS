using System;
using System.Collections.Generic;
using System.Linq;

namespace AVS.Util
{
    public static class ListExt
    {
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

        public static TSource Last<TSource>(this IReadOnlyList<TSource> source)
        {
            if (source.Count == 0)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }
            return source[source.Count - 1];
        }
        public static IReadOnlyList<TSource> ToRoList<TSource>(this TSource source)
        {
            return new TSource[] { source };
        }

        public static bool CollectionsEqual<TSource>(this IReadOnlyCollection<TSource> first, IReadOnlyCollection<TSource> second)
            => first.Count == second.Count && Enumerable.SequenceEqual(first, second);

        public static T[] ToArray<T>(this IReadOnlyList<T> source)
        {
            if (source.Count == 0)
            {
                return Array.Empty<T>();
            }
            if (source is T[] array)
            {
                return array;
            }
            T[] result = new T[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                result[i] = source[i];
            }
            return result;
        }

        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                set.Add(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static T Least<T>(this IEnumerable<T> source, Func<T, float> selector)
        {
            IEnumerator<T> enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            T current = enumerator.Current;
            float num = selector(current);
            while (enumerator.MoveNext())
            {
                float num2 = selector(enumerator.Current);
                if (num2 < num)
                {
                    num = num2;
                    current = enumerator.Current;
                }
            }

            return current;
        }

        //public static T LeastOrDefault<T>(this IEnumerable<T> source, Func<T, float> selector)
        //{
        //    IEnumerator<T> enumerator = source.GetEnumerator();
        //    if (!enumerator.MoveNext())
        //    {
        //        return default(T);
        //    }

        //    T current = enumerator.Current;
        //    float num = selector(current);
        //    while (enumerator.MoveNext())
        //    {
        //        float num2 = selector(enumerator.Current);
        //        if (num2 < num)
        //        {
        //            num = num2;
        //            current = enumerator.Current;
        //        }
        //    }

        //    return current;
        //}
    }
}
