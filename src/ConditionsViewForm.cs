using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class ConditionsViewForm : Form
    {
        public ConditionsViewForm(JtToken token)
        {
            InitializeComponent();

            Text = $"Condition View for {token.Name}";

            foreach (JtCondition? item in token.Conditions)
            {
                dataGridView.Rows.Add(item.VariableId, item.Type, item.Value, item.IgnoreCase);
            }

        }
    }
}
