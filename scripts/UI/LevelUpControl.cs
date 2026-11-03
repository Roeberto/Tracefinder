using System.Collections.Generic;
using Godot;
using Tracefinder.Character;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class LevelUpControl : Control
    {
        private CharacterCore _character;
        private ClassStats _class;

        public override void _Ready()
        {
            _character = GameSession.CurrentCharacter();
            _class = GameSession.CharacterClass;
            if (_character.Health.MaxHp == 0)
                _character.InitializeHealth(_class);

            GetNode<Button>("%SheetButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/CharacterSheet.tscn");
            GetNode<Button>("%AwardButton").Pressed += OnAward;
            Refresh();
        }

        private void OnAward()
        {
            int gained = _character.AddExperience((int)GetNode<SpinBox>("%XpAmount").Value, _class);
            GetNode<Label>("%MessageLabel").Text = gained > 0 ? $"Awans na poziom {_character.Level}." : "";
            Persist();
        }

        private void Persist()
        {
            CharacterStorage.SaveIfAlreadyStored(_character);
            Refresh();
        }

        private void Refresh()
        {
            string name = _character.Name.Length > 0 ? _character.Name : "Bezimienny";
            GetNode<Label>("%TitleLabel").Text = $"{name}, poziom {_character.Level}";
            GetNode<Label>("%XpLabel").Text = _character.Level >= Progression.MaxLevel
                ? "Najwyższy poziom"
                : $"{_character.Experience} z {Progression.ExperiencePerLevel} XP";
            GetNode<Label>("%HpLabel").Text = $"HP {_character.Health.CurrentHp} / {_character.Health.MaxHp}";
            int pending = LevelUp.PendingChoices(_character, _class);
            GetNode<Label>("%PendingLabel").Text = pending > 0 ? $"Do wyboru: {pending}" : "Wszystko wybrane";

            FillFeats();
            FillSkills();
            FillAttributes();
        }

        private void FillFeats()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%FeatsList");
            Clear(list);
            AddFeatCategory(list, FeatCategory.Ancestry, "Rodowód");
            AddFeatCategory(list, FeatCategory.Class, "Klasa");
            AddFeatCategory(list, FeatCategory.Skill, "Umiejętności");
            AddFeatCategory(list, FeatCategory.General, "Ogólne");

            if (_character.Feats.Count > 0)
            {
                list.AddChild(new HSeparator());
                list.AddChild(new Label { Text = "Wybrane featy" });
                foreach (Feat feat in _character.Feats)
                    list.AddChild(new Label { Text = $"{feat.Name}: {feat.Summary}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
            }
        }

        private void AddFeatCategory(VBoxContainer list, FeatCategory category, string title)
        {
            int slots = LevelUp.AvailableFeatSlots(_character, _class, category);
            List<Feat> feats = LevelUp.AvailableFeats(_character, _class, category);
            list.AddChild(new Label { Text = $"{title}: wolne miejsca {slots}" });

            if (slots == 0)
                return;
            if (feats.Count == 0)
            {
                list.AddChild(new Label { Text = "Na liście nie ma jeszcze featów do wyboru." });
                return;
            }

            foreach (Feat feat in feats)
            {
                Button button = new()
                {
                    Text = $"{feat.Name}: {feat.Summary}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    Alignment = HorizontalAlignment.Left
                };
                Feat captured = feat;
                button.Pressed += () =>
                {
                    LevelUp.TakeFeat(_character, _class, captured);
                    Persist();
                };
                list.AddChild(button);
            }
        }

        private void FillSkills()
        {
            GetNode<Label>("%SkillsTitle").Text = $"UMIEJĘTNOŚCI (do wydania {LevelUp.AvailableSkillIncreases(_character, _class)})";
            VBoxContainer list = GetNode<VBoxContainer>("%SkillsList");
            Clear(list);
            foreach (SkillName skill in SkillCatalog.All)
            {
                HBoxContainer row = new();
                ProficiencyRank rank = _character.GetSkillRank(skill, _class);
                row.AddChild(new Label { Text = UiText.SkillName(skill), SizeFlagsHorizontal = SizeFlags.ExpandFill });
                row.AddChild(new Label { Text = rank.ToString() });
                Button button = new()
                {
                    Text = "+",
                    CustomMinimumSize = new Vector2(40, 0),
                    Disabled = !LevelUp.CanIncreaseSkill(_character, _class, skill),
                    Name = $"Skill{skill}"
                };
                SkillName captured = skill;
                button.Pressed += () =>
                {
                    LevelUp.IncreaseSkill(_character, _class, captured);
                    Persist();
                };
                row.AddChild(button);
                list.AddChild(row);
            }
        }

        private void FillAttributes()
        {
            GetNode<Label>("%AttributesTitle").Text = $"CECHY (do wydania {LevelUp.AvailableAttributeBoosts(_character)})";
            VBoxContainer list = GetNode<VBoxContainer>("%AttributesList");
            Clear(list);
            foreach (AbilityScore ability in System.Enum.GetValues<AbilityScore>())
            {
                HBoxContainer row = new();
                row.AddChild(new Label { Text = UiText.AbilityName(ability), SizeFlagsHorizontal = SizeFlags.ExpandFill });
                row.AddChild(new Label { Text = $"{_character.GetScore(ability)} ({UiText.Signed(_character.AbilityModifier(ability))})" });
                Button button = new()
                {
                    Text = "+",
                    CustomMinimumSize = new Vector2(40, 0),
                    Disabled = !LevelUp.CanBoost(_character, ability),
                    Name = $"Boost{ability}"
                };
                AbilityScore captured = ability;
                button.Pressed += () =>
                {
                    LevelUp.Boost(_character, _class, captured);
                    Persist();
                };
                row.AddChild(button);
                list.AddChild(row);
            }
        }

        private static void Clear(Node node)
        {
            foreach (Node child in node.GetChildren())
            {
                node.RemoveChild(child);
                child.QueueFree();
            }
        }
    }
}
