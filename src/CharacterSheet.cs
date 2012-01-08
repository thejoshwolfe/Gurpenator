using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

namespace Gurpenator
{
    public partial class CharacterSheet : Form
    {
        private EditorMode mode = EditorMode.EditMode;
        private GurpsCharacter character;
        private Control layoutControl;
        private GurpenatorUiElement layout;
        public GurpsCharacter Character { get { return character; } }
        public GurpsDatabase database;
        public CharacterSheet(GurpsDatabase database, string characterPath)
        {
            InitializeComponent();
            this.database = database;
            if (characterPath == null)
                newCharacter();
            else
                load(characterPath);
        }

        private void newCharacter()
        {
            // default human
            var character = new GurpsCharacter(database);
            character.getPurchasedProperty("Human").PurchasedLevels = 1;
            setCharacter(character);
            filePath = null;
            savedName = null;
            Dirty = false;
            setTitle();
            Preferences.Instance.RecentCharacter = filePath;
        }

        private void setCharacter(GurpsCharacter character)
        {
            if (this.character != null)
                character.changed -= setDirty;
            if (layoutControl != null)
                this.Controls.Remove(layoutControl);
            this.character = character;
            character.changed += setDirty;
            layout = character.layout.createUi(this);
            layoutControl = layout.RootControl;
            this.Controls.Add(layoutControl);
            layoutControl.BringToFront();
            Dirty = false;
        }

        private void toggleModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = mode != EditorMode.PlayMode ? EditorMode.PlayMode : EditorMode.EditMode;
            suspendUi();
            foreach (var table in layout.getTables())
                table.Mode = mode;
            resumeUi();
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
            Action<string, string> handleError = delegate(string text, string windowTitle)
            {
                MessageBox.Show(this, text, windowTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                newCharacter();
            };
            try { setCharacter(GurpsCharacter.fromJson(DataLoader.stringToJson(File.ReadAllText(path)), database)); }
            catch (IOException e)
            {
                handleError(e.Message, "File Not Found");
                return;
            }
            catch (GurpenatorException e)
            {
                handleError(e.Message, "Data Error");
                return;
            }
            filePath = path;
            Preferences.Instance.RecentCharacter = path;
            savedName = character.Name;
            Dirty = false;
            setTitle();
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            save();
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveAs();
        }
        private bool saveAs()
        {
            string path = showFileDialog(new SaveFileDialog());
            if (path == null)
                return false;
            filePath = path;
            return save();
        }
        private bool save()
        {
            if (filePath == null)
                return saveAs();
            string serialization = DataLoader.jsonToString(character.toJson());
            File.WriteAllText(filePath, serialization);
            savedName = character.Name;
            Dirty = false;
            Preferences.Instance.RecentCharacter = filePath;
            return true;
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
            foreach (var table in layout.getTables())
                table.suspendLayout();
        }
        public void resumeUi()
        {
            foreach (var table in layout.getTables().Reverse())
                table.resumeLayout();
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
            string displayName = savedName;
            if (displayName == null || displayName == "")
                displayName = "[No Name]";
            displayName += " - Gurpenator";
            if (dirty)
                displayName = "*" + displayName;
            this.Text = displayName;
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
                    if (!save())
                        e.Cancel = true;
                    break;
                case System.Windows.Forms.DialogResult.No:
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            MessageBox.Show(this, "Gurpenator " + version.Major + "." + version.Minor + "." + version.Build + "\n\nhttp://github.com/thejoshwolfe/Gurpenator", "About Gurpenator");
        }

        private void databasesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (new DatabaseListEditor().ShowDialog(this) == DialogResult.Cancel)
                return;
            // reload database and character
            database = new GurpsDatabase();
            DataLoader.readData(database, Preferences.Instance.Databases);
            setCharacter(GurpsCharacter.fromJson(character.toJson(), database));
        }

        private void diceRollerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new DiceRollerWindow().Show();
        }
    }
}
