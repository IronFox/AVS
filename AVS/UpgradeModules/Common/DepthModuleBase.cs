using AVS.Crafting;
using System.Collections.Generic;

namespace AVS.UpgradeModules.Common
{
    internal abstract class DepthModuleBase : AvsVehicleModule
    {

        internal static List<TechType> AllDepthModuleTypes { get; } = new List<TechType>();

        /// <inheritdoc/>
        public override IReadOnlyCollection<TechType>? AutoDisplace => AllDepthModuleTypes;


        protected override void OnTechTypesAssigned(UpgradeTechTypes techTypes)
        {
            base.OnTechTypesAssigned(techTypes);
            AllDepthModuleTypes.AddRange(techTypes.AllNotNone);
        }
    }


    internal abstract class DepthModuleBase<T> : DepthModuleBase
        where T : DepthModuleBase<T>
    {
        /// <summary>
        /// Tech types of the derived depth module.
        /// </summary>
        public static UpgradeTechTypes Registered { get; private set; }

        protected override void OnTechTypesAssigned(UpgradeTechTypes techTypes)
        {
            base.OnTechTypesAssigned(techTypes);
            Registered = techTypes;
        }

    }
}