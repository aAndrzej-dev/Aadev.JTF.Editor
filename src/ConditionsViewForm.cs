using Aadev.JTF.Editor.EditorItems;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class ConditionsViewForm : Form
    {
        private readonly EditorItem editorItem;
        internal ConditionsViewForm(EditorItem editorItem)
        {
            this.editorItem = editorItem;
            InitializeComponent();

            Text = $"Condition View (beta) for {editorItem.Name}";

            foreach (JtCondition? item in editorItem.Node.Conditions)
            {

                int rowIndex = dataGridView.Rows.Add(item.VariableId, item.Type, item.Value, item.IgnoreCase);

                dataGridView.Rows[rowIndex].Tag = item.VariableId;
            }
            dataGridView.CellDoubleClick += DataGridView_CellDoubleClick;
        }

        private void DataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;
            string? id = dataGridView.Rows[e.RowIndex].Tag?.ToString();
            if (id is null)
            {
                MessageBox.Show("Something went wrong");
                return;
            }
            EditorItem? ei = editorItem.EventManager.GetEvent(id)?.EditorItem;

            if (ei is null)
            {
                MessageBox.Show("Something went wrong");
                return;
            }

            Close();
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                ei.Invoke(new Helpers.ControlDelegate(() => ei?.Focus()));
            });


        }
    }
}
