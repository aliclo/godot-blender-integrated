#if TOOLS
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using static SceneModelNode;

[Tool]
public partial class Pipelines : EditorPlugin
{

	private const string ADDON_PATH = "res://addons/Pipelines";

	private PipelineEditor _pipelineEditor;
	private PipeContext _activeContext;
	private EditorSelection _selection;
    private PipelineAccess _pipelineAccess = new PipelineAccess();
	private ImportEventer _importEventer;

	public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
        AddAutoloadSingleton("PipelinesSingleton", $"{ADDON_PATH}/{nameof(PipelinesSingleton)}.tscn");
        _importEventer = new ImportEventer();
        var pipeContextScript = GD.Load<CSharpScript>($"{ADDON_PATH}/{nameof(PipeContext)}.cs");
        var pipeContextIcon = GD.Load<Texture2D>($"{ADDON_PATH}/{nameof(PipeContext)}.png");
        AddCustomType(nameof(PipeContext), nameof(Node), pipeContextScript, pipeContextIcon);
        AddScenePostImportPlugin(_importEventer);
        PipelinesSingleton.Singleton(s => s.SceneImportUpdated += SavePipelineScenes);
        _selection = EditorInterface.Singleton.GetSelection();
        _selection.SelectionChanged += OnSelectionChanged;
        PipelinesSingleton.Singleton(s => SceneSaved += s.OnSave);
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
        // TODO: Clear save history at this point
        PipelinesSingleton.Singleton(s => s.SceneImportUpdated -= SavePipelineScenes);
        RemoveScenePostImportPlugin(_importEventer);
        RemoveCustomType(nameof(PipeContext));
        PipelinesSingleton.Singleton(s => SceneSaved -= s.OnSave);
        _selection.SelectionChanged -= OnSelectionChanged;
        if (_pipelineEditor != null)
        {
            ClearContextAndPipelineGraph();
        }
        RemoveAutoloadSingleton("PipelinesSingleton");
	}

	private void OnSelectionChanged()
	{
		var selectedNodes = _selection.GetSelectedNodes();
		var pipelineContext = (PipeContext)selectedNodes.FirstOrDefault(n => n is PipeContext);

		if (pipelineContext != null)
		{
            GD.Print("Pipelines loading pipeline context: ", pipelineContext);
            GD.Print("Output nodes: ", pipelineContext.OutputNodes);
			if (_pipelineEditor != null)
            {
                ClearContextAndPipelineGraph();
            }

			_activeContext = pipelineContext;
			_activeContext.TreeExiting += ClearContextAndPipelineGraph;
			_pipelineEditor = GD.Load<PackedScene>($"{ADDON_PATH}/PipelineEditor.tscn").Instantiate<PipelineEditor>();

			_pipelineEditor.Ready += InitPipelineEditor;
			AddControlToBottomPanel(_pipelineEditor, "Pipeline Graph");
		}
	}

	private void InitPipelineEditor()
	{
        _pipelineEditor.Ready -= InitPipelineEditor;
		_pipelineEditor.PipelineGraph.UndoRedo = GetUndoRedo();
		_pipelineEditor.PipelineGraph.OnLoadContext(_activeContext);
	}

	private void ClearContextAndPipelineGraph()
	{
		_activeContext.TreeExiting -= ClearContextAndPipelineGraph;
		_pipelineAccess.Write(_activeContext.Owner.SceneFilePath, _activeContext);
		_pipelineEditor.PipelineGraph.Cleanup();
        GD.Print("Removing pipeline editor: ", _pipelineEditor);
		RemoveControlFromBottomPanel(_pipelineEditor);
		_pipelineEditor.QueueFree();
		_pipelineEditor = null;
		_activeContext = null;
	}

	private void SavePipelineScenes(string fileName)
	{
        GD.Print("Saving scene! ", fileName);
		var openedScenes = EditorInterface.Singleton.GetOpenScenes();

		var pipeFiles = FindFilesEndingWith("res://", ".pipelines.json");
		var sceneFilePaths = pipeFiles
			.Select(p => p.Substring(0, p.Length - ".pipelines.json".Length))
			.Select(p => new { Path = p, PipelineStore = _pipelineAccess.Read(p) })
			.Select(p => new { p.Path, SceneModelNodes = p.PipelineStore.SelectMany(p => p.Nodes.Where(n => n.Type == nameof(SceneModelNode))) })
			.Select(p => new { p.Path, SceneModelDataNodes = p.SceneModelNodes.Select(n => GodotJsonParser.FromJsonType<SceneModelNodeStore>(n.Data)) })
			.Where(p => p.SceneModelDataNodes.Any(smn => smn.ChosenScene == fileName))
			.Select(p => p.Path)
			.ToList();

		foreach (var sceneFilePath in sceneFilePaths)
		{
			EditorInterface.Singleton.OpenSceneFromPath(sceneFilePath);
			// TODO: This is pointless as it doesn't run yet at the time of context running
			EditorInterface.Singleton.SaveScene();
		}
	}

	private IEnumerable<string> FindFilesEndingWith(string dirPath, string str)
	{
		var dir = DirAccess.Open(dirPath);
		var files = dir.GetDirectories()
			.Where(d => d != ".godot")
			.SelectMany(d => FindFilesEndingWith($"{dirPath}{d}/", str));

		var dirFiles = dir
			.GetFiles()
			.Where(f => f.EndsWith(str))
			.Select(f => $"{dirPath}{f}");

		return files.Concat(dirFiles);
	}
}
#endif
