using System.Drawing;

namespace Aadev.JTF.Editor;

public sealed class JtColorTable
{
    internal SolidBrush SelectedNodeTypeBackBrush { get; } = new SolidBrush(Color.RoyalBlue);
    internal SolidBrush ExpandButtonBackBrush { get; } = new SolidBrush(Color.Green);
    internal Pen ExpandButtonForePen { get; } = new Pen(Color.White);
    internal SolidBrush AddItemButtonBackBrush { get; } = new SolidBrush(Color.Green);
    internal Pen AddItemButtonForePen { get; } = new Pen(Color.White);
    internal SolidBrush RemoveItemButtonBackBrush { get; } = new SolidBrush(Color.Red);
    internal Pen RemoveItemButtonForePen { get; } = new Pen(Color.White);
    internal SolidBrush TextBoxBackBrush { get; } = new SolidBrush(Color.FromArgb(80, 80, 80));
    internal SolidBrush NameForeBrush { get; } = new SolidBrush(Color.White);
    internal SolidBrush TextBoxForeBrush { get; } = new SolidBrush(Color.White);
    internal SolidBrush InvalidElementForeBrush { get; } = new SolidBrush(Color.Red);
    internal SolidBrush ActiveBorderBrush { get; } = new SolidBrush(Color.DarkCyan);
    internal SolidBrush InactiveBorderBrush { get; } = new SolidBrush(Color.FromArgb(200, 200, 200));
    internal SolidBrush InvalidBorderBrush { get; } = new SolidBrush(Color.Red);
    internal SolidBrush WarningBorderBrush { get; } = new SolidBrush(Color.Yellow);
    internal SolidBrush RequiredStarBrush { get; } = new SolidBrush(Color.Gold);
    internal SolidBrush TrueValueBackBrush { get; } = new SolidBrush(Color.Green);
    internal SolidBrush TrueValueForeBrush { get; } = new SolidBrush(Color.White);
    internal SolidBrush FalseValueBackBrush { get; } = new SolidBrush(Color.Red);
    internal SolidBrush FalseValueForeBrush { get; } = new SolidBrush(Color.White);
    internal SolidBrush WarningValueBrush { get; } = new SolidBrush(Color.Yellow);
    internal SolidBrush InvalidValueBrush { get; } = new SolidBrush(Color.Red);
    internal SolidBrush DefaultElementForeBrush { get; } = new SolidBrush(Color.LightGray);
    internal SolidBrush DiscardInvalidValueButtonBackBrush { get; } = new SolidBrush(Color.Red);
    internal SolidBrush DiscardInvalidValueButtonForeBrush { get; } = new SolidBrush(Color.White);
    internal SolidBrush RestoreDefaultValueButtonBackBrush { get; } = new SolidBrush(Color.Red);
    internal Pen RestoreDefaultValueButtonForePen { get; } = new Pen(Color.White, 1);


    public Color SelectedNodeTypeBackColor { get => SelectedNodeTypeBackBrush.Color; set => SelectedNodeTypeBackBrush.Color = value; }
    public Color ExpandButtonBackColor { get => ExpandButtonBackBrush.Color; set => ExpandButtonBackBrush.Color = value; }
    public Color ExpandButtonForeColor { get => ExpandButtonForePen.Color; set => ExpandButtonForePen.Color = value; }
    public Color AddItemButtonBackColor { get => AddItemButtonBackBrush.Color; set => AddItemButtonBackBrush.Color = value; }
    public Color AddItemButtonForeColor { get => AddItemButtonForePen.Color; set => AddItemButtonForePen.Color = value; }
    public Color RemoveItemButtonBackColor { get => RemoveItemButtonBackBrush.Color; set => RemoveItemButtonBackBrush.Color = value; }
    public Color RemoveItemButtonForeColor { get => RemoveItemButtonForePen.Color; set => RemoveItemButtonForePen.Color = value; }
    public Color TextBoxBackColor { get => TextBoxBackBrush.Color; set => TextBoxBackBrush.Color = value; }
    public Color TextBoxForeColor { get => TextBoxForeBrush.Color; set => TextBoxForeBrush.Color = value; }
    public Color NameForeColor { get => NameForeBrush.Color; set => NameForeBrush.Color = value; }
    public Color InvalidElementForeColor { get => InvalidElementForeBrush.Color; set => InvalidElementForeBrush.Color = value; }
    public Color ActiveBorderColor { get => ActiveBorderBrush.Color; set => ActiveBorderBrush.Color = value; }
    public Color InactiveBorderColor { get => InactiveBorderBrush.Color; set => InactiveBorderBrush.Color = value; }
    public Color InvalidBorderColor { get => InvalidBorderBrush.Color; set => InvalidBorderBrush.Color = value; }
    public Color WarningBorderColor { get => WarningBorderBrush.Color; set => WarningBorderBrush.Color = value; }
    public Color RequiredStarColor { get => RequiredStarBrush.Color; set => RequiredStarBrush.Color = value; }
    public Color TrueValueBackColor { get => TrueValueBackBrush.Color; set => TrueValueBackBrush.Color = value; }
    public Color TrueValueForeColor { get => TrueValueForeBrush.Color; set => TrueValueForeBrush.Color = value; }
    public Color FalseValueBackColor { get => FalseValueBackBrush.Color; set => FalseValueBackBrush.Color = value; }
    public Color FalseValueForeColor { get => FalseValueForeBrush.Color; set => FalseValueForeBrush.Color = value; }
    public Color WarningValueColor { get => WarningValueBrush.Color; set => WarningValueBrush.Color = value; }
    public Color InvalidValueColor { get => InvalidValueBrush.Color; set => InvalidValueBrush.Color = value; }
    public Color DefaultElementForeColor { get => DefaultElementForeBrush.Color; set => DefaultElementForeBrush.Color = value; }
    public Color DiscardInvalidValueButtonBackColor { get => DiscardInvalidValueButtonBackBrush.Color; set => DiscardInvalidValueButtonBackBrush.Color = value; }
    public Color DiscardInvalidValueButtonForeColor { get => DiscardInvalidValueButtonForeBrush.Color; set => DiscardInvalidValueButtonForeBrush.Color = value; }
    public Color RestoreDefaultValueButtonBackColor { get => RestoreDefaultValueButtonBackBrush.Color; set => RestoreDefaultValueButtonBackBrush.Color = value; }
    public Color RestoreDefaultValueButtonForeColor { get => RestoreDefaultValueButtonForePen.Color; set => RestoreDefaultValueButtonForePen.Color = value; }
    public JtColorTable()
    {

    }
    public JtColorTable(Color selectedNodeTypeBackColor, Color expandButtonBackColor, Color expandButtonForeColor, Color addItemButtonBackColor, Color addItemButtonForeColor, Color removeItemButtonBackColor, Color removeItemButtonForeColor, Color textBoxBackColor, Color textBoxForeColor, Color invalidElementForeColor, Color activeBorderColor, Color inactiveBorderColor, Color invalidBorderColor, Color warningBorderColor, Color requiredStarColor, Color trueValueBackColor, Color trueValueForeColor, Color falseValueBackColor, Color falseValueForeColor, Color warningValueColor, Color invalidValueColor, Color defaultElementForeColor, Color discardInvalidValueButtonBackColor, Color discardInvalidValueButtonForeColor)
    {
        SelectedNodeTypeBackColor = selectedNodeTypeBackColor;
        ExpandButtonBackColor = expandButtonBackColor;
        ExpandButtonForeColor = expandButtonForeColor;
        AddItemButtonBackColor = addItemButtonBackColor;
        AddItemButtonForeColor = addItemButtonForeColor;
        RemoveItemButtonBackColor = removeItemButtonBackColor;
        RemoveItemButtonForeColor = removeItemButtonForeColor;
        TextBoxBackColor = textBoxBackColor;
        TextBoxForeColor = textBoxForeColor;
        InvalidElementForeColor = invalidElementForeColor;
        ActiveBorderColor = activeBorderColor;
        InactiveBorderColor = inactiveBorderColor;
        InvalidBorderColor = invalidBorderColor;
        WarningBorderColor = warningBorderColor;
        RequiredStarColor = requiredStarColor;
        TrueValueBackColor = trueValueBackColor;
        TrueValueForeColor = trueValueForeColor;
        FalseValueBackColor = falseValueBackColor;
        FalseValueForeColor = falseValueForeColor;
        WarningValueColor = warningValueColor;
        InvalidValueColor = invalidValueColor;
        DefaultElementForeColor = defaultElementForeColor;
        DiscardInvalidValueButtonBackColor = discardInvalidValueButtonBackColor;
        DiscardInvalidValueButtonForeColor = discardInvalidValueButtonForeColor;
    }

    public static JtColorTable Default { get; } = new JtColorTable();
}
