using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gurpenator
{
    public partial class MainWindow : Form
    {
        private EditorMode mode = EditorMode.EditMode;
        private GurpenatorTable table;
        public MainWindow()
        {
            InitializeComponent();
            var nameToThing = DataLoader.readData(new List<string> { "../../example.gurpenator_data", "../../core.gurpenator_data" });

            // delete place holders
            attributesGroup.SuspendLayout();
            {
                attributesGroup.Controls.Clear();
                table = new GurpenatorTable(attributesGroup);
                GurpsCharacter character = new GurpsCharacter(nameToThing);
                foreach (PurchasedProperty property in character.visibleAttributes)
                    table.add(new GurpenatorRow(property));
            }
            attributesGroup.ResumeLayout();
        }

        private void toggleModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = mode != EditorMode.PlayMode ? EditorMode.PlayMode : EditorMode.EditMode;
            table.Mode = mode;
        }
    }
}
