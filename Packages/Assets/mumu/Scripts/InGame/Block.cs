using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace mumu
{
    /// <summary>
    /// マップの1ブロック
    /// </summary>
    public class Block : MonoBehaviour
    {
        /// <summary>通れない・通過済みブロックの持ち上げ量</summary>
        public const float RaiseHeight = 12f;
        /// <summary>クリア時に Goal のパグが横にずれる量（もう1匹と並ぶため）</summary>
        public const float GoalPairOffset = 12f;

        public const string BodyName = "body";
        public const string StateDown = "Down";
        public const string StateUp = "Up";
        public const string StatePopUp = "PopUp";
        public const string StateDrop = "Drop";

        private static readonly int StateDownHash = Animator.StringToHash(StateDown);
        private static readonly int StateUpHash = Animator.StringToHash(StateUp);
        private static readonly int StatePopUpHash = Animator.StringToHash(StatePopUp);
        private static readonly int StateDropHash = Animator.StringToHash(StateDrop);

        private const float PugCryFps = 4f;
        private const float PugSitFps = 3f;
        private const string PawSpritePath = "Sprites/Common/paw";
        private const float PawSize = 14f;
        /// <summary>足あとを左右にずらす量（交互に並べる）</summary>
        private const float PawSideOffset = 4f;
        private const float PugHappyFps = 8f;
        private const float PugPunchAmount = 0.35f;
        private const float PugPunchDuration = 0.3f;
        private const float ShakeAmplitude = 4f;
        private const float ShakeDuration = 0.3f;
        private const float MessageDuration = 1.2f;
        private const float MessagePunchAmount = 0.3f;
        private const float MessagePunchDuration = 0.2f;
        private const float CheckpointPunchAmount = 0.6f;
        private const float CheckpointPunchDuration = 0.3f;
        private const float HighlightPulseSpeed = 5f;
        private const float HighlightMinAlpha = 0.45f;

        private const string CheckpointStarPath = "Sprites/UI/ui_star_on";
        private const float CheckpointStarSize = 28f;
        /// <summary>ブロックの中心から★の中心までの高さ（持ち上げは body の移動に含まれる）</summary>
        private const float CheckpointStarY = 44f;
        private const float CheckpointStarBob = 3f;
        private const float CheckpointStarBobSpeed = 3f;
        /// <summary>未通過の★の濃さと大きさ</summary>
        private const float CheckpointStarIdleAlpha = 0.6f;
        private const float CheckpointStarIdleScale = 0.8f;
        private const float CheckpointJumpHeight = 36f;
        private const float CheckpointJumpScale = 0.8f;
        private const float CheckpointJumpDuration = 0.6f;

        private static Sprite checkpointStarSprite = null;
        private static Sprite pawSprite = null;

        private const string GimmickSpriteFolder = "Sprites/Common/";
        private const float RouteIconSize = 34f;
        private const float BoneSize = 30f;
        /// <summary>ブロックの中心からほねの中心までの高さ（チェックポイントの★より下）</summary>
        private const float BoneY = 28f;
        private const float BoneBob = 2.5f;
        private const float BoneBobSpeed = 4f;
        private const float BonePickDuration = 0.35f;
        private const float BonePickRise = 30f;
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
        private static readonly Color CrumbledColor = new Color(0.55f, 0.5f, 0.5f);

        private static readonly Dictionary<string, Sprite> gimmickSprites = new Dictionary<string, Sprite>();

        /// <summary>通れないブロックの土台の色（暗くして通れるマスと見分けやすくする。デコの色は変えない）</summary>
        public static readonly Color ObstructTint = new Color(0.58f, 0.56f, 0.62f);

        /// <summary>ブロック画像</summary>
        [SerializeField]
        private Image image;
        /// <summary>ブロックの上のデコ画像（通れないブロックの岩・木など）</summary>
        [SerializeField]
        private Image deco;
        /// <summary>ラベル</summary>
        [SerializeField]
        private TextMeshProUGUI text;
        /// <summary>次に進めるブロックの上面の光る枠</summary>
        [SerializeField]
        private Image highlight;
        /// <summary>Goal のパグ（泣いている女の子）</summary>
        [FormerlySerializedAs("flag")]
        [SerializeField]
        private GameObject pug;
        /// <summary>経路の線の親（上面に沿わせるため Y を潰している）</summary>
        [SerializeField]
        private GameObject lineRoot;
        /// <summary>前のブロックへの線</summary>
        [SerializeField]
        private RectTransform linePrevious;
        /// <summary>次のブロックへの線</summary>
        [SerializeField]
        private RectTransform lineNext;

        private Animator animator = null;
        private RectTransform body = null;
        private SpriteFrameAnimation pugAnimation = null;
        private PugSprites pugSprites = null;
        private Vector2 pugBasePosition = Vector2.zero;
        private eBlockType blockType = eBlockType.Empty;
        private readonly List<eBlockSubType> blockSubTypes = new();
        private ThemeSprites sprites = null;

        /// <summary>経路上の順番（-1 = 経路外、0 = Start）</summary>
        private int pathStep = -1;
        private bool isGoalUnlocked = false;
        private bool isCleared = false;
        /// <summary>男の子が近くに来て Goal の子が泣きやんでいるか</summary>
        private bool isGoalNoticed = false;
        /// <summary>経路上の足あと</summary>
        private RectTransform paw = null;
        /// <summary>おにわの地面（通れるマスを最初から草にする）</summary>
        private bool isGardenGround = false;
        /// <summary>上面に寝かせたギミックのアイコン（やじるし・まがる・まっすぐ）</summary>
        private Image routeIcon = null;
        /// <summary>浮かんでいるほね</summary>
        private Image boneIcon = null;
        /// <summary>立っている扉</summary>
        private Image gateIcon = null;
        private bool isGateOpen = false;
        private bool isCrumbled = false;
        private CancellationTokenSource crumbleCts = null;
        private CancellationTokenSource boneCts = null;
        /// <summary>ほねを拾った時刻（拾う演出用）</summary>
        private float bonePickTime = float.NegativeInfinity;
        /// <summary>一時表示中のテキスト</summary>
        private string message = null;
        /// <summary>デバッグ表示のテキスト（null で通常表示）</summary>
        private string debugLabel = null;
        /// <summary>チェックポイント番号（0 = なし）</summary>
        private int checkpointNumber = 0;

        private CancellationTokenSource messageCts = null;
        private CancellationTokenSource highlightCts = null;
        private CancellationTokenSource checkpointCts = null;

        /// <summary>チェックポイントの★（チェックポイントのみ生成）</summary>
        private Image checkpointStar = null;
        /// <summary>★が跳ねた時刻</summary>
        private float checkpointJumpTime = float.NegativeInfinity;

        /// <summary>描画順（大きいほど手前）</summary>
        public int SortIndex { get; private set; }
        /// <summary>ブロック種別</summary>
        public eBlockType BlockType => blockType;
        /// <summary>持ち上がっているか（アニメーション中も最終状態）</summary>
        public bool IsRaised { get; private set; }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="_type"></param>
        /// <param name="_subTypes"></param>
        /// <param name="_sprites"></param>
        public void Setup(eBlockType _type, IReadOnlyList<eBlockSubType> _subTypes, ThemeSprites _sprites)
        {
            blockType = _type;
            blockSubTypes.Clear();
            blockSubTypes.AddRange(_subTypes);
            sprites = _sprites;
            animator = GetComponent<Animator>();
            body = (RectTransform)transform.Find(BodyName);

            // 入力は MapViewController で座標判定するためレイキャスト不要
            image.raycastTarget = false;
            image.color = getBaseColor();
            text.raycastTarget = false;
            applyDeco();

            // パグは SetupPug で画像が設定されるまで非表示
            pug.SetActive(false);
            highlight.raycastTarget = false;
            highlight.gameObject.SetActive(false);
            lineRoot.SetActive(false);
            applyGimmicks();

            applyVisual();

            // 初期状態はアニメーションなし
            IsRaised = shouldRaise();
            playState(IsRaised ? StateUpHash : StateDownHash);
        }

        /// <summary>
        /// 経路上の順番を設定
        /// </summary>
        /// <param name="_step"></param>
        public void SetPathStep(int _step)
        {
            if (pathStep == _step)
            {
                return;
            }

            bool isPassed = pathStep < 0 && _step >= 0;
            pathStep = _step;
            applyVisual();
            if (isPassed)
            {
                bonePickTime = Time.unscaledTime;
            }

            if (isPassed && checkpointNumber > 0)
            {
                UiTween.PunchScale(text.transform, Vector3.one, CheckpointPunchAmount, CheckpointPunchDuration, destroyCancellationToken).Forget();
                checkpointJumpTime = Time.unscaledTime;
            }

            bool raise = shouldRaise();
            if (raise != IsRaised)
            {
                IsRaised = raise;
                playState(raise ? StatePopUpHash : StateDropHash);
            }
        }

        /// <summary>
        /// 次に進めるブロックの光る枠
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetHighlight(bool _isVisible)
        {
            if (!_isVisible)
            {
                cancel(ref highlightCts);
                highlight.gameObject.SetActive(false);
                return;
            }

            if (highlightCts == null)
            {
                highlight.gameObject.SetActive(true);
                highlightCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                highlightPulseAsync(highlightCts.Token).Forget();
            }
        }

        /// <summary>
        /// Goal のパグを設定
        /// </summary>
        /// <param name="_pugSprites"></param>
        public void SetupPug(PugSprites _pugSprites)
        {
            if (blockType != eBlockType.Goal || _pugSprites == null)
            {
                return;
            }

            pugSprites = _pugSprites;
            pugAnimation = pug.GetComponent<SpriteFrameAnimation>();
            if (pugAnimation == null)
            {
                pugAnimation = pug.AddComponent<SpriteFrameAnimation>();
            }

            Image pugImage = pug.GetComponent<Image>();
            pugImage.preserveAspect = true;
            pugImage.raycastTarget = false;
            pugImage.color = Color.white;
            pugBasePosition = ((RectTransform)pug.transform).anchoredPosition;
            PugAura.Attach((RectTransform)pug.transform, pugSprites.GirlTier, ((RectTransform)pug.transform).sizeDelta.x);

            pug.SetActive(true);
            applyPug();
        }

        /// <summary>
        /// 経路の線（前後のブロックへの方向、zero でその側なし）
        /// </summary>
        /// <param name="_isOnPath"></param>
        /// <param name="_toPrevious"></param>
        /// <param name="_toNext"></param>
        public void SetLine(bool _isOnPath, Vector2Int _toPrevious, Vector2Int _toNext)
        {
            lineRoot.SetActive(_isOnPath);
            if (!_isOnPath)
            {
                return;
            }

            setLineSegment(linePrevious, _toPrevious);
            setLineSegment(lineNext, _toNext);
            setPaw(_toPrevious, _toNext);
        }

        /// <summary>
        /// 進む向きに足あとを置く（Start・Goal には置かない）
        /// </summary>
        /// <param name="_toPrevious"></param>
        /// <param name="_toNext"></param>
        private void setPaw(Vector2Int _toPrevious, Vector2Int _toNext)
        {
            bool isVisible = blockType != eBlockType.Start && blockType != eBlockType.Goal;
            if (!isVisible)
            {
                if (paw != null)
                {
                    paw.gameObject.SetActive(false);
                }
                return;
            }

            if (paw == null)
            {
                if (pawSprite == null)
                {
                    pawSprite = ResourceManager.Instance.Load<Sprite>(PawSpritePath);
                }
                var go = new GameObject("paw", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(lineRoot.transform, false);
                Image pawImage = go.GetComponent<Image>();
                pawImage.sprite = pawSprite;
                pawImage.raycastTarget = false;
                pawImage.preserveAspect = true;
                paw = pawImage.rectTransform;
                paw.sizeDelta = new Vector2(PawSize, PawSize);
            }

            // 次のマスへ向ける（先端は前のマスから来た向き）
            Vector2Int forward = _toNext != Vector2Int.zero ? _toNext : -_toPrevious;
            paw.gameObject.SetActive(forward != Vector2Int.zero);
            float angle = getPlaneAngle(forward) - 90f;
            paw.localRotation = Quaternion.Euler(0f, 0f, angle);

            // 左右の足を交互に
            float side = (pathStep % 2 == 0 ? 1f : -1f) * PawSideOffset;
            paw.anchoredPosition = (Vector2)(paw.localRotation * new Vector3(side, 0f, 0f));
            paw.SetAsLastSibling();
        }

        /// <summary>
        /// おにわの地面にする（通れるマスを通過済みの見た目に）
        /// </summary>
        public void SetGardenGround()
        {
            isGardenGround = true;
            applyVisual();
        }

        /// <summary>
        /// 男の子が近くに来たか（Goal の子が泣きやんでこっちを見る）
        /// </summary>
        /// <param name="_isNoticed"></param>
        public void SetGoalNoticed(bool _isNoticed)
        {
            if (blockType != eBlockType.Goal || isGoalNoticed == _isNoticed)
            {
                return;
            }

            isGoalNoticed = _isNoticed;
            applyPug();
            if (_isNoticed)
            {
                punchPug();
            }
        }

        /// <summary>
        /// Goal の解放状態
        /// </summary>
        /// <param name="_isUnlocked"></param>
        public void SetGoalUnlocked(bool _isUnlocked)
        {
            if (blockType != eBlockType.Goal || isGoalUnlocked == _isUnlocked)
            {
                return;
            }

            isGoalUnlocked = _isUnlocked;
            if (_isUnlocked)
            {
                punchPug();
            }
        }

        /// <summary>
        /// クリア状態（パグが喜んで横にずれる）
        /// </summary>
        /// <param name="_isCleared"></param>
        public void SetCleared(bool _isCleared)
        {
            if (blockType != eBlockType.Goal || isCleared == _isCleared)
            {
                return;
            }

            isCleared = _isCleared;
            applyPug();
            if (_isCleared)
            {
                punchPug();
            }
        }

        /// <summary>
        /// チェックポイント番号を設定
        /// </summary>
        /// <param name="_number"></param>
        public void SetCheckpoint(int _number)
        {
            checkpointNumber = _number;
            applyVisual();

            if (_number > 0 && checkpointStar == null)
            {
                createCheckpointStar();
            }
            if (checkpointStar != null)
            {
                checkpointStar.gameObject.SetActive(_number > 0);
            }
        }

        /// <summary>
        /// 横揺れ演出
        /// </summary>
        public void PlayShake()
        {
            if (body != null)
            {
                UiTween.ShakeX(body, ShakeAmplitude, ShakeDuration, destroyCancellationToken).Forget();
            }
        }

        /// <summary>
        /// テキストを一時表示
        /// </summary>
        /// <param name="_message"></param>
        public void ShowMessage(string _message)
        {
            cancel(ref messageCts);
            messageCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            showMessageAsync(_message, messageCts.Token).Forget();
        }

        /// <summary>
        /// クリア演出
        /// </summary>
        public void PlayCelebrate()
        {
            playState(StatePopUpHash);
        }

        /// <summary>
        /// 追加属性を持っているか
        /// </summary>
        /// <param name="_subType"></param>
        public bool HasSubType(eBlockSubType _subType)
        {
            return blockSubTypes.Contains(_subType);
        }

        /// <summary>
        /// 描画順を設定
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_maxX"></param>
        /// <param name="_maxY"></param>
        public void SetOrder(int _x, int _y, int _maxX, int _maxY)
        {
            SortIndex = (_maxX * _maxY) - (_y * _maxX + _x);
        }

        /// <summary>
        /// 破棄時に演出を止める
        /// </summary>
        private void OnDestroy()
        {
            cancel(ref messageCts);
            cancel(ref highlightCts);
            cancel(ref checkpointCts);
            cancel(ref boneCts);
            cancel(ref crumbleCts);
        }

        /// <summary>
        /// ギミックのアイコンを跳ねさせる（進めなかった時）
        /// </summary>
        public void PunchGimmick()
        {
            Image target = routeIcon != null ? routeIcon : gateIcon;
            if (target != null)
            {
                UiTween.PunchScale(target.transform, Vector3.one, GimmickPunchAmount, GimmickPunchDuration, destroyCancellationToken).Forget();
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
                UiTween.PunchScale(rect, rect.localScale, GimmickPunchAmount, GimmickPunchDuration, destroyCancellationToken).Forget();
            }
        }

        /// <summary>
        /// こわれる橋の状態（壊れると板が落ちてブロックが暗くなる）
        /// </summary>
        /// <param name="_isCrumbled"></param>
        public void SetCrumbled(bool _isCrumbled)
        {
            if (routeIcon == null || !blockSubTypes.Contains(eBlockSubType.Crumble) || isCrumbled == _isCrumbled)
            {
                return;
            }
            isCrumbled = _isCrumbled;
            image.color = getBaseColor();
            if (_isCrumbled)
            {
                cancel(ref crumbleCts);
                crumbleCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
                UiTween.MoveFade(routeIcon.rectTransform, routeIcon, new Vector2(0f, -CrumbleFall), CrumbleDuration, crumbleCts.Token).Forget();
                routeIcon = null;
                PlayShake();
            }
        }

        /// <summary>
        /// ギミックの見た目を作る（やじるし・まがる・まっすぐ・ワープ・こわれる橋は上面に寝かせ、扉は立て、ほね・カギは浮かべる）
        /// </summary>
        private void applyGimmicks()
        {
            eBlockSubType exclusive = GimmickUtility.GetExclusive(blockSubTypes);
            int color = GimmickUtility.GetColorIndex(exclusive);
            if (GimmickUtility.IsRoute(exclusive))
            {
                Image icon = createPlaneIcon(GimmickUtility.IsArrow(exclusive) ? "gimmick_arrow" : (exclusive == eBlockSubType.Turn ? "gimmick_turn" : "gimmick_straight"), Color.white);
                if (GimmickUtility.IsArrow(exclusive))
                {
                    icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, getPlaneAngle(GimmickUtility.GetArrowDirection(exclusive)));
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
                gateIcon = createGimmickImage("gate", body != null ? body : transform, "gimmick_gate", GateSize);
                gateIcon.color = GimmickUtility.Colors[color];
                gateIcon.rectTransform.pivot = new Vector2(0.5f, 0f);
                gateIcon.rectTransform.anchoredPosition = new Vector2(0f, GateY);
            }

            eBlockSubType key = GimmickUtility.GetKey(blockSubTypes);
            eBlockSubType treasure = GimmickUtility.GetTreasure(blockSubTypes);
            bool hasTreasure = treasure != eBlockSubType.None;
            if (hasTreasure)
            {
                string sprite = "gimmick_treasure" + (treasure - eBlockSubType.Treasure1 + 1);
                boneIcon = createFloating("treasure", sprite, TreasureSize, Color.white, key != eBlockSubType.None ? -FloatingSpacing : 0f);
            }
            if (key != eBlockSubType.None)
            {
                createFloating("key", "gimmick_key", KeySize, GimmickUtility.Colors[GimmickUtility.GetColorIndex(key)], hasTreasure ? FloatingSpacing : 0f);
            }
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

            Image icon = createGimmickImage("routeIcon", planeRect, _sprite, RouteIconSize);
            icon.color = _color;
            return icon;
        }

        /// <summary>
        /// 浮かんでいる拾うもの（ほね・カギ。拾うと上に飛んで消え、巻き戻すとまた出る）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        /// <param name="_x"></param>
        private Image createFloating(string _name, string _sprite, float _size, Color _color, float _x)
        {
            Image item = createGimmickImage(_name, body != null ? body : transform, _sprite, _size);
            item.color = _color;
            if (boneCts == null)
            {
                boneCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            }
            floatingAsync(item, _color, _x, boneCts.Token).Forget();
            return item;
        }

        /// <summary>
        /// ふわふわさせ、拾った時は上に飛ばして消す
        /// </summary>
        /// <param name="_item"></param>
        /// <param name="_color"></param>
        /// <param name="_x"></param>
        /// <param name="_token"></param>
        private async UniTaskVoid floatingAsync(Image _item, Color _color, float _x, CancellationToken _token)
        {
            RectTransform rect = _item.rectTransform;
            float phase = SortIndex * 0.7f + _x;
            while (!await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
            {
                float y = BoneY + Mathf.Sin(Time.unscaledTime * BoneBobSpeed + phase) * BoneBob;
                float alpha = 1f;
                float scale = 1f;
                if (pathStep >= 0)
                {
                    // 拾った直後だけ上に飛んで消える
                    float t = (Time.unscaledTime - bonePickTime) / BonePickDuration;
                    if (t >= 1f)
                    {
                        _item.enabled = false;
                        continue;
                    }
                    y += t * BonePickRise;
                    alpha = 1f - t;
                    scale = 1f + t * 0.5f;
                }
                _item.enabled = true;
                rect.anchoredPosition = new Vector2(_x, y);
                rect.localScale = new Vector3(scale, scale, 1f);
                _item.color = new Color(_color.r, _color.g, _color.b, alpha);
            }
        }
        /// <summary>
        /// ギミックの画像（Sprites/Common の画像を中央に置く）
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_parent"></param>
        /// <param name="_sprite"></param>
        /// <param name="_size"></param>
        private static Image createGimmickImage(string _name, Transform _parent, string _sprite, float _size)
        {
            if (!gimmickSprites.TryGetValue(_sprite, out Sprite sprite))
            {
                sprite = ResourceManager.Instance.Load<Sprite>(GimmickSpriteFolder + _sprite);
                gimmickSprites[_sprite] = sprite;
            }

            var go = new GameObject(_name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_size, _size);
            return image;
        }

        /// <summary>
        /// チェックポイントの★を作る（ブロックの上に浮かぶ）
        /// </summary>
        private void createCheckpointStar()
        {
            if (checkpointStarSprite == null)
            {
                checkpointStarSprite = ResourceManager.Instance.Load<Sprite>(CheckpointStarPath);
            }

            var go = new GameObject("checkpointStar", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(body != null ? body : transform, false);
            checkpointStar = go.GetComponent<Image>();
            checkpointStar.sprite = checkpointStarSprite;
            checkpointStar.raycastTarget = false;
            checkpointStar.preserveAspect = true;

            RectTransform rect = checkpointStar.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(CheckpointStarSize, CheckpointStarSize);

            checkpointCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            checkpointStarAsync(checkpointCts.Token).Forget();
        }

        /// <summary>
        /// ★をふわふわ動かし、通過時は回りながら跳ねる
        /// </summary>
        /// <param name="_token"></param>
        private async UniTaskVoid checkpointStarAsync(CancellationToken _token)
        {
            RectTransform rect = checkpointStar.rectTransform;
            while (!await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
            {
                bool isPassed = pathStep >= 0;
                float y = CheckpointStarY + Mathf.Sin(Time.unscaledTime * CheckpointStarBobSpeed + SortIndex) * CheckpointStarBob;
                float scale = isPassed ? 1f : CheckpointStarIdleScale;
                float angle = 0f;

                float t = (Time.unscaledTime - checkpointJumpTime) / CheckpointJumpDuration;
                if (t >= 0f && t < 1f)
                {
                    float arc = Mathf.Sin(t * Mathf.PI);
                    y += arc * CheckpointJumpHeight;
                    scale += arc * CheckpointJumpScale;
                    angle = -t * 360f;
                }

                rect.anchoredPosition = new Vector2(0f, y);
                rect.localScale = new Vector3(scale, scale, 1f);
                rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                Color color = checkpointStar.color;
                color.a = isPassed ? 1f : CheckpointStarIdleAlpha;
                checkpointStar.color = color;
            }
        }

        /// <summary>
        /// 光る枠を点滅させる
        /// </summary>
        /// <param name="_token"></param>
        private async UniTaskVoid highlightPulseAsync(CancellationToken _token)
        {
            while (!await UniTask.Yield(PlayerLoopTiming.Update, _token).SuppressCancellationThrow())
            {
                // 全ブロックで位相をそろえる
                float wave = Mathf.Sin(Time.unscaledTime * HighlightPulseSpeed) * 0.5f + 0.5f;
                Color color = highlight.color;
                color.a = Mathf.Lerp(HighlightMinAlpha, 1f, wave);
                highlight.color = color;
            }
        }

        /// <summary>
        /// 持ち上げるか
        /// </summary>
        private bool shouldRaise()
        {
            return BlockTypeUtility.IsObstruct(blockType) || blockType == eBlockType.Start || pathStep >= 0;
        }

        /// <summary>
        /// Animator のステートを再生
        /// </summary>
        /// <param name="_stateHash"></param>
        private void playState(int _stateHash)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.Play(_stateHash, 0, 0f);
            }
        }

        /// <summary>
        /// 線の片側を設定
        /// </summary>
        /// <param name="_segment"></param>
        /// <param name="_direction"></param>
        private void setLineSegment(RectTransform _segment, Vector2Int _direction)
        {
            bool isVisible = _direction != Vector2Int.zero;
            _segment.gameObject.SetActive(isVisible);
            if (isVisible)
            {
                _segment.localRotation = Quaternion.Euler(0f, 0f, getPlaneAngle(_direction));
            }
        }

        /// <summary>
        /// パグのアニメーションを反映（クリア前は泣き、クリアで喜ぶ）
        /// </summary>
        private void applyPug()
        {
            if (pugAnimation == null || pugSprites == null)
            {
                return;
            }

            if (isCleared)
            {
                pugAnimation.Play(pugSprites.GirlHappy, PugHappyFps);
            }
            else if (isGoalNoticed)
            {
                pugAnimation.Play(pugSprites.GirlSit, PugSitFps);
            }
            else
            {
                pugAnimation.Play(pugSprites.GirlCry, PugCryFps);
            }
            ((RectTransform)pug.transform).anchoredPosition = pugBasePosition + new Vector2(isCleared ? GoalPairOffset : 0f, 0f);
        }

        /// <summary>
        /// パグを跳ねさせる
        /// </summary>
        private void punchPug()
        {
            if (pugAnimation != null)
            {
                UiTween.PunchScale(pug.transform, Vector3.one, PugPunchAmount, PugPunchDuration, destroyCancellationToken).Forget();
            }
        }

        /// <summary>
        /// テキストを一定時間表示
        /// </summary>
        /// <param name="_message"></param>
        /// <param name="_token"></param>
        private async UniTaskVoid showMessageAsync(string _message, CancellationToken _token)
        {
            message = _message;
            applyVisual();
            UiTween.PunchScale(text.transform, Vector3.one, MessagePunchAmount, MessagePunchDuration, _token).Forget();

            if (await UniTask.Delay(TimeSpan.FromSeconds(MessageDuration), cancellationToken: _token).SuppressCancellationThrow())
            {
                return;
            }

            message = null;
            applyVisual();
        }

        /// <summary>
        /// CancellationTokenSource を止めて破棄
        /// </summary>
        /// <param name="_cts"></param>
        private static void cancel(ref CancellationTokenSource _cts)
        {
            if (_cts == null)
            {
                return;
            }
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        /// <summary>
        /// グリッド方向 → 上面での角度（画像は右向き）
        /// </summary>
        /// <param name="_direction"></param>
        private static float getPlaneAngle(Vector2Int _direction)
        {
            if (_direction.x > 0) return 45f;    // 右上
            if (_direction.x < 0) return 225f;   // 左下
            if (_direction.y > 0) return -45f;   // 右下（エディタの下方向）
            return 135f;                         // 左上（エディタの上方向）
        }

        /// <summary>
        /// 画像とラベルを反映
        /// </summary>
        private void applyVisual()
        {
            if (blockType == eBlockType.Empty)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            image.sprite = getSprite();
            image.color = getBaseColor();
            text.text = message ?? debugLabel ?? getLabel();
        }

        /// <summary>
        /// ブロック画像の色（通れないブロックは暗く、壊れた橋はくすませる）
        /// </summary>
        private Color getBaseColor()
        {
            if (BlockTypeUtility.IsObstruct(blockType))
            {
                return ObstructTint;
            }
            return isCrumbled ? CrumbledColor : Color.white;
        }

        /// <summary>
        /// デバッグ用の文字を表示（null で戻す）
        /// </summary>
        /// <param name="_label"></param>
        public void SetDebugLabel(string _label)
        {
            if (debugLabel == _label)
            {
                return;
            }
            debugLabel = _label;
            applyVisual();
        }

        /// <summary>
        /// デコの表示切り替え
        /// </summary>
        /// <param name="_isVisible"></param>
        public void SetDecoVisible(bool _isVisible)
        {
            deco.gameObject.SetActive(_isVisible && BlockTypeUtility.GetObstructIndex(blockType) >= 0);
        }

        /// <summary>
        /// 通れないブロックならデコを表示
        /// </summary>
        private void applyDeco()
        {
            int decoIndex = BlockTypeUtility.GetObstructIndex(blockType);
            deco.raycastTarget = false;
            deco.gameObject.SetActive(decoIndex >= 0);
            if (decoIndex < 0)
            {
                return;
            }

            Sprite sprite = sprites.GetDeco(decoIndex);
            deco.sprite = sprite;

            // ブロックの下端にそろえ、背の高いデコは上にはみ出して奥のブロックに重なる
            RectTransform rect = deco.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            float width = ((RectTransform)transform).rect.width;
            rect.sizeDelta = new Vector2(0f, width * sprite.rect.height / sprite.rect.width);
        }

        /// <summary>
        /// 表示する画像
        /// </summary>
        private Sprite getSprite()
        {
            if (BlockTypeUtility.IsObstruct(blockType))
            {
                return sprites.Obstruct;
            }

            switch (blockType)
            {
                case eBlockType.Water:
                    return sprites.Water;
                case eBlockType.Bridge:
                    return sprites.Bridge;
                case eBlockType.Start:
                case eBlockType.Goal:
                    return sprites.Touched;
                default:
                    return pathStep >= 0 || isGardenGround ? sprites.Touched : sprites.Untouched;
            }
        }

        /// <summary>
        /// 表示するラベル
        /// </summary>
        private string getLabel()
        {
            // TODO: Start・チェックポイントのアイコン追加後に差し替え
            if (blockType == eBlockType.Start)
            {
                return "S";
            }
            return checkpointNumber > 0 ? checkpointNumber.ToString() : "";
        }
    }
}
