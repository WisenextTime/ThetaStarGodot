using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Godot;

namespace ThetaStar.Scripts;

public static class ThetaStar
{
    private static List<ThetaStarNode> _open = [];                    //所有将要搜寻的点
    private static Dictionary<Vector2I, ThetaStarNode> _grid = new(); //所有节点
    private static ThetaStarNode _startNode;
    private static ThetaStarNode _endNode;

    public static List<Vector2I> FindPath(Vector2I from, Vector2I to, int[,] map)
    {
        _open = [];
        _grid = new();

        //给grid表中预先添加所有坐标对应的节点, 同时添加墙体
        for (var x = 0; x < map.GetLength(0); x++)
        {
            for (var y = 0; y < map.GetLength(1); y++)
            {
                var newNode = new ThetaStarNode(new Vector2I(x, y), from, to,
                                                //new Vector2I(map.GetLength(0), map.GetLength(1)),
                                                map[x, y] == int.MaxValue ? ThetaStarNodeState.Wall : ThetaStarNodeState.Open);

                _grid.Add(new Vector2I(x, y), newNode);
                //if (newNode.Cost == int.MaxValue) newNode.State = ThetaStarNodeState.Wall; //Walls.Add(newNode);
            }
        }
        _startNode = _grid[from];
        _endNode = _grid[to];
        _open.Add(_startNode); //反正startNode马上就被标记为Closed了，想来没必要标记InSearch
        _startNode.GValue = 0;
        while (_open.Any())
        {
            //找到可能最优的起始节点(遍历效率低下可改用二叉树之类的)
            var current = _open.Aggregate((a, b) =>
                                              a.FValue < b.FValue || a.FValue.Equals(b.FValue) && a.HValue < b.HValue ? a : b);

            current.State = ThetaStarNodeState.Closed;
            _open.Remove(current);

            //寻路成功, 返回结果
            if (current == _endNode) return current.GetPath();

            //AP-θ*: 更新角度边界
            UpdateBounds(current);

            //寻路核心, 找到下一个节点
            foreach (var pos in current.GetNeighborsPos())
            {
                if (!_grid.TryGetValue(pos, out var neighbor)) continue;
                if (neighbor.State is not ThetaStarNodeState.Open) continue;

                if (neighbor.State != ThetaStarNodeState.InSearch) neighbor.GValue = float.PositiveInfinity;
                UpdateNode(neighbor, current);
            }
        }

        return null;
    }

    private static void UpdateBounds(ThetaStarNode current)
    {
        if (current.Parent is null) return; //起点保持无穷就好

        foreach (var corner in GetCorners(current))
        {
            if (corner.All(n => current.Parent == n ||
                               current.AngleTo(n) < 0 ||
                               (current.AngleTo(n).Equals(0) && current.Parent.DistanceTo(n) <= current.Parent.DistanceTo(current))))
                current.LowerBound = 0;

            if (corner.All(n => current.Parent == n ||
                               current.AngleTo(n) > 0 ||
                               (current.AngleTo(n).Equals(0) && current.Parent.DistanceTo(n) <= current.Parent.DistanceTo(current))))
                current.UpperBound = 0;
        }

        // foreach (var wall in GetNearWalls(current))
        // {
        //     if (current.AngleTo(wall) < 0 ||
        //         (current.AngleTo(wall).Equals(0) && current.Parent.DistanceTo(wall) <= current.Parent.DistanceTo(current)))
        //         current.LowerBound = 0;
        //
        //     if (current.AngleTo(wall) > 0 ||
        //         (current.AngleTo(wall).Equals(0) && current.Parent.DistanceTo(wall) <= current.Parent.DistanceTo(current)))
        //         current.UpperBound = 0;
        // }

        // foreach (var pos in current.GetNeighborsPos())
        foreach (var pos in current.GetNearPos())    //todo: 干脆注释了这段看看会怎么样
        {
            if (!_grid.TryGetValue(pos, out var n)) continue;
            if (n.State is ThetaStarNodeState.Wall) continue;

            if (n.State is ThetaStarNodeState.Closed && n.Parent == current.Parent && n != _startNode)
            {
                var nlb = n.LowerBound + current.AngleTo(n);
                var nub = n.UpperBound + current.AngleTo(n);
                if (nlb <= 0) current.LowerBound = MathF.Max(current.LowerBound, nlb);
                if (nub >= 0) current.UpperBound = MathF.Min(current.UpperBound, nub);
            }

            if (current.Parent.DistanceTo(n) < current.Parent.DistanceTo(current) && current.Parent != n &&
                (n.State is not ThetaStarNodeState.Closed || n.Parent != current.Parent))
            {
                var nb = current.AngleTo(n);
                if (nb < 0) current.LowerBound = MathF.Max(current.LowerBound, nb);
                if (nb > 0) current.UpperBound = MathF.Min(current.UpperBound, nb);
            }
        }
    }

    private static void UpdateNode(ThetaStarNode neighbor, ThetaStarNode current)
    {
        var inSearch = neighbor.State is ThetaStarNodeState.InSearch;

        //AP-θ*: 判断路径
        if (current != _startNode && current.AngleTo(neighbor).IsBetween(current.LowerBound, current.UpperBound))
        {
            //path 2
            var cost = current.Parent.GValue + current.Parent.DistanceTo(neighbor);
            if (inSearch && cost > neighbor.GValue) return; //已在搜寻中，无需更新

            neighbor.GValue = cost;
            neighbor.Parent = current.Parent;
        }
        else if (neighbor.Position - current.Position is { X: 0 } or { Y: 0 }) //不许斜着滑走！
        {
            //path 1
            var cost = current.GValue + current.DistanceTo(neighbor);
            if (inSearch && cost > neighbor.GValue) return; //已在搜寻中，无需更新

            neighbor.GValue = cost;
            neighbor.Parent = current;
        }
        else return;

        if (!inSearch)
        {
            _open.Add(neighbor);
            neighbor.State = ThetaStarNodeState.InSearch;
        }
    }

    private static IEnumerable<ThetaStarNode[]> GetCorners(ThetaStarNode node)
    {
        var corners =
            from dir in new (int x, int y)[] { (-1, -1), (-1, 1), (1, -1), (1, 1) }
            let pos = new[]
            {
                node.Position + new Vector2I(dir.x, 0),
                node.Position + new Vector2I(0,     dir.y),
                node.Position + new Vector2I(dir.x, dir.y)
            }
            select pos.Select(p => _grid.GetValueOrDefault(p))
                      .Where(n => n is not null).ToArray();

        foreach (var corner in corners)
        {
            if (corner.Any(n => n.State is ThetaStarNodeState.Wall))
                yield return corner; //.Where(n => n.State is not ThetaStarNodeState.Wall).ToArray();
        }
    }

    // private static IEnumerable<ThetaStarNode> GetNearWalls(ThetaStarNode node) =>
    //     node.GetNeighborsPos().Select(p => _grid.GetValueOrDefault(p))
    //         .Where(n => n is { State: ThetaStarNodeState.Wall });
}