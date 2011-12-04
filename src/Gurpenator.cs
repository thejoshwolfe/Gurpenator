
namespace Gurpenator
{
    public class GurpsProperty
    {
        public string name { get { return parsedThing.name; } }
        public ParsedThing parsedThing;
        public GurpsProperty(ParsedThing parsedThing)
        {
            this.parsedThing = parsedThing;
        }
    }
    public enum SkillDifficulty
    {
        Easy, Average, Hard, VeryHard
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
            private IdentifierToken text;
            public Identifier(IdentifierToken text)
            {
                this.text = text;
            }
            public override string ToString()
            {
                return text.ToString();
            }
        }
        public class IntLiteral : Leaf
        {
            private IntToken value;
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
            private PercentToken value;
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
