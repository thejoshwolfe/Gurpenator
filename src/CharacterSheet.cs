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
        public CharacterSheet(GurpsDatabase database, string characterPath)
        {
            InitializeComponent();
            this.database = database;
            if (characterPath == null)
                newCharacter();
            else
                load(characterPath);
            setTitle();
        }

        private void newCharacter()
        {
            // default human
            var character = new GurpsCharacter(database);
            character.addToSecondList("Human");
            character.getPurchasedProperty("Human").PurchasedLevels = 1;
            setCharacter(character);
            filePath = null;
            savedName = null;
            Dirty = false;
            Preferences.Instance.RecentCharacter = filePath;
        }

        private void setCharacter(GurpsCharacter character)
        {
            if (this.character != null)
                character.changed -= setDirty;
            this.character = character;
            character.changed += setDirty;
            nameTextBox.Text = character.Name;
            createTable(attributesGroup, character.getVisibleAttributes());
            createTable(otherGroup, character.getSecondPanelOfTraits());
            Dirty = false;
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
        private string savedName = null;
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            newCharacter();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = showFileDialog(new OpenFileDialog());
            if (path == null)
                return;
            load(path);
        }
        private void load(string path)
        {
            try { setCharacter(GurpsCharacter.fromJson(DataLoader.stringToJson(File.ReadAllText(path)), database)); }
            catch (IOException e)
            {
                MessageBox.Show(this, e.Message, "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                newCharacter();
                return;
            }
            filePath = path;
            Preferences.Instance.RecentCharacter = path;
            savedName = character.Name;
            Dirty = false;
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
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
            if (filePath == null)
            {
                saveAs();
                return;
            }
            string serialization = DataLoader.jsonToString(character.toJson());
            File.WriteAllText(filePath, serialization);
            savedName = character.Name;
            Dirty = false;
            Preferences.Instance.RecentCharacter = filePath;
        }
        private string showFileDialog(FileDialog dialog)
        {
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Filter = dialogFilter;
            dialog.FileName = character.Name;
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

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            character.Name = nameTextBox.Text.Trim();
        }

        private bool dirty = false;
        private bool Dirty
        {
            set
            {
                if (dirty == value)
                    return;
                dirty = value;
                setTitle();
            }
        }

        private void setTitle()
        {
            if (dirty)
                this.Text = "*" + savedName;
            else
                this.Text = savedName;
        }
        private void setDirty()
        {
            Dirty = true;
        }

        private void CharacterSheet_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!dirty)
                return;
            var result = MessageBox.Show(this, "Save changes to " + character.Name + "?", "Save Changes", MessageBoxButtons.YesNoCancel);
            switch (result)
            {
                case System.Windows.Forms.DialogResult.Yes:
                    save();
                    break;
                case System.Windows.Forms.DialogResult.No:
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }
    }
}
