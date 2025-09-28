#if TOOLS
using Godot;

public partial class ImportEventer : EditorScenePostImportPlugin
{

    public static ImportEventer Instance { get; private set; }

    private Timer _timer;
    private string _filePath;

    [Signal]
    public delegate void SceneImportUpdatedEventHandler(string filePath);
    private Pipelines _pipelines;

    public void Init(Pipelines pipelines)
    {
        _pipelines = pipelines;
        Instance = this;
        _timer = new Timer();
        _timer.OneShot = true;
    }

    public override void _GetImportOptions(string path)
    {
        _filePath = path;
    }

    public override void _PostProcess(Node scene)
    {
        // Warning: If an import is done too close to another, then this will not catch the others until the first one is completed after the delay
        // This means that when another is done that would need more time, not enough time will be given
        if (_timer != null)
        {
            GD.Print("WARNING: Already handling import request so won't send another");
        }
        else
        {
            _timer.Timeout += RaiseSceneImportUpdated;
            _pipelines.AddChild(_timer);
            _timer.Start(1);
        }
    }

    private void RaiseSceneImportUpdated()
    {
        _timer.Timeout -= RaiseSceneImportUpdated;
        EmitSignal(SignalName.SceneImportUpdated, _filePath);
        _pipelines.RemoveChild(_timer);
    }

}
#endif