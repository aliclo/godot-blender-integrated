#if TOOLS
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class SetPropertyNode : PipelineNode, IReceivePipe
{

    private partial class SetPropertyNodeStore : GodotObject
    {

        [Export]
        public Array<string> Props { get; set; }
        [Export]
        public int SelectedIndex { get; set; }
        [Export]
        public string PropValue { get; set; }

    }

    // TODO: Use this from the context instead of having to provide it to the context
    private static readonly List<string[]> UNTOUCHED_PROPERTIES = new List<string[]>() {};

    private SetPropertyNodeStore _setPropertyNodeStore;
    private PipeContext _context;

    private OptionButton _propertyOptions;
    private LineEdit _valueEdit;

    private ICloneablePipeValue _inputCloneablePipeValue;
    private CloneablePipeValue _cloneablePipeValue;
    private Array<string> _props;
    private int _selectedIndex;
    private string _previousPropName;
    private string _propValue;

    public List<IReceivePipe> NextPipes { get; set; } = new List<IReceivePipe>();
    private List<List<IReceivePipe>> _nodeConnections;
    public override List<List<IReceivePipe>> NodeConnections => _nodeConnections;
    public override List<NodePath> NodeDependencies => new List<NodePath>();

    public override Variant GetData()
    {
        return GodotJsonParser.ToJsonType(new SetPropertyNodeStore()
        {
            Props = _props,
            SelectedIndex = _selectedIndex,
            PropValue = _propValue
        });
    }

    public override void Load(Variant data)
    {
        _setPropertyNodeStore = GodotJsonParser.FromJsonType<SetPropertyNodeStore>(data);
    }

    public override void Init(PipeContext context)
    {
        _context = context;

        _nodeConnections = Enumerable.Range(0, 1)
            .Select(n => new List<IReceivePipe>())
            .ToList();

        var setNodeContainer = new HBoxContainer();
        AddChild(setNodeContainer);

        SetSlotEnabledLeft(0, true);
        SetSlotTypeLeft(0, (int)PipelineNodeTypes.Any);
        SetSlotColorLeft(0, TypeConnectorColors.ANY);
        var inputAnimationLabel = new Label();
        inputAnimationLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        inputAnimationLabel.HorizontalAlignment = HorizontalAlignment.Left;
        inputAnimationLabel.Text = "Node";
        setNodeContainer.AddChild(inputAnimationLabel);

        SetSlotEnabledRight(0, true);
        SetSlotTypeRight(0, (int)PipelineNodeTypes.Any);
        SetSlotColorRight(0, TypeConnectorColors.ANY);
        var outputAnimationLabel = new Label();
        outputAnimationLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        outputAnimationLabel.HorizontalAlignment = HorizontalAlignment.Right;
        outputAnimationLabel.Text = "Node";
        setNodeContainer.AddChild(outputAnimationLabel);

        _propertyOptions = new OptionButton();
        _propertyOptions.ItemSelected += PropertySelected;
        AddChild(_propertyOptions);

        _valueEdit = new LineEdit();
        _valueEdit.TextChanged += PropValueChanged;
        AddChild(_valueEdit);

        if (_setPropertyNodeStore != null)
        {
            _props = _setPropertyNodeStore.Props;
            foreach (var prop in _props)
            {
                _propertyOptions.AddItem(prop);
            }
            _selectedIndex = _setPropertyNodeStore.SelectedIndex;
            _propertyOptions.Select(_selectedIndex);
            _propValue = _setPropertyNodeStore.PropValue;
            _valueEdit.Text = _propValue;
        }
    }

    public void Register()
    {
        
    }

    public ICloneablePipeValue Pipe(ICloneablePipeValue cloneablePipeValue)
    {
        _inputCloneablePipeValue = cloneablePipeValue;
        var pipeValue = cloneablePipeValue.ClonePipeValue();
        var node = pipeValue.Value;
        var propName = _selectedIndex == -1 ? null : _props[_selectedIndex];

        if (_props == null || !_props.Any() || propName == null)
        {
            _props = [.. node.GetPropertyList().Select(p => (string)p["name"])];
        }
        else
        {
            _props = [.. node.GetPropertyList().Select(p => (string)p["name"])];
            _selectedIndex = _props.IndexOf(propName);
        }

        _propertyOptions.Clear();
        foreach (var prop in _props)
        {
            _propertyOptions.AddItem(prop);
        }

        _propertyOptions.Select(_selectedIndex);

        if (propName != null)
        {
            
            node.Set(propName, _propValue);
        }

        var touchedProperties = pipeValue.TouchedProperties;

        if (_previousPropName != null)
        {
            touchedProperties.Add([_previousPropName]);
        }

        _previousPropName = propName;

        if (propName != null)
        {
            touchedProperties.Add([propName]);
        }

        var resultPipeValue = new PipeValue()
        {
            Value = node,
            TouchedProperties = touchedProperties,
            UntouchedProperties = pipeValue.UntouchedProperties.Union(UNTOUCHED_PROPERTIES).ToList()
        };

        _cloneablePipeValue = new CloneablePipeValue() { PipeValue = resultPipeValue };

        return _cloneablePipeValue;
    }

    public void Clean()
    {
        //_cloneablePipeValue = null;
    }

    public void PipeDisconnect()
    {
        Clean();
    }

    private void PropertySelected(long index)
    {
        _selectedIndex = (int)index;

        // if (_cloneablePipeValue != null)
        // {

        // }

        var valuePipe = new ValuePipe() { Pipe = this, CloneablePipeValue = _inputCloneablePipeValue };
        _context.ReprocessPipe([valuePipe]);
        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    private void PropValueChanged(string value)
    {
        _propValue = value;

        var valuePipe = new ValuePipe() { Pipe = this, CloneablePipeValue = _inputCloneablePipeValue };
        _context.ReprocessPipe([valuePipe]);
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
        destinationHelper.AddReceivePipes(_context, receivePipes, _cloneablePipeValue == null ? null : _cloneablePipeValue);
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