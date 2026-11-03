using Tracefinder.Character;

namespace Tracefinder.Gameplay
{
    public record HealingResult(bool Attempted, int Healed, int Damage, DegreeOfSuccess Degree);

    public static class Healing
    {
        // Treat Wounds z Player Core, DC i premia zależą od rangi Medycyny
        public static HealingResult TreatWounds(CharacterCore core, ClassStats classStats, bool removeWounded = true)
        {
            ProficiencyRank rank = core.GetSkillRank(SkillName.Medicine, classStats);
            if (rank == ProficiencyRank.Untrained)
                return new HealingResult(false, 0, 0, DegreeOfSuccess.Failure);

            (int dc, int flat) = rank switch
            {
                ProficiencyRank.Trained => (15, 0),
                ProficiencyRank.Expert => (20, 10),
                ProficiencyRank.Master => (30, 30),
                _ => (40, 50)
            };

            Check.Result check = Check.Roll(core.ComputeSkillBonus(SkillName.Medicine, classStats), dc);
            int healed = 0;
            int damage = 0;
            switch (check.Degree)
            {
                case DegreeOfSuccess.CriticalSuccess:
                    healed = Dice.Roll(Dice.DiceSize.D8, 4) + flat;
                    break;
                case DegreeOfSuccess.Success:
                    healed = Dice.Roll(Dice.DiceSize.D8, 2) + flat;
                    break;
                case DegreeOfSuccess.CriticalFailure:
                    damage = Dice.Roll(Dice.DiceSize.D8, 1);
                    break;
            }

            if (healed > 0)
            {
                core.Health.HealDamage(healed);
                if (removeWounded)
                    core.Health.ClearWounded();
            }
            if (damage > 0)
                core.Health.TakeDamage(damage);
            return new HealingResult(true, healed, damage, check.Degree);
        }

        public static int HalvedOrScaled(int amount, DegreeOfSuccess saveDegree)
        {
            switch (saveDegree)
            {
                case DegreeOfSuccess.CriticalSuccess:
                    return 0;
                case DegreeOfSuccess.Success:
                    return amount / 2;
                case DegreeOfSuccess.CriticalFailure:
                    return amount * 2;
                default:
                    return amount;
            }
        }
    }
}
