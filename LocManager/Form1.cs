using System.IO.Compression;
using Newtonsoft.Json;

namespace LocManager;

public partial class Form1 : Form
{
    private readonly List<LocEntry> _entries = new();

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
        ShowEntryDetails(entry);
    }

    private LocEntry GetEntryFromSelectedNode(TreeViewEventArgs e)
    {
        var selectedEntry = e.Node;
        var entry = _entries.First(locEntry => locEntry.EntryName == selectedEntry?.Text);
        return entry;
    }

    private void ShowEntryDetails(LocEntry entry)
    {
        textBox1.Text = entry.HierarchyPath;
        richTextBox1.Text = entry.EntryName;
        ClearListView();
        ShowEntryLanguageInListView(entry);
    }

    private void ShowEntryLanguageInListView(LocEntry entry)
    {
        var lang = entry.Translations.Keys.FirstOrDefault().ToString();
        var trans = entry.Translations.Values.FirstOrDefault();
        listView1.Items.Add(lang).SubItems.Add(trans);
    }

    private void ClearListView()
    {
        listView1.Items.Clear();
    }
}