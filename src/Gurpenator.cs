using System;
using System.Collections.Generic;
using System.Linq;

namespace Gurpenator
{
    public class GurpsProperty
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
        public ParsedThing parsedThing;
        public GurpsProperty(ParsedThing parsedThing)
        {
            this.parsedThing = parsedThing;
        }

        public Func<int, string> formattingFunction = delegate(int value) { return value.ToString(); };
    }
    public enum SkillDifficulty
    {
        Unspecified, Easy, Average, Hard, VeryHard
    }
    public class Skill : GurpsProperty
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
    public class InheritedSkill : GurpsProperty
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
    public class Advantage : GurpsProperty
    {
        public Formula costFormula;
        public Advantage(ParsedThing parsedThing, Formula costFormula)
            : base(parsedThing)
        {
            this.costFormula = costFormula;
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
    }

    public class PurchasedProperty
    {
        public GurpsProperty property;
        private int purchasedLevels = 0;
        public PurchasedProperty(GurpsProperty property)
        {
            this.property = property;
        }
        public int level { get { return 0; } }
        public string formattedValue { get { return property.formattingFunction(level); } }
        public int PurchasedLevels
        {
            get { return purchasedLevels; }
            set
            {
                purchasedLevels = value;
                changed();
            }
        }
        public int cost { get { return 0; } }

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

    public abstract class Formula
    {
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
            public override string ToString()
            {
                return token.ToString();
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
        }
        public class UnaryPrefix : Formula
        {
            public readonly SymbolToken operator_;
            public Formula operand = NullFormula.Instance;
            public UnaryPrefix(SymbolToken operator_)
            {
                this.operator_ = operator_;
            }
            public override string ToString()
            {
                return "(" + operator_ + operand.ToString() + ")";
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
            public override string ToString()
            {
                return "(" + left.ToString() + operator_.ToString() + right.ToString() + ")";
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
            public override string ToString()
            {
                return "(IF" + condition.ToString() + " THEN " + thenPart.ToString() + " ELSE " + operand.ToString() + ")";
            }
        }
    }
}
