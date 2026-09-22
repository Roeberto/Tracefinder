using System;

namespace Tracefinder.Items
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