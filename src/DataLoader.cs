using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Gurpenator
{
    static class DataLoader
    {
        // regex parsing? better than using a praser generator.
        private const string nameBeginningCharacters = @"A-Za-z_ \(\)!";
        private const string nameMiddleCharacters = "0-9" + nameBeginningCharacters;
        private const string namePattern = "[" + nameBeginningCharacters + "][" + nameMiddleCharacters + "]*";
        private const string commentPattern = "\"(.*?)\"";
        private static readonly Regex thingStartLineRegex = new Regex(@"^\s*(" + namePattern + @")(?:\.(" + namePattern + @"))?\s*(:|=|\+=|-=)([^\{" + "\"" + "]*)(?:" + commentPattern + @")?\s*(\{)?\s*$");
        private static readonly Regex commentRegex = new Regex(@"^\s*" + commentPattern + @"\s*$");
        public static void readData(IEnumerable<string> paths)
        {
            List<ParsedThing> things = new List<ParsedThing>();
            foreach (string path in paths)
                things.AddRange(readFile(path));
        }
        private static bool isLineBlank(string line)
        {
            return line == "" || line.StartsWith("#");
        }
        private static IEnumerable<ParsedThing> readFile(string path)
        {
            List<string> lines = readLines(path);
            for (int i = 0; i < lines.Count; i++)
            {
                string line;
                if (isLineBlank(line = lines[i].Trim()))
                    continue;
                Action throwParseError = delegate() { throw new Exception("ERROR: syntax problem: " + path + ":" + (i + 1)); };
                Match match = null;
                // hack for local recursion (for DRY)
                Func<ParsedThing> parseThing = null;
                parseThing = delegate()
                {
                    string name = match.Groups[1].Value.Trim();
                    string subPropertyName = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
                    string declarationOperator = match.Groups[3].Value;
                    string formula = match.Groups[4].Value.Trim();
                    string comment = match.Groups[5].Success ? match.Groups[5].Value.Trim() : null;
                    ParsedThing thing = new ParsedThing(name, subPropertyName, declarationOperator, formula, comment);
                    bool hasOpenBrace = match.Groups[6].Success;
                    if (hasOpenBrace)
                    {
                        i++;
                        int firstLineIndex = i;
                        for (; i < lines.Count; i++)
                        {
                            if (isLineBlank(line = lines[i].Trim()))
                                continue;
                            if (line == "}")
                                break;
                            if (i == firstLineIndex && (match = commentRegex.Match(line)).Success)
                            {
                                // the first line can be the comment
                                if (thing.comment != null)
                                    throwParseError(); // two comments?
                                thing.comment = match.Groups[1].Value;
                                continue;
                            }
                            if ((match = thingStartLineRegex.Match(line)).Success)
                            {
                                thing.subThings.Add(parseThing());
                                continue;
                            }
                            throwParseError();
                        }
                    }
                    return thing;
                };
                if ((match = thingStartLineRegex.Match(line)).Success)
                {
                    yield return parseThing();
                    continue;
                }
                throwParseError();
            }
        }

        public static List<string> readLines(string path)
        {
            using (StreamReader reader = File.OpenText(path))
            {
                List<string> result = new List<string>();
                while (!reader.EndOfStream)
                {
                    result.Add(reader.ReadLine());
                }
                return result;
            }
        }
        private class ParsedThing
        {
            public string name;
            public string subPropertyName;
            public string declarationOperator;
            public string formula;
            public string comment;
            public List<ParsedThing> subThings = new List<ParsedThing>();
            public ParsedThing(string name, string subPropertyName, string declarationOperator, string formula, string comment)
            {
                this.name = name;
                this.subPropertyName = subPropertyName;
                this.declarationOperator = declarationOperator;
                this.formula = formula;
                this.comment = comment;
            }
        }
    }
}
