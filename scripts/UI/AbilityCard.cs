using Godot;

namespace Tracefinder.UI
{
    public partial class AbilityCard : PanelContainer
    {
        public void SetValues(string abilityName, int score, int modifier)
        {
            GetNode<Label>("Box/AbilityName").Text = abilityName;
            GetNode<Label>("Box/Modifier").Text = UiText.Signed(modifier);
            GetNode<Label>("Box/Score").Text = score.ToString();
        }
    }
}
