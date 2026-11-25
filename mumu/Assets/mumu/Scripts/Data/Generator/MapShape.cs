using System;
using System.Collections.Generic;
using UnityEngine;
using static mumu.MapGrid;

namespace mumu
{
    /// <summary>
    /// マップの形（陸と水の配置。形ごとに seed で少しずつ変える）
    /// </summary>
    internal static class MapShape
    {
        /// <summary>形の端を削る・盛る割合</summary>
        private const double EdgeErodeRate = 0.12;
        private const double EdgeGrowRate = 0.06;
        /// <summary>内側に穴を開ける割合</summary>
        private const double HoleRate = 0.03;

        /// <summary>
        /// 形の外側を何にするか（水がある形は水、それ以外は何もない）
        /// </summary>
        /// <param name="_pattern"></param>
        public static eCell OutsideOf(eMapPattern _pattern)
        {
            switch (_pattern)
            {
                case eMapPattern.Donut:
                case eMapPattern.UShape:
                case eMapPattern.SShape:
                case eMapPattern.River:
                case eMapPattern.TwoRivers:
                case eMapPattern.Lake:
                case eMapPattern.Islands:
                case eMapPattern.Coast:
                case eMapPattern.Checker:
                case eMapPattern.Ponds:
                    return eCell.Water;
                default:
                    return eCell.Empty;
            }
        }

        /// <summary>
        /// 形を作る（座標は -1〜1 に正規化して判定）
        /// </summary>
        /// <param name="_pattern"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        /// <param name="_random"></param>
        public static eCell[,] CreateShape(eMapPattern _pattern, int _width, int _height, System.Random _random)
        {
            var cells = new eCell[_width, _height];
            eCell outside = OutsideOf(_pattern);

            // シードごとに形を少し変える
            double phase = _random.NextDouble() * Math.PI * 2;
            double wave = 0.15 + _random.NextDouble() * 0.15;
            double scale = 0.9 + _random.NextDouble() * 0.1;
            double offsetX = (_random.NextDouble() - 0.5) * 0.3;
            double offsetY = (_random.NextDouble() - 0.5) * 0.3;
            List<double[]> circles = createCircles(_pattern, _width, _random);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    double nx = (x + 0.5) / _width * 2 - 1;
                    double ny = (y + 0.5) / _height * 2 - 1;
                    bool isLand = isLandAt(_pattern, nx, ny, phase, wave, scale, offsetX, offsetY, circles);
                    cells[x, y] = isLand ? eCell.Land : outside;
                }
            }
            return cells;
        }

        /// <summary>
        /// 島・池の円（中心 x, y と半径）
        /// </summary>
        /// <param name="_pattern"></param>
        /// <param name="_size"></param>
        /// <param name="_random"></param>
        private static List<double[]> createCircles(eMapPattern _pattern, int _size, System.Random _random)
        {
            var circles = new List<double[]>();
            int count = _pattern == eMapPattern.Islands ? 3 + _random.Next(3) : (_pattern == eMapPattern.Ponds ? Mathf.Max(2, _size / 3) : 0);
            for (int i = 0; i < count; i++)
            {
                double radius = _pattern == eMapPattern.Islands ? 0.3 + _random.NextDouble() * 0.15 : 0.1 + _random.NextDouble() * 0.08;
                circles.Add(new[] { (_random.NextDouble() * 2 - 1) * 0.7, (_random.NextDouble() * 2 - 1) * 0.7, radius });
            }
            return circles;
        }

        /// <summary>
        /// 正規化座標が陸か
        /// </summary>
        private static bool isLandAt(eMapPattern _pattern, double _x, double _y, double _phase, double _wave, double _scale,
            double _offsetX, double _offsetY, List<double[]> _circles)
        {
            double ax = Math.Abs(_x);
            double ay = Math.Abs(_y);
            double r = Math.Sqrt(_x * _x + _y * _y);

            switch (_pattern)
            {
                case eMapPattern.Square:
                case eMapPattern.WideRect:
                case eMapPattern.TallRect:
                    return true;
                case eMapPattern.Circle:
                case eMapPattern.Oval:
                    return r <= 0.98 * _scale;
                case eMapPattern.Hexagon:
                    return ay <= 0.92 * _scale && ax * 0.87 + ay * 0.5 <= 0.92 * _scale;
                case eMapPattern.Diamond:
                    return ax + ay <= 1.05 * _scale;
                case eMapPattern.Cross:
                    return ax <= 0.36 || ay <= 0.36;
                case eMapPattern.Donut:
                    return r <= 0.98 * _scale && r >= 0.38;
                case eMapPattern.LShape:
                    return _x <= -0.15 + _offsetX || _y >= 0.15 + _offsetY;
                case eMapPattern.UShape:
                    return _x <= -0.35 || _x >= 0.35 || _y >= 0.35;
                case eMapPattern.SShape:
                    return _y <= -0.5 || ay <= 0.18 || _y >= 0.5 || (_x <= -0.45 && _y < 0) || (_x >= 0.45 && _y > 0);
                case eMapPattern.River:
                    return Math.Abs(_x - _offsetX - Math.Sin(_y * Math.PI + _phase) * _wave) > 0.2;
                case eMapPattern.TwoRivers:
                    double curve = Math.Sin(_y * Math.PI + _phase) * _wave * 0.6;
                    return Math.Abs(_x + 0.45 - curve) > 0.14 && Math.Abs(_x - 0.45 - curve) > 0.14;
                case eMapPattern.Lake:
                    double lx = (_x - _offsetX) / 0.5, ly = (_y - _offsetY) / 0.42;
                    return lx * lx + ly * ly > 1;
                case eMapPattern.Islands:
                    foreach (double[] c in _circles) if (dist(_x, _y, c) <= c[2]) return true;
                    return false;
                case eMapPattern.Coast:
                    return _x <= 0.2 + Math.Sin(_y * Math.PI * 1.5 + _phase) * _wave;
                case eMapPattern.Checker:
                    double fx = frac((_x + 1) * 1.5 + 0.1), fy = frac((_y + 1) * 1.5 + 0.1);
                    return fx < 0.72 && fy < 0.72;
                case eMapPattern.Ponds:
                    foreach (double[] c in _circles) if (dist(_x, _y, c) <= c[2]) return false;
                    return true;
                default:
                    return true;
            }
        }

        /// <summary>
        /// 円の中心までの距離
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_circle"></param>
        private static double dist(double _x, double _y, double[] _circle)
        {
            double dx = _x - _circle[0], dy = _y - _circle[1];
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 小数部分
        /// </summary>
        /// <param name="_value"></param>
        private static double frac(double _value) => _value - Math.Floor(_value);

        /// <summary>
        /// 端を削る・盛る、内側に穴を開ける（シードで形を変える）
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_outside"></param>
        /// <param name="_random"></param>
        public static void Vary(eCell[,] _cells, eCell _outside, System.Random _random)
        {
            int width = _cells.GetLength(0), height = _cells.GetLength(1);
            var next = (eCell[,])_cells.Clone();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isLand = _cells[x, y] == eCell.Land;
                    bool isEdge = countLandNeighbors(_cells, x, y) < 4 || x == 0 || y == 0 || x == width - 1 || y == height - 1;

                    if (isLand && isEdge && _random.NextDouble() < EdgeErodeRate)
                    {
                        next[x, y] = _outside;
                    }
                    else if (isLand && !isEdge && _random.NextDouble() < HoleRate)
                    {
                        next[x, y] = _outside;
                    }
                    else if (!isLand && countLandNeighbors(_cells, x, y) > 0 && _random.NextDouble() < EdgeGrowRate)
                    {
                        next[x, y] = eCell.Land;
                    }
                }
            }
            Array.Copy(next, _cells, next.Length);
        }

        /// <summary>
        /// 上下左右の陸の数
        /// </summary>
        /// <param name="_cells"></param>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        private static int countLandNeighbors(eCell[,] _cells, int _x, int _y)
        {
            int count = 0;
            foreach (Vector2Int d in Directions)
            {
                int x = _x + d.x, y = _y + d.y;
                if (IsInside(_cells, x, y) && _cells[x, y] == eCell.Land) count++;
            }
            return count;
        }
    }
}
