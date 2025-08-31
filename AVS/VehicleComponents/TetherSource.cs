using AVS.Util;
using AVS.VehicleTypes;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace AVS.VehicleComponents;

/*
 * Tether Sources are meant to be placed throughout the Submarine.
 * The Submarine has one TetherSource component to manage them.
 * They should be strictly inside the ship.
 * A player will "leash" to them when close enough,
 * which ensures the player's entry is recognized no matter what (warp in).
 */
/// <summary>
/// Represents a tether point within a Submarine, which manages player interactions
/// and ensures proper recognition of player proximity and entry events.
/// </summary>
/// <remarks>
/// Tether sources are designed to purely exist within the bounds of a submarine
/// and are integral for the functionality of player leashing.
/// This ensures smooth player recognition regardless of warp or entry conditions.
/// </remarks>
internal class TetherSource : AvAttached, IScuttleListener, IDockListener
{
    private bool isLive = true;
    public bool isSimple;

    private Submarine Sub => (AV as Submarine).OrThrow($"Attached vehicle {AV.NiceName()} is not a Submarine");


    public Bounds Bounds
    {
        get
        {
            if (av.IsNull() || av.isScuttled)
                return new Bounds(Vector3.zero, Vector3.zero);
            var collider = Sub.Com.BoundingBoxCollider;
            if (collider.IsNull())
                return new Bounds(Vector3.zero, Vector3.zero);
            collider.gameObject.SetActive(true);
            collider.enabled = true;
            var result = collider.bounds;
            collider.enabled = false;
            if (result.size == Vector3.zero)
            {
                if (!collider.gameObject.activeInHierarchy)
                {
                    if (!collider.gameObject.activeSelf)
                    {
                        Logger.Warn("TetherSource Error: BoundingBoxCollider was not active. Setting it active.");
                        collider.gameObject.SetActive(true);
                    }

                    if (!collider.gameObject.activeInHierarchy)
                    {
                        Logger.Warn(
                            "TetherSource Error: BoundingBoxCollider was not active in its hierarchy. One of its parents must be inactive. Trying to set them active...");
                        var iterator = collider.transform;
                        while (iterator != Sub.transform)
                        {
                            if (!iterator.gameObject.activeSelf)
                            {
                                iterator.gameObject.SetActive(true);
                                Logger.Warn("Set " + iterator.name + " active.");
                            }

                            iterator = iterator.parent;
                        }
                    }

                    collider.enabled = true;
                    result = collider.bounds;
                    collider.enabled = false;
                    return result;
                }
                else
                {
                    Logger.Warn("TetherSource Error: BoundingBox Bounds had zero volume (size was zero).");
                }

                return new Bounds(Vector3.one, Vector3.zero);
            }

            return result;
        }
    }

    public void Start()
    {
        if (av.IsNull() || Sub.Com.BoundingBoxCollider.IsNull() || Sub.Com.TetherSources.Count == 0)
        {
            isSimple = true;
        }
        else
        {
            isSimple = false;
            Sub.Com.BoundingBoxCollider.gameObject.SetActive(true);
            Sub.Com.BoundingBoxCollider.enabled = false;
            Sub.Com.TetherSources.ForEach(x => x.SetActive(false));
        }

        Player.main.StartCoroutine(ManageTether());
    }

    public void TryToDropLeash()
    {
        if (av.IsNull() || av.IsPlayerControlling())
            return;
        if (isSimple)
        {
            if (Vector3.Distance(Player.main.transform.position, transform.position) > 10)
                MVExit($"TryToDropLeash: Simple tether: Distance to vehicle center has exceded 10 meters");
        }
        else
        {
            var bounds = Bounds;
            if (!bounds.Contains(Player.main.transform.position))
                MVExit(
                    $"TryToDropLeash: Vehicle bounding box ({bounds}) no longer contains player at ({Player.main.transform.position})");
        }
    }

    private void MVExit(string reason)
    {
        using var log = Sub!.NewAvsLog();
        log.Write("TetherSource: Player exiting vehicle because " + reason);
        Sub.ExitHelmControl();
        Sub.ClosestPlayerExit(false);

        //// the following block is just for the gargantuan leviathan
        //// that mod disables all vehicle colliders in GargantuanGrab.GrabVehicle
        //// but doesn't later re-enable them.
        //// I don't know why it rips the player out of its position anyways.
        //IEnumerator PleaseEnableColliders()
        //{
        //    var effectedColliders = Sub.GetComponentsInChildren<Collider>(true)
        //        .Where(x => !x.enabled) // collisionModel is set active again
        //        .Where(x => x != Sub.Com.BoundingBoxCollider) // want this to remain disabled
        //        .ToList();
        //    while (effectedColliders.Any(x => !x.enabled))
        //    {
        //        yield return new WaitForSeconds(5);
        //        effectedColliders.ForEach(x => x.enabled = true);
        //        yield return new WaitForSeconds(5);
        //    }
        //}
        //MainPatcher.Instance.StartCoroutine(PleaseEnableColliders());
    }

    public void TryToEstablishLeash()
    {
        if (av.IsNull())
            return;

        using var log = Sub.NewLazyAvsLog();

        bool PlayerWithinLeash(GameObject tetherSrc)
        {
            var radius = 0.75f;
            if (tetherSrc.GetComponentInChildren<SphereCollider>(true).IsNotNull())
                radius = tetherSrc.GetComponentInChildren<SphereCollider>(true).radius;
            return Vector3.Distance(Player.main.transform.position, tetherSrc.transform.position) < radius;
        }

        if (Player.main.GetVehicle().IsNull())
        {
            if (isSimple)
            {
                if (Vector3.Distance(Player.main.transform.position, transform.position) < 1f)
                {
                    log.Write(
                        "TetherSource: Player is close enough to simple tether source. Registering player entry.");
                    Sub.RegisterTetherEntry(this);
                }
                else if (Vector3.Distance(Player.main.transform.position,
                             Sub.Com.Helms.First().Root.transform.position) < 1f)
                {
                    log.Write("TetherSource: Player is close enough to helms root. Registering player entry.");
                    Sub.RegisterTetherEntry(this);
                }
            }
            else
            {
                var closest = Sub.Com.TetherSources.FirstOrDefault(PlayerWithinLeash);
                if (closest.IsNotNull())
                {
                    log.Write(
                        $"TetherSource: Player is close enough to tether source {closest.NiceName()}. Registering player entry.");
                    Sub.RegisterTetherEntry(this);
                }
            }
        }
    }

    public IEnumerator ManageTether()
    {
        yield return new WaitForSeconds(3f);
        while (true)
        {
            if (av.IsNull())
                yield break;
            if (isLive)
            {
                if (Sub.IsPlayerInside())
                    TryToDropLeash();
                else
                    TryToEstablishLeash();
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void IScuttleListener.OnScuttle()
    {
        isLive = false;
    }

    void IScuttleListener.OnUnscuttle()
    {
        isLive = true;
    }

    void IDockListener.OnDock()
    {
        isLive = false;
    }

    void IDockListener.OnUndock()
    {
        isLive = true;
    }
}