using System.Collections.Generic;
using System.Linq;
using Godot;
using Tracefinder.Character;
using Tracefinder.Items;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class CharacterCreatorControl : Control
    {
        private const int MinScore = 8;
        private const int MaxScore = 18;

        private readonly Dictionary<AbilityScore, LineEdit> _inputs = new();
        private readonly Dictionary<AbilityScore, Label> _modifiers = new();
        private readonly Dictionary<SkillName, CheckBox> _skills = new();
        private CharacterRepository _repository;
        private LineEdit _name;
        private OptionButton _classOption;
        private OptionButton _keyAbility;
        private OptionButton _ancestry;
        private OptionButton _weapon;
        private OptionButton _ancestryFeat;
        private OptionButton _classFeat;
        private readonly List<Feat> _ancestryFeats = new();
        private readonly List<Feat> _classFeats = new();
        private Label _message;

        public override void _Ready()
        {
            _repository = new CharacterRepository(ProjectSettings.GlobalizePath("user://characters"));
            _name = GetNode<LineEdit>("%NameInput");
            _classOption = GetNode<OptionButton>("%ClassOption");
            _keyAbility = GetNode<OptionButton>("%KeyAbilityOption");
            _ancestry = GetNode<OptionButton>("%AncestryOption");
            _weapon = GetNode<OptionButton>("%WeaponOption");
            _ancestryFeat = GetNode<OptionButton>("%AncestryFeatOption");
            _classFeat = GetNode<OptionButton>("%ClassFeatOption");
            _message = GetNode<Label>("%ErrorLabel");

            foreach (AbilityScore ability in System.Enum.GetValues<AbilityScore>())
            {
                LineEdit input = GetNode<LineEdit>($"%{ability}Input");
                input.TextChanged += _ => Recalculate();
                _inputs[ability] = input;
                _modifiers[ability] = GetNode<Label>($"%{ability}ModValue");
            }

            foreach (ClassStats classStats in ClassList.All)
                _classOption.AddItem(classStats.Name);
            foreach (Ancestry ancestry in AncestryList.All)
                _ancestry.AddItem(ancestry.Name);
            foreach (Equipment.Weapon weapon in WeaponList.All)
                _weapon.AddItem(weapon.Name);

            VBoxContainer skillList = GetNode<VBoxContainer>("%SkillsList");
            foreach (SkillName skill in SkillCatalog.All)
            {
                CheckBox box = new();
                box.Toggled += _ => Recalculate();
                skillList.AddChild(box);
                _skills[skill] = box;
            }

            _classOption.ItemSelected += _ => OnClassChanged();
            _keyAbility.ItemSelected += _ => Recalculate();
            _ancestry.ItemSelected += _ =>
            {
                RebuildFeats(FeatCategory.Ancestry, _ancestryFeat, _ancestryFeats, AncestryList.All[_ancestry.Selected].Name, null);
                Recalculate();
            };
            _name.TextChanged += _ => ShowMessage("");
            GetNode<Button>("%SaveButton").Pressed += Save;
            GetNode<Button>("%SheetButton").Pressed += OpenSheet;
            GetNode<Button>("%BackButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            GetNode<Button>("%NewCharacterButton").Pressed += () =>
            {
                Fill(GameSession.ExampleCharacter());
                _name.Text = "";
            };

            RefreshSavedList();
            Fill(GameSession.CurrentCharacter());
        }

        private ClassStats SelectedClass => ClassList.All[_classOption.Selected];

        private void OnClassChanged()
        {
            RebuildKeyAbilities(null);
            ResetSkillBoxes(new List<SkillName>());
            RebuildClassFeats(null);
            Recalculate();
        }

        private void RebuildKeyAbilities(AbilityScore? preferred)
        {
            _keyAbility.Clear();
            List<AbilityScore> options = SelectedClass.KeyAbilityOptions;
            foreach (AbilityScore ability in options)
                _keyAbility.AddItem(UiText.AbilityName(ability));
            int index = preferred.HasValue ? options.IndexOf(preferred.Value) : 0;
            _keyAbility.Select(index >= 0 ? index : 0);
        }

        private void RebuildClassFeats(List<Feat> preferred)
        {
            RebuildFeats(FeatCategory.Class, _classFeat, _classFeats, SelectedClass.Name, preferred);
            // Czarodziej i Kapłan dostają pierwszy feat klasowy dopiero na poziomie 2
            _classFeat.Disabled = !Progression.GrantsClassFeat(1, SelectedClass) || _classFeats.Count == 0;
        }

        private static void RebuildFeats(FeatCategory category, OptionButton option, List<Feat> feats, string source, List<Feat> preferred)
        {
            feats.Clear();
            option.Clear();
            option.AddItem("nie wybrano");
            foreach (Feat feat in FeatList.All)
            {
                if (feat.Category == category && feat.Source == source && feat.Level <= 1)
                {
                    feats.Add(feat);
                    option.AddItem(feat.Name);
                    option.SetItemTooltip(option.ItemCount - 1, feat.Summary);
                }
            }

            int chosen = preferred == null ? -1 : feats.FindIndex(f => preferred.Any(p => p.Name == f.Name));
            option.Select(chosen + 1);
            option.Disabled = feats.Count == 0;
        }

        private void ResetSkillBoxes(List<SkillName> extra)
        {
            foreach (KeyValuePair<SkillName, CheckBox> pair in _skills)
            {
                bool fromClass = SelectedClass.TrainedSkills.Contains(pair.Key);
                pair.Value.Disabled = fromClass;
                pair.Value.SetPressedNoSignal(fromClass || extra.Contains(pair.Key));
            }
        }

        private void Fill(CharacterCore source)
        {
            _name.Text = source.Name;
            foreach (KeyValuePair<AbilityScore, LineEdit> pair in _inputs)
                pair.Value.Text = source.GetScore(pair.Key).ToString();

            int classIndex = ClassList.All.ToList().FindIndex(c => c.Name == source.ClassID);
            _classOption.Select(classIndex >= 0 ? classIndex : 0);
            RebuildKeyAbilities(source.SelectedKeyAbility);

            int ancestryIndex = AncestryList.All.ToList().FindIndex(a => a == source.Ancestry);
            _ancestry.Select(ancestryIndex >= 0 ? ancestryIndex : 0);
            int weaponIndex = WeaponList.All.ToList().FindIndex(w => w.Name == source.EquippedWeapon?.Name);
            _weapon.Select(weaponIndex >= 0 ? weaponIndex : 0);

            ResetSkillBoxes(source.ExtraTrainedSkills);
            RebuildFeats(FeatCategory.Ancestry, _ancestryFeat, _ancestryFeats, AncestryList.All[_ancestry.Selected].Name, source.Feats);
            RebuildClassFeats(source.Feats);
            Recalculate();
        }

        // Zwraca postać z formularza. Przy błędnych danych postać jest null, a error mówi co poprawić.
        private CharacterCore ReadForm(out string error)
        {
            error = null;
            CharacterCore core = new()
            {
                Name = _name.Text.Trim(),
                ClassID = SelectedClass.Name,
                Ancestry = AncestryList.All[_ancestry.Selected],
                SelectedKeyAbility = SelectedClass.KeyAbilityOptions[_keyAbility.Selected]
            };

            Dictionary<AbilityScore, int> scores = new();
            foreach (KeyValuePair<AbilityScore, LineEdit> pair in _inputs)
            {
                if (!int.TryParse(pair.Value.Text, out int score) || score < MinScore || score > MaxScore)
                {
                    error = $"{UiText.AbilityName(pair.Key)}: wpisz liczbę od {MinScore} do {MaxScore}.";
                    return null;
                }
                scores[pair.Key] = score;
            }
            core.Strength = scores[AbilityScore.Strength];
            core.Dexterity = scores[AbilityScore.Dexterity];
            core.Constitution = scores[AbilityScore.Constitution];
            core.Intelligence = scores[AbilityScore.Intelligence];
            core.Wisdom = scores[AbilityScore.Wisdom];
            core.Charisma = scores[AbilityScore.Charisma];

            foreach (KeyValuePair<SkillName, CheckBox> pair in _skills)
            {
                if (pair.Value.ButtonPressed && !SelectedClass.TrainedSkills.Contains(pair.Key))
                    core.ExtraTrainedSkills.Add(pair.Key);
            }

            int allowed = System.Math.Max(SelectedClass.BonusSkillCount + core.AbilityModifier(AbilityScore.Intelligence), 0);
            if (core.ExtraTrainedSkills.Count > allowed)
            {
                error = $"Za dużo umiejętności: wybrano {core.ExtraTrainedSkills.Count}, a można {allowed}.";
                return null;
            }

            AddSelectedFeat(core, _ancestryFeat, _ancestryFeats);
            AddSelectedFeat(core, _classFeat, _classFeats);

            core.EquippedWeapon = new CarriedWeapon(WeaponList.All[_weapon.Selected]);
            core.Inventory.Add(core.EquippedWeapon);
            core.InitializeHealth(SelectedClass);
            return core;
        }

        private static void AddSelectedFeat(CharacterCore core, OptionButton option, List<Feat> feats)
        {
            if (option.Selected > 0)
                core.Feats.Add(feats[option.Selected - 1]);
        }

        private void Recalculate()
        {
            CharacterCore core = ReadForm(out string error);
            ShowMessage(error ?? "");
            if (core == null)
                return;

            ClassStats classStats = SelectedClass;
            foreach (KeyValuePair<AbilityScore, Label> pair in _modifiers)
                pair.Value.Text = UiText.Signed(core.AbilityModifier(pair.Key));

            GetNode<Label>("%AcValue").Text = core.ComputeMaxAc(classStats).ToString();
            GetNode<Label>("%ClassDcValue").Text = core.ComputeClassDc(classStats).ToString();
            int hp = core.ComputeMaxHp(classStats);
            GetNode<Label>("%HpValue").Text = $"{hp} / {hp}";
            ProgressBar bar = GetNode<ProgressBar>("%HpBar");
            bar.MaxValue = System.Math.Max(hp, 1);
            bar.Value = hp;

            SetSave("Fortitude", ProficiencyStat.Fortitude, core.ComputeFortitude(classStats), classStats, core.Level);
            SetSave("Reflex", ProficiencyStat.Reflex, core.ComputeReflex(classStats), classStats, core.Level);
            SetSave("Will", ProficiencyStat.Will, core.ComputeWill(classStats), classStats, core.Level);
            SetSave("Perception", ProficiencyStat.Perception, core.ComputePerception(classStats), classStats, core.Level);

            foreach (KeyValuePair<SkillName, CheckBox> pair in _skills)
            {
                string ability = UiText.AbilityShort(SkillCatalog.KeyAbilityFor(pair.Key));
                pair.Value.Text = $"{UiText.SkillName(pair.Key)} ({ability}) {UiText.Signed(core.ComputeSkillBonus(pair.Key, classStats))}";
            }

            int allowed = System.Math.Max(classStats.BonusSkillCount + core.AbilityModifier(AbilityScore.Intelligence), 0);
            GetNode<Label>("Margin/RootVBox/MainLayout/RightPanel/RightPanelBox/SkillsTitle").Text = $"Umiejętności (dodatkowe {core.ExtraTrainedSkills.Count} z {allowed})";

            VBoxContainer features = GetNode<VBoxContainer>("%ClassFeaturesList");
            foreach (Node child in features.GetChildren())
                child.QueueFree();
            foreach (string feature in classStats.Level1Features)
                features.AddChild(new Label { Text = feature });
        }

        private void SetSave(string prefix, ProficiencyStat stat, int value, ClassStats classStats, int level)
        {
            GetNode<Label>($"%{prefix}BadgeLabel").Text = UiText.RankLetter(classStats.RankAt(stat, level));
            GetNode<Label>($"%{prefix}Value").Text = UiText.Signed(value);
        }

        private void Save()
        {
            CharacterCore core = ReadForm(out string error);
            if (core == null)
            {
                ShowMessage(error);
                return;
            }
            if (CharacterRepository.FileNameFor(core.Name).Length == 0)
            {
                ShowMessage("Wpisz nazwę postaci, żeby ją zapisać.");
                return;
            }

            _repository.Save(core);
            GameSession.Character = core;
            RefreshSavedList();
            ShowMessage($"Zapisano postać {core.Name}.");
        }

        private void OpenSheet()
        {
            CharacterCore core = ReadForm(out string error);
            if (core == null)
            {
                ShowMessage(error);
                return;
            }
            GameSession.Character = core;
            GetTree().ChangeSceneToFile("res://scenes/CharacterSheet.tscn");
        }

        private void RefreshSavedList()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%CharacterNames");
            foreach (Node child in list.GetChildren())
                child.QueueFree();

            foreach (string name in _repository.ListNames())
            {
                Button button = new() { Text = name };
                string captured = name;
                button.Pressed += () => LoadCharacter(captured);
                list.AddChild(button);
            }
        }

        private void LoadCharacter(string name)
        {
            CharacterCore core = _repository.Load(name);
            if (core == null)
            {
                ShowMessage($"Nie udało się wczytać postaci {name}.");
                return;
            }
            GameSession.Character = core;
            Fill(core);
            ShowMessage($"Wczytano postać {name}.");
        }

        private void ShowMessage(string text)
        {
            _message.Text = text;
        }
    }
}
