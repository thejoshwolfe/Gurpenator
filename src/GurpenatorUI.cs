using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Gurpenator
{
    public enum EditorMode
    {
        EditMode, PlayMode
    }
    public class GurpenatorTable
    {
        public readonly CharacterSheet characterSheet;
        private TableLayoutPanel table;
        private List<GurpenatorRow> rows = new List<GurpenatorRow>();
        private EditorMode mode = EditorMode.EditMode;
        public GurpenatorTable(Control parent, CharacterSheet characterSheet)
        {
            this.characterSheet = characterSheet;
            table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.AutoSize = true;
            parent.Controls.Add(table);

            refreshControls();
        }
        private void refreshControls()
        {
            table.SuspendLayout();
            {
                table.Controls.Clear();
                table.ColumnCount = mode == EditorMode.PlayMode ? 2 : 4;
                foreach (GurpenatorRow row in rows)
                    addRowControls(row);
            }
            table.ResumeLayout();
        }
        private void addRowControls(GurpenatorRow row)
        {
            table.Controls.Add(row.createHeaderLabel());
            if (table.ColumnCount == 4)
            {
                table.Controls.Add(row.createSpendingControl());
                table.Controls.Add(row.createCostLabel());
            }
            table.Controls.Add(row.createOutputLabel());
        }
        public void addRange(IEnumerable<GurpenatorRow> rows)
        {
            this.rows.AddRange(rows);
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
        public void suspendLayout() { table.SuspendLayout(); }
        public void resumeLayout() { table.ResumeLayout(); }
    }
    public class GurpenatorRow
    {
        private readonly PurchasedProperty purchasedProperty;
        private readonly GurpenatorTable table;
        private NumericUpDown spendingSpinner;
        private CheckBox spendingCheckbox;
        private Label costLabel;
        private Label outputLabel;
        public GurpenatorRow(PurchasedProperty purchasedProperty, GurpenatorTable table)
        {
            this.purchasedProperty = purchasedProperty;
            this.table = table;
            purchasedProperty.changed += purchasedProperty_changed;
        }
        public void dispose()
        {
            spendingSpinner = null;
            spendingCheckbox = null;
            costLabel = null;
            outputLabel = null;
            purchasedProperty.changed -= purchasedProperty_changed;
        }
        private void purchasedProperty_changed()
        {
            if (spendingSpinner != null)
                spendingSpinner.Value = (decimal)purchasedProperty.PurchasedLevels;
            if (spendingCheckbox != null)
                spendingCheckbox.Checked = purchasedProperty.PurchasedLevels != 0;
            if (costLabel != null)
                costLabel.Text = purchasedProperty.getCost().ToString();
            if (outputLabel != null)
                outputLabel.Text = purchasedProperty.getFormattedValue();
        }
        public Label createHeaderLabel()
        {
            Label header = creatLabel();
            header.TextAlign = ContentAlignment.MiddleLeft;
            header.Text = purchasedProperty.property.DisplayName;
            return header;
        }
        private CharacterSheet characterSheet { get { return table.characterSheet; } }
        private EventHandler createEventHandler(Action action)
        {
            return delegate(object sender, EventArgs e)
            {
                characterSheet.suspendUi();
                action();
                characterSheet.resumeUi();
            };
        }
        public Control createSpendingControl()
        {
            if (purchasedProperty.hasPurchasedLevels)
            {
                spendingSpinner = new NumericUpDown();
                spendingSpinner.Minimum = -9999;
                spendingSpinner.Maximum = 9999;
                spendingSpinner.AutoSize = true;
                spendingSpinner.Dock = DockStyle.Fill;
                spendingSpinner.Value = purchasedProperty.PurchasedLevels;
                spendingSpinner.ValueChanged += createEventHandler(delegate() { purchasedProperty.PurchasedLevels = (int)spendingSpinner.Value; });
                return spendingSpinner;
            }
            if (purchasedProperty.isBooleanPurchasable)
            {
                spendingCheckbox = new CheckBox();
                spendingCheckbox.AutoSize = true;
                spendingCheckbox.Dock = DockStyle.Fill;
                spendingCheckbox.Checked = purchasedProperty.PurchasedLevels != 0;
                spendingCheckbox.CheckedChanged += createEventHandler(delegate() { purchasedProperty.PurchasedLevels = spendingCheckbox.Checked ? 1 : 0; });
                return spendingCheckbox;
            }
            return createFiller();
        }
        public Control createCostLabel()
        {
            if (!purchasedProperty.hasCost)
                return createFiller();
            costLabel = creatLabel();
            costLabel.TextAlign = ContentAlignment.MiddleRight;
            costLabel.Text = purchasedProperty.getCost().ToString();
            return costLabel;
        }
        public Label createOutputLabel()
        {
            outputLabel = creatLabel();
            outputLabel.Text = purchasedProperty.getFormattedValue();
            if (purchasedProperty.property.formattingFunction == GurpsProperty.formatAsDice)
                outputLabel.TextAlign = ContentAlignment.MiddleLeft;
            else
                outputLabel.TextAlign = ContentAlignment.MiddleRight;
            return outputLabel;
        }
        private static Label creatLabel()
        {
            var label = new Label();
            label.AutoSize = true;
            label.Dock = DockStyle.Fill;
            return label;
        }
        private static Control createFiller()
        {
            var result = new Label();
            result.Size = new Size();
            return result;
        }
    }
}
