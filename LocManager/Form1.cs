using System.IO.Compression;
using Newtonsoft.Json;

namespace LocManager;

public partial class Form1 : Form
{
    private readonly List<LocEntry> _entries = new();
    private TreeNode? selectedNode = null;

    public Form1()
    {
        InitializeComponent();
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


    private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        var entry = GetEntryFromSelectedNode(e);
        if (entry != null)
            ShowEntryDetails(entry);
    }

    private LocEntry? GetEntryFromSelectedNode(TreeViewEventArgs e)
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
        ShowEntryInSearchListView(entry);
    }

    private void ShowEntryInDetailsListView(LocEntry entry)
    {
        var lang = entry.Translations.Keys.FirstOrDefault().ToString();
        var trans = entry.Translations.Values.FirstOrDefault();
        lstDetails.Items.Add(lang).SubItems.Add(trans);
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
        if(e.KeyChar == (char)Keys.Enter)
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
}