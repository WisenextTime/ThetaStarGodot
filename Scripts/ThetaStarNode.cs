using System.Collections.Generic;
using System.Linq;

using Godot;

namespace ThetaStar.Scripts;

// ReSharper disable NotAccessedPositionalProperty.Global
public record ThetaStarNode(Vector2I Position, Vector2I Start, Vector2I End, ThetaStarNodeState State, int Cost)
{
    // ReSharper restore NotAccessedPositionalProperty.Global
    public ThetaStarNodeState State { get; set; } = State;
    
    public float HValue = Position.DistanceTo(End);
    public float GValue = Position.DistanceTo(Start);
	
    public int Cost = Cost;
    
    public float UpperBound = float.PositiveInfinity;
    public float LowerBound = float.NegativeInfinity;

    public ThetaStarNode Parent;
    public float FValue => GValue + HValue;
    public float DistanceTo(ThetaStarNode target) => Position.DistanceTo(target.Position);

    public IEnumerable<Vector2I> GetNeighborsPos() =>
        from x in Enumerable.Range(-1, 3)
        from y in Enumerable.Range(-1, 3)
        where (x, y) != (0, 0)
        select new Vector2I(Position.X + x, Position.Y + y);
}

public enum ThetaStarNodeState
{
    Open,
    Closed,
    Wall
}