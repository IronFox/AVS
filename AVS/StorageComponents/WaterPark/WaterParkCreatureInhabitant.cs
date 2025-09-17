//#define WATERPARK_DEBUG
using AVS.Log;
using AVS.Util;
using AVS.Util.Math;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        GlobalPosition? InitialPosition,
        Quaternion? InitialRotation
        )
        : WaterParkInhabitant(WaterPark, GameObject, GameObject.transform, Live, Infect, Pickupable)
    {
        public bool IsSupposedToBeHero { get; set; }
        public LocalPosition LastSwimTarget { get; private set; }
        public LocalPosition NextSwimTarget { get; private set; }
        public LocalPosition LastAppliedSwimTarget { get; private set; }
        public float InterpolationProgress { get; private set; }
        public bool SonarDetectable { get; private set; }
        public float SwimVelocity => Mathf.Lerp(WpCreature.swimMinVelocity, WpCreature.swimMaxVelocity, WpCreature.age)
            * (1f - 0.8f * Creature.Tired.Value) * CreatureScale;

        public float SwimTimeSeconds { get; private set; } = 1;
        public float CreatureScale => Mathf.Lerp(WpCreature.data.initialSize / WpCreature.data.maxSize, 1f, WpCreature.age) * WaterPark.creatureScale;

        public float BreedInterval => WpCreature.data.growingPeriod * 0.5f;
        public Vector3 CurrentScale => Vector3.one * (WpCreature.data.maxSize * CreatureScale);


#if WATERPARK_DEBUG
        private Queue<GameObject> debugSpheres = new();
        private GameObject? currentDebugSphere = null;
#endif

        private SmartLog NewLog(LogParameters? p = null, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string memberName = "")
            => WaterPark.AV.NewLazyAvsLog(tags: [Creature.name.SanitizeObjectName(), InstanceId.ToString()], parameters: p, callerFilePath: callerFilePath, memberName: memberName);
        internal override void OnInstantiate()
        {
            using var log = NewLog();
            if (WpCreature.data.IsNull())
            {
                RootTransform.localScale = Vector3.one;
                float nativeRadius = Radius;
                float maxSize = 0.5f;
                float scale = Mathf.Min(1f, maxSize / nativeRadius);
                WpCreature.data = new WaterParkCreatureData();
                WpCreature.data.maxSize = scale; //assume something hacky
            }
            RootTransform.localScale = CurrentScale;
            RootTransform.position = (InitialPosition ?? WaterPark.GetRandomLocation(false, Radius)).GlobalCoordinates;
            RootTransform.rotation = InitialRotation ?? Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

            WpCreature.pickupable = Pickupable;
            WpCreature.infectedMixin = Infect;



            var peeper = Creature as Peeper;

            if (peeper.IsNotNull() && peeper.enzymeAmount > 0)
            {
                log.Debug($"Activating enzyme visualization for {Creature.NiceName()}");

                peeper.UpdateEnzymeFX();
                peeper.enzymeParticles.Play();
                peeper.enzymeTrail.enabled = true;
                peeper.healingTrigger.SetActive(value: true);
                Infect.SetInfectedAmount(0f);
            }

            ExpectedInfectionLevel = Infect.GetInfectedAmount();
            IsSupposedToBeHero = peeper.IsNotNull() && peeper.isHero;
            log.Debug($"Instantiating creature {Creature.NiceName()} @{RootTransform.position}/{RootTransform.localPosition} with healthy={ExpectedInfectionLevel}, hero={IsSupposedToBeHero}, age={WpCreature.age}, radius={Radius.ToStr()}");

            SonarDetectable = Creature.cyclopsSonarDetectable;
            Creature.cyclopsSonarDetectable = false;



            SetInsideState();
            Creature.ScanCreatureActions();
            Creature.AllowCreatureUpdates(false);

            LastAppliedSwimTarget = NextSwimTarget = LastSwimTarget = GlobalPosition.Of(GameObject).ToLocal(WaterPark);
            WpCreature.swimTarget = LastSwimTarget.LocalCoordinates;
            WpCreature.swimBehaviour = GameObject.GetComponent<SwimBehaviour>();
            WpCreature.breedInterval = BreedInterval;
            WpCreature.ResetBreedTime();
            WpCreature.timeNextSwim = 0;
            if (WaterPark.EnforceEnclosure(Creature.transform, Radius) == MobileWaterPark.EnforcementResult.Failed)
            {
                log.Warn($"Creature {Creature.NiceName()} with radius {Radius} could not be placed inside the enclosure at {Creature.transform.position}, trying to move.");
                Creature.transform.position = WaterPark.GetRandomLocation(false, Radius).GlobalCoordinates;
                if (WaterPark.EnforceEnclosure(Creature.transform, Radius) == MobileWaterPark.EnforcementResult.Failed)
                {
                    log.Error($"Creature {Creature.NiceName()} radius {Radius} could still not be placed inside the enclosure at {Creature.transform.position}, destroying it.");
                    WaterPark.DestroyInhabitant(this);
                    return;
                }
            }
            Creature.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
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
                //rb.angularDrag = 0.5f;
            });

#if WATERPARK_DEBUG
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
#endif


            //GameObject.GetComponentsInChildren<Collider>(true).ForEach(x => x.enabled = false);

            base.OnInstantiate();
            log.Debug($"Spawning creature {Creature.NiceName()} @local={GameObject.transform.localPosition} with breed interval {WpCreature.breedInterval}");
        }

        private void SetInsideState()
        {
            using var log = NewLog();
            WpCreature.isInside = true;
            if (!GameObject.activeSelf)
            {
                GameObject.SetActive(value: true);
            }

            Animator animator = Creature.GetAnimator();
            if (animator != null)
            {
                AnimateByVelocity component = animator.GetComponent<AnimateByVelocity>();
                if (component != null)
                {
                    log.Debug($"Overriding max movement speed {component.animationMoveMaxSpeed.ToStr()} with {WpCreature.swimMaxVelocity.ToStr()}");
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

                        || type == typeof(AttackLastTarget)
                        || type == typeof(AttackCyclops)
                        || type == typeof(SwimToHeroPeeper)
                        || type == typeof(AggressiveWhenSeeTarget)
                        || type == typeof(CreatureFear)
                        || type == typeof(ConstructionObstacle)
                        || type == typeof(OnTouch)
                        || type == typeof(AggressiveOnDamage)
                        || type == typeof(AvoidTerrain)
                        || type == typeof(RangedAttackLastTarget)
                        || type == typeof(AvoidPosition)
                        || type == typeof(SeaDragonAggressiveTowardsSharks)
                        || type == typeof(SeaDragonCurrents)

                        || type == typeof(CreatureFollowPlayer)
                        || type == typeof(CreatureFriend)


                        || type == typeof(SwimToMushroom)
                        || type == typeof(Coil)

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
            using var log = NewLog();
            log.Write($"Deinstantiating creature {Creature.NiceName()} ({WpCreature.isInside}), reactivating {WpCreature.disabledBehaviours.Count} disabled behaviours");
            WpCreature.SetOutsideState();
            var component = Creature.GetAnimator().GetComponent<AnimateByVelocity>();
            log.Debug($"Max movement speed restored to {component.animationMoveMaxSpeed.ToStr()}");
            Rigidbody.SafeDo(rb =>
            {
                rb.detectCollisions = true;
            });

            Creature.enabled = true;
            Creature.cyclopsSonarDetectable = SonarDetectable;
            log.Debug($"Cyclops sonar detectable restored to {Creature.cyclopsSonarDetectable}");
            Creature.ScanCreatureActions();
            Creature.AllowCreatureUpdates(true);

            if (RootTransform.parent == WaterPark.transform)
            {
                log.Debug($"Detaching creature {Creature.NiceName()} from WaterPark");
                RootTransform.parent = null;
            }
            RootTransform.localScale = Vector3.one * WpCreature.data.outsideSize;
            Creature.Start();

#if WATERPARK_DEBUG
            foreach (var sphere in debugSpheres)
                Object.Destroy(sphere);
            debugSpheres.Clear();
            Object.Destroy(currentDebugSphere);
#endif

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
                LastAppliedSwimTarget = LocalPosition
                    .LerpUnclamped(LastSwimTarget, NextSwimTarget, InterpolationProgress);
                WpCreature.swimTarget = LastAppliedSwimTarget
                    .ToGlobal().GlobalCoordinates;
                //if (M.SqrDistance(WpCreature.swimTarget, Creature.transform.position) < (1f + Radius))
                //{
                //    WpCreature.swimTarget = Creature.transform.position + (WpCreature.swimTarget - Creature.transform.position).normalized * (1f + Radius);
                //    WaterPark.EnforceEnclosure(ref WpCreature.swimTarget, null, radius);
                //}
                WpCreature.swimBehaviour.SwimTo(WpCreature.swimTarget, SwimVelocity);
#if WATERPARK_DEBUG
                currentDebugSphere!.transform.position = WpCreature.swimTarget;
#endif
            }
        }

        private void RandomizeSwimTargetNow(float radius)
        {
            using var log = NewLog(Params.Of(radius));
            LastSwimTarget = LastAppliedSwimTarget;
            NextSwimTarget = WaterPark.GetRandomSwimTarget(
                GameObject.transform,
                radius,
                velocity: Rigidbody.SafeGet(x => (SwimVelocity + x.velocity.magnitude) / 2f, SwimVelocity)
                ).ToLocal(WaterPark);
            InterpolationProgress = 0;
            //resetSwimToIn = 0.1f;
            log.Debug($"Creature {Creature.NiceName()} swimming to {WpCreature.swimTarget} from {Creature.transform.position} (age={WpCreature.age.ToStr()}, minV={WpCreature.swimMinVelocity.ToStr()}, maxV={WpCreature.swimMaxVelocity.ToStr()}), radius={radius.ToStr()}");
            SwimTimeSeconds = WpCreature.swimInterval * UnityEngine.Random.Range(1f, 2f);
            WpCreature.timeNextSwim = Time.time + SwimTimeSeconds;
            nextTargetUpdate = 0;


#if WATERPARK_DEBUG
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = NextSwimTarget;
                sphere.transform.localScale = Vector3.one * 0.2f * CreatureScale;
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
#endif
        }

        internal override void OnUpdate()
        {
            using var log = NewLog();
            float radius = Radius;

            RootTransform.localScale = CurrentScale;

            UpdateMovement(radius);

            Creature.Hunger.Value = 1f; //disable hunger
            Creature.Scared.Value = 0f; //disable fear
            Creature.Aggression.Value = 0f; //disable aggression

            double timePassed = DayNightCycle.main.timePassed;
            if (!WpCreature.isMature)
            {
                float a = (float)(WpCreature.matureTime - (double)WpCreature.data.growingPeriod);
                WpCreature.age = Mathf.InverseLerp(a, (float)WpCreature.matureTime, (float)timePassed);
                if (WpCreature.age == 1f)
                {
                    WpCreature.isMature = true;
                    if (WpCreature.data.canBreed)
                    {
                        WpCreature.breedInterval = BreedInterval;
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
                log.Debug($"Creature {Creature.NiceName()} is ready to breed.");
                WpCreature.ResetBreedTime();
                var breedingPartner = WaterPark.GetBreedingPartner(this);
                if (breedingPartner.IsNotNull())
                {
                    breedingPartner.WpCreature.ResetBreedTime();
                    log.Debug($"Breeding {Creature.NiceName()} with {breedingPartner.Creature.NiceName()}");
                    WaterPark.AddChildOrEggSpawn(log, WpCreature.data.eggOrChildPrefab, GlobalPosition.Of(GameObject));
                }
                else
                    log.Debug($"Creature {Creature.NiceName()} could not find a breeding partner. Retrying in {WpCreature.breedInterval}");
            }

            MonitorSwimTarget(radius);
            ClampPosition(radius);

            if (ExpectedInfectionLevel != Infect.GetInfectedAmount())
            {
                log.Warn($"Creature {Creature.NiceName()} infection level changed from {ExpectedInfectionLevel} to {Infect.GetInfectedAmount()}, resetting to expected.");
                Infect.SetInfectedAmount(ExpectedInfectionLevel);
            }
            base.OnUpdate();



        }

        private void ClampPosition(float radius)
        {
            using var log = NewLog(Params.Of(radius));
            var p = GlobalPosition.Of(RootTransform);
            if (WaterPark.EnforceEnclosure(ref p, Rigidbody, radius) != MobileWaterPark.EnforcementResult.Inside)
            {
                //log.Debug($"Creature {Creature.NiceName()} was out of bounds at {WpCreature.transform.position}, clamping to {p}");
                RootTransform.position = p.GlobalCoordinates;
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
            RootTransform.localScale = CurrentScale;
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
            if (collidersLive)
            {
                //RandomizeSwimTargetNow(Radius);
            }
        }
    }
}