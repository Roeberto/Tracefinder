using System;
using Tracefinder.Gameplay;

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

    // Potwory zwykle giną od razu przy 0 HP, bez stanu Dying
    public bool DiesAtZeroHp { get; set; }

    public void SetMaxHp(int value)
    {
        MaxHp = value;
        CurrentHp = Math.Min(CurrentHp, MaxHp);
    }

    public void TakeDamage(int amount, bool criticalHit = false)
    {
        bool wasDown = CurrentHp <= 0;
        CurrentHp = Math.Max(CurrentHp - amount, 0);

        if (CurrentHp > 0 || DiesAtZeroHp || amount <= 0)
            return;

        int gain = criticalHit ? 2 : 1;
        // Wounded dolicza się tylko przy nowym powaleniu, nie przy kolejnych obrażeniach
        int total = wasDown ? Dying + gain : Dying + gain + Wounded;
        Dying = Math.Min(total, MaxDying);
    }

    public void HealDamage(int amount)
    {
        if (amount <= 0)
            return;

        bool wasDown = CurrentHp <= 0;
        CurrentHp = Math.Min(CurrentHp + amount, MaxHp);

        if (wasDown && CurrentHp > 0 && Dying > 0)
        {
            Dying = 0;
            IncreaseWounded();
        }
    }

    public void SetToMaxHp()
    {
        CurrentHp = MaxHp;
    }

    // Płaski test na początku tury postaci umierającej, DC 10 + Dying
    public Check.Result RecoveryCheck()
    {
        Check.Result result = Check.Roll(0, 10 + Dying);
        ApplyRecovery(result.Degree);
        return result;
    }

    public void ApplyRecovery(DegreeOfSuccess degree)
    {
        if (Dying <= 0)
            return;

        switch (degree)
        {
            case DegreeOfSuccess.CriticalSuccess:
                Dying -= 2;
                break;
            case DegreeOfSuccess.Success:
                Dying -= 1;
                break;
            case DegreeOfSuccess.Failure:
                Dying += 1;
                break;
            case DegreeOfSuccess.CriticalFailure:
                Dying += 2;
                break;
        }

        if (Dying <= 0)
        {
            Dying = 0;
            IncreaseWounded();
        }
        Dying = Math.Min(Dying, MaxDying);
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

    public bool IsDead => DiesAtZeroHp ? CurrentHp <= 0 : Dying >= MaxDying;

}

}
