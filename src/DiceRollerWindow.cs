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
    public partial class DiceRollerWindow : Form
    {
        Random random = new Random();
        public DiceRollerWindow()
        {
            InitializeComponent();
            rollingTable.Controls.Remove(dummy0);
            rollingTable.Controls.Remove(dummy1);
            rollingTable.Controls.Remove(dummy2);
            rollingTable.Controls.Remove(dummy3);
            rollingTable.Controls.Remove(dummy4);
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            using (new LayoutSuspender(rollingTable))
            {
                rollingTable.Controls.Remove(addButton);

                var deleteButton = new Button();
                deleteButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                deleteButton.AutoSize = true;
                deleteButton.Text = "delete";
                deleteButton.Dock = DockStyle.Fill;
                rollingTable.Controls.Add(deleteButton);

                var countSpinner = new NumericUpDown();
                countSpinner.Dock = DockStyle.Fill;
                countSpinner.AutoSize = true;
                countSpinner.Minimum = 1;
                countSpinner.Value = 3;
                rollingTable.Controls.Add(countSpinner);

                var label = new Label();
                label.AutoSize = true;
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Text = "d";
                label.Dock = DockStyle.Fill;
                rollingTable.Controls.Add(label);

                var dSpinner = new NumericUpDown();
                dSpinner.Dock = DockStyle.Fill;
                dSpinner.AutoSize = true;
                dSpinner.Minimum = 1;
                dSpinner.Value = 6;
                rollingTable.Controls.Add(dSpinner);

                var rollButton = new Button();
                rollButton.AutoSize = true;
                rollButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                rollButton.Dock = DockStyle.Fill;
                rollButton.Text = "Roll";
                rollButton.Click += delegate(object _, EventArgs __)
                {
                    var count = (int)countSpinner.Value;
                    var d = (int)dSpinner.Value;
                    var numbers = new List<int>();
                    for (int i = 0; i < count; i++)
                        numbers.Add(random.Next(d) + 1);

                    string newText = "Rolling " + count + "d" + d + ":\r\n" +
                        numbers.Sum() + " = " + string.Join("+", numbers) + "\r\n\r\n";
                    consoleText.Text = newText + consoleText.Text;
                };
                rollingTable.Controls.Add(rollButton);

                deleteButton.Click += delegate(object _, EventArgs __)
                {
                    using (new LayoutSuspender(rollingTable))
                    {
                        rollingTable.Controls.Remove(deleteButton);
                        rollingTable.Controls.Remove(countSpinner);
                        rollingTable.Controls.Remove(label);
                        rollingTable.Controls.Remove(dSpinner);
                        rollingTable.Controls.Remove(rollButton);
                    }
                };

                addButton.Click -= addButton_Click;
                addButton = new Button();
                addButton.Text = "&add";
                addButton.AutoSize = true;
                addButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                addButton.Click += addButton_Click;

                rollingTable.Controls.Add(addButton);
            }
        }
    }
}
