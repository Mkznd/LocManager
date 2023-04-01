using System.Xml.Linq;

namespace LocManager;

public static class TreeBuilder
{
    public static void BuildTree(List<LocEntry> entries, TreeView treeView)
    {
        treeView.Nodes.Clear();
        foreach (var entry in entries) ProcessEntry(entry, treeView);
    }

    private static void ProcessEntry(LocEntry entry, TreeView treeView)
    {
        var labels = entry.HierarchyPath.Split('-');
        var nodes = treeView.Nodes;
        var name = entry.EntryName;
        InsertToTree(labels, nodes, name);
    }

    private static void InsertToTree(IEnumerable<string> labels, TreeNodeCollection nodes, string name)
    {
        nodes = labels.Aggregate(nodes, (current, label)
            => TrySeekNodesWithTextSameAsLabel(current, label) ?? AddNewNonLeafNodeToTree(current, label));
        AddNewEntryAsLeaf(nodes, name);
    }

    private static TreeNodeCollection AddNewNonLeafNodeToTree(TreeNodeCollection nodes, string label)
    {
        var node = new TreeNode(label, imageIndex: 1, selectedImageIndex: 1);
        nodes.Add(node);
        return node.Nodes;
    }

    private static TreeNodeCollection? TrySeekNodesWithTextSameAsLabel(TreeNodeCollection nodes, string label)
    {
        for (var i = 0; i < nodes.Count; i++)
            if (nodes[i].Text == label)
                return nodes[i].Nodes;

        return null;
    }

    private static void AddNewEntryAsLeaf(TreeNodeCollection nodes, string name)
    {
        var node = new TreeNode(name, imageIndex: 0, selectedImageIndex: 0);
        nodes.Add(node);
    }
}