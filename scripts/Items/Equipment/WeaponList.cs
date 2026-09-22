using System;
using System.Collections.Generic;
using Tracefinder.Gameplay;

namespace Tracefinder.Items
{

public static class WeaponList
{
    public static readonly Equipment.Weapon Longsword = new()
    {
        Name = "Longsword",
        Category = WeaponCategory.Martial,
        DamageDice = Dice.DiceSize.D8,
        DamageType = WeaponDamageType.Slashing,
        DiceNumber = 1,
        Group = WeaponGroup.Sword,
        Handedness = WeaponHandedness.OneHanded,
        TraitList = [Trait.Versatile(WeaponDamageType.Piercing)]
    };

    public static readonly Equipment.Weapon Longbow = new()
    {
        Name = "Longbow",
        Category = WeaponCategory.Martial,
        DamageDice = Dice.DiceSize.D8,
        DamageType = WeaponDamageType.Piercing,
        DiceNumber = 1,
        Group = WeaponGroup.Bow,
        Handedness = WeaponHandedness.OneOrTwoHanded,
        RangeIncrement = 100,
        TraitList = [Trait.Deadly(Dice.DiceSize.D8), Trait.Volley(30)],
    };

    public static readonly Equipment.Weapon Dagger = new()
    {
        Name = "Dagger",
        Category = WeaponCategory.Simple,
        DamageDice = Dice.DiceSize.D4,
        DamageType = WeaponDamageType.Piercing,
        DiceNumber = 1,
        Group = WeaponGroup.Knife,
        Handedness = WeaponHandedness.OneHanded,
        TraitList = [WeaponTrait.Agile, WeaponTrait.Finesse, WeaponTrait.Thrown],
        RangeIncrement = 10
    };

    public static readonly Equipment.Weapon Rapier = new()
    {
        Name = "Rapier",
        Category = WeaponCategory.Martial,
        DamageDice = Dice.DiceSize.D6,
        DamageType = WeaponDamageType.Piercing,
        DiceNumber = 1,
        Group = WeaponGroup.Sword,
        Handedness = WeaponHandedness.OneHanded,
        TraitList = [WeaponTrait.Deadly, WeaponTrait.Disarm, WeaponTrait.Finesse]
    };

    public static readonly Equipment.Weapon Warhammer = new()
    {
        Name = "Warhammer",
        Category = WeaponCategory.Martial,
        DamageDice = Dice.DiceSize.D8,
        DamageType = WeaponDamageType.Bludgeoning,
        DiceNumber = 1,
        Group = WeaponGroup.Hammer,
        Handedness = WeaponHandedness.OneHanded,
        TraitList = []
    };

    public static readonly Equipment.Weapon Shortbow = new()
    {
        Name = "Shortbow",
        Category = WeaponCategory.Martial,
        DamageDice = Dice.DiceSize.D6,
        DamageType = WeaponDamageType.Piercing,
        DiceNumber = 1,
        Group = WeaponGroup.Bow,
        Handedness = WeaponHandedness.TwoHanded,
        TraitList = [WeaponTrait.Deadly],
        RangeIncrement = 60
    };

    // Musi być zadeklarowane po wszystkich broniach — pola statyczne
    // inicjalizują się w kolejności tekstowej, więc wcześniej byłyby tu null-e.
    public static readonly IReadOnlyList<Equipment.Weapon> All =
    [
        Dagger, Longsword, Rapier, Warhammer, Shortbow
    ];

}

}