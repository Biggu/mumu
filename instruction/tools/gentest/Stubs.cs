using System;
namespace UnityEngine
{
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int right => new Vector2Int(1, 0);
        public static Vector2Int left => new Vector2Int(-1, 0);
        public static Vector2Int up => new Vector2Int(0, 1);
        public static Vector2Int down => new Vector2Int(0, -1);
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public bool Equals(Vector2Int o) => this == o;
        public override bool Equals(object o) => o is Vector2Int v && this == v;
        public override int GetHashCode() => x * 73856093 ^ y * 19349663;
    }
    public static class Mathf
    {
        public static int Clamp(int v, int a, int b) => Math.Min(Math.Max(v, a), b);
        public static float Clamp(float v, float a, float b) => Math.Min(Math.Max(v, a), b);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static float Clamp01(float v) => Math.Min(Math.Max(v, 0f), 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static int RoundToInt(float v) => (int)Math.Round(v, MidpointRounding.ToEven);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Abs(int v) => Math.Abs(v);
    }
    public struct Color { public static Color white => new Color(1f, 1f, 1f, 1f); public float r, g, b, a; public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; } }
    public static class Debug { public static void Log(object m) { } public static void LogWarning(object m) { } public static void LogError(object m) { } }
    public class ScriptableObject { public string name; public static T CreateInstance<T>() where T : ScriptableObject, new() => new T(); }
    public class TextAsset { public string text; }
    public static class Resources { public static T Load<T>(string p) where T : class => null; }
    public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; }
}
namespace UnityEngine
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0f, 0f);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.x * s, a.y * s);
        public static Vector2 operator /(Vector2 a, float s) => new Vector2(a.x / s, a.y / s);
        public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);
        public float magnitude => (float)Math.Sqrt(x * x + y * y);
        public bool Equals(Vector2 o) => this == o;
        public override bool Equals(object o) => o is Vector2 v && this == v;
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
    }
}
namespace mumu
{
    /// 方向の表（GridUtility.cs は UI を含むので、マップ生成に必要な分だけ置く）
    public static class GridUtility
    {
        public static readonly UnityEngine.Vector2Int[] Directions4 = { UnityEngine.Vector2Int.up, UnityEngine.Vector2Int.down, UnityEngine.Vector2Int.left, UnityEngine.Vector2Int.right };
    }
}
