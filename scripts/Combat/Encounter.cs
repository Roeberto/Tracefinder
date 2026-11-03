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
        private readonly HashSet<Combatant> _reportedFallen = new();
        private int _turnIndex;

        public BattleGrid Grid { get; }
        public IReadOnlyList<Combatant> Combatants => _order;
        public EncounterState State { get; private set; } = EncounterState.NotStarted;
        public int Round { get; private set; }
        public Side? Winner { get; private set; }
        public int ExperienceAwarded { get; private set; }
        public List<string> Log { get; } = new();
        public event Action<string> LogAdded;
        public event Action Finished;
        public int ExperienceMultiplier { get; set; } = 1;

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
                combatant.Initiative = Dice.Roll(Dice.DiceSize.D20, 1) + combatant.InitiativeBonus;
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
                combatant.ShieldRaised = false;
                combatant.HasActed = true;
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
            foreach (PlayerCombatant player in _order.OfType<PlayerCombatant>())
            {
                foreach (CarriedWeapon weapon in player.Core.Inventory)
                    weapon.Reload();
            }
            Finished?.Invoke();
            return true;
        }

        private void AwardExperience()
        {
            int partyLevel = _order.Where(c => c.Side == Side.Party).Max(c => c.Level);
            ExperienceAwarded = ExperienceMultiplier * _order.Where(c => c.Side == Side.Enemies).Sum(c => Rewards.ExperienceFor(partyLevel, c.Level));
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
            if (Current.IsOut)
                return new ActionResult(false, "Ta jednostka już nie może działać.");
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
            unit.Actions.UseMainAction(1);
            if (!TriggerReactiveStrikes(unit))
                return Done($"{unit.Name} pada, zanim zdąży się ruszyć.");

            unit.Position = destination;
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

        public ActionResult Strike(
            Combatant target,
            StrikeOption strike,
            WeaponDamageType? damageType = null,
            bool viciousSwing = false,
            bool useHeroPoint = false)
        {
            int cost = viciousSwing ? 2 : 1;
            ActionResult refused = CheckCanAct(cost);
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
            if (viciousSwing && !attacker.HasViciousSwing)
                return new ActionResult(false, "Ta postać nie zna Vicious Swing.");
            if (viciousSwing && (ranged || thrown))
                return new ActionResult(false, "Vicious Swing działa tylko wręcz.");
            if (strike.Carried != null && !strike.Carried.CanFire)
                return new ActionResult(false, $"{strike.Name} nie jest gotowa do użycia.");

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

            attacker.Actions.UseMainAction(cost);
            attacker.Actions.RegisterAttack();
            if (viciousSwing)
                attacker.Actions.RegisterAttack();

            // Strzał w zasięgu wroga prowokuje Reactive Strike
            if ((ranged || thrown) && !TriggerReactiveStrikes(attacker))
                return Done($"{attacker.Name} pada, zanim zdąży zaatakować.");

            if (ranged)
                strike.Carried?.Fire();
            else if (thrown)
                strike.Carried?.MarkThrown();

            string summary = ResolveAttack(attacker, target, strike, bonus, thrown, type, viciousSwing, useHeroPoint);
            ActionResult result = Done(summary);
            AfterAttack(target);
            return result;
        }

        private void AfterAttack(Combatant target)
        {
            ReportFallen();
            CheckVictory();
        }

        private void ReportFallen()
        {
            foreach (Combatant unit in _order.Where(c => c.IsOut && !_reportedFallen.Contains(c)))
            {
                _reportedFallen.Add(unit);
                Write(unit.Health.IsDead ? $"{unit.Name} ginie." : $"{unit.Name} pada nieprzytomny.");
            }
            _reportedFallen.RemoveWhere(c => !c.IsOut);
        }

        // Rzut, reakcje obrońcy, obrażenia i opis wyniku do dziennika
        private string ResolveAttack(
            Combatant attacker,
            Combatant target,
            StrikeOption strike,
            int bonus,
            bool thrown,
            WeaponDamageType type,
            bool viciousSwing,
            bool useHeroPoint)
        {
            Equipment.Weapon weapon = strike.Weapon;
            bool offGuard = IsOffGuardAgainst(attacker, target, strike, thrown);
            int armorClass = EffectiveArmorClass(target) - (offGuard ? 2 : 0);

            Check.Result roll = Check.Roll(bonus, armorClass);
            string extras = offGuard ? " Cel jest off-guard." : "";

            if (useHeroPoint && roll.Degree <= DegreeOfSuccess.Failure && attacker.Health.HeroPoints > 0)
            {
                attacker.Health.SpendHeroPoint();
                roll = Check.Roll(bonus, armorClass);
                extras += " Hero Point: przerzut.";
            }

            roll = TryDefensiveReaction(target, roll, ref extras);

            int damageBonus = attacker.DamageBonus(strike, thrown);
            if (offGuard && QualifiesForSneakAttack(attacker, weapon, thrown))
            {
                damageBonus += Dice.Roll(Dice.DiceSize.D6, attacker.SneakAttackDice);
                extras += " Sneak Attack.";
            }

            int strikingDice = (strike.Carried?.StrikingRune ?? 0) + (viciousSwing ? ViciousSwingDice(attacker.Level) : 0);
            int amount = Damage.Roll(weapon, damageBonus, roll.Degree, strikingDice);
            amount = TryShieldBlock(target, amount, ref extras);

            if (amount > 0)
                target.Health.TakeDamage(amount, roll.Degree == DegreeOfSuccess.CriticalSuccess);

            string sign = bonus >= 0 ? "+" : "-";
            string summary = $"{attacker.Name} atakuje {target.Name} ({strike.Name}): {roll.Natural} {sign} {Math.Abs(bonus)} = {roll.Total} przeciw AC {roll.Dc}, {DegreeName(roll.Degree)}";
            if (amount > 0)
                summary += $", obrażenia: {amount} ({DamageTypeName(type)})";
            return summary + "." + extras;
        }

        // Dwie kości od poziomu 10, trzy od 18
        private static int ViciousSwingDice(int level)
        {
            if (level >= 18)
                return 3;
            return level >= 10 ? 2 : 1;
        }

        private static int EffectiveArmorClass(Combatant target)
        {
            return target.ArmorClass + (target.ShieldRaised && target.HasShield ? ShopCatalog.ShieldAcBonus : 0);
        }

        // Nimble Dodge i Reactive Shield zużywają reakcję tylko wtedy, gdy zmienia to trafienie w pudło
        private Check.Result TryDefensiveReaction(Combatant target, Check.Result roll, ref string extras)
        {
            if (roll.Degree < DegreeOfSuccess.Success || target.Actions.Reaction <= 0 || target.IsOut)
                return roll;

            bool canShield = target.HasReactiveShield && target.HasShield && !target.ShieldRaised;
            if (!target.HasNimbleDodge && !canShield)
                return roll;

            Check.Result withBonus = Check.Evaluate(roll.Natural, roll.Total, roll.Dc + 2);
            if (withBonus.Degree >= DegreeOfSuccess.Success)
                return roll;

            target.Actions.UseReaction();
            if (target.HasNimbleDodge)
            {
                extras += $" {target.Name} unika dzięki Nimble Dodge.";
            }
            else
            {
                target.ShieldRaised = true;
                extras += $" {target.Name} zasłania się tarczą.";
            }
            return withBonus;
        }

        private static int TryShieldBlock(Combatant target, int amount, ref string extras)
        {
            if (amount <= 0 || !target.ShieldRaised || !target.HasShield || !target.HasShieldBlock || target.Actions.Reaction <= 0)
                return amount;

            target.Actions.UseReaction();
            int reduced = Math.Max(amount - ShopCatalog.ShieldHardness, 0);
            if (target is PlayerCombatant player)
                player.DamageShield(reduced);
            extras += $" {target.Name} blokuje tarczą {amount - reduced} obrażeń.";
            return reduced;
        }

        public bool IsOffGuardAgainst(Combatant attacker, Combatant target, StrikeOption strike, bool thrown)
        {
            // Surprise Attack Zbira. Uproszczenie: działa zawsze w pierwszej rundzie, bez rzutu na inicjatywę z Oszustwa albo Skradania.
            if (Round == 1 && !target.HasActed && attacker.SneakAttackDice > 0)
                return true;

            if (strike.Weapon.Kind == WeaponKind.Ranged || thrown)
                return false;
            return IsFlanked(attacker, target);
        }

        // Sojusznik atakującego stoi naprzeciwko niego, po drugiej stronie celu. Reguła flankowania z pamięci modelu: NIEZWERYFIKOWANA.
        public bool IsFlanked(Combatant attacker, Combatant target)
        {
            int dx = target.Position.X - attacker.Position.X;
            int dy = target.Position.Y - attacker.Position.Y;
            if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1)
                return false;

            GridPos opposite = new(target.Position.X + dx, target.Position.Y + dy);
            Combatant ally = UnitAt(opposite);
            return ally != null && ally != attacker && ally.Side == attacker.Side;
        }

        private static bool QualifiesForSneakAttack(Combatant attacker, Equipment.Weapon weapon, bool thrown)
        {
            if (attacker.SneakAttackDice <= 0)
                return false;
            if (weapon.Kind == WeaponKind.Ranged)
                return true;
            return weapon.HasTrait<AgileTrait>() || weapon.HasTrait<FinesseTrait>();
        }

        // Reactive Strike wroga, gdy jednostka rusza się, strzela albo manipuluje w jego zasięgu. Zwraca false, gdy jednostka padła.
        private bool TriggerReactiveStrikes(Combatant mover)
        {
            foreach (Combatant foe in _order.Where(c => c.Side != mover.Side && !c.IsOut && c.HasReactiveStrike && c.Actions.Reaction > 0))
            {
                StrikeOption melee = foe.Strikes.FirstOrDefault(s => s.Weapon.Kind == WeaponKind.Melee && (s.Carried == null || s.Carried.CanFire));
                if (melee == null || BattleGrid.DistanceFeet(foe.Position, mover.Position) > AttackRules.MeleeReachFeet(melee.Weapon))
                    continue;

                foe.Actions.UseReaction();
                WeaponDamageType type = melee.Carried?.CurrentDamageType ?? melee.Weapon.DamageType;
                Write("Reactive Strike! " + ResolveAttack(foe, mover, melee, foe.AttackBonus(melee, false), false, type, false, false));
                AfterAttack(mover);
                if (mover.IsOut || State != EncounterState.InProgress)
                    return false;
            }
            return true;
        }

        public ActionResult Reload(CarriedWeapon weapon)
        {
            if (weapon.IsLoaded)
                return new ActionResult(false, "Ta broń jest już gotowa.");

            int cost = weapon.ActionsToReady;
            ActionResult refused = CheckCanAct(cost);
            if (refused != null)
                return refused;

            Combatant unit = Current;
            unit.Actions.UseMainAction(cost);
            if (!TriggerReactiveStrikes(unit))
                return Done($"{unit.Name} pada, zanim przygotuje broń.");

            weapon.Reload();
            return Done($"{unit.Name} przygotowuje {weapon.Name}.");
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

        public ActionResult RaiseShield()
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            Combatant unit = Current;
            if (!unit.HasShield)
                return new ActionResult(false, "Ta postać nie ma tarczy.");
            if (unit.ShieldRaised)
                return new ActionResult(false, "Tarcza jest już podniesiona.");

            unit.Actions.UseMainAction(1);
            unit.ShieldRaised = true;
            return Done($"{unit.Name} podnosi tarczę (+{ShopCatalog.ShieldAcBonus} do AC).");
        }

        public ActionResult EquipWeapon(CarriedWeapon weapon)
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            if (Current is not PlayerCombatant player || !player.Core.Inventory.Contains(weapon))
                return new ActionResult(false, "Tej broni nie ma w ekwipunku.");
            if (player.Core.EquippedWeapon == weapon)
                return new ActionResult(false, "Ta broń jest już w ręku.");

            player.Actions.UseMainAction(1);
            if (!TriggerReactiveStrikes(player))
                return Done($"{player.Name} pada, zanim dobędzie broń.");

            player.Core.EquippedWeapon = weapon;
            return Done($"{player.Name} dobywa {weapon.Name}.");
        }

        public ActionResult DrinkPotion(Potion potion)
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            if (Current is not PlayerCombatant player || player.Core.Potions.GetValueOrDefault(potion.Name) <= 0)
                return new ActionResult(false, "Nie ma takiej mikstury.");

            player.Actions.UseMainAction(1);
            if (!TriggerReactiveStrikes(player))
                return Done($"{player.Name} pada, zanim wypije miksturę.");

            player.Core.Potions[potion.Name]--;
            int healed = Dice.Roll(potion.Die, potion.Dice) + potion.Flat;
            player.Health.HealDamage(healed);
            return Done($"{player.Name} pije {potion.Name} i odzyskuje {healed} HP.");
        }

        public ActionResult UseBattleMedicine()
        {
            ActionResult refused = CheckCanAct(1);
            if (refused != null)
                return refused;

            if (Current is not PlayerCombatant player || !player.Core.HasFeat(FeatList.BattleMedicine))
                return new ActionResult(false, "Ta postać nie zna Battle Medicine.");
            if (player.UsedBattleMedicine)
                return new ActionResult(false, "Battle Medicine już zużyte w tej walce.");

            player.Actions.UseMainAction(1);
            if (!TriggerReactiveStrikes(player))
                return Done($"{player.Name} pada, zanim opatrzy rany.");

            HealingResult result = Healing.TreatWounds(player.Core, player.ClassStats, false);
            if (!result.Attempted)
                return Done($"{player.Name} nie ma wyszkolenia w Medycynie.");

            player.UsedBattleMedicine = true;
            return Done(result.Healed > 0
                ? $"{player.Name} opatruje rany: {result.Healed} HP."
                : result.Damage > 0 ? $"{player.Name} pogarsza rany: {result.Damage} obrażeń." : $"{player.Name} nie udaje się opatrzyć ran.");
        }

        public int SpellRange(Spell spell)
        {
            bool reach = Current is PlayerCombatant player && player.Core.HasFeat(FeatList.ReachSpell);
            return spell.RangeFeet + (reach ? 30 : 0);
        }

        public string SpellTargetRefusal(Spell spell, Combatant target)
        {
            Combatant caster = Current;
            if (target.IsOut && !(spell.Effect == SpellEffect.Heal && target.Side == caster.Side))
                return "Ten cel już nie walczy.";

            if (spell.Effect == SpellEffect.Heal)
            {
                if (target.Side != caster.Side && !target.IsUndead)
                    return "Heal leczy sojuszników, a przeciwnika może tylko ranić, gdy jest nieumarły.";
                if (target.Side == caster.Side && target.IsUndead)
                    return "Tego celu nie da się wyleczyć.";
            }
            else if (spell.BurstFeet == 0 && target.Side == caster.Side)
            {
                return "To zaklęcie rzuca się na przeciwników.";
            }
            else if (spell == SpellList.Harm && target.IsUndead)
            {
                return "Harm nie rani nieumarłych.";
            }

            int distance = BattleGrid.DistanceFeet(caster.Position, target.Position);
            if (distance > SpellRange(spell))
                return "Cel jest poza zasięgiem zaklęcia.";
            if (target != caster && !Grid.HasLineOfSight(caster.Position, target.Position))
                return "Brak linii widzenia do celu.";
            return null;
        }

        public ActionResult CastSpell(Spell spell, Combatant target, int? chosenRank = null)
        {
            ActionResult refused = CheckCanAct(spell.Actions);
            if (refused != null)
                return refused;

            if (Current is not PlayerCombatant caster)
                return new ActionResult(false, "Ta jednostka nie rzuca zaklęć.");

            string refusal = SpellTargetRefusal(spell, target);
            if (refusal != null)
                return new ActionResult(false, refusal);

            List<int> ranks = caster.Core.CastingRanks(spell, caster.ClassStats);
            int rank = chosenRank ?? (ranks.Count > 0 ? ranks[0] : 0);
            if (!ranks.Contains(rank))
                return new ActionResult(false, $"Brak wolnych slotów na {spell.Name}.");

            caster.Actions.UseMainAction(spell.Actions);
            caster.Core.ConsumeSlot(spell, rank, caster.ClassStats);
            if (!TriggerReactiveStrikes(caster))
                return Done($"{caster.Name} pada, zanim dokończy zaklęcie.");

            string summary;
            switch (spell.Effect)
            {
                case SpellEffect.AttackRoll:
                    summary = CastAttack(caster, spell, rank, target);
                    break;
                case SpellEffect.BasicSave:
                    summary = CastSave(caster, spell, rank, target);
                    break;
                case SpellEffect.Heal:
                    summary = CastHeal(caster, spell, rank, target);
                    break;
                default:
                    int amount = SpellSlots.RollEffect(spell, rank);
                    target.Health.TakeDamage(amount);
                    summary = $"{caster.Name} rzuca {spell.Name} na {target.Name}: {amount} obrażeń ({spell.DamageName}).";
                    break;
            }

            ActionResult result = Done(summary);
            ReportFallen();
            CheckVictory();
            return result;
        }

        private string CastAttack(PlayerCombatant caster, Spell spell, int rank, Combatant target)
        {
            int bonus = caster.Core.ComputeSpellAttack(caster.ClassStats) + AttackRules.MultipleAttackPenalty(caster.Actions.AttacksThisTurn, false);
            caster.Actions.RegisterAttack();
            Check.Result roll = Check.Roll(bonus, EffectiveArmorClass(target));
            int amount = 0;
            if (roll.Degree >= DegreeOfSuccess.Success)
            {
                amount = SpellSlots.RollEffect(spell, rank);
                if (roll.Degree == DegreeOfSuccess.CriticalSuccess)
                    amount *= 2;
                target.Health.TakeDamage(amount, roll.Degree == DegreeOfSuccess.CriticalSuccess);
            }
            string text = $"{caster.Name} rzuca {spell.Name} na {target.Name}: {roll.Natural} + {bonus} = {roll.Total} przeciw AC {roll.Dc}, {DegreeName(roll.Degree)}";
            return amount > 0 ? $"{text}, obrażenia: {amount} ({spell.DamageName})." : text + ".";
        }

        private string CastSave(PlayerCombatant caster, Spell spell, int rank, Combatant target)
        {
            int dc = caster.Core.ComputeSpellDc(caster.ClassStats);
            int amount = SpellSlots.RollEffect(spell, rank);
            List<Combatant> targets = spell.BurstFeet > 0
                ? _order.Where(c => !c.IsOut && BattleGrid.DistanceFeet(c.Position, target.Position) <= spell.BurstFeet).ToList()
                : new List<Combatant> { target };

            List<string> parts = new();
            foreach (Combatant unit in targets)
                parts.Add(ApplySave(unit, spell.Save, dc, amount));
            return $"{caster.Name} rzuca {spell.Name} (DC {dc}, ranga {rank}): {string.Join("; ", parts)}.";
        }

        private static string ApplySave(Combatant unit, SaveType save, int dc, int amount)
        {
            Check.Result roll = Check.Roll(unit.SaveBonus(save), dc);
            int taken = Healing.HalvedOrScaled(amount, roll.Degree);
            if (taken > 0)
                unit.Health.TakeDamage(taken, roll.Degree == DegreeOfSuccess.CriticalFailure);
            return $"{unit.Name} {DegreeName(roll.Degree)}, obrażenia: {taken}";
        }

        private string CastHeal(PlayerCombatant caster, Spell spell, int rank, Combatant target)
        {
            int amount = SpellSlots.RollEffect(spell, rank);
            if (caster.Core.HasFeat(FeatList.HealingHands))
            {
                amount += spell.Dice + spell.DicePerStep * Math.Max(rank - 1, 0);
            }

            if (target.IsUndead)
            {
                int damage = SpellSlots.RollEffect(spell, rank, false);
                return $"{caster.Name} rzuca {spell.Name}: {ApplySave(target, SaveType.Fortitude, caster.Core.ComputeSpellDc(caster.ClassStats), damage)}.";
            }

            target.Health.HealDamage(amount);
            return $"{caster.Name} leczy {target.Name} o {amount} HP.";
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
