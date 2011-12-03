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
        private static readonly Regex skillFormulaRegex = new Regex("^.+ [EAHV]$");

        public static void readData(IEnumerable<string> paths)
        {
            var nameToThing = new Dictionary<string, GurpsProperty>();
            // parse and check for duplicate names
            foreach (string path in paths)
            {
                foreach (ParsedThing parsedThing in parseFile(path))
                {
                    GurpsProperty property = interpretParsedThing(parsedThing);
                    try
                    {
                        nameToThing.Add(property.name, property);
                    }
                    catch (ArgumentException)
                    {
                        GurpsProperty previousThing = nameToThing[property.name];
                        throw new Exception("ERROR: duplicate definitions of \"" + parsedThing.name + "\". " + previousThing.parsedThing.getLocationString() + ", " + parsedThing.getLocationString());
                    }
                }
            }
        }

        private static GurpsProperty interpretParsedThing(ParsedThing parsedThing)
        {
            if (parsedThing.declarationOperator == ":" && skillFormulaRegex.IsMatch(parsedThing.formula))
                return interpretSkill(parsedThing);
            return interpretAttribute(parsedThing);
        }
        private static GurpsProperty interpretAttribute(ParsedThing parsedThing)
        {
            switch (parsedThing.declarationOperator)
            {
                case ":":
                    Formula costFormula = FormulaParser.parseFormula(parsedThing.formula, parsedThing);
                    return new Advantage(parsedThing, costFormula);
                case "=":
                    Formula formula = FormulaParser.parseFormula(parsedThing.formula, parsedThing);
                    return new AttributeFunction(parsedThing, formula);
                default:
                    throw new Exception("ERROR: Expected ':' or '='. Got '" + parsedThing.declarationOperator + "' " + parsedThing.getLocationString());
            }
        }
        private static GurpsProperty interpretSkill(ParsedThing parsedThing)
        {
            SkillDifficulty difficulty = difficultyFromChar(parsedThing.formula[parsedThing.formula.Length - 1]);
            Formula formula = FormulaParser.parseFormula(parsedThing.formula.Remove(parsedThing.formula.Length - 1), parsedThing);
            return new Skill(parsedThing, difficulty, formula);
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
                    ParsedThing thing = new ParsedThing(name, subPropertyName, declarationOperator, formula, comment, path, i + 1);
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
    }

    public class FormulaParser
    {
        public static Formula parseFormula(string text, ParsedThing parsedThing)
        {
            return new FormulaParser().parse(text, parsedThing);
        }
        private static readonly List<List<string>> operatorPrecidenceGroups = new List<List<string>> {
            new List<string> { "*", "/" },
            new List<string> { "+", "-" },
            new List<string> { "<=", "<", ">=", ">" },
            new List<string> { "AND", "OR" },
            new List<string> { "," },
        };
        // the lower the number, the higher the precidence
        private static readonly Dictionary<string, int> operatorPriority = new Dictionary<string, int>();
        private static readonly int lowestPriority = operatorPrecidenceGroups.Count + 1;
        static FormulaParser()
        {
            for (int i = 0; i < operatorPrecidenceGroups.Count; i++)
                foreach (string operator_ in operatorPrecidenceGroups[i])
                    operatorPriority.Add(operator_, i + 1); // start with 1
        }
        private Formula parse(string text, ParsedThing parsedThing)
        {
            var fullyParenthesized = new List<Token>();
            // http://en.wikipedia.org/wiki/Operator-precedence_parser#Alternatives_to_Dijkstra.27s_Algorithm
            Action<Token, int> addToList = delegate(Token token, int times)
            {
                for (int i = 0; i < times; i++)
                    fullyParenthesized.Add(token);
            };
            addToList(SymbolToken.OpenParens, lowestPriority);
            Token previousToken = null;
            foreach (Token token in tokenize(text))
            {
                SymbolToken symbolToken = token as SymbolToken;
                if (symbolToken != null)
                {
                    switch (symbolToken.text)
                    {
                        case "(":
                            addToList(SymbolToken.OpenParens, lowestPriority);
                            break;
                        case ")":
                            addToList(SymbolToken.CloseParens, lowestPriority);
                            break;
                        case "+":
                        case "-":
                            if (hasOpenRight(previousToken))
                            {
                                // actually a "positive" or "negavite" unary operator. not a "plus" or "minus".
                                if (symbolToken.text == "+")
                                    break; // let's not even acknowledge unary plus
                                fullyParenthesized.Add(token);
                                break;
                            }
                            goto default; // handle "plus" and "minus" with the rest of the binary operators
                        default:
                            addToList(SymbolToken.CloseParens, operatorPriority[symbolToken.text]);
                            fullyParenthesized.Add(token);
                            addToList(SymbolToken.OpenParens, operatorPriority[symbolToken.text]);
                            break;
                    }
                }
                else
                {
                    fullyParenthesized.Add(token);
                }
                previousToken = token;
            }
            addToList(SymbolToken.CloseParens, lowestPriority);

            int index = 0;
            Func<Token> getNextToken = delegate()
            {
                if (index < fullyParenthesized.Count)
                    return fullyParenthesized[index++];
                throw new Exception("ERROR: unexpected end of formula " + parsedThing.getLocationString());
            };
            Func<Formula> recurse = null;
            recurse = delegate()
            {
                Token firstToken = getNextToken();
                if (!(firstToken is SymbolToken))
                    return firstToken.toFormula();
                SymbolToken symbolToken = (SymbolToken)firstToken;
                switch (symbolToken.text)
                {
                    case "(":
                        Formula accumulator = recurse();
                        while (true)
                        {
                            SymbolToken nextToken = getNextToken() as SymbolToken;
                            if (nextToken == null)
                                throw new Exception("ERROR: expected symbol. found '" + fullyParenthesized[index - 1].ToString() + "' " + parsedThing.getLocationString());
                            if (nextToken.text == ")")
                                return accumulator;
                            // binary operator
                            Formula right = recurse();
                            accumulator = new Formula.Binary(accumulator, nextToken, right);
                        }
                    case "-":
                        return new Formula.Unary(symbolToken.text, recurse());
                    default:
                        throw new Exception("ERROR: unexpected symbol '" + symbolToken.text + "' " + parsedThing.getLocationString());
                }
            };
            return recurse();
        }

        private bool hasOpenRight(Token token)
        {
            if (token == null)
                return true;
            if (!(token is SymbolToken))
                return false;
            string symbol = ((SymbolToken)token).text;
            if (symbol == ")")
                return false;
            return true;
        }

        private static Regex tokenizerRegex = new Regex(
            @"(?<whitespace>\s+)|" +
            @"(?<symbol>\(|\)|\*|\+|,|-|/|<=|<|>=|>|IF|THEN|ELSE|AND|OR)|" +
            @"(?<literal>\d+%?)|" +
            @"(?<identifier>[A-Za-z_](?:[A-Za-z0-9_ ]*(?:\([A-Za-z0-9_ ]*\))?)*)|" +
            @"(?<invalid>.)" // catch anything else as invalid
        );
        private static IEnumerable<Token> tokenize(string text)
        {
            foreach (Match match in tokenizerRegex.Matches(text))
            {
                switch (findMatchedGroupName(tokenizerRegex, match))
                {
                    case "whitespace":
                        break; // ignore
                    case "symbol":
                        yield return new SymbolToken(match.Value);
                        break;
                    case "identifier":
                        yield return new IdentifierToken(match.Value.TrimEnd());
                        break;
                    case "literal":
                        if (match.Value.EndsWith("%"))
                            yield return new PercentToken(decimal.Parse(match.Value.Remove(match.Value.Length - 1)) / 100);
                        else
                            yield return new IntToken(int.Parse(match.Value));
                        break;
                    case "invalid":
                        throw new Exception("ERROR: invalid character in formula: " + match.Value);
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
        public virtual Formula toFormula()
        {
            throw new NotImplementedException();
        }
    }
    public class IdentifierToken : Token
    {
        public readonly string text;
        public IdentifierToken(string text)
        {
            this.text = text;
        }
        public override string ToString()
        {
            return text;
        }
        public override Formula toFormula()
        {
            return new Formula.Identifier(this);
        }
    }
    public class SymbolToken : Token
    {
        public static readonly SymbolToken OpenParens = new SymbolToken("(");
        public static readonly SymbolToken CloseParens = new SymbolToken(")");
        public readonly string text;
        public SymbolToken(string text)
        {
            this.text = text;
        }
        public override string ToString()
        {
            return text;
        }
    }
    public class IntToken : Token
    {
        public readonly int value;
        public IntToken(int value)
        {
            this.value = value;
        }
        public override string ToString()
        {
            return value.ToString();
        }
        public override Formula toFormula()
        {
            return new Formula.IntLiteral(this);
        }
    }
    public class PercentToken : Token
    {
        public readonly decimal value;
        public PercentToken(decimal value)
        {
            this.value = value;
        }
        public override string ToString()
        {
            return ((int)(value * 100)).ToString() + "%";
        }
        public override Formula toFormula()
        {
            return new Formula.PercentLiteral(this);
        }
    }
}
