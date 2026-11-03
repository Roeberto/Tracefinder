using Godot;
using Tracefinder.World;

namespace Tracefinder.UI
{
    public static class CampaignStorage
    {
        private static CampaignRepository Repository => new(ProjectSettings.GlobalizePath("user://campaign.json"));

        public static bool HasSave => Repository.Exists;

        public static void Save()
        {
            if (GameSession.Campaign != null)
                Repository.Save(GameSession.Campaign);
        }

        // Zwraca false, gdy zapisu nie ma albo jest uszkodzony
        public static bool Load()
        {
            Campaign campaign = Repository.Load();
            if (campaign == null)
                return false;

            GameSession.Campaign = campaign;
            GameSession.PendingPlan = null;
            return true;
        }
    }
}
