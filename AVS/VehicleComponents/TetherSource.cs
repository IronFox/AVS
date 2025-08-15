using AVS.Util;
using AVS.VehicleTypes;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace AVS
{
    /*
     * Tether Sources are meant to be placed throughout the Submarine.
     * The Submarine has one TetherSource component to manage them.
     * They should be strictly inside the ship.
     * A player will "leash" to them when close enough,
     * which ensures the player's entry is recognized no matter what (warp in).
     */
    public class TetherSource : MonoBehaviour, IScuttleListener, IDockListener
    {
        public Submarine? mv = null;
        private bool isLive = true;
        public bool isSimple;
        public Bounds Bounds
        {
            get
            {
                if (mv == null || mv.isScuttled)
                {
                    return new Bounds(Vector3.zero, Vector3.zero);
                }
                var collider = mv.Com.BoundingBoxCollider;
                if (collider == null)
                {
                    return new Bounds(Vector3.zero, Vector3.zero);
                }
                collider.gameObject.SetActive(true);
                collider.enabled = true;
                Bounds result = collider.bounds;
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
                            Logger.Warn("TetherSource Error: BoundingBoxCollider was not active in its hierarchy. One of its parents must be inactive. Trying to set them active...");
                            Transform iterator = collider.transform;
                            while (iterator != mv.transform)
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

            if (mv == null || mv.Com.BoundingBoxCollider == null || mv.Com.TetherSources.Count == 0)
            {
                isSimple = true;
            }
            else
            {
                isSimple = false;
                mv.Com.BoundingBoxCollider.gameObject.SetActive(true);
                mv.Com.BoundingBoxCollider.enabled = false;
                mv.Com.TetherSources.ForEach(x => x.SetActive(false));
            }
            Player.main.StartCoroutine(ManageTether());
        }

        public void TryToDropLeash()
        {
            if (mv == null || mv.IsPlayerControlling())
            {
                return;
            }
            if (isSimple)
            {
                if (Vector3.Distance(Player.main.transform.position, transform.position) > 10)
                {
                    MVExit($"TryToDropLeash: Simple tether: Distance to vehicle center has exceded 10 meters");
                }
            }
            else
            {
                var bounds = Bounds;
                if (!bounds.Contains(Player.main.transform.position))
                {
                    MVExit($"TryToDropLeash: Vehicle bounding box ({bounds}) no longer contains player at ({Player.main.transform.position})");
                }
            }
        }
        private void MVExit(string reason)
        {
            mv!.Log.Write("TetherSource: Player exiting vehicle because " + reason);
            mv.ExitHelmControl();
            mv.ClosestPlayerExit(false);

            //// the following block is just for the gargantuan leviathan
            //// that mod disables all vehicle colliders in GargantuanGrab.GrabVehicle
            //// but doesn't later re-enable them.
            //// I don't know why it rips the player out of its position anyways.
            //IEnumerator PleaseEnableColliders()
            //{
            //    var effectedColliders = mv.GetComponentsInChildren<Collider>(true)
            //        .Where(x => !x.enabled) // collisionModel is set active again
            //        .Where(x => x != mv.Com.BoundingBoxCollider) // want this to remain disabled
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
            if (mv == null)
            {
                return;
            }
            bool PlayerWithinLeash(GameObject tetherSrc)
            {
                float radius = 0.75f;
                if (tetherSrc.GetComponentInChildren<SphereCollider>(true) != null)
                {
                    radius = tetherSrc.GetComponentInChildren<SphereCollider>(true).radius;
                }
                return Vector3.Distance(Player.main.transform.position, tetherSrc.transform.position) < radius;
            }
            if (Player.main.GetVehicle() == null)
            {
                if (isSimple)
                {
                    if (Vector3.Distance(Player.main.transform.position, transform.position) < 1f)
                    {
                        mv.Log.Write("TetherSource: Player is close enough to simple tether source. Registering player entry.");
                        mv.RegisterTetherEntry(this);
                    }
                    else if (Vector3.Distance(Player.main.transform.position, mv.Com.Helms.First().Root.transform.position) < 1f)
                    {
                        mv.Log.Write("TetherSource: Player is close enough to helms root. Registering player entry.");
                        mv.RegisterTetherEntry(this);
                    }
                }
                else
                {
                    var closest = mv.Com.TetherSources.FirstOrDefault(PlayerWithinLeash);
                    if (closest != null)
                    {
                        mv.Log.Write($"TetherSource: Player is close enough to tether source {closest.NiceName()}. Registering player entry.");
                        mv.RegisterTetherEntry(this);
                    }
                }
            }

        }

        public IEnumerator ManageTether()
        {
            yield return new WaitForSeconds(3f);
            while (true)
            {
                if (mv == null)
                {
                    yield break;
                }
                if (isLive)
                {
                    if (mv.IsPlayerInside())
                    {
                        TryToDropLeash();
                    }
                    else
                    {
                        TryToEstablishLeash();
                    }
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
}
