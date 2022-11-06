using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public interface ISuggestionSelector
    {
        public DialogResult Show(IJtSuggestion[] suggestions, bool forceUsingSuggestion, IJtSuggestion? selectedSuggestion = null);

        public IJtSuggestion? SelectedSuggestion { get; set; }
    }
}
