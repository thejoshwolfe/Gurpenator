﻿using System;
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
            if (mode == EditorMode.EditMode)
            {
                table.Controls.Add(row.createSpendingSpinner());
                table.Controls.Add(row.createCostLabel());
            }
            table.Controls.Add(row.createOutputLabel());
        }
        public void add(GurpenatorRow row)
        {
            rows.Add(row);
            addRowControls(row);
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
        private PurchasedProperty purchasedProperty;
        private NumericUpDown spendingSpinner;
        private Label costLabel;
        private Label outputLabel;
        public GurpenatorRow(PurchasedProperty purchasedProperty)
        {
            this.purchasedProperty = purchasedProperty;
            purchasedProperty.changed += purchasedProperty_changed;
        }
        public void dispose()
        {
            spendingSpinner = null;
            costLabel = null;
            outputLabel = null;
            purchasedProperty.changed -= purchasedProperty_changed;
        }
        private void purchasedProperty_changed()
        {
            if (spendingSpinner != null)
                spendingSpinner.Value = (decimal)purchasedProperty.PurchasedLevels;
            if (costLabel != null)
                costLabel.Text = purchasedProperty.cost.ToString();
            if (outputLabel != null)
                outputLabel.Text = purchasedProperty.formattedValue;
        }
        public Label createHeaderLabel()
        {
            Label header = creatLabel();
            header.TextAlign = ContentAlignment.MiddleLeft;
            header.Text = purchasedProperty.property.name;
            return header;
        }
        public Label createCostLabel()
        {
            costLabel = creatLabel();
            costLabel.TextAlign = ContentAlignment.MiddleRight;
            costLabel.Text = purchasedProperty.cost.ToString();
            return costLabel;
        }
        public NumericUpDown createSpendingSpinner()
        {
            spendingSpinner = new NumericUpDown();
            spendingSpinner.Minimum = -9999;
            spendingSpinner.Maximum = 9999;
            spendingSpinner.AutoSize = true;
            spendingSpinner.Dock = DockStyle.Fill;
            spendingSpinner.Value = purchasedProperty.PurchasedLevels;
            spendingSpinner.ValueChanged += delegate(object sender, EventArgs e)
            {
                purchasedProperty.PurchasedLevels = (int)spendingSpinner.Value;
            };
            return spendingSpinner;
        }
        public Label createOutputLabel()
        {
            outputLabel = creatLabel();
            outputLabel.Text = purchasedProperty.formattedValue;
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
    }
}