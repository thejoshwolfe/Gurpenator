using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Gurpenator
{
    public partial class DatabaseListEditor : Form
    {
        public List<string> items;
        public DatabaseListEditor()
        {
            InitializeComponent();
            this.items = new List<string>(Preferences.Instance.Databases);
            refreshListBox();
        }

        private void listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            removeButton.Enabled = listBox.SelectedIndices.Count != 0;
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = dialog.ShowDialog(this);
            if (result != DialogResult.OK)
                return;
            string file = dialog.FileName;
            if (file.StartsWith(Directory.GetCurrentDirectory() + "\\"))
                file = file.Substring(Directory.GetCurrentDirectory().Length + 1);
            items.Add(file);
            refreshListBox();
        }

        private void refreshListBox()
        {
            items.Sort();
            listBox.Items.Clear();
            foreach (var item in items)
                listBox.Items.Add(item);
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            items.Remove((string)listBox.SelectedItem);
            refreshListBox();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Preferences.Instance.Databases = items;
            this.DialogResult = DialogResult.OK;
        }
    }
}
