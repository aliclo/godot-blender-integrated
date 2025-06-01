using System;
using Godot;

public partial class NumericLineEdit : LineEdit
{

	[Signal]
	public delegate void NumberChangedEventHandler(double number);

	public double Number
	{
		get
		{
			return _number;
		}
		set
		{
			_number = value;
			Text = _number.ToString();
		}
	}
	
	private double _number;
	private double _newNumber;
	private Vector2 _firstMousePosition;
	private bool _held = false;
	private bool _draggedNumber = false;
	private bool _editingByEntry = false;

	public override void _Ready()
	{
		Editable = false;
		SelectingEnabled = false;
		TextChanged += OnTextChanged;
		FocusExited += OnFocusExited;
		MouseFilter = MouseFilterEnum.Pass;
	}

    public override void _GuiInput(InputEvent @event)
    {
		if (@event is InputEventMouseButton inputEventMouseButton)
		{
			if (inputEventMouseButton.IsPressed() && !_editingByEntry)
			{
				_firstMousePosition = inputEventMouseButton.GlobalPosition;
				_held = true;
			}
			else if (inputEventMouseButton.IsReleased())
			{
				_held = false;
				if (_draggedNumber)
				{
					_draggedNumber = false;
					_number = _newNumber;
				}
				else
				{
					Editable = true;
					SelectingEnabled = true;
					_editingByEntry = true;
				}
			}
		}
		else if (@event is InputEventMouseMotion inputEventMouseMotion)
		{
			if (_held)
			{
				_draggedNumber = true;
				var mouseDifference = inputEventMouseMotion.GlobalPosition.X - _firstMousePosition.X;
				_newNumber = _number + mouseDifference/10.0;
				Text = _newNumber.ToString();
				EmitSignal(SignalName.NumberChanged, _newNumber);
			}
		}
		
        base._GuiInput(@event);
    }

	private void OnFocusExited()
	{
		Editable = false;
		SelectingEnabled = false;
		_editingByEntry = false;
	}

	private void OnTextChanged(string newText)
	{
		var successful = double.TryParse(newText, out var number);
		if (successful)
		{
			_number = number;
			EmitSignal(SignalName.NumberChanged, _number);
		}
		else
		{
			Text = _number.ToString();
		}
	}

    protected override void Dispose(bool disposing)
	{
		TextChanged -= OnTextChanged;
		FocusExited -= OnFocusExited;
		base.Dispose(disposing);
	}



}
