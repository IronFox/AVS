using AVS.Log;
using AVS.Util;
using AVS.Util.Math;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AVS.StorageComponents.WaterPark;

internal record WaterParkEggInhabitant(
    MobileWaterPark WaterPark,
    GameObject GameObject,
    LiveMixin Live,
    InfectedMixin Infect,
    CreatureEgg Egg,
    Pickupable Pickupable,
    GlobalPosition? InitialPosition) :
    WaterParkInhabitant(WaterPark, GameObject, GameObject.transform, Live, Infect, Pickupable)
{
    public bool IsHatching { get; private set; }

    private SmartLog NewLog(LogParameters? p = null, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string memberName = "")
        => WaterPark.AV.NewLazyAvsLog(tags: [Egg.name.SanitizeObjectName(), InstanceId.ToString()], callerFilePath: callerFilePath, memberName: memberName, parameters: p);

    internal override void OnInstantiate()
    {
        using var log = NewLog();
        Egg.transform.localScale = 0.6f * Vector3.one;
        Egg.transform.position = (InitialPosition ?? WaterPark.GetRandomLocation(true, Radius)).GlobalCoordinates;
        log.Debug($"Spawning egg {GameObject.NiceName()} @{InitialPosition} => {RootTransform.localPosition} @r={Radius} progress={Egg.progress}");
        if (WaterPark.hatchEggs)
        {
            Egg.insideWaterPark = true;
            Egg.animator.enabled = true;
            if (Egg.creaturePrefab != null && Egg.creaturePrefab.RuntimeKeyIsValid())
            {
                Egg.UpdateHatchingTime();
                IsHatching = true;
            }
        }
        GameObject.GetComponent<Rigidbody>().SafeDo(x =>
        {
            x.isKinematic = true;
            x.detectCollisions = false;
        });
        base.OnInstantiate();
    }

    internal override void OnDeinstantiate()
    {
        GameObject.GetComponent<Rigidbody>().SafeDo(x =>
        {
            x.isKinematic = false;
            x.detectCollisions = true;
        });
        base.OnDeinstantiate();
    }

    internal override void SignalCollidersChanged(bool collidersLive)
    {
        using var log = NewLog(Params.Of(collidersLive));
        log.Debug($"Setting colliders live={collidersLive} for egg {GameObject.NiceName()} @ {RootTransform.localPosition}");
        //Egg.enabled = collidersLive;
        base.SignalCollidersChanged(collidersLive);
        Egg.animator.enabled = collidersLive;
        if (collidersLive)
            Egg.animator.Play(AnimatorHashID.progress, 0, Egg.progress);
    }

    internal override void OnUpdate()
    {
        if (IsHatching)
        {
            float timePassedAsFloat = DayNightCycle.main.timePassedAsFloat;
            Egg.progress = Mathf.InverseLerp(Egg.timeStartHatching, Egg.timeStartHatching + Egg.GetHatchDuration(), timePassedAsFloat);
            Egg.animator.SetFloat(AnimatorHashID.progress, Egg.progress);
            if (Egg.progress >= 1f)
            {
                Hatch();
            }
        }
        else
            Egg.animator.SetFloat(AnimatorHashID.progress, Egg.progress);
        base.OnUpdate();
    }

    private void Hatch()
    {
        using var log = NewLog();
        log.Debug($"Hatching egg {GameObject.NiceName()} @ {GameObject.transform.localPosition}");
        IsHatching = false;
        WaterParkItem component = Egg.GetComponent<WaterParkItem>();
        if (component != null)
        {
            if (KnownTech.Add(Egg.eggType, verbose: false))
            {
                ErrorMessage.AddMessage(Language.main.GetFormat("EggDiscovered", Language.main.Get(Egg.eggType.AsString())));
            }

            WaterPark.AddChildOrEggSpawn(log, Egg.creaturePrefab, GlobalPosition.Of(Egg) + Vector3.up);
        }
        WaterPark.DestroyInhabitant(this);
    }
}
