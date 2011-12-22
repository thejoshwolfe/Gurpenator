using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Gurpenator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var database = new GurpsDatabase();
            var sources = new List<string> { "core.gurpenator_data", "example.gurpenator_data" };
            DataLoader.readData(database, sources);
            Application.Run(new CharacterSheet(database));
        }
    }
}
