#if TOOLS
using Godot;
using System;

[Tool]
public partial class EditorScenePostImportPlusPlugin : EditorPlugin
{

	private static EditorScenePostImportPluginPlus _editorScenePostImportPluginPlus;

	public override void _EnterTree()
	{
		if(_editorScenePostImportPluginPlus == null) {
            _editorScenePostImportPluginPlus = new EditorScenePostImportPluginPlus();
        }

        AddScenePostImportPlugin(_editorScenePostImportPluginPlus);
	}

	public override void _ExitTree()
	{
		RemoveScenePostImportPlugin(_editorScenePostImportPluginPlus);
	}
}
#endif
