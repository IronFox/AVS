using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.Admin
{

    /// <summary>
    /// Manages a collection of game objects of a specified type, providing functionality for registration,
    /// deregistration, filtering, and proximity-based queries.
    /// </summary>
    /// <remarks>This class is designed to manage game objects of a specific type, allowing for operations 
    /// such as finding the nearest object to a given position, filtering objects based on a predicate,  and maintaining
    /// a registry of active objects. It is particularly useful in scenarios where  game objects of a specific type need
    /// to be tracked and queried efficiently.</remarks>
    /// <typeparam name="T">The type of game object managed by this class. Must derive from <see cref="Component"/>.</typeparam>
    public static class GameObjectManager<T> where T : Component
    {

        static GameObjectManager()
        {
            GameStateWatcher.OnSceneUnloaded.Add(ClearList);
        }


        private static List<T> AllSuchObjects { get; } = new List<T>();
        /// <summary>
        /// Finds the nearest object of type <typeparamref name="T"/> to the specified target position, optionally
        /// filtered by a provided predicate.
        /// </summary>
        /// <remarks>If an object does not have a valid <c>transform</c> or its position cannot be
        /// determined, it is excluded from the distance calculation.</remarks>
        /// <param name="target">The target position to measure distances from.</param>
        /// <param name="filter">An optional predicate to filter the objects. Only objects for which the predicate returns <see
        /// langword="true"/> will be considered. If <see langword="null"/>, no filtering is applied.</param>
        /// <returns>The nearest object of type <typeparamref name="T"/> to the <paramref name="target"/> position that satisfies
        /// the filter, or <see langword="null"/> if no such object is found.</returns>
        public static T FindNearestSuch(Vector3 target, Func<T, bool> filter = null)
        {
            float ComputeDistance(T thisObject)
            {
                if (!thisObject || !thisObject.transform)
                {
                    return 99999;
                }
                try
                {
                    return Vector3.Distance(target, thisObject.transform.position);
                }
                catch
                {
                    return 99999;
                }
            }

            List<T> FilteredSuchObjects = Where(x => x != null);
            if (filter != null)
            {
                FilteredSuchObjects = FilteredSuchObjects.Where(x => filter(x)).ToList();
            }
            return FilteredSuchObjects.OrderBy(x => ComputeDistance(x)).FirstOrDefault();
        }

        private static void SanitizeList()
        {
            AllSuchObjects.RemoveAll(x => !x);
        }

        /// <summary>
        /// Filters the collection of objects based on a specified predicate.
        /// </summary>
        /// <remarks>
        /// Removes registered objects from the local manager that no longer exist.
        /// </remarks>
        /// <param name="pred">A function that defines the condition each object must satisfy to be included in the result.</param>
        /// <returns>A list of objects that satisfy the specified predicate.</returns>
        public static List<T> Where(Func<T, bool> pred)
        {
            SanitizeList();
            return AllSuchObjects.Where(pred).ToList();
        }

        /// <summary>
        /// Registers the specified object in the collection of tracked objects.
        /// </summary>
        /// <remarks>The registered object is added to a shared collection, which is used to track all
        /// such objects. Ensure that the object being registered is valid and not already present in the collection to
        /// avoid duplication.</remarks>
        /// <param name="cont">The object to register. This object must not be null.</param>
        public static void Register(T cont)
        {
            if (cont)
                AllSuchObjects.Add(cont);
        }

        /// <summary>
        /// Removes the specified object from the collection of registered objects.
        /// </summary>
        /// <param name="cont">The object to deregister. Must not be null.</param>
        public static void Deregister(T cont)
        {
            if (cont)
                AllSuchObjects.Remove(cont);
            else
                SanitizeList();
        }

        /// <summary>
        /// Clears all items from the list of objects.
        /// </summary>
        /// <remarks>After calling this method, the list will be empty. This operation does not raise any
        /// events or perform additional actions beyond clearing the list.</remarks>
        public static void ClearList()
        {
            AllSuchObjects.Clear();
        }
    }

}
