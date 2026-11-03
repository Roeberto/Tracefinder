using Godot;
using Tracefinder.Character;

namespace Tracefinder.UI
{
    public partial class SaveRow : HBoxContainer
    {
        public void SetValues(ProficiencyRank rank, string saveName, int bonus)
        {
            GetNode<Label>("Rank").Text = UiText.RankLetter(rank);
            GetNode<Label>("SaveName").Text = saveName;
            GetNode<Label>("Bonus").Text = UiText.Signed(bonus);
        }
    }
}
