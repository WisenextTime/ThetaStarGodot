using System.Collections.Generic;
using System.Linq;

using Godot;

namespace ThetaStar.Scripts;

public class ThetaStar
{
    public static List<Vector2I> FindPath(Vector2I from, Vector2I to, int[,] map)
    {
        List<ThetaStarNode> open = []; //所有将要搜寻的点
        var grid = new Dictionary<Vector2I, ThetaStarNode>(); //所有节点

        //给grid表中预先添加所有坐标对应的节点, 同时添加墙体
        for (var x = 0; x < map.GetLength(0); x++)
        {
            for (var y = 0; y < map.GetLength(1); y++)
            {
                var newNode = new ThetaStarNode(new Vector2I(x, y), from, to,
                                                //new Vector2I(map.GetLength(0), map.GetLength(1)),
                                                ThetaStarNodeState.Open,
                                                map[x, y]);

                grid.Add(new Vector2I(x, y), newNode);
                if (newNode.Cost == int.MaxValue) newNode.State = ThetaStarNodeState.Wall; //Walls.Add(newNode);
            }
        }
        var startNode = grid[from];
        var endNode = grid[to];
        open.Add(startNode);
        while (open.Any())
        {
            //找到可能最优的起始节点(遍历效率低下可改用二叉树之类的)
            var current = open.Aggregate((a, b) =>
                                             a.FValue < b.FValue || a.FValue.Equals(b.FValue) && a.HValue < b.HValue ? a : b);

            current.State = ThetaStarNodeState.Closed;
            open.Remove(current);

            //寻路成功, 返回结果
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

            //寻路核心, 找到下一个节点
            foreach (var pos in current.GetNeighborsPos())
            {
                if (!grid.TryGetValue(pos, out var neighbor)) continue;
                if (neighbor.State is not ThetaStarNodeState.Open) continue;

                var inSearch = open.Contains(neighbor);
                var cost = current.GValue + current.DistanceTo(neighbor) * neighbor.Cost;
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