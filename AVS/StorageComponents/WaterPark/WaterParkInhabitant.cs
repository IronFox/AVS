using AVS.Interfaces;
using UnityEngine;

namespace AVS.StorageComponents.WaterPark
{
    internal record WaterParkInhabitant(
        MobileWaterPark WaterPark,
        GameObject GameObject,
        LiveMixin Live,
        InfectedMixin Infect,
        Pickupable Pickupable
        ) : INullTestableType
    {

        public int InstanceId => GameObject.GetInstanceID();
        public float Radius => WaterPark.GetItemWorldRadius(Pickupable.GetTechType(), GameObject);

        public bool IsInstantiated { get; private set; }

        internal virtual void OnDeinstantiate()
        {
            Live.invincible = false;
            Live.shielded = false;
            IsInstantiated = false;
            GameObject.SetActive(false); //disable the item so it doesn't cause issues
        }

        internal virtual void OnInstantiate()
        {
            Live.invincible = true;
            Live.shielded = true;
            IsInstantiated = true;
            GameObject.SetActive(true); //enable the item so it can be added to the water park

        }

        internal virtual void OnUpdate()
        { }

        internal virtual void OnLateUpdate()
        { }

        internal virtual void OnFixedUpdate()
        { }

        internal virtual void SignalCollidersChanged(bool collidersLive)
        { }
    }
}