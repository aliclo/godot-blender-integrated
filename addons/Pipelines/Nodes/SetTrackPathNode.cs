#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class SetTrackPathNode : PipelineNode, IReceivePipe
{

    private partial class SetTrackPathNodeStore : GodotObject
    {

        [Export]
        public string TrackPath { get; set; }

    }

    // TODO: Use this from the context instead of having to provide it to the context
    private static readonly List<string> TOUCHED_PROPERTIES = new List<string>() {
        
    };

    private SetTrackPathNodeStore _setTrackPathNodeStore;
    private PipeContext _context;

    private Button _outputNodePicker;

    private AnimationPlayer _animationPlayer;
    private string _nodeName;
    private Node _targetNode;

    public List<IReceivePipe> NextPipes { get; set; } = new List<IReceivePipe>();
    private List<List<IReceivePipe>> _nodeConnections;
    public override List<List<IReceivePipe>> NodeConnections => _nodeConnections;

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new SetTrackPathNodeStore()
        {
            TrackPath = _context.RootNode.GetPathTo(_targetNode)
        });
    }

    public override void Load(Variant data)
    {
        _setTrackPathNodeStore = GodotJsonParser.FromJsonType<SetTrackPathNodeStore>(data);
    }

    public override void Init(PipeContext context)
    {
        _context = context;

        _nodeConnections = Enumerable.Range(0, 1)
            .Select(n => new List<IReceivePipe>())
            .ToList();

        var animationNodeContainer = new HBoxContainer();
        AddChild(animationNodeContainer);

        SetSlotEnabledLeft(0, true);
        SetSlotTypeLeft(0, (int)PipelineNodeTypes.Any);
        SetSlotColorLeft(0, TypeConnectorColors.ANY);
        var inputAnimationLabel = new Label();
        inputAnimationLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        inputAnimationLabel.HorizontalAlignment = HorizontalAlignment.Left;
        inputAnimationLabel.Text = "Ani";
        animationNodeContainer.AddChild(inputAnimationLabel);

        SetSlotEnabledRight(0, true);
        SetSlotTypeRight(0, (int)PipelineNodeTypes.Any);
        SetSlotColorRight(0, TypeConnectorColors.ANY);
        var outputAnimationLabel = new Label();
        outputAnimationLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        outputAnimationLabel.HorizontalAlignment = HorizontalAlignment.Right;
        outputAnimationLabel.Text = "Ani";
        animationNodeContainer.AddChild(outputAnimationLabel);

        _outputNodePicker = new Button();
        _outputNodePicker.Pressed += OutputNodePickerPressed;
        AddChild(_outputNodePicker);

        if (_setTrackPathNodeStore != null)
        {
            _outputNodePicker.Text = _setTrackPathNodeStore.TrackPath;
            _targetNode = _context.RootNode.GetNode(_setTrackPathNodeStore.TrackPath);
        }
        else
        {
            _outputNodePicker.Text = "Select target";
        }
    }

    public void Register()
    {
        foreach (var pipe in NextPipes)
        {
            pipe.Register();
        }
    }

    public void PreRegister(string nodeName)
    {
        _nodeName = nodeName;

        foreach (var pipe in NextPipes)
        {
            pipe.PreRegister(_nodeName);
        }
    }

    public PipeValue Pipe(PipeValue pipeValue)
    {
        var obj = pipeValue.Value;

        if (obj is not AnimationPlayer)
        {
            return null;
        }

        var animationPlayer = (AnimationPlayer)obj;

        var newAnimationPlayer = (AnimationPlayer) animationPlayer.Duplicate();
        _animationPlayer = newAnimationPlayer;

        if (_targetNode != null)
        {
            var animationName = _animationPlayer.GetAnimationList().First();
            _animationPlayer.GetAnimation(animationName).TrackSetPath(0, _targetNode.GetPath());
        }

        return new PipeValue()
        {
            Value = _animationPlayer.Duplicate(),
            TouchedProperties = TOUCHED_PROPERTIES
        };
    }

    public void Clean()
    {
        _animationPlayer = null;
        foreach (var pipe in NextPipes)
        {
            pipe.Clean();
        }
    }

    public void PipeDisconnect()
    {
        Clean();
    }

    private void OutputNodePickerPressed()
    {
        EditorInterface.Singleton.PopupNodeSelector(Callable.From<NodePath>(OutputNodePathChanged));
    }

    private void OutputNodePathChanged(NodePath destinationPath)
    {
        if (!destinationPath.IsEmpty)
        {
            var newlyChosenParentNode = _context.RootNode.Owner.GetNodeOrNull(destinationPath);

            if (newlyChosenParentNode != null)
            {
                _targetNode = newlyChosenParentNode;
                _outputNodePicker.Text = _context.RootNode.GetPathTo(_targetNode);

                var cloneableAnimationPlayer = new CloneablePipeValue()
                {
                    PipeValue = new PipeValue()
                    {
                        TouchedProperties = TOUCHED_PROPERTIES,
                        Value = _animationPlayer.Duplicate()
                    }
                };

                var valuePipe = new ValuePipe() { Pipe = this, CloneablePipeValue = cloneableAnimationPlayer };
                _context.ReprocessPipe([valuePipe]);
            }
        }
        
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    public override void AddConnection(int index, List<IReceivePipe> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);
        NextPipes.AddRange(receivePipes);
    }

    public override void Connect(int index, List<IReceivePipe> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);
        NextPipes.AddRange(receivePipes);

        var destinationHelper = new DestinationHelper();
        var pipeValue = new PipeValue() { Value = _animationPlayer.Duplicate(), TouchedProperties = TOUCHED_PROPERTIES };
        destinationHelper.AddReceivePipes(_context, _nodeName, receivePipes, _animationPlayer == null ? null : new CloneablePipeValue() { PipeValue = pipeValue });
    }

    public override void Disconnect(int index, List<IReceivePipe> receivePipes)
    {
        _nodeConnections[index].RemoveAll(rp => receivePipes.Contains(rp));
        NextPipes.RemoveAll(p => receivePipes.Contains(p));

        var destinationHelper = new DestinationHelper();
        destinationHelper.RemoveReceivePipes(receivePipes);
    }

    public override void DisposePipe()
    {
        // Nothing to dispose
    }

}
#endif