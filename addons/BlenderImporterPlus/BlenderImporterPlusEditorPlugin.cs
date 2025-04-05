#if TOOLS
using Godot;
using System;

[Tool]
public partial class BlenderImporterPlusEditorPlugin : EditorPlugin
{
	private static BlenderImporterPlusImportPlugin _blenderImporterPlusImportPlugin;

    public override void _EnterTree()
    {
        if(_blenderImporterPlusImportPlugin == null) {
            _blenderImporterPlusImportPlugin = new BlenderImporterPlusImportPlugin();
        }

        AddScenePostImportPlugin(_blenderImporterPlusImportPlugin);
    }

    public override void _ExitTree()
    {
        RemoveScenePostImportPlugin(_blenderImporterPlusImportPlugin);
    }

}
#endif
