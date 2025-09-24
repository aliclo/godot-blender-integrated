#if TOOLS
using Godot;

public partial class ImportEventer : EditorScenePostImportPlugin
{

    public static ImportEventer Instance { get; private set; }

    private string _filePath;

    [Signal]
    public delegate void SceneImportUpdatedEventHandler(string filePath);
    private Pipelines _pipelines;

    public void Init(Pipelines pipelines)
    {
        _pipelines = pipelines;
        Instance = this;
    }

    public override void _GetImportOptions(string path)
    {
        GD.Print("Get import options");
        _filePath = path;
    }

    public override void _PostProcess(Node scene)
    {
        GD.Print("PostProcess");
        var sceneFilePath = _filePath;

        var gdTimer = new Timer();
        gdTimer.OneShot = true;
        gdTimer.Timeout += () => RaiseSceneImportUpdated(sceneFilePath);
        _pipelines.AddChild(gdTimer);
        gdTimer.Start(1);
    }

    private void RaiseSceneImportUpdated(string filePath)
    {
        GD.Print("RaiseSceneImportUpdated");
        EmitSignal(SignalName.SceneImportUpdated, filePath);
    }

}
#endif