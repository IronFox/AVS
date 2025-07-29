using AVS.Crafting;
using System;
using System.Collections.Generic;

namespace AVS.UpgradeModules
{
    /// <summary>
    /// Folder in the crafting tree.
    /// Can only contain either folders or upgrade modules, not both.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// The parent folder of this folder, if any.
        /// </summary>
        public Node? Parent { get; }

        /// <summary>
        /// The name of the folder, used for identification.
        /// Must not be empty, must not contain slashes, and must be unique within the parent folder.
        /// </summary>
        public string Identifier { get; }
        /// <summary>
        /// Gets the display name of the folder.
        /// </summary>
        public string DisplayName { get; }
        /// <summary>
        /// Gets the icon associated with this folder.
        /// Should not be null but loading may have failed, so it is nullable.
        /// </summary>
        public Atlas.Sprite? Icon { get; }

        internal Dictionary<string, Node> Children { get; } = new Dictionary<string, Node>();
        internal List<AvsVehicleUpgrade> Modules { get; } = new List<AvsVehicleUpgrade>();


        private Node(
            string identifier,
            string displayName,
            Atlas.Sprite? icon,
            Node? parent = null)
        {
            Identifier = identifier;
            DisplayName = displayName;
            Icon = icon;
            Parent = parent;
        }

        /// <summary>
        /// Creates a new folder with the specified name, display name, and ideally icon, and adds it to the specified
        /// parent folder if provided.
        /// </summary>
        /// <remarks>If a folder with the same path already exists, the existing folder is returned
        /// instead of creating a new one.</remarks>
        /// <param name="name">The unique name of the folder to be created. This name is used as an identifier within the folder path.</param>
        /// <param name="displayName">The display name of the folder, which is shown in the user interface.</param>
        /// <param name="icon">An icon representing the folder. If not provided, the folder will have no icon.</param>
        /// <param name="parent">The parent folder to which the new folder will be added. If <see langword="null"/>, the folder is created at
        /// the root level.</param>
        /// <returns>The newly created <see cref="Node"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the specified parent folder already contains modules, preventing the addition of a new folder.</exception>
        /// <exception cref="ArgumentException">Thrown if the provided name is null, empty, or contains slashes.</exception>
        public static Node Create(
            string name,
            string displayName,
            Atlas.Sprite? icon,
            Node? parent = null)
        {
            name = CraftPath.Sanitize(name);
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Folder name cannot be null or empty.", nameof(name));
            return CraftTreeHandler.GetOrCreateNode(name, parent, () => new Node(name, displayName, icon, parent));
        }


        /// <summary>
        /// Registers an upgrade to the local node node with the given compatibility settings.
        /// </summary>
        /// <param name="upgrade">The upgrade the register.</param>
        /// <param name="compat">The compatibility settings for the upgrade. Defaults to the standard compatibility if not specified.</param>
        /// <returns>The type of upgrade technology registered for the node.</returns>
        public UpgradeTechTypes RegisterUpgrade(AvsVehicleUpgrade upgrade, UpgradeCompat compat = default)
        {
            upgrade.SetNode(this);
            return UpgradeRegistrar.RegisterUpgrade(
                this,
                upgrade,
                compat);
        }

        private void WriteTo(List<string> list)
        {
            if (Parent != null)
                Parent.WriteTo(list);
            list.Add(Identifier);
        }

        /// <summary>
        /// Retrieves the name path of this folder in the crafting tree, starting from the root.
        /// </summary>
        public CraftPath GetPath()
        {
            if (Parent == null)
            {
                return new CraftPath(Identifier);
            }
            else
            {
                List<string> list = new List<string>();
                WriteTo(list);
                return new CraftPath(list);
            }
        }
    }
}
