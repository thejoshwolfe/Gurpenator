
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
        public class Identifier : Formula
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
        public class IntLiteral : Formula
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
        public class PercentLiteral : Formula
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
        public class Unary : Formula
        {
            private string operator_;
            private Formula operand;
            public Unary(string operator_, Formula operand)
            {
                this.operator_ = operator_;
                if (operator_ != "-")
                    throw null;
                this.operand = operand;
            }
            public override string ToString()
            {
                return "(" + operator_ + operand.ToString() + ")";
            }
        }
        public class Binary : Formula
        {
            private Formula left;
            private SymbolToken operator_;
            private Formula right;
            public Binary(Formula left, SymbolToken operator_, Formula right)
            {
                this.left = left;
                this.operator_ = operator_;
                this.right = right;
            }
            public override string ToString()
            {
                return "(" + left.ToString() + operator_.ToString() + right.ToString() + ")";
            }
        }
    }
}
