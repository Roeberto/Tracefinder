using System;

namespace Tracefinder.Character
{
    public class HealthTracker
    {
        public const int maxHeroPoints = 3;
        public const int maxDying = 4;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int heroPoints { get; private set; }
        public int dying { get; private set; }
        public int wounded { get; private set; }


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

        public void gainHeroPoint()
        {
            heroPoints = Math.Min(heroPoints + 1, maxHeroPoints);
        }

        public void spendHeroPoints()
        {
            heroPoints = Math.Max(heroPoints - 1, 0);
        }

        public void increaseDying()
        {
            dying = Math.Min(dying + 1, maxDying);
        }

        public void decreaseDying()
        {
            dying = Math.Max(dying - 1, 0);
        }

        public void increaseWounded()
        {
            wounded++;
        }

        public void decreaseWounded()
        {
            wounded = Math.Max(wounded - 1, 0);
        }

        public bool IsUnconscious => CurrentHp <= 0;

    }
}
