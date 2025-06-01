#if TOOLS
using Godot;
using System.Linq;

[Tool]
public partial class Pipelines : EditorPlugin
{

	private PipelineEditor _pipelineEditor;
	private PipeContext _context;
	private EditorSelection _selection;
	private PipelineAccess _pipelineAccess = new PipelineAccess();

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		_selection = EditorInterface.Singleton.GetSelection();
		_selection.SelectionChanged += OnSelectionChanged;
		SceneSaved += OnSave;
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		// TODO: Clear save history at this point
		SceneSaved -= OnSave;
		_selection.SelectionChanged -= OnSelectionChanged;
		if(_pipelineEditor != null) {
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
		var pipelineContext = (PipeContext) selectedNodes.FirstOrDefault(n => n is PipeContext);

		if (pipelineContext != null)
		{
			_context = pipelineContext;
			_context.TreeExiting += ClearContextAndPipelineGraph;
			_pipelineEditor = GD.Load<PackedScene>("res://addons/Pipelines/PipelineEditor.tscn").Instantiate<PipelineEditor>();

			_pipelineEditor.Ready += InitPipelineEditor;
			AddControlToBottomPanel(_pipelineEditor, "Pipeline Graph");
		}
	}

	private void InitPipelineEditor() {
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
}
#endif
