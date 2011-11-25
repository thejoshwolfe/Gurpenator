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
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            DataLoader.readData(new string[] {"../../example.gurpenator_data"}.ToList());
        }
    }
}
