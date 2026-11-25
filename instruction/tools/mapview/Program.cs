using System;
using System.Collections.Generic;
using System.Text;
using mumu;

/// <summary>
/// 難易度30の3つの形を生成して、マップ(M)と枠(R)を文字で表示する（MapBlockView.buildRing と同じ枠の選び方）
/// </summary>
public static class Program
{
    public static void Main()
    {
        eMapPattern[] shapes = { eMapPattern.Circle, eMapPattern.Hexagon, eMapPattern.Diamond };
        foreach (eMapPattern shape in shapes)
        {
            var map = new MapData();
            var result = MapGenerator.Generate(map, 30, shape, 1, false);
            Console.WriteLine($"== {shape}  difficulty 30  seed 1  size {map.width}x{map.height}  success {result.IsSuccess}");
            Console.WriteLine(render(map));
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 枠の1層目（奥・右の空マス）と2層目を集めて、表を作る
    /// </summary>
    /// <param name="_map"></param>
    private static string render(MapData _map)
    {
        int w = _map.width;
        int h = _map.height;
        var outside = findOutside(_map);

        var layer1 = new HashSet<(int x, int y)>();
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (!isMap(_map, x, y))
                {
                    continue;
                }
                addCell(_map, outside, x, y - 1, layer1, null);
                addCell(_map, outside, x + 1, y, layer1, null);
            }
        }

        var layer2 = new HashSet<(int x, int y)>();
        foreach (var cell in layer1)
        {
            addCell(_map, outside, cell.x, cell.y - 1, layer2, layer1);
            addCell(_map, outside, cell.x + 1, cell.y, layer2, layer1);
        }

        var sb = new StringBuilder();
        sb.Append("      ");
        for (int x = 0; x <= w + 1; x++)
        {
            sb.Append($"{x,3}");
        }
        sb.AppendLine();
        for (int y = -2; y <= h - 1; y++)
        {
            sb.Append($"y{y,3} ");
            for (int x = 0; x <= w + 1; x++)
            {
                string box;
                if (isMap(_map, x, y))
                {
                    box = "[M]";
                }
                else if (layer1.Contains((x, y)) || layer2.Contains((x, y)))
                {
                    box = "[R]";
                }
                else
                {
                    box = "[ ]";
                }
                sb.Append(box);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// 地形のマスか（範囲内で空でない）
    /// </summary>
    private static bool isMap(MapData _map, int _x, int _y)
    {
        return _x >= 0 && _y >= 0 && _x < _map.width && _y < _map.height && _map.GetBlockType(_x, _y) != eBlockType.Empty;
    }

    /// <summary>
    /// 空のマスを枠の候補に入れる（除外の集合にあるマスは入れない）
    /// </summary>
    private static void addCell(MapData _map, HashSet<(int x, int y)> _outside, int _x, int _y, HashSet<(int x, int y)> _set, HashSet<(int x, int y)> _exclude)
    {
        if (!_outside.Contains((_x, _y)) || isMap(_map, _x, _y) || (_exclude != null && _exclude.Contains((_x, _y))))
        {
            return;
        }
        _set.Add((_x, _y));
    }

    /// <summary>
    /// マップの外側とつながる空のマス（MapBlockView.findOutside と同じ）
    /// </summary>
    private static HashSet<(int x, int y)> findOutside(MapData _map)
    {
        var outside = new HashSet<(int x, int y)>();
        var queue = new Queue<(int x, int y)>();
        outside.Add((-2, -2));
        queue.Enqueue((-2, -2));
        var dirs = new[] { (0, 1), (0, -1), (-1, 0), (1, 0) };
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var d in dirs)
            {
                var next = (cell.x + d.Item1, cell.y + d.Item2);
                if (next.Item1 < -2 || next.Item2 < -2 || next.Item1 > _map.width + 1 || next.Item2 > _map.height + 1)
                {
                    continue;
                }
                if (isMap(_map, next.Item1, next.Item2) || !outside.Add(next))
                {
                    continue;
                }
                queue.Enqueue(next);
            }
        }
        return outside;
    }
}
