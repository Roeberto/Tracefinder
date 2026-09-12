using System;

public static class Diceroll
{   

    static Random rnd = new Random();
    public static int D4(int diceNumber)
    {
        int d4 = rnd.Next(1, 5);
        return d4;
    }

    public static int D6(int diceNumber)
    {
        int d6  = rnd.Next(1, 7);
        return d6;
    }

    public static int D8(int diceNumber)
    {
        int d8  = rnd.Next(1, 9);
        return d8;
    }

    public static int D10(int diceNumber)
    {
        int d10  = rnd.Next(1, 11);
        return d10;
    }

    public static int D12(int diceNumber)
    {
        int d12  = rnd.Next(1, 13);
        return d12;
    }

    public static int D20(int diceNumber)
    {
        int d20  = rnd.Next(1, 21);
        return d20;
    }

    public static int D100(int diceNumber)
    {
        int d100  = rnd.Next(1, 101);
        return d100;
    }
}