using Tracefinder.Gameplay;

namespace Tracefinder.WeaponStats
{
    public abstract record WeaponTrait;
    public sealed record DeadlyTrait(Dice.DiceSize Die) : WeaponTrait;           // Deadly d10 - dodatkowa kość przy krytyku
    public sealed record FatalTrait(Dice.DiceSize Die) : WeaponTrait;            // Fatal d12 - krytyk zmienia kość obrażeń
    public sealed record JoustingTrait(Dice.DiceSize Die) : WeaponTrait;         // Jousting d6 - bonus przy walce wierzchem
    public sealed record TwoHandTrait(Dice.DiceSize Die) : WeaponTrait;          // Two-Hand d10 - kość obrażeń przy chwycie oburącz
    public sealed record VersatileTrait(WeaponDamageType Damage) : WeaponTrait;  // Versatile P - alternatywny typ obrażeń
    public sealed record VolleyTrait(int Range) : WeaponTrait;                   // Volley 30 ft. - -2 do ataku poniżej tego dystansu
    public sealed record ThrownTrait(int? Range = null) : WeaponTrait;
    public sealed record ModularTrait(params WeaponDamageType[] DamageTypes) : WeaponTrait;
    public sealed record AgileTrait : WeaponTrait;
    public sealed record AttachedToShieldTrait : WeaponTrait;
    public sealed record BackstabberTrait : WeaponTrait;
    public sealed record BackswingTrait : WeaponTrait;
    public sealed record ConcealableTrait : WeaponTrait;
    public sealed record DisarmTrait : WeaponTrait;
    public sealed record FinesseTrait : WeaponTrait;
    public sealed record ForcefulTrait : WeaponTrait;
    public sealed record FreeHandTrait : WeaponTrait;
    public sealed record GrappleTrait : WeaponTrait;
    public sealed record HamperingTrait : WeaponTrait;
    public sealed record NonlethalTrait : WeaponTrait;
    public sealed record ParryTrait : WeaponTrait;
    public sealed record PropulsiveTrait : WeaponTrait;
    public sealed record RangedTripTrait : WeaponTrait;
    public sealed record RazingTrait : WeaponTrait;
    public sealed record ReachTrait : WeaponTrait;
    public sealed record ShoveTrait : WeaponTrait;
    public sealed record SweepTrait : WeaponTrait;
    public sealed record TetheredTrait : WeaponTrait;
    public sealed record TripTrait : WeaponTrait;
    public sealed record TwinTrait : WeaponTrait;
    public sealed record UnarmedTrait : WeaponTrait;


// clasa pomocnicza
    public static class Trait
    {
        public static readonly AgileTrait Agile = new();
        public static readonly AttachedToShieldTrait AttachedToShield = new();
        public static readonly BackstabberTrait Backstabber = new();
        public static readonly BackswingTrait Backswing = new();
        public static readonly ConcealableTrait Concealable = new();
        public static readonly DisarmTrait Disarm = new();
        public static readonly FinesseTrait Finesse = new();
        public static readonly ForcefulTrait Forceful = new();
        public static readonly FreeHandTrait FreeHand = new();
        public static readonly GrappleTrait Grapple = new();
        public static readonly HamperingTrait Hampering = new();
        public static readonly NonlethalTrait Nonlethal = new();
        public static readonly ParryTrait Parry = new();
        public static readonly PropulsiveTrait Propulsive = new();
        public static readonly RangedTripTrait RangedTrip = new();
        public static readonly RazingTrait Razing = new();
        public static readonly ReachTrait Reach = new();
        public static readonly ShoveTrait Shove = new();
        public static readonly SweepTrait Sweep = new();
        public static readonly TetheredTrait Tethered = new();
        public static readonly TripTrait Trip = new();
        public static readonly TwinTrait Twin = new();
        public static readonly UnarmedTrait Unarmed = new();

        public static DeadlyTrait Deadly(Dice.DiceSize die) => new(die);
        public static FatalTrait Fatal(Dice.DiceSize die) => new(die);
        public static JoustingTrait Jousting(Dice.DiceSize die) => new(die);
        public static TwoHandTrait TwoHand(Dice.DiceSize die) => new(die);
        public static VersatileTrait Versatile(WeaponDamageType damage) => new(damage);
        public static VolleyTrait Volley(int range) => new(range);
        public static ThrownTrait Thrown(int? range = null) => new(range);
        public static ModularTrait Modular(params WeaponDamageType[] types) => new(types);
    }
}
