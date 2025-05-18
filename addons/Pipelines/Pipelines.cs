#if TOOLS
using Godot;
using System;
using System.Linq;

[Tool]
public partial class Pipelines : EditorPlugin
{

	private PipelineEditor _pipelineEditor;
	private string _pipelineContextPath;
	private string _pipelineContextName;
	private EditorSelection _selection;
	private PipelineAccess _pipelineAccess = new PipelineAccess();

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		_selection = EditorInterface.Singleton.GetSelection();
		_selection.SelectionChanged += OnSelectionChanged;
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		_selection.SelectionChanged -= OnSelectionChanged;
		if(_pipelineEditor != null) {
			ClearPipelineGraph();
		}
	}

	public void OnSelectionChanged()
	{
		if(_pipelineEditor != null) {
			ClearPipelineGraph();
		}

		var selectedNodes = _selection.GetSelectedNodes();
		var pipelineContext = selectedNodes.FirstOrDefault(n => n is PipeContext);

		if (pipelineContext != null)
		{
			_pipelineContextPath = pipelineContext.Owner.SceneFilePath;
			_pipelineContextName = pipelineContext.Name;
			_pipelineEditor = GD.Load<PackedScene>("res://addons/Pipelines/PipelineEditor.tscn").Instantiate<PipelineEditor>();

			_pipelineEditor.Ready += InitPipelineEditor;
			AddControlToBottomPanel(_pipelineEditor, "Pipeline Graph");
		}
	}

	private void InitPipelineEditor() {
		var pipelineContextStores = _pipelineAccess.Read(_pipelineContextPath);
		var pipelineContextStore = pipelineContextStores?.SingleOrDefault(pcs => pcs.Name == _pipelineContextName);
		_pipelineEditor.PipelineGraph.Load(pipelineContextStore);
		_pipelineEditor.PipelineGraph.ContextName = _pipelineContextName;
		_pipelineEditor.Ready -= InitPipelineEditor;
	}

	private void ClearPipelineGraph()
	{
		// TODO: Write every time a change is made
		_pipelineAccess.Write(_pipelineContextPath, _pipelineEditor.PipelineGraph);
		RemoveControlFromBottomPanel(_pipelineEditor);
		_pipelineEditor.QueueFree();
		_pipelineEditor = null;
		_pipelineContextPath = null;
		_pipelineContextName = null;
	}
}
#endif
