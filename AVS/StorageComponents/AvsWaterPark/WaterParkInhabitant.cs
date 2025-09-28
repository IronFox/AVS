using AVS.Interfaces;
using AVS.Util;
using UnityEngine;

namespace AVS.StorageComponents.AvsWaterPark
{
    internal record WaterParkInhabitant(
        MobileWaterPark WaterPark,
        GameObject GameObject,
        Transform RootTransform,
        LiveMixin Live,
        InfectedMixin Infect,
        Pickupable Pickupable
        ) : INullTestableType
    {
        public float ExpectedInfectionLevel { get; set; }


        public int InstanceId => GameObject.GetInstanceID();
        public float Radius => WaterPark.GetItemWorldRadius(Pickupable.GetTechType(), GameObject);

        public bool IsInstantiated { get; private set; }

        internal virtual void OnDeinstantiate()
        {
            GameObject.name = GameObject.name.Replace(NameTag, "");
            Live.invincible = false;
            Live.shielded = false;
            IsInstantiated = false;
            Object.Destroy(GameObject.GetComponent<InhabitantTag>());
            GameObject.SetActive(false); //disable the item so it doesn't cause issues
        }

        internal virtual void OnInstantiate()
        {
            GameObject.name += NameTag;
            Live.invincible = true;
            Live.shielded = true;
            IsInstantiated = true;
            GameObject.EnsureComponent<InhabitantTag>();
            GameObject.SetActive(true); //enable the item so it can be added to the water park

        }

        internal virtual void OnUpdate()
        { }

        internal virtual void OnLateUpdate()
        { }

        internal virtual void OnFixedUpdate()
        { }

        internal virtual void SignalCollidersChanged(bool collidersLive)
        {
            GameObject.SetActive(collidersLive);
        }


        public const float ConsideredCuredInfectionLevel = 0;
        public bool CanBeCured => ExpectedInfectionLevel > ConsideredCuredInfectionLevel;
        public bool IsContagious => ExpectedInfectionLevel > 0.25f;
        public bool IsLessThanCompletelyInfected => ExpectedInfectionLevel < 1f;
        public void Cure()
        {
            ExpectedInfectionLevel = ConsideredCuredInfectionLevel;
            Infect.SetInfectedAmount(ConsideredCuredInfectionLevel);
        }

        internal void ContractInfection()
        {
            ExpectedInfectionLevel = 1;
            Infect.SetInfectedAmount(1);
        }
        public static string NameTag { get; } = "[[Avs.MWP.Inhabitant]]";

        protected static bool IsLikely(GameObject x, MobileWaterPark checkFor)
        {
            if (x.name.IndexOf(NameTag) < 0)
                return false;
            var live = x.GetComponent<LiveMixin>();
            if (live.IsNull())
                return false;
            if (live.invincible)
                return true;
            return !IsForeign(x, checkFor);

        }

        protected static bool IsForeign(GameObject x, MobileWaterPark checkFor)
        {
            var wp1 = x.GetComponentInParent<WaterPark>();
            if (wp1.IsNotNull())
                return true;
            var wp2 = x.GetComponentInParent<MobileWaterPark>();
            return wp2.IsNotNull() && wp2 != checkFor;
        }
    }
}