using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace LocManager;

public partial class Form1 : Form
{
    private const string newGroupString = "<NEW GROUP>";
    private const string rootString = "<ROOT>";
    private const string debugString = "Debug";

    private readonly List<LocEntry> _entries = new();
    private TreeNode? selectedNode = null;
    private string? selectedLang = null;

    //https://stackoverflow.com/questions/1167361/how-do-i-convert-an-enum-to-a-list-in-c
    public Form1()
    {
        InitializeComponent();
        AddDefaultRoot();
        var langStrings = Enum.GetValues(typeof(Language))
            .Cast<Language>()
            .Select(v => v.ToString())
            .ToList();
        foreach (var lang in langStrings)
        {
            var a = new ToolStripButton(lang.ToString());
            a.Click += SelectLanguage;
            btnTranslate.DropDownItems.Add(a);
        }
        selectedLang = debugString;

    }

    private void SelectLanguage(object sender, EventArgs e)
    {
        var button = (ToolStripButton)sender;
        if (button == null) return;
        selectedLang = button!.Text;
    }

    private void AddDefaultRoot()
    {
        var node = new TreeNode(rootString, imageIndex: 1, selectedImageIndex: 1);
        treeView.Nodes.Add(node);
    }

    //https://stackoverflow.com/questions/31774795/deserialize-json-from-file-in-c-sharp
    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
        PopulateEntriesFromZip();
        TreeBuilder.BuildTree(_entries, treeView);
    }

    private void PopulateEntriesFromZip()
    {
        using var zip = ZipFile.Open(openFileDialog1.FileName, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries) PopulateEntry(entry);
    }

    private void PopulateEntry(ZipArchiveEntry entry)
    {
        var text = new StreamReader(entry.Open()).ReadToEnd();
        _entries.Add(JsonConvert.DeserializeObject<LocEntry>(text)
                     ?? throw new InvalidOperationException());
    }

    private LocEntry? GetEntryFromSelectedNode(TreeNodeMouseClickEventArgs e)
    {
        selectedNode = e.Node ?? throw new InvalidOperationException();
        LocEntry? entry = GetEntryByName(selectedNode.Text);
        return entry;
    }

    private LocEntry? GetEntryByName(string text)
    {
        return _entries.FirstOrDefault(locEntry => locEntry.EntryName.Equals(text, StringComparison.OrdinalIgnoreCase));
    }

    private void ShowEntryDetails(LocEntry entry)
    {
        textBox1.Text = entry.HierarchyPath;
        richTextBox1.Text = entry.EntryName;
        ClearListView(lstDetails);
        ShowEntryInDetailsListView(entry);
    }

    private void ShowEntryInDetailsListView(LocEntry entry)
    {
        if (selectedLang is null) return;
        foreach(var kvp in entry.Translations) {
            lstDetails.Items.Add(kvp.Key.ToString()).SubItems.Add(kvp.Value);
        }
    }

    private string GetTranslatedStringFromSelectedLanguageOrDebug(LocEntry entry)
    {
        Language lang = (Language)Enum.Parse(typeof(Language), selectedLang);
        if (!entry.Translations.ContainsKey(lang))
                return entry.Translations[(Language)Enum.Parse(typeof(Language), debugString)];
        return entry.Translations[lang];
    }

    private void ShowEntryInSearchListView(LocEntry entry)
    {
        var loc = entry.LocKey;
        var path = entry.HierarchyPath;
        var trans = entry.Translations.Values.FirstOrDefault();

        var listRow = lstSearch.Items.Add(loc);
        listRow.SubItems.Add(path);
        listRow.SubItems.Add(trans);
    }

    private void ClearListView(ListView view)
    {
        view.Items.Clear();
    }

    private void btnSearch_Click(object sender, EventArgs e)
    {
        PerformSearch();
    }

    private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Enter)
        {
            PerformSearch();
        }
    }
    private void PerformSearch()
    {
        var searchedEntry = GetEntryByName(txtSearch.Text);
        if (searchedEntry != null)
        {
            ClearListView(lstSearch);
            ShowEntryInSearchListView(searchedEntry);
        }
    }

    private void treeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        var entry = GetEntryFromSelectedNode(e);
        if (entry != null)
            ShowEntryDetails(entry);
    }
    private void treeView1_MouseUp(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            PerformRightClickOnTreeView(e);
        }
    }

    private void PerformRightClickOnTreeView(MouseEventArgs e)
    {
        TreeNode clickedNode = treeView.GetNodeAt(e.X, e.Y);

        if (clickedNode is null) return;
        SelectNode(clickedNode);
        TryOpenContextMenu(clickedNode, e.Location);
    }

    private void SelectNode(TreeNode clickedNode)
    {
        treeView.SelectedNode = clickedNode;
        selectedNode = clickedNode;
    }

    private void TryOpenContextMenu(TreeNode node, Point p)
    {
        var canOpenMenu = !IsLeaf(node) || IsRootNode(node);

        if (!canOpenMenu) return;
        contextMenuStrip1.Show(treeView.PointToScreen(p));
    }

    private bool IsRootNode(TreeNode node)
    {
        return node.Parent is null;
    }

    private bool IsLeaf(TreeNode node)
    {
        return (node.Nodes.Count == 0 && node.ImageIndex == 0);
    }

    private bool IsOnlyDefaultRoot()
    {
        return treeView.Nodes.Count == 1 && treeView.Nodes[0].Text == rootString;
    }

    private void newGroupToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (IsOnlyDefaultRoot())
        {
            selectedNode?.BeginEdit();
            return;
        }
        AddNewNeighborGroupToSelectedNode();
    }

    private void AddNewNeighborGroupToSelectedNode()
    {
        if (selectedNode is null) return;
        var parent = selectedNode.Parent;
        var node = new TreeNode(newGroupString, imageIndex: 1, selectedImageIndex: 1);
        _ = parent is null ? treeView.Nodes.Add(node) : parent.Nodes.Add(node);
        node.BeginEdit();
    }

    private void newSubgroupToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (IsOnlyDefaultRoot())
        {
            ShowError("Can't add subgroup to default root", "ERROR");
            return;
        }
        AddNewChilldGroupToSelectedNode();
    }

    private void AddNewChilldGroupToSelectedNode()
    {
        if (selectedNode is null) return;
        var node = new TreeNode(newGroupString, imageIndex: 1, selectedImageIndex: 1);
        selectedNode.Nodes.Add(node);
        selectedNode.Expand();
        node.BeginEdit();
    }

    private void ShowError(string text, string title)
    {
        MessageBox.Show(text, title,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void deleteGroupToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (CountNodes(treeView.Nodes) < 2)
        {
            ShowError("Can't delete the only group", "ERROR");
            return;
        }
        deleteSelectedGroup();
    }

    private void deleteSelectedGroup()
    {
        if (selectedNode is null) return;
        selectedNode.Remove();
        selectedNode = null;
    }

    private int CountNodes(TreeNodeCollection nodes)
    {
        int count = nodes.Count;
        foreach (TreeNode node in nodes)
        {
            count += CountNodes(node.Nodes);
        }
        return count;
    }
    private void toolStripSplitButton1_ButtonClick(object sender, EventArgs e)
    {
        if (selectedLang != null && selectedLang != debugString && selectedNode != null && IsLeaf(selectedNode!))
            _ = TranslateSelectedNode();
    }
    public async Task TranslateSelectedNode()
    {
        CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        string abbreviation = "";
        foreach (CultureInfo culture in cultures)
        {
            if (culture.EnglishName.Equals(selectedLang, StringComparison.OrdinalIgnoreCase))
            {
                abbreviation = culture.TwoLetterISOLanguageName;
                break;
            }
        }
        if (!string.IsNullOrEmpty(abbreviation)) { }
        var entry = GetEntryByName(selectedNode!.Text);
        if (entry == null) return;
        var message = GetTranslatedStringFromSelectedLanguageOrDebug(entry);
        var translatedText = await Translator.Translate(message, abbreviation);
        entry.Translations.Add((Language)Enum.Parse(typeof(Language), selectedLang), translatedText);
        ShowEntryDetails(entry);
    }
}