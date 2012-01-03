using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gurpenator
{
    class Preferences
    {
        private static Preferences instance;
        public static Preferences Instance
        {
            get
            {
                if (instance == null)
                    instance = load();
                return instance;
            }
        }

        private List<string> databases;
        public List<string> Databases
        {
            get { return databases; }
            set
            {
                if (databases.Equals(value))
                    return;
                databases = value;
                save();
            }
        }
        private string recentCharacter;
        public string RecentCharacter
        {
            get { return recentCharacter; }
            set
            {
                if (recentCharacter == value)
                    return;
                recentCharacter = value;
                save();
            }
        }
        private Preferences() { }

        public void save()
        {
            var rootObject = new Dictionary<string, object>();
            rootObject["databases"] = new List<object>(databases);
            if (recentCharacter != null)
                rootObject["recentCharacter"] = recentCharacter;

            string serialization = DataLoader.jsonToString(rootObject);
            if (!Directory.Exists(preferencesDirectoryPath))
                Directory.CreateDirectory(preferencesDirectoryPath);
            File.WriteAllText(preferencesFilePath, serialization);
        }

        private static readonly string preferencesDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.gurpenator";
        private static readonly string preferencesFilePath = preferencesDirectoryPath + "/settings.json";
        private static Preferences load()
        {
            object jsonObject;
            try { jsonObject = DataLoader.stringToJson(File.ReadAllText(preferencesFilePath)); }
            catch (IOException)
            {
                jsonObject = new Dictionary<string, object>();
            }
            return fromJson((Dictionary<string, object>)jsonObject);
        }

        private static Preferences fromJson(Dictionary<string, object> rootObject)
        {
            var result = new Preferences();
            try { result.databases = new List<string>(from o in (List<object>)rootObject["databases"] select (string)o); }
            catch (KeyNotFoundException) { result.databases = new List<string> { "core.gurpenator_data" }; }
            try { result.recentCharacter = (string)rootObject["recentCharacter"]; }
            catch (KeyNotFoundException) { result.recentCharacter = null; }
            return result;
        }
    }
}
