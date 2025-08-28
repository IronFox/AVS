using AVS.Util;
using System.Collections;
using UnityEngine;

namespace AVS.BaseVehicle;

public abstract partial class AvsVehicle
{
    /// <summary>
    /// True if the vehicle is scuttled (destroyed and ready to be salvaged).
    /// </summary>
    public bool isScuttled { get; private set; } = false;


    /// <summary>
    /// Destroys the vehicle and executes the death action.
    /// </summary>
    /// <remarks>Calls <see cref="DeathAction" /> and <see cref="ScuttleVehicle" /></remarks>
    public virtual void DestroyVehicle()
    {
        DeathAction();
        ScuttleVehicle();
    }

    /// <summary>
    /// Executed when the vehicle is destroyed.
    /// This default behavior lets the vehicle slowly sink to the bottom of the ocean.
    /// </summary>
    public virtual void DeathAction()
    {
        worldForces.enabled = true;
        worldForces.handleGravity = true;
        worldForces.underwaterGravity = 1.5f;
    }

    /// <summary>
    /// Executed when the vehicle is destroyed.
    /// Sets this vehicle as ready to be salvaged.
    /// </summary>
    public virtual void ScuttleVehicle()
    {
        if (isScuttled) return;
        HudPingInstance.enabled = false;

        void OnCutOpen(Sealed sealedComp)
        {
            OnSalvage();
        }

        isScuttled = true;
        foreach (var component in GetComponentsInChildren<IScuttleListener>())
            (component as IScuttleListener).OnScuttle();
        Com.WaterClipProxies.ForEach(x => x.SetActive(false));
        IsPoweredOn = false;
        gameObject.EnsureComponent<Scuttler>().Scuttle(Owner);
        var sealedThing = gameObject.EnsureComponent<Sealed>();
        sealedThing.openedAmount = 0;
        sealedThing.maxOpenedAmount = liveMixin.maxHealth / 5f;
        sealedThing.openedEvent.AddHandler(gameObject, new UWE.Event<Sealed>.HandleFunction(OnCutOpen));
    }

    /// <summary>
    /// Returns the vehicle to a non-scuttled state.
    /// </summary>
    public virtual void UnscuttleVehicle()
    {
        isScuttled = false;
        foreach (var component in GetComponentsInChildren<IScuttleListener>()) component.OnUnscuttle();
        Com.WaterClipProxies.ForEach(x => x.SetActive(true));
        IsPoweredOn = true;
        gameObject.EnsureComponent<Scuttler>().Unscuttle(Owner);
    }

    /// <summary>
    /// Executed when the vehicle is salvaged.
    /// </summary>
    public virtual void OnSalvage()
    {
        IEnumerator DropLoot(Vector3 place, GameObject root)
        {
            var result = new InstanceContainer();
            foreach (var item in Config.Recipe)
                for (var i = 0; i < item.Amount; i++)
                {
                    yield return null;
                    if (Random.value < 0.6f) continue;

                    yield return AvsCraftData.InstantiateFromPrefabAsync(Log.Tag(nameof(OnSalvage)), item.Type, result);
                    var go = result.Instance;
                    if (!go.IsNull())
                    {
                        var loc = place + 1.2f * Random.onUnitSphere;
                        var rot = 360 * Random.onUnitSphere;
                        go.transform.position = loc;
                        go.transform.eulerAngles = rot;
                        var rb = go.EnsureComponent<Rigidbody>();
                        rb.isKinematic = false;
                    }
                }

            while (root.IsNotNull())
            {
                Destroy(root);
                yield return null;
            }
        }

        StartCoroutine(DropLoot(transform.position, gameObject));
    }
}