#if TOOLS
using Godot;

public partial class ImportEventer : EditorScenePostImportPlugin
{

    private string _filePath;

    public override void _GetImportOptions(string path)
    {
        _filePath = path;
    }

    public override void _PostProcess(Node scene)
    {
        GD.Print("Post process: ", scene.Name);
        PipelinesSingleton.Singleton(s => s.EmitSignal(PipelinesSingleton.SignalName.SceneImportPostProcess, _filePath));
    }

}
#endif