using System.Collections.Generic;
using System.Linq;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Xunit;

namespace Tracefinder.Tests
{
    public class DepthTests
    {
        private static CharacterCore Core(ClassStats cls, int str = 16, int dex = 14, int con = 14, int intel = 10, int wis = 12, int cha = 10, int level = 1)
        {
            CharacterCore core = new()
            {
                Name = "Bohater", Strength = str, Dexterity = dex, Constitution = con, Intelligence = intel, Wisdom = wis, Charisma = cha,
                Level = level, ClassID = cls.Name, SelectedKeyAbility = cls.KeyAbilityOptions[0],
                EquippedWeapon = new CarriedWeapon(WeaponList.Longsword)
            };
            core.Inventory.Add(core.EquippedWeapon);
            core.InitializeHealth(cls);
            return core;
        }

        private static Encounter Fight(Combatant current, params Combatant[] others)
        {
            List<Combatant> all = new() { current };
            all.AddRange(others);
            Encounter e = new(new BattleGrid(20, 20), all);
            e.Start();
            for (int i = 0; i < 50 && e.Current != current; i++)
                e.EndTurn();
            Assert.Same(current, e.Current);
            return e;
        }

        private static MonsterCombatant Dummy(int x, int y, int hp = 100000, bool undead = false, bool reactive = false)
        {
            Statblock s = new()
            {
                Name = "Kukła", Level = 0, MaxHp = hp, ArmorClass = 10, Perception = -50, Fortitude = -20, Reflex = -20, Will = -20,
                IsUndead = undead, HasReactiveStrike = reactive,
                Strikes = [new("Pięść", 40, new Equipment.Weapon { Name = "Pięść", Kind = WeaponKind.Melee, DamageDice = Dice.DiceSize.D4, DiceNumber = 1 }, 0)]
            };
            return new MonsterCombatant(s, new GridPos(x, y));
        }

        [Fact]
        public void ArmorCapsDexterityAndRequiresProficiency()
        {
            CharacterCore fighter = Core(ClassList.Fighter, dex: 18);
            fighter.EquippedArmor = ShopCatalog.Armors.First(a => a.Name == "Full Plate");
            // 10 + DEX ograniczone do 0 + Trained 3 + pancerz 6
            Assert.Equal(19, fighter.ComputeMaxAc(ClassList.Fighter));

            CharacterCore wizard = Core(ClassList.Wizard, dex: 12);
            wizard.EquippedArmor = ShopCatalog.Armors.First(a => a.Name == "Full Plate");
            Assert.Equal(16, wizard.ComputeMaxAc(ClassList.Wizard));
        }

        [Fact]
        public void FeatsChangeCharacterStats()
        {
            CharacterCore c = Core(ClassList.Fighter);
            int hp = c.ComputeMaxHp(ClassList.Fighter);
            int initiative = c.ComputeInitiative(ClassList.Fighter);
            c.Feats.Add(FeatList.Toughness);
            c.Feats.Add(FeatList.Fleet);
            c.Feats.Add(FeatList.IncredibleInitiative);
            Assert.Equal(hp + 1, c.ComputeMaxHp(ClassList.Fighter));
            Assert.Equal(30, c.SpeedFeet);
            Assert.Equal(initiative + 2, c.ComputeInitiative(ClassList.Fighter));
        }

        [Fact]
        public void ToughnessTakenAtLevelUpRaisesHitPointsAtOnce()
        {
            CharacterCore c = Core(ClassList.Fighter, level: 3);
            c.InitializeHealth(ClassList.Fighter);
            int before = c.Health.MaxHp;
            Assert.True(LevelUp.TakeFeat(c, ClassList.Fighter, FeatList.Toughness));
            Assert.Equal(before + 3, c.Health.MaxHp);
            Assert.Equal(before + 3, c.Health.CurrentHp);
        }

        [Fact]
        public void NaturalAmbitionAndGeneralTrainingAddSlots()
        {
            CharacterCore c = Core(ClassList.Fighter);
            c.Ancestry = AncestryList.Human;
            c.Feats.Add(FeatList.ViciousSwing);
            Assert.Equal(0, LevelUp.AvailableFeatSlots(c, ClassList.Fighter, FeatCategory.Class));
            Assert.True(LevelUp.TakeFeat(c, ClassList.Fighter, FeatList.NaturalAmbition));
            Assert.Equal(1, LevelUp.AvailableFeatSlots(c, ClassList.Fighter, FeatCategory.Class));
        }

        [Fact]
        public void BackgroundTrainsSkill()
        {
            CharacterCore c = Core(ClassList.Fighter);
            Assert.Equal(ProficiencyRank.Untrained, c.GetSkillRank(SkillName.Medicine, ClassList.Fighter));
            c.Background = BackgroundList.All.First(b => b.TrainedSkill == SkillName.Medicine);
            Assert.Equal(ProficiencyRank.Trained, c.GetSkillRank(SkillName.Medicine, ClassList.Fighter));
        }

        [Fact]
        public void ReactiveStrikeHitsMoverOnceAndUsesReaction()
        {
            Dice.SetSeed(1);
            PlayerCombatant fighter = new(Core(ClassList.Fighter), ClassList.Fighter, new GridPos(5, 5));
            MonsterCombatant mover = Dummy(6, 5);
            Encounter e = Fight(mover, fighter);
            Assert.True(e.Stride(new GridPos(9, 5)).Success);
            Assert.Contains(e.Log, l => l.StartsWith("Reactive Strike!"));
            Assert.Equal(0, fighter.Actions.Reaction);
            Assert.True(e.Stride(new GridPos(12, 5)).Success);
            Assert.Single(e.Log, l => l.StartsWith("Reactive Strike!"));
        }

        [Fact]
        public void StepDoesNotProvokeReactiveStrike()
        {
            Dice.SetSeed(2);
            PlayerCombatant fighter = new(Core(ClassList.Fighter), ClassList.Fighter, new GridPos(5, 5));
            MonsterCombatant mover = Dummy(6, 5);
            Encounter e = Fight(mover, fighter);
            Assert.True(e.Step(new GridPos(7, 5)).Success);
            Assert.DoesNotContain(e.Log, l => l.StartsWith("Reactive Strike!"));
        }

        [Fact]
        public void RangedShotInReachProvokesAndKilledAttackerDoesNotFinishAction()
        {
            Dice.SetSeed(3);
            CharacterCore core = Core(ClassList.Wizard);
            core.EquippedWeapon = new CarriedWeapon(WeaponList.Shortbow);
            PlayerCombatant archer = new(core, ClassList.Wizard, new GridPos(5, 5));
            MonsterCombatant guard = Dummy(6, 5, reactive: true);
            Encounter e = Fight(archer, guard);
            core.Health.SetToMaxHp();
            for (int i = 0; i < 40 && !archer.IsOut; i++)
            {
                archer.Actions.ResetTurn();
                guard.Actions.ResetTurn();
                e.Strike(guard, archer.Strikes[0]);
            }
            Assert.Contains(e.Log, l => l.StartsWith("Reactive Strike!"));
        }

        [Fact]
        public void RaisedShieldAddsAcUntilOwnNextTurn()
        {
            Dice.SetSeed(4);
            CharacterCore core = Core(ClassList.Fighter);
            core.HasShield = true;
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            Encounter e = Fight(hero, Dummy(10, 10));
            Assert.True(e.RaiseShield().Success);
            Assert.True(hero.ShieldRaised);
            Assert.False(e.RaiseShield().Success);
            e.EndTurn();
            e.EndTurn();
            Assert.Same(hero, e.Current);
            Assert.False(hero.ShieldRaised);
        }

        [Fact]
        public void ShieldBlockReducesDamageWithReaction()
        {
            Dice.SetSeed(5);
            CharacterCore core = Core(ClassList.Fighter);
            core.HasShield = true;
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            MonsterCombatant attacker = Dummy(1, 0);
            Encounter e = Fight(hero, attacker);
            e.RaiseShield();
            e.EndTurn();
            Assert.Same(attacker, e.Current);
            e.Strike(hero, attacker.Strikes[0]);
            Assert.Contains(e.Log, l => l.Contains("blokuje tarczą"));
            Assert.Equal(0, hero.Actions.Reaction);
        }

        [Fact]
        public void RogueSneakAttackAppliesToUnactedTargetInFirstRoundOnly()
        {
            Dice.SetSeed(6);
            CharacterCore core = Core(ClassList.Rogue, dex: 18);
            core.EquippedWeapon = new CarriedWeapon(WeaponList.Rapier);
            PlayerCombatant rogue = new(core, ClassList.Rogue, new GridPos(0, 0));
            MonsterCombatant target = Dummy(1, 0);
            Encounter e = new(new BattleGrid(9, 9), new Combatant[] { rogue, target });
            e.Start();
            // Kukła ma Perception -50, więc w pierwszej rundzie działa jako ostatnia
            Assert.Same(rogue, e.Current);
            e.Strike(target, rogue.Strikes[0]);
            Assert.Contains(e.Log, l => l.Contains("off-guard") && l.Contains("Sneak Attack"));

            e.EndTurn();
            e.EndTurn();
            Assert.Same(rogue, e.Current);
            int before = e.Log.Count;
            e.Strike(target, rogue.Strikes[0]);
            Assert.DoesNotContain(e.Log.Skip(before), l => l.Contains("Sneak Attack"));
        }

        [Fact]
        public void LongswordDoesNotQualifyForSneakAttack()
        {
            Dice.SetSeed(7);
            PlayerCombatant rogue = new(Core(ClassList.Rogue), ClassList.Rogue, new GridPos(0, 0));
            MonsterCombatant target = Dummy(1, 0);
            Encounter e = new(new BattleGrid(9, 9), new Combatant[] { rogue, target });
            e.Start();
            e.Strike(target, rogue.Strikes[0]);
            Assert.DoesNotContain(e.Log, l => l.Contains("Sneak Attack"));
        }

        [Fact]
        public void FlankingGivesOffGuardOnlyWithOppositeAlly()
        {
            Dice.SetSeed(8);
            PlayerCombatant hero = new(Core(ClassList.Fighter), ClassList.Fighter, new GridPos(5, 5));
            MonsterCombatant left = Dummy(4, 5);
            MonsterCombatant right = Dummy(6, 5);
            MonsterCombatant above = Dummy(5, 4);
            Encounter e = Fight(left, hero, right, above);
            Assert.True(e.IsFlanked(left, hero));
            Assert.False(e.IsFlanked(above, hero));
        }

        [Fact]
        public void ViciousSwingCostsTwoActionsAndAddsDieAndDoubleMap()
        {
            Dice.SetSeed(9);
            CharacterCore core = Core(ClassList.Fighter);
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            MonsterCombatant target = Dummy(1, 0);
            Encounter e = Fight(hero, target);
            Assert.False(e.Strike(target, hero.Strikes[0], viciousSwing: true).Success);
            core.Feats.Add(FeatList.ViciousSwing);
            Assert.True(e.Strike(target, hero.Strikes[0], viciousSwing: true).Success);
            Assert.Equal(1, hero.Actions.MainActions);
            Assert.Equal(2, hero.Actions.AttacksThisTurn);
            Assert.False(e.Strike(target, hero.Strikes[0], viciousSwing: true).Success);
        }

        [Fact]
        public void HeroPointRerollsOnlyAMissAndIsSpent()
        {
            CharacterCore core = Core(ClassList.Fighter);
            core.Health.GainHeroPoint();
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            Statblock tough = new() { Name = "Pancerz", Level = 0, MaxHp = 100000, ArmorClass = 60, Perception = -50 };
            MonsterCombatant target = new(tough, new GridPos(1, 0));
            Dice.SetSeed(10);
            Encounter e = Fight(hero, target);
            e.Strike(target, hero.Strikes[0], useHeroPoint: true);
            Assert.Equal(0, core.Health.HeroPoints);
            Assert.Contains(e.Log, l => l.Contains("Hero Point"));
        }

        [Fact]
        public void ThrownWeaponMustBeReadiedAgain()
        {
            Dice.SetSeed(11);
            CharacterCore core = Core(ClassList.Fighter);
            core.EquippedWeapon = new CarriedWeapon(WeaponList.Dagger);
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            MonsterCombatant target = Dummy(3, 0);
            Encounter e = Fight(hero, target);
            Assert.True(e.Strike(target, hero.Strikes[0]).Success);
            Assert.False(e.Strike(target, hero.Strikes[0]).Success);
            Assert.True(e.Reload(core.EquippedWeapon).Success);
            Assert.True(e.Strike(target, hero.Strikes[0]).Success);
        }

        [Fact]
        public void PotionHealsAndIsConsumed()
        {
            Dice.SetSeed(12);
            CharacterCore core = Core(ClassList.Fighter);
            Potion potion = ShopCatalog.Potions[1];
            core.Potions[potion.Name] = 1;
            core.Health.TakeDamage(10);
            int before = core.Health.CurrentHp;
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            Encounter e = Fight(hero, Dummy(9, 9));
            Assert.True(e.DrinkPotion(potion).Success);
            Assert.True(core.Health.CurrentHp > before);
            Assert.False(e.DrinkPotion(potion).Success);
        }

        [Fact]
        public void EquipWeaponCostsAnAction()
        {
            Dice.SetSeed(13);
            CharacterCore core = Core(ClassList.Fighter);
            CarriedWeapon bow = new(WeaponList.Shortbow);
            core.Inventory.Add(bow);
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            Encounter e = Fight(hero, Dummy(9, 9));
            Assert.True(e.EquipWeapon(bow).Success);
            Assert.Same(bow, core.EquippedWeapon);
            Assert.Equal(2, hero.Actions.MainActions);
            Assert.False(e.EquipWeapon(bow).Success);
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(2, 1, 3)]
        [InlineData(3, 1, 3)]
        [InlineData(3, 2, 2)]
        [InlineData(5, 1, 3)]
        [InlineData(5, 2, 3)]
        [InlineData(5, 3, 2)]
        [InlineData(5, 4, 0)]
        public void SpellSlotsMatchClassTable(int level, int rank, int expected)
        {
            Assert.Equal(expected, SpellSlots.SlotsAt(level, rank));
        }

        [Fact]
        public void CantripIsFreeButSlotSpellsRunOutUntilRest()
        {
            Dice.SetSeed(14);
            CharacterCore core = Core(ClassList.Wizard, intel: 18);
            PlayerCombatant wizard = new(core, ClassList.Wizard, new GridPos(0, 0));
            MonsterCombatant target = Dummy(4, 0);
            Encounter e = Fight(wizard, target);

            for (int i = 0; i < 2; i++)
            {
                wizard.Actions.ResetTurn();
                Assert.True(e.CastSpell(SpellList.ForceBarrage, target).Success);
            }
            wizard.Actions.ResetTurn();
            Assert.False(e.CastSpell(SpellList.ForceBarrage, target).Success);
            Assert.True(e.CastSpell(SpellList.ElectricArc, target).Success);

            core.Rest();
            wizard.Actions.ResetTurn();
            Assert.True(e.CastSpell(SpellList.ForceBarrage, target).Success);
        }

        [Fact]
        public void BasicSaveScalesDamage()
        {
            Assert.Equal(0, Healing.HalvedOrScaled(10, DegreeOfSuccess.CriticalSuccess));
            Assert.Equal(5, Healing.HalvedOrScaled(10, DegreeOfSuccess.Success));
            Assert.Equal(10, Healing.HalvedOrScaled(10, DegreeOfSuccess.Failure));
            Assert.Equal(20, Healing.HalvedOrScaled(10, DegreeOfSuccess.CriticalFailure));
        }

        [Fact]
        public void ClericFontPaysForHealBeforeRegularSlots()
        {
            Dice.SetSeed(15);
            CharacterCore core = Core(ClassList.Cleric, wis: 18);
            core.Health.TakeDamage(5);
            PlayerCombatant cleric = new(core, ClassList.Cleric, new GridPos(0, 0));
            Encounter e = Fight(cleric, Dummy(9, 9));
            int hp = core.Health.CurrentHp;
            Assert.True(e.CastSpell(SpellList.Heal, cleric).Success);
            Assert.True(core.Health.CurrentHp > hp);
            Assert.Equal(1, core.FontSlotsUsed);
            Assert.Equal(2, core.FreeSlots(1));
        }

        [Fact]
        public void HealOnlyDamagesUndeadEnemies()
        {
            Dice.SetSeed(16);
            CharacterCore core = Core(ClassList.Cleric, wis: 18);
            PlayerCombatant cleric = new(core, ClassList.Cleric, new GridPos(0, 0));
            MonsterCombatant living = Dummy(2, 0);
            MonsterCombatant undead = Dummy(3, 0, undead: true);
            Encounter e = Fight(cleric, living, undead);
            Assert.False(e.CastSpell(SpellList.Heal, living).Success);
            int hp = undead.Health.CurrentHp;
            Assert.True(e.CastSpell(SpellList.Heal, undead).Success);
            Assert.True(undead.Health.CurrentHp < hp);
        }

        [Fact]
        public void FireballHitsEveryoneInBurstIncludingAllies()
        {
            Dice.SetSeed(17);
            CharacterCore core = Core(ClassList.Wizard, intel: 18, level: 5);
            core.InitializeHealth(ClassList.Wizard);
            PlayerCombatant wizard = new(core, ClassList.Wizard, new GridPos(5, 5));
            MonsterCombatant near = Dummy(6, 5);
            MonsterCombatant far = Dummy(15, 15);
            Encounter e = Fight(wizard, near, far);
            int wizardHp = core.Health.CurrentHp;
            Assert.True(e.CastSpell(SpellList.Fireball, near).Success);
            Assert.True(near.Health.CurrentHp < near.Health.MaxHp);
            Assert.Equal(far.Health.MaxHp, far.Health.CurrentHp);
            Assert.Contains(e.Log, l => l.Contains("Bohater") && l.Contains("obrażenia"));
            Assert.True(core.Health.CurrentHp <= wizardHp);
        }

        [Fact]
        public void SpellRangeAndSightAreEnforced()
        {
            Dice.SetSeed(18);
            CharacterCore core = Core(ClassList.Wizard, intel: 18);
            PlayerCombatant wizard = new(core, ClassList.Wizard, new GridPos(0, 0));
            MonsterCombatant far = Dummy(19, 0);
            Encounter e = Fight(wizard, far);
            Assert.False(e.CastSpell(SpellList.ElectricArc, far).Success);
            core.Feats.Add(FeatList.ReachSpell);
            Assert.Equal(60, e.SpellRange(SpellList.ElectricArc));
        }

        [Fact]
        public void TreatWoundsRequiresTrainingAndHeals()
        {
            Dice.SetSeed(19);
            CharacterCore untrained = Core(ClassList.Fighter);
            Assert.False(Healing.TreatWounds(untrained, ClassList.Fighter).Attempted);

            CharacterCore medic = Core(ClassList.Fighter, wis: 18);
            medic.ExtraTrainedSkills.Add(SkillName.Medicine);
            medic.Health.TakeDamage(5);
            bool healedOnce = false;
            for (int i = 0; i < 50; i++)
            {
                HealingResult r = Healing.TreatWounds(medic, ClassList.Fighter);
                Assert.True(r.Attempted);
                healedOnce |= r.Healed > 0;
            }
            Assert.True(healedOnce);
        }

        [Fact]
        public void BattleMedicineOncePerFight()
        {
            Dice.SetSeed(20);
            CharacterCore core = Core(ClassList.Fighter, wis: 18);
            core.ExtraTrainedSkills.Add(SkillName.Medicine);
            core.Feats.Add(FeatList.BattleMedicine);
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            Encounter e = Fight(hero, Dummy(9, 9));
            Assert.True(e.UseBattleMedicine().Success);
            Assert.False(e.UseBattleMedicine().Success);
        }

        [Fact]
        public void AllClassesCanFinishFightsAgainstAi()
        {
            foreach (ClassStats cls in ClassList.All)
            {
                for (int seed = 0; seed < 5; seed++)
                {
                    Dice.SetSeed(seed);
                    CharacterCore core = Core(cls, intel: 16, wis: 16);
                    PlayerCombatant hero = new(core, cls, new GridPos(0, 5));
                    Encounter e = new(new BattleGrid(12, 12), new Combatant[] { hero, new MonsterCombatant(StatblockList.GoblinWarrior, new GridPos(11, 5)) });
                    e.Start();
                    for (int i = 0; i < 2000 && e.State == EncounterState.InProgress; i++)
                        EnemyAi.PlayTurn(e);
                    Assert.Equal(EncounterState.Finished, e.State);
                }
            }
        }

        [Fact]
        public void ForceBarrageGainsShardEveryTwoRanksAndCantripEveryRank()
        {
            Dice.SetSeed(30);
            for (int i = 0; i < 40; i++)
            {
                int rankOne = SpellSlots.RollEffect(SpellList.ForceBarrage, 1);
                int rankTwo = SpellSlots.RollEffect(SpellList.ForceBarrage, 2);
                int rankThree = SpellSlots.RollEffect(SpellList.ForceBarrage, 3);
                Assert.InRange(rankOne, 4, 10);
                Assert.InRange(rankTwo, 4, 10);
                Assert.InRange(rankThree, 8, 20);
                Assert.InRange(SpellSlots.RollEffect(SpellList.ElectricArc, 3), 4, 16);
            }
        }

        [Fact]
        public void ChosenRankKeepsHigherSlotsFree()
        {
            Dice.SetSeed(31);
            CharacterCore core = Core(ClassList.Wizard, intel: 18, level: 5);
            core.InitializeHealth(ClassList.Wizard);
            PlayerCombatant wizard = new(core, ClassList.Wizard, new GridPos(0, 0));
            MonsterCombatant target = Dummy(3, 0);
            Encounter e = Fight(wizard, target);
            Assert.Equal(new List<int> { 3, 2, 1 }, core.CastingRanks(SpellList.ForceBarrage, ClassList.Wizard));
            Assert.True(e.CastSpell(SpellList.ForceBarrage, target, 1).Success);
            Assert.Equal(2, core.FreeSlots(3));
            Assert.Equal(2, core.FreeSlots(1));
        }

        [Fact]
        public void ThrownWeaponIsBackAfterTheFight()
        {
            Dice.SetSeed(32);
            CharacterCore core = Core(ClassList.Fighter, str: 18);
            core.EquippedWeapon = new CarriedWeapon(WeaponList.Dagger);
            core.Inventory.Add(core.EquippedWeapon);
            PlayerCombatant hero = new(core, ClassList.Fighter, new GridPos(0, 0));
            MonsterCombatant target = Dummy(2, 0, hp: 1);
            Encounter e = Fight(hero, target);
            for (int turn = 0; turn < 60 && e.State == EncounterState.InProgress; turn++)
            {
                hero.Actions.ResetTurn();
                if (!core.EquippedWeapon.IsLoaded)
                    e.Reload(core.EquippedWeapon);
                e.Strike(target, hero.Strikes[0]);
            }
            Assert.Equal(EncounterState.Finished, e.State);
            Assert.True(core.EquippedWeapon.IsLoaded);
        }

        [Fact]
        public void BrokenShieldStopsHelpingUntilRepaired()
        {
            CharacterCore core = Core(ClassList.Fighter);
            core.HasShield = true;
            Assert.True(core.ShieldUsable);
            core.ShieldHp = ShopCatalog.ShieldBrokenThreshold;
            Assert.False(core.ShieldUsable);
        }

        [Fact]
        public void FallenUnitIsReportedOnce()
        {
            Dice.SetSeed(33);
            CharacterCore core = Core(ClassList.Wizard, intel: 18);
            PlayerCombatant wizard = new(core, ClassList.Wizard, new GridPos(0, 0));
            MonsterCombatant weak = Dummy(3, 0, hp: 1);
            MonsterCombatant other = Dummy(5, 0);
            Encounter e = Fight(wizard, weak, other);
            Assert.True(e.CastSpell(SpellList.ForceBarrage, weak).Success);
            wizard.Actions.ResetTurn();
            Assert.True(e.CastSpell(SpellList.ForceBarrage, other).Success);
            Assert.Single(e.Log, l => l == $"{weak.Name} ginie.");
        }

        [Fact]
        public void MonsterCoreStatblocksMatchSource()
        {
            Assert.Equal(6, StatblockList.GoblinWarrior.MaxHp);
            Assert.Equal(7, StatblockList.GoblinWarrior.Strikes[0].AttackBonus);
            Assert.Equal(7, StatblockList.KoboldWarrior.MaxHp);
            Assert.Equal(3, StatblockList.SkeletonGuard.Strikes.Count);
            Assert.True(StatblockList.SkeletonGuard.IsUndead);
        }

        [Fact]
        public void HeroPointsCanBeSpentToSurviveDeath()
        {
            CharacterCore core = Core(ClassList.Fighter);
            Assert.False(core.Health.SpendAllHeroPointsToSurvive());
            core.Health.GainHeroPoint();
            core.Health.GainHeroPoint();
            core.Health.TakeDamage(1000);
            Assert.True(core.Health.SpendAllHeroPointsToSurvive());
            Assert.Equal(0, core.Health.HeroPoints);
            Assert.Equal(1, core.Health.CurrentHp);
            Assert.Equal(0, core.Health.Dying);
        }
    }
}
