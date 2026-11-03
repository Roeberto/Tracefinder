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
            Strike
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

            Location location = GameSession.PendingLocation ?? LocationGenerator.Generate(GameSession.WorldSeed, 0, 0, Biome.Plains);
            GameSession.PendingLocation = null;

            _encounter = GameSession.CreateEncounter(location, character, classStats);
            _encounter.LogAdded += line => _log.AddText(line + "\n");
            _encounter.Start();
            _grid.Encounter = _encounter;
            _grid.CellClicked += OnCellClicked;

            GetNode<Button>("%StrideButton").Pressed += () => SetMode(Mode.Stride);
            GetNode<Button>("%StepButton").Pressed += () => SetMode(Mode.Step);
            GetNode<Button>("%StrikeButton").Pressed += () => SetMode(Mode.Strike);
            GetNode<Button>("%InteractButton").Pressed += OnInteract;
            GetNode<Button>("%EndTurnButton").Pressed += OnEndTurn;
            GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            _strikeOption.ItemSelected += _ => OnStrikeChanged();
            _damageOption.ItemSelected += _ => Refresh();

            Refresh();
            RunEnemyTurns();
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
                        Report(_encounter.Strike(target, strike, SelectedDamageType()));
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
            interact.Visible = strike?.Carried != null && strike.Carried.Template.ActionsToReload > 0 && !strike.Carried.IsLoaded;
            interact.Disabled = !playerTurn || (strike?.Carried != null && !unit.Actions.CanUseMainAction(strike.Carried.Template.ActionsToReload));

            _strikeOption.Disabled = !playerTurn;
            _damageOption.Disabled = !playerTurn;

            UpdateHighlights(unit, strike);
            _grid.QueueRedraw();
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
