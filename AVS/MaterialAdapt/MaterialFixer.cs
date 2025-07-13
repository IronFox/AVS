using AVS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public ModVehicle Vehicle { get; }

        /// <summary>
        /// Controls how debug logging should be performed
        /// </summary>
        public Logging Logging { get; set; }

        /// <summary>
        /// The used material resolver function.
        /// </summary>
        public Func<IEnumerable<UnityMaterialData>> MaterialResolver { get; }

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
        /// <param name="materialResolver">The solver function to fetch all materials to translate.
        /// If null, a default implementation is used which 
        /// mimics VF's default material selection in addition to filtering out non-standard materials</param>
        /// <param name="logConfig">Log Configuration. If null, defaults to <see cref="Logging.Default" /></param>
        public MaterialFixer(
            ModVehicle owner,
            Logging? logConfig = null,
            Func<IEnumerable<UnityMaterialData>>? materialResolver = null
            )
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            Vehicle = owner;
            Logging = logConfig ?? Logging.Default;
            MaterialResolver = materialResolver ?? (() => DefaultMaterialResolver(owner, Logging));
        }

        /// <summary>
        /// Default material address resolver function. Can be modified to also return materials with divergent shader names
        /// </summary>
        /// <param name="vehicle">Owning vehicle</param>
        /// <param name="ignoreShaderNames">True to return all materials, false to only return Standard materials</param>
        /// <param name="logConfig">Log Configuration</param>
        /// <returns>Enumerable of all suitable material addresses</returns>
        public static IEnumerable<UnityMaterialData> DefaultMaterialResolver(ModVehicle vehicle, Logging logConfig, bool ignoreShaderNames = false)
        {
            var renderers = vehicle.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // copied from VF default behavior:

                // skip some materials
                if (renderer.gameObject.GetComponent<Skybox>())
                {
                    // I feel okay using Skybox as the designated "don't apply marmoset to me" component.
                    // I think there's no reason a vehicle should have a skybox anywhere.
                    // And if there is, I'm sure that developer can work around this.
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject.GetComponent<Skybox>());
                    continue;
                }
                if (renderer.gameObject.name.ToLower().Contains("light"))
                {
                    continue;
                }
                if (vehicle.Com.CanopyWindows.Contains(renderer.gameObject))
                {
                    continue;
                }

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    var material = UnityMaterialData.From(renderer, i, logConfig, ignoreShaderNames);
                    if (material != null)
                        yield return material;
                }
            }
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
                //GlassPrototype = GlassPrototype ?? MaterialPrototype.GlassFromExosuit(Logging.Verbose);

                if (HullPrototype != null /*&& GlassPrototype != null*/)
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

                        foreach (var data in MaterialResolver())
                        {
                            try
                            {
                                var materialAdaptation = new MaterialAdaptation(HullPrototype, data, shader);
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