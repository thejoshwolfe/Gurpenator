﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

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
            var extension = ".gurpenator_data";
            dialog.Filter = "Gurpenator Database (*" + extension + ")|*" + extension;
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

            // check for errors
            try
            {
                DataLoader.readData(new GurpsDatabase(), items);
                setErrorMessage(null);
            }
            catch (GurpenatorException e)
            {
                setErrorMessage(e.Message);
            }
        }
        private void setErrorMessage(string message)
        {
            if (message != null)
                errorDisplayBox.Text = message;
            errorDisplayBox.Visible = message != null;
            okButton.Enabled = message == null;
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

        private void refreshButton_Click(object sender, EventArgs e)
        {
            refreshListBox();
        }
    }
}
