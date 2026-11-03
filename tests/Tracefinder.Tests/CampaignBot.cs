using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Tracefinder.World;

namespace Tracefinder.Tests
{
    // Prosty gracz: kupuje sprzęt, rozwija postać, chodzi po mapie, walczy, a po porażce wczytuje autozapis jak człowiek
    public class CampaignBot
    {
        private const int MaxTurnsPerFight = 400;
        private const int MaxSteps = 20000;
        private const int MaxFights = 400;

        private readonly CampaignRepository _repository;
        private readonly string _file;
        private readonly int _seed;
        private int _steps;
        private int _extraLevels;
        private int _losses;

        public Campaign Campaign { get; private set; }
        public int Fights { get; private set; }
        public int Reloads { get; private set; }
        public int Rescues { get; private set; }

        public CampaignBot(string className, int seed)
        {
            _seed = seed;
            _file = Path.Combine(Path.GetTempPath(), "tracefinder-bot-" + Guid.NewGuid() + ".json");
            _repository = new CampaignRepository(_file);
            Dice.SetSeed(seed);
            Campaign = Campaign.New(seed * 101, MakeHero(className));
            Campaign.Roller = new Random(seed);
        }

        public string Summary()
        {
            CharacterCore h = Campaign.Hero;
            return $"{h.ClassID} ziarno {_seed}: ukończono {Campaign.Completed}, poziom {h.Level}, walki {Fights}, wczytania {Reloads}, ocalenia {Rescues}, kroki {_steps}, leża {Campaign.LairsCleared}, srebro {h.Silver}, broń +{h.EquippedWeapon?.PotencyRune}, pancerz {h.EquippedArmor?.Name}";
        }

        private static CharacterCore MakeHero(string className)
        {
            ClassStats cls = ClassList.ByName(className);
            bool wizard = className == "Wizard";
            bool cleric = className == "Cleric";
            bool rogue = className == "Rogue";
            CharacterCore core = new()
            {
                Name = "Bot",
                ClassID = className,
                SelectedKeyAbility = cls.KeyAbilityOptions[0],
                Ancestry = AncestryList.Human,
                Strength = wizard || rogue ? 10 : 18,
                Dexterity = rogue ? 18 : 14,
                Constitution = 14,
                Intelligence = wizard ? 18 : 10,
                Wisdom = cleric ? 18 : 12,
                Charisma = 10
            };
            Equipment.Weapon weapon = rogue ? WeaponList.Rapier : wizard ? WeaponList.Dagger : WeaponList.Longsword;
            core.EquippedWeapon = new CarriedWeapon(weapon);
            core.Inventory.Add(core.EquippedWeapon);
            core.InitializeHealth(cls);
            return core;
        }

        public void Play()
        {
            VisitTown();
            _repository.Save(Campaign);
            try
            {
                while (!Campaign.Completed && _steps < MaxSteps && Fights < MaxFights)
                    PlayStep();
            }
            finally
            {
                if (File.Exists(_file))
                    File.Delete(_file);
            }
        }

        private void PlayStep()
        {
            Spend();
            Site goal = NextGoal();
            if (Campaign.CurrentSite == goal)
            {
                if (goal.Kind == SiteKind.Town)
                {
                    VisitTown();
                    Farm();
                }
                else if (Prepare())
                {
                    Fight(Campaign.SitePlan(goal));
                }
                return;
            }
            WalkOneStepTo(goal);
        }

        // Przed walką z bossem rozbija obóz, aż odzyska siły. Zwraca false, gdy obóz przerwała walka.
        private bool Prepare()
        {
            CharacterCore h = Campaign.Hero;
            if (h.Health.CurrentHp == h.Health.MaxHp && h.SpellSlotsUsed.Count == 0 && h.FontSlotsUsed == 0)
                return true;

            EncounterPlan ambush = Campaign.Camp();
            _steps++;
            if (ambush == null)
                return true;
            Fight(ambush);
            return false;
        }

        // Do miasta, gdy ranny; do leża, gdy gotowy; inaczej poluje blisko miasta
        private Site NextGoal()
        {
            CharacterCore h = Campaign.Hero;
            Site target = Campaign.Sites
                .Where(s => s.Kind == SiteKind.Lair && !Campaign.IsCleared(s))
                .OrderBy(s => s.Level)
                .FirstOrDefault()
                ?? Campaign.Sites.First(s => s.Kind == SiteKind.Fortress);

            bool hurt = h.Health.CurrentHp < h.Health.MaxHp * 0.6;
            bool ready = h.Level >= RequiredLevel(target);
            if (hurt || !ready)
                return Campaign.NearestTown();
            return target;
        }

        // Rzucający zaklęcia mają mało HP, więc wchodzą do leża poziom później
        private int RequiredLevel(Site target)
        {
            bool caster = SpellList.For(Campaign.HeroClass).Count > 0;
            return target.Level + 1 + (caster ? 1 : 0) + _extraLevels;
        }

        // Przechadzka wokół miasta, żeby trafić na zasadzkę i zdobyć poziom
        private void Farm()
        {
            CharacterCore h = Campaign.Hero;
            Site target = Campaign.Sites.Where(s => s.Kind != SiteKind.Town && !Campaign.IsCleared(s)).OrderBy(s => s.Level).First();
            if (h.Level >= RequiredLevel(target))
                return;

            var neighbors = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            Random random = new(_steps + _seed);
            for (int i = 0; i < 12; i++)
            {
                (int dx, int dy) = neighbors[random.Next(4)];
                if (Campaign.CanWalk(Campaign.X + dx, Campaign.Y + dy))
                    Move(Campaign.X + dx, Campaign.Y + dy);
                if (h.Health.CurrentHp < h.Health.MaxHp * 0.6)
                    return;
            }
        }

        private void WalkOneStepTo(Site goal)
        {
            Dictionary<(int X, int Y), int> distance = Campaign.Distances((goal.X, goal.Y));
            var neighbors = new[] { (Campaign.X + 1, Campaign.Y), (Campaign.X - 1, Campaign.Y), (Campaign.X, Campaign.Y + 1), (Campaign.X, Campaign.Y - 1) };
            (int X, int Y) next = neighbors
                .Where(c => distance.ContainsKey(c))
                .OrderBy(c => distance[c])
                .First();
            Move(next.X, next.Y);
        }

        private void Move(int x, int y)
        {
            _steps++;
            TravelEvent happened = Campaign.Travel(x, y, out EncounterPlan ambush);
            if (happened == TravelEvent.Ambush)
                Fight(ambush);
        }

        private void Fight(EncounterPlan plan)
        {
            _repository.Save(Campaign);
            Fights++;
            int levelBefore = Campaign.Hero.Level;
            Encounter encounter = Campaign.BuildEncounter(plan);
            encounter.Start();
            int turns = 0;
            while (encounter.State == EncounterState.InProgress && turns++ < MaxTurnsPerFight)
            {
                if (encounter.Current is PlayerCombatant player)
                    PlayerTurn(encounter, player);
                else
                    EnemyAi.PlayTurn(encounter);
            }

            if (encounter.Winner == Side.Party)
            {
                if (plan.Site != null)
                    _losses = 0;
                Campaign.ResolveVictory(plan, levelBefore);
                Spend();
                _repository.Save(Campaign);
                return;
            }

            // Człowiek po kilku porażkach z bossem też poszedłby najpierw zdobyć poziom
            if (plan.Site != null && ++_losses % 3 == 0)
                _extraLevels++;

            if (Campaign.Hero.Health.HeroPoints > 0)
            {
                Rescues++;
                Campaign.HeroicRecovery();
                _repository.Save(Campaign);
            }
            else
            {
                Reloads++;
                Campaign = _repository.Load();
                Campaign.Roller = new Random(_seed + Fights);
            }
        }

        private void PlayerTurn(Encounter e, PlayerCombatant player)
        {
            int guard = 0;
            while (e.Current == player && e.State == EncounterState.InProgress && player.Actions.MainActions > 0 && guard++ < 6)
            {
                CharacterCore h = player.Core;
                Potion potion = ShopCatalog.Potions.LastOrDefault(p => h.Potions.GetValueOrDefault(p.Name) > 0);
                if (h.Health.CurrentHp < h.Health.MaxHp * 0.4)
                {
                    if (player.ClassStats.Name == "Cleric" && player.Actions.MainActions >= 2 && h.CastingRank(SpellList.Heal, player.ClassStats) > 0)
                    {
                        if (e.CastSpell(SpellList.Heal, player).Success)
                            continue;
                    }
                    if (potion != null && e.DrinkPotion(potion).Success)
                        continue;
                }

                if (TryCast(e, player))
                    continue;
                if (SpellList.For(player.ClassStats).Count > 0 && Backstep(e, player))
                    continue;
                if (!EnemyAi.TakeAction(e))
                    break;
            }
            if (e.Current == player && e.State == EncounterState.InProgress)
                e.EndTurn();
        }

        // Rzucający, któremu została jedna akcja, odsuwa się od najbliższego wroga zamiast iść w jego stronę
        private static bool Backstep(Encounter e, PlayerCombatant player)
        {
            Combatant foe = e.Combatants.Where(c => c.Side != player.Side && !c.IsOut).OrderBy(c => BattleGrid.DistanceFeet(c.Position, player.Position)).FirstOrDefault();
            if (foe == null || BattleGrid.DistanceFeet(foe.Position, player.Position) > 30 || player.Actions.MainActions >= 2)
                return false;

            GridPos[] options = e.StepTargets().OrderByDescending(p => BattleGrid.DistanceFeet(p, foe.Position)).ToArray();
            return options.Length > 0 && BattleGrid.DistanceFeet(options[0], foe.Position) > BattleGrid.DistanceFeet(player.Position, foe.Position) && e.Step(options[0]).Success;
        }

        private static bool TryCast(Encounter e, PlayerCombatant player)
        {
            IReadOnlyList<Spell> spells = SpellList.For(player.ClassStats);
            if (spells.Count == 0 || player.Actions.MainActions < 2)
                return false;

            Combatant target = e.Combatants.Where(c => c.Side != player.Side && !c.IsOut).OrderBy(c => BattleGrid.DistanceFeet(c.Position, player.Position)).FirstOrDefault();
            if (target == null)
                return false;

            foreach (Spell spell in spells.Where(s => s.Effect != SpellEffect.Heal).OrderByDescending(s => s.BaseRank))
            {
                if (spell.BurstFeet > 0 && BattleGrid.DistanceFeet(target.Position, player.Position) <= spell.BurstFeet)
                    continue;
                if (e.SpellTargetRefusal(spell, target) == null && player.Core.CastingRank(spell, player.ClassStats) > 0)
                    return e.CastSpell(spell, target).Success;
            }
            return false;
        }

        private void VisitTown()
        {
            if (Campaign.CurrentSite?.Kind == SiteKind.Town)
                Campaign.RestInTown();
            Shopping();
        }

        private void Shopping()
        {
            if (Campaign.CurrentSite?.Kind != SiteKind.Town)
                return;

            CharacterCore h = Campaign.Hero;
            ClassStats cls = Campaign.HeroClass;
            Armor best = ShopCatalog.Armors
                .Where(a => cls.ArmorRankAt(a.Category, h.Level) != ProficiencyRank.Untrained && Shop.IsAvailable(a.ItemLevel, h) && a.PriceSp <= h.Silver)
                .OrderByDescending(a => a.AcBonus + Math.Min(a.DexCap, h.AbilityModifier(AbilityScore.Dexterity)))
                .FirstOrDefault();
            if (best != null && (h.EquippedArmor == null || Score(best, h) > Score(h.EquippedArmor, h)))
                Shop.BuyArmor(h, best);

            Shop.BuyPotencyRune(h, h.EquippedWeapon);
            Shop.BuyStrikingRune(h, h.EquippedWeapon);

            Potion potion = ShopCatalog.Potions.LastOrDefault(p => Shop.IsAvailable(p.ItemLevel, h));
            while (potion != null && h.Potions.GetValueOrDefault(potion.Name) < 3 && h.Silver >= potion.PriceSp)
                Shop.BuyPotion(h, potion);
        }

        private static int Score(Armor armor, CharacterCore h)
        {
            return armor.AcBonus + Math.Min(armor.DexCap, h.AbilityModifier(AbilityScore.Dexterity));
        }

        private void Spend()
        {
            CharacterCore h = Campaign.Hero;
            ClassStats cls = Campaign.HeroClass;
            foreach (FeatCategory category in Enum.GetValues<FeatCategory>())
            {
                while (LevelUp.AvailableFeatSlots(h, cls, category) > 0 && LevelUp.AvailableFeats(h, cls, category).Count > 0)
                    LevelUp.TakeFeat(h, cls, LevelUp.AvailableFeats(h, cls, category)[0]);
            }
            foreach (AbilityScore ability in new[] { h.SelectedKeyAbility, AbilityScore.Constitution, AbilityScore.Dexterity, AbilityScore.Strength, AbilityScore.Wisdom, AbilityScore.Intelligence, AbilityScore.Charisma })
                LevelUp.Boost(h, cls, ability);
            while (LevelUp.AvailableSkillIncreases(h, cls) > 0 && LevelUp.IncreaseSkill(h, cls, SkillName.Medicine))
            {
            }
            foreach (SkillName skill in SkillCatalog.All)
            {
                if (LevelUp.AvailableSkillIncreases(h, cls) == 0)
                    break;
                LevelUp.IncreaseSkill(h, cls, skill);
            }
        }
    }
}
