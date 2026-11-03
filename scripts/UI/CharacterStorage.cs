using Godot;
using Tracefinder.Character;

namespace Tracefinder.UI
{
    public static class CharacterStorage
    {
        public static CharacterRepository Repository => new(ProjectSettings.GlobalizePath("user://characters"));

        // Aktualizuje tylko postacie już zapisane, żeby przykładowa postać nie tworzyła plików sama z siebie
        public static void SaveIfAlreadyStored(CharacterCore character)
        {
            CharacterRepository repository = Repository;
            if (repository.Exists(character.Name))
                repository.Save(character);
        }
    }
}
