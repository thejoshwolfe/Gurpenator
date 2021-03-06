﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gurpenator
{
    public class GurpsDatabase
    {
        public Dictionary<string, GurpsProperty> nameToThing = new Dictionary<string, GurpsProperty>();

        public List<GurpsProperty> search(string query, TraitTypeFilter filter, GurpsCharacter character)
        {
            return new List<GurpsProperty>(order(internalSearch(query, filter, character)));
        }
        private IEnumerable<GurpsProperty> order(IEnumerable<GurpsProperty> results)
        {
            return results.OrderBy((property) => property.DisplayName);
        }
        private IEnumerable<GurpsProperty> internalSearch(string query, TraitTypeFilter filter, GurpsCharacter character)
        {
            var dontInclude = new HashSet<string>(character.layout.getNames());
            var words = query.ToLower().Split(' ');
            foreach (GurpsProperty property in nameToThing.Values)
            {
                if (!filterPasses(filter, property))
                    continue;
                if (dontInclude.Contains(property.name))
                    continue;
                var nameToLower = property.DisplayName.ToLower();
                if (words.All((word) => nameToLower.Contains(word)))
                    yield return property;
            }
        }
        private static bool filterPasses(TraitTypeFilter filter, GurpsProperty property)
        {
            switch (filter)
            {
                case TraitTypeFilter.Locked: return true;
                case TraitTypeFilter.Skills:
                    return property is AbstractSkill && !((AbstractSkill)property).category;
                case TraitTypeFilter.Advantages:
                    return property is Advantage && isAdvantageNonNegative((Advantage)property);
                case TraitTypeFilter.Disadvantages:
                    return property is Advantage && !isAdvantageNonNegative((Advantage)property);
            }
            throw null;
        }
        private static bool isAdvantageNonNegative(Advantage advantage)
        {
            return advantage.costFormula.evalInt(new EvaluationContext(null, null, 1)) >= 0;
        }
    }
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
        public List<Effect> effects = new List<Effect>();
        public List<Effect> effectedBy = new List<Effect>();
        public virtual IEnumerable<string> usedNames() { yield break; }

        public virtual void resolveInheritance(Dictionary<string, GurpsProperty> nameToThing) { }
        public virtual void checkFormula(Dictionary<string, GurpsProperty> nameToThing) { }
    }
    public enum SkillDifficulty
    {
        Unspecified, Easy, Average, Hard, VeryHard
    }
    public enum TraitTypeFilter
    {
        Locked, Advantages, Disadvantages, Skills
    }
    public abstract class AbstractSkill : GurpsProperty
    {
        public bool category = false;

        protected AbstractSkill(ParsedThing parsedThing)
            : base(parsedThing) { }

        public abstract SkillDifficulty getDifficulty();
        public static int difficultyOffset(SkillDifficulty difficulty)
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

        public abstract Formula getBaseFormula();
        public static SkillDifficulty difficultyFromChar(char c)
        {
            switch (c)
            {
                case 'E': return SkillDifficulty.Easy;
                case 'A': return SkillDifficulty.Average;
                case 'H': return SkillDifficulty.Hard;
                case 'V': return SkillDifficulty.VeryHard;
            }
            throw null;
        }
        public static string difficultyToString(SkillDifficulty difficulty)
        {
            switch (difficulty)
            {
                case SkillDifficulty.Easy: return "E";
                case SkillDifficulty.Average: return "A";
                case SkillDifficulty.Hard: return "H";
                case SkillDifficulty.VeryHard: return "V";
            }
            throw null;
        }
    }
    public class Skill : AbstractSkill
    {
        private SkillDifficulty difficulty;
        public Formula formula;
        public Skill(ParsedThing parsedThing, SkillDifficulty difficulty, Formula formula)
            : base(parsedThing)
        {
            this.difficulty = difficulty;
            this.formula = formula;
        }
        public override IEnumerable<string> usedNames() { return formula.usedNames(); }
        public override SkillDifficulty getDifficulty() { return difficulty; }
        public override Formula getBaseFormula() { return formula; }
        public override void checkFormula(Dictionary<string, GurpsProperty> nameToThing)
        {
            formula.checkIsInt(new CheckingContext(nameToThing, null));
        }
    }
    public class InheritedSkill : AbstractSkill
    {
        public SkillDifficulty difficultyOverride;
        public IdentifierToken parentSkillToken;
        public AbstractSkill parent = null;
        public InheritedSkill(ParsedThing parsedThing, SkillDifficulty difficultyOverride, IdentifierToken parentSkillToken)
            : base(parsedThing)
        {
            this.difficultyOverride = difficultyOverride;
            this.parentSkillToken = parentSkillToken;
        }
        public override SkillDifficulty getDifficulty()
        {
            SkillDifficulty difficulty = difficultyOverride;
            if (difficulty == SkillDifficulty.Unspecified)
            {
                difficulty = parent.getDifficulty();
                if (!parent.category)
                {
                    // demote difficulty for optional specialties
                    difficulty = (SkillDifficulty)((int)difficulty - 1);
                }
            }
            return difficulty;
        }
        public override Formula getBaseFormula() { return parent.getBaseFormula(); }
        public override IEnumerable<string> usedNames() { return parent.usedNames(); }
        public override void resolveInheritance(Dictionary<string, GurpsProperty> nameToThing)
        {
            IdentifierToken parentNameToken = parentSkillToken;
            GurpsProperty parent;
            try { parent = nameToThing[parentNameToken.text]; }
            catch (KeyNotFoundException) { throw parentNameToken.parseThing.createError("Parent skill not defined '" + parentNameToken.text + "'"); }
            if ((this.parent = parent as AbstractSkill) == null)
                throw parentNameToken.parseThing.createError("Parent is not a skill '" + parentNameToken.text + "'");
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
        public override IEnumerable<string> usedNames() { return costFormula.usedNames(); }
        public override void checkFormula(Dictionary<string, GurpsProperty> nameToThing)
        {
            costFormula.checkIsInt(new CheckingContext(null, this));
        }
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
        public override void checkFormula(Dictionary<string, GurpsProperty> nameToThing)
        {
            formula.checkIsInt(new CheckingContext(nameToThing, null));
        }
    }

    public abstract class Effect
    {
        public GurpsProperty owner;
        public string traitName;
        public Formula formula;
        public ParsedThing parsedThing;
        protected Effect(GurpsProperty owner, string traitName, Formula formula, ParsedThing parsedThing)
        {
            this.owner = owner;
            this.traitName = traitName;
            this.formula = formula;
            this.parsedThing = parsedThing;
        }

        public abstract void checkFormula(Dictionary<string, GurpsProperty> nameToThing, GurpsProperty affectedProperty);
    }
    public class CostModifier : Effect
    {
        public CostModifier(GurpsProperty owner, string traitName, Formula formula, ParsedThing parsedThing)
            : base(owner, traitName, formula, parsedThing) { }
        public override void checkFormula(Dictionary<string, GurpsProperty> nameToThing, GurpsProperty affectedProperty)
        {
            if (affectedProperty is AttributeFunction) // TODO: use hasCost or whatever
                throw parsedThing.createError("Cannot modify the cost of a trait with no cost");
            formula.checkIsPercent(new CheckingContext(nameToThing, owner));
        }
    }
    public class TraitModifier : Effect
    {
        public TraitModifier(GurpsProperty owner, string traitName, Formula formula, ParsedThing parsedThing)
            : base(owner, traitName, formula, parsedThing) { }
        public override void checkFormula(Dictionary<string, GurpsProperty> nameToThing, GurpsProperty affectedProperty)
        {
            if (affectedProperty is BooleanAdvantage) // TODO: use hasLevels or whatever
                throw parsedThing.createError("Cannot modify the level of a trait with no level");
            formula.checkIsInt(new CheckingContext(nameToThing, owner));
        }
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
            int level = getUnmodifiedLevel();
            if (level == int.MinValue)
                return level;
            foreach (TraitModifier effect in from x in property.effectedBy where x is TraitModifier select x)
            {
                PurchasedProperty otherPurchasedProperty = character.getPurchasedProperty(effect.owner.name);
                if (!otherPurchasedProperty.nonDefault)
                    continue;
                int effectLevel = effect.formula.evalInt(new EvaluationContext(character, otherPurchasedProperty, otherPurchasedProperty.getLevel()));
                level += effectLevel;
            }
            return level;
        }
        private int getUnmodifiedLevel()
        {
            if (property is Advantage)
                return purchasedLevels;
            if (property is AttributeFunction)
                return ((AttributeFunction)property).formula.evalInt(new EvaluationContext(character, this));
            if (property is AbstractSkill)
            {
                AbstractSkill skill = (AbstractSkill)property;
                if (purchasedLevels == 0)
                {
                    return int.MinValue; // TODO: defaults
                }
                else
                {
                    int attribute = skill.getBaseFormula().evalInt(new EvaluationContext(character, this));
                    int difficultyOffset = AbstractSkill.difficultyOffset(skill.getDifficulty());
                    return attribute + difficultyOffset + purchasedLevels;
                }
            }
            throw null;
        }
        public bool nonDefault { get { return purchasedLevels > 0 || property is AttributeFunction || GurpsCharacter.coreAttributeNames.Contains(property.name); } }
        public string getFormattedValue() { return property.formattingFunction(getLevel()); }
        public bool hasCost { get { return !(property is AttributeFunction); } }
        public int getCost()
        {
            int cost = getUnmodifiedCost();
            decimal discountPercent = 0;
            decimal extraCostPercent = 0;
            foreach (CostModifier effect in from x in property.effectedBy where x is CostModifier select x)
            {
                PurchasedProperty otherPurchasedProperty = character.getPurchasedProperty(effect.owner.name);
                if (!otherPurchasedProperty.nonDefault)
                    continue;
                EvaluationContext context = new EvaluationContext(character, otherPurchasedProperty, otherPurchasedProperty.getLevel());
                if (effect.parsedThing.declarationOperator == "-=")
                    discountPercent += effect.formula.evalPercent(context);
                else if (effect.parsedThing.declarationOperator == "+=")
                    extraCostPercent += effect.formula.evalPercent(context);
                else
                    throw null;
            }
            // TODO enhancement/limitation cost modifiers go here
            // cap discount at -80%
            discountPercent = Math.Min(discountPercent, 0.80m);
            int costEffect = (int)Math.Floor(cost * (extraCostPercent - discountPercent));
            cost += costEffect;
            return cost;
        }
        private int getUnmodifiedCost()
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

        public bool isBooleanPurchasable { get { return property is BooleanAdvantage; } }
        public bool hasPurchasedLevels
        {
            get
            {
                if (property is IntAdvantage)
                    return true;
                if (property is AbstractSkill)
                    return !((AbstractSkill)property).category;
                return false;
            }
        }
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
            return property.name + ":" + purchasedLevels + ":" + getCost() + ":" + getFormattedValue();
        }
    }

    public class GurpsCharacter
    {
        public static readonly HashSet<string> coreAttributeNames = new HashSet<string>
        {
                "TL",
                "ST", "DX", "IQ", "HT",
                "HP", "Will", "Per", "FP",
                "Basic Lift ST", "Basic Lift",
                "Basic Speed x4", "Basic Move",
                "Damage ST",
                "Thrust", "Swing",
                "Dodge",
                "Fright Check",
        };
        public const string HUMAN = "Human";
        public AbstractTraitGroup layout = new TraitContainer(null, Orientation.Vertical,
            new BasicInfoTraitGroup(),
            new TraitContainer(null, Orientation.Horizontal,
                new TraitContainer(null, Orientation.Vertical,
                    new TraitContainer("Attributes", Orientation.Horizontal,
                        new TraitList(null, TraitTypeFilter.Locked, "ST", "DX", "IQ", "HT"),
                        new TraitList(null, TraitTypeFilter.Locked, "HP", "Will", "Per", "FP")
                    ),
                    new TraitList("Asdf", TraitTypeFilter.Locked, "Dodge", "Basic Lift"),
                    new TraitList("Fdsa", TraitTypeFilter.Locked,
                        "Thrust",
                        "Swing",
                        "Basic Speed x4",
                        "Basic Move",
                        "Fright Check"
                    )
                ),
                new TraitContainer(null, Orientation.Vertical,
                    new TraitList("Advantages", TraitTypeFilter.Advantages, "Human"),
                    new TraitList("Disadvantages", TraitTypeFilter.Disadvantages)
                ),
                new TraitContainer(null, Orientation.Vertical,
                    new TraitList("Skills", TraitTypeFilter.Skills)
                )
            )
        );

        private Dictionary<string, PurchasedProperty> nameToPurchasedAttribute = new Dictionary<string, PurchasedProperty>();

        public event Action changed;

        private string name = "";
        public string Name
        {
            get { return name; }
            set
            {
                if (name == value)
                    return;
                name = value;
                raiseChanged();
            }
        }

        public GurpsCharacter(GurpsDatabase database)
        {
            foreach (GurpsProperty property in database.nameToThing.Values)
                nameToPurchasedAttribute[property.name] = new PurchasedProperty(property, this);
            foreach (PurchasedProperty purchasedProperty in nameToPurchasedAttribute.Values)
            {
                purchasedProperty.changed += raiseChanged;
                foreach (string name in purchasedProperty.property.usedNames())
                {
                    if (DataLoader.reservedWords.Contains(name))
                        continue;
                    nameToPurchasedAttribute[name].changed += purchasedProperty.handleChange;
                }
                foreach (Effect effect in purchasedProperty.property.effectedBy)
                {
                    nameToPurchasedAttribute[effect.owner.name].changed += purchasedProperty.handleChange;
                }
            }
        }
        public void raiseChanged()
        {
            if (changed != null) changed();
        }
        public PurchasedProperty getPurchasedProperty(string name)
        {
            try { return nameToPurchasedAttribute[name]; }
            catch (KeyNotFoundException) { throw new GurpenatorException("Character requires trait that is not defined in any loaded database: \"" + name + "\""); }
        }

        public object toJson()
        {
            return new Dictionary<string, object> {
                { "name", Name } ,
                { "layout", layout.toJson() },
                { "purchases", new List<object>(from purchase in getAllPurchases()
                                                select new Dictionary<string, object> {
                                                    {"trait", purchase.property.name},
                                                    {"purchased", purchase.PurchasedLevels},
                                                })
                },
            };
        }

        public IEnumerable<PurchasedProperty> getAllPurchases()
        {
            return from purchase in nameToPurchasedAttribute.Values where purchase.PurchasedLevels != 0 select purchase;
        }
        public static GurpsCharacter fromJson(object jsonObject, GurpsDatabase database)
        {
            GurpsCharacter character = new GurpsCharacter(database);
            var dict = (Dictionary<string, object>)jsonObject;
            character.name = (string)dict["name"];
            character.layout = AbstractTraitGroup.fromJson(dict["layout"]);
            foreach (object purchaseObject in (List<object>)dict["purchases"])
            {
                var purchase = (Dictionary<string, object>)purchaseObject;
                character.getPurchasedProperty((string)purchase["trait"]).PurchasedLevels = (int)purchase["purchased"];
            }
            return character;
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
            if (token.text == "level")
                if (enclosingProperty != null)
                    return enclosingProperty;

            if (nameToThing == null)
                throwNotAnIntError(token);
            try { return nameToThing[token.text]; }
            catch (KeyNotFoundException) { }

            throw token.parseThing.createError("name not defined '" + token.text + "'");
        }
        public static void throwNotAnIntError(Token token)
        {
            throw token.parseThing.createError("cannot interpret '" + token.ToString() + "' as an integer");
        }
        public static void throwNotABooleanError(Token token)
        {
            throw token.parseThing.createError("cannot interpret '" + token.ToString() + "' as a conditional expression");
        }
        public static void throwNotAPercentError(Token token)
        {
            throw token.parseThing.createError("cannot interpret '" + token.ToString() + "' as a percent");
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
        public virtual int evalInt(EvaluationContext context) { throw new NotImplementedException(); }
        public virtual void checkIsBoolean(CheckingContext context) { throw new NotImplementedException(); }
        public virtual bool evalBoolean(EvaluationContext context) { throw new NotImplementedException(); }
        public virtual void checkIsPercent(CheckingContext context) { throw new NotImplementedException(); }
        public virtual decimal evalPercent(EvaluationContext context) { throw new NotImplementedException(); }

        public class NullFormula : Formula
        {
            public static readonly NullFormula Instance = new NullFormula();
            private NullFormula() { }
        }
        public abstract class Leaf : Formula { }
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
            public override void checkIsPercent(CheckingContext context) { CheckingContext.throwNotAPercentError(token); }
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
            public override void checkIsPercent(CheckingContext context) { CheckingContext.throwNotAPercentError(value); }
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
            public override void checkIsPercent(CheckingContext context) { }
            public override decimal evalPercent(EvaluationContext context) { return value.value; }
        }
        public class BooleanLiteral : Leaf
        {
            public BooleanToken value;
            public BooleanLiteral(BooleanToken value)
            {
                this.value = value;
            }
            public override string ToString() { return value.ToString(); }
            public override void checkIsInt(CheckingContext context) { CheckingContext.throwNotAnIntError(value); }
            public override bool evalBoolean(EvaluationContext context) { return value.value; }
            public override void checkIsBoolean(CheckingContext context) { }
            public override void checkIsPercent(CheckingContext context) { CheckingContext.throwNotAPercentError(value); }
        }
        public class UnaryPrefix : Formula
        {
            public readonly SymbolToken operator_;
            public Formula operand = NullFormula.Instance;
            public UnaryPrefix(SymbolToken operator_)
                : this(operator_, NullFormula.Instance) { }
            public UnaryPrefix(SymbolToken operator_, Formula operand)
            {
                this.operator_ = operator_;
                this.operand = operand;
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
                    case "-": return -operandValue;
                }
                throw null;
            }
            public override void checkIsBoolean(CheckingContext context)
            {
                throw operator_.parseThing.createError("cannot evaluate '" + operator_.text + operand.ToString() + "' as a conditional expression");
            }
            public override void checkIsPercent(CheckingContext context) { operand.checkIsPercent(context); }
            public override decimal evalPercent(EvaluationContext context)
            {
                decimal operandValue = operand.evalPercent(context);
                switch (operator_.text)
                {
                    case "-": return -operandValue;
                }
                throw null;
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
            public override IEnumerable<string> usedNames() { return left.usedNames().Concat(right.usedNames()); }
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
                            throw operator_.parseThing.createError("denominator must be a literal integer, not '" + left.ToString() + "'");
                        if (((IntLiteral)right).value.value == 0)
                            throw operator_.parseThing.createError("divide by 0");
                        return;
                }
                throw operator_.parseThing.createError("operator '" + operator_.text + "' does not produce an integer");
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
                throw operator_.parseThing.createError("operator '" + operator_.text + "' does not produce a conditional expresion");
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
            public override void checkIsPercent(CheckingContext context)
            {
                // this is really tricky
                if (operator_.text != "*")
                    throw operator_.parseThing.createError("operator '" + operator_.text + "' does not produce a percent");
                if (left.usedNames().Contains("level"))
                {
                    left.checkIsInt(context);
                    right.checkIsPercent(context);
                }
                else if (right.usedNames().Contains("level"))
                {
                    left.checkIsPercent(context);
                    right.checkIsInt(context);
                }
                else
                {
                    throw operator_.parseThing.createError("multiplication can only produce a percent by multiplying a percent by something with 'level' in it");
                }
            }
            public override decimal evalPercent(EvaluationContext context)
            {
                if (left.ToString().Contains("level"))
                    return left.evalInt(context) * right.evalPercent(context);
                else
                    return left.evalPercent(context) * right.evalInt(context);
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
                return condition.usedNames().Concat(thenPart.usedNames()).Concat(operand.usedNames());
            }
            public override string ToString()
            {
                if (operand == NullFormula.Instance)
                    return "IF...ELSE";
                return "(IF " + condition.ToString() + " THEN " + thenPart.ToString() + " ELSE " + operand.ToString() + ")";
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
            public override void checkIsPercent(CheckingContext context)
            {
                condition.checkIsBoolean(context);
                thenPart.checkIsPercent(context);
                operand.checkIsPercent(context);
            }
            public override decimal evalPercent(EvaluationContext context)
            {
                if (condition.evalBoolean(context))
                    return thenPart.evalPercent(context);
                else
                    return operand.evalPercent(context);
            }
        }
    }
}
