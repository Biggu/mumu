using UnityEngine;

namespace mumu
{
    /// <summary>
    /// マップの操作の設定（ScriptableObject。Resources/Settings/MapInputSettings）。ドラッグ・拡大縮小・タップの数値をここで変える
    /// </summary>
    [CreateAssetMenu(fileName = "MapInputSettings", menuName = "mumu/Map Input Settings")]
    public class MapInputSettings : ScriptableObject
    {
        /// <summary>設定のパス（Resources から）</summary>
        public const string AssetPath = "Settings/MapInputSettings";

        private static MapInputSettings current = null;

        /// <summary>今の設定（アセットが無ければ既定の値）</summary>
        public static MapInputSettings Current
        {
            get
            {
                if (current != null)
                {
                    return current;
                }
                current = ResourceManager.LoadResource<MapInputSettings>(AssetPath, false);
                if (current != null)
                {
                    return current;
                }
                current = CreateInstance<MapInputSettings>();
                return current;
            }
        }

        [Header("ドラッグ")]
        [Tooltip("ドラッグで1マス進むごとの待ち時間（秒）。0 で待たない（速い）")]
        public float DragDelayPerBlock = 0.2f;
        [Tooltip("ドラッグで経路を1マス戻るごとの待ち時間（秒）。進む時の待ち時間とは別。0 で待たない")]
        public float DragRewindDelayPerBlock = 0.35f;
        [Tooltip("先端からこのマス数以内なら、押してもドラッグを始められる")]
        public int NoDragRange = 2;
        [Tooltip("ドラッグで1マス進むのに必要な指の移動（マス幅に対する割合）")]
        public float DragStepRate = 0.5f;

        [Header("拡大・移動")]
        [Tooltip("初期表示の倍率。FitScale に対する倍率（1 = FitScale、1.2 = 20% 拡大）")]
        [Min(0.01f)]
        public float DefaultZoomRate = 1f;
        [Tooltip("初めて表示した時、マップが画面に占める割合（横）")]
        public float FitRateX = 0.85f;
        [Tooltip("初めて表示した時、マップが画面に占める割合（縦）")]
        public float FitRateY = 0.75f;
        [Tooltip("初期倍率に対する最小倍率")]
        public float MinZoomRate = 0.6f;
        [Tooltip("初期倍率に対する最大倍率")]
        public float MaxZoomRate = 2.5f;
        [Tooltip("拡大の上限（絶対値）")]
        public float MaxScaleAbsolute = 7f;
        [Tooltip("マップが画面より小さい時、中心を画面端の何割まで動かせるか")]
        public float PanLimitRate = 0.8f;
        [Tooltip("先端へ寄る時の移動時間（秒）")]
        public float FocusDuration = 0.35f;
        [Header("自動フォーカス")]
        [Tooltip("先端が画面端に近づき、さらに端方向へ動いている時、自動で先端を中央へ寄せる")]
        public bool AutoFocusNearEdge = true;
        [Tooltip("画面の各端から何割以内を端付近と判定するか（0.18 = 画面の18%）")]
        [Range(0f, 0.49f)]
        public float AutoFocusEdgeMarginRate = 0.18f;
        [Tooltip("自動フォーカスの連続発生を防ぐ待ち時間（秒）")]
        [Min(0f)]
        public float AutoFocusCooldown = 0.75f;
        [Tooltip("ホイール1段あたりの拡大率")]
        public float ScrollZoomStep = 1.1f;
        [Tooltip("ホイール1回で拡大縮小に使う上限")]
        public float MaxScrollDelta = 3f;

        [Header("タップ")]
        [Tooltip("タップと見なす指の移動の上限（ピクセル）")]
        public float TapMoveThreshold = 20f;
        [Tooltip("タップと見なす時間の上限（秒）")]
        public float TapMaxDuration = 0.3f;
        [Tooltip("ダブルタップと見なす間隔（秒）")]
        public float DoubleTapInterval = 0.35f;
        [Tooltip("ダブルタップと見なす2回の距離（ピクセル）")]
        public float DoubleTapDistance = 80f;

        [Header("タッチパッド（経路をドラッグしている間、指の下に出る丸）")]
        [Tooltip("丸の大きさ（ピクセル）")]
        public float TouchPadSize = 60f;
        [Tooltip("つまみが、押した場所（輪の中心）から動ける最大の距離（ピクセル）。輪の半径と同じにする")]
        public float TouchPadMaxDistance = 30f;
        [Tooltip("丸の透明度（0 = 見えない、1 = はっきり）")]
        [Range(0f, 1f)]
        public float TouchPadAlpha = 0.25f;
        [Tooltip("タッチパッドを出すか（オフにすると何も出ない）")]
        public bool TouchPadEnabled = true;
        [Tooltip("画面の左下から、タッチパッドまでの余白（ピクセル）")]
        public float TouchPadMargin = 40f;
    }
}
