using System;


namespace Tracefinder.Gameplay
{

public class Dice
{   

    static Random rnd = new Random();

    public enum DiceSize
    {
        D4,
        D6,
        D8,
        D10,
        D12,
        D20,
        D100
    }
    public static int NumerOfSides(DiceSize diceSize)
    {
        switch(diceSize)
        {
            case DiceSize.D4:
                return 4;
            case DiceSize.D6:
                return 6;
            case DiceSize.D8:
                return 8;
            case DiceSize.D10:
                return 10;
            case DiceSize.D12:
                return 12;
            case DiceSize.D20:
                return 20;
            case DiceSize.D100:
                return 100;
            default:
                throw new ArgumentException($"Unknown DiceSize: {diceSize}");           
        }
    }

    public static int Roll(DiceSize diceSize, int diceNumber)
        {
            int sides = NumerOfSides(diceSize);
            int total = 0;
            for (int i = 0; i < diceNumber; i++)
            {
                total += rnd.Next(1, sides + 1);
            }
            return total;
        }
}
}