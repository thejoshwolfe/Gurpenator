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

        public Func<int, string> formattingFunction = delegate(int value) { return value.ToString(); };
    }
    public enum SkillDifficulty
    {
        Unspecified, Easy, Average, Hard, VeryHard
    }
    public abstract class AbstractSkill : GurpsProperty
    {
        protected AbstractSkill(ParsedThing parsedThing)
            : base(parsedThing) { }
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
    }
    public abstract class Advantage : GurpsProperty
    {
        public Formula costFormula;
        public Advantage(ParsedThing parsedThing, Formula costFormula)
            : base(parsedThing)
        {
            this.costFormula = costFormula;
        }
    }
    public class IntAdvantage : Advantage
    {
        public IntAdvantage(ParsedThing parsedThing, Formula costFormula)
            : base(parsedThing, costFormula) { }
    }
    public class BooleanAdvantage : Advantage
    {
        public BooleanAdvantage(ParsedThing parsedThing, Formula costFormula)
            : base(parsedThing, costFormula) { }
    }
    public class AttributeFunction : GurpsProperty
    {
        public Formula formula;
        public AttributeFunction(ParsedThing parsedThing, Formula formula)
            : base(parsedThing)
        {
            this.formula = formula;
        }
    }

    public class PurchasedProperty
    {
        public GurpsProperty property;
        public PurchasedProperty(GurpsProperty property)
        {
            this.property = property;
        }
        public int level { get { return 0; } }
        public string formattedValue { get { return property.formattingFunction(level); } }
        public bool hasCost { get { return !(property is AttributeFunction); } }
        public int cost { get { return 0; } }

        public bool hasPurchasedLevels { get { return property is IntAdvantage; } }
        private int purchasedLevels = 0;
        public int PurchasedLevels
        {
            get { return purchasedLevels; }
            set
            {
                purchasedLevels = value;
                changed();
            }
        }

        public event Action changed;
    }

    public class GurpsCharacter
    {
        private static readonly string[] attributeNames = {
            "TL",
            "ST", "DX", "IQ", "HT",
            "HP", "Will", "Per", "FP",
            "Basic Lift",
            "Basic Speed x4",
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
            foreach (string name in coreAttributeNames)
                nameToPurchasedAttribute[name] = new PurchasedProperty(nameToThing[name]);
        }
        public IEnumerable<PurchasedProperty> visibleAttributes
        {
            get
            {
                foreach (string attributeName in attributeNames)
                    yield return nameToPurchasedAttribute[attributeName];
            }
        }
    }

    public class EvaluationContext
    {
        private Dictionary<string, GurpsProperty> nameToThing;
        private Dictionary<string, GurpsProperty> specialThings = new Dictionary<string, GurpsProperty>();
        private GurpsProperty enclosingProperty;
        public EvaluationContext(Dictionary<string, GurpsProperty> nameToThing, GurpsProperty enclosingProperty)
        {
            this.nameToThing = nameToThing;
            this.enclosingProperty = enclosingProperty;
            if (isInt(enclosingProperty))
                specialThings["level"] = enclosingProperty;
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
            GurpsProperty property = getProperty(token);
            if (!(property is Advantage || property is AbstractSkill))
                throwNotABooleanError(token);
        }
        private GurpsProperty getProperty(IdentifierToken token)
        {
            try { return nameToThing[token.text]; }
            catch (KeyNotFoundException) { }

            try { return specialThings[token.text]; }
            catch (KeyNotFoundException) { }

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
    public abstract class Formula
    {
        public virtual bool usesLevel()
        {
            return false;
        }
        public virtual void checkIsInt(EvaluationContext context)
        {
            throw new NotImplementedException();
        }
        public virtual void checkIsBoolean(EvaluationContext context)
        {
            throw new NotImplementedException();
        }

        public class NullFormula : Formula
        {
            public static readonly NullFormula Instance = new NullFormula();
            private NullFormula() { }
            public override string ToString() { return "?"; }
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
            public override bool usesLevel()
            {
                return token.text == "level";
            }
            public override string ToString()
            {
                return token.ToString();
            }
            public override void checkIsInt(EvaluationContext context)
            {
                context.checkIsInt(token);
            }
            public override void checkIsBoolean(EvaluationContext context)
            {
                context.checkIsBoolean(token);
            }
        }
        public class IntLiteral : Leaf
        {
            public IntToken value;
            public IntLiteral(IntToken value)
            {
                this.value = value;
            }
            public override string ToString()
            {
                return value.ToString();
            }
            public override void checkIsInt(EvaluationContext context) { }
            public override void checkIsBoolean(EvaluationContext context)
            {
                EvaluationContext.throwNotABooleanError(value);
            }
        }
        public class PercentLiteral : Leaf
        {
            public PercentToken value;
            public PercentLiteral(PercentToken value)
            {
                this.value = value;
            }
            public override string ToString()
            {
                return value.ToString();
            }
            public override void checkIsInt(EvaluationContext context)
            {
                EvaluationContext.throwNotAnIntError(value);
            }
            public override void checkIsBoolean(EvaluationContext context)
            {
                EvaluationContext.throwNotABooleanError(value);
            }
        }
        public class UnaryPrefix : Formula
        {
            public readonly SymbolToken operator_;
            public Formula operand = NullFormula.Instance;
            public UnaryPrefix(SymbolToken operator_)
            {
                this.operator_ = operator_;
            }
            public override bool usesLevel()
            {
                return operand.usesLevel();
            }
            public override string ToString()
            {
                if (operand == NullFormula.Instance)
                    return operator_.text;
                return "(" + operator_.text + operand.ToString() + ")";
            }
            public override void checkIsInt(EvaluationContext context)
            {
                operand.checkIsInt(context);
            }
            public override void checkIsBoolean(EvaluationContext context)
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
            public override bool usesLevel()
            {
                return left.usesLevel() || right.usesLevel();
            }
            public override string ToString()
            {
                if (left == NullFormula.Instance)
                    return operator_.text;
                return "(" + left.ToString() + operator_.ToString() + right.ToString() + ")";
            }
            public override void checkIsInt(EvaluationContext context)
            {
                switch (operator_.text)
                {
                    case "*":
                    case "/":
                    case "+":
                    case "-":
                        left.checkIsInt(context);
                        right.checkIsInt(context);
                        return;
                }
                throw new Exception("ERROR: operator '" + operator_.text + "' does not produce an integer " + operator_.parseThing.getLocationString());
            }
            public override void checkIsBoolean(EvaluationContext context)
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
            public override bool usesLevel()
            {
                return condition.usesLevel() || thenPart.usesLevel() || base.usesLevel();
            }
            public override string ToString()
            {
                if (operand == NullFormula.Instance)
                    return "IF...ELSE";
                return "(IF" + condition.ToString() + " THEN " + thenPart.ToString() + " ELSE " + operand.ToString() + ")";
            }
            public override void checkIsInt(EvaluationContext context)
            {
                condition.checkIsBoolean(context);
                thenPart.checkIsInt(context);
                operand.checkIsInt(context);
            }
            public override void checkIsBoolean(EvaluationContext context)
            {
                condition.checkIsBoolean(context);
                thenPart.checkIsBoolean(context);
                operand.checkIsBoolean(context);
            }
        }
    }
}
