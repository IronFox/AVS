using HarmonyLib;


namespace AVS.Patches.CompatibilityPatches
{
    /// <summary>
    /// ChameleonSubPatcher is a compatibility patch specifically created for the Chameleon mod.
    /// It ensures that AvsVehicles are built correctly despite the presence of the Chameleon sub.
    /// Without this patch, vehicles such as submarines may not complete the construction process properly
    /// and would lack essential components like fabricators.
    /// </summary>
    /// <remarks>
    /// The patch addresses issues caused by a specific transpilation chain that interferes with
    /// the crafting process of vehicles. Although the implementation does not contain any logic
    /// within the postfix method, it effectively resolves the problem.
    /// The underlying issue and its resolution through this patch are currently not fully understood.
    /// </remarks>
    [HarmonyPatch(typeof(ConstructorInput))]
    public static class ChameleonSubPatcher
    {
        /// <summary>
        /// Postfix method applied to `ConstructorInput.OnCraftingBeginAsync`.
        /// This patch is implemented to resolve compatibility issues with the Chameleon mod,
        /// ensuring that AvsVehicles, such as submarines, finish construction correctly
        /// and include essential components like fabricators.
        /// </summary>
        /// <remarks>
        /// The method itself contains no explicit logic and serves to modify or adjust
        /// the transpilation chain affecting the crafting process. The specific behavior
        /// of the original transpiler and why this method fixes the issue are currently unclear.
        /// This patch plays a critical role in maintaining proper functionality of vehicles
        /// during their construction process when the Chameleon mod is in use.
        /// </remarks>
        [HarmonyPatch(nameof(ConstructorInput.OnCraftingBeginAsync)), HarmonyPatch(MethodType.Enumerator), HarmonyPostfix]
        public static void OnCraftingBeginAsyncPostfix()
        {
        }
    }
}
