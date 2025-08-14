using AVS.Assets;
using AVS.Log;
using AVS.UpgradeModules;
using System;
using System.Collections.Generic;
//using AVS.Localization;

namespace AVS.Crafting
{
    internal static class CraftTreeHandler
    {

        private static Dictionary<string, Node> KnownRootNodes { get; } = new Dictionary<string, Node>();

        internal static Node GetOrCreateNode(
            string name,
            Node? parent,
            Func<Node> folderFactory)
        {

            Node? existing = null;
            if (parent is null)
            {
                if (KnownRootNodes.TryGetValue(name, out existing))
                    return existing;
            }
            else
            {
                if (parent.Children.TryGetValue(name, out existing))
                    return existing;
            }

            if (parent != null)
            {
                if (parent.Modules.Count > 0)
                    throw new InvalidOperationException($"CraftTreeHandler: Cannot add a folder to a path that already contains modules. Path: {parent.GetPath()}");
            }
            var folder = folderFactory();
            var parentPath = (parent?.GetPath() ?? CraftPath.Empty);

            LogWriter.Default.Write($"CraftTreeHandler: Adding crafting tab {name} with display name {folder.DisplayName} in path {parentPath}");
            Nautilus.Handlers.CraftTreeHandler.AddTabNode(AvsFabricator.TreeType, name, folder.DisplayName, folder.Icon, parentPath.Segments);

            if (parent != null)
                parent.Children.Add(name, folder);
            else
                KnownRootNodes.Add(name, folder);
            return folder;
        }

    }
}
