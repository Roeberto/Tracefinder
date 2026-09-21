using System;

namespace Tracefinder.Character
{

public class HealthTracker
{
    public const int MaxHeroPoints = 3;
    public const int MaxDying = 4;

    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public int HeroPoints { get; private set; }
    public int Dying { get; private set; }
    public int Wounded { get; private set; }

    public void SetMaxHp(int value)
    {
        MaxHp = value;
        CurrentHp = Math.Min(CurrentHp, MaxHp);
    }

    public void TakeDamage(int amount)
    {
        CurrentHp = Math.Max(CurrentHp - amount, 0);
    }

    public void HealDamage(int amount)
    {
        CurrentHp = Math.Min(CurrentHp + amount, MaxHp);
    }

    public void SetToMaxHp()
    {
        CurrentHp = MaxHp;
    }

    public void GainHeroPoint()
    {
        HeroPoints = Math.Min(HeroPoints + 1, MaxHeroPoints);
    }

    public void SpendHeroPoint()
    {
        HeroPoints = Math.Max(HeroPoints - 1, 0);
    }

    public void IncreaseDying()
    {
        Dying = Math.Min(Dying + 1, MaxDying);
    }

    public void DecreaseDying()
    {
        Dying = Math.Max(Dying - 1, 0);
    }

    public void IncreaseWounded()
    {
        Wounded++;
    }

    public void DecreaseWounded()
    {
        Wounded = Math.Max(Wounded - 1, 0);
    }

    public bool IsUnconscious => CurrentHp <= 0;

}

}