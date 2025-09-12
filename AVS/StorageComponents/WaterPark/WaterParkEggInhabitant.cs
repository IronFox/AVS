using AVS.Util;
using UnityEngine;

namespace AVS.StorageComponents.WaterPark
{
    internal record WaterParkEggInhabitant(
        MobileWaterPark WaterPark,
        GameObject GameObject,
        LiveMixin Live,
        InfectedMixin Infect,
        CreatureEgg Egg,
        Pickupable Pickupable,
        Vector3? InitialPosition) :
        WaterParkInhabitant(WaterPark, GameObject, Live, Infect, Pickupable)
    {

        internal override void OnInstantiate()
        {
            using var log = WaterPark.AV.NewLazyAvsLog();
            Egg.transform.position = InitialPosition ?? WaterPark.GetRandomLocation(true, Radius);
            log.Debug($"Spawning egg {GameObject.NiceName()} @ {GameObject.transform.localPosition}");
            if (WaterPark.hatchEggs)
            {
                Egg.OnAddToWaterPark();
            }
            GameObject.GetComponent<Rigidbody>().SafeDo(x => x.isKinematic = true);
            base.OnInstantiate();
        }
    }
}
