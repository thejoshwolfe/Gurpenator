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
        public MainWindow()
        {
            InitializeComponent();
            var nameToThing = DataLoader.readData(new List<string> { "../../example.gurpenator_data", "../../core.gurpenator_data" });

            // delete place holders
            attributesGroup.Controls.Clear();
            GurpenatorTable table = new GurpenatorTable(attributesGroup);
            var attributeNames = new string[] { "ST", "DX", "IQ", "HT", "Thrust" };
            foreach (string name in attributeNames)
                table.add(new GurpenatorRow(nameToThing[name]));
        }
    }
}
