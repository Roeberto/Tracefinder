using System;
using System.IO;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Tracefinder.World;
using Xunit;

namespace Tracefinder.Tests
{
    public class WorldTests
    {
        [Fact]
        public void SameSeedGivesSameWorld()
        {
            WorldMap a = WorldMap.Generate(99);
            WorldMap b = WorldMap.Generate(99);
            for (int y = 0; y < a.Height; y++)
                for (int x = 0; x < a.Width; x++)
                    Assert.Equal(a[x, y], b[x, y]);
        }

        [Fact]
        public void DifferentSeedsGiveDifferentWorlds()
        {
            WorldMap a = WorldMap.Generate(1);
            WorldMap b = WorldMap.Generate(2);
            int same = 0;
            for (int y = 0; y < a.Height; y++)
                for (int x = 0; x < a.Width; x++)
                    if (a[x, y] == b[x, y])
                        same++;
            Assert.True(same < a.Width * a.Height);
        }

        [Fact]
        public void WorldsContainVariedBiomes()
        {
            int[] counts = new int[Enum.GetValues<Biome>().Length];
            foreach (int seed in new[] { 1, 2, 3, 4, 5, 2026 })
            {
                WorldMap map = WorldMap.Generate(seed);
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                        counts[(int)map[x, y]]++;
            }
            Assert.All(counts, c => Assert.True(c > 0));
        }

        [Fact]
        public void SameCellGivesSameLocation()
        {
            Location a = LocationGenerator.Generate(5, 3, 4, Biome.Forest);
            Location b = LocationGenerator.Generate(5, 3, 4, Biome.Forest);
            Assert.Equal(a.Grid.AllCells().Select(p => a.Grid[p]), b.Grid.AllCells().Select(p => b.Grid[p]));
        }

        [Fact]
        public void DifferentCellsGiveDifferentLocations()
        {
            Location a = LocationGenerator.Generate(5, 3, 4, Biome.Forest);
            Location b = LocationGenerator.Generate(5, 4, 4, Biome.Forest);
            Assert.NotEqual(a.Grid.AllCells().Select(p => a.Grid[p]), b.Grid.AllCells().Select(p => b.Grid[p]));
        }

        [Theory]
        [InlineData(Biome.Plains)]
        [InlineData(Biome.Forest)]
        [InlineData(Biome.Mountains)]
        [InlineData(Biome.Settlement)]
        [InlineData(Biome.Water)]
        public void EveryLocationConnectsSpawnsAcrossManyCells(Biome biome)
        {
            for (int x = 0; x < 40; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    Location l = LocationGenerator.Generate(x * 31 + 7, x, y, biome);
                    GridPos party = l.PartySpawns[0];
                    Assert.All(l.PartySpawns, p => Assert.True(l.Grid.IsPassable(p)));
                    Assert.All(l.EnemySpawns, p => Assert.True(l.Grid.IsPassable(p)));
                    Assert.All(l.EnemySpawns, p => Assert.True(LocationGenerator.Reaches(l.Grid, party, p), $"brak przejścia {biome} ({x},{y})"));
                }
            }
        }

        [Fact]
        public void BiomesLookDifferent()
        {
            int Count(Biome biome, TerrainType terrain)
            {
                Location l = LocationGenerator.Generate(11, 2, 2, biome);
                return l.Grid.AllCells().Count(p => l.Grid[p] == terrain);
            }
            Assert.True(Count(Biome.Forest, TerrainType.Pillar) > Count(Biome.Plains, TerrainType.Pillar));
            Assert.True(Count(Biome.Mountains, TerrainType.Wall) > 0);
            Assert.True(Count(Biome.Settlement, TerrainType.Wall) > 0);
            Assert.True(Count(Biome.Water, TerrainType.Water) > 0);
        }

        [Fact]
        public void SessionBuildsPlayableEncounterFromLocation()
        {
            Dice.SetSeed(21);
            Location l = LocationGenerator.Generate(1, 1, 1, Biome.Plains);
            CharacterCore hero = GameSession.ExampleCharacter();
            Encounter e = GameSession.CreateEncounter(l, hero, ClassList.Fighter);
            e.Start();
            Assert.Equal(EncounterState.InProgress, e.State);
            Assert.True(e.Combatants.Count(c => c.Side == Side.Enemies) >= 2);
            Assert.NotEmpty(e.Combatants.First(c => c.Side == Side.Party).Strikes);
        }

        private static string TempDir() => Path.Combine(Path.GetTempPath(), "tracefinder-tests-" + Guid.NewGuid());

        [Fact]
        public void CharacterRoundTripsThroughJson()
        {
            string dir = TempDir();
            try
            {
                CharacterCore hero = GameSession.ExampleCharacter();
                hero.Name = "Żółta Amber";
                hero.ExtraTrainedSkills.Add(SkillName.Stealth);
                hero.Feats.Add(FeatList.All[0]);
                hero.Experience = 120;

                CharacterRepository repo = new(dir);
                repo.Save(hero);
                Assert.Equal(new[] { "Żółta Amber" }, repo.ListNames());

                CharacterCore loaded = repo.Load("Żółta Amber");
                Assert.Equal(18, loaded.Strength);
                Assert.Equal("Fighter", loaded.ClassID);
                Assert.Same(AncestryList.Human, loaded.Ancestry);
                Assert.Equal("Longsword", loaded.EquippedWeapon.Name);
                Assert.Contains(SkillName.Stealth, loaded.ExtraTrainedSkills);
                Assert.Equal(FeatList.All[0].Name, loaded.Feats[0].Name);
                Assert.Equal(120, loaded.Experience);
                Assert.Equal(hero.Health.MaxHp, loaded.Health.CurrentHp);
            }
            finally
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void RepositoryKeepsNamesInsideItsDirectory()
        {
            Assert.Equal("etcpasswd", CharacterRepository.FileNameFor("../../etc/passwd"));
            string dir = TempDir();
            CharacterRepository repo = new(dir);
            Assert.Throws<ArgumentException>(() => repo.Save(new CharacterCore { Name = "../.." }));
            Assert.Null(repo.Load("../../etc/passwd"));
            Assert.Empty(repo.ListNames());
        }

        [Fact]
        public void CorruptSaveLoadsAsNull()
        {
            string dir = TempDir();
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "zla.json"), "{ to nie jest json");
                Assert.Null(new CharacterRepository(dir).Load("zla"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
