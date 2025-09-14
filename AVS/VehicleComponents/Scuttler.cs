using AVS.Log;
using AVS.Util;
using System.Collections;
using UnityEngine;

namespace AVS;

internal class Scuttler : MonoBehaviour
{
    private Vector3 scuttlePosition = Vector3.zero;
    private Vector3 initialLandRayCast = Vector3.zero;
    private ICoroutineHandle? scuttleCor;
    private ICoroutineHandle? establish;
    private ICoroutineHandle? check;

    public void Scuttle(RootModController rmc)
    {
        scuttleCor = rmc.StartAvsCoroutine(
            nameof(Scuttler) + '.' + nameof(DoScuttle),
            _ => DoScuttle(rmc));
    }

    public void Unscuttle(RootModController rmc)
    {
        scuttleCor?.Stop();
        establish?.Stop();
        check?.Stop();
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
    }

    public IEnumerator DoScuttle(RootModController rmc)
    {
        establish = rmc.StartAvsCoroutine(
            nameof(Scuttler) + '.' + nameof(EstablishScuttlePosition),
            _ => EstablishScuttlePosition());
        yield return establish;
        check = rmc.StartAvsCoroutine(
            nameof(Scuttler) + '.' + nameof(CheckScuttlePosition),
            CheckScuttlePosition);
        yield return check;
    }

    public IEnumerator EstablishScuttlePosition()
    {
        scuttlePosition = Vector3.zero;
        while (scuttlePosition == Vector3.zero)
        {
            yield return new WaitForSeconds(1f);
            scuttlePosition = RaycastDownToLand(transform);
        }

        initialLandRayCast = scuttlePosition;
        while (true)
        {
            if (transform.position.y < initialLandRayCast.y - 40) // why 40?
                transform.position = initialLandRayCast + Vector3.up * 25f;
            var oldScuttle = RaycastDownToLand(transform);
            var oldPosition = transform.position;
            yield return new WaitForSeconds(1f);
            scuttlePosition = RaycastDownToLand(transform);
            if (scuttlePosition == oldScuttle && Vector3.Distance(oldPosition, transform.position) < 0.1)
                // If we got here,
                // then we had the same scuttlePosition for one second,
                // and we were stationary for that same second.
                if (Vector3.Distance(scuttlePosition, transform.position) < 5f) // why 5?
                {
                    // If we got here,
                    // we are nearby the scuttle position.
                    gameObject.GetComponent<Rigidbody>().isKinematic = true;
                    yield break;
                }
        }
    }

    public IEnumerator CheckScuttlePosition(SmartLog log)
    {
        while (true)
        {
            if (transform.IsNull())
                break;
            if (transform.position.y < scuttlePosition.y - 20) // why 20?
            {
                log.Write("Moving wreck to " + scuttlePosition.ToString());
                transform.position = scuttlePosition + Vector3.up * 3f;
            }

            yield return new WaitForSeconds(5f);
        }
    }

    public static Vector3 RaycastDownToLand(Transform root)
    {
        RaycastHit[] allHits;
        allHits = Physics.RaycastAll(root.position, Vector3.down, 1000f);
        foreach (var hit in allHits)
            if (hit.transform.GetComponent<TerrainChunkPieceCollider>().IsNotNull())
                return hit.point;

        // we did not hit terrain
        return Vector3.zero;
    }

    public static bool IsDescendant(Transform child, Transform ancestor)
    {
        if (child == ancestor)
            return true;
        var currentParent = child;
        while (currentParent.IsNotNull())
        {
            if (currentParent == ancestor)
                return true; // The child is a descendant of ancestor
            currentParent = currentParent.parent; // Move up in the hierarchy
        }

        return false; // No ancestor found in the hierarchy
    }
}