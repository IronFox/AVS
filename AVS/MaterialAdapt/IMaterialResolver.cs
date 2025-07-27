using System.Collections.Generic;

namespace AVS.MaterialAdapt
{
    /// <summary>
    /// Resolver to find materials for adaptation.
    /// </summary>
    public interface IMaterialResolver
    {
        /// <summary>
        /// Identifies all materials that should be adapted.
        /// </summary>
        public IEnumerable<UnityMaterialData> ResolveMaterials();
    }
}