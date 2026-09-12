using System;

namespace Tracefinder.Character
{
    public class HealthTracker
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }


        public void TakeDamage(int amount)
        {
            CurrentHp = Math.Max(CurrentHp - amount, 0);
        }

        public void HealDamage(int amount)
        {
            CurrentHp = Math.Min(CurrentHp + amount, MaxHp);
        }

        public void SetMaxHp(int MaxHp)
        {
            CurrentHp = MaxHp;
        }

        public bool IsUnconscious => CurrentHp <= 0;

    }
}
