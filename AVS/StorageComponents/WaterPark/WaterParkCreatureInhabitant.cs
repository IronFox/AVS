using AVS.Log;
using AVS.Util;
using AVS.Util.Math;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

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
        public Vector3 NextSwimTarget { get; private set; }
        public float InterpolationProgress { get; private set; }
        public float SwimVelocity => Mathf.Lerp(WpCreature.swimMinVelocity, WpCreature.swimMaxVelocity, WpCreature.age)
            * (1f - 0.8f * Creature.Tired.Value);

        public float SwimTimeSeconds { get; private set; } = 1;

        private Queue<GameObject> debugSpheres = new();
        private GameObject? currentDebugSphere = null;

        private SmartLog NewLog([CallerFilePath] string callerFilePath = "", [CallerMemberName] string memberName = "")
            => WaterPark.AV.NewLazyAvsLog(tags: [Creature.name.SanitizeObjectName(), InstanceId.ToString()], callerFilePath: callerFilePath, memberName: memberName);
        internal override void OnInstantiate()
        {
            using var log = NewLog();

            var peeper = Creature as Peeper;
            IsSupposedToBeHealthy = Infect.GetInfectedAmount() == 0f;
            IsSupposedToBeHero = peeper.IsNotNull() && peeper.isHero;
            log.Debug($"Instantiating creature {Creature.NiceName()} with healthy={IsSupposedToBeHealthy}, hero={IsSupposedToBeHero}, age={WpCreature.age}");

            SetInsideState();
            NextSwimTarget = LastSwimTarget = WpCreature.swimTarget = GameObject.transform.position;
            WpCreature.swimBehaviour = GameObject.GetComponent<SwimBehaviour>();
            WpCreature.breedInterval = WpCreature.data.growingPeriod * 0.5f;
            WpCreature.ResetBreedTime();
            WpCreature.timeNextSwim = 0;
            Creature.transform.position = InitialPosition ?? WaterPark.GetRandomLocation(false, Radius);
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

            {
                currentDebugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentDebugSphere.transform.position = WpCreature.swimTarget;
                currentDebugSphere.transform.localScale = Vector3.one * 0.3f;
                var mat = new Material(currentDebugSphere.GetComponent<Renderer>().material);
                //Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.yellow;
                currentDebugSphere.GetComponent<Renderer>().material = mat;
                Object.Destroy(currentDebugSphere.GetComponent<Collider>());
            }



            //GameObject.GetComponentsInChildren<Collider>(true).ForEach(x => x.enabled = false);

            base.OnInstantiate();
            log.Debug($"Spawning creature {Creature.NiceName()} @ {GameObject.transform.localPosition} with breed interval {WpCreature.breedInterval}");
        }

        private void SetInsideState()
        {
            using var log = NewLog();
            WpCreature.isInside = true;
            if (!GameObject.activeSelf)
            {
                GameObject.SetActive(value: true);
            }

            Animator animator = GameObject.GetComponent<Creature>().GetAnimator();
            if (animator != null)
            {
                AnimateByVelocity component = animator.GetComponent<AnimateByVelocity>();
                if (component != null)
                {
                    WpCreature.outsideMoveMaxSpeed = component.animationMoveMaxSpeed;
                    component.animationMoveMaxSpeed = WpCreature.swimMaxVelocity;
                }
            }

            Locomotion component2 = GameObject.GetComponent<Locomotion>();
            component2.canMoveAboveWater = true;
            WpCreature.locomotionParametersOverrode = true;
            WpCreature.locomotionDriftFactor = component2.driftFactor;
            component2.driftFactor = 0.1f;
            component2.forwardRotationSpeed = 0.6f;
            if (WpCreature.swimBehaviour != null)
            {
                WpCreature.outsideTurnSpeed = WpCreature.swimBehaviour.turnSpeed;
                WpCreature.swimBehaviour.turnSpeed = 1f;
            }

            WpCreature.disabledBehaviours = new List<Behaviour>();
            Behaviour[] componentsInChildren = WpCreature.GetComponentsInChildren<Behaviour>(includeInactive: true);
            foreach (Behaviour behaviour in componentsInChildren)
            {
                if (behaviour == null)
                {
                    log.Warn("Discarded missing behaviour on a WaterParkCreature gameObject");
                }
                else
                {
                    if (!behaviour.enabled)
                    {
                        continue;
                    }
                    bool doDisable = false;
                    Type type = behaviour.GetType();


                    if (type == typeof(SwimRandom)
                        || type == typeof(Scareable)
                        || type == typeof(AvoidObstacles)
                        || type == typeof(FleeWhenScared)
                        || type == typeof(FleeOnDamage)
                        || type == typeof(SwimToVent)
                        || type == typeof(SwimToEnzymeCloud)
                        || type == typeof(SwimInSchool)
                        || type == typeof(CreatureFlinch)
                        || type == typeof(CreatureDeath)
                        || type == typeof(StayAtLeashPosition)
                        || type == typeof(Breach)
                        || type == typeof(MoveTowardsTarget)
                        )
                        doDisable = true;

                    if (!doDisable)
                        for (int j = 0; j < WaterParkCreature.behavioursToDisableInside.Length; j++)
                        {
                            if (type.Equals(WaterParkCreature.behavioursToDisableInside[j])
                                || type.IsSubclassOf(WaterParkCreature.behavioursToDisableInside[j]))
                            {
                                doDisable = true;
                                break;
                            }
                        }
                    if (doDisable)
                    {
                        log.Debug($"Disabling behaviour {behaviour.NiceName()} on creature {Creature.NiceName()}");
                        behaviour.enabled = false;
                        WpCreature.disabledBehaviours.Add(behaviour);
                    }
                    else
                        log.Debug($"Leaving behaviour {behaviour.NiceName()} enabled on creature {Creature.NiceName()}");
                }
            }
        }

        internal override void OnDeinstantiate()
        {
            WpCreature.SetOutsideState();
            Rigidbody.SafeDo(rb =>
            {
                rb.detectCollisions = true;
            });

            foreach (var sphere in debugSpheres)
                Object.Destroy(sphere);
            debugSpheres.Clear();
            Object.Destroy(currentDebugSphere);

            base.OnDeinstantiate();
        }

        private float nextTargetUpdate = 0.1f;
        private void UpdateMovement(float radius)
        {
            if (Time.time > WpCreature.timeNextSwim)
            {
                RandomizeSwimTargetNow(radius);
            }

            InterpolationProgress += Time.deltaTime / SwimTimeSeconds;
            nextTargetUpdate -= Time.deltaTime;
            if (nextTargetUpdate <= 0f)
            {
                nextTargetUpdate = 0.1f;
                WpCreature.swimTarget = Vector3.LerpUnclamped(LastSwimTarget, NextSwimTarget, InterpolationProgress);
                //if (M.SqrDistance(WpCreature.swimTarget, Creature.transform.position) < (1f + Radius))
                //{
                //    WpCreature.swimTarget = Creature.transform.position + (WpCreature.swimTarget - Creature.transform.position).normalized * (1f + Radius);
                //    WaterPark.EnforceEnclosure(ref WpCreature.swimTarget, null, radius);
                //}
                WpCreature.swimBehaviour.SwimTo(WpCreature.swimTarget, SwimVelocity);
                currentDebugSphere!.transform.position = WpCreature.swimTarget;
            }



            //resetSwimToIn -= Time.deltaTime;
            //if (resetSwimToIn <= 0f)
            //{
            //    resetSwimToIn = 0.1f;
            //    WpCreature.swimBehaviour.SwimTo(LastSwimTarget, SwimVelocity);
            //}

        }

        //private float resetSwimToIn = 10f;
        private void RandomizeSwimTargetNow(float radius)
        {
            using var log = NewLog();
            LastSwimTarget = WpCreature.swimTarget;
            NextSwimTarget = WaterPark.GetRandomSwimTarget(
                GameObject.transform,
                radius,
                velocity: Rigidbody.SafeGet(x => (SwimVelocity + x.velocity.magnitude) / 2f, SwimVelocity)
                );
            InterpolationProgress = 0;
            //resetSwimToIn = 0.1f;
            log.Debug($"Creature {Creature.NiceName()} swimming to {WpCreature.swimTarget} from {Creature.transform.position} (age={WpCreature.age}, minV={WpCreature.swimMinVelocity}, maxV={WpCreature.swimMaxVelocity})");
            SwimTimeSeconds = WpCreature.swimInterval * UnityEngine.Random.Range(1f, 2f);
            WpCreature.timeNextSwim = Time.time + SwimTimeSeconds;
            nextTargetUpdate = 0;


            //if (WaterPark.AV.DebugMode)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = NextSwimTarget;
                sphere.transform.localScale = Vector3.one * 0.2f;
                var mat = new Material(sphere.GetComponent<Renderer>().material);
                //Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.cyan;
                sphere.GetComponent<Renderer>().material = mat;
                Object.Destroy(sphere.GetComponent<Collider>());
                debugSpheres.Enqueue(sphere);
                while (debugSpheres.Count > 5)
                    Object.Destroy(debugSpheres.Dequeue());
                foreach (var s in debugSpheres)
                {
                    var c = s.GetComponent<Renderer>().material.color;
                    c = M.ScaleRGB(c, 0.6f);
                    s.GetComponent<Renderer>().material.color = c;
                }
            }
        }

        internal override void OnUpdate()
        {
            float radius = Radius;

            UpdateMovement(radius);

            Creature.Hunger.Value = 1f; //disable hunger
            Creature.Scared.Value = 0f; //disable fear
            Creature.Aggression.Value = 0f; //disable aggression

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
                using var log = NewLog();
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
            if (WaterPark.EnforceEnclosure(ref p, Rigidbody, radius) != MobileWaterPark.EnforcementResult.Inside)
            {
                using var log = NewLog();
                //log.Debug($"Creature {Creature.NiceName()} was out of bounds at {WpCreature.transform.position}, clamping to {p}");
                GameObject.transform.position = p;
                RandomizeSwimTargetNow(radius);
            }
        }

        private void MonitorSwimTarget(float radius)
        {
            //if (WpCreature.swimTarget == LastSwimTarget/* && !WpCreature.swimBehaviour.overridingTarget*/)
            //    return;
            //using var log = NewLog();
            ////if (WpCreature.swimBehaviour.overridingTarget)
            ////{
            ////    log.Warn($"Creature {Creature.NiceName()} swim target overridden");
            ////    WpCreature.swimBehaviour.EndTargetOverride();
            ////}
            ////if (WpCreature.swimTarget != LastSwimTarget)
            //{
            //    log.Warn($"Creature {Creature.NiceName()} swim target changed from {LastSwimTarget} to {WpCreature.swimTarget}");

            //    WpCreature.swimTarget = LastSwimTarget;
            //    WpCreature.swimBehaviour.SwimTo(WpCreature.swimTarget, SwimVelocity);
            //}
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