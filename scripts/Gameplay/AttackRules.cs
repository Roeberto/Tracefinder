using Tracefinder.Items;

namespace Tracefinder.Gameplay
{
    public static class AttackRules
    {
        public const int MaxRangeIncrements = 6;
        public const int DefaultMeleeReachFeet = 5;

        public static int MultipleAttackPenalty(int attacksAlreadyMade, bool agile)
        {
            if (attacksAlreadyMade <= 0)
                return 0;
            if (attacksAlreadyMade == 1)
                return agile ? -4 : -5;
            return agile ? -8 : -10;
        }

        public static int MultipleAttackPenalty(int attacksAlreadyMade, Equipment.Weapon weapon)
        {
            return MultipleAttackPenalty(attacksAlreadyMade, weapon.HasTrait<AgileTrait>());
        }

        // Dla broni rzucanej liczy się zasięg z cechy Thrown, jeśli jest podany
        public static int? RangeIncrementFor(Equipment.Weapon weapon, bool thrown)
        {
            if (thrown)
            {
                foreach (WeaponTrait trait in weapon.TraitList)
                {
                    if (trait is ThrownTrait thrownTrait && thrownTrait.Range.HasValue)
                        return thrownTrait.Range;
                }
            }
            return weapon.RangeIncrement;
        }

        public static bool IsInRange(int? rangeIncrement, int distanceFeet)
        {
            return rangeIncrement.HasValue && distanceFeet <= rangeIncrement.Value * MaxRangeIncrements;
        }

        // -2 za każdy pełny przedział zasięgu ponad pierwszy
        public static int RangePenalty(int? rangeIncrement, int distanceFeet)
        {
            if (!rangeIncrement.HasValue || distanceFeet <= 0)
                return 0;
            return -2 * ((distanceFeet - 1) / rangeIncrement.Value);
        }

        // Volley: -2 do celów bliżej niż podany dystans
        public static int VolleyPenalty(Equipment.Weapon weapon, int distanceFeet)
        {
            foreach (WeaponTrait trait in weapon.TraitList)
            {
                if (trait is VolleyTrait volley && distanceFeet <= volley.Range)
                    return -2;
            }
            return 0;
        }

        public static int MeleeReachFeet(Equipment.Weapon weapon)
        {
            return weapon.HasTrait<ReachTrait>() ? 10 : DefaultMeleeReachFeet;
        }
    }
}
