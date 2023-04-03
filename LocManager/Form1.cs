using System.Data.SqlTypes;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

namespace LocManager;

public partial class Form1 : Form
{
    private const string newGroupString = "<NEW GROUP>";
    private const string rootString = "<ROOT>";
    private const string debugString = "Debug";
    private const string locString = "LocKey#";

    private readonly List<LocEntry> _entries = new();
    private TreeNode? selectedNode = null;
    private string? selectedLang = null;

    private static readonly CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

    public Form1()
    {
        InitializeComponent();
        AddDefaultRoot();
        InitializeLanguages();

    }
    //https://stackoverflow.com/questions/1167361/how-do-i-convert-an-enum-to-a-list-in-c
    private void InitializeLanguages()
    {
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
        var node = GetNewGroup(rootString);
        treeView.Nodes.Add(node);
    }

    private TreeNode GetNewGroup(string text)
    {
        return new TreeNode(text, imageIndex: 1, selectedImageIndex: 1);
    }

    private TreeNode GetNewLeaf(string text)
    {
        return new TreeNode(text, imageIndex: 0, selectedImageIndex: 0);
    }

    //https://stackoverflow.com/questions/31774795/deserialize-json-from-file-in-c-sharp
    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
        treeView.Nodes.Clear();
        _entries.Clear();
        ClearDetails();
        PopulateEntriesFromZip();
        TreeBuilder.BuildTree(_entries, treeView);
    }

    private void ClearDetails()
    {
        textBox1.Text = string.Empty;
        richTextBox1.Text = string.Empty;
        lstDetails.Items.Clear();
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
        LocEntry? entry = GetEntryFromNode(selectedNode);
        return entry;
    }
    private void ShowEntryDetails(LocEntry entry)
    {
        textBox1.Text = entry.HierarchyPath;
        richTextBox1.Text = GetValueOfKeyDebugString(entry);
        ShowEntryInDetailsListView(entry);
    }

    private void ShowEntryInDetailsListView(LocEntry entry)
    {
        ClearListView(lstDetails);
        foreach (var kvp in entry.Translations)
        {
            lstDetails.Items.Add(kvp.Key.ToString()).SubItems.Add(kvp.Value);
        }
    }

    private string GetValueOfKeyDebugString(LocEntry entry)
    {
        return entry.Translations[(Language)Enum.Parse(typeof(Language), debugString)];
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
        ClearListView(lstSearch);
        var searchedEntries = GetEntryContainingSubstring(txtSearch.Text);
        foreach (var entry in searchedEntries)
        {
            if (entry != null)
            {
                ShowEntryInSearchListView(entry);
            }
        }
    }

    private List<LocEntry> GetEntryContainingSubstring(string text)
    {
        return _entries.Where(locEntry => locEntry.EntryName.Contains(text, StringComparison.OrdinalIgnoreCase)).ToList();
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
        return (node.Nodes.Count == 0 && GetEntryFromNode(node) != null);
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
        var node = GetNewGroup(newGroupString);
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
        var node = GetNewGroup(newGroupString);
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
            backgroundWorker1.RunWorkerAsync();
    }
    public async Task TranslateSelectedNode()
    {
        var entry = GetEntryFromNode(selectedNode!);
        if (entry is null) return;
        var lang = GetLanguageFromString(selectedLang!);
        var abbreviation = GetAbbreviationFromLanguageName(lang.ToString());
        var message = GetValueOfKeyDebugString(entry);

        if (string.IsNullOrEmpty(abbreviation)) return;
        if (entry.Translations.ContainsKey(lang)) return;
        var translatedText = await Translator.Translate(message, abbreviation);
        if (translatedText == null) return;
        entry.Translations.Add(lang, translatedText);
    }

    private string GetAbbreviationFromLanguageName(string lang)
    {
        foreach (CultureInfo culture in cultures)
        {
            if (culture.EnglishName.Equals(lang.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return culture.TwoLetterISOLanguageName;
            }
        }
        return "";
    }

    private Language GetLanguageFromString(string text)
    {
        return (Language)Enum.Parse(typeof(Language), text);
    }

    private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
    {
        var a = TranslateSelectedNode();
        for (int i = 0; i < 10; i++)
        {
            if (a.IsCompleted)
            {
                backgroundWorker1.ReportProgress(100);
                return;
            }
            backgroundWorker1.ReportProgress(i * 10);
            Thread.Sleep(100);
        }

    }

    private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
    {
        toolStripProgressBar1.Value = e.ProgressPercentage;
    }

    private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
    {
        Thread.Sleep(300);
        toolStripProgressBar1.Value = 0;
        var a = GetEntryFromNode(selectedNode);
        ShowEntryDetails(a);
    }

    public static Dictionary<string, string> GetTextDictionaryFromTree(List<LocEntry> entries)
    {
        var result = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            result.Add(locString + entry.LocKey, JsonConvert.SerializeObject(entry));
        }
        return result;
    }

    private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
        {
            string filename = saveFileDialog1.FileName;
            var dict = GetTextDictionaryFromTree(_entries);
            var archive = CreateZipAchiveAndSave(filename, dict);
        }
    }

    private ZipArchive CreateZipAchiveAndSave(string zipFilePath, Dictionary<string, string> fileContents)
    {
        if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
        using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        PopulateArchiveWithEntries(fileContents, archive);
        return archive;
    }

    private static void PopulateArchiveWithEntries(Dictionary<string, string> fileContents, ZipArchive archive)
    {
        foreach (var fileContent in fileContents)
        {
            AddEntryToArchive(archive, fileContent);
        }
    }

    private static void AddEntryToArchive(ZipArchive archive, KeyValuePair<string, string> fileContent)
    {
        var entry = archive.CreateEntry(fileContent.Key + ".txt");

        using (var writer = new StreamWriter(entry.Open()))
        {
            writer.Write(fileContent.Value);
        }
    }

    private void newEntryToolStripMenuItem1_Click(object sender, EventArgs e)
    {
        if (selectedNode == null || IsLeaf(selectedNode) || IsOnlyDefaultRoot()) return;
        var path = GetNodePathWithName(selectedNode);
        var startingString = path + "-";
        tabControl1.SelectedTab = tabPage2;
        richTextBox1.Clear();
        textBox1.ReadOnly = false;
        textBox1.Focus();
        textBox1.Text = startingString;
        textBox1.SelectionStart = textBox1.TextLength;
    }

    private string GetNodePathWithName(TreeNode node)
    {
        if (node.Parent == null)
        {
            return node.Text;
        }
        else
        {
            return GetNodePathWithName(node.Parent) + "-" + node.Text;
        }
    }

    private string GetNodePath(TreeNode node)
    {
        if (node == null)
        {
            return string.Empty;
        }

        var path = new List<string>();
        while (node != null)
        {
            path.Add(node.Text);
            node = node.Parent;
        }

        path.Reverse();
        path.RemoveAt(path.Count - 1);

        return string.Join("-", path);
    }

    private void textBox1_Leave(object sender, EventArgs e)
    {
        if (textBox1.ReadOnly || selectedNode is null) return;
        textBox1.ReadOnly = true;
        var labels = textBox1.Text.Split("-");
        var name = labels.Last();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Name can't be empty", "ERROR");
            textBox1.Text = string.Empty;
            return;
        }
        var node = AddNewChildLeafToSelectedGroup(name);
        if (GetEntryFromNode(node) != null)
        {
            ShowError("Can't create entries with the same name", "ERROR");
            textBox1.Text = string.Empty;
            node.Parent.Nodes.Remove(node);
            return;
        }
        var entry = CreateNewEntryFromNode(node);
        _entries.Add(entry);
        ShowEntryDetails(entry);

        SelectNode(node);
    }

    private LocEntry CreateNewEntryFromNode(TreeNode node)
    {
        var entry = new LocEntry(GetNodePath(node), node.Text);
        entry.Translations[GetLanguageFromString(debugString)] = "";

        return entry;
    }

    private TreeNode AddNewChildLeafToSelectedGroup(string name)
    {
        var node = GetNewLeaf(name);
        selectedNode.Nodes.Add(node);
        return node;
    }

    private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Enter)
        {
            SelectNextControl((Control)sender, true, true, true, true);
        }
    }

    private void richTextBox1_TextChanged(object sender, EventArgs e)
    {
        if (selectedNode is null) return;
        var entry = GetEntryFromNode(selectedNode);
        if (entry is null) return;
        entry.Translations[GetLanguageFromString(debugString)] = ((RichTextBox)sender).Text;
        ShowEntryInDetailsListView(entry);
    }

    private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (selectedNode is null || !IsLeaf(selectedNode)) return;
        var entry = GetEntryFromNode(selectedNode);
        if (entry is null) return;
        _entries.Remove(entry);
        selectedNode.Parent.Nodes.Remove(selectedNode);
    }

    private LocEntry? GetEntryFromNode(TreeNode node)
    {
        var entry = _entries.FirstOrDefault(e => string.Equals($"{e.HierarchyPath}-{e.EntryName}", GetNodePathWithName(node)));
        return entry;
    }
}