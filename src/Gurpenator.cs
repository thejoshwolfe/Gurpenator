using System;
using System.Collections.Generic;
using System.Linq;

namespace Gurpenator
{
    public abstract class GurpsProperty
    {
        public static string formatAsDice(int value)
        {
            int dice = Math.Max(1, (value + 1) / 4);
            int modifier = value - dice * 4;
            if (modifier < 0)
                return dice + "d" + modifier;
            if (modifier == 0)
                return dice + "d";
            return dice + "d+" + modifier;
        }

        public string name { get { return parsedThing.name; } }
        private string displayName = null;
        public string DisplayName
        {
            get { return displayName == null ? name : displayName; }
            set { displayName = value; }
        }
        public ParsedThing parsedThing;
        protected GurpsProperty(ParsedThing parsedThing)
        {
            this.parsedThing = parsedThing;
        }

        public static readonly Func<int, string> toStringFormattingFunction = (value) => value == int.MinValue ? "NA" : value.ToString();
        public Func<int, string> formattingFunction = toStringFormattingFunction;
        public virtual IEnumerable<string> usedNames() { yield break; }
    }
    public enum SkillDifficulty
    {
        Unspecified, Easy, Average, Hard, VeryHard
    }
    public abstract class AbstractSkill : GurpsProperty
    {
        protected AbstractSkill(ParsedThing parsedThing)
            : base(parsedThing) { }

        protected static int difficultyOffset(SkillDifficulty difficulty)
        {
            switch (difficulty)
            {
                case SkillDifficulty.Easy: return -1;
                case SkillDifficulty.Average: return -2;
                case SkillDifficulty.Hard: return -3;
                case SkillDifficulty.VeryHard: return -4;
            }
            throw null;
        }
    }
    public class Skill : AbstractSkill
    {
        public SkillDifficulty difficulty;
        public Formula formula;
        public Skill(ParsedThing parsedThing, SkillDifficulty difficulty, Formula formula)
            : base(parsedThing)
        {
            this.difficulty = difficulty;
            this.formula = formula;
        }
        public override IEnumerable<string> usedNames() { return formula.usedNames(); }

        public int difficultyOffset()
        {
            return difficultyOffset(difficulty);
        }
    }
    public class InheritedSkill : AbstractSkill
    {
        private SkillDifficulty difficultyOverride;
        private string parentSkillName;
        public InheritedSkill(ParsedThing parsedThing, SkillDifficulty difficultyOverride, string parentSkillName)
            : base(parsedThing)
        {
            this.difficultyOverride = difficultyOverride;
            this.parentSkillName = parentSkillName;
        }
        public override IEnumerable<string> usedNames() { yield return parentSkillName; }
    }
    public abstract class Advantage : GurpsProperty
    {
        public Formula costFormula;
        public Advantage(ParsedThing parsedThing, Formula costFormula)
            : base(parsedThing)
        {
            this.costFormula = costFormula;
        }
        public override IEnumerable<string> usedNames() { return costFormula.usedNames(); }
    }
    public class IntAdvantage : Advantage
    {
        public IntAdvantage(ParsedThing parsedThing, Formula costFormula)
            : base(parsedThing, costFormula) { }
    }
    public class BooleanAdvantage : Advantage
    {
        public static readonly Func<int, string> intToBoolFormattingFunction = (value) => value != 0 ? "yes" : "no";
        public BooleanAdvantage(ParsedThing parsedThing, Formula costFormula)
            : base(parsedThing, costFormula)
        {
            formattingFunction = intToBoolFormattingFunction;
        }
    }
    public class AttributeFunction : GurpsProperty
    {
        public Formula formula;
        public AttributeFunction(ParsedThing parsedThing, Formula formula)
            : base(parsedThing)
        {
            this.formula = formula;
        }
        public override IEnumerable<string> usedNames() { return formula.usedNames(); }
    }

    public class PurchasedProperty
    {
        public GurpsProperty property;
        private GurpsCharacter character;
        public PurchasedProperty(GurpsProperty property, GurpsCharacter character)
        {
            this.property = property;
            this.character = character;
        }
        public bool hasLevel { get { return !(property is BooleanAdvantage); } }
        public int getLevel()
        {
            if (property is Advantage)
                return purchasedLevels;
            if (property is AttributeFunction)
                return ((AttributeFunction)property).formula.evalInt(new EvaluationContext(character, this));
            if (property is Skill)
            {
                Skill skill = (Skill)property;
                if (purchasedLevels == 0)
                {
                    return int.MinValue; // TODO: defaults
                }
                else
                {
                    int attribute = skill.formula.evalInt(new EvaluationContext(character, this));
                    int difficultyOffset = skill.difficultyOffset();
                    return attribute + difficultyOffset + purchasedLevels;
                }
            }
            if (property is InheritedSkill)
                return 0; // TODO
            throw null;
        }
        public bool nonDefault { get { return purchasedLevels > 0; } }
        public string getFormattedValue() { return property.formattingFunction(getLevel()); }
        public bool hasCost { get { return !(property is AttributeFunction); } }
        public int getCost()
        {
            EvaluationContext context = new EvaluationContext(character, this, purchasedLevels);
            if (property is Advantage)
                return ((Advantage)property).costFormula.evalInt(context);
            if (property is AbstractSkill)
            {
                // purchased: 0, 1, 2, 3, 4, ... +1
                // cost:      0, 1, 2, 4, 8, ... +4
                return purchasedLevels < 3 ? purchasedLevels : 4 * (purchasedLevels - 2);
            }
            if (property is AttributeFunction)
                return 0;
            throw null;
        }

        public bool hasPurchasedLevels { get { return property is IntAdvantage || property is AbstractSkill; } }
        private int purchasedLevels = 0;
        public int PurchasedLevels
        {
            get { return purchasedLevels; }
            set
            {
                purchasedLevels = value;
                handleChange();
            }
        }

        private string previousSerialization = "";
        public void handleChange()
        {
            string serialization = this.ToString();
            if (serialization == previousSerialization)
                return;
            previousSerialization = serialization;
            if (changed != null) changed();
        }

        public event Action changed;

        public override string ToString()
        {
            return property.name + ":" + purchasedLevels + ":" + getCost() + ":" + getLevel();
        }
    }

    public class GurpsCharacter
    {
        private static readonly string[] attributeNames = {
            "TL",
            "ST", "DX", "IQ", "HT",
            "HP", "Will", "Per", "FP",
            "Basic Lift",
            "Basic Speed x4",
            "Basic Move",
            "Thrust", "Swing",
            "Dodge",
            "Fright Check",
        };
        private static readonly string[] hiddenAttributeNames = {
            "Basic Lift ST",
            "Damage ST",
        };
        public static IEnumerable<string> coreAttributeNames { get { return attributeNames.Concat(hiddenAttributeNames); } }

        private Dictionary<string, PurchasedProperty> nameToPurchasedAttribute = new Dictionary<string, PurchasedProperty>();
        public GurpsCharacter(Dictionary<string, GurpsProperty> nameToThing)
        {
            foreach (GurpsProperty property in nameToThing.Values)
                nameToPurchasedAttribute[property.name] = new PurchasedProperty(property, this);
            foreach (PurchasedProperty purchasedProperty in nameToPurchasedAttribute.Values)
            {
                foreach (string name in purchasedProperty.property.usedNames())
                {
                    if (DataLoader.reservedWords.Contains(name))
                        continue;
                    nameToPurchasedAttribute[name].changed += purchasedProperty.handleChange; ;
                }
            }
        }
        public IEnumerable<PurchasedProperty> visibleAttributes
        {
            get
            {
                foreach (string attributeName in attributeNames)
                    yield return nameToPurchasedAttribute[attributeName];
            }
        }
        public IEnumerable<PurchasedProperty> otherTraits
        {
            get
            {
                HashSet<string> visibleSet = new HashSet<string>(attributeNames);
                List<string> allNames = nameToPurchasedAttribute.Keys.ToList();
                allNames.Sort();
                foreach (var name in allNames)
                    if (!visibleSet.Contains(name))
                        yield return nameToPurchasedAttribute[name];
            }
        }

        public PurchasedProperty getPurchasedProperty(string name)
        {
            return nameToPurchasedAttribute[name];
        }
    }

    public class CheckingContext
    {
        private Dictionary<string, GurpsProperty> nameToThing;
        private GurpsProperty enclosingProperty;
        public CheckingContext(Dictionary<string, GurpsProperty> nameToThing, GurpsProperty enclosingProperty)
        {
            this.nameToThing = nameToThing;
            this.enclosingProperty = enclosingProperty;
        }
        public void checkIsInt(IdentifierToken token)
        {
            GurpsProperty property = getProperty(token);
            if (!isInt(property))
                throwNotAnIntError(token);
        }
        private bool isInt(GurpsProperty property)
        {
            return property is IntAdvantage || property is AttributeFunction || property is AbstractSkill;
        }
        public void checkIsBoolean(IdentifierToken token)
        {
            if (token.text == "level")
                throwNotABooleanError(token);
            GurpsProperty property = getProperty(token);
            if (!(property is Advantage || property is AbstractSkill))
                throwNotABooleanError(token);
        }
        private GurpsProperty getProperty(IdentifierToken token)
        {
            try { return nameToThing[token.text]; }
            catch (KeyNotFoundException) { }

            if (token.text == "level")
                if (enclosingProperty is IntAdvantage || enclosingProperty is AbstractSkill)
                    return enclosingProperty;

            throw new Exception("ERROR: name not defined '" + token.text + "' " + token.parseThing.getLocationString());
        }
        public static void throwNotAnIntError(Token token)
        {
            throw new Exception("ERROR: cannot interpret '" + token.ToString() + "' as an integer " + token.parseThing.getLocationString());
        }
        public static void throwNotABooleanError(Token token)
        {
            throw new Exception("ERROR: cannot interpret '" + token.ToString() + "' as a conditional expression " + token.parseThing.getLocationString());
        }
    }
    public class EvaluationContext
    {
        private GurpsCharacter character;
        private PurchasedProperty purchasedProperty;
        private int levelValue;
        public EvaluationContext(GurpsCharacter character, PurchasedProperty purchasedProperty, int levelValue = int.MinValue)
        {
            this.character = character;
            this.purchasedProperty = purchasedProperty;
            this.levelValue = levelValue;
        }
        public int evalInt(string name)
        {
            if (name == "level")
            {
                if (levelValue == int.MinValue)
                    throw null; // should have been checked earlier
                return levelValue;
            }
            return character.getPurchasedProperty(name).getLevel();
        }
        public bool evalBoolean(string name)
        {
            return character.getPurchasedProperty(name).PurchasedLevels > 0;
        }
    }

    public abstract class Formula
    {
        public virtual IEnumerable<string> usedNames() { yield break; }
        public virtual void checkIsInt(CheckingContext context) { throw new NotImplementedException(); }
        public virtual void checkIsBoolean(CheckingContext context) { throw new NotImplementedException(); }
        public virtual int evalInt(EvaluationContext context) { throw new NotImplementedException(); }
        public virtual bool evalBoolean(EvaluationContext context) { throw new NotImplementedException(); }

        public class NullFormula : Formula
        {
            public static readonly NullFormula Instance = new NullFormula();
            private NullFormula() { }
        }
        public abstract class Leaf : Formula
        {
        }
        public class Identifier : Leaf
        {
            public IdentifierToken token;
            public Identifier(IdentifierToken token)
            {
                this.token = token;
            }
            public override IEnumerable<string> usedNames() { yield return token.text; }
            public override string ToString() { return token.ToString(); }
            public override void checkIsInt(CheckingContext context) { context.checkIsInt(token); }
            public override int evalInt(EvaluationContext context) { return context.evalInt(token.text); }
            public override void checkIsBoolean(CheckingContext context) { context.checkIsBoolean(token); }
            public override bool evalBoolean(EvaluationContext context) { return context.evalBoolean(token.text); }
        }
        public class IntLiteral : Leaf
        {
            public IntToken value;
            public IntLiteral(IntToken value)
            {
                this.value = value;
            }
            public override string ToString() { return value.ToString(); }
            public override void checkIsInt(CheckingContext context) { }
            public override int evalInt(EvaluationContext context) { return value.value; }
            public override void checkIsBoolean(CheckingContext context) { CheckingContext.throwNotABooleanError(value); }
        }
        public class PercentLiteral : Leaf
        {
            public PercentToken value;
            public PercentLiteral(PercentToken value)
            {
                this.value = value;
            }
            public override string ToString() { return value.ToString(); }
            public override void checkIsInt(CheckingContext context) { CheckingContext.throwNotAnIntError(value); }
            public override void checkIsBoolean(CheckingContext context) { CheckingContext.throwNotABooleanError(value); }
        }
        public class UnaryPrefix : Formula
        {
            public readonly SymbolToken operator_;
            public Formula operand = NullFormula.Instance;
            public UnaryPrefix(SymbolToken operator_)
            {
                this.operator_ = operator_;
            }
            public override IEnumerable<string> usedNames() { foreach (string reult in operand.usedNames()) yield return reult; }
            public override string ToString()
            {
                if (operand == NullFormula.Instance)
                    return operator_.text;
                return "(" + operator_.text + operand.ToString() + ")";
            }
            public override void checkIsInt(CheckingContext context) { operand.checkIsInt(context); }
            public override int evalInt(EvaluationContext context)
            {
                int operandValue = operand.evalInt(context);
                switch (operator_.text)
                {
                    case "-":
                        return -operandValue;
                }
                throw null;
            }
            public override void checkIsBoolean(CheckingContext context)
            {
                throw new Exception("ERROR: cannot evaluate '" + operator_.text + operand.ToString() + "' as a conditional expression " + operator_.parseThing.getLocationString());
            }
        }
        public class Binary : Formula
        {
            public Formula left = NullFormula.Instance;
            public readonly SymbolToken operator_;
            public Formula right = NullFormula.Instance;
            public Binary(SymbolToken operator_)
            {
                this.operator_ = operator_;
            }
            public override IEnumerable<string> usedNames() { foreach (string reult in left.usedNames().Concat(right.usedNames())) yield return reult; }
            public override string ToString()
            {
                if (left == NullFormula.Instance)
                    return operator_.text;
                return "(" + left.ToString() + operator_.ToString() + right.ToString() + ")";
            }
            public override void checkIsInt(CheckingContext context)
            {
                switch (operator_.text)
                {
                    case "*":
                    case "+":
                    case "-":
                        left.checkIsInt(context);
                        right.checkIsInt(context);
                        return;
                    case "/":
                        left.checkIsInt(context);
                        if (!(right is IntLiteral))
                            throw new Exception("ERROR: denominator must be a literal integer, not '" + left.ToString() + "' " + operator_.parseThing.getLocationString());
                        if (((IntLiteral)right).value.value == 0)
                            throw new Exception("ERROR: divide by 0 " + operator_.parseThing.getLocationString());
                        return;
                }
                throw new Exception("ERROR: operator '" + operator_.text + "' does not produce an integer " + operator_.parseThing.getLocationString());
            }
            public override int evalInt(EvaluationContext context)
            {
                int leftValue = left.evalInt(context);
                int rightValue = right.evalInt(context);
                switch (operator_.text)
                {
                    case "*": return leftValue * rightValue;
                    case "/": return leftValue / rightValue;
                    case "+": return leftValue + rightValue;
                    case "-": return leftValue - rightValue;
                }
                throw null;
            }
            public override void checkIsBoolean(CheckingContext context)
            {
                switch (operator_.text)
                {
                    case "<=":
                    case "<":
                    case ">=":
                    case ">":
                        left.checkIsInt(context);
                        right.checkIsInt(context);
                        return;
                    case "AND":
                    case "OR":
                        left.checkIsBoolean(context);
                        right.checkIsBoolean(context);
                        return;
                }
                throw new Exception("ERROR: operator '" + operator_.text + "' does not produce a conditional expresion" + operator_.parseThing.getLocationString());
            }
            public override bool evalBoolean(EvaluationContext context)
            {
                switch (operator_.text)
                {
                    case "<=": return left.evalInt(context) <= right.evalInt(context);
                    case "<": return left.evalInt(context) < right.evalInt(context);
                    case ">=": return left.evalInt(context) >= right.evalInt(context);
                    case ">": return left.evalInt(context) > right.evalInt(context);
                    case "AND": return left.evalBoolean(context) && right.evalBoolean(context);
                    case "OR": return left.evalBoolean(context) || right.evalBoolean(context);
                }
                throw null;
            }
        }
        public class Conditional : UnaryPrefix
        {
            public readonly Formula condition = NullFormula.Instance;
            public readonly Formula thenPart = NullFormula.Instance;
            public Conditional(SymbolToken ifToken, Formula condition, Formula thenPart)
                : base(ifToken)
            {
                this.condition = condition;
                this.thenPart = thenPart;
            }
            public override IEnumerable<string> usedNames()
            {
                foreach (string reult in condition.usedNames().Concat(thenPart.usedNames()).Concat(operand.usedNames()))
                    yield return reult;
            }
            public override string ToString()
            {
                if (operand == NullFormula.Instance)
                    return "IF...ELSE";
                return "(IF" + condition.ToString() + " THEN " + thenPart.ToString() + " ELSE " + operand.ToString() + ")";
            }
            public override void checkIsInt(CheckingContext context)
            {
                condition.checkIsBoolean(context);
                thenPart.checkIsInt(context);
                operand.checkIsInt(context);
            }
            public override int evalInt(EvaluationContext context)
            {
                if (condition.evalBoolean(context))
                    return thenPart.evalInt(context);
                else
                    return operand.evalInt(context);
            }
            public override void checkIsBoolean(CheckingContext context)
            {
                condition.checkIsBoolean(context);
                thenPart.checkIsBoolean(context);
                operand.checkIsBoolean(context);
            }
            public override bool evalBoolean(EvaluationContext context)
            {
                if (condition.evalBoolean(context))
                    return thenPart.evalBoolean(context);
                else
                    return operand.evalBoolean(context);
            }
        }
    }
}
