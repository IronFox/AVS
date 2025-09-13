using AVS.Log;
using AVS.Util;
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
    Vector3? InitialPosition) :
    WaterParkInhabitant(WaterPark, GameObject, GameObject.transform, Live, Infect, Pickupable)
{
    public bool IsHatching { get; private set; }

    private SmartLog NewLog([CallerFilePath] string callerFilePath = "", [CallerMemberName] string memberName = "")
        => WaterPark.AV.NewLazyAvsLog(tags: [Egg.name.SanitizeObjectName(), InstanceId.ToString()], callerFilePath: callerFilePath, memberName: memberName);

    internal override void OnInstantiate()
    {
        using var log = NewLog();
        Egg.transform.localScale = 0.6f * Vector3.one;
        Egg.transform.position = InitialPosition ?? WaterPark.GetRandomLocation(true, Radius);
        log.Debug($"Spawning egg {GameObject.NiceName()} @{InitialPosition} => {GameObject.transform.localPosition} @r={Radius} progress={Egg.progress}");
        if (WaterPark.hatchEggs)
        {
            Egg.insideWaterPark = true;
            Egg.animator.enabled = true;
            if (Egg.creaturePrefab != null && Egg.creaturePrefab.RuntimeKeyIsValid())
            {
                Egg.UpdateHatchingTime();
                IsHatching = true;
            }
            Egg.OnAddToWaterPark();
        }
        GameObject.GetComponent<Rigidbody>().SafeDo(x => x.isKinematic = true);
        base.OnInstantiate();
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

            WaterPark.AddChild(log, Egg.creaturePrefab, Egg.transform.position + Vector3.up);
        }
        WaterPark.DestroyInhabitant(this);
    }
}
