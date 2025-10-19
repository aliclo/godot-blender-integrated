#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class PipelinesSingleton : Node
{

    private static PipelinesSingleton _instance;
    private static List<Action<PipelinesSingleton>> _pipelinesSingletonActions = new List<Action<PipelinesSingleton>>();

    public static void Singleton(Action<PipelinesSingleton> action)
    {
        if (_instance == null)
        {
            _pipelinesSingletonActions.Add(action);
        }
        else
        {
            action(_instance);
        }
    }

    private Array<PipeContext> _pipeContexts = new Array<PipeContext>();
    private PipelineAccess _pipelineAccess = new PipelineAccess();

    [Signal]
    public delegate void SceneImportPostProcessEventHandler(string filePath);

    [Signal]
    public delegate void SceneImportUpdatedEventHandler(string filePath);

    public override void _Ready()
    {
        _instance = this;
        SceneImportPostProcess += OnSceneImportPostProcess;

        foreach (var action in _pipelinesSingletonActions)
        {
            action(_instance);
        }
        _pipelinesSingletonActions.Clear();
    }

    public void RegisterContext(PipeContext context)
    {
        _pipeContexts.Add(context);
    }

    public void UnregisterContext(PipeContext context)
    {
        _pipeContexts.Remove(context);
    }

    public void OnSave(string filePath)
    {
        var filePipeContexts = _pipeContexts.Where(c => c.Owner.SceneFilePath == filePath);
        foreach (var context in filePipeContexts)
        {
            _pipelineAccess.Write(context.Owner.SceneFilePath, context);
        }
    }

    private void OnSceneImportPostProcess(string filePath)
    {
        var timer = new Timer();
        timer.OneShot = true;
        timer.Timeout += () => {
            EmitSignal(SignalName.SceneImportUpdated, filePath);
            RemoveChild(timer);
            timer.QueueFree();
        };
        AddChild(timer);
        timer.Start(1);
    }

}
#endif