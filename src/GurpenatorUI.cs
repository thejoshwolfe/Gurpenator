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
        private bool allowAddRemoveRows;
        private Type typeFilter;
        private TextBox newItemTextBox;
        private EditorMode mode = EditorMode.EditMode;
        public GurpenatorTable(Control parent, CharacterSheet characterSheet, bool allowAddRemoveRows, Type typeFilter)
        {
            this.characterSheet = characterSheet;
            this.allowAddRemoveRows = allowAddRemoveRows;
            this.typeFilter = typeFilter;
            table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.AutoSize = true;
            parent.Controls.Clear();
            parent.Controls.Add(table);

            refreshControls();
        }
        private void refreshControls()
        {
            using (new LayoutSuspender(table))
            {
                table.Controls.Clear();
                newItemTextBox = null;
                table.ColumnCount = mode == EditorMode.PlayMode ? 2 : 4;
                foreach (GurpenatorRow row in rows)
                    addRowControls(row);
                if (allowAddRemoveRows && mode == EditorMode.EditMode)
                    addLastRow();
            }
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
        private TableLayoutPanel searchSuggestionBox = null;
        private void suggest(List<GurpsProperty> suggestions)
        {
            if (suggestions.Count == 0)
            {
                clearSearchSuggestions();
                return;
            }
            if (searchSuggestionBox == null)
            {
                searchSuggestionBox = new TableLayoutPanel();
                Point location = characterSheet.PointToClient(newItemTextBox.Parent.PointToScreen(newItemTextBox.Location));
                location.Offset(0, newItemTextBox.Height);
                searchSuggestionBox.Location = location;
                searchSuggestionBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                searchSuggestionBox.AutoSize = true;
                searchSuggestionBox.BackColor = Color.White;

                characterSheet.Controls.Add(searchSuggestionBox);
                searchSuggestionBox.BringToFront();
            }

            using (new LayoutSuspender(searchSuggestionBox))
            {
                searchSuggestionBox.Controls.Clear();
                foreach (var suggestion in suggestions)
                {
                    Label label = new Label();
                    label.Text = suggestion.name;
                    label.AutoSize = true;
                    label.Dock = DockStyle.Fill;
                    label.MouseEnter += (EventHandler)((_, __) => { label.BackColor = Color.PaleTurquoise; });
                    label.MouseLeave += (EventHandler)((_, __) => { label.BackColor = Color.Transparent; });
                    var localSelection = suggestion;
                    label.MouseDown += (MouseEventHandler)((_, __) => { addNewItem(localSelection); });
                    searchSuggestionBox.Controls.Add(label);
                }
            }
        }

        private void addNewItem(GurpsProperty property)
        {
            using (new LayoutSuspender(table))
            {
                table.Controls.Remove(newItemTextBox);
                // TODO: addToSecondPanel is retarded
                addRowControls(new GurpenatorRow(characterSheet.Character.addToSecondPanel(property.name), this));
                addLastRow();
            }
        }
        private void clearSearchSuggestions()
        {
            if (searchSuggestionBox == null)
                return;
            characterSheet.Controls.Remove(searchSuggestionBox);
            searchSuggestionBox = null;
        }
        private void addLastRow()
        {
            newItemTextBox = new TextBox();
            newItemTextBox.Dock = DockStyle.Fill;
            newItemTextBox.TextChanged += delegate(object sender, EventArgs e)
            {
                if (newItemTextBox.Text.Trim() != "")
                    suggest(characterSheet.database.search(newItemTextBox.Text));
                else
                    clearSearchSuggestions();
            };
            newItemTextBox.LostFocus += (EventHandler)((_, __) => { clearSearchSuggestions(); });
            table.Controls.Add(newItemTextBox);
        }
        public void setRows(IEnumerable<GurpenatorRow> rows)
        {
            if (this.rows.Count != 0)
                throw null;
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

    public class LayoutSuspender : IDisposable
    {
        private readonly Control control;
        public LayoutSuspender(Control control)
        {
            this.control = control;
            control.SuspendLayout();
        }
        public void Dispose()
        {
            control.ResumeLayout();
        }
    }
}
