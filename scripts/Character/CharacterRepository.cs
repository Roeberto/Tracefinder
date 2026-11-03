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
        List<string> FeatNames);

    // Zapis postaci jako pliki JSON. Stan HP nie jest zapisywany, wczytana postać jest w pełni sił.
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
                c.Feats.Select(f => f.Name).ToList());
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
                Ancestry = AncestryList.All.FirstOrDefault(a => a.Name == s.AncestryName)
            };
            core.ExtraTrainedSkills.AddRange(s.ExtraTrainedSkills ?? new List<SkillName>());

            Equipment.Weapon weapon = WeaponList.All.FirstOrDefault(w => w.Name == s.WeaponName);
            if (weapon != null)
            {
                core.EquippedWeapon = new CarriedWeapon(weapon);
                core.Inventory.Add(core.EquippedWeapon);
            }

            foreach (string featName in s.FeatNames ?? new List<string>())
            {
                Feat feat = FeatList.All.FirstOrDefault(f => f.Name == featName);
                if (feat != null)
                    core.Feats.Add(feat);
            }

            ClassStats classStats = ClassList.ByName(s.ClassName);
            if (classStats != null)
                core.InitializeHealth(classStats);
            return core;
        }
    }
}
