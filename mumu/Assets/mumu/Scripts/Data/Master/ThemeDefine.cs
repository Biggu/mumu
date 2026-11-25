using System.Collections.Generic;
using UnityEngine;

namespace mumu
{
    /// <summary>
    /// ときどき画面を横切る生き物（Sprites/Common/critters: critter_{名前小文字}_N）
    /// </summary>
    public enum eCritter
    {
        None,
        Butterfly,
        Gull,
        Bunny,
        Candy,
        Plane,
    }

    /// <summary>
    /// テーマ定義（画像は Resources/Sprites/Themes/theme_{id:000000}/）
    /// </summary>
    public static class ThemeDefine
    {
        /// <summary>
        /// 雰囲気演出の設定（Count = 0 で演出なし）
        /// </summary>
        public class AmbientSettings
        {
            /// <summary>色（ランダムに選ぶ）</summary>
            public Color[] Colors;
            /// <summary>1粒の大きさ</summary>
            public Vector2 Size;
            /// <summary>粒の数</summary>
            public int Count;
            /// <summary>最低速度</summary>
            public float MinSpeed;
            /// <summary>最高速度</summary>
            public float MaxSpeed;
            /// <summary>横揺れ幅</summary>
            public float Sway;
            /// <summary>下から上へ動くか</summary>
            public bool Rise;
            /// <summary>回転するか</summary>
            public bool Spin = true;
        }

        /// <summary>
        /// テーマ情報
        /// </summary>
        public class Info
        {
            /// <summary>名前</summary>
            public string Name;
            /// <summary>雰囲気演出</summary>
            public AmbientSettings Ambient;
        }

        /// <summary>初期テーマ</summary>
        public const int DefaultThemeId = 1;
        /// <summary>ボーナスステージのテーマ（金色。おさんぽや一覧のテーマ選びには入らない）</summary>
        public const int BonusThemeId = 40;
        /// <summary>ボーナスステージの背景の色合い（金色）</summary>
        private static readonly Color BonusTint = new Color(1f, 0.84f, 0.4f, 1f);

        private static readonly AmbientSettings None = new AmbientSettings { Count = 0 };

        /// <summary>
        /// 落ち葉の設定
        /// </summary>
        /// <param name="_colors"></param>
        private static AmbientSettings leaves(params Color[] _colors) => new AmbientSettings
        {
            Colors = _colors, Size = new Vector2(12f, 7f), Count = 18, MinSpeed = 30f, MaxSpeed = 65f, Sway = 40f,
        };

        /// <summary>
        /// 花びらの設定
        /// </summary>
        /// <param name="_colors"></param>
        private static AmbientSettings petals(params Color[] _colors) => new AmbientSettings
        {
            Colors = _colors, Size = new Vector2(10f, 7f), Count = 30, MinSpeed = 25f, MaxSpeed = 55f, Sway = 50f,
        };

        /// <summary>
        /// 雪の設定
        /// </summary>
        private static AmbientSettings snow() => new AmbientSettings
        {
            Colors = new[] { new Color(1f, 1f, 1f, 0.9f), new Color(0.85f, 0.92f, 1f, 0.9f) },
            Size = new Vector2(6f, 6f), Count = 45, MinSpeed = 30f, MaxSpeed = 70f, Sway = 25f, Spin = false,
        };

        /// <summary>
        /// 星・光の粒の設定（宇宙）
        /// </summary>
        /// <param name="_colors"></param>
        private static AmbientSettings sparkle(params Color[] _colors) => new AmbientSettings
        {
            Colors = _colors, Size = new Vector2(3f, 3f), Count = 30, MinSpeed = 8f, MaxSpeed = 20f, Sway = 6f, Spin = false,
        };

        /// <summary>
        /// 泡の設定（海の底）
        /// </summary>
        private static AmbientSettings bubbles() => new AmbientSettings
        {
            Colors = new[] { new Color(0.8f, 0.95f, 1f, 0.6f), new Color(1f, 1f, 1f, 0.5f) },
            Size = new Vector2(5f, 5f), Count = 26, MinSpeed = 25f, MaxSpeed = 50f, Sway = 12f, Rise = true, Spin = false,
        };

        /// <summary>
        /// ほたるの設定（夜の森）
        /// </summary>
        private static AmbientSettings fireflies() => new AmbientSettings
        {
            Colors = new[] { new Color(0.95f, 0.95f, 0.45f, 0.9f), new Color(0.7f, 1f, 0.5f, 0.9f) },
            Size = new Vector2(4f, 4f), Count = 18, MinSpeed = 6f, MaxSpeed = 18f, Sway = 35f, Rise = true, Spin = false,
        };

        /// <summary>
        /// 火の粉の設定（お城）
        /// </summary>
        private static AmbientSettings embers() => new AmbientSettings
        {
            Colors = new[] { new Color(1f, 0.55f, 0.15f, 0.9f), new Color(1f, 0.8f, 0.3f, 0.8f) },
            Size = new Vector2(4f, 4f), Count = 24, MinSpeed = 20f, MaxSpeed = 45f, Sway = 18f, Rise = true, Spin = false,
        };

        /// <summary>
        /// 夜のテーマ用に雰囲気演出の色を暗くする
        /// </summary>
        /// <param name="_day"></param>
        private static AmbientSettings dim(AmbientSettings _day)
        {
            if (_day.Colors == null)
            {
                return _day;
            }
            var colors = new Color[_day.Colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                Color c = _day.Colors[i];
                colors[i] = new Color(c.r * 0.7f, c.g * 0.75f, c.b * 0.9f, c.a);
            }
            return new AmbientSettings
            {
                Colors = colors, Size = _day.Size, Count = _day.Count, MinSpeed = _day.MinSpeed,
                MaxSpeed = _day.MaxSpeed, Sway = _day.Sway, Rise = _day.Rise, Spin = _day.Spin,
            };
        }

        private static readonly Dictionary<int, Info> infos = new Dictionary<int, Info>
        {
            { 1, new Info { Name = "meadow", Ambient = leaves(new Color(0.6f, 0.9f, 0.4f, 0.8f), new Color(0.75f, 0.95f, 0.55f, 0.8f)) } },
            { 2, new Info { Name = "summer", Ambient = new AmbientSettings
                {
                    Colors = new[] { new Color(1f, 0.97f, 0.7f, 0.8f), new Color(1f, 1f, 1f, 0.7f) },
                    Size = new Vector2(5f, 5f), Count = 20, MinSpeed = 10f, MaxSpeed = 25f, Sway = 30f, Spin = false,
                } } },
            { 3, new Info { Name = "winter", Ambient = snow() } },
            { 4, new Info { Name = "autumn", Ambient = leaves(new Color(0.95f, 0.55f, 0.2f, 0.9f), new Color(0.85f, 0.35f, 0.15f, 0.9f), new Color(0.75f, 0.55f, 0.25f, 0.9f)) } },
            { 5, new Info { Name = "spring", Ambient = petals(new Color(1f, 1f, 1f, 0.9f), new Color(0.75f, 0.95f, 0.55f, 0.85f), new Color(1f, 0.95f, 0.6f, 0.9f)) } },
            { 6, new Info { Name = "sakura", Ambient = petals(new Color(1f, 0.75f, 0.85f, 0.9f), new Color(1f, 0.85f, 0.9f, 0.9f)) } },
            { 7, new Info { Name = "desert", Ambient = new AmbientSettings
                {
                    Colors = new[] { new Color(0.9f, 0.75f, 0.5f, 0.7f), new Color(0.85f, 0.65f, 0.4f, 0.6f) },
                    Size = new Vector2(4f, 4f), Count = 35, MinSpeed = 15f, MaxSpeed = 35f, Sway = 80f, Spin = false,
                } } },
            { 8, new Info { Name = "cake", Ambient = new AmbientSettings
                {
                    Colors = new[] { new Color(0.97f, 0.88f, 0.31f), new Color(0.38f, 0.78f, 0.94f), Color.white, new Color(0.5f, 0.88f, 0.5f), new Color(0.69f, 0.44f, 0.88f) },
                    Size = new Vector2(8f, 3f), Count = 35, MinSpeed = 40f, MaxSpeed = 80f, Sway = 15f,
                } } },
            { 9, new Info { Name = "pudding", Ambient = new AmbientSettings
                {
                    Colors = new[] { new Color(1f, 0.97f, 0.9f, 0.85f) },
                    Size = new Vector2(7f, 7f), Count = 15, MinSpeed = 15f, MaxSpeed = 30f, Sway = 20f, Spin = false,
                } } },
            { 10, new Info { Name = "city", Ambient = new AmbientSettings
                {
                    Colors = new[] { new Color(0.75f, 0.82f, 0.9f, 0.5f) },
                    Size = new Vector2(2f, 14f), Count = 50, MinSpeed = 420f, MaxSpeed = 600f, Sway = 0f, Spin = false,
                } } },
            { 11, new Info { Name = "village", Ambient = leaves(new Color(0.55f, 0.8f, 0.35f, 0.85f), new Color(0.9f, 0.8f, 0.35f, 0.85f)) } },
            { 12, new Info { Name = "sea", Ambient = new AmbientSettings
                {
                    Colors = new[] { new Color(0.85f, 0.95f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.45f) },
                    Size = new Vector2(8f, 8f), Count = 25, MinSpeed = 30f, MaxSpeed = 70f, Sway = 15f, Rise = true, Spin = false,
                } } },
            { 13, new Info { Name = "ice", Ambient = snow() } },
            { 14, new Info { Name = "office", Ambient = None } },
            { 15, new Info { Name = "space", Ambient = sparkle(new Color(1f, 1f, 1f, 0.9f), new Color(0.6f, 0.85f, 1f, 0.9f)) } },
            { 16, new Info { Name = "underwater", Ambient = bubbles() } },
            { 17, new Info { Name = "moon_forest", Ambient = fireflies() } },
            { 18, new Info { Name = "castle", Ambient = embers() } },
            { 19, new Info { Name = "mountain", Ambient = leaves(new Color(0.8f, 0.85f, 0.7f, 0.8f), new Color(0.6f, 0.7f, 0.5f, 0.8f)) } },
            { BonusThemeId, new Info { Name = "bonus", Ambient = new AmbientSettings
                {
                    Colors = new[] { new Color(1f, 0.85f, 0.3f, 0.95f), new Color(1f, 0.95f, 0.6f, 0.9f), Color.white },
                    Size = new Vector2(7f, 7f), Count = 40, MinSpeed = 10f, MaxSpeed = 30f, Sway = 20f, Spin = true,
                } } },
        };

        /// <summary>テーマごとの横切る生き物</summary>
        private static readonly Dictionary<int, eCritter> critters = new Dictionary<int, eCritter>
        {
            { 1, eCritter.Butterfly }, { 2, eCritter.Gull }, { 3, eCritter.Bunny }, { 4, eCritter.Butterfly },
            { 5, eCritter.Butterfly }, { 6, eCritter.Butterfly }, { 7, eCritter.Gull }, { 8, eCritter.Candy },
            { 9, eCritter.Candy }, { 10, eCritter.Plane }, { 11, eCritter.Butterfly }, { 12, eCritter.Gull },
            { 13, eCritter.Bunny }, { 14, eCritter.Plane },
            { 15, eCritter.Plane }, { 16, eCritter.Gull }, { 17, eCritter.Butterfly }, { 18, eCritter.Bunny }, { 19, eCritter.Gull },
        };

        /// <summary>夜のテーマを作る昼のテーマの数（昼 1〜14 → 夜 20〜33）</summary>
        private const int DayThemeCount = 14;
        /// <summary>昼の ID に足すと夜の ID になる数</summary>
        private const int NightOffset = 19;

        /// <summary>
        /// 夜のテーマを昼のテーマから作る（名前に night_ を付け、色を暗くし、生き物は昼と同じ）
        /// </summary>
        static ThemeDefine()
        {
            for (int day = 1; day <= DayThemeCount; day++)
            {
                int night = day + NightOffset;
                infos[night] = new Info { Name = "night_" + infos[day].Name, Ambient = dim(infos[day].Ambient) };
                critters[night] = critters[day];
            }
        }

        /// <summary>全テーマ ID</summary>
        public static IEnumerable<int> ThemeIds
        {
            get
            {
                foreach (int id in infos.Keys)
                {
                    if (id != BonusThemeId)
                    {
                        yield return id;
                    }
                }
            }
        }

        /// <summary>
        /// テーマの背景の色合い（ボーナスは金色、ほかは白 = そのまま）
        /// </summary>
        /// <param name="_themeId"></param>
        public static Color GetTint(int _themeId)
        {
            return _themeId == BonusThemeId ? BonusTint : Color.white;
        }

        /// <summary>
        /// テーマの横切る生き物
        /// </summary>
        /// <param name="_themeId"></param>
        public static eCritter GetCritter(int _themeId)
        {
            return critters.TryGetValue(_themeId, out eCritter critter) ? critter : eCritter.None;
        }

        /// <summary>
        /// テーマ名
        /// </summary>
        /// <param name="_themeId"></param>
        public static string GetName(int _themeId)
        {
            return infos.TryGetValue(_themeId, out Info info) ? info.Name : "unknown";
        }

        /// <summary>
        /// 雰囲気演出の設定
        /// </summary>
        /// <param name="_themeId"></param>
        public static AmbientSettings GetAmbient(int _themeId)
        {
            return infos.TryGetValue(_themeId, out Info info) ? info.Ambient : None;
        }
    }
}
