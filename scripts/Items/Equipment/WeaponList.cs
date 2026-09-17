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
        DiceNumer = 1,
        Group = WeaponGroup.Sword,
        Handedness = WeaponHandedness.OneHanded,
        TraitList = [WeaponTrait.Versatile]
    };



}

}