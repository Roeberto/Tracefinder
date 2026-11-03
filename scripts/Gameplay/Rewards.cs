using System;

namespace Tracefinder.Gameplay
{
    public static class Rewards
    {
        // GM Core, tabela 10-8: indeks to poziom przeciwnika minus poziom drużyny, od -4 do +4.
        // Wartości są podane dla czteroosobowej drużyny i według zasad nie zmieniają się przy innym składzie.
        private static readonly int[] CreatureXp = { 10, 15, 20, 30, 40, 60, 80, 120, 160 };

        public static int ExperienceFor(int partyLevel, int creatureLevel)
        {
            int difference = creatureLevel - partyLevel;
            if (difference < -4)
                return 0;
            return CreatureXp[Math.Min(difference, 4) + 4];
        }
    }
}
