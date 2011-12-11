using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace Gurpenator
{
    public partial class MainWindow : Form
    {
        private EditorMode mode = EditorMode.EditMode;
        private List<GurpenatorTable> tables = new List<GurpenatorTable>();
        private GurpsCharacter character;
        private Dictionary<string, GurpsProperty> nameToThing;
        public MainWindow()
        {
            InitializeComponent();
            nameToThing = DataLoader.readData(new List<string> { "../../example.gurpenator_data", "../../core.gurpenator_data" });
            setCharacter(new GurpsCharacter(nameToThing));
        }

        private void setCharacter(GurpsCharacter character)
        {
            this.character = character;
            // delete place holders
            attributesGroup.SuspendLayout();
            {
                attributesGroup.Controls.Clear();
                var table = new GurpenatorTable(attributesGroup);
                foreach (PurchasedProperty property in character.visibleAttributes)
                    table.add(new GurpenatorRow(property));
                tables.Add(table);
            }
            attributesGroup.ResumeLayout();

            otherGroup.SuspendLayout();
            {
                otherGroup.Controls.Clear();
                var table = new GurpenatorTable(otherGroup);
                foreach (PurchasedProperty property in character.otherTraits)
                    table.add(new GurpenatorRow(property));
                tables.Add(table);
            }
            otherGroup.ResumeLayout();
        }

        private void toggleModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = mode != EditorMode.PlayMode ? EditorMode.PlayMode : EditorMode.EditMode;
            foreach (var table in tables)
                table.Mode = mode;
        }

        private const string extension = ".gurpenator_character";
        private const string dialogFilter = "Gurpenator Character (*" + extension + ")|*" + extension;
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string serialization = DataLoader.jsonToString(character.toJson());
            string path = showFileDialog(new SaveFileDialog());
            File.WriteAllText(path, serialization);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = showFileDialog(new OpenFileDialog());
            setCharacter(GurpsCharacter.fromJson(DataLoader.stringToJson(File.ReadAllText(path)), nameToThing));
        }
        private string showFileDialog(FileDialog dialog)
        {
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.Filter = dialogFilter;
            if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.Cancel)
                return null;
            return dialog.FileName;
        }
    }
}
