#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;
using static SceneModelNode;

[Tool]
public partial class Pipelines : EditorPlugin
{

	private const string ADDON_PATH = "res://addons/Pipelines";

	public static Pipelines Instance;

	private PipelineEditor _pipelineEditor;
	private PipeContext _activeContext;
	private EditorSelection _selection;
	private PipelineAccess _pipelineAccess = new PipelineAccess();
	private ImportEventer _importEventer;
	private List<PipeContext> _pipeContexts = new List<PipeContext>();

    public Pipelines()
    {
        Instance = this;

        _importEventer = new ImportEventer();
        _importEventer.Init(this);
    }

	public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
        _importEventer.SceneImportUpdated += SavePipelineScenes;
        var pipeContextScript = GD.Load<CSharpScript>($"{ADDON_PATH}/{nameof(PipeContext)}.cs");
        var pipeContextIcon = GD.Load<Texture2D>($"{ADDON_PATH}/{nameof(PipeContext)}.png");
        AddCustomType(nameof(PipeContext), nameof(Node), pipeContextScript, pipeContextIcon);
        AddScenePostImportPlugin(_importEventer);
        _selection = EditorInterface.Singleton.GetSelection();
        _selection.SelectionChanged += OnSelectionChanged;
        SceneSaved += OnSave;
    }

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		// TODO: Clear save history at this point
		Instance = null;
		_importEventer.SceneImportUpdated -= SavePipelineScenes;
		RemoveScenePostImportPlugin(_importEventer);
		RemoveCustomType(nameof(PipeContext));
		SceneSaved -= OnSave;
		_selection.SelectionChanged -= OnSelectionChanged;
		if (_pipelineEditor != null)
		{
			ClearContextAndPipelineGraph();
		}
	}

	public void RegisterContext(PipeContext context)
	{
		_pipeContexts.Add(context);
	}

	public void UnregisterContext(PipeContext context)
	{
		_pipeContexts.Remove(context);
	}

	private void OnSave(string filePath)
	{
		var filePipeContexts = _pipeContexts.Where(c => c.Owner.SceneFilePath == filePath);
		foreach (var context in filePipeContexts)
		{
			_pipelineAccess.Write(context.Owner.SceneFilePath, context);
		}
	}

	private void OnSelectionChanged()
	{
		var selectedNodes = _selection.GetSelectedNodes();
		var pipelineContext = (PipeContext)selectedNodes.FirstOrDefault(n => n is PipeContext);

		if (pipelineContext != null)
		{
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
		_pipelineEditor.PipelineGraph.UndoRedo = GetUndoRedo();
		_pipelineEditor.PipelineGraph.OnLoadContext(_activeContext);
		_pipelineEditor.Ready -= InitPipelineEditor;
	}

	private void ClearContextAndPipelineGraph()
	{
		_activeContext.TreeExiting -= ClearContextAndPipelineGraph;
		_pipelineAccess.Write(_activeContext.Owner.SceneFilePath, _activeContext);
		_pipelineEditor.PipelineGraph.Cleanup();
		RemoveControlFromBottomPanel(_pipelineEditor);
		_pipelineEditor.QueueFree();
		_pipelineEditor = null;
		_activeContext = null;
	}

	private void SavePipelineScenes(string fileName)
	{
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
