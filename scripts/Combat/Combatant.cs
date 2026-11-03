using System;
using System.Collections.Generic;
using Tracefinder.Character;
using Tracefinder.Gameplay;
using Tracefinder.Items;

namespace Tracefinder.Combat
{
    public enum Side
    {
        Party,
        Enemies
    }

    // Monster jest ustawiony tylko dla potworów, Carried tylko dla postaci gracza
    public record StrikeOption(string Name, Equipment.Weapon Weapon, CarriedWeapon Carried, MonsterStrike Monster);

    // Wspólny widok jednostki w walce, dzięki czemu Encounter nie rozróżnia postaci gracza i potwora
    public abstract class Combatant
    {
        public string Name { get; protected init; } = "";
        public Side Side { get; protected init; }
        public GridPos Position { get; set; }
        public int Initiative { get; set; }
        public ActionTracker Actions { get; } = new();
        public bool ShieldRaised { get; set; }
        public bool HasActed { get; set; }
        public bool UsedBattleMedicine { get; set; }

        public abstract HealthTracker Health { get; }
        public abstract int Level { get; }
        public abstract int SpeedFeet { get; }
        public abstract int ArmorClass { get; }
        public abstract int Perception { get; }
        public abstract IReadOnlyList<StrikeOption> Strikes { get; }
        public abstract int AttackBonus(StrikeOption strike, bool thrown);
        public abstract int DamageBonus(StrikeOption strike, bool thrown);
        public abstract int SaveBonus(SaveType save);

        public virtual int InitiativeBonus => Perception;
        public virtual bool IsUndead => false;
        public virtual bool HasReactiveStrike => false;
        public virtual bool HasShieldBlock => false;
        public virtual bool HasShield => false;
        public virtual bool HasViciousSwing => false;
        public virtual bool HasNimbleDodge => false;
        public virtual bool HasReactiveShield => false;
        public virtual int SneakAttackDice => 0;

        public bool IsOut => Health.IsUnconscious;
    }

    public class PlayerCombatant : Combatant
    {
        private readonly CharacterCore _core;
        private readonly ClassStats _classStats;

        public PlayerCombatant(CharacterCore core, ClassStats classStats, GridPos position)
        {
            _core = core;
            _classStats = classStats;
            Name = core.Name;
            Side = Side.Party;
            Position = position;
            if (core.Health.MaxHp == 0)
                core.InitializeHealth(classStats);
        }

        public CharacterCore Core => _core;
        public ClassStats ClassStats => _classStats;

        public override HealthTracker Health => _core.Health;
        public override int Level => _core.Level;
        public override int SpeedFeet => _core.SpeedFeet;
        public override int ArmorClass => _core.ComputeMaxAc(_classStats);
        public override int Perception => _core.ComputePerception(_classStats);
        public override int InitiativeBonus => _core.ComputeInitiative(_classStats);
        public override bool HasReactiveStrike => _classStats.Name == ClassList.Fighter.Name;
        public override bool HasShieldBlock => _classStats.Name == ClassList.Fighter.Name;
        public override bool HasShield => _core.ShieldUsable;
        public override bool HasViciousSwing => _core.HasFeat(FeatList.ViciousSwing);
        public override bool HasNimbleDodge => _core.HasFeat(FeatList.NimbleDodge);
        public override bool HasReactiveShield => _core.HasFeat(FeatList.ReactiveShield);
        public override int SneakAttackDice => _core.SneakAttackDice(_classStats);

        public void DamageShield(int amount)
        {
            _core.ShieldHp = Math.Max(_core.ShieldHp - amount, 0);
        }

        // Zwraca liczbę zdobytych poziomów
        public int AwardExperience(int amount)
        {
            return _core.AddExperience(amount, _classStats);
        }

        public override IReadOnlyList<StrikeOption> Strikes
        {
            get
            {
                List<StrikeOption> strikes = new();
                if (_core.EquippedWeapon != null)
                    strikes.Add(new StrikeOption(_core.EquippedWeapon.Name, _core.EquippedWeapon.Template, _core.EquippedWeapon, null));
                return strikes;
            }
        }

        public override int AttackBonus(StrikeOption strike, bool thrown)
        {
            return _core.ComputeAttackBonus(strike.Carried, _classStats, thrown);
        }

        public override int DamageBonus(StrikeOption strike, bool thrown)
        {
            return _core.ComputeDamageBonus(strike.Weapon, thrown);
        }

        public override int SaveBonus(SaveType save)
        {
            switch (save)
            {
                case SaveType.Fortitude:
                    return _core.ComputeFortitude(_classStats);
                case SaveType.Reflex:
                    return _core.ComputeReflex(_classStats);
                default:
                    return _core.ComputeWill(_classStats);
            }
        }
    }

    public class MonsterCombatant : Combatant
    {
        private readonly Statblock _statblock;
        private readonly HealthTracker _health = new() { DiesAtZeroHp = true };
        private readonly List<StrikeOption> _strikes = new();

        public MonsterCombatant(Statblock statblock, GridPos position, string name = null)
        {
            _statblock = statblock;
            Name = name ?? statblock.Name;
            Side = Side.Enemies;
            Position = position;
            _health.SetMaxHp(statblock.MaxHp);
            _health.SetToMaxHp();
            foreach (MonsterStrike strike in statblock.Strikes)
                _strikes.Add(new StrikeOption(strike.Name, strike.Weapon, null, strike));
        }

        public override HealthTracker Health => _health;
        public override int Level => _statblock.Level;
        public override int SpeedFeet => _statblock.SpeedFeet;
        public override int ArmorClass => _statblock.ArmorClass;
        public override int Perception => _statblock.Perception;
        public override IReadOnlyList<StrikeOption> Strikes => _strikes;
        public override bool IsUndead => _statblock.IsUndead;
        public override bool HasReactiveStrike => _statblock.HasReactiveStrike;

        public override int AttackBonus(StrikeOption strike, bool thrown)
        {
            return strike.Monster.AttackBonus;
        }

        public override int DamageBonus(StrikeOption strike, bool thrown)
        {
            return strike.Monster.DamageBonus;
        }

        public override int SaveBonus(SaveType save)
        {
            switch (save)
            {
                case SaveType.Fortitude:
                    return _statblock.Fortitude;
                case SaveType.Reflex:
                    return _statblock.Reflex;
                default:
                    return _statblock.Will;
            }
        }
    }
}
