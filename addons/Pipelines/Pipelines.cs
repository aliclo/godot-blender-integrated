#if TOOLS
using Godot;
using System;

[Tool]
public partial class Pipelines : EditorPlugin
{

	private Control _pipelineGraph;

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		_pipelineGraph = GD.Load<PackedScene>("res://addons/Pipelines/PipelineEditor.tscn").Instantiate<Control>();
		AddControlToBottomPanel(_pipelineGraph, "Pipeline Graph");
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		RemoveControlFromBottomPanel(_pipelineGraph);
		_pipelineGraph.QueueFree();
	}
}
#endif
