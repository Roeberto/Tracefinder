using Tracefinder.Character;
using Tracefinder.Gameplay;
using Tracefinder.Items;

namespace Tracefinder.UI
{
    public static class UiText
    {
        public static string Signed(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }

        public static string RankLetter(ProficiencyRank rank)
        {
            return rank.ToString().Substring(0, 1);
        }

        public static string AbilityShort(AbilityScore ability)
        {
            return ability.ToString().Substring(0, 3).ToUpperInvariant();
        }

        public static string AbilityName(AbilityScore ability)
        {
            switch (ability)
            {
                case AbilityScore.Strength:
                    return "SIŁA";
                case AbilityScore.Dexterity:
                    return "ZRĘCZNOŚĆ";
                case AbilityScore.Constitution:
                    return "KONDYCJA";
                case AbilityScore.Intelligence:
                    return "INTELIGENCJA";
                case AbilityScore.Wisdom:
                    return "MĄDROŚĆ";
                default:
                    return "CHARYZMA";
            }
        }

        public static string SkillName(SkillName skill)
        {
            switch (skill)
            {
                case Character.SkillName.Acrobatics:
                    return "Akrobatyka";
                case Character.SkillName.Arcana:
                    return "Arkana";
                case Character.SkillName.Athletics:
                    return "Atletyka";
                case Character.SkillName.Crafting:
                    return "Rzemiosło";
                case Character.SkillName.Deception:
                    return "Oszustwo";
                case Character.SkillName.Diplomacy:
                    return "Dyplomacja";
                case Character.SkillName.Intimidation:
                    return "Zastraszanie";
                case Character.SkillName.Medicine:
                    return "Medycyna";
                case Character.SkillName.Nature:
                    return "Natura";
                case Character.SkillName.Occultism:
                    return "Okultyzm";
                case Character.SkillName.Performance:
                    return "Występy";
                case Character.SkillName.Religion:
                    return "Religia";
                case Character.SkillName.Society:
                    return "Społeczeństwo";
                case Character.SkillName.Stealth:
                    return "Skradanie";
                case Character.SkillName.Survival:
                    return "Przetrwanie";
                default:
                    return "Złodziejstwo";
            }
        }

        public static string DamageText(Equipment.Weapon weapon, int bonus, int strikingDice = 0)
        {
            string dice = $"{weapon.DiceNumber + strikingDice}{weapon.DamageDice.ToString().ToLowerInvariant()}";
            return bonus == 0 ? dice : dice + Signed(bonus);
        }

        public static string TraitName(WeaponTrait trait)
        {
            switch (trait)
            {
                case DeadlyTrait deadly:
                    return $"Deadly {Die(deadly.Die)}";
                case FatalTrait fatal:
                    return $"Fatal {Die(fatal.Die)}";
                case VersatileTrait versatile:
                    return $"Versatile {versatile.Damage.ToString().Substring(0, 1)}";
                case VolleyTrait volley:
                    return $"Volley {volley.Range} ft";
                case ThrownTrait thrown:
                    return thrown.Range.HasValue ? $"Thrown {thrown.Range} ft" : "Thrown";
                default:
                    return trait.GetType().Name.Replace("Trait", "");
            }
        }

        private static string Die(Dice.DiceSize die)
        {
            return die.ToString().ToLowerInvariant();
        }

        public static string TraitList(Equipment.Weapon weapon)
        {
            return string.Join(", ", weapon.TraitList.ConvertAll(TraitName));
        }

        public static string Pips(int filled, int total)
        {
            return new string('●', filled) + new string('○', total - filled);
        }
    }
}
