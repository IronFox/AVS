using AVS.Log;
using UnityEngine;
using UnityEngine.Rendering;

namespace AVS.MaterialAdapt.Variables
{
    internal interface IShaderVariable
    {
        ShaderPropertyType Type { get; }

        /// <summary>
        /// Updates a material according to the preserved values present in the local variable
        /// </summary>
        /// <param name="m">Material to update</param>
        /// <param name="logConfig">Log Configuration</param>
        /// <param name="rmc">Root mod controller for logging purposes</param>
        /// <param name="materialName">Optional custom material name to use instead of the nice name of the material itself</param>
        void SetTo(RootModController rmc, Material m, MaterialLog logConfig, string? materialName);
    }

}
