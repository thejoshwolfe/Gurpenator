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
    public abstract class AbstractTraitGroup
    {
        public abstract GurpenatorUiElement createUi(CharacterSheet characterSheet);

        public abstract object toJson();
        public static AbstractTraitGroup fromJson(object jsonObject)
        {
            if (jsonObject as string == "BasicInfoTraitGroup")
                return new BasicInfoTraitGroup();
            if (jsonObject is Dictionary<string, object>)
            {
                var dict = (Dictionary<string, object>)jsonObject;
                if (dict.ContainsKey("orientation"))
                {
                    string title = null;
                    try { title = (string)dict["title"]; }
                    catch (KeyNotFoundException) { }
                    return new TraitContainer(title, (Orientation)dict["orientation"], (from o in (List<object>)dict["members"] select fromJson(o)).ToArray());
                }
                if (dict.ContainsKey("names"))
                {
                    string title = null;
                    try { title = (string)dict["title"]; }
                    catch (KeyNotFoundException) { }
                    TraitTypeFilter filter = TraitTypeFilter.Locked;
                    try { filter = (TraitTypeFilter)dict["filter"]; }
                    catch (KeyNotFoundException) { }
                    return new TraitList(title, filter, (from name in (List<object>)dict["names"] select (string)name).ToArray());
                }
            }
            throw null;
        }

        public virtual IEnumerable<string> getNames() { yield break; }
    }
    public class TraitContainer : AbstractTraitGroup
    {
        private string title;
        private Orientation orientation;
        private AbstractTraitGroup[] members;
        public TraitContainer(string title, Orientation orientation, params AbstractTraitGroup[] members)
        {
            this.title = title;
            this.orientation = orientation;
            this.members = members;
        }
        public override GurpenatorUiElement createUi(CharacterSheet characterSheet)
        {
            return new GurpenatorLayoutPanel(title, orientation, characterSheet, (from m in members select m.createUi(characterSheet)).ToList());
        }
        public override object toJson()
        {
            var result = new Dictionary<string, object>();
            if (title != null)
                result["title"] = title;
            result["orientation"] = (int)orientation;
            result["members"] = new List<object>(from m in members select m.toJson());
            return result;
        }
        public override IEnumerable<string> getNames()
        {
            foreach (var member in members)
                foreach (var result in member.getNames())
                    yield return result;
        }
    }
    public class TraitList : AbstractTraitGroup
    {
        public string title;
        public TraitTypeFilter filter;
        public List<string> names;
        public TraitList(string title, TraitTypeFilter filter, params string[] names)
        {
            this.title = title;
            this.filter = filter;
            this.names = names.ToList();
        }
        public override GurpenatorUiElement createUi(CharacterSheet characterSheet)
        {
            return new GurpenatorTable(characterSheet, this);
        }
        public override object toJson()
        {
            var result = new Dictionary<string, object>();
            if (title != null)
                result["title"] = title;
            if (filter != TraitTypeFilter.Locked)
                result["filter"] = (int)filter;
            result["names"] = new List<object>(names);
            return result;
        }
        public override IEnumerable<string> getNames()
        {
            return names;
        }
    }
    public class BasicInfoTraitGroup : AbstractTraitGroup
    {
        public override GurpenatorUiElement createUi(CharacterSheet characterSheet)
        {
            return new BasicInfoUiThing(characterSheet);
        }
        public override object toJson()
        {
            return "BasicInfoTraitGroup";
        }
    }

    public abstract class GurpenatorUiElement
    {
        public abstract Control RootControl { get; }
        public abstract IEnumerable<GurpenatorTable> getTables();
        protected Control maybeContainInGroupBox(string title, TableLayoutPanel panel, CharacterSheet characterSheet)
        {
            if (title == null)
                return panel;
            GroupBox groupBox = new GroupBox();
            groupBox.Dock = DockStyle.Fill;
            groupBox.AutoSize = true;
            groupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Action updateText = delegate()
            {
                int cost = getTables().Sum((table) => table.layout.names.Sum((name) => characterSheet.Character.getPurchasedProperty(name).getCost()));
                groupBox.Text = title + " (" + cost + ")";
            };
            updateText();
            characterSheet.Character.changed += updateText;
            groupBox.Controls.Add(panel);

            return groupBox;
        }
    }
    public class BasicInfoUiThing : GurpenatorUiElement
    {
        TableLayoutPanel panel;
        public override Control RootControl { get { return panel; } }
        TextBox nameTextBox;
        Label pointsTotalLabel;
        CharacterSheet characterSheet;
        public BasicInfoUiThing(CharacterSheet characterSheet)
        {
            this.characterSheet = characterSheet;
            panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.ColumnCount = 4;

            Label label = GurpenatorRow.createLabel();
            label.Text = "Name:";
            panel.Controls.Add(label);

            nameTextBox = new TextBox();
            nameTextBox.Width = 167;
            nameTextBox.Text = characterSheet.Character.Name;
            nameTextBox.TextChanged += nameTextBox_TextChanged;
            panel.Controls.Add(nameTextBox);

            Label pointsTotalLabelLabel = GurpenatorRow.createLabel();
            pointsTotalLabelLabel.Text = "Total:";
            panel.Controls.Add(pointsTotalLabelLabel);

            pointsTotalLabel = GurpenatorRow.createLabel();
            pointsTotalLabel.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(pointsTotalLabel);

            character_changed();
            characterSheet.Character.changed += character_changed;
        }

        private void character_changed()
        {
            pointsTotalLabel.Text = characterSheet.Character.getAllPurchases().Sum((property) => property.getCost()).ToString();
        }
        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            characterSheet.Character.Name = nameTextBox.Text.Trim();
        }
        public override IEnumerable<GurpenatorTable> getTables() { yield break; }
    }
    public class GurpenatorLayoutPanel : GurpenatorUiElement
    {
        private List<GurpenatorUiElement> members;
        private Control rootControl;
        public override Control RootControl { get { return rootControl; } }
        public GurpenatorLayoutPanel(string title, Orientation orientation, CharacterSheet characterSheet, List<GurpenatorUiElement> members)
        {
            this.members = members;
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.ColumnCount = 1;
            panel.RowCount = 1;
            panel.GrowStyle = orientation == Orientation.Vertical ? TableLayoutPanelGrowStyle.AddRows : TableLayoutPanelGrowStyle.AddColumns;
            foreach (GurpenatorUiElement element in members)
                panel.Controls.Add(element.RootControl);
            rootControl = maybeContainInGroupBox(title, panel, characterSheet);
        }
        public override IEnumerable<GurpenatorTable> getTables()
        {
            foreach (var member in members)
                foreach (var result in member.getTables())
                    yield return result;
        }
    }
    public class GurpenatorTable : GurpenatorUiElement
    {
        public readonly CharacterSheet characterSheet;
        public readonly TraitList layout;
        private Control rootControl;
        public override Control RootControl { get { return rootControl; } }
        private TableLayoutPanel table;
        private List<GurpenatorRow> rows = new List<GurpenatorRow>();
        private bool allowAddRemoveRows { get { return layout.filter != TraitTypeFilter.Locked; } }
        private TextBox newItemTextBox;
        private EditorMode mode = EditorMode.EditMode;
        public GurpenatorTable(CharacterSheet characterSheet, TraitList layout)
        {
            this.characterSheet = characterSheet;
            this.layout = layout;
            table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            table.AutoSize = true;
            rootControl = maybeContainInGroupBox(layout.title, table, characterSheet);

            refreshRows();
        }

        private void refreshRows()
        {
            rows.Clear();
            foreach (string name in layout.names)
                rows.Add(new GurpenatorRow(characterSheet.Character.getPurchasedProperty(name), this));
            refreshControls();
        }
        public override IEnumerable<GurpenatorTable> getTables() { yield return this; }
        private void refreshControls()
        {
            table.Controls.Clear();
            newItemTextBox = null;
            table.ColumnCount = mode == EditorMode.PlayMode ? 2 : allowAddRemoveRows ? 5 : 4;
            foreach (GurpenatorRow row in rows)
                addRowControls(row);
            if (allowAddRemoveRows && mode == EditorMode.EditMode)
                addLastRow();
            else
                table.Controls.Add(GurpenatorRow.createFiller());
        }
        private void addRowControls(GurpenatorRow row)
        {
            table.Controls.Add(row.createHeaderLabel());
            table.Controls.Add(row.createOutputLabel());
            if (mode != EditorMode.EditMode)
                return;
            table.Controls.Add(row.createSpendingControl());
            table.Controls.Add(row.createCostLabel());
            if (allowAddRemoveRows)
            {
                var options = new Button();
                options.AutoSize = true;
                options.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                options.Text = "...";
                options.Click += delegate(object _, EventArgs __)
                {
                    var menu = new ContextMenu();
                    var deleteItem = new MenuItem("Delete");
                    deleteItem.Click += delegate(object ___, EventArgs ____)
                    {
                        row.dispose();
                        rows.Remove(row);
                        layout.names.Remove(row.purchasedProperty.property.name);
                        using (new LayoutSuspender(table))
                            refreshControls();
                    };
                    menu.MenuItems.Add(deleteItem);
                    menu.Show(options, new Point(0, options.Height));
                };
                table.Controls.Add(options);
            }
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
                    label.Text = suggestion.DisplayName;
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
                layout.names.Add(property.name);
                layout.names.Sort();
                characterSheet.Character.raiseChanged();
                refreshRows();
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
                    suggest(characterSheet.database.search(newItemTextBox.Text, layout.filter, characterSheet.Character));
                else
                    clearSearchSuggestions();
            };
            newItemTextBox.LostFocus += (EventHandler)((_, __) => { clearSearchSuggestions(); });
            table.Controls.Add(newItemTextBox);
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
        public readonly PurchasedProperty purchasedProperty;
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
            purchasedProperty.PurchasedLevels = 0;
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
            Label header = createLabel();
            header.TextAlign = ContentAlignment.MiddleLeft;
            header.Text = purchasedProperty.property.DisplayName.Replace("&", "&&");
            header.Font = new Font(header.Font, FontStyle.Bold);
            if (purchasedProperty.property is AbstractSkill)
            {
                // we probably want to do something more formal than this
                var skill = (AbstractSkill)purchasedProperty.property;
                new ToolTip().SetToolTip(header, skill.getBaseFormula() + " " + AbstractSkill.difficultyToString(skill.getDifficulty()));
            }
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
            costLabel = createLabel();
            costLabel.TextAlign = ContentAlignment.MiddleRight;
            costLabel.Text = purchasedProperty.getCost().ToString();
            return costLabel;
        }
        public Label createOutputLabel()
        {
            outputLabel = createLabel();
            outputLabel.Text = purchasedProperty.getFormattedValue();
            outputLabel.TextAlign = ContentAlignment.MiddleRight;
            return outputLabel;
        }
        public static Label createLabel()
        {
            var label = new Label();
            label.AutoSize = true;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            return label;
        }
        public static Control createFiller()
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
