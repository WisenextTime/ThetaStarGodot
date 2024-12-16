using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ThetaStar.Scripts;

public class ThetaStar
{
	public static List<Vector2I> FindPath(Vector2I from, Vector2I to, int[,] map)
	{
		List<Node> open = [];
		List<Node> closed = [];
		List<Node> Walls = [];
		var grid = new Dictionary<Vector2I, Node>();
		for (var x = 0; x < map.GetLength(0); x++)
		{
			for (var y = 0; y < map.GetLength(1); y++)
			{
				var newNode = new Node(new Vector2I(x, y), from, to,
					new Vector2I(map.GetLength(0), map.GetLength(1)),
					map[x, y]);
				grid.Add(new Vector2I(x, y), newNode);
				if (newNode.Cost == int.MaxValue) Walls.Add(newNode);
			}
		}
		var startNode = grid[from];
		var endNode = grid[to];
		open.Add(startNode);
		while (open.Any())
		{
			var current = open[0];
			foreach (var node in open.Where(node =>
				         node.FValue < current.FValue ||
				         node.FValue.Equals(current.FValue) && node.HValue < current.HValue))
				current = node;
			closed.Add(current);
			open.Remove(current);

			if (current == endNode)
			{
				var currentNode = endNode;
				var path = new List<Vector2I>();
				while (currentNode != startNode)
				{
					path.Add(currentNode.Position);
					currentNode = currentNode.Parent;
				}
				return path;
			}
			
			foreach (var neighbor in current.GetNeighbors(grid).Where(n => !closed.Contains(n)))
			{
				var inSearch = open.Contains(neighbor);
				var cost = current.GValue + current.DistanceTo(neighbor);
				if (!inSearch || cost <= neighbor.GValue)
				{
					neighbor.GValue = cost;
					neighbor.Parent = current;
					
					if (!inSearch)
					{
						open.Add(neighbor);
					}
				}
			}
		}
		return null;
	}
}

// ReSharper disable NotAccessedPositionalProperty.Global
public record Node(Vector2I Position, Vector2I Start, Vector2I End, Vector2I Size, int Cost)
{
	public Vector2I Position = Position;
	public Vector2I Size = Size;
	public float HValue = Position.DistanceTo(Start);
	public float GValue = Position.DistanceTo(End);

	public float UpperBound = float.PositiveInfinity;
	public float LowerBound = float.NegativeInfinity;
	
	public int Cost = Cost;
	public Node Parent;
	public float FValue => GValue + HValue + Cost;
	public float DistanceTo(Node target) => Position.DistanceTo(target.Position);

	private readonly bool _onLeft = Position.X == 0;
	private readonly bool _onRight = Position.X == Size.X - 1;
	private readonly bool _onTop = Position.Y == 0;
	private readonly bool _onBottom = Position.Y == Size.Y - 1;

	public List<Node> GetNeighbors(Dictionary<Vector2I, Node> map)
	{
		List<Vector2I> neighbors = [];
		if (!_onLeft) neighbors.Add(new Vector2I(Position.X - 1, Position.Y));
		if (!_onTop) neighbors.Add(new Vector2I(Position.X, Position.Y - 1));
		if (!_onRight) neighbors.Add(new Vector2I(Position.X + 1, Position.Y));
		if (!_onBottom) neighbors.Add(new Vector2I(Position.X, Position.Y + 1));

		if (!_onLeft && !_onTop) neighbors.Add(new Vector2I(Position.X - 1, Position.Y - 1));
		if (!_onRight && !_onTop) neighbors.Add(new Vector2I(Position.X + 1, Position.Y - 1));
		if (!_onLeft && !_onBottom) neighbors.Add(new Vector2I(Position.X - 1, Position.Y + 1));
		if (!_onRight && !_onBottom) neighbors.Add(new Vector2I(Position.X + 1, Position.Y + 1));
		
		return neighbors.Select(node => map[node]).ToList();
	}
}