using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace Gurpenator
{
    public partial class CharacterSheet : Form
    {
        private EditorMode mode = EditorMode.EditMode;
        private List<GurpenatorTable> tables = new List<GurpenatorTable>();
        private GurpsCharacter character;
        public GurpsCharacter Character { get { return character; } }
        public readonly GurpsDatabase database;
        public CharacterSheet()
        {
            InitializeComponent();
            database = new GurpsDatabase();
            DataLoader.readData(database, new List<string> { "../../example.gurpenator_data", "../../core.gurpenator_data" });
            newCharacter();
        }

        private void newCharacter()
        {
            // default human
            var character = new GurpsCharacter(database);
            character.addToSecondList("Human");
            character.getPurchasedProperty("Human").PurchasedLevels = 1;
            setCharacter(character);
            filePath = null;
        }

        private void setCharacter(GurpsCharacter character)
        {
            this.character = character;
            createTable(attributesGroup, character.getVisibleAttributes());
            createTable(otherGroup, character.getSecondPanelOfTraits());
        }
        private void createTable(Control parent, IEnumerable<PurchasedProperty> properties)
        {
            parent.Controls.Clear();
            var table = new GurpenatorTable(parent, this);
            var rows = new List<GurpenatorRow>();
            foreach (PurchasedProperty property in properties)
                rows.Add(new GurpenatorRow(property, table));
            table.setRows(rows);
            tables.Add(table);
        }

        private void toggleModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = mode != EditorMode.PlayMode ? EditorMode.PlayMode : EditorMode.EditMode;
            foreach (var table in tables)
                table.Mode = mode;
        }

        private const string extension = ".gurpenator_character";
        private const string dialogFilter = "Gurpenator Character (*" + extension + ")|*" + extension;
        private string filePath = null;
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            newCharacter();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = showFileDialog(new OpenFileDialog());
            if (path == null)
                return;
            filePath = path;
            setCharacter(GurpsCharacter.fromJson(DataLoader.stringToJson(File.ReadAllText(path)), database));
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filePath == null)
                saveAs();
            else
                save();
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAs();
        }
        private void saveAs()
        {
            string path = showFileDialog(new SaveFileDialog());
            if (path == null)
                return;
            filePath = path;
            save();
        }
        private void save()
        {
            string serialization = DataLoader.jsonToString(character.toJson());
            File.WriteAllText(filePath, serialization);
        }
        private string showFileDialog(FileDialog dialog)
        {
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Filter = dialogFilter;
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return null;
            return dialog.FileName;
        }

        public void suspendUi()
        {
            foreach (var table in tables)
                table.suspendLayout();
        }
        public void resumeUi()
        {
            foreach (var table in tables)
                table.resumeLayout();
        }
    }
}
