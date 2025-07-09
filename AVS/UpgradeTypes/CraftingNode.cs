
namespace AVS.UpgradeTypes
{
    public readonly struct CraftingNode
    {
        public string Name { get; }
        public string DisplayName { get; }
        public Atlas.Sprite Icon { get; }
        public CraftingNode(string name, string displayName, Atlas.Sprite icon)
        {
            Name = name;
            DisplayName = displayName;
            Icon = icon;
        }
    }
}
