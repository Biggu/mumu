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
                current = Resources.Load<MapInputSettings>(AssetPath);
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
        [Tooltip("先端からこのマス数以内なら、押してもドラッグを始められる")]
        public int NoDragRange = 2;
        [Tooltip("ドラッグで1マス進むのに必要な指の移動（マス幅に対する割合）")]
        public float DragStepRate = 0.5f;

        [Header("拡大・移動")]
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
    }
}
