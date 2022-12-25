using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public interface ISuggestionSelector
    {
        DialogResult Show(IJtSuggestion[] suggestions, bool forceUsingSuggestion, IJtSuggestion? selectedSuggestion = null);
        IJtSuggestion? SelectedSuggestion { get; set; }
    }
}
