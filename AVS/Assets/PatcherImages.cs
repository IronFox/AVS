using UnityEngine;

namespace AVS.Assets
{
    /// <summary>
    /// Represents a collection of images used by AVS.
    /// </summary>
    /// <param name="DepthModule1Icon">The icon for the Depth Module 1 upgrade.</param>
    /// <param name="DepthModule2Icon">The icon for the Depth Module 2 upgrade.</param>
    /// <param name="DepthModule3Icon">The icon for the Depth Module 3 upgrade.</param>
    /// <param name="DepthModuleNodeIcon">The icon to use for the parent node of all depth modules in the crafting tree.</param>
    /// <param name="FabricatorIcon">Icon used by the AVS fabricator.</param>
    /// <param name="ModulesBackground">Background image for the updates module panel.</param>
    public record PatcherImages(
        Sprite DepthModule1Icon,
        Sprite DepthModule2Icon,
        Sprite DepthModule3Icon,
        Sprite DepthModuleNodeIcon,
        Sprite FabricatorIcon,
        Sprite ModulesBackground
    );
}
