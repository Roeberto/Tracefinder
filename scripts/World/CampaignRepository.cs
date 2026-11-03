using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracefinder.World
{
    // Jeden automatyczny zapis gry, nadpisywany w trakcie przygody
    public class CampaignRepository
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _path;

        public CampaignRepository(string path)
        {
            _path = path;
        }

        public bool Exists => File.Exists(_path);

        public void Save(Campaign campaign)
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // Najpierw plik tymczasowy, żeby przerwany zapis nie zniszczył poprzedniego
            string temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(campaign.ToSave(), Options));
            File.Move(temporary, _path, true);
        }

        // Zwraca null, gdy zapisu nie ma albo jest uszkodzony
        public Campaign Load()
        {
            if (!Exists)
                return null;

            try
            {
                CampaignSave save = JsonSerializer.Deserialize<CampaignSave>(File.ReadAllText(_path), Options);
                return save?.Hero == null ? null : Campaign.FromSave(save);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
