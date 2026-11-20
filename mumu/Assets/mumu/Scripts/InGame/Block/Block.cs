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
    /// マップの1ブロック。見た目の部品（道・パグ・ギミック・★・揺れ）は各クラスに任せる
    /// </summary>
    public class Block : MonoBehaviour
    {
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

        /// <summary>通れないブロックの土台の色（暗くして通れるマスと見分けやすくする。デコの色は変えない）</summary>
        public static readonly Color ObstructTint = new Color(0.58f, 0.56f, 0.62f);
        /// <summary>壊れた橋の色（くすませる）</summary>
        private static readonly Color CrumbledColor = new Color(0.55f, 0.5f, 0.5f);

        /// <summary>チェックポイントの★が跳ねた時の大きさと時間</summary>
        private const float CheckpointPunchAmount = 0.6f;
        private const float CheckpointPunchDuration = 0.3f;
        private const float MessageDuration = 1.2f;
        private const float MessagePunchAmount = 0.3f;
        private const float MessagePunchDuration = 0.2f;

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
        private eBlockType blockType = eBlockType.Empty;
        private readonly List<eBlockSubType> blockSubTypes = new List<eBlockSubType>();
        private ThemeSprites sprites = null;

        /// <summary>経路上の順番（-1 = 経路外、0 = Start）</summary>
        private int pathStep = -1;
        /// <summary>経路に初めて乗った時刻（拾う・跳ねる演出用）</summary>
        private float passedTime = float.NegativeInfinity;
        private bool isCleared = false;
        /// <summary>男の子が近くに来て Goal の子が泣きやんでいるか</summary>
        private bool isGoalNoticed = false;
        private bool isGoalUnlocked = false;
        /// <summary>おにわの地面（通れるマスを最初から草にする）</summary>
        private bool isGardenGround = false;
        private bool isCrumbled = false;
        /// <summary>一時表示中のテキスト</summary>
        private string message = null;
        /// <summary>デバッグ表示のテキスト（null で通常表示）</summary>
        private string debugLabel = null;
        /// <summary>チェックポイント番号（0 = なし）</summary>
        private int checkpointNumber = 0;
        private CancellationTokenSource messageCts = null;

        private BlockPathView pathView = null;
        private BlockPugView pugView = null;
        private BlockGimmickView gimmickView = null;
        private BlockCheckpointStar checkpointStar = null;
        private BlockShake shake = null;

        /// <summary>描画順（大きいほど手前）</summary>
        public int SortIndex { get; private set; }
        /// <summary>ブロック種別</summary>
        public eBlockType BlockType => blockType;
        /// <summary>持ち上がっているか（アニメーション中も最終状態）</summary>
        public bool IsRaised { get; private set; }

        /// <summary>ギミック・★を置く親（body があればそこ、なければ自身）</summary>
        private Transform visualRoot => body != null ? body : transform;
        private bool isGoal => blockType == eBlockType.Goal;

        /// <summary>
        /// 初期化（見た目の部品を作り直す）
        /// </summary>
        /// <param name="_type"></param>
        /// <param name="_subTypes"></param>
        /// <param name="_sprites"></param>
        public void Setup(eBlockType _type, IReadOnlyList<eBlockSubType> _subTypes, ThemeSprites _sprites)
        {
            cancelAnimations();

            blockType = _type;
            blockSubTypes.Clear();
            blockSubTypes.AddRange(_subTypes);
            sprites = _sprites;
            animator = GetComponent<Animator>();
            body = (RectTransform)transform.Find(BodyName);

            pathView = new BlockPathView(lineRoot, linePrevious, lineNext, highlight, destroyCancellationToken);
            pugView = new BlockPugView(pug, destroyCancellationToken);
            gimmickView = new BlockGimmickView(destroyCancellationToken);
            checkpointStar = new BlockCheckpointStar(destroyCancellationToken);
            shake = new BlockShake(destroyCancellationToken);

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
            gimmickView.Build(blockSubTypes, visualRoot, lineRoot, SortIndex);

            applyVisual();

            // 初期状態はアニメーションなし
            IsRaised = shouldRaise();
            playState(IsRaised ? StateUpHash : StateDownHash);
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
                passedTime = Time.unscaledTime;
            }
            gimmickView.SetPassed(pathStep >= 0, passedTime);
            checkpointStar.SetPassed(pathStep >= 0, passedTime);

            if (isPassed && checkpointNumber > 0)
            {
                CommonUtility.Punch(text.transform, CheckpointPunchAmount, CheckpointPunchDuration, destroyCancellationToken);
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
            pathView.SetHighlight(_isVisible);
        }

        /// <summary>
        /// 経路の線（前後のブロックへの方向、zero でその側なし）
        /// </summary>
        /// <param name="_isOnPath"></param>
        /// <param name="_toPrevious"></param>
        /// <param name="_toNext"></param>
        public void SetLine(bool _isOnPath, Vector2Int _toPrevious, Vector2Int _toNext)
        {
            bool hasPaw = blockType != eBlockType.Start && blockType != eBlockType.Goal;
            pathView.SetLine(_isOnPath, _toPrevious, _toNext, hasPaw, pathStep);
        }

        /// <summary>
        /// ヒントの線（経路の外のマスだけ）
        /// </summary>
        /// <param name="_isHint"></param>
        /// <param name="_toPrevious"></param>
        /// <param name="_toNext"></param>
        public void SetHint(bool _isHint, Vector2Int _toPrevious, Vector2Int _toNext)
        {
            pathView.SetHint(_isHint, _toPrevious, _toNext);
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
            if (!isGoal || isGoalNoticed == _isNoticed)
            {
                return;
            }

            isGoalNoticed = _isNoticed;
            applyPug();
            if (_isNoticed)
            {
                pugView.Punch();
            }
        }

        /// <summary>
        /// Goal の解放状態
        /// </summary>
        /// <param name="_isUnlocked"></param>
        public void SetGoalUnlocked(bool _isUnlocked)
        {
            if (!isGoal || isGoalUnlocked == _isUnlocked)
            {
                return;
            }

            isGoalUnlocked = _isUnlocked;
            if (_isUnlocked)
            {
                pugView.Punch();
            }
        }

        /// <summary>
        /// クリア状態（パグが喜んで横にずれる）
        /// </summary>
        /// <param name="_isCleared"></param>
        public void SetCleared(bool _isCleared)
        {
            if (!isGoal || isCleared == _isCleared)
            {
                return;
            }

            isCleared = _isCleared;
            applyPug();
            if (_isCleared)
            {
                pugView.Punch();
            }
        }

        /// <summary>
        /// Goal のパグを設定
        /// </summary>
        /// <param name="_pugSprites"></param>
        public void SetupPug(PugSprites _pugSprites)
        {
            if (!isGoal || _pugSprites == null)
            {
                return;
            }

            pugView.Setup(_pugSprites);
            applyPug();
        }

        /// <summary>
        /// クリア演出
        /// </summary>
        public void PlayCelebrate()
        {
            playState(StatePopUpHash);
        }

        /// <summary>
        /// チェックポイント番号を設定（★は初めて番号が付いた時に作る）
        /// </summary>
        /// <param name="_number"></param>
        public void SetCheckpoint(int _number)
        {
            checkpointNumber = _number;
            applyVisual();

            if (_number > 0 && !checkpointStar.IsCreated)
            {
                checkpointStar.Create(visualRoot, SortIndex);
            }
            checkpointStar.SetVisible(_number > 0);
        }

        /// <summary>
        /// 横揺れ演出
        /// </summary>
        public void PlayShake()
        {
            shake.Play(body);
        }

        /// <summary>
        /// テキストを一時表示
        /// </summary>
        /// <param name="_message"></param>
        public void ShowMessage(string _message)
        {
            showMessageAsync(_message, TokenUtility.Restart(ref messageCts, destroyCancellationToken)).Forget();
        }

        /// <summary>
        /// ギミックのアイコンを跳ねさせる（進めなかった時）
        /// </summary>
        public void PunchGimmick()
        {
            gimmickView.Punch();
        }

        /// <summary>
        /// 扉の開け閉め（開くと柵が倒れる）
        /// </summary>
        /// <param name="_isOpen"></param>
        public void SetGateOpen(bool _isOpen)
        {
            gimmickView.SetGateOpen(_isOpen);
        }

        /// <summary>
        /// こわれる橋の状態（壊れると板が落ちてブロックが暗くなる）
        /// </summary>
        /// <param name="_isCrumbled"></param>
        public void SetCrumbled(bool _isCrumbled)
        {
            if (!gimmickView.HasRouteIcon || !blockSubTypes.Contains(eBlockSubType.Crumble) || isCrumbled == _isCrumbled)
            {
                return;
            }

            isCrumbled = _isCrumbled;
            image.color = getBaseColor();
            if (_isCrumbled)
            {
                gimmickView.Crumble();
                PlayShake();
            }
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
        /// 破棄時に演出を止める
        /// </summary>
        private void OnDestroy()
        {
            cancelAnimations();
        }

        /// <summary>
        /// 動いている演出をすべて止める
        /// </summary>
        private void cancelAnimations()
        {
            TokenUtility.Cancel(ref messageCts);
            pathView?.Cancel();
            gimmickView?.Cancel();
            checkpointStar?.Cancel();
            shake?.Cancel();
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
        /// パグのアニメーションを反映（クリア前は泣き、クリアで喜ぶ）
        /// </summary>
        private void applyPug()
        {
            pugView.Apply(isCleared, isGoalNoticed);
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
        /// 通れないブロックは暗く、壊れた橋はくすませる
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

            // コマ送りのデコ（スプライトシート）は絵を順に変える
            Sprite[] frames = sprites.GetDecoFrames(decoIndex);
            if (frames != null && frames.Length > 1)
            {
                CommonUtility.GetOrAddComponent<SpriteFrameAnimation>(deco.gameObject).Play(frames, ThemeSprites.DecoFps);
            }

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

        /// <summary>
        /// テキストを一定時間表示
        /// </summary>
        /// <param name="_message"></param>
        /// <param name="_token"></param>
        private async UniTaskVoid showMessageAsync(string _message, CancellationToken _token)
        {
            message = _message;
            applyVisual();
            CommonUtility.Punch(text.transform, MessagePunchAmount, MessagePunchDuration, _token);

            if (await UniTask.Delay(TimeSpan.FromSeconds(MessageDuration), cancellationToken: _token).SuppressCancellationThrow())
            {
                return;
            }

            message = null;
            applyVisual();
        }
    }
}
