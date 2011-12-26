using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Gurpenator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var database = new GurpsDatabase();
            DataLoader.readData(database, Preferences.Instance.Databases);
            var mainWindow = new CharacterSheet(database, Preferences.Instance.RecentCharacter);
            Application.Run(mainWindow);
        }
    }
}
