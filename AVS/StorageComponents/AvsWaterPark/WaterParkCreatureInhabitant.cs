//#define WATERPARK_DEBUG
using AVS.Log;
using AVS.Util;
using AVS.Util.Math;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AVS.StorageComponents.AvsWaterPark
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
        Quaternion? InitialRotation,
        float? InitialInfection
        )
        : WaterParkInhabitant(WaterPark, GameObject, GameObject.transform, Live, Infect, Pickupable)
    {

        public override string ToString()
            => Creature.NiceName();
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

        public Crash? Crash { get; private set; }

        public bool WillNotSwimOnItsOwn => Crash.IsNotNull();

        private float SpawnAge { get; set; } = 0f;

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
            log.Debug($"Creature radius={Radius.ToStr()}, creatureScale={CreatureScale.ToStr()}, waterpark scale={WaterPark.creatureScale.ToStr()}");
            RootTransform.position = (InitialPosition ?? WaterPark.GetRandomLocation(false, Radius)).GlobalCoordinates;
            RootTransform.rotation = InitialRotation ?? Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

            WpCreature.pickupable = Pickupable;
            WpCreature.infectedMixin = Infect;

            if (Creature is not CrabSnake)
            {
                Creature.enabled = false;
                EmulateCreatureStart();
            }
            RootTransform.localScale = CurrentScale;


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

            ExpectedInfectionLevel = InitialInfection ?? Infect.GetInfectedAmount();
            if (InitialInfection.IsNotNull() && Infect.GetInfectedAmount() != InitialInfection.Value)
            {
                log.Debug($"Resetting current infection of {Creature.NiceName()} to from {Infect.GetInfectedAmount()} to {InitialInfection}");
                Infect.SetInfectedAmount(InitialInfection.Value);
            }
            IsSupposedToBeHero = peeper.IsNotNull() && peeper.isHero;
            log.Debug($"Instantiating creature {Creature.NiceName()} @{RootTransform.position}/{RootTransform.localPosition} with infect={ExpectedInfectionLevel}, hero={IsSupposedToBeHero}, age={WpCreature.age}, radius={Radius.ToStr()}");

            SonarDetectable = Creature.cyclopsSonarDetectable;
            Creature.cyclopsSonarDetectable = false;



            SetInsideState();
            RootTransform.localScale = CurrentScale;

            //Creature.ScanCreatureActions();
            //Creature.AllowCreatureUpdates(false);

            var p = GlobalPosition.Of(RootTransform);
            var rs = WaterPark.EnforcePointEnclosure(p, null, Radius);
            if (rs.Result != MobileWaterPark.EnforcementResult.Inside)
                p = rs.Position;
            LastAppliedSwimTarget = NextSwimTarget = LastSwimTarget = p.ToLocal(WaterPark);
            WpCreature.swimTarget = LastSwimTarget.ToGlobal().GlobalCoordinates;
            WpCreature.swimBehaviour = GameObject.GetComponent<SwimBehaviour>();
            WpCreature.breedInterval = BreedInterval;
            WpCreature.ResetBreedTime();
            WpCreature.timeNextSwim = 0;
            if (WaterPark.EnforceTransformEnclosure(Creature.transform, Radius) == MobileWaterPark.EnforcementResult.Failed)
            {
                log.Warn($"Creature {Creature.NiceName()} with radius {Radius.ToStr()} could not be placed inside the enclosure at {Creature.transform.position}, trying to move.");
                Creature.transform.position = WaterPark.GetRandomLocation(false, Radius).GlobalCoordinates;
                if (WaterPark.EnforceTransformEnclosure(Creature.transform, Radius) == MobileWaterPark.EnforcementResult.Failed)
                {
                    log.Error($"Creature {Creature.NiceName()} radius {Radius.ToStr()} could still not be placed inside the enclosure at {Creature.transform.position}, destroying it.");
                    WaterPark.DestroyInhabitant(this);
                    return;
                }
            }
            RootTransform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
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
                if (WillNotSwimOnItsOwn)
                    rb.isKinematic = true;
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
            log.Debug($"Locomotion: maxAcceleration={WpCreature.swimBehaviour.splineFollowing.locomotion.maxAcceleration.ToStr()}");
            log.Debug($"Locomotion: maxVelocity={WpCreature.swimBehaviour.splineFollowing.locomotion.maxVelocity.ToStr()}");
            log.Debug($"Locomotion: forwardRotationSpeed={WpCreature.swimBehaviour.splineFollowing.locomotion.forwardRotationSpeed.ToStr()}");
            log.Debug($"Locomotion: upRotationSpeed={WpCreature.swimBehaviour.splineFollowing.locomotion.upRotationSpeed.ToStr()}");
            log.Debug($"Locomotion: driftFactor={WpCreature.swimBehaviour.splineFollowing.locomotion.driftFactor.ToStr()}");
            log.Debug($"Locomotion: canMoveAboveWater={WpCreature.swimBehaviour.splineFollowing.locomotion.canMoveAboveWater}");
            log.Debug($"Locomotion: canWalkOnSurface={WpCreature.swimBehaviour.splineFollowing.locomotion.canWalkOnSurface}");
            log.Debug($"Locomotion: freezeHorizontalRotation={WpCreature.swimBehaviour.splineFollowing.locomotion.freezeHorizontalRotation}");
            log.Debug($"Locomotion: rotateToSurfaceNormal={WpCreature.swimBehaviour.splineFollowing.locomotion.rotateToSurfaceNormal}");
            log.Debug($"Locomotion: acceleration={WpCreature.swimBehaviour.splineFollowing.locomotion.acceleration}");
            log.Debug($"Locomotion: enabled={WpCreature.swimBehaviour.splineFollowing.locomotion.enabled}");
            log.Debug($"Locomotion: rb constraints={WpCreature.swimBehaviour.splineFollowing.locomotion.useRigidbody.constraints}");
            log.Debug($"Swim velocity={SwimVelocity}");
            RandomizeSwimTargetNow(Radius);
        }

        private void EmulateCreatureStart()
        {
            using var log = NewLog();
            bool flag = !Creature.isInitialized && Creature.Size < 0f;
            float magnitude = (RootTransform.localScale - Vector3.one).magnitude;
            if (flag && !Utils.NearlyEqual(magnitude, 0f))
            {
                RootTransform.localScale = Vector3.one;
            }

            GrowMixin component = GameObject.GetComponent<GrowMixin>();
            if ((bool)component)
            {
                component.growScalarChanged.AddHandler(GameObject, Creature.OnGrowChanged);
            }
            else if (flag && Creature.sizeDistribution != null)
            {
                float size = Mathf.Clamp01(Creature.sizeDistribution.Evaluate(UnityEngine.Random.value));
                Creature.SetSize(size);
            }

            TechType techType = CraftData.GetTechType(GameObject);
            if (techType != 0)
            {
                Creature.techTypeHash = UWE.Utils.SDBMHash(techType.AsString());
            }
            else
            {
                log.Error($"Creature: Couldn't find tech type for creature name: {GameObject.name}");
            }

            Creature.ScanCreatureActions();
            if (Creature.isInitialized)
            {
                Creature.InitializeAgain();
            }
            else
            {
                Creature.InitializeOnce();
                Creature.isInitialized = true;
            }



            //DeferredSchedulerUtils.Schedule(this);
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

            if (Creature is Crash crash)
                Crash = crash;
            //{
            //    Crash = crash;
            //    log.Debug($"Creature is crash");
            //    if (!crash.waterParkCreature)
            //    {
            //        log.Warn($"Setting Crash.waterParkCreature component");
            //        crash.waterParkCreature = WpCreature;
            //    }
            //    if (!WpCreature.bornInside)
            //    {
            //        log.Warn($"Creature Crash was not born inside, setting bornInside=true");
            //        WpCreature.bornInside = true;
            //    }
            //    Crash.CancelInvoke("Inflate");
            //    Crash.CancelInvoke("AnimateInflate");
            //    Crash.CancelInvoke("Detonate");
            //}

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

                        || type == typeof(AggressiveWhenSeePlayer)
                        || type == typeof(ProtectCrashHome)

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
                        log.Debug($"Leaving behaviour {behaviour.NiceName()} enabled({behaviour.enabled}) on creature {Creature.NiceName()}");
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
                if (WillNotSwimOnItsOwn)
                    rb.isKinematic = false;
            });

            GameObject.name.Replace(NameTag, "");


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
            if (nextTargetUpdate <= 0f || WillNotSwimOnItsOwn)
            {
                nextTargetUpdate = 0.1f;
                LastAppliedSwimTarget = LocalPosition
                    .Lerp(LastSwimTarget, NextSwimTarget, InterpolationProgress);
                var target = LastAppliedSwimTarget.ToGlobal();
                var rs = WaterPark.EnforcePointEnclosure(target, null, radius);
                if (rs.Result != MobileWaterPark.EnforcementResult.Inside)
                {
                    using var log = NewLog(Params.Of(radius));
                    log.Warn($"Creature {Creature.NiceName()} swim target {target.ToLocal(WaterPark)}->{rs.Position.ToLocal(WaterPark)} was not in WP confines.");

                    var rs2 = WaterPark.EnforcePointEnclosure(NextSwimTarget.ToGlobal(), null, radius);
                    if (rs2.Result != MobileWaterPark.EnforcementResult.Inside)
                    {
                        log.Warn($"Creature {Creature.NiceName()} next swim target {NextSwimTarget}->{rs2.Position.ToLocal(WaterPark)} was not in WP confines");
                    }
                    rs2 = WaterPark.EnforcePointEnclosure(LastSwimTarget.ToGlobal(), null, radius);
                    if (rs2.Result != MobileWaterPark.EnforcementResult.Inside)
                    {
                        log.Warn($"Creature {Creature.NiceName()} last swim target {LastSwimTarget}->{rs2.Position.ToLocal(WaterPark)} was not in WP confines");
                    }
                    target = rs.Position;
                }
                WpCreature.swimTarget = target.GlobalCoordinates;
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

            if (WillNotSwimOnItsOwn)
            {
                var target = WpCreature.swimTarget;
                var delta = target - RootTransform.position;
                if (delta.sqrMagnitude > 0.01f)
                    RootTransform.position += delta.normalized * SwimVelocity * Time.deltaTime;
                RootTransform.LookAt(target, Vector3.up);
            }
        }

        private void RandomizeSwimTargetNow(float radius)
        {
            using var log = NewLog(Params.Of(radius));
            LastSwimTarget = LastAppliedSwimTarget;
            var t = WaterPark.GetRandomSwimTarget(
                GameObject.transform,
                radius,
                velocity: Rigidbody.SafeGet(x => (SwimVelocity + x.velocity.magnitude) / 2f, SwimVelocity)
                );
            var c = WaterPark.EnforcePointEnclosure(t, null, radius);
            if (c.Result != MobileWaterPark.EnforcementResult.Inside)
            {
                log.Warn($"Creature {Creature.NiceName()} random swim target {t.ToLocal(WaterPark)}->{c.Position.ToLocal(WaterPark)} was not in WP confines, using enforced position.");
                t = c.Position;
            }
            NextSwimTarget = t.ToLocal(WaterPark);
            InterpolationProgress = 0;
            //resetSwimToIn = 0.1f;
            log.Debug($"Creature {Creature.NiceName()} swimming to {WpCreature.swimTarget} from {GlobalPosition.Of(RootTransform).ToLocal(WaterPark)} (age={WpCreature.age.ToStr()}, radius={radius.ToStr()}");
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

            //if (Crash.IsNotNull())
            //{
            //    Crash.CancelInvoke("Inflate");
            //    Crash.CancelInvoke("AnimateInflate");
            //    Crash.CancelInvoke("Detonate");
            //}

            //Creature.Hunger.Value = 1f; //disable hunger
            //Creature.Scared.Value = 0f; //disable fear
            //Creature.Aggression.Value = 0f; //disable aggression

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
            //else if (SpawnAge < 5f)
            {
                //log.Debug($"Force-updating infections of {Infect.materials?.Count ?? 0} material(s)");

                if (Infect.materials.IsNotNull())
                {
                    foreach (var material in Infect.materials)
                    {
                        material.SetFloat(ShaderPropertyID._InfectionAmount, ExpectedInfectionLevel);
                        if (ExpectedInfectionLevel > 0)
                        {
                            if (!material.IsKeywordEnabled(Infect.shaderKeyWord))
                            {
                                log.Warn($"Creature {Creature.NiceName()} infection shader keyword {Infect.shaderKeyWord} on {material.NiceName()} was not enabled, enabling it.");
                                material.EnableKeyword(Infect.shaderKeyWord);
                            }
                        }
                        else
                        {
                            if (material.IsKeywordEnabled(Infect.shaderKeyWord))
                            {
                                log.Warn($"Creature {Creature.NiceName()} infection shader keyword {Infect.shaderKeyWord} on {material.NiceName()} was enabled, disabling it.");
                                material.DisableKeyword(Infect.shaderKeyWord);
                            }
                        }
                    }
                }
                if (ExpectedInfectionLevel == 0)
                {
                    foreach (var renderer in GameObject.GetComponentsInChildren<Renderer>())
                    {
                        foreach (var material in renderer.materials)
                        {
                            //material.SetFloat(ShaderPropertyID._InfectionAmount, ExpectedInfectionLevel);
                            if (material.IsKeywordEnabled(Infect.shaderKeyWord))
                            {
                                log.Warn($"Creature {Creature.NiceName()} infection shader keyword {Infect.shaderKeyWord} on {material.NiceName()} was enabled, disabling it.");
                                material.DisableKeyword(Infect.shaderKeyWord);
                            }
                        }
                    }
                }
            }
            base.OnUpdate();

            SpawnAge += Time.deltaTime;


        }

        private void ClampPosition(float radius)
        {
            using var log = NewLog(Params.Of(radius));
            var p = GlobalPosition.Of(RootTransform);
            var rs = WaterPark.EnforcePointEnclosure(p, Rigidbody, radius);
            if (rs.Result != MobileWaterPark.EnforcementResult.Inside)
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

        internal static bool IsLikelyWaterParkCreature(GameObject? x, MobileWaterPark checkFor)
        {
            if (x.IsNull())
                return false;

            var pu = x.GetComponent<Pickupable>();
            if (pu.IsNull())
                return false;
            var wpc = x.GetComponent<WaterParkCreature>();
            if (wpc.IsNull())
                return false;
            if (wpc.data.IsNull())
                return false;
            if (!wpc.data.isPickupableOutside)
            {
                return !WaterParkInhabitant.IsForeign(x, checkFor);
            }
            else
                if (!WaterParkInhabitant.IsLikely(x, checkFor))
                return false;   //no way to identify
            return true;
        }
    }
}