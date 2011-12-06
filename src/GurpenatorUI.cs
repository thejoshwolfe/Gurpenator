using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gurpenator
{
    public enum EditorMode
    {
        EditMode, PlayMode
    }
    public class GurpenatorTable
    {
        private TableLayoutPanel table;
        private List<GurpenatorRow> rows = new List<GurpenatorRow>();
        private EditorMode mode = EditorMode.EditMode;
        public GurpenatorTable(Control parent)
        {
            table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.AutoSize = true;
            parent.Controls.Add(table);

            refreshControls();
        }
        private void refreshControls()
        {
            table.Controls.Clear();
            Func<Control, Control> addDoubleClickHandler = delegate(Control c) { c.DoubleClick += toggleMode; return c; };
            switch (mode)
            {
                case EditorMode.PlayMode:
                case EditorMode.EditMode:
                    table.ColumnCount = 2;
                    foreach (GurpenatorRow row in rows)
                    {
                        table.Controls.Add(addDoubleClickHandler(row.createHeaderLabel()));
                        table.Controls.Add(addDoubleClickHandler(row.createOutputLabel()));
                    }
                    break;
                default:
                    throw null;
            }
        }

        private void toggleMode(object sender, EventArgs e)
        {
            if (mode == EditorMode.PlayMode)
                Mode = EditorMode.EditMode;
            else
                Mode = EditorMode.PlayMode;
        }
        public void add(GurpenatorRow row)
        {
            rows.Add(row);
            refreshControls();
        }
        public EditorMode Mode
        {
            set
            {
                if (value == mode)
                    return;
                mode = value;
                refreshControls();
            }
        }
    }
    public class GurpenatorRow
    {
        private GurpsProperty property;
        Label outputLabel;
        public GurpenatorRow(GurpsProperty property)
        {
            this.property = property;
        }
        public Label createHeaderLabel()
        {
            Label header = new Label();
            header.AutoSize = true;
            header.Text = property.name;
            return header;
        }
        public Label createOutputLabel()
        {
            outputLabel = new Label();
            outputLabel.AutoSize = true;
            refreshOutput();
            return outputLabel;
        }

        private void refreshOutput()
        {
            outputLabel.Text = property.formattedValue;
        }
    }
}
