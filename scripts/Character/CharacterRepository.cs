using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tracefinder.Items;

namespace Tracefinder.Character
{
    public record CharacterSave(
        string Name,
        int Level,
        int Experience,
        int Strength,
        int Dexterity,
        int Constitution,
        int Intelligence,
        int Wisdom,
        int Charisma,
        string ClassName,
        AbilityScore KeyAbility,
        string AncestryName,
        List<SkillName> ExtraTrainedSkills,
        string WeaponName,
        List<string> FeatNames,
        Dictionary<SkillName, ProficiencyRank> SkillIncreases = null,
        int SkillIncreasesUsed = 0,
        List<AbilityScore> AttributeBoosts = null,
        List<WeaponSave> Weapons = null,
        int EquippedWeaponIndex = -1,
        string ArmorName = null,
        List<string> OwnedArmors = null,
        bool HasShield = false,
        int ShieldHp = ShopCatalog.ShieldMaxHp,
        Dictionary<string, int> Potions = null,
        int Silver = 0,
        string BackgroundName = null,
        Dictionary<int, int> SpellSlotsUsed = null,
        int FontSlotsUsed = 0,
        int? CurrentHp = null,
        int Dying = 0,
        int Wounded = 0,
        int HeroPoints = 0);

    public record WeaponSave(string Name, int PotencyRune, int StrikingRune);

    // Zapis postaci jako pliki JSON. Starsze pliki bez ekwipunku i HP wczytują się jako postać w pełni sił.
    public class CharacterRepository
    {
        private const string Extension = ".json";

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _directory;

        public CharacterRepository(string directory)
        {
            _directory = directory;
        }

        public IReadOnlyList<string> ListNames()
        {
            if (!Directory.Exists(_directory))
                return Array.Empty<string>();

            return Directory.GetFiles(_directory, "*" + Extension)
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .OrderBy(name => name, StringComparer.CurrentCulture)
                .ToList();
        }

        public bool Exists(string name)
        {
            string fileName = FileNameFor(name);
            return fileName.Length > 0 && File.Exists(Path.Combine(_directory, fileName + Extension));
        }

        public void Save(CharacterCore character)
        {
            string fileName = FileNameFor(character.Name);
            if (fileName.Length == 0)
                throw new ArgumentException("Nazwa postaci jest pusta.");

            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, fileName + Extension), JsonSerializer.Serialize(ToSave(character), Options));
        }

        // Zwraca null, gdy pliku nie ma albo nie da się go odczytać
        public CharacterCore Load(string name)
        {
            string path = Path.Combine(_directory, FileNameFor(name) + Extension);
            if (!File.Exists(path))
                return null;

            try
            {
                CharacterSave save = JsonSerializer.Deserialize<CharacterSave>(File.ReadAllText(path), Options);
                return save == null ? null : FromSave(save);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Nazwa postaci to tekst wpisany przez gracza, więc nie może wyjść poza katalog zapisów
        public static string FileNameFor(string name)
        {
            StringBuilder safe = new();
            foreach (char c in name ?? "")
            {
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
                    safe.Append(c);
            }
            return safe.ToString().Trim();
        }

        public static CharacterSave ToSave(CharacterCore c)
        {
            return new CharacterSave(
                c.Name, c.Level, c.Experience,
                c.Strength, c.Dexterity, c.Constitution, c.Intelligence, c.Wisdom, c.Charisma,
                c.ClassID, c.SelectedKeyAbility, c.Ancestry?.Name,
                c.ExtraTrainedSkills.ToList(),
                c.EquippedWeapon?.Name,
                c.Feats.Select(f => f.Name).ToList(),
                new Dictionary<SkillName, ProficiencyRank>(c.SkillIncreases),
                c.SkillIncreasesUsed,
                c.AttributeBoosts.ToList(),
                c.Inventory.Select(w => new WeaponSave(w.Name, w.PotencyRune, w.StrikingRune)).ToList(),
                c.Inventory.IndexOf(c.EquippedWeapon),
                c.EquippedArmor?.Name,
                c.OwnedArmors.Select(a => a.Name).ToList(),
                c.HasShield,
                c.ShieldHp,
                new Dictionary<string, int>(c.Potions),
                c.Silver,
                c.Background?.Name,
                new Dictionary<int, int>(c.SpellSlotsUsed),
                c.FontSlotsUsed,
                c.Health.CurrentHp,
                c.Health.Dying,
                c.Health.Wounded,
                c.Health.HeroPoints);
        }

        public static CharacterCore FromSave(CharacterSave s)
        {
            CharacterCore core = new()
            {
                Name = s.Name,
                Level = s.Level,
                Experience = s.Experience,
                Strength = s.Strength,
                Dexterity = s.Dexterity,
                Constitution = s.Constitution,
                Intelligence = s.Intelligence,
                Wisdom = s.Wisdom,
                Charisma = s.Charisma,
                ClassID = s.ClassName,
                SelectedKeyAbility = s.KeyAbility,
                Ancestry = AncestryList.All.FirstOrDefault(a => a.Name == s.AncestryName),
                SkillIncreasesUsed = s.SkillIncreasesUsed,
                HasShield = s.HasShield,
                ShieldHp = s.ShieldHp,
                Silver = s.Silver,
                FontSlotsUsed = s.FontSlotsUsed,
                Background = BackgroundList.All.FirstOrDefault(b => b.Name == s.BackgroundName),
                EquippedArmor = ShopCatalog.Armors.FirstOrDefault(a => a.Name == s.ArmorName)
            };
            foreach (string armor in s.OwnedArmors ?? new List<string>())
            {
                Armor owned = ShopCatalog.Armors.FirstOrDefault(a => a.Name == armor);
                if (owned != null)
                    core.OwnedArmors.Add(owned);
            }
            foreach (KeyValuePair<string, int> potion in s.Potions ?? new Dictionary<string, int>())
                core.Potions[potion.Key] = potion.Value;
            foreach (KeyValuePair<int, int> slot in s.SpellSlotsUsed ?? new Dictionary<int, int>())
                core.SpellSlotsUsed[slot.Key] = slot.Value;
            core.ExtraTrainedSkills.AddRange(s.ExtraTrainedSkills ?? new List<SkillName>());
            core.AttributeBoosts.AddRange(s.AttributeBoosts ?? new List<AbilityScore>());
            foreach (KeyValuePair<SkillName, ProficiencyRank> increase in s.SkillIncreases ?? new Dictionary<SkillName, ProficiencyRank>())
                core.SkillIncreases[increase.Key] = increase.Value;

            LoadWeapons(core, s);

            foreach (string featName in s.FeatNames ?? new List<string>())
            {
                Feat feat = FeatList.All.FirstOrDefault(f => f.Name == featName);
                if (feat != null)
                    core.Feats.Add(feat);
            }

            ClassStats classStats = ClassList.ByName(s.ClassName);
            if (classStats != null)
            {
                core.InitializeHealth(classStats);
                if (s.CurrentHp.HasValue)
                    core.Health.Restore(s.CurrentHp.Value, s.Dying, s.Wounded, s.HeroPoints);
            }
            return core;
        }

        private static void LoadWeapons(CharacterCore core, CharacterSave s)
        {
            if (s.Weapons == null)
            {
                Equipment.Weapon weapon = WeaponList.All.FirstOrDefault(w => w.Name == s.WeaponName);
                if (weapon != null)
                {
                    core.EquippedWeapon = new CarriedWeapon(weapon);
                    core.Inventory.Add(core.EquippedWeapon);
                }
                return;
            }

            foreach (WeaponSave saved in s.Weapons)
            {
                Equipment.Weapon template = WeaponList.All.FirstOrDefault(w => w.Name == saved.Name);
                if (template != null)
                    core.Inventory.Add(new CarriedWeapon(template) { PotencyRune = saved.PotencyRune, StrikingRune = saved.StrikingRune });
            }
            if (s.EquippedWeaponIndex >= 0 && s.EquippedWeaponIndex < core.Inventory.Count)
                core.EquippedWeapon = core.Inventory[s.EquippedWeaponIndex];
        }
    }
}
