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
        _filePath = path;
    }

    public override void _PostProcess(Node scene)
    {
        var sceneFilePath = _filePath;

        var gdTimer = new Timer();
        gdTimer.OneShot = true;
        gdTimer.Timeout += () => RaiseSceneImportUpdated(sceneFilePath);
        _pipelines.AddChild(gdTimer);
        gdTimer.Start(1);
    }

    private void RaiseSceneImportUpdated(string filePath)
    {
        EmitSignal(SignalName.SceneImportUpdated, filePath);
    }

}
#endif