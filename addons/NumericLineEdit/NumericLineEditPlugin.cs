#if TOOLS
using Godot;
using System;

[Tool]
public partial class NumericLineEditPlugin : EditorPlugin
{

	private const string ADDON_PATH = "res://addons/NumericLineEdit";
	private const string NUMERICAL_LINE_EDIT_NAME = nameof(NumericLineEdit);

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		var numericalLineEditScript = GD.Load<CSharpScript>($"{ADDON_PATH}/{NUMERICAL_LINE_EDIT_NAME}.cs");
		var numericalLineEditIcon = GD.Load<Texture2D>($"{ADDON_PATH}/{NUMERICAL_LINE_EDIT_NAME}.png");
		AddCustomType(NUMERICAL_LINE_EDIT_NAME, nameof(LineEdit), numericalLineEditScript, numericalLineEditIcon);
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		RemoveCustomType(NUMERICAL_LINE_EDIT_NAME);
	}
}
#endif
