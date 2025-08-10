#define PLAIN_COPY_GLASS
using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.MaterialAdapt
{


    /// <summary>
    /// Helper class to fix materials automatically. Should be instantiated on the vehicle
    /// you wish to fix materials of
    /// </summary>
    /// <author>https://github.com/IronFox</author>
    public class MaterialFixer
    {

        private float repairMaterialsInSeconds = float.MaxValue;
        private bool doRepairMaterialsPostUndock;
        private int repairMaterialsInFrames = 3;
        private bool materialsFixed;
        private readonly List<MaterialAdaptation> adaptations = new List<MaterialAdaptation>();

        /// <summary>
        /// True if materials have been fixed at least once.
        /// </summary>
        public bool MaterialsAreFixed => materialsFixed;

        /// <summary>
        /// The owning vehicle.
        /// </summary>
        public AvsVehicle Vehicle { get; }

        /// <summary>
        /// Controls how debug logging should be performed
        /// </summary>
        public MaterialLog Logging { get; set; }

        /// <summary>
        /// The used material resolver.
        /// </summary>
        public IMaterialResolver MaterialResolver { get; }

        /// <summary>
        /// Null or in [0,1].<br/>
        /// If non-null, enforces the same uniform shininess level on all materials
        /// </summary>
        public float? UniformShininess { get; set; }
        private float? uniformShininess;

        /// <summary>
        /// Constructs the instance
        /// </summary>
        /// <param name="owner">Owning vehicle</param>
        /// <param name="materialResolver">The solver to fetch all materials to translate.</param>
        /// <param name="logConfig">Log Configuration. If null, defaults to <see cref="MaterialLog.Default" /></param>
        public MaterialFixer(
            AvsVehicle owner,
            IMaterialResolver materialResolver,
            MaterialLog? logConfig = null
            )
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            Vehicle = owner;
            Logging = logConfig ?? MaterialLog.Default;
            MaterialResolver = materialResolver;
        }


        /// <summary>
        /// Notifies that the vehicle has just undocked from a docking bay (moonpool, etc).
        /// </summary>
        /// <remarks>Should be called from your vehicle OnVehicleUndocked() method</remarks>
        public void OnVehicleUndocked()
        {
            repairMaterialsInSeconds = 0.2f;
            repairMaterialsInFrames = 1;
            doRepairMaterialsPostUndock = true;
        }

        /// <summary>
        /// Notifies that the vehicle has just docked to a docking bay (moonpool, etc).
        /// </summary>
        /// <remarks>Should be called from your vehicle OnVehicleDocked() method</remarks>
        public void OnVehicleDocked() => OnVehicleUndocked();


        /// <summary>
        /// Forcefully reapplies all material adaptations.
        /// Normally not necessary
        /// </summary>
        public void ReApply()
        {
            //Logging.LogExtraStep($"Reapplying {adaptations.Count} material adaptations");
            foreach (MaterialAdaptation adaptation in adaptations)
            {
                Logging.LogExtraStep($"Reapplying {adaptation.Target.GetMaterial().NiceName()}");
                adaptation.ApplyToTarget(Logging);
            }
        }

        private SubnauticaMaterialPrototype? HullPrototype { get; set; }
#if PLAIN_COPY_GLASS
        /// <summary>
        /// There is something odd with the glass material in Subnautica.
        /// Even if we copy everything, the glass material is oddly opaque.
        /// If, instead, we use the material directly, it looks way more
        /// convincing. My best guess is, the material is not completely
        /// loaded when we copy it, and some part of Subnautica changes it
        /// at a later point.
        /// </summary>
        private bool HaveGlassMaterial { get; set; }
        private Material? glassMaterial;
#else
        private SubnauticaMaterialPrototype? GlassPrototype { get; set; }
        private bool HaveGlassMaterial => GlassPrototype != null;
#endif

        /// <summary>
        /// Fixes materials if necessary/possible.
        /// Also fixes undock material changes if <see cref="OnVehicleUndocked"/> was called before
        /// </summary>
        /// <remarks>Should be called from your vehicle Update() method</remarks>
        /// <param name="subTransform">Root transform of your sub</param>
        public bool OnUpdate()
        {
            bool anyChanged = false;

            if (!materialsFixed)
            {
                HullPrototype = HullPrototype ?? SubnauticaMaterialPrototype.FromSeamoth(Logging);
#if PLAIN_COPY_GLASS
                if (!HaveGlassMaterial)
                    HaveGlassMaterial = SubnauticaMaterialPrototype.GlassMaterialFromSeamoth(out glassMaterial, Logging);
#else
                GlassPrototype = GlassPrototype ?? SubnauticaMaterialPrototype.GlassFromSeamoth(Logging);
#endif

                if (HullPrototype != null && HaveGlassMaterial)
                {
                    materialsFixed = true;
                    uniformShininess = UniformShininess;



                    if (HullPrototype.IsEmpty)
                    {
                        Logging.Error($"No material prototype found on Seamoth");
                    }
                    else
                    {
                        var shader = Shaders.FindMainShader();
                        if (shader == null)
                        {
                            Logging.Error($"No main shader found. Cannot adapt materials");
                            return false;
                        }

                        foreach (var data in MaterialResolver.ResolveMaterials())
                        {
                            try
                            {
                                MaterialAdaptation materialAdaptation;

                                switch (data.Type)
                                {
                                    case MaterialType.Opaque:
                                        materialAdaptation = new MaterialAdaptation(HullPrototype, data, shader);
                                        break;
                                    case MaterialType.Glass:
#if PLAIN_COPY_GLASS
                                        if (glassMaterial == null)
                                        {
                                            Logging.Error($"Glass material is null. Cannot adapt glass material {data}");
                                            continue;
                                        }
                                        data.Source.Renderer.ReplaceMaterial(data.Source.MaterialIndex, glassMaterial);
                                        continue;
#else
                                        materialAdaptation = new MaterialAdaptation(GlassPrototype, data, shader);
                                        break;
#endif
                                    default:
                                        Logging.Warn($"Unknown material type for material {data}. Skipping adaptation");
                                        continue;
                                }
                                //= new MaterialAdaptation(HullPrototype, data, shader);
                                materialAdaptation.ApplyToTarget(Logging, uniformShininess);

                                adaptations.Add(materialAdaptation);
                            }
                            catch (Exception ex)
                            {
                                Logging.Error($"Adaptation failed for material {data}: {ex}");
                                Debug.LogException(ex);
                            }
                        }
                        //foreach (var data in GlassMaterialResolver())
                        //{
                        //    try
                        //    {
                        //        var materialAdaptation = new MaterialAdaptation(GlassPrototype, data, shader);
                        //        materialAdaptation.ApplyToTarget(Logging);

                        //        adaptations.Add(materialAdaptation);
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        Logging.LogError($"Adaptation failed for glass material {data}: {ex}");
                        //        Debug.LogException(ex);
                        //    }
                        //}
                        Logging.LogExtraStep($"All done. Applied {adaptations.Count} adaptations");
                    }
                }
                anyChanged = true;

            }
            else if (uniformShininess != UniformShininess)
            {
                uniformShininess = UniformShininess;
                foreach (MaterialAdaptation adaptation in adaptations)
                {
                    adaptation.ApplyToTarget(Logging, uniformShininess);
                }
                anyChanged = true;
            }

            if (doRepairMaterialsPostUndock)
            {
                repairMaterialsInSeconds -= Time.deltaTime;
                if (repairMaterialsInSeconds < 0 && --repairMaterialsInFrames == 0)
                {
                    repairMaterialsInSeconds = float.MaxValue;
                    doRepairMaterialsPostUndock = false;
                    Logging.LogExtraStep($"Undocked. Resetting materials");
                    foreach (MaterialAdaptation adaptation in adaptations)
                        adaptation.PostDockFixOnTarget(Logging);
                    anyChanged = true;
                }
            }
            return anyChanged;
        }
    }
}