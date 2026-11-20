using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// ブロックのギミックの見た目（上面の矢印・まがる・まっすぐ・ワープ・こわれる橋、立っている扉、浮かぶほね・カギ）
    /// </summary>
    public class BlockGimmickView
    {
        private const float RouteIconSize = 34f;
        private const float GimmickPunchAmount = 0.4f;
        private const float GimmickPunchDuration = 0.25f;
        private const float KeySize = 34f;
        private const float TreasureSize = 34f;
        /// <summary>ほねとカギが同じマスにある時の左右のずれ</summary>
        private const float FloatingSpacing = 12f;
        private const float GateSize = 40f;
        /// <summary>ブロックの中心から扉の下端までの高さ（上面の中心）</summary>
        private const float GateY = 10f;
        /// <summary>開いた扉の縦の大きさと濃さ（倒れて薄くなる）</summary>
        private const float GateOpenScaleY = 0.3f;
        private const float GateOpenAlpha = 0.45f;
        private const float CrumbleFall = 30f;
        private const float CrumbleDuration = 0.5f;

        private readonly CancellationToken destroyToken;
        private readonly BlockFloatingItems floating;
        private Transform visualRoot = null;
        private GameObject lineRoot = null;

        /// <summary>上面に寝かせたギミックのアイコン（やじるし・まがる・まっすぐ・ワープ・こわれる橋）</summary>
        private Image routeIcon = null;
        /// <summary>立っている扉</summary>
        private Image gateIcon = null;
        private bool isGateOpen = false;
        private CancellationTokenSource crumbleCts = null;

        /// <summary>
        /// 停止のためのトークンを受け取る
        /// </summary>
        /// <param name="_destroyToken"></param>
        public BlockGimmickView(CancellationToken _destroyToken)
        {
            destroyToken = _destroyToken;
            floating = new BlockFloatingItems(_destroyToken);
        }

        /// <summary>上面のアイコンがあるか（こわれる橋の判定用）</summary>
        public bool HasRouteIcon => routeIcon != null;

        /// <summary>
        /// ギミックの見た目を作る（上面に寝かせるもの・立てるもの・浮かべるもの）
        /// </summary>
        /// <param name="_subTypes"></param>
        /// <param name="_visualRoot"></param>
        /// <param name="_lineRoot"></param>
        /// <param name="_sortIndex"></param>
        public void Build(System.Collections.Generic.IReadOnlyList<eBlockSubType> _subTypes, Transform _visualRoot, GameObject _lineRoot, int _sortIndex)
        {
            visualRoot = _visualRoot;
            lineRoot = _lineRoot;

            eBlockSubType exclusive = GimmickUtility.GetExclusive(_subTypes);
            int color = GimmickUtility.GetColorIndex(exclusive);
            if (GimmickUtility.IsRoute(exclusive))
            {
                Image icon = createPlaneIcon(GimmickUtility.IsArrow(exclusive) ? "gimmick_arrow" : (exclusive == eBlockSubType.Turn ? "gimmick_turn" : "gimmick_straight"), Color.white);
                if (GimmickUtility.IsArrow(exclusive))
                {
                    icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, GridUtility.GetPlaneAngle(GimmickUtility.GetArrowDirection(exclusive)));
                }
                routeIcon = icon;
            }
            else if (GimmickUtility.IsWarpIn(exclusive) || GimmickUtility.IsWarpOut(exclusive))
            {
                routeIcon = createPlaneIcon(GimmickUtility.IsWarpIn(exclusive) ? "gimmick_warp_in" : "gimmick_warp_out", GimmickUtility.Colors[color]);
            }
            else if (exclusive == eBlockSubType.Crumble)
            {
                routeIcon = createPlaneIcon("gimmick_crumble", Color.white);
            }
            else if (GimmickUtility.IsGate(exclusive))
            {
                // 扉は立てて置く（手前のブロックに隠れないよう少し上）
                gateIcon = GimmickSprites.CreateImage("gate", visualRoot, "gimmick_gate", GateSize);
                gateIcon.color = GimmickUtility.Colors[color];
                gateIcon.rectTransform.pivot = new Vector2(0.5f, 0f);
                gateIcon.rectTransform.anchoredPosition = new Vector2(0f, GateY);
            }

            eBlockSubType key = GimmickUtility.GetKey(_subTypes);
            eBlockSubType treasure = GimmickUtility.GetTreasure(_subTypes);
            bool hasTreasure = treasure != eBlockSubType.None;
            if (hasTreasure)
            {
                string sprite = "gimmick_treasure" + (treasure - eBlockSubType.Treasure1 + 1);
                floating.Add("treasure", sprite, TreasureSize, Color.white, key != eBlockSubType.None ? -FloatingSpacing : 0f, _sortIndex, visualRoot);
            }
            if (key != eBlockSubType.None)
            {
                floating.Add("key", "gimmick_key", KeySize, GimmickUtility.Colors[GimmickUtility.GetColorIndex(key)], hasTreasure ? FloatingSpacing : 0f, _sortIndex, visualRoot);
            }
        }

        /// <summary>
        /// 経路に乗ったか（拾うものの演出に使う）
        /// </summary>
        /// <param name="_isPassed"></param>
        /// <param name="_passedTime"></param>
        public void SetPassed(bool _isPassed, float _passedTime)
        {
            floating.SetPassed(_isPassed, _passedTime);
        }

        /// <summary>
        /// ギミックのアイコンを跳ねさせる（進めなかった時）
        /// </summary>
        public void Punch()
        {
            Image target = routeIcon != null ? routeIcon : gateIcon;
            if (target != null)
            {
                CommonUtility.Punch(target.transform, GimmickPunchAmount, GimmickPunchDuration, destroyToken);
            }
        }

        /// <summary>
        /// 扉の開け閉め（開くと柵が倒れる）
        /// </summary>
        /// <param name="_isOpen"></param>
        public void SetGateOpen(bool _isOpen)
        {
            if (gateIcon == null || isGateOpen == _isOpen)
            {
                return;
            }
            isGateOpen = _isOpen;
            RectTransform rect = gateIcon.rectTransform;
            rect.localScale = _isOpen ? new Vector3(1f, GateOpenScaleY, 1f) : Vector3.one;
            Color color = gateIcon.color;
            color.a = _isOpen ? GateOpenAlpha : 1f;
            gateIcon.color = color;
            if (_isOpen)
            {
                // 倒れた後の大きさを基準に跳ねさせる
                CommonUtility.Punch(rect, rect.localScale, GimmickPunchAmount, GimmickPunchDuration, destroyToken);
            }
        }

        /// <summary>
        /// こわれる橋の板を落とす（アイコンは薄くして消す）
        /// </summary>
        public void Crumble()
        {
            UiTween.MoveFade(routeIcon.rectTransform, routeIcon, new Vector2(0f, -CrumbleFall), CrumbleDuration, TokenUtility.Restart(ref crumbleCts, destroyToken)).Forget();
            routeIcon = null;
        }

        /// <summary>
        /// 動いている演出を止める
        /// </summary>
        public void Cancel()
        {
            TokenUtility.Cancel(ref crumbleCts);
            floating.Cancel();
        }

        /// <summary>
        /// 上面に寝かせたアイコン（経路の線と同じ平面、線より下に描く）
        /// </summary>
        /// <param name="_sprite"></param>
        /// <param name="_color"></param>
        private Image createPlaneIcon(string _sprite, Color _color)
        {
            var plane = new GameObject("gimmickPlane", typeof(RectTransform));
            var planeRect = (RectTransform)plane.transform;
            var lineRect = (RectTransform)lineRoot.transform;
            planeRect.SetParent(lineRect.parent, false);
            planeRect.SetSiblingIndex(lineRect.GetSiblingIndex());
            planeRect.anchorMin = lineRect.anchorMin;
            planeRect.anchorMax = lineRect.anchorMax;
            planeRect.anchoredPosition = lineRect.anchoredPosition;
            planeRect.localScale = lineRect.localScale;
            planeRect.sizeDelta = Vector2.zero;

            Image icon = GimmickSprites.CreateImage("routeIcon", planeRect, _sprite, RouteIconSize);
            icon.color = _color;
            return icon;
        }
    }
}
