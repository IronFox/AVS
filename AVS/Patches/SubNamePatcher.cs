using AVS.BaseVehicle;
using HarmonyLib;
using UnityEngine;

// PURPOSE: Create AvsVehicle API for changing colors via normal routines (eg MoonPool terminal)
// VALUE: High.

namespace AVS.Patches
{
    /// <summary>
    /// Provides patching functionality for the SubName class by modifying its behavior when setting names
    /// and colors, enabling integration with AVS vehicles.
    /// </summary>
    /// <remarks>
    /// This class uses the Harmony library to apply postfix patches to the SubName class methods,
    /// allowing custom operations to update AVS vehicles associated with the SubName component.
    /// </remarks>
    [HarmonyPatch(typeof(SubName))]
    public class SubNamePatcher
    {
        /// <summary>
        /// A postfix method applied to the "SetName" method of the SubName class.
        /// This method ensures proper configuration of SubName decals and name painting
        /// for vehicles, specifically for submarines.
        /// </summary>
        /// <param name="__instance">The instance of the SubName class on which the original SetName method was called.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SubName.SetName))]
        public static void SubNameSetNamePostfix(SubName __instance)
        {
            AvsVehicle mv = __instance.GetComponent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            if (mv.Com.SubNameDecals.Count > 0)
            {
                SetSubNameDecals(mv);
            }
            if (mv is VehicleTypes.Submarine sub)
            {
                sub.PaintVehicleName(mv.subName.GetName(), mv.NameColor.RGB, mv.BaseColor.RGB);
            }
        }
        private static void SetSubNameDecals(AvsVehicle mv)
        {
            foreach (var tmprougui in mv.Com.SubNameDecals)
            {
                tmprougui.font = Nautilus.Utility.FontUtils.Aller_Rg;
                tmprougui.text = mv.subName.GetName();
            }
        }

        private static void SetSubNameDecalsWithColor(AvsVehicle mv, Vector3 hsb, Color color)
        {
            if (mv == null)
            {
                return;
            }

            var col = new VehicleComponents.VehicleColor(color, hsb);
            mv.SetNameColor(col);
            SetSubNameDecals(mv);
            foreach (var tmprougui in mv.Com.SubNameDecals)
            {
                tmprougui.color = color;
            }
            if (mv is VehicleTypes.Submarine sub)
            {
                sub.PaintVehicleName(mv.subName.GetName(), mv.NameColor.RGB, mv.BaseColor.RGB);
            }
        }

        /// <summary>
        /// A postfix method applied to the "SetColor" method of the SubName class.
        /// This method ensures that the appropriate colors are applied to the components of the associated vehicle, taking into account the index specified.
        /// Properly handles base colors, decals, interior colors, and stripe colors for vehicles, while also logging warnings for any unrecognized indices.
        /// </summary>
        /// <param name="__instance">The instance of the SubName class whose SetColor method is being modified.</param>
        /// <param name="index">Specifies which vehicle component should have its color set (e.g., base color, stripes, interior, or decals).</param>
        /// <param name="hsb">The HSB (hue, saturation, brightness) values used to represent the desired color.</param>
        /// <param name="color">The RGB color to be applied to the specified component.</param>
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SubName.SetColor))]
        public static void SubNameSetColorPostfix(SubName __instance, int index, Vector3 hsb, Color color)
        {
            AvsVehicle mv = __instance.GetComponent<AvsVehicle>();
            if (mv == null)
            {
                return;
            }
            var col = new VehicleComponents.VehicleColor(color, hsb);
            if (index == 0)
            {
                mv.SetBaseColor(col);
            }
            else if (index == 1)
            {
                if (mv.Com.SubNameDecals.Count > 0)
                {
                    SetSubNameDecalsWithColor(mv, hsb, color);
                }
            }
            else if (index == 2)
            {
                mv.SetInteriorColor(col);
            }
            else if (index == 3)
            {
                mv.SetStripeColor(col);
            }
            else
            {
                Logger.Warn("SubName.SetColor Error: Tried to set the color of an index that was not known!");
            }

        }
    }
}
