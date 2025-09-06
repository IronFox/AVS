using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Util
{
    /// <summary>
    /// Physical helper structure to contain all hits from a ray cast.
    /// </summary>
    public class RayCaster : IReadOnlyList<RaycastHit>
    {
        private RaycastHit[] hits = new RaycastHit[4];
        private int lastHitCount = 0;

        /// <inheritdoc/>
        public RaycastHit this[int index] => hits[index];

        /// <inheritdoc/>
        public int Count => lastHitCount;

        /// <inheritdoc/>
        public IEnumerator<RaycastHit> GetEnumerator()
        {
            for (int i = 0; i < lastHitCount; i++)
            {
                yield return hits[i];
            }
        }


        /// <summary>
        /// Raycasts all objects in the scene and returns the number of hits.
        /// </summary>
        /// <remarks>The results are stored in the local instance</remarks>
        /// <param name="ray">Ray to cast</param>
        /// <param name="maxDistance">Max hit distance along the ray</param>
        /// <param name="layerMask">Collider layer mask</param>
        /// <param name="queryTriggerInteraction">Whether to hit trigger colliders</param>
        /// <returns>Number of detected hits that are stored in the local structure</returns>
        public int RayCastAll(Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            var numHits = Physics.RaycastNonAlloc(ray, hits, maxDistance: maxDistance, layerMask: layerMask, queryTriggerInteraction: queryTriggerInteraction);
            while (numHits == hits.Length)
            {
                Array.Resize(ref hits, hits.Length * 2);
                numHits = Physics.RaycastNonAlloc(ray, hits, maxDistance: maxDistance, layerMask: layerMask, queryTriggerInteraction: queryTriggerInteraction);
            }
            return lastHitCount = numHits;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
