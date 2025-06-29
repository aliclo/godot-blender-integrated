#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class Pipelines : EditorPlugin
{

	private const string ADDON_PATH = "res://addons/Pipelines";

	private PipelineEditor _pipelineEditor;
	private PipeContext _context;
	private EditorSelection _selection;
	private PipelineAccess _pipelineAccess = new PipelineAccess();
	private ImportEventer _importEventer;

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		_importEventer = new ImportEventer();
		_importEventer.Init();
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

	public void OnSave(string filePath)
	{
		if (_context != null && filePath == _context.Owner.SceneFilePath)
		{
			_pipelineAccess.Write(_context.Owner.SceneFilePath, _context);
		}
	}

	public void OnSelectionChanged()
	{
		if (_pipelineEditor != null)
		{
			ClearContextAndPipelineGraph();
		}

		var selectedNodes = _selection.GetSelectedNodes();
		var pipelineContext = (PipeContext)selectedNodes.FirstOrDefault(n => n is PipeContext);

		if (pipelineContext != null)
		{
			_context = pipelineContext;
			_context.TreeExiting += ClearContextAndPipelineGraph;
			_pipelineEditor = GD.Load<PackedScene>($"{ADDON_PATH}/PipelineEditor.tscn").Instantiate<PipelineEditor>();

			_pipelineEditor.Ready += InitPipelineEditor;
			AddControlToBottomPanel(_pipelineEditor, "Pipeline Graph");
		}
	}

	private void InitPipelineEditor()
	{
		var pipelineContextStores = _pipelineAccess.Read(_context.Owner.SceneFilePath);
		var pipelineContextStore = pipelineContextStores?.SingleOrDefault(pcs => pcs.Name == _context.Name);
		_pipelineEditor.PipelineGraph.UndoRedo = GetUndoRedo();
		_pipelineEditor.PipelineGraph.OnLoadContext(_context);
		_pipelineEditor.Ready -= InitPipelineEditor;
	}

	private void ClearContextAndPipelineGraph()
	{
		_context.TreeExiting -= ClearContextAndPipelineGraph;
		_pipelineAccess.Write(_context.Owner.SceneFilePath, _context);
		_pipelineEditor.PipelineGraph.Cleanup();
		RemoveControlFromBottomPanel(_pipelineEditor);
		_pipelineEditor.QueueFree();
		_pipelineEditor = null;
		_context = null;
	}

	private void SavePipelineScenes()
	{
		var openedScenes = EditorInterface.Singleton.GetOpenScenes();

		var pipeFiles = FindFilesEndingWith("res://", ".pipelines.json");
		var sceneFilePaths = pipeFiles
			.Select(p => p.Substring(0, p.Length - ".pipelines.json".Length))
			.ToList();

		foreach (var sceneFilePath in sceneFilePaths)
		{
			CallDeferred(MethodName.SaveScene, sceneFilePath);
		}
	}

	private void SaveScene(string sceneFilePath)
	{
		EditorInterface.Singleton.OpenSceneFromPath(sceneFilePath);
		EditorInterface.Singleton.SaveScene();
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
