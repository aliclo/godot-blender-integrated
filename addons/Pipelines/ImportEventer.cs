using Godot;

public partial class ImportEventer : EditorScenePostImportPlugin
{

    public static ImportEventer Instance { get; private set; }

    public delegate void SceneImportUpdatedEventHandler();
    public event SceneImportUpdatedEventHandler SceneImportUpdated;
    private Pipelines _pipelines;

    public void Init(Pipelines pipelines)
    {
        _pipelines = pipelines;
        Instance = this;
    }

    public override void _PostProcess(Node scene)
    {
        var gdTimer = new Timer();
        gdTimer.OneShot = true;
        gdTimer.Timeout += RaiseSceneImportUpdated;
        _pipelines.AddChild(gdTimer);
        gdTimer.Start(1);
    }

    private void RaiseSceneImportUpdated()
    {
        SceneImportUpdated();
    }
    
}