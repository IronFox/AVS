using HarmonyLib;
using UnityEngine;
using AVS.Util;

// PURPOSE: ensures building ghosts truly attach to Submarines, and in a non-problematic way
// VALUE: High.

namespace AVS.Patches;

/// <summary>
/// A Harmony patch class for modifying the behavior of the BuilderTool component in Unity.
/// </summary>
/// <remarks>
/// This patch ensures that constructed building ghosts properly attach to the submarine
/// they are being built in and addresses several potential issues such as ensuring correct
/// parenting and removing unwanted components.
/// </remarks>
[HarmonyPatch(typeof(BuilderTool))]
public class BuilderToolPatcher
{
    /// <summary>
    /// Ensures that building ghosts are properly attached to the Submarine they are built in and removes unwanted components.
    /// </summary>
    /// <param name="__instance">The instance of the BuilderTool being used.</param>
    /// <param name="c">The Constructable object representing the building ghost being constructed.</param>
    /// <param name="state">The state of the construction (e.g., whether it has started or completed).</param>
    /// <param name="start">Indicates whether the construction process is starting.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(BuilderTool.Construct))]
    public static void ConstructPostfix(BuilderTool __instance, Constructable c, bool state, bool start)
    {
        if (Player.main.IsNotNull())
        {
            var subroot = Player.main.currentSub;
            if (subroot.IsNotNull() && subroot.GetComponent<VehicleTypes.Submarine>().IsNotNull() && c.IsNotNull())
            {
                if (c.gameObject.GetComponent<LargeWorldEntity>().IsNotNull())
                    c.gameObject.GetComponent<LargeWorldEntity>().cellLevel = LargeWorldEntity.CellLevel.Global;
                c.gameObject.transform.SetParent(subroot.gameObject.transform);
                if (c.gameObject.GetComponent<Rigidbody>())
                    // The architect fabricator from RotA, for example, has a rigidbody for some reason.
                    Object.DestroyImmediate(c.gameObject.GetComponent<Rigidbody>());
            }
        }
    }
}