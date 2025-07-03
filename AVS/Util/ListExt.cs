using System;
using System.Collections.Generic;

namespace AVS.Util
{
    public static class ListExt
    {
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

        public static T LeastOrDefault<T>(this IEnumerable<T> source, Func<T, float> selector)
        {
            IEnumerator<T> enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return default(T);
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
    }
}
