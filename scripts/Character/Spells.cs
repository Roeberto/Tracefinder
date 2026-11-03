using System;
using System.Collections.Generic;
using Tracefinder.Gameplay;

namespace Tracefinder.Character
{
    public enum SaveType
    {
        Fortitude,
        Reflex,
        Will
    }

    public enum SpellEffect
    {
        AttackRoll,
        BasicSave,
        AutoHit,
        Heal
    }

    // BaseRank 0 oznacza sztuczkę: bez slotu, siła rośnie z poziomem postaci
    public record Spell(
        string Name,
        int BaseRank,
        int Actions,
        int RangeFeet,
        SpellEffect Effect,
        SaveType Save,
        int Dice,
        Dice.DiceSize Die,
        int Flat,
        int HeightenStep,
        int DicePerStep,
        int FlatPerStep,
        string DamageName,
        int BurstFeet,
        string Summary)
    {
        public bool IsCantrip => BaseRank == 0;
    }

    // Wartości odczytane z Archives of Nethys (Heal) i danych systemu PF2e dla Foundry VTT (reszta).
    // Uproszczenia: każde zaklęcie ma jeden wariant liczby akcji, jest znane bez przygotowania, a Electric Arc ma jeden cel.
    public static class SpellList
    {
        public static readonly Spell TelekineticProjectile = new("Telekinetic Projectile", 0, 2, 30, SpellEffect.AttackRoll, SaveType.Reflex, 2, Gameplay.Dice.DiceSize.D6, 0, 1, 1, 0, "fizyczne", 0, "Rzut przedmiotem w cel. Atak zaklęciem.");
        public static readonly Spell ElectricArc = new("Electric Arc", 0, 2, 30, SpellEffect.BasicSave, SaveType.Reflex, 2, Gameplay.Dice.DiceSize.D4, 0, 1, 1, 0, "elektryczne", 0, "Łuk elektryczny. Podstawowy rzut na Reflex.");
        public static readonly Spell DivineLance = new("Divine Lance", 0, 2, 60, SpellEffect.AttackRoll, SaveType.Reflex, 2, Gameplay.Dice.DiceSize.D4, 0, 1, 1, 0, "duchowe", 0, "Włócznia boskiej energii. Atak zaklęciem.");
        public static readonly Spell ForceBarrage = new("Force Barrage", 1, 2, 120, SpellEffect.AutoHit, SaveType.Reflex, 2, Gameplay.Dice.DiceSize.D4, 2, 2, 2, 2, "siłą", 0, "Dwa odłamki, które zawsze trafiają. Co dwie rangi po jednym więcej na akcję.");
        public static readonly Spell Fireball = new("Fireball", 3, 2, 500, SpellEffect.BasicSave, SaveType.Reflex, 6, Gameplay.Dice.DiceSize.D6, 0, 1, 2, 0, "od ognia", 20, "Wybuch rani wszystkich w promieniu 20 stóp, także sojuszników i rzucającego.");
        public static readonly Spell Heal = new("Heal", 1, 2, 30, SpellEffect.Heal, SaveType.Fortitude, 1, Gameplay.Dice.DiceSize.D8, 8, 1, 1, 8, "życiowe", 0, "Leczy sojusznika lub siebie. Nieumarłych rani, podstawowy rzut na Fortitude.");
        public static readonly Spell Harm = new("Harm", 1, 2, 30, SpellEffect.BasicSave, SaveType.Fortitude, 1, Gameplay.Dice.DiceSize.D8, 0, 1, 1, 0, "pustki", 0, "Rani żywego przeciwnika. Podstawowy rzut na Fortitude.");

        public static readonly IReadOnlyList<Spell> Arcane = [TelekineticProjectile, ElectricArc, ForceBarrage, Fireball];
        public static readonly IReadOnlyList<Spell> Divine = [DivineLance, Heal, Harm];

        public static IReadOnlyList<Spell> For(ClassStats classStats)
        {
            if (classStats.Name == ClassList.Wizard.Name)
                return Arcane;
            if (classStats.Name == ClassList.Cleric.Name)
                return Divine;
            return Array.Empty<Spell>();
        }
    }

    // Tabela slotów sprawdzona w Archives of Nethys dla poziomów 1, 2, 3 i 5, reszta według tego samego wzoru
    public static class SpellSlots
    {
        public const int MaxRank = 10;

        public static int HighestRank(int level)
        {
            return Math.Min((level + 1) / 2, MaxRank);
        }

        public static int SlotsAt(int level, int rank)
        {
            int highest = HighestRank(level);
            if (rank < 1 || rank > highest)
                return 0;
            if (rank == MaxRank)
                return 1;
            if (rank < highest)
                return 3;
            return level % 2 == 1 ? 2 : 3;
        }

        // Divine Font: 4 sloty na Heal, 5 od poziomu 5 i 6 od poziomu 15, na najwyższej randze
        public static int FontSlots(int level)
        {
            if (level >= 15)
                return 6;
            return level >= 5 ? 5 : 4;
        }

        public static int CantripRank(int level)
        {
            return HighestRank(level);
        }

        // Siła zaklęcia po podniesieniu rangi. Sztuczka bazowo liczy się jak ranga 1.
        public static int RollEffect(Spell spell, int rank, bool withFlat = true)
        {
            int steps = Math.Max(rank - Math.Max(spell.BaseRank, 1), 0) / spell.HeightenStep;
            int dice = Dice.Roll(spell.Die, spell.Dice + spell.DicePerStep * steps);
            return withFlat ? dice + spell.Flat + spell.FlatPerStep * steps : dice;
        }
    }
}
