using AVS.Util;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AVS.StorageComponents.WaterPark
{
    internal record WaterParkCreatureInhabitant(
        MobileWaterPark WaterPark,
        GameObject GameObject,
        Rigidbody? Rigidbody,
        LiveMixin Live,
        InfectedMixin Infect,
        Pickupable Pickupable,
        WaterParkCreature WpCreature,
        Creature Creature,
        Vector3? InitialPosition
        )
        : WaterParkInhabitant(WaterPark, GameObject, Live, Infect, Pickupable)
    {
        public bool IsSupposedToBeHealthy { get; set; }
        public bool IsSupposedToBeHero { get; set; }
        public Vector3 LastSwimTarget { get; private set; }
        public float SwimVelocity => Mathf.Lerp(WpCreature.swimMinVelocity, WpCreature.swimMaxVelocity, WpCreature.age) * 0.2f;

        internal override void OnInstantiate()
        {
            using var log = WaterPark.AV.NewLazyAvsLog();

            var peeper = Creature as Peeper;
            IsSupposedToBeHealthy = Infect.GetInfectedAmount() == 0f;
            IsSupposedToBeHero = peeper.IsNotNull() && peeper.isHero;
            log.Debug($"Instantiating creature {Creature.NiceName()} with healthy={IsSupposedToBeHealthy}, hero={IsSupposedToBeHero}");

            WpCreature.SetInsideState();
            WpCreature.swimBehaviour = GameObject.GetComponent<SwimBehaviour>();
            WpCreature.breedInterval = 30;// WpCreature.data.growingPeriod * 0.5f;
            WpCreature.ResetBreedTime();
            WpCreature.timeNextSwim = 0;
            Creature.transform.position = InitialPosition ?? WaterPark.RandomLocation(false, Radius);
            WaterPark.EnforceEnclosure(Creature.transform, Radius);
            Creature.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            WpCreature.transform.localScale = WpCreature.data.maxSize * Vector3.one;
            if (IsSupposedToBeHero)
            {
                peeper!.UpdateEnzymeFX();
                peeper.enzymeParticles.Play();
                peeper.enzymeTrail.enabled = true;
                peeper.healingTrigger.SetActive(value: true);
            }
            else if (peeper.IsNotNull())
            {
                peeper.UpdateEnzymeFX();
                peeper.enzymeParticles.Stop();
                peeper.enzymeTrail.enabled = false;
                peeper.healingTrigger.SetActive(value: false);
            }
            if (Infect.renderers.IsNull())
                log.Warn($"Creature {Creature.NiceName()} has no infected renderers");
            Infect.prevInfectedAmount = -1;
            Infect.UpdateInfectionShading();
            Rigidbody.SafeDo(rb =>
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.detectCollisions = false;
                rb.angularDrag = 0.5f;
            });


            //GameObject.GetComponentsInChildren<Collider>(true).ForEach(x => x.enabled = false);

            base.OnInstantiate();
            log.Debug($"Spawning creature {Creature.NiceName()} @ {GameObject.transform.localPosition} with breed interval {WpCreature.breedInterval}");
        }


        internal override void OnDeinstantiate()
        {
            WpCreature.SetOutsideState();
            Rigidbody.SafeDo(rb =>
            {
                rb.detectCollisions = true;
            });

            base.OnDeinstantiate();
        }


        private void UpdateMovement(float radius)
        {
            if (Time.time > WpCreature.timeNextSwim)
            {
                RandomizeSwimTargetNow(radius);
            }

        }

        private void RandomizeSwimTargetNow(float radius)
        {
            using var log = WaterPark.AV.NewLazyAvsLog();
            LastSwimTarget = WpCreature.swimTarget = WaterPark.RandomLocation(false, radius + 0.5f);
            WpCreature.swimBehaviour.SwimTo(WpCreature.swimTarget, SwimVelocity);
            log.Debug($"Creature {Creature.NiceName()} swimming to {WpCreature.swimTarget} from {Creature.transform.position} (age={WpCreature.age}, minV={WpCreature.swimMinVelocity}, maxV={WpCreature.swimMaxVelocity})");
            WpCreature.timeNextSwim = Time.time + WpCreature.swimInterval * UnityEngine.Random.Range(1f, 2f);
        }

        internal override void OnUpdate()
        {
            float radius = Radius;

            UpdateMovement(radius);

            Creature.Hunger.Value = 1f; //disable hunger
            Creature.Scared.Value = 0f; //disable fear

            double timePassed = DayNightCycle.main.timePassed;
            if (!WpCreature.isMature)
            {
                float a = (float)(WpCreature.matureTime - (double)WpCreature.data.growingPeriod);
                WpCreature.age = Mathf.InverseLerp(a, (float)WpCreature.matureTime, (float)timePassed);
                WpCreature.transform.localScale = Mathf.Lerp(WpCreature.data.initialSize, WpCreature.data.maxSize, WpCreature.age) * Vector3.one;
                if (WpCreature.age == 1f)
                {
                    WpCreature.isMature = true;
                    if (WpCreature.data.canBreed)
                    {
                        WpCreature.breedInterval = WpCreature.data.growingPeriod * 0.5f;
                        if (WpCreature.timeNextBreed < 0f)
                        {
                            WpCreature.ResetBreedTime();
                        }
                    }
                    else
                    {
                        //this is some strange behavior. It seems to reinstantiate itself as an adult...
                        AssetReferenceGameObject adultPrefab = WpCreature.data.adultPrefab;
                        if (adultPrefab != null && adultPrefab.RuntimeKeyIsValid())
                        {
                            WaterPark.Reincarnate(this, adultPrefab, Infect.GetInfectedAmount(), Creature is Peeper peeper ? peeper.enzymeAmount : 0, WpCreature.transform.position);
                            return;
                        }
                    }
                }
            }

            if (WaterPark.breedCreatures && WpCreature.GetCanBreed() && timePassed > WpCreature.timeNextBreed)
            {
                using var log = WaterPark.AV.NewLazyAvsLog();
                log.Debug($"Creature {Creature.NiceName()} is ready to breed.");
                WpCreature.ResetBreedTime();
                var breedingPartner = WaterPark.GetBreedingPartner(this);
                if (breedingPartner.IsNotNull())
                {
                    breedingPartner.WpCreature.ResetBreedTime();
                    log.Debug($"Breeding {Creature.NiceName()} with {breedingPartner.Creature.NiceName()}");
                    WaterPark.AddChild(log, WpCreature.data.eggOrChildPrefab, GameObject.transform.position);
                }
                else
                    log.Debug($"Creature {Creature.NiceName()} could not find a breeding partner. Retrying in {WpCreature.breedInterval}");
            }

            MonitorSwimTarget(radius);
            ClampPosition(radius);

            if (IsSupposedToBeHealthy)
                Infect.SetInfectedAmount(0);
            base.OnUpdate();



        }

        private void ClampPosition(float radius)
        {
            var p = GameObject.transform.position;
            if (WaterPark.EnforceEnclosure(ref p, Rigidbody, radius))
            {
                using var log = WaterPark.AV.NewLazyAvsLog();
                //log.Debug($"Creature {Creature.NiceName()} was out of bounds at {WpCreature.transform.position}, clamping to {p}");
                GameObject.transform.position = p;
                RandomizeSwimTargetNow(radius);
            }
        }

        private void MonitorSwimTarget(float radius)
        {
            if (WpCreature.swimTarget == LastSwimTarget/* && !WpCreature.swimBehaviour.overridingTarget*/)
                return;
            using var log = WaterPark.AV.NewLazyAvsLog();
            //if (WpCreature.swimBehaviour.overridingTarget)
            //{
            //    log.Warn($"Creature {Creature.NiceName()} swim target overridden");
            //    WpCreature.swimBehaviour.EndTargetOverride();
            //}
            //if (WpCreature.swimTarget != LastSwimTarget)
            {
                log.Warn($"Creature {Creature.NiceName()} swim target changed from {LastSwimTarget} to {WpCreature.swimTarget}");

                WpCreature.swimTarget = LastSwimTarget;
                WpCreature.swimBehaviour.SwimTo(WpCreature.swimTarget, SwimVelocity);
            }
        }

        internal override void OnLateUpdate()
        {
            float radius = Radius;
            MonitorSwimTarget(radius);
            ClampPosition(radius);

            base.OnLateUpdate();
        }

        //internal override void OnFixedUpdate()
        //{
        //    float radius = Radius;

        //    base.OnFixedUpdate();
        //}

        internal override void SignalCollidersChanged(bool collidersLive)
        {
            GameObject.SetActive(collidersLive); //disable so it doesn't cause issues
        }
    }
}