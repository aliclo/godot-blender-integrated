using System.Timers;
using Godot;

public partial class ImportEventer : EditorScenePostImportPlugin
{

    public static ImportEventer Instance { get; private set; }

    public delegate void SceneImportUpdatedEventHandler();
    public event SceneImportUpdatedEventHandler SceneImportUpdated;

    public void Init()
    {
        Instance = this;
    }

    public override void _PostProcess(Node scene)
    {
        var timer = new System.Timers.Timer();
        timer.Elapsed += (object sender, ElapsedEventArgs e) => CallDeferred(MethodName.RaiseSceneImportUpdated);
        timer.AutoReset = false;
        timer.Interval = 0.5;
        timer.Start();
    }

    private void RaiseSceneImportUpdated()
    {
        SceneImportUpdated();
    }
    
}