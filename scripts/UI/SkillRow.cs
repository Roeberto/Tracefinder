using Godot;
using Tracefinder.Character;

namespace Tracefinder.UI
{
    public partial class SkillRow : HBoxContainer
    {
        private static readonly Color Trained = new(0.91f, 0.36f, 0.38f);
        private static readonly Color Untrained = new(0.62f, 0.64f, 0.66f);

        public void SetValues(ProficiencyRank rank, string ability, string skillName, int bonus)
        {
            Label rankLabel = GetNode<Label>("Rank");
            rankLabel.Text = UiText.RankLetter(rank);
            rankLabel.AddThemeColorOverride("font_color", rank == ProficiencyRank.Untrained ? Untrained : Trained);
            GetNode<Label>("Ability").Text = ability;
            GetNode<Label>("SkillName").Text = skillName;
            GetNode<Label>("Bonus").Text = UiText.Signed(bonus);
        }
    }
}
