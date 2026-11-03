using System;

namespace Tracefinder.World
{
    public enum Biome
    {
        Water,
        Plains,
        Forest,
        Mountains,
        Settlement
    }

    // Mapa świata powstaje wyłącznie z ziarna, więc w zapisie gry wystarczy samo ziarno
    public class WorldMap
    {
        private const double WaterLevel = 0.36;
        private const double MountainLevel = 0.66;
        private const double ForestMoisture = 0.52;
        private const int SettlementChancePercent = 4;

        private readonly Biome[,] _cells;

        public int Seed { get; }
        public int Width { get; }
        public int Height { get; }

        private WorldMap(int seed, int width, int height)
        {
            Seed = seed;
            Width = width;
            Height = height;
            _cells = new Biome[width, height];
        }

        public Biome this[int x, int y] => _cells[x, y];

        public static bool IsVisitable(Biome biome)
        {
            return biome != Biome.Water;
        }

        public static WorldMap Generate(int seed, int width = 48, int height = 32)
        {
            WorldMap map = new(seed, width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double elevation = Noise(seed, x, y);
                    double moisture = Noise(seed ^ 0x5F3759D, x, y);
                    map._cells[x, y] = Classify(seed, x, y, elevation, moisture);
                }
            }
            return map;
        }

        private static Biome Classify(int seed, int x, int y, double elevation, double moisture)
        {
            if (elevation < WaterLevel)
                return Biome.Water;
            if (elevation > MountainLevel)
                return Biome.Mountains;
            if (moisture > ForestMoisture)
                return Biome.Forest;
            if (Mix(seed ^ 0x1234567, x, y) % 100 < SettlementChancePercent)
                return Biome.Settlement;
            return Biome.Plains;
        }

        // Ziarno lokalizacji: ta sama komórka tego samego świata zawsze daje tę samą mapę walki
        public static int LocationSeed(int worldSeed, int x, int y)
        {
            return (int)Mix(worldSeed, x, y);
        }

        private static double Noise(int seed, int x, int y)
        {
            double total = 0;
            double weight = 0;
            double amplitude = 1;
            double scale = 0.09;
            for (int octave = 0; octave < 3; octave++)
            {
                total += ValueNoise(seed + octave * 7919, x * scale, y * scale) * amplitude;
                weight += amplitude;
                amplitude *= 0.5;
                scale *= 2;
            }
            return total / weight;
        }

        private static double ValueNoise(int seed, double x, double y)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            double tx = Smooth(x - x0);
            double ty = Smooth(y - y0);
            double top = Lerp(Corner(seed, x0, y0), Corner(seed, x0 + 1, y0), tx);
            double bottom = Lerp(Corner(seed, x0, y0 + 1), Corner(seed, x0 + 1, y0 + 1), tx);
            return Lerp(top, bottom, ty);
        }

        private static double Corner(int seed, int x, int y)
        {
            return Mix(seed, x, y) % 10000 / 10000.0;
        }

        private static double Smooth(double t)
        {
            return t * t * (3 - 2 * t);
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        // Własne mieszanie zamiast GetHashCode, bo tamto nie jest stabilne między uruchomieniami
        private static uint Mix(int seed, int x, int y)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u;
                h ^= (uint)x * 73856093u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 19349663u;
                h *= 2246822519u;
                h ^= h >> 15;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
