using System.Linq;
using Godot;

namespace ThetaStar.Scripts;

public partial class Scene : Control
{
	private int _mousePressed = -1;
	private bool _launching;

	private Vector2I LastMousePosition =>
		new Vector2I((int)GetGlobalMousePosition().X / 32, (int)GetGlobalMousePosition().Y / 32);
	private ItemList Selector => field ??= GetNode<ItemList>("Operation/Selector");
	private TileMapLayer Map => field ??= GetNode<TileMapLayer>("Map");
	private Line2D Path => field ??= GetNode<Line2D>("Path");

	// ReSharper disable once FieldCanBeMadeReadOnly.Local
	private int[,] _tiles = new int[30, 20];
	private Vector2I _startPos = new(0, 19);
	private Vector2I _endPos = new(29, 19);
	
	public override void _Ready()
	{
		Selector.Select(0);
		for (var x = 0; x < 30; x++)
		{
			for (var y = 0; y < 20; y++)
			{
				_tiles[x, y] = 1;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (_launching) return;
		switch (@event)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
				if (mouseButton.Pressed)
				{
					
					var pos = LastMousePosition;
					if (pos.X > 29 || pos.Y > 19)
					{
						_mousePressed = -1;
						return;
					}
					if(Selector.GetSelectedItems()[0]==0)
					{
						_mousePressed = _tiles[pos.X, pos.Y];
					}
					else
					{
						SetCell(pos, 0);
					}
				}
				else
				{
					_mousePressed = -1;
				}
				break;
			case InputEventMouseMotion when _mousePressed != -1:
				SetCell(LastMousePosition, _mousePressed);
				break;
		}
	}

	private void SetCell(Vector2I pos, int value)
	{
		if(pos.X > 29 || pos.Y > 19) return;
		switch (Selector.GetSelectedItems()[0])
		{
			case 0:
				if(pos == _startPos || pos == _endPos) break;
				if (value == int.MaxValue)
				{
					Map.SetCell(pos, 0, new Vector2I(1, 0));
					_tiles[pos.X, pos.Y] = 1;
				}
				else
				{
					Map.SetCell(pos, 0, new Vector2I(0, 0));
					_tiles[pos.X, pos.Y] = int.MaxValue;
				}
				break;
			case 1:
				if (_tiles[pos.X, pos.Y] == 1) break;
				Map.SetCell(_startPos, 0, new Vector2I(1, 0));
				Map.SetCell(pos, 0, new Vector2I(2, 0));
				_startPos = pos;
				break;
			case 2:
				if (_tiles[pos.X, pos.Y] == 1) break;
				Map.SetCell(_endPos, 0, new Vector2I(1, 0));
				Map.SetCell(pos, 0, new Vector2I(3, 0));
				_endPos = pos;
				break;
		}
	}

	public void Start()
	{
		_launching = true;
		var final = ThetaStar.FindPath(_startPos, _endPos, _tiles);
		if(final == null) return;
		foreach (var node in final)
		{
			Path.AddPoint(node * 32 + new Vector2I(16, 16));
			if(node == _endPos) continue;
			Map.SetCell(node, 0, new Vector2I(0, 1));
		}
		// Path.AddPoint(_startPos * 32 + new Vector2I(16, 16));
	}
}