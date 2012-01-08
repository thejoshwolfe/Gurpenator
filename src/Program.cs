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

            GurpsDatabase database = null;
            for (int tryNumber = 0; tryNumber < 2; tryNumber++)
            {
                database = new GurpsDatabase();
                try { DataLoader.readData(database, Preferences.Instance.Databases); }
                catch (GurpenatorException e)
                {
                    if (tryNumber == 0)
                    {
                        var result = MessageBox.Show(e.Message + "\r\n\r\n" + "Revert Database preferences to default?", "Gurpenator - Database Problem", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                        if (result == DialogResult.Cancel)
                            return;
                        Preferences.Instance.Databases = Preferences.defaultDatabases;
                        continue;
                    }
                    else
                    {
                        MessageBox.Show(e.Message, "Gurpenator - Database Problem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                break;
            }
            var mainWindow = new CharacterSheet(database, Preferences.Instance.RecentCharacter);
            Application.Run(mainWindow);
        }
    }
}
