using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Gurpenator
{
    public partial class MainWindow : Form
    {
        private EditorMode mode = EditorMode.EditMode;
        private List<GurpenatorTable> tables = new List<GurpenatorTable>();
        public MainWindow()
        {
            InitializeComponent();
            var nameToThing = DataLoader.readData(new List<string> { "../../example.gurpenator_data", "../../core.gurpenator_data" });
            GurpsCharacter character = new GurpsCharacter(nameToThing);

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
    }
}
