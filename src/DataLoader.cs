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
        private const string nameBeginningCharacters = @"A-Za-z_ &\(\)!";
        private const string nameMiddleCharacters = "0-9" + nameBeginningCharacters;
        private const string namePattern = "[" + nameBeginningCharacters + "][" + nameMiddleCharacters + "]*";
        private const string commentPattern = "\"(.*?)\"";
        private static readonly Regex thingStartLineRegex = new Regex(@"^\s*(" + namePattern + @")(?:\.(" + namePattern + @"))?\s*(:=|:|=|\+=|-=)([^\{" + "\"" + "]*)(?:" + commentPattern + @")?\s*(\{)?\s*$");
        private static readonly Regex commentRegex = new Regex(@"^\s*" + commentPattern + @"\s*$");
        private static readonly Regex skillFormulaRegex = new Regex("^.+ [EAHV]$");

        public static readonly HashSet<string> reservedWords = new HashSet<string> { "level", "cost", "none" };

        public static void readData(GurpsDatabase database, IEnumerable<string> paths)
        {
            var nameToThing = database.nameToThing;
            // parse and check for duplicate names
            foreach (string path in paths)
            {
                foreach (ParsedThing parsedThing in parseFile(path))
                {
                    GurpsProperty property = interpretParsedThing(parsedThing);
                    try { nameToThing.Add(property.name, property); }
                    catch (ArgumentException)
                    {
                        GurpsProperty previousThing = nameToThing[property.name];
                        throw new Exception("ERROR: duplicate definitions of \"" + parsedThing.name + "\". " + previousThing.parsedThing.getLocationString() + ", " + parsedThing.getLocationString());
                    }
                }
            }

            // make sure core attributes are defined
            foreach (string name in GurpsCharacter.coreAttributeNames)
                if (!nameToThing.ContainsKey(name))
                    throw new Exception("ERROR: missing definition of core attribute \"" + name + "\"");

            checkFormulas(nameToThing);

            // tweek some attributes specially
            nameToThing["Thrust"].formattingFunction = GurpsProperty.formatAsDice;
            nameToThing["Swing"].formattingFunction = GurpsProperty.formatAsDice;
            // display Basic Speed in m/s rather than Basic Speed x4 in m/s*4.
            nameToThing["Basic Speed x4"].DisplayName = "Basic Speed";
            nameToThing["Basic Speed x4"].formattingFunction = delegate(int value) { return (value * 0.25).ToString(); };
        }

        private static void checkFormulas(Dictionary<string, GurpsProperty> nameToThing)
        {
            // check names are defined; check variables are proper type; link parent skills
            foreach (GurpsProperty property in nameToThing.Values)
            {
                CheckingContext context = new CheckingContext(nameToThing, property);
                if (property is Advantage) { ((Advantage)property).costFormula.checkIsInt(context); }
                else if (property is AttributeFunction) { ((AttributeFunction)property).formula.checkIsInt(context); }
                else if (property is Skill) { ((Skill)property).formula.checkIsInt(context); }
                else if (property is InheritedSkill)
                {
                    InheritedSkill inheritedSkill = (InheritedSkill)property;
                    IdentifierToken parentNameToken = inheritedSkill.parentSkillToken;
                    GurpsProperty parent;
                    try { parent = nameToThing[parentNameToken.text]; }
                    catch (KeyNotFoundException) { throw parentNameToken.parseThing.createError("Parent skill not defined '" + parentNameToken.text + "'"); }
                    if (!(parent is AbstractSkill))
                        throw parentNameToken.parseThing.createError("Parent is not a skill '" + parentNameToken.text + "'");
                    inheritedSkill.parent = (AbstractSkill)parent;
                }
                foreach (Effect effect in property.effects)
                {
                    GurpsProperty affectedProperty;
                    try { affectedProperty = nameToThing[effect.traitName]; }
                    catch (KeyNotFoundException) { throw effect.parsedThing.createError("Name not found '" + effect.traitName + "'"); }
                    affectedProperty.effectedBy.Add(effect);
                    if (effect is TraitModifier)
                    {
                        if (affectedProperty is BooleanAdvantage) // TODO: use hasLevels or whatever
                            throw effect.parsedThing.createError("Cannot modify the level of a trait with no level");
                        effect.formula.checkIsInt(context);
                    }
                    else if (effect is CostModifier)
                    {
                        if (affectedProperty is AttributeFunction) // TODO: use hasCost or whatever
                            throw effect.parsedThing.createError("Cannot modify the cost of a trait with no cost");
                        effect.formula.checkIsPercent(context);
                    }
                    else
                        throw null;
                }
            }

            // check for circularity
            foreach (GurpsProperty property in nameToThing.Values)
            {
                if (property is InheritedSkill)
                {
                    var visited = new HashSet<InheritedSkill>();
                    Action<InheritedSkill> recurse = null;
                    recurse = delegate(InheritedSkill skill)
                    {
                        if (!visited.Add(skill))
                        {
                            string names = string.Join(", ", from s in visited select s.name + " " + s.parsedThing.getLocationString());
                            throw new Exception("ERROR: recursive skill inheritance: " + names);
                        }
                        if (skill.parent is InheritedSkill)
                            recurse((InheritedSkill)skill.parent);
                        visited.Remove(skill);
                    };
                    InheritedSkill inheritedSkill = (InheritedSkill)property;
                    recurse(inheritedSkill);
                    // also check optional specialty rulz
                    if (inheritedSkill.category)
                    {
                        if (!inheritedSkill.parent.category)
                            throw inheritedSkill.parsedThing.createError("Optional specialties cannot be categories");
                    }
                    else
                    {
                        if (inheritedSkill.parent is InheritedSkill && !((InheritedSkill)inheritedSkill.parent).parent.category)
                            throw inheritedSkill.parsedThing.createError("Optional specialties cannot be based on optional specialties");
                    }
                }
                else if (property is AttributeFunction)
                {
                    // this could probably be generalized greatly
                    AttributeFunction function = (AttributeFunction)property;
                    // per undocumented behavior, the Dictionary class seems to perserve insertion order
                    var visited = new HashSet<AttributeFunction>();
                    Action<AttributeFunction> recurse = null;
                    recurse = delegate(AttributeFunction localFunction)
                    {
                        if (!visited.Add(localFunction))
                        {
                            string names = string.Join(", ", from f in visited select f.name + " " + f.parsedThing.getLocationString());
                            throw new Exception("ERROR: recursive functions: " + names);
                        }
                        foreach (var name in localFunction.usedNames())
                        {
                            GurpsProperty usedProperty = nameToThing[name];
                            if (usedProperty is AttributeFunction)
                                recurse((AttributeFunction)usedProperty);
                        }
                        visited.Remove(localFunction);
                    };
                    recurse(function);
                }
            }
        }

        private static GurpsProperty interpretParsedThing(ParsedThing parsedThing)
        {
            GurpsProperty property = createParsedThing(parsedThing);
            foreach (ParsedThing subThing in parsedThing.subThings)
            {
                if (new string[] { "category", "default", "requires" }.Contains(subThing.name))
                {
                    if (subThing.subPropertyName != null)
                        throw subThing.createError("property '" + subThing.name + "' has no subproperty '" + subThing.subPropertyName + "'");
                    Formula formula = FormulaParser.parseFormula(subThing.formula, subThing);
                    switch (subThing.name)
                    {
                        case "category":
                            if (!(formula is Formula.BooleanLiteral))
                                throw subThing.createError("categry can only be 'true' or 'false'");
                            if (!(property is AbstractSkill))
                                throw subThing.createError("only skills can be categories");
                            ((AbstractSkill)property).category = ((Formula.BooleanLiteral)formula).value.value;
                            continue;
                        case "default":
                        case "requires":
                            // TODO
                            continue;
                    }
                    throw null;
                }
                switch (subThing.declarationOperator)
                {
                    case "+=":
                    case "-=":
                        {
                            string traitName = subThing.name;
                            Formula formula = FormulaParser.parseFormula(subThing.formula, subThing);
                            if (subThing.subPropertyName == null)
                            {
                                if (subThing.declarationOperator == "-=")
                                {
                                    // negate the formula
                                    formula = new Formula.UnaryPrefix(new SymbolToken(subThing, "-"), formula);
                                }
                                property.effects.Add(new TraitModifier(property, traitName, formula, subThing));
                            }
                            else if (subThing.subPropertyName == "cost")
                                property.effects.Add(new CostModifier(property, traitName, formula, subThing));
                            else
                                throw subThing.createError("can't modify the '" + subThing.subPropertyName + "' of another trait");
                            if (subThing.subThings.Count > 0)
                                throw subThing.subThings[0].createError("Subitems not allowed here");
                            continue;
                        }
                    case ":":
                        if (subThing.subPropertyName != null)
                            throw subThing.createError("No '.' allowed in names");
                        // TODO
                        continue;
                }
                throw subThing.createError("illegal operator '" + subThing.declarationOperator + "' for property");
            }
            return property;
        }
        private static GurpsProperty createParsedThing(ParsedThing parsedThing)
        {
            if (parsedThing.subPropertyName != null)
                throw parsedThing.createError("No '.' allowed in names");
            switch (parsedThing.declarationOperator)
            {
                case ":":
                    if (skillFormulaRegex.IsMatch(parsedThing.formula))
                    {
                        SkillDifficulty difficulty = difficultyFromChar(parsedThing.formula[parsedThing.formula.Length - 1]);
                        Formula formula = FormulaParser.parseFormula(parsedThing.formula.Remove(parsedThing.formula.Length - 1), parsedThing);
                        return new Skill(parsedThing, difficulty, formula);
                    }
                    else
                    {
                        Formula costFormula = FormulaParser.parseFormula(parsedThing.formula, parsedThing);
                        if (costFormula.usedNames().Contains("level"))
                            return new IntAdvantage(parsedThing, costFormula);
                        else
                            return new BooleanAdvantage(parsedThing, costFormula);
                    }
                case ":=":
                    {
                        SkillDifficulty difficulty = SkillDifficulty.Unspecified;
                        string formulaText = parsedThing.formula;
                        if (skillFormulaRegex.IsMatch(parsedThing.formula))
                        {
                            difficulty = difficultyFromChar(parsedThing.formula[parsedThing.formula.Length - 1]);
                            formulaText = parsedThing.formula.Remove(parsedThing.formula.Length - 1);
                        }
                        Formula formula = FormulaParser.parseFormula(formulaText, parsedThing);
                        if (!(formula is Formula.Identifier))
                            throw new Exception("ERROR: expected name of skill. got '" + formulaText + "' " + parsedThing.getLocationString());
                        return new InheritedSkill(parsedThing, difficulty, ((Formula.Identifier)formula).token);
                    }
                case "=":
                    {
                        Formula formula = FormulaParser.parseFormula(parsedThing.formula, parsedThing);
                        return new AttributeFunction(parsedThing, formula);
                    }
                default:
                    throw new Exception("ERROR: expected ':', '=', or ':='. Got '" + parsedThing.declarationOperator + "' " + parsedThing.getLocationString());
            }
        }
        private static SkillDifficulty difficultyFromChar(char c)
        {
            switch (c)
            {
                case 'E':
                    return SkillDifficulty.Easy;
                case 'A':
                    return SkillDifficulty.Average;
                case 'H':
                    return SkillDifficulty.Hard;
                case 'V':
                    return SkillDifficulty.VeryHard;
            }
            throw null;
        }

        private static bool isLineBlank(string line)
        {
            return line == "" || line.StartsWith("#");
        }
        private static IEnumerable<ParsedThing> parseFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line;
                if (isLineBlank(line = lines[i].Trim()))
                    continue;
                Action throwParseError = delegate() { throw new Exception("ERROR: syntax problem (" + path + ":" + (i + 1) + ")"); };
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
                    ParsedThing thing = new ParsedThing(name, subPropertyName, declarationOperator, formula, comment, path, i + 1);
                    if (reservedWords.Contains(name))
                        throw new Exception("ERROR: name '" + name + "' is reserved " + thing.getLocationString());
                    bool hasOpenBrace = match.Groups[6].Success;
                    if (hasOpenBrace)
                    {
                        i++;
                        int firstLineIndex = i;
                        for (; i < lines.Length; i++)
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

        private static readonly Dictionary<char, string> escapeSequences = new Dictionary<char, string> {
            {'\\', @"\\"},
            {'"', "\\\""},
            {'\r', "\\r"},
            {'\n', "\\n"},
            {'\t', "\\t"},
        };
        private static readonly Dictionary<char, char> reverseEscapeSequences = new Dictionary<char, char>();
        static DataLoader()
        {
            foreach (var item in escapeSequences)
                reverseEscapeSequences.Add(item.Value[1], item.Key);
        }
        public static string jsonToString(object outterJsonObject)
        {
            StringBuilder result = new StringBuilder();
            Action<object, int> recurse = null;
            recurse = delegate(object jsonObject, int indentCount)
            {
                Action appendIndent = delegate()
                {
                    for (int i = 0; i < indentCount; i++)
                        result.Append("    ");
                };
                if (jsonObject is Dictionary<string, object>)
                {
                    var dict = (Dictionary<string, object>)jsonObject;
                    result.AppendLine("{");
                    indentCount++;
                    foreach (var item in dict)
                    {
                        string key = item.Key;
                        appendIndent();
                        recurse(key, indentCount);
                        result.Append(": ");
                        recurse(item.Value, indentCount);
                        result.AppendLine(",");
                    }
                    indentCount--;
                    appendIndent();
                    result.Append("}");
                }
                else if (jsonObject is List<object>)
                {
                    var list = (List<object>)jsonObject;
                    result.AppendLine("[");
                    indentCount++;
                    foreach (object item in list)
                    {
                        appendIndent();
                        recurse(item, indentCount);
                        result.AppendLine(",");
                    }
                    indentCount--;
                    appendIndent();
                    result.Append("]");
                }
                else if (jsonObject is string)
                {
                    var str = (string)jsonObject;
                    result.Append('"');
                    foreach (char c in str)
                    {
                        string escapeSequence;
                        if (escapeSequences.TryGetValue(c, out escapeSequence))
                            result.Append(escapeSequence);
                        else
                            result.Append(c);
                    }
                    result.Append('"');
                }
                else if (jsonObject is int)
                {
                    result.Append((int)jsonObject);
                }
                else
                    throw null;
            };
            recurse(outterJsonObject, 0);
            result.AppendLine();
            return result.ToString();
        }
        public static object stringToJson(string serialization)
        {
            int index = 0;
            Func<int> skipWhitespace = delegate
            {
                for (; index < serialization.Length; index++)
                    if (!char.IsWhiteSpace(serialization[index]))
                        break;
                return index;
            };
            Func<object> recurse = null;
            recurse = delegate()
            {
                switch (serialization[skipWhitespace()])
                {
                    case '{':
                        {
                            index++;
                            var dict = new Dictionary<string, object>();
                            while (serialization[skipWhitespace()] != '}')
                            {
                                var key = (string)recurse();
                                if (serialization[skipWhitespace()] != ':')
                                    throw null;
                                index++;
                                var value = recurse();
                                dict.Add(key, value);
                                if (serialization[skipWhitespace()] == ',')
                                    index++;
                            }
                            index++;
                            return dict;
                        }
                    case '[':
                        {
                            index++;
                            var list = new List<object>();
                            while (serialization[skipWhitespace()] != ']')
                            {
                                var item = recurse();
                                list.Add(item);
                                if (serialization[skipWhitespace()] == ',')
                                    index++;
                            }
                            index++;
                            return list;
                        }
                    case '"':
                        {
                            index++;
                            var result = new StringBuilder();
                            while (serialization[index] != '"')
                            {
                                char c = serialization[index];
                                if (c != '\\')
                                    result.Append(c);
                                else
                                {
                                    index++;
                                    result.Append(reverseEscapeSequences[serialization[index]]);
                                }
                                index++;
                            }
                            index++;
                            return result.ToString();
                        }
                    default:
                        {
                            int startIndex = index;
                            for (; index < serialization.Length; index++)
                                if (!(char.IsNumber(serialization[index]) || serialization[index] == '-'))
                                    break;
                            return int.Parse(serialization.Substring(startIndex, index - startIndex));
                        }
                }
            };
            var outterResult = recurse();
            if (skipWhitespace() != serialization.Length)
                throw null;
            return outterResult;
        }
    }
    public class ParsedThing
    {
        public string name;
        public string subPropertyName;
        public string declarationOperator;
        public string formula;
        public string comment;
        public string sourcePath;
        public int lineNumber;
        public List<ParsedThing> subThings = new List<ParsedThing>();
        public ParsedThing(string name, string subPropertyName, string declarationOperator, string formula, string comment, string sourcePath, int lineNumber)
        {
            this.name = name;
            this.subPropertyName = subPropertyName;
            this.declarationOperator = declarationOperator;
            this.formula = formula;
            this.comment = comment;
            this.sourcePath = sourcePath;
            this.lineNumber = lineNumber;
        }
        public string getLocationString()
        {
            return "(" + sourcePath + ":" + lineNumber + ")";
        }

        public Exception createError(string message)
        {
            return new Exception("ERROR: " + message + " " + getLocationString());
        }
    }

    public class FormulaParser
    {
        public static Formula parseFormula(string text, ParsedThing parsedThing)
        {
            return new FormulaParser(parsedThing).parse(text);
        }
        private static readonly List<List<string>> operatorPrecedenceGroups = new List<List<string>> {
            new List<string> { "*", "/" },
            new List<string> { "+", "-" },
            new List<string> { "<=", "<", ">=", ">" },
            new List<string> { "AND" },
            new List<string> { "OR" },
            new List<string> { "IF" },
            new List<string> { "," },
        };
        // the lower the number, the higher the precedence
        private static readonly Dictionary<string, int> operatorPriority = new Dictionary<string, int>();
        static FormulaParser()
        {
            for (int i = 0; i < operatorPrecedenceGroups.Count; i++)
                foreach (string operator_ in operatorPrecedenceGroups[i])
                    operatorPriority.Add(operator_, i);
        }

        private ParsedThing parsedThing;
        public FormulaParser(ParsedThing parsedThing)
        {
            this.parsedThing = parsedThing;
        }
        private void throwError(string message)
        {
            throw new Exception("ERROR: " + message + " " + parsedThing.getLocationString());
        }
        private Formula parse(string text)
        {
            List<Token> tokens = new List<Token>(tokenize(text));
            int index = 0;
            Action<string> checkNextTokenIsSymbol = delegate(string symbolText)
            {
                if (index >= tokens.Count)
                    throwError("unexpected end of formula");
                SymbolToken result = tokens[index++] as SymbolToken;
                if (result == null || result.text != symbolText)
                    throwError("expected '" + symbolText + "'. got '" + tokens[index - 1].ToString() + "'");
            };
            Func<Formula> recurse = null;
            recurse = delegate()
            {
                List<Formula> treeBuffer = new List<Formula>();
                {
                    Func<bool> hasOpenRight = delegate()
                    {
                        if (treeBuffer.Count == 0)
                            return true;
                        return !(treeBuffer[treeBuffer.Count - 1] is Formula.Leaf);
                    };
                    while (index < tokens.Count)
                    {
                        Token token = tokens[index++];
                        SymbolToken symbolToken = token as SymbolToken;
                        if (symbolToken == null)
                        {
                            treeBuffer.Add(token.toFormula());
                            continue;
                        }
                        switch (symbolToken.text)
                        {
                            case "(":
                                treeBuffer.Add(recurse());
                                checkNextTokenIsSymbol(")");
                                break;
                            case "IF":
                                {
                                    Formula condition = recurse();
                                    checkNextTokenIsSymbol("THEN");
                                    Formula thenPart = recurse();
                                    checkNextTokenIsSymbol("ELSE");
                                    treeBuffer.Add(new Formula.Conditional(symbolToken, condition, thenPart));
                                    break;
                                }
                            case ")":
                            case "THEN":
                            case "ELSE":
                                index--;
                                goto breakMainFor;
                            case "+":
                            case "-":
                                if (hasOpenRight())
                                {
                                    // actually a "positive" or "negavite" unary operator. not a "plus" or "minus".
                                    if (symbolToken.text == "+")
                                        break; // let's not even acknowledge unary plus
                                    treeBuffer.Add(new Formula.UnaryPrefix(symbolToken));
                                    break;
                                }
                                goto default; // handle "plus" and "minus" with the rest of the binary operators
                            default:
                                if (!operatorPriority.ContainsKey(symbolToken.text))
                                    throwError("symbol out of place '" + symbolToken.text + "'");
                                treeBuffer.Add(new Formula.Binary(symbolToken));
                                break;
                        }
                    }
                }
            breakMainFor:
                // group the tree buffer into a proper tree
                {
                    Action<int> checkHasClosedLeft = delegate(int i)
                    {
                        if (i >= treeBuffer.Count)
                            throwError("expected something after '" + treeBuffer[i - 1].ToString() + "'");
                        Formula formula = treeBuffer[i];
                        if (formula is Formula.Binary)
                            if (((Formula.Binary)formula).left == Formula.NullFormula.Instance)
                                throwError("expected something between '" + treeBuffer[i - 1].ToString() + "' and '" + ((Formula.Binary)formula).operator_.text + "'");
                    };
                    Action<int> checkHasClosedRight = delegate(int i)
                    {
                        if (i < 0)
                            throwError("can't start an expression with '" + treeBuffer[i + 1].ToString() + "'");
                        Formula formula = treeBuffer[i];
                        if (formula is Formula.Binary)
                            if (((Formula.Binary)formula).right == Formula.NullFormula.Instance)
                                throwError("expected something between '" + ((Formula.Binary)formula).operator_.text + "' and '" + treeBuffer[i + 1].ToString() + "'");
                        if (formula is Formula.UnaryPrefix)
                            if (((Formula.UnaryPrefix)formula).operand == Formula.NullFormula.Instance)
                                throwError("expected something between '" + ((Formula.UnaryPrefix)formula).operator_.text + "' and '" + treeBuffer[i + 1].ToString() + "'");
                    };
                    // O(n*n*m). Oh well.
                    foreach (List<string> operatorGroup in operatorPrecedenceGroups)
                    {
                        // prefix operators first so that -1-2 is (-1)-2 instead of -(1-2).
                        for (int i = treeBuffer.Count - 1; i >= 0; i--)
                        {
                            Formula.UnaryPrefix unaryPrefix = treeBuffer[i] as Formula.UnaryPrefix;
                            if (unaryPrefix == null || unaryPrefix.operand != Formula.NullFormula.Instance)
                                continue;
                            if (!operatorGroup.Contains(unaryPrefix.operator_.text))
                                continue;
                            checkHasClosedLeft(i + 1);
                            unaryPrefix.operand = treeBuffer[i + 1];
                            treeBuffer.RemoveAt(i + 1);
                        }
                        for (int i = 0; i < treeBuffer.Count; i++)
                        {
                            Formula.Binary binary = treeBuffer[i] as Formula.Binary;
                            if (binary == null || binary.left != Formula.NullFormula.Instance)
                                continue;
                            if (binary.right != Formula.NullFormula.Instance)
                                throw null;
                            if (!operatorGroup.Contains(binary.operator_.text))
                                continue;
                            checkHasClosedLeft(i + 1);
                            checkHasClosedRight(i - 1);
                            binary.left = treeBuffer[i - 1];
                            binary.right = treeBuffer[i + 1];
                            treeBuffer.RemoveAt(i - 1);
                            i--;
                            treeBuffer.RemoveAt(i + 1);
                        }
                    }
                    if (treeBuffer.Count == 0)
                        throwError("empty expression");
                    if (treeBuffer.Count > 1)
                        throwError("expected end of expression after '" + treeBuffer[0].ToString() + "'");
                    return treeBuffer[0];
                }
            };
            {
                Formula result = recurse();
                if (index != tokens.Count)
                    throwError("expected end of expression. got '" + tokens[index].ToString() + "'");
                return result;
            }
        }

        private static Regex tokenizerRegex = new Regex(
            @"(?<whitespace>\s+)|" +
            @"(?<symbol>\(|\)|\*|\+|,|-|/|<=|<|>=|>|IF|THEN|ELSE|AND|OR)|" +
            @"(?<numberLiteral>\d+%?)|" +
            @"(?<booleanLiteral>true|false)|" +
            @"(?<identifier>[A-Za-z_](?:[A-Za-z0-9_ &]*(?:\([A-Za-z0-9_ &]*\))?)*)|" +
            @"(?<invalid>.)" // catch anything else as invalid
        );
        private IEnumerable<Token> tokenize(string text)
        {
            foreach (Match match in tokenizerRegex.Matches(text))
            {
                switch (findMatchedGroupName(tokenizerRegex, match))
                {
                    case "whitespace":
                        break; // ignore
                    case "symbol":
                        yield return new SymbolToken(parsedThing, match.Value);
                        break;
                    case "identifier":
                        yield return new IdentifierToken(parsedThing, match.Value.TrimEnd());
                        break;
                    case "numberLiteral":
                        if (match.Value.EndsWith("%"))
                            yield return new PercentToken(parsedThing, decimal.Parse(match.Value.Remove(match.Value.Length - 1)) / 100);
                        else
                            yield return new IntToken(parsedThing, int.Parse(match.Value));
                        break;
                    case "booleanLiteral":
                        yield return new BooleanToken(parsedThing, match.Value == "true");
                        break;
                    case "invalid":
                        throwError("invalid character in formula '" + match.Value + "'");
                        throw null;
                    default:
                        throw null;
                }
            }
        }

        private static string findMatchedGroupName(Regex regex, Match match)
        {
            string[] names = regex.GetGroupNames();
            int[] numbers = regex.GetGroupNumbers();
            for (int i = 1; i < numbers.Length; i++)
                if (match.Groups[numbers[i]].Success)
                    return names[i];
            throw null;
        }
    }
    public abstract class Token
    {
        public ParsedThing parseThing;
        public Token(ParsedThing parseThing)
        {
            this.parseThing = parseThing;
        }
        public virtual Formula toFormula() { throw new NotImplementedException(); }
    }
    public class IdentifierToken : Token
    {
        public readonly string text;
        public IdentifierToken(ParsedThing parseThing, string text)
            : base(parseThing)
        {
            this.text = text;
        }
        public override string ToString() { return text; }
        public override Formula toFormula() { return new Formula.Identifier(this); }
    }
    public class SymbolToken : Token
    {
        public readonly string text;
        public SymbolToken(ParsedThing parseThing, string text)
            : base(parseThing)
        {
            this.text = text;
        }
        public override string ToString() { return text; }
    }
    public class IntToken : Token
    {
        public readonly int value;
        public IntToken(ParsedThing parseThing, int value)
            : base(parseThing)
        {
            this.value = value;
        }
        public override string ToString() { return value.ToString(); }
        public override Formula toFormula() { return new Formula.IntLiteral(this); }
    }
    public class PercentToken : Token
    {
        public readonly decimal value;
        public PercentToken(ParsedThing parseThing, decimal value)
            : base(parseThing)
        {
            this.value = value;
        }
        public override string ToString() { return ((int)(value * 100)).ToString() + "%"; }
        public override Formula toFormula() { return new Formula.PercentLiteral(this); }
    }
    public class BooleanToken : Token
    {
        public readonly bool value;
        public BooleanToken(ParsedThing parseThing, bool value)
            : base(parseThing)
        {
            this.value = value;
        }
        public override string ToString() { return value ? "true" : "false"; }
        public override Formula toFormula() { return new Formula.BooleanLiteral(this); }
    }
}
