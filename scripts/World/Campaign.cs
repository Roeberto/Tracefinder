using System;
using System.Collections.Generic;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;

namespace Tracefinder.World
{
    public enum SiteKind
    {
        Town,
        Lair,
        Fortress
    }

    public enum TravelEvent
    {
        None,
        Blocked,
        Ambush,
        EnteredSite
    }

    public record Site(SiteKind Kind, int X, int Y, string Name, int Level, string Description);

    public record EncounterPlan(IReadOnlyList<Statblock> Enemies, Threat Threat, Site Site);

    public record VictoryReward(int Silver, int LevelsGained, Potion Potion, bool CampaignComplete);

    // Przebieg gry: bohater, świat, miejsca do odwiedzenia i postęp. Bez zależności od Godota, żeby dało się ją zagrać w testach.
    public class Campaign
    {
        // Wartości balansu kampanii dla jednej postaci, nie pochodzą z PF2e. Zasady liczą XP tak samo dla każdej drużyny,
        // co przy jednym bohaterze dałoby awans co kilkadziesiąt walk. Mnożnik skraca to do kilku walk na poziom. Skarb to wartość z tabeli GM Core za starcie.
        public const int ExperienceMultiplier = 15;
        public const int TreasureMultiplier = 1;
        public const int PotionDropPercent = 25;
        public const int CampAmbushPercent = 25;
        public const int StepsBetweenAmbushes = 6;
        public const int StepsBetweenTreatments = 5;
        public const int MinimumLand = 200;
        public const int LairCount = 3;
        public const int TownCount = 4;
        private const int MinimumSiteSpacing = 6;

        private static readonly string[] TownNames = { "Ostrów", "Brzezina", "Wilcze Pole", "Stary Bród", "Kamienna Dolina", "Jarzębina" };
        private static readonly (string Name, string Boss, int Level, MonsterFamily Family, MonsterFamily Minion)[] LairData =
        {
            ("Obóz Rozbójników", "Herszt Wrona", 2, MonsterFactory.Bandit, MonsterFactory.Poacher),
            ("Gniazdo w Starym Lesie", "Królowa Pająków", 3, MonsterFactory.Spider, MonsterFactory.Wolf),
            ("Warownia Ogrów", "Wódz Ogrów Murg", 4, MonsterFactory.Ogre, MonsterFactory.Orc)
        };
        private const string FortressName = "Popielna Twierdza";
        private const string FortressBoss = "Władca Popiołów";
        private const int FortressLevel = 6;

        private readonly HashSet<(int X, int Y)> _cleared = new();
        private readonly List<Site> _sites = new();

        public int Seed { get; }
        public WorldMap Map { get; }
        public CharacterCore Hero { get; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int FightsWon { get; private set; }
        public int Defeats { get; private set; }
        public int StepsTaken { get; private set; }
        public int StepsSinceFight { get; private set; }
        public int StepsSinceTreatment { get; private set; } = StepsBetweenTreatments;
        public bool Completed { get; private set; }

        // Nie zależy od zapisu: po wczytaniu gry zasadzka nie może powtórzyć się w nieskończoność, bo inaczej gracz by utknął
        public Random Roller { get; set; } = new();

        public IReadOnlyList<Site> Sites => _sites;
        public ClassStats HeroClass => ClassList.ByName(Hero.ClassID) ?? ClassList.Fighter;
        public Site CurrentSite => SiteAt(X, Y);
        public Site StartTown => _sites.First(s => s.Kind == SiteKind.Town);
        public int LairsCleared => _sites.Count(s => s.Kind == SiteKind.Lair && IsCleared(s));
        public bool FortressOpen => LairsCleared == LairCount;

        private Campaign(int seed, CharacterCore hero)
        {
            Seed = seed;
            Hero = hero;
            Map = WorldMap.Generate(seed);
            PlaceSites();
            X = StartTown.X;
            Y = StartTown.Y;
        }

        // Ziarno z małym lądem albo bez miejsca na wszystkie lokacje przechodzi na następne, więc każda gra da się ukończyć
        public static Campaign New(int seed, CharacterCore hero)
        {
            int playable = FindPlayableSeed(seed);
            hero.Silver = ShopCatalog.StartingSilver;
            hero.Health.GainHeroPoint();
            return new Campaign(playable, hero);
        }

        public static int FindPlayableSeed(int seed)
        {
            int current = seed;
            while (LargestLand(WorldMap.Generate(current)).Count < MinimumLand)
                current++;
            return current;
        }

        public Site SiteAt(int x, int y)
        {
            return _sites.FirstOrDefault(s => s.X == x && s.Y == y);
        }

        public bool IsCleared(Site site)
        {
            return _cleared.Contains((site.X, site.Y));
        }

        public bool CanWalk(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Map.Width && y < Map.Height && WorldMap.IsVisitable(Map[x, y]);
        }

        public string Objective()
        {
            if (Completed)
                return "Władca Popiołów został pokonany.";
            if (FortressOpen)
                return $"Wszystkie klucze zdobyte. Idź do miejsca: {FortressName}.";
            return $"Zniszcz trzy leża i zdobądź klucze do twierdzy ({LairsCleared} z {LairCount}).";
        }

        // Wejście na nowe pole mapy. Zwraca, co się stało, a przy zasadzce plan walki przez out.
        public TravelEvent Travel(int x, int y, out EncounterPlan ambush)
        {
            ambush = null;
            if (!CanWalk(x, y))
                return TravelEvent.Blocked;
            if (x == X && y == Y)
                return TravelEvent.None;

            X = x;
            Y = y;
            StepsTaken++;
            StepsSinceFight++;
            StepsSinceTreatment++;

            if (CurrentSite != null)
                return TravelEvent.EnteredSite;

            if (StepsSinceFight >= StepsBetweenAmbushes && Roller.Next(100) < AmbushPercent(Map[x, y]))
            {
                ambush = WildPlan();
                return TravelEvent.Ambush;
            }
            return TravelEvent.None;
        }

        private static int AmbushPercent(Biome biome)
        {
            switch (biome)
            {
                case Biome.Forest:
                    return 12;
                case Biome.Mountains:
                    return 15;
                case Biome.Settlement:
                    return 3;
                default:
                    return 8;
            }
        }

        private static Threat WildThreat(Biome biome)
        {
            switch (biome)
            {
                case Biome.Forest:
                    return Threat.Moderate;
                case Biome.Mountains:
                    return Threat.Severe;
                default:
                    return Threat.Low;
            }
        }

        private EncounterPlan WildPlan()
        {
            Biome biome = Map[X, Y];
            Threat threat = WildThreat(biome);
            List<Statblock> enemies = MonsterFactory.ForBudget(Roller, Hero.Level, threat, MonsterFactory.FamiliesFor(biome));
            return new EncounterPlan(enemies, threat, null);
        }

        public EncounterPlan SitePlan(Site site)
        {
            if (site.Kind == SiteKind.Lair)
            {
                var data = LairData.First(l => l.Name == site.Name);
                return new EncounterPlan(
                    new List<Statblock>
                    {
                        MonsterFactory.Create(data.Family, site.Level, true, data.Boss),
                        MonsterFactory.Create(data.Minion, site.Level - 3)
                    },
                    Threat.Severe,
                    site);
            }

            return new EncounterPlan(
                new List<Statblock>
                {
                    MonsterFactory.Create(MonsterFactory.Wraith, FortressLevel, true, FortressBoss),
                    MonsterFactory.Create(MonsterFactory.AshArcher, FortressLevel - 3)
                },
                Threat.Extreme,
                site);
        }

        public Encounter BuildEncounter(EncounterPlan plan)
        {
            Location location = LocationGenerator.Generate(Seed, X, Y, Map[X, Y]);
            List<Combatant> units = new() { new PlayerCombatant(Hero, HeroClass, location.PartySpawns[1]) };
            for (int i = 0; i < plan.Enemies.Count && i < location.EnemySpawns.Count; i++)
            {
                Statblock enemy = plan.Enemies[i];
                string name = plan.Enemies.Count(e => e.Name == enemy.Name) > 1 ? $"{enemy.Name} {i + 1}" : enemy.Name;
                units.Add(new MonsterCombatant(enemy, location.EnemySpawns[i], name));
            }
            return new Encounter(location.Grid, units) { ExperienceMultiplier = ExperienceMultiplier };
        }

        public VictoryReward ResolveVictory(EncounterPlan plan, int levelBeforeFight)
        {
            FightsWon++;
            StepsSinceFight = 0;

            int silver = Rewards.TreasureGp(levelBeforeFight, plan.Threat) * ShopCatalog.SilverPerGold * TreasureMultiplier;
            Hero.Silver += silver;

            Potion potion = null;
            if (Roller.Next(100) < PotionDropPercent)
            {
                potion = ShopCatalog.Potions.Last(p => p.ItemLevel <= Math.Max(Hero.Level, 1));
                Hero.Potions[potion.Name] = Hero.Potions.GetValueOrDefault(potion.Name) + 1;
            }

            int levels = Hero.Level - levelBeforeFight;
            for (int i = 0; i < levels; i++)
                Hero.Health.GainHeroPoint();

            if (plan.Site != null)
            {
                _cleared.Add((plan.Site.X, plan.Site.Y));
                if (plan.Site.Kind == SiteKind.Fortress)
                    Completed = true;
            }
            return new VictoryReward(silver, levels, potion, Completed);
        }

        // Hero Pointy za uniknięcie śmierci: bohater budzi się ranny w najbliższej osadzie
        public bool HeroicRecovery()
        {
            if (!Hero.Health.SpendAllHeroPointsToSurvive())
                return false;

            Defeats++;
            Hero.Health.IncreaseWounded();
            StepsSinceFight = 0;
            Site town = NearestTown();
            X = town.X;
            Y = town.Y;
            return true;
        }

        public Site NearestTown()
        {
            return _sites.Where(s => s.Kind == SiteKind.Town).OrderBy(s => Math.Abs(s.X - X) + Math.Abs(s.Y - Y)).First();
        }

        // Odpoczynek w osadzie jest bezpieczny, a kowal naprawia przy tym tarczę
        public void RestInTown()
        {
            Hero.Rest();
            if (Hero.HasShield)
                Hero.ShieldHp = ShopCatalog.ShieldMaxHp;
        }

        // Obóz w dziczy leczy tak samo, ale może przerwać go zasadzka. Wtedy odpoczynku nie ma, a zwraca się plan walki.
        public EncounterPlan Camp()
        {
            if (Roller.Next(100) < CampAmbushPercent)
                return WildPlan();
            Hero.Rest();
            return null;
        }

        public bool CanTreatWounds =>
            Hero.Health.CurrentHp < Hero.Health.MaxHp
            && StepsSinceTreatment >= StepsBetweenTreatments
            && Hero.GetSkillRank(SkillName.Medicine, HeroClass) != ProficiencyRank.Untrained;

        public HealingResult TreatWounds()
        {
            StepsSinceTreatment = 0;
            return Healing.TreatWounds(Hero, HeroClass);
        }

        // ---------- układ świata ----------

        private void PlaceSites()
        {
            Random random = new(Seed);
            List<(int X, int Y)> land = LargestLand(Map);
            (int X, int Y) start = land
                .OrderBy(c => Map[c.X, c.Y] == Biome.Settlement ? 0 : 1)
                .ThenBy(c => Math.Abs(c.X - Map.Width / 2) + Math.Abs(c.Y - Map.Height / 2))
                .First();
            Dictionary<(int X, int Y), int> distance = Distances(start);
            List<(int X, int Y)> ordered = distance.OrderBy(p => p.Value).ThenBy(p => p.Key.Y).ThenBy(p => p.Key.X).Select(p => p.Key).ToList();
            int farthest = distance[ordered[^1]];

            List<string> townNames = TownNames.OrderBy(_ => random.Next()).ToList();
            AddSite(SiteKind.Town, start, townNames[0], 0, "Bezpieczne miejsce: sklep i odpoczynek.");

            // Twierdza na końcu świata, leża po drodze do niej, coraz dalej
            AddSite(SiteKind.Fortress, ordered[^1], FortressName, FortressLevel, $"Siedziba: {FortressBoss}. Wejście wymaga trzech kluczy z leży. Zalecany poziom: {FortressLevel + 1}.");
            double[] lairDepth = { 0.3, 0.55, 0.8 };
            for (int i = 0; i < LairCount; i++)
            {
                var data = LairData[i];
                (int X, int Y) tile = FarEnough(ordered, distance, (int)(farthest * lairDepth[i]));
                AddSite(SiteKind.Lair, tile, data.Name, data.Level, $"{data.Boss} i obstawa. Zalecany poziom: {data.Level + 1}.");
            }

            for (int i = 1; i < TownCount; i++)
            {
                (int X, int Y) tile = ordered
                    .Where(c => Map[c.X, c.Y] == Biome.Settlement && IsFree(c))
                    .OrderBy(c => random.Next())
                    .DefaultIfEmpty(FarEnough(ordered, distance, farthest * i / TownCount))
                    .First();
                AddSite(SiteKind.Town, tile, townNames[i], 0, "Bezpieczne miejsce: sklep i odpoczynek.");
            }
        }

        private (int X, int Y) FarEnough(List<(int X, int Y)> ordered, Dictionary<(int X, int Y), int> distance, int wanted)
        {
            return ordered
                .Where(IsFree)
                .OrderBy(c => Math.Abs(distance[c] - wanted))
                .First();
        }

        private bool IsFree((int X, int Y) cell)
        {
            return _sites.All(s => Math.Abs(s.X - cell.X) + Math.Abs(s.Y - cell.Y) >= MinimumSiteSpacing);
        }

        private void AddSite(SiteKind kind, (int X, int Y) tile, string name, int level, string description)
        {
            _sites.Add(new Site(kind, tile.X, tile.Y, name, level, description));
        }

        public Dictionary<(int X, int Y), int> Distances((int X, int Y) from)
        {
            Dictionary<(int X, int Y), int> distance = new() { [from] = 0 };
            Queue<(int X, int Y)> queue = new();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                (int X, int Y) cell = queue.Dequeue();
                foreach ((int X, int Y) next in Neighbors(cell))
                {
                    if (CanWalk(next.X, next.Y) && !distance.ContainsKey(next))
                    {
                        distance[next] = distance[cell] + 1;
                        queue.Enqueue(next);
                    }
                }
            }
            return distance;
        }

        private static IEnumerable<(int X, int Y)> Neighbors((int X, int Y) cell)
        {
            yield return (cell.X + 1, cell.Y);
            yield return (cell.X - 1, cell.Y);
            yield return (cell.X, cell.Y + 1);
            yield return (cell.X, cell.Y - 1);
        }

        private static List<(int X, int Y)> LargestLand(WorldMap map)
        {
            HashSet<(int X, int Y)> seen = new();
            List<(int X, int Y)> best = new();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!WorldMap.IsVisitable(map[x, y]) || seen.Contains((x, y)))
                        continue;

                    List<(int X, int Y)> region = new();
                    Queue<(int X, int Y)> queue = new();
                    queue.Enqueue((x, y));
                    seen.Add((x, y));
                    while (queue.Count > 0)
                    {
                        (int X, int Y) cell = queue.Dequeue();
                        region.Add(cell);
                        foreach ((int X, int Y) next in Neighbors(cell))
                        {
                            bool inside = next.X >= 0 && next.Y >= 0 && next.X < map.Width && next.Y < map.Height;
                            if (inside && WorldMap.IsVisitable(map[next.X, next.Y]) && seen.Add(next))
                                queue.Enqueue(next);
                        }
                    }
                    if (region.Count > best.Count)
                        best = region;
                }
            }
            return best;
        }

        // ---------- zapis ----------

        public CampaignSave ToSave()
        {
            return new CampaignSave(
                Seed, CharacterRepository.ToSave(Hero), X, Y,
                _cleared.Select(c => new[] { c.X, c.Y }).ToList(),
                FightsWon, Defeats, StepsTaken, StepsSinceFight, StepsSinceTreatment, Completed);
        }

        public static Campaign FromSave(CampaignSave save)
        {
            Campaign campaign = new(save.Seed, CharacterRepository.FromSave(save.Hero))
            {
                X = save.X,
                Y = save.Y,
                FightsWon = save.FightsWon,
                Defeats = save.Defeats,
                StepsTaken = save.StepsTaken,
                StepsSinceFight = save.StepsSinceFight,
                StepsSinceTreatment = save.StepsSinceTreatment,
                Completed = save.Completed
            };
            foreach (int[] cell in save.Cleared ?? new List<int[]>())
                campaign._cleared.Add((cell[0], cell[1]));
            return campaign;
        }
    }

    public record CampaignSave(
        int Seed,
        CharacterSave Hero,
        int X,
        int Y,
        List<int[]> Cleared,
        int FightsWon,
        int Defeats,
        int StepsTaken,
        int StepsSinceFight,
        int StepsSinceTreatment,
        bool Completed);
}
