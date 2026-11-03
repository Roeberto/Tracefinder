using System;
using System.Collections.Generic;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Gameplay;
using Tracefinder.Items;

namespace Tracefinder.Combat
{
    public enum EncounterState
    {
        NotStarted,
        InProgress,
        Finished
    }

    public record ActionResult(bool Success, string Message);

    public class Encounter
    {
        private readonly List<Combatant> _order;
        private int _turnIndex;

        public BattleGrid Grid { get; }
        public IReadOnlyList<Combatant> Combatants => _order;
        public EncounterState State { get; private set; } = EncounterState.NotStarted;
        public int Round { get; private set; }
        public Side? Winner { get; private set; }
        public int ExperienceAwarded { get; private set; }
        public List<string> Log { get; } = new();
        public event Action<string> LogAdded;

        public Combatant Current => State == EncounterState.InProgress ? _order[_turnIndex] : null;

        public Encounter(BattleGrid grid, IEnumerable<Combatant> combatants)
        {
            Grid = grid;
            _order = combatants.ToList();
            if (_order.Count == 0)
                throw new ArgumentException("Starcie wymaga co najmniej jednej jednostki.");
        }

        public void Start()
        {
            foreach (Combatant combatant in _order)
            {
                combatant.Initiative = Dice.Roll(Dice.DiceSize.D20, 1) + combatant.Perception;
                Write($"{combatant.Name}: inicjatywa {combatant.Initiative}");
            }

            // Przy remisie potwory działają przed graczami
            List<Combatant> sorted = _order
                .OrderByDescending(c => c.Initiative)
                .ThenBy(c => c.Side == Side.Enemies ? 0 : 1)
                .ToList();
            _order.Clear();
            _order.AddRange(sorted);

            State = EncounterState.InProgress;
            Round = 1;
            _turnIndex = 0;
            Write($"Runda {Round}");
            BeginTurn();
        }

        public void EndTurn()
        {
            if (State != EncounterState.InProgress)
                return;
            AdvanceTurn();
            BeginTurn();
        }

        private void AdvanceTurn()
        {
            _turnIndex++;
            if (_turnIndex >= _order.Count)
            {
                _turnIndex = 0;
                Round++;
                Write($"Runda {Round}");
            }
        }

        // Jednostki bez przytomności tylko robią test wyjścia z Dying i tura przechodzi dalej
        private void BeginTurn()
        {
            for (int skipped = 0; skipped <= _order.Count; skipped++)
            {
                if (CheckVictory())
                    return;

                Combatant combatant = _order[_turnIndex];
                combatant.Actions.ResetTurn();
                if (!combatant.IsOut)
                {
                    Write($"Tura: {combatant.Name}");
                    return;
                }

                if (!combatant.Health.DiesAtZeroHp && !combatant.Health.IsDead && combatant.Health.Dying > 0)
                {
                    Check.Result recovery = combatant.Health.RecoveryCheck();
                    Write($"{combatant.Name} walczy o życie: {recovery.Natural} przeciw DC {recovery.Dc}, Dying {combatant.Health.Dying}");
                    if (combatant.Health.IsDead)
                        Write($"{combatant.Name} ginie.");
                }
                AdvanceTurn();
            }
        }

        private bool CheckVictory()
        {
            if (State == EncounterState.Finished)
                return true;

            bool partyStanding = _order.Any(c => c.Side == Side.Party && !c.IsOut);
            bool enemiesStanding = _order.Any(c => c.Side == Side.Enemies && !c.IsOut);
            if (partyStanding && enemiesStanding)
                return false;

            State = EncounterState.Finished;
            Winner = partyStanding ? Side.Party : Side.Enemies;
            Write(Winner == Side.Party ? "Drużyna wygrywa starcie." : "Drużyna przegrywa starcie.");
            if (Winner == Side.Party)
                AwardExperience();
            return true;
        }

        private void AwardExperience()
        {
            int partyLevel = _order.Where(c => c.Side == Side.Party).Max(c => c.Level);
            ExperienceAwarded = _order.Where(c => c.Side == Side.Enemies).Sum(c => Rewards.ExperienceFor(partyLevel, c.Level));
            Write($"Zdobyte doświadczenie: {ExperienceAwarded} XP.");

            foreach (PlayerCombatant player in _order.OfType<PlayerCombatant>())
            {
                if (player.AwardExperience(ExperienceAwarded) > 0)
                    Write($"{player.Name} awansuje na poziom {player.Level}.");
            }
        }

        public Combatant UnitAt(GridPos pos)
        {
            return _order.FirstOrDefault(c => c.Position == pos && !c.IsOut);
        }

        public Dictionary<GridPos, int> StrideTargets()
        {
            Combatant unit = Current;
            if (unit == null)
                return new Dictionary<GridPos, int>();

            return Movement.Reachable(
                Grid,
                unit.Position,
                unit.SpeedFeet,
                pos => UnitAt(pos) is Combatant other && other.Side != unit.Side,
                pos => UnitAt(pos) != null);
        }

        public List<GridPos> StepTargets()
        {
            Combatant unit = Current;
            if (unit == null)
                return new List<GridPos>();
            return Movement.StepTargets(Grid, unit.Position, pos => UnitAt(pos) != null);
        }

        private ActionResult CheckCanAct(int cost)
        {
            if (State != EncounterState.InProgress)
                return new ActionResult(false, "Starcie nie trwa.");
            if (!Current.Actions.CanUseMainAction(cost))
                return new ActionResult(false, "Brakuje akcji w tej turze.");
            return null;
        }

        public ActionResult Stride(GridPos destination)
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            if (!StrideTargets().TryGetValue(destination, out int cost))
                return new ActionResult(false, "Tego pola nie da się osiągnąć w jednym ruchu.");

            Combatant unit = Current;
            unit.Position = destination;
            unit.Actions.UseMainAction(1);
            return Done($"{unit.Name} idzie {cost} stóp.");
        }

        public ActionResult Step(GridPos destination)
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            if (!StepTargets().Contains(destination))
                return new ActionResult(false, "Krok musi prowadzić na sąsiednie wolne pole bez trudnego terenu.");

            Combatant unit = Current;
            unit.Position = destination;
            unit.Actions.UseMainAction(1);
            return Done($"{unit.Name} robi krok.");
        }

        // Czy i jak ta broń sięga celu. Null w Refusal oznacza, że atak jest możliwy.
        public (string Refusal, bool Thrown, int? RangeIncrement) StrikeReach(Combatant attacker, Combatant target, StrikeOption strike)
        {
            int distance = BattleGrid.DistanceFeet(attacker.Position, target.Position);
            Equipment.Weapon weapon = strike.Weapon;
            bool thrown = false;

            if (weapon.Kind == WeaponKind.Melee)
            {
                if (distance <= AttackRules.MeleeReachFeet(weapon))
                    return (null, false, null);
                if (!weapon.HasTrait<ThrownTrait>())
                    return ("Cel jest poza zasięgiem broni wręcz.", false, null);
                thrown = true;
            }

            int? increment = AttackRules.RangeIncrementFor(weapon, thrown);
            if (!AttackRules.IsInRange(increment, distance))
                return ("Cel jest poza maksymalnym zasięgiem.", thrown, increment);
            if (!Grid.HasLineOfSight(attacker.Position, target.Position))
                return ("Brak linii widzenia do celu.", thrown, increment);
            return (null, thrown, increment);
        }

        public ActionResult Strike(Combatant target, StrikeOption strike, WeaponDamageType? damageType = null)
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            Combatant attacker = Current;
            if (target == attacker || target.Side == attacker.Side)
                return new ActionResult(false, "Można atakować tylko przeciwników.");
            if (target.IsOut)
                return new ActionResult(false, "Ten cel już nie walczy.");

            (string refusal, bool thrown, int? increment) = StrikeReach(attacker, target, strike);
            if (refusal != null)
                return new ActionResult(false, refusal);

            Equipment.Weapon weapon = strike.Weapon;
            bool ranged = weapon.Kind == WeaponKind.Ranged;
            if (ranged && strike.Carried != null && !strike.Carried.CanFire)
                return new ActionResult(false, $"{strike.Name} nie jest gotowa do strzału.");

            WeaponDamageType type = strike.Carried?.CurrentDamageType ?? weapon.DamageType;
            if (damageType.HasValue)
            {
                bool allowed = strike.Carried != null ? strike.Carried.CanStrikeWith(damageType.Value) : weapon.CanDeal(damageType.Value);
                if (!allowed)
                    return new ActionResult(false, $"{strike.Name} nie zadaje teraz tego typu obrażeń.");
                type = damageType.Value;
            }

            int distance = BattleGrid.DistanceFeet(attacker.Position, target.Position);
            int bonus = attacker.AttackBonus(strike, thrown)
                + AttackRules.MultipleAttackPenalty(attacker.Actions.AttacksThisTurn, weapon);
            if (ranged || thrown)
                bonus += AttackRules.RangePenalty(increment, distance) + AttackRules.VolleyPenalty(weapon, distance);

            Check.Result roll = Check.Roll(bonus, target.ArmorClass);
            int amount = Damage.Roll(weapon, attacker.DamageBonus(strike, thrown), roll.Degree, strike.Carried?.StrikingRune ?? 0);

            attacker.Actions.UseMainAction(1);
            attacker.Actions.RegisterAttack();
            if (ranged)
                strike.Carried?.Fire();

            if (amount > 0)
                target.Health.TakeDamage(amount, roll.Degree == DegreeOfSuccess.CriticalSuccess);

            string sign = bonus >= 0 ? "+" : "-";
            string summary = $"{attacker.Name} atakuje {target.Name} ({strike.Name}): {roll.Natural} {sign} {Math.Abs(bonus)} = {roll.Total} przeciw AC {roll.Dc}, {DegreeName(roll.Degree)}";
            if (amount > 0)
                summary += $", obrażenia: {amount} ({DamageTypeName(type)})";
            ActionResult result = Done(summary + ".");

            if (target.IsOut)
                Write(target.Health.IsDead ? $"{target.Name} ginie." : $"{target.Name} pada nieprzytomny.");
            CheckVictory();
            return result;
        }

        public ActionResult Reload(CarriedWeapon weapon)
        {
            int cost = weapon.Template.ActionsToReload;
            if (cost <= 0)
                return new ActionResult(false, "Tej broni nie trzeba przeładowywać.");

            ActionResult refused = CheckCanAct(cost);
            if (refused != null)
                return refused;

            weapon.Reload();
            Current.Actions.UseMainAction(cost);
            return Done($"{Current.Name} przeładowuje {weapon.Name}.");
        }

        public ActionResult SwitchModular(CarriedWeapon weapon, WeaponDamageType type)
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            if (!weapon.SwitchModular(type))
                return new ActionResult(false, $"{weapon.Name} nie ma trybu {DamageTypeName(type)}.");

            Current.Actions.UseMainAction(1);
            return Done($"{Current.Name} przełącza {weapon.Name} na {DamageTypeName(type)}.");
        }

        private ActionResult Done(string message)
        {
            Write(message);
            return new ActionResult(true, message);
        }

        private void Write(string message)
        {
            Log.Add(message);
            LogAdded?.Invoke(message);
        }

        public static string DegreeName(DegreeOfSuccess degree)
        {
            switch (degree)
            {
                case DegreeOfSuccess.CriticalSuccess:
                    return "krytyczny sukces";
                case DegreeOfSuccess.Success:
                    return "sukces";
                case DegreeOfSuccess.Failure:
                    return "porażka";
                default:
                    return "krytyczna porażka";
            }
        }

        public static string DamageTypeName(WeaponDamageType type)
        {
            switch (type)
            {
                case WeaponDamageType.Bludgeoning:
                    return "obuchowe";
                case WeaponDamageType.Piercing:
                    return "kłute";
                default:
                    return "cięte";
            }
        }
    }
}
