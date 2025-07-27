namespace AVS.Crafting
{
    /// <summary>
    /// A node declaration in the crafting interface, representing a tab or module.
    /// </summary>
    public readonly struct CraftingNode
    {
        /// <summary>
        /// The name of the crafting node, used for identification.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the display name of the entity.
        /// </summary>
        public string DisplayName { get; }
        /// <summary>
        /// Gets the icon associated with this item.
        /// </summary>
        public Atlas.Sprite Icon { get; }
        /// <summary>
        /// Constructs a new instance of the <see cref="CraftingNode"/> struct.
        /// </summary>
        public CraftingNode(string name, string displayName, Atlas.Sprite icon)
        {
            Name = name;
            DisplayName = displayName;
            Icon = icon;
        }
    }
}
