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

        using var zip = ZipFile.Open(openFileDialog1.FileName, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            var a = new StreamReader(entry.Open());
            _entries.Add(JsonConvert.DeserializeObject<LocEntry>(a.ReadToEnd())
                         ?? throw new InvalidOperationException());
        }

        TreeBuilder.BuildTree(_entries, treeView1);
    }


    private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
    {
        var entry = e.Node;
        var a = _entries.FirstOrDefault(locEntry => locEntry?.EntryName == entry?.Text);
        if (a == null) return;
        listView1.Items.Clear();
        textBox1.Text = a.HierarchyPath;
        richTextBox1.Text = a.EntryName;
        var lang = a.Translations.Keys.FirstOrDefault().ToString();
        var trans = a.Translations.Values.FirstOrDefault();
        listView1.Items.Add(lang).SubItems.Add(trans);
    }
}