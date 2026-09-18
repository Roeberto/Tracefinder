using System;
using Tracefinder.WeaponStats;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using System.Collections.Generic;

namespace Tracefinder.WeaponList
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
        TraitList = [WeaponTrait.Versatile]
    };

    public static readonly Equipment.Weapon Longbow = new()
    {
        Name = "Longbow",
        Category = WeaponCategory.Martial,
        DamageDice = Dice.DiceSize.D8,
        DamageType = WeaponDamageType.Piercing,
        DiceNumber = 1,
        Group = WeaponGroup.Bow,
        Handedness = WeaponHandedness.OneHanded,
        RangeIncrement = 100,
        TraitList = [WeaponTrait.Deadly, WeaponTrait.Volley],
        DeadlyDie = Dice.DiceSize.D10,
        VolleyRange = 30,
    };



}

}