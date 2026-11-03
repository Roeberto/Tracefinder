using System.Collections.Generic;
using Godot;
using Tracefinder.Character;
using Tracefinder.Items;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public partial class CharacterSheetControl : Control
    {
        private static readonly PackedScene SkillRowScene = GD.Load<PackedScene>("res://scenes/ui/SkillRow.tscn");

        private CharacterCore _character;
        private ClassStats _class;
        private readonly Dictionary<SkillName, SkillRow> _skillRows = new();

        public override void _Ready()
        {
            _character = GameSession.CurrentCharacter();
            _class = GameSession.CharacterClass;
            if (_character.Health.MaxHp == 0)
                _character.InitializeHealth(_class);

            bool inCampaign = GameSession.Campaign != null;
            Button back = GetNode<Button>("%MenuButton");
            back.Text = inCampaign ? "Wróć na mapę" : "Menu";
            back.Pressed += () => GetTree().ChangeSceneToFile(inCampaign ? "res://scenes/Exploration.tscn" : "res://scenes/MainMenu.tscn");
            // W kampanii HP i Hero Pointy zmieniają się tylko w grze, a nie ręcznie z arkusza
            foreach (string path in new[] { "%EditButton", "%RestButton", "%HeroPlus", "%HeroMinus", "%HealButton", "%DamageButton", "%HpAmount" })
                GetNode<Control>(path).Visible = !inCampaign;
            GetNode<Button>("%EditButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/CharacterCreator.tscn");
            GetNode<Button>("%LevelUpButton").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/LevelUp.tscn");
            GetNode<Button>("%RestButton").Pressed += () => Change(() => _character.Rest());
            GetNode<Button>("%HealButton").Pressed += () => Change(() => _character.Health.HealDamage(Amount()));
            GetNode<Button>("%DamageButton").Pressed += () => Change(() => _character.Health.TakeDamage(Amount()));
            GetNode<Button>("%HeroPlus").Pressed += () => Change(() => _character.Health.GainHeroPoint());
            GetNode<Button>("%HeroMinus").Pressed += () => Change(() => _character.Health.SpendHeroPoint());

            VBoxContainer skills = GetNode<VBoxContainer>("%SkillsList");
            foreach (SkillName skill in SkillCatalog.All)
            {
                SkillRow row = SkillRowScene.Instantiate<SkillRow>();
                skills.AddChild(row);
                _skillRows[skill] = row;
            }

            Refresh();
        }

        private int Amount()
        {
            return (int)GetNode<SpinBox>("%HpAmount").Value;
        }

        private void Change(System.Action action)
        {
            action();
            Refresh();
        }

        private void Refresh()
        {
            string ancestry = _character.Ancestry?.Name ?? "Bez rodowodu";
            GetNode<Label>("%NameLabel").Text = _character.Name.Length > 0 ? _character.Name : "Bezimienny";
            GetNode<Label>("%PortraitLetter").Text = _character.Name.Length > 0 ? _character.Name.Substring(0, 1).ToUpperInvariant() : "?";
            GetNode<Label>("%SubtitleLabel").Text = _character.Background != null ? $"{ancestry} {_class.Name}, {_character.Background.Name}" : $"{ancestry} {_class.Name}";
            GetNode<Label>("%LevelLabel").Text = $"Poziom {_character.Level}, {_character.Experience} z {Progression.ExperiencePerLevel} XP";

            int pending = LevelUp.PendingChoices(_character, _class);
            GetNode<Button>("%LevelUpButton").Text = pending > 0 ? $"Rozwój ({pending})" : "Rozwój";

            foreach (AbilityScore ability in System.Enum.GetValues<AbilityScore>())
                GetNode<AbilityCard>($"%{ability}Card").SetValues(UiText.AbilityName(ability), _character.GetScore(ability), _character.AbilityModifier(ability));

            GetNode<Label>("%SpeedValue").Text = _character.SpeedFeet.ToString();
            GetNode<Label>("%HeroValue").Text = UiText.Pips(_character.Health.HeroPoints, HealthTracker.MaxHeroPoints);

            HealthTracker health = _character.Health;
            GetNode<Label>("%HpValue").Text = $"{health.CurrentHp} / {health.MaxHp}";
            ProgressBar bar = GetNode<ProgressBar>("%HpBar");
            bar.MaxValue = System.Math.Max(health.MaxHp, 1);
            bar.Value = health.CurrentHp;

            int level = _character.Level;
            GetNode<SaveRow>("%FortitudeRow").SetValues(_class.RankAt(ProficiencyStat.Fortitude, level), "Fortitude", _character.ComputeFortitude(_class));
            GetNode<SaveRow>("%ReflexRow").SetValues(_class.RankAt(ProficiencyStat.Reflex, level), "Reflex", _character.ComputeReflex(_class));
            GetNode<SaveRow>("%WillRow").SetValues(_class.RankAt(ProficiencyStat.Will, level), "Will", _character.ComputeWill(_class));
            GetNode<SaveRow>("%PerceptionRow").SetValues(_class.RankAt(ProficiencyStat.Perception, level), "Percepcja", _character.ComputePerception(_class));
            GetNode<Label>("%VisionLabel").Text = VisionText();

            FillProficiencies(level);
            foreach (KeyValuePair<SkillName, SkillRow> pair in _skillRows)
            {
                pair.Value.SetValues(
                    _character.GetSkillRank(pair.Key, _class),
                    UiText.AbilityShort(SkillCatalog.KeyAbilityFor(pair.Key)),
                    UiText.SkillName(pair.Key),
                    _character.ComputeSkillBonus(pair.Key, _class));
            }

            GetNode<Label>("%InitiativeValue").Text = UiText.Signed(_character.ComputeInitiative(_class));
            GetNode<Label>("%AcValue").Text = _character.ComputeMaxAc(_class).ToString();
            GetNode<Label>("%ClassDcValue").Text = _character.ComputeClassDc(_class).ToString();
            GetNode<Label>("%ConditionsLabel").Text = ConditionsText(health);

            FillActions();
            FillInventory();
            FillFeatures();
        }

        private string VisionText()
        {
            switch (_character.Ancestry?.Vision)
            {
                case Vision.Darkvision:
                    return "Widzenie w ciemności";
                case Vision.LowLight:
                    return "Widzenie w słabym świetle";
                default:
                    return "Zwykłe widzenie";
            }
        }

        private static string ConditionsText(HealthTracker health)
        {
            if (health.IsDead)
                return "Martwy";
            List<string> conditions = new();
            if (health.IsUnconscious)
                conditions.Add("Nieprzytomny");
            if (health.Dying > 0)
                conditions.Add($"Dying {health.Dying}");
            if (health.Wounded > 0)
                conditions.Add($"Wounded {health.Wounded}");
            return conditions.Count > 0 ? string.Join(", ", conditions) : "Brak aktywnych stanów";
        }

        private void FillProficiencies(int level)
        {
            VBoxContainer list = GetNode<VBoxContainer>("%ProficienciesList");
            Clear(list);
            AddRow(list, "Proste bronie", _class.RankAt(ProficiencyStat.SimpleWeapons, level));
            AddRow(list, "Wojskowe bronie", _class.RankAt(ProficiencyStat.MartialWeapons, level));
            AddRow(list, "Zaawansowane bronie", _class.RankAt(ProficiencyStat.AdvancedWeapons, level));
            AddRow(list, "Ataki bez broni", _class.RankAt(ProficiencyStat.UnarmedAttacks, level));
            AddRow(list, "Bez pancerza", _class.RankAt(ProficiencyStat.Unarmored, level));
            AddRow(list, "Lekkie pancerze", _class.RankAt(ProficiencyStat.LightArmor, level));
            AddRow(list, "Średnie pancerze", _class.RankAt(ProficiencyStat.MediumArmor, level));
            AddRow(list, "Ciężkie pancerze", _class.RankAt(ProficiencyStat.HeavyArmor, level));
            if (_class.SpellcastingProficiency != ProficiencyRank.Untrained)
                AddRow(list, "Zaklęcia", _class.RankAt(ProficiencyStat.Spellcasting, level));
            AddRow(list, "Class DC", _class.RankAt(ProficiencyStat.ClassDc, level));
        }

        private static void AddRow(VBoxContainer list, string title, ProficiencyRank rank)
        {
            HBoxContainer row = new();
            row.AddChild(new Label { Text = title, SizeFlagsHorizontal = SizeFlags.ExpandFill });
            row.AddChild(new Label { Text = rank.ToString() });
            list.AddChild(row);
        }

        private void FillActions()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%ActionsList");
            Clear(list);
            CarriedWeapon weapon = _character.EquippedWeapon;
            if (weapon == null)
            {
                list.AddChild(new Label { Text = "Postać nie ma założonej broni. Załóż ją w zakładce Ekwipunek." });
                return;
            }

            int attack = _character.ComputeAttackBonus(weapon, _class);
            int damage = _character.ComputeDamageBonus(weapon.Template);
            string text = $"{weapon.Name}: atak {UiText.Signed(attack)}, obrażenia {UiText.DamageText(weapon.Template, damage, weapon.StrikingRune)} ({weapon.CurrentDamageType})";
            list.AddChild(new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart });

            string traits = UiText.TraitList(weapon.Template);
            if (traits.Length > 0)
                list.AddChild(new Label { Text = $"Cechy: {traits}", AutowrapMode = TextServer.AutowrapMode.WordSmart });

            list.AddChild(new Label { Text = "Kolejne ataki w turze mają karę MAP: -5 i -10, a przy Agile -4 i -8." });
            FillSpells(list);
        }

        private void FillSpells(VBoxContainer list)
        {
            IReadOnlyList<Spell> spells = SpellList.For(_class);
            if (spells.Count == 0)
                return;

            list.AddChild(new HSeparator());
            list.AddChild(new Label { Text = $"Zaklęcia: atak {UiText.Signed(_character.ComputeSpellAttack(_class))}, DC {_character.ComputeSpellDc(_class)}" });
            List<string> slots = new();
            for (int rank = 1; rank <= SpellSlots.HighestRank(_character.Level); rank++)
                slots.Add($"ranga {rank}: {_character.FreeSlots(rank)}");
            if (_class.Name == ClassList.Cleric.Name)
                slots.Add($"Divine Font: {_character.FreeFontSlots(_class)}");
            list.AddChild(new Label { Text = "Wolne sloty: " + string.Join(", ", slots) });
            foreach (Spell spell in spells)
            {
                string rank = spell.IsCantrip ? "sztuczka" : $"ranga {spell.BaseRank}";
                list.AddChild(new Label { Text = $"{spell.Name} ({rank}, {spell.Actions} akcje, {spell.RangeFeet} stóp): {spell.Summary}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
            }
        }

        private void FillInventory()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%InventoryList");
            Clear(list);
            list.AddChild(new Label { Text = $"Sakiewka: {UiText.Money(_character.Silver)}" });
            foreach (CarriedWeapon weapon in _character.Inventory)
            {
                HBoxContainer row = new();
                row.AddChild(new Label { Text = weapon.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill });
                bool equipped = weapon == _character.EquippedWeapon;
                Button button = new() { Text = equipped ? "Założona" : "Załóż", Disabled = equipped };
                CarriedWeapon captured = weapon;
                button.Pressed += () => Change(() =>
                {
                    _character.EquippedWeapon = captured;
                    CharacterStorage.SaveIfAlreadyStored(_character);
                });
                row.AddChild(button);
                list.AddChild(row);
            }

            foreach (Armor armor in _character.OwnedArmors)
            {
                HBoxContainer row = new();
                row.AddChild(new Label { Text = $"{armor.Name} (AC +{armor.AcBonus}, limit ZRĘ {armor.DexCap})", SizeFlagsHorizontal = SizeFlags.ExpandFill });
                bool worn = armor == _character.EquippedArmor;
                Button button = new() { Text = worn ? "Założony" : "Załóż", Disabled = worn };
                Armor captured = armor;
                button.Pressed += () => Change(() =>
                {
                    _character.EquippedArmor = captured;
                    CharacterStorage.SaveIfAlreadyStored(_character);
                });
                row.AddChild(button);
                list.AddChild(row);
            }

            if (_character.HasShield)
                list.AddChild(new Label { Text = $"Stalowa tarcza: {_character.ShieldHp} z {ShopCatalog.ShieldMaxHp} HP" });
            foreach (KeyValuePair<string, int> potion in _character.Potions)
            {
                if (potion.Value > 0)
                    list.AddChild(new Label { Text = $"{potion.Key} x{potion.Value}" });
            }
        }

        private void FillFeatures()
        {
            VBoxContainer list = GetNode<VBoxContainer>("%FeaturesList");
            Clear(list);
            foreach (string feature in _class.Level1Features)
                list.AddChild(new Label { Text = feature });

            int level = _character.Level;
            list.AddChild(new HSeparator());
            list.AddChild(new Label { Text = $"Miejsca na featy: rodowód {Progression.FeatSlots(FeatCategory.Ancestry, level, _class)}, klasa {Progression.FeatSlots(FeatCategory.Class, level, _class)}, umiejętności {Progression.FeatSlots(FeatCategory.Skill, level, _class)}, ogólne {Progression.FeatSlots(FeatCategory.General, level, _class)}" });
            foreach (Feat feat in _character.Feats)
                list.AddChild(new Label { Text = $"{feat.Name}: {feat.Summary}", AutowrapMode = TextServer.AutowrapMode.WordSmart });
        }

        private static void Clear(Node node)
        {
            foreach (Node child in node.GetChildren())
                child.QueueFree();
        }
    }
}
