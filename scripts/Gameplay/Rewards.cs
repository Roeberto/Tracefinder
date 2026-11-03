using System;

namespace Tracefinder.Gameplay
{
    public enum Threat
    {
        Low,
        Moderate,
        Severe,
        Extreme
    }

    public static class Rewards
    {
        // GM Core, tabela 10-1: budżet XP dla czterech postaci i zmiana na każdą postać mniej lub więcej
        private const int BaseParty = 4;
        private static readonly int[] Budgets = { 60, 80, 120, 160 };
        private static readonly int[] CharacterAdjustments = { 20, 20, 30, 40 };

        // GM Core, tabela 5-3: skarb w gp za starcie, wiersze to poziomy 1-20
        private static readonly int[,] Treasure =
        {
            { 13, 18, 26, 35 },
            { 23, 30, 45, 60 },
            { 38, 50, 75, 100 },
            { 65, 85, 130, 170 },
            { 100, 135, 200, 270 },
            { 150, 200, 300, 400 },
            { 220, 290, 440, 580 },
            { 300, 400, 600, 800 },
            { 430, 570, 860, 1140 },
            { 600, 800, 1200, 1600 },
            { 865, 1150, 1725, 2300 },
            { 1250, 1650, 2475, 3300 },
            { 1875, 2500, 3750, 5000 },
            { 2750, 3650, 5500, 7300 },
            { 4100, 5450, 8200, 10900 },
            { 6200, 8250, 12400, 16500 },
            { 9600, 12800, 19200, 25600 },
            { 15600, 20800, 31200, 41600 },
            { 26600, 35500, 53250, 71000 },
            { 36800, 49000, 73500, 98000 }
        };

        public static int Budget(Threat threat, int partySize)
        {
            int index = (int)threat;
            return Math.Max(Budgets[index] + (partySize - BaseParty) * CharacterAdjustments[index], 0);
        }

        public static int TreasureGp(int level, Threat threat)
        {
            return Treasure[Math.Clamp(level, 1, 20) - 1, (int)threat];
        }

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
