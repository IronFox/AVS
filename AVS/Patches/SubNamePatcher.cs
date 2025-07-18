using AVS.BaseVehicle;
using HarmonyLib;
using UnityEngine;

// PURPOSE: Create ModVehicle API for changing colors via normal routines (eg MoonPool terminal)
// VALUE: High.

namespace AVS.Patches
{
    [HarmonyPatch(typeof(SubName))]
    public class SubNamePatcher
    {
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
