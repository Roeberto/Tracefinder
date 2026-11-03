using System.Collections.Generic;
using System.Linq;
using Tracefinder.Items;

namespace Tracefinder.Combat
{
    // Wybiera jedną akcję na raz, dzięki czemu interfejs może pokazywać kolejne ruchy z przerwą
    public static class EnemyAi
    {
        private const int UnlimitedFeet = int.MaxValue / 2;

        // Wykonuje jedną akcję obecnej jednostki. Zwraca false, gdy nie ma już nic sensownego do zrobienia w tej turze.
        public static bool TakeAction(Encounter encounter)
        {
            Combatant unit = encounter.Current;
            if (unit == null || unit.IsOut || !unit.Actions.CanUseMainAction(1))
                return false;

            Combatant foe = NearestFoe(encounter, unit);
            if (foe == null)
                return false;

            StrikeOption strike = BestStrike(encounter, unit, foe);
            if (strike != null)
            {
                // Trzeci atak przy karze MAP rzadko trafia, więc łucznik woli wtedy odsunąć się od celu
                bool pointless = unit.Actions.AttacksThisTurn >= 2 && strike.Weapon.Kind == WeaponKind.Ranged;
                if (pointless)
                    return StepAway(encounter, unit, foe);
                return encounter.Strike(foe, strike).Success;
            }

            return MoveToward(encounter, unit, foe);
        }

        private static bool StepAway(Encounter encounter, Combatant unit, Combatant foe)
        {
            int current = BattleGrid.DistanceFeet(unit.Position, foe.Position);
            GridPos? best = null;
            foreach (GridPos target in encounter.StepTargets())
            {
                int distance = BattleGrid.DistanceFeet(target, foe.Position);
                if (distance > current)
                {
                    current = distance;
                    best = target;
                }
            }
            return best.HasValue && encounter.Step(best.Value).Success;
        }

        public static void PlayTurn(Encounter encounter)
        {
            Combatant unit = encounter.Current;
            while (encounter.Current == unit && TakeAction(encounter))
            {
            }

            if (encounter.Current == unit)
                encounter.EndTurn();
        }

        private static Combatant NearestFoe(Encounter encounter, Combatant unit)
        {
            return encounter.Combatants
                .Where(c => c.Side != unit.Side && !c.IsOut)
                .OrderBy(c => BattleGrid.DistanceFeet(c.Position, unit.Position))
                .FirstOrDefault();
        }

        // Wręcz ma pierwszeństwo, a łuk służy do atakowania z dystansu. Rzut bronią wręcz pomijamy,
        // bo broń po rzucie nie znika i potwór rzucałby nią bez końca zamiast podejść.
        private static StrikeOption BestStrike(Encounter encounter, Combatant unit, Combatant foe)
        {
            StrikeOption best = null;
            int bestBonus = int.MinValue;
            foreach (StrikeOption strike in unit.Strikes)
            {
                (string refusal, bool thrown, _) = encounter.StrikeReach(unit, foe, strike);
                if (refusal != null || thrown)
                    continue;
                if (strike.Carried != null && strike.Weapon.Kind == WeaponKind.Ranged && !strike.Carried.CanFire)
                    continue;

                int priority = strike.Weapon.Kind == WeaponKind.Melee ? 100 : 0;
                int bonus = unit.AttackBonus(strike, false) + priority;
                if (bonus > bestBonus)
                {
                    bestBonus = bonus;
                    best = strike;
                }
            }
            return best;
        }

        // Liczy drogę od celu, a nie linię prostą, żeby nie utknąć po złej stronie ściany
        private static bool MoveToward(Encounter encounter, Combatant unit, Combatant foe)
        {
            Dictionary<GridPos, int> toFoe = Movement.Reachable(encounter.Grid, foe.Position, UnlimitedFeet, _ => false, _ => false);
            int current = toFoe.GetValueOrDefault(unit.Position, int.MaxValue);

            GridPos? best = null;
            int bestDistance = current;
            foreach (KeyValuePair<GridPos, int> target in encounter.StrideTargets())
            {
                if (toFoe.TryGetValue(target.Key, out int distance) && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = target.Key;
                }
            }

            return best.HasValue && encounter.Stride(best.Value).Success;
        }
    }
}
