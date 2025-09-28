#if TOOLS
using Godot;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class SetTrackPathNode : PipelineNode
{

    private partial class SetTrackPathNodeStore : GodotObject
    {

        [Export]
        public string TrackPath { get; set; }

    }

    // TODO: Use this from the context instead of having to provide it to the context
    private static readonly Array<Array<string>> TOUCHED_PROPERTIES = new Array<Array<string>>() {};
    private static readonly Array<Array<string>> UNTOUCHED_PROPERTIES = new Array<Array<string>>() {};

    private SetTrackPathNodeStore _setTrackPathNodeStore;
    private PipeContext _context;

    private Button _outputNodePicker;

    private AnimationPlayer _animationPlayer;
    private ICloneablePipeValue _inputCloneablePipeValue;
    private CloneablePipeValue _cloneablePipeValue;
    private NodePath _targetNodePath;
    private Node _targetNode;
    private Array<ClonedRegister> _clonedRegister;
    private Array<NodePath> _nodeDependencies;

    public override Array<PipelineNode> NextPipes { get; } = new Array<PipelineNode>();
    private Array<Array<PipelineNode>> _nodeConnections;
    public override Array<Array<PipelineNode>> NodeConnections => _nodeConnections;
    public override Array<NodePath> NodeDependencies => _nodeDependencies;

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new SetTrackPathNodeStore()
        {
            TrackPath = _targetNodePath
        });
    }

    public override void Load(Variant data)
    {
        _setTrackPathNodeStore = GodotJsonParser.FromJsonType<SetTrackPathNodeStore>(data);
    }

    public override void Init(PipeContext context)
    {
        _context = context;
        _clonedRegister = new Array<ClonedRegister>();
        _nodeDependencies = new Array<NodePath>();

        _nodeConnections = new Array<Array<PipelineNode>>(Enumerable.Range(0, 1)
            .Select(n => new Array<PipelineNode>()));

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
            _targetNodePath = _setTrackPathNodeStore.TrackPath;
        }
        else
        {
            _outputNodePicker.Text = "Select target";
        }
    }

    public override void Register()
    {
        _nodeDependencies = new Array<NodePath>();

        if (_targetNodePath != null && !_targetNodePath.IsEmpty)
        {
            _nodeDependencies.Add(_targetNodePath);
        }
    }

    public override ICloneablePipeValue PipeValue(ICloneablePipeValue cloneablePipeValue)
    {
        _inputCloneablePipeValue = cloneablePipeValue;
        var pipeValue = cloneablePipeValue.ClonePipeValue();
        var obj = pipeValue.Value;

        if (obj is not AnimationPlayer)
        {
            return null;
        }

        _animationPlayer = (AnimationPlayer)obj;

        var resultPipeValue = new PipeValue()
        {
            Value = _animationPlayer,
            TouchedProperties = new Array<Array<string>>(pipeValue.TouchedProperties.Union(TOUCHED_PROPERTIES)),
            UntouchedProperties = new Array<Array<string>>(pipeValue.UntouchedProperties.Union(UNTOUCHED_PROPERTIES))
        };

        _cloneablePipeValue = new CloneablePipeValue() { PipeValue = resultPipeValue };

        if (_targetNodePath != null && !_targetNodePath.IsEmpty)
        {
            _targetNode = _context.RootNode.GetNodeOrNull(_targetNodePath);

            if (_targetNode != null)
            {
                _cloneablePipeValue.OnClone += OnValueCloned;
            }
            else
            {
                GD.PrintErr("Target node doesn't exist for ", nameof(SetTrackPathNode), " '", _targetNodePath, "'");
            }
        }

        return _cloneablePipeValue;
    }

    private void OnValueCloned(PipeValue clonedPipeValue)
    {
        var action = () => OnValueTreeEntered(clonedPipeValue);
        _clonedRegister.Add(new ClonedRegister()
        {
            Action = action,
            PipeValue = clonedPipeValue
        });
        clonedPipeValue.Value.TreeEntered += action;
    }

    private void OnValueTreeEntered(PipeValue clonedPipeValue)
    {
        var clonedRegister = _clonedRegister.Single(cr => cr.PipeValue == clonedPipeValue);
        clonedPipeValue.Value.TreeEntered -= clonedRegister.Action;
        string targetNodeRelativePath = clonedPipeValue.Value.GetNode(_animationPlayer.RootNode).GetPathTo(_targetNode);
        foreach (var animationLibraryName in _animationPlayer.GetAnimationLibraryList())
        {
            var animationLibrary = _animationPlayer.GetAnimationLibrary(animationLibraryName);

            foreach (var animationName in animationLibrary.GetAnimationList())
            {
                var animation = animationLibrary.GetAnimation(animationName);

                for (int ti = 0; ti < animation.GetTrackCount(); ti++)
                {
                    animation.TrackSetPath(ti, clonedPipeValue.Value.GetNode(_animationPlayer.RootNode).GetPathTo(_targetNode));
                }
            }
        }
    }

    public override void Clean()
    {
        _animationPlayer = null;
    }

    public override void PipeDisconnect()
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
                _targetNodePath = _context.RootNode.GetPathTo(newlyChosenParentNode);
                _outputNodePicker.Text = _targetNodePath;

                var valuePipe = new ValuePipe() { Pipe = this, CloneablePipeValue = _inputCloneablePipeValue };
                _context.ReprocessPipe([valuePipe]);
            }
            else
            {
                GD.PrintErr("Target node doesn't exist for ", nameof(SetTrackPathNode), " '", _targetNodePath, "'");
            }
        }
        
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    public override void AddConnection(int index, Array<PipelineNode> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);
        NextPipes.AddRange(receivePipes);
    }

    public override void Connect(int index, Array<PipelineNode> receivePipes)
    {
        _nodeConnections[index].AddRange(receivePipes);
        NextPipes.AddRange(receivePipes);

        var destinationHelper = new DestinationHelper();
        destinationHelper.AddReceivePipes(_context, receivePipes, _cloneablePipeValue == null ? null : _cloneablePipeValue);
    }

    public override void Disconnect(int index, Array<PipelineNode> receivePipes)
    {
        var nodePortConnections = _nodeConnections[index];
        foreach (var receivePipe in receivePipes)
        {
            nodePortConnections.Remove(receivePipe);
            NextPipes.Remove(receivePipe);
        }

        var destinationHelper = new DestinationHelper();
        destinationHelper.RemoveReceivePipes(receivePipes);
    }

    public override void _ExitTree()
    {
        _outputNodePicker.Pressed -= OutputNodePickerPressed;
    }

}
#endif