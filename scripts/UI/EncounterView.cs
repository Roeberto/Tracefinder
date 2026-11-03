using System.Collections.Generic;
using Godot;
using Tracefinder.Character;
using Tracefinder.Combat;
using Tracefinder.Gameplay;
using Tracefinder.Items;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class EncounterView : Control
    {
        private enum Mode
        {
            None,
            Stride,
            Step,
            Strike,
            Spell
        }

        private Encounter _encounter;
        private BattleGridView _grid;
        private OptionButton _strikeOption;
        private OptionButton _damageOption;
        private RichTextLabel _log;
        private Label _message;
        private Mode _mode;
        private Combatant _shownUnit;
        private bool _enemyTurnRunning;
        private EncounterPlan _plan;
        private int _levelBeforeFight;

        private const double EnemyActionDelaySeconds = 0.45;

        public override void _Ready()
        {
            _grid = GetNode<BattleGridView>("%GridView");
            _strikeOption = GetNode<OptionButton>("%StrikeOption");
            _damageOption = GetNode<OptionButton>("%DamageTypeOption");
            _log = GetNode<RichTextLabel>("%LogText");
            _message = GetNode<Label>("%MessageLabel");

            CharacterCore character = GameSession.CurrentCharacter();
            ClassStats classStats = GameSession.CharacterClass;
            if (character.Health.MaxHp == 0)
                character.InitializeHealth(classStats);
            if (character.Health.IsUnconscious)
                character.Health.FullRest();

            _plan = GameSession.PendingPlan;
            GameSession.PendingPlan = null;
            _levelBeforeFight = character.Level;
            if (_plan != null && GameSession.Campaign != null)
            {
                _encounter = GameSession.Campaign.BuildEncounter(_plan);
            }
            else
            {
                Location location = LocationGenerator.Generate(GameSession.WorldSeed, 0, 0, Biome.Plains);
                _encounter = GameSession.CreateEncounter(location, character, classStats);
            }

            _encounter.LogAdded += line => _log.AddText(line + "\n");
            _encounter.Start();
            _grid.Encounter = _encounter;
            _grid.CellClicked += OnCellClicked;

            GetNode<Button>("%StrideButton").Pressed += () => SetMode(Mode.Stride);
            GetNode<Button>("%StepButton").Pressed += () => SetMode(Mode.Step);
            GetNode<Button>("%StrikeButton").Pressed += () => SetMode(Mode.Strike);
            GetNode<Button>("%InteractButton").Pressed += OnInteract;
            GetNode<Button>("%ShieldButton").Pressed += () => Act(() => _encounter.RaiseShield());
            GetNode<Button>("%MedicineButton").Pressed += () => Act(() => _encounter.UseBattleMedicine());
            GetNode<Button>("%SpellButton").Pressed += () => SetMode(Mode.Spell);
            GetNode<Button>("%PotionButton").Pressed += OnDrink;
            GetNode<Button>("%SwapButton").Pressed += OnSwap;
            GetNode<OptionButton>("%SpellOption").ItemSelected += _ => { RebuildRanks(); Refresh(); };
            GetNode<Button>("%EndTurnButton").Pressed += OnEndTurn;
            GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            GetNode<Button>("%LevelUpButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/LevelUp.tscn");
            GetNode<Button>("%ContinueButton").Pressed += OnContinue;
            _encounter.Finished += OnFinished;
            _strikeOption.ItemSelected += _ => OnStrikeChanged();
            _damageOption.ItemSelected += _ => Refresh();

            Refresh();
            RunEnemyTurns();
        }

        private void OnFinished()
        {
            CharacterCore character = GameSession.CurrentCharacter();
            bool won = _encounter.Winner == Tracefinder.Combat.Side.Party;
            if (_plan != null && GameSession.Campaign != null)
                FinishCampaignFight(won);
            else
                CharacterStorage.SaveIfAlreadyStored(character);

            GetNode<Button>("%LevelUpButton").Visible = won && LevelUp.PendingChoices(character, GameSession.CharacterClass) > 0;
        }

        // Porażki nie zapisujemy, żeby autozapis sprzed walki pozostał do wczytania
        private void FinishCampaignFight(bool won)
        {
            Button next = GetNode<Button>("%ContinueButton");
            next.Visible = true;
            if (!won)
                return;

            VictoryReward reward = GameSession.Campaign.ResolveVictory(_plan, _levelBeforeFight);
            string line = $"Łup: {UiText.Money(reward.Silver)}";
            if (reward.Potion != null)
                line += $" i {reward.Potion.Name}";
            _log.AddText(line + ".\n");
            if (reward.LevelsGained > 0)
                _log.AddText($"Awans na poziom {GameSession.Campaign.Hero.Level}! Hero Point +{reward.LevelsGained}.\n");
            CampaignStorage.Save();
        }

        private void OnContinue()
        {
            bool won = _encounter.Winner == Tracefinder.Combat.Side.Party;
            if (!won || GameSession.Campaign.Completed)
            {
                GameSession.EndingVictory = won;
                GetTree().ChangeSceneToFile("res://scenes/Ending.tscn");
                return;
            }
            GetTree().ChangeSceneToFile("res://scenes/Exploration.tscn");
        }

        // Pola podświetlone dla obecnej akcji, do testów interfejsu bez okna
        public Godot.Collections.Array<Vector2I> HighlightedCells()
        {
            Godot.Collections.Array<Vector2I> cells = new();
            foreach (GridPos pos in _grid.Highlighted)
                cells.Add(new Vector2I(pos.X, pos.Y));
            return cells;
        }

        public Vector2I GridSize()
        {
            return new Vector2I(_encounter.Grid.Width, _encounter.Grid.Height);
        }

        private bool IsPlayerTurn => _encounter.Current?.Side == Tracefinder.Combat.Side.Party;

        // Przeciwnicy działają sami, po jednej akcji, żeby gracz widział co się dzieje
        private async void RunEnemyTurns()
        {
            if (_enemyTurnRunning)
                return;
            _enemyTurnRunning = true;

            while (_encounter.Current?.Side == Tracefinder.Combat.Side.Enemies)
            {
                await ToSignal(GetTree().CreateTimer(EnemyActionDelaySeconds), SceneTreeTimer.SignalName.Timeout);
                if (!IsInstanceValid(this) || !IsInsideTree())
                    return;

                Combatant unit = _encounter.Current;
                if (unit != null && !EnemyAi.TakeAction(_encounter) && _encounter.Current == unit)
                    _encounter.EndTurn();
                Refresh();
            }

            _enemyTurnRunning = false;
            Refresh();
        }

        private StrikeOption SelectedStrike()
        {
            Combatant unit = _encounter.Current;
            if (unit == null || unit.Strikes.Count == 0 || _strikeOption.Selected < 0 || _strikeOption.Selected >= unit.Strikes.Count)
                return null;
            return unit.Strikes[_strikeOption.Selected];
        }

        private WeaponDamageType? SelectedDamageType()
        {
            return _damageOption.Selected >= 0 ? (WeaponDamageType)_damageOption.GetItemId(_damageOption.Selected) : null;
        }

        private void SetMode(Mode mode)
        {
            _mode = _mode == mode ? Mode.None : mode;
            _message.Text = "";
            Refresh();
        }

        private void OnEndTurn()
        {
            if (!IsPlayerTurn)
                return;
            _encounter.EndTurn();
            _mode = Mode.None;
            _message.Text = "";
            Refresh();
            RunEnemyTurns();
        }

        private void OnInteract()
        {
            StrikeOption strike = SelectedStrike();
            if (strike?.Carried != null)
                Report(_encounter.Reload(strike.Carried));
            Refresh();
        }

        private void Act(System.Func<ActionResult> action)
        {
            Report(action());
            RebuildActionLists();
            Refresh();
        }

        private void OnDrink()
        {
            OptionButton option = GetNode<OptionButton>("%PotionOption");
            if (option.Selected >= 0)
                Act(() => _encounter.DrinkPotion(ShopCatalog.Potions[option.GetItemId(option.Selected)]));
        }

        private void OnSwap()
        {
            OptionButton option = GetNode<OptionButton>("%SwapOption");
            if (option.Selected < 0 || _encounter.Current is not PlayerCombatant player)
                return;
            Act(() => _encounter.EquipWeapon(player.Core.Inventory[option.GetItemId(option.Selected)]));
            RebuildStrikeOptions();
            Refresh();
        }

        private Spell SelectedSpell()
        {
            OptionButton option = GetNode<OptionButton>("%SpellOption");
            if (_encounter.Current is not PlayerCombatant player || option.Selected < 0)
                return null;
            return SpellList.For(player.ClassStats)[option.Selected];
        }

        private void RebuildRanks()
        {
            OptionButton ranks = GetNode<OptionButton>("%RankOption");
            ranks.Clear();
            Spell spell = SelectedSpell();
            if (spell == null || _encounter.Current is not PlayerCombatant player)
                return;
            foreach (int rank in player.Core.CastingRanks(spell, player.ClassStats))
                ranks.AddItem(RankText(player, spell, rank), rank);
        }

        private static string RankText(PlayerCombatant player, Spell spell, int rank)
        {
            if (spell.IsCantrip)
                return "Sztuczka";
            bool font = spell.Name == SpellList.Heal.Name && rank == SpellSlots.HighestRank(player.Level) && player.Core.FreeFontSlots(player.ClassStats) > 0;
            int free = font ? player.Core.FreeFontSlots(player.ClassStats) : player.Core.FreeSlots(rank);
            return $"Ranga {rank} ({free})";
        }

        // Listy mikstur, broni i zaklęć zależą od postaci w turze i zmieniają się po ich użyciu
        private void RebuildActionLists()
        {
            OptionButton potions = GetNode<OptionButton>("%PotionOption");
            OptionButton swap = GetNode<OptionButton>("%SwapOption");
            OptionButton spells = GetNode<OptionButton>("%SpellOption");
            int chosenPotion = potions.Selected;
            int chosenSpell = spells.Selected;
            potions.Clear();
            swap.Clear();
            spells.Clear();

            if (_encounter.Current is PlayerCombatant player)
            {
                for (int i = 0; i < ShopCatalog.Potions.Count; i++)
                {
                    int count = player.Core.Potions.GetValueOrDefault(ShopCatalog.Potions[i].Name);
                    if (count > 0)
                        potions.AddItem($"{ShopCatalog.Potions[i].Name} x{count}", i);
                }
                for (int i = 0; i < player.Core.Inventory.Count; i++)
                {
                    if (player.Core.Inventory[i] != player.Core.EquippedWeapon)
                        swap.AddItem(player.Core.Inventory[i].Name, i);
                }
                foreach (Spell spell in SpellList.For(player.ClassStats))
                    spells.AddItem(spell.Name);
            }

            if (chosenPotion >= 0 && chosenPotion < potions.ItemCount)
                potions.Select(chosenPotion);
            if (chosenSpell >= 0 && chosenSpell < spells.ItemCount)
                spells.Select(chosenSpell);
            RebuildRanks();
        }

        private void OnStrikeChanged()
        {
            RebuildDamageOptions();
            Refresh();
        }

        private void OnCellClicked(GridPos pos)
        {
            if (!IsPlayerTurn)
                return;

            switch (_mode)
            {
                case Mode.Stride:
                    if (Report(_encounter.Stride(pos)))
                        _mode = Mode.None;
                    break;
                case Mode.Step:
                    if (Report(_encounter.Step(pos)))
                        _mode = Mode.None;
                    break;
                case Mode.Strike:
                    Combatant target = _encounter.UnitAt(pos);
                    StrikeOption strike = SelectedStrike();
                    if (target == null)
                        _message.Text = "Na tym polu nie ma celu.";
                    else if (strike != null)
                        Report(_encounter.Strike(
                            target,
                            strike,
                            SelectedDamageType(),
                            GetNode<CheckBox>("%ViciousSwingCheck").ButtonPressed,
                            GetNode<CheckBox>("%HeroPointCheck").ButtonPressed));
                    break;
                case Mode.Spell:
                    Combatant spellTarget = _encounter.UnitAt(pos);
                    OptionButton ranks = GetNode<OptionButton>("%RankOption");
                    if (spellTarget == null)
                    {
                        _message.Text = "Na tym polu nie ma celu.";
                    }
                    else if (SelectedSpell() != null && ranks.Selected >= 0 && Report(_encounter.CastSpell(SelectedSpell(), spellTarget, ranks.GetItemId(ranks.Selected))))
                    {
                        _mode = Mode.None;
                        RebuildRanks();
                    }
                    break;
                default:
                    _message.Text = "Najpierw wybierz akcję.";
                    break;
            }
            Refresh();
        }

        private bool Report(ActionResult result)
        {
            _message.Text = result.Success ? "" : result.Message;
            return result.Success;
        }

        private void RebuildStrikeOptions()
        {
            _strikeOption.Clear();
            Combatant unit = _encounter.Current;
            if (unit == null)
                return;
            foreach (StrikeOption strike in unit.Strikes)
                _strikeOption.AddItem(strike.Name);
            RebuildDamageOptions();
        }

        private void RebuildDamageOptions()
        {
            _damageOption.Clear();
            StrikeOption strike = SelectedStrike();
            if (strike == null)
                return;

            foreach (WeaponDamageType type in strike.Weapon.PossibleDamageTypes())
            {
                bool usable = strike.Carried == null || strike.Carried.CanStrikeWith(type);
                if (usable)
                    _damageOption.AddItem(Encounter.DamageTypeName(type), (int)type);
            }
        }

        private void Refresh()
        {
            Combatant unit = _encounter.Current;
            if (unit != _shownUnit)
            {
                _shownUnit = unit;
                RebuildStrikeOptions();
                RebuildActionLists();
            }

            GetNode<Label>("%RoundLabel").Text = $"Runda {_encounter.Round}";
            FillInitiative();

            bool active = unit != null;
            GetNode<Label>("%ActiveName").Text = active ? unit.Name : FinishedText();
            GetNode<Label>("%ActiveStats").Text = active
                ? $"HP {unit.Health.CurrentHp}/{unit.Health.MaxHp}, AC {unit.ArmorClass}, szybkość {unit.SpeedFeet} stóp"
                : _encounter.ExperienceAwarded > 0 ? $"Zdobyte doświadczenie: {_encounter.ExperienceAwarded} XP" : "";
            GetNode<Label>("%ActionsLabel").Text = active ? UiText.Pips(unit.Actions.MainActions, ActionTracker.MaxMainAction) : "";

            StrikeOption strike = SelectedStrike();
            GetNode<Label>("%StrikeInfo").Text = strike == null ? "" : StrikeInfo(unit, strike);

            bool playerTurn = IsPlayerTurn;
            bool canAct = playerTurn && unit.Actions.CanUseMainAction(1);
            if (!playerTurn)
                _mode = Mode.None;
            SetButton("%StrideButton", canAct, _mode == Mode.Stride);
            SetButton("%StepButton", canAct, _mode == Mode.Step);
            SetButton("%StrikeButton", canAct && strike != null, _mode == Mode.Strike);
            GetNode<Button>("%EndTurnButton").Disabled = !playerTurn;

            Button interact = GetNode<Button>("%InteractButton");
            interact.Visible = strike?.Carried != null && !strike.Carried.IsLoaded;
            interact.Disabled = !playerTurn || (strike?.Carried != null && !unit.Actions.CanUseMainAction(strike.Carried.ActionsToReady));

            RefreshClassActions(unit, playerTurn, canAct);

            _strikeOption.Disabled = !playerTurn;
            _damageOption.Disabled = !playerTurn;

            UpdateHighlights(unit, strike);
            _grid.QueueRedraw();
        }

        private void RefreshClassActions(Combatant unit, bool playerTurn, bool canAct)
        {
            PlayerCombatant player = playerTurn ? unit as PlayerCombatant : null;

            CheckBox swing = GetNode<CheckBox>("%ViciousSwingCheck");
            swing.Visible = player?.HasViciousSwing ?? false;
            swing.Disabled = player == null || !player.Actions.CanUseMainAction(2);
            if (swing.Disabled)
                swing.SetPressedNoSignal(false);

            CheckBox hero = GetNode<CheckBox>("%HeroPointCheck");
            hero.Visible = player != null && player.Health.HeroPoints > 0;
            if (!hero.Visible)
                hero.SetPressedNoSignal(false);

            Button shield = GetNode<Button>("%ShieldButton");
            shield.Visible = player?.HasShield ?? false;
            SetButton("%ShieldButton", canAct && player != null && !player.ShieldRaised, false);

            Button medicine = GetNode<Button>("%MedicineButton");
            medicine.Visible = player != null && player.Core.HasFeat(FeatList.BattleMedicine);
            medicine.Disabled = !canAct || (player?.UsedBattleMedicine ?? true);

            OptionButton swap = GetNode<OptionButton>("%SwapOption");
            GetNode<HBoxContainer>("%SwapRow").Visible = player != null && swap.ItemCount > 0;
            GetNode<Button>("%SwapButton").Disabled = !canAct;

            OptionButton potions = GetNode<OptionButton>("%PotionOption");
            GetNode<HBoxContainer>("%PotionRow").Visible = player != null && potions.ItemCount > 0;
            GetNode<Button>("%PotionButton").Disabled = !canAct;

            OptionButton spells = GetNode<OptionButton>("%SpellOption");
            GetNode<VBoxContainer>("%SpellBox").Visible = player != null && spells.ItemCount > 0;
            Spell spell = SelectedSpell();
            OptionButton ranks = GetNode<OptionButton>("%RankOption");
            GetNode<Label>("%SpellInfo").Text = spell == null || player == null
                ? ""
                : $"{spell.Actions} akcje, {_encounter.SpellRange(spell)} stóp, DC {player.Core.ComputeSpellDc(player.ClassStats)}, atak {UiText.Signed(player.Core.ComputeSpellAttack(player.ClassStats))}. {spell.Summary}";
            SetButton("%SpellButton", spell != null && playerTurn && unit.Actions.CanUseMainAction(spell.Actions) && ranks.ItemCount > 0, _mode == Mode.Spell);
        }

        private void SetButton(string path, bool enabled, bool pressedLook)
        {
            Button button = GetNode<Button>(path);
            button.Disabled = !enabled;
            button.Modulate = pressedLook ? new Color(1.3f, 1.3f, 0.8f) : new Color(1, 1, 1);
        }

        private string FinishedText()
        {
            return _encounter.Winner == Tracefinder.Combat.Side.Party ? "Zwycięstwo" : "Porażka";
        }

        private string StrikeInfo(Combatant unit, StrikeOption strike)
        {
            int map = AttackRules.MultipleAttackPenalty(unit.Actions.AttacksThisTurn, strike.Weapon);
            int attack = unit.AttackBonus(strike, false);
            string damage = UiText.DamageText(strike.Weapon, unit.DamageBonus(strike, false), strike.Carried?.StrikingRune ?? 0);
            if (strike.Carried != null && !strike.Carried.IsLoaded)
                return "Broń trzeba przygotować.";
            string traits = UiText.TraitList(strike.Weapon);
            string text = $"Atak {UiText.Signed(attack + map)} (MAP {map}), obrażenia {damage}";
            return traits.Length > 0 ? $"{text}\n{traits}" : text;
        }

        private void UpdateHighlights(Combatant unit, StrikeOption strike)
        {
            HashSet<GridPos> cells = _grid.Highlighted;
            cells.Clear();
            if (unit == null)
                return;

            switch (_mode)
            {
                case Mode.Stride:
                    foreach (GridPos pos in _encounter.StrideTargets().Keys)
                        cells.Add(pos);
                    break;
                case Mode.Step:
                    foreach (GridPos pos in _encounter.StepTargets())
                        cells.Add(pos);
                    break;
                case Mode.Strike:
                    if (strike == null)
                        break;
                    foreach (Combatant other in _encounter.Combatants)
                    {
                        if (other.Side != unit.Side && !other.IsOut && _encounter.StrikeReach(unit, other, strike).Refusal == null)
                            cells.Add(other.Position);
                    }
                    break;
                case Mode.Spell:
                    Spell spell = SelectedSpell();
                    if (spell == null)
                        break;
                    foreach (Combatant other in _encounter.Combatants)
                    {
                        if (_encounter.SpellTargetRefusal(spell, other) == null)
                            cells.Add(other.Position);
                    }
                    break;
            }
        }

        private void FillInitiative()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%InitiativeList");
            foreach (Node child in list.GetChildren())
                child.QueueFree();

            foreach (Combatant combatant in _encounter.Combatants)
            {
                string marker = combatant == _encounter.Current ? "> " : "";
                string state = combatant.Health.IsDead ? " (martwy)" : combatant.IsOut ? " (nieprzytomny)" : $" {combatant.Health.CurrentHp}/{combatant.Health.MaxHp}";
                Label label = new() { Text = $"{marker}{combatant.Initiative}  {combatant.Name}{state}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
                if (combatant.IsOut)
                    label.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.52f));
                list.AddChild(label);
            }
        }
    }
}
