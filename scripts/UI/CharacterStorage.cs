using Godot;
using Tracefinder.Character;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public static class CharacterStorage
    {
        public static CharacterRepository Repository => new(ProjectSettings.GlobalizePath("user://characters"));

        // W kampanii postać jest częścią zapisu gry. Poza nią aktualizuje tylko postacie już zapisane,
        // żeby przykładowa postać nie tworzyła plików sama z siebie.
        public static void SaveIfAlreadyStored(CharacterCore character)
        {
            if (GameSession.Campaign != null)
            {
                CampaignStorage.Save();
                return;
            }

            CharacterRepository repository = Repository;
            if (repository.Exists(character.Name))
                repository.Save(character);
        }
    }
}
