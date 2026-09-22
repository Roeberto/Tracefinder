using System;

namespace Tracefinder.WeaponStats
{
    public enum WeaponGroup
    {
        Bow,
        Hammer,
        Knife,
        Sword
    }

    public enum WeaponCategory
    {
        Simple,
        Martial,
        Advanced,
        Unarmed
    }

    public enum WeaponDamageType
    {
        Bludgeoning,
        Piercing,
        Slashing
    }

    public enum WeaponHandedness
    {
        OneHanded,
        TwoHanded,
        OneOrTwoHanded // noszenie w 1 ręce atak w 2 rękach
    }
}