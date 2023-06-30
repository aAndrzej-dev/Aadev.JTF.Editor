using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor;

internal class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => Color.FromArgb(30, 30, 30);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(100, 100, 100);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(100, 100, 100);
    public override Color MenuStripGradientBegin => Color.FromArgb(80, 80, 80);
    public override Color MenuStripGradientEnd => Color.FromArgb(80, 80, 80);
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(80, 80, 80);
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(80, 80, 80);
    public override Color ToolStripDropDownBackground => Color.FromArgb(80, 80, 80);
    public override Color MenuItemSelected => Color.FromArgb(100, 100, 100);
    public override Color MenuItemPressedGradientMiddle => Color.FromArgb(80, 80, 80);
    public override Color MenuItemBorder => Color.FromArgb(150, 150, 150);
}