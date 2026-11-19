using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace mumu
{
    /// <summary>
    /// おにわを歩きまわるパグ（座る・歩く・お昼寝・ついて歩く、タップで跳ねて名前を出す）
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GardenPug : MonoBehaviour, IPointerClickHandler
    {
        private const float SitFps = 3f;
        private const float WalkFps = 8f;
        private const float HappyFps = 8f;
        private const float WalkSpeed = 45f;
        private const float MinSitTime = 1.5f;
        private const float MaxSitTime = 5f;
        private const int MaxWalkSteps = 4;
        /// <summary>ついて歩く時の最大マス数</summary>
        private const int MaxFollowSteps = 6;
        private const float HappyDuration = 1.2f;
        private const float HopHeight = 16f;
        /// <summary>座り終わった時にお昼寝する確率</summary>
        private const float SleepRate = 0.15f;
        private const float MinSleepTime = 4f;
        private const float MaxSleepTime = 8f;
        /// <summary>Zzz の出る間隔</summary>
        private const float ZzzInterval = 0.9f;
        private const float ZzzDuration = 1.6f;
        private const float ZzzRise = 28f;
        /// <summary>お昼寝の時に見せるコマ（座りの目を閉じたコマ）</summary>
        private const int SleepFrameIndex = 2;
        private const float BubbleY = 52f;
        private const float BubbleFontSize = 22f;
        private static readonly Vector2 BubblePadding = new Vector2(24f, 10f);
        private static readonly Color BubbleColor = new Color(1f, 1f, 1f, 0.92f);
        private static readonly Color BubbleTextColor = new Color(0.29f, 0.17f, 0.06f);
        private static readonly Color ZzzColor = new Color(0.35f, 0.45f, 0.85f);
        private const string EquipButtonPath = "Sprites/UI/ui_button";
        private static readonly Vector2 EquipButtonSize = new Vector2(120f, 40f);
        private const string EquippedMarkPath = "Sprites/UI/ui_star_on";
        private const float EquippedMarkSize = 22f;
        private const float EquippedMarkY = 46f;
        /// <summary>吹き出しを出しておく時間（ボタンを押せるよう少し長め）</summary>
        private const float BubbleDuration = 3f;

        private enum eState
        {
            Sit,
            Walk,
            Happy,
            Sleep,
        }

        private CharacterSprites sprites = null;
        private CharacterData character = null;
        private GardenMap map = null;
        /// <summary>つれていく・はずすを押した時の処理</summary>
        private Action<GardenPug> onEquipAction = null;
        private MumuButton equipButton = null;
        private TextMeshProUGUI equipText = null;
        /// <summary>連れていく子の印</summary>
        private GameObject equippedMark = null;
        private SpriteFrameAnimation frameAnimation = null;
        private Image image = null;
        private RectTransform rect = null;
        private GameObject bubble = null;

        private readonly Queue<Vector2Int> route = new Queue<Vector2Int>();
        private Vector2Int cell = Vector2Int.zero;
        private Vector2 position = Vector2.zero;
        private eState state = eState.Sit;
        private float stateTime = 0f;
        private float sitTime = 0f;
        private float sleepTime = 0f;
        private float nextZzzTime = 0f;
        private float hideBubbleTime = 0f;
        private Sprite[] playing = null;

        /// <summary>描画順に使う画面上の高さ（低いほど手前）</summary>
        public float Depth => position.y;
        /// <summary>今いるマス</summary>
        public Vector2Int Cell => cell;
        /// <summary>描画順の基準のマス（歩いている時は今のマスと次のマスの手前の方）</summary>
        public Vector2Int AnchorCell
        {
            get
            {
                if (state != eState.Walk || route.Count == 0)
                {
                    return cell;
                }
                Block current = map.GetBlock(cell);
                Block next = map.GetBlock(route.Peek());
                return IsoDepth.Front(current, next) == next ? route.Peek() : cell;
            }
        }
        /// <summary>座っていて、ほかのことを始められるか</summary>
        public bool IsIdle => state == eState.Sit;
        /// <summary>キャラクター</summary>
        public CharacterData Character => character;

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_character"></param>
        /// <param name="_sprites"></param>
        /// <param name="_map"></param>
        /// <param name="_cell"></param>
        /// <param name="_size"></param>
        public static GardenPug Create(Transform _parent, CharacterData _character, CharacterSprites _sprites, GardenMap _map, Vector2Int _cell, float _size)
        {
            var go = new GameObject($"pug_{_character.Id}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            var pug = go.AddComponent<GardenPug>();
            pug.setup(_character, _sprites, _map, _cell, _size);
            return pug;
        }

        /// <summary>
        /// タップで起きて喜ぶ
        /// </summary>
        /// <param name="_eventData"></param>
        public void OnPointerClick(PointerEventData _eventData)
        {
            route.Clear();
            setState(eState.Happy);
            refreshEquipButton();
            bubble.SetActive(true);
            SoundManager sound = SoundManager.Instance;
            if (sound != null)
            {
                sound.PlaySe(eSe.Checkpoint);
            }
        }

        /// <summary>
        /// つれていく・はずすを押した時の処理を設定
        /// </summary>
        /// <param name="_action"></param>
        public void SetOnEquip(Action<GardenPug> _action)
        {
            onEquipAction = _action;
        }

        /// <summary>
        /// 連れていく子の印を出すか
        /// </summary>
        /// <param name="_isEquipped"></param>
        public void SetEquipped(bool _isEquipped)
        {
            equippedMark.SetActive(_isEquipped);
            refreshEquipButton();
        }

        /// <summary>
        /// つれていく・はずすボタン押下時の処理
        /// </summary>
        private void onClickEquipButton()
        {
            onEquipAction?.Invoke(this);
        }

        /// <summary>
        /// ボタンの文字（連れていく子なら はずす）
        /// </summary>
        private void refreshEquipButton()
        {
            if (equipText != null)
            {
                equipText.text = TextLocalizer.Get(equippedMark.activeSelf ? "PUG_UNEQUIP" : "PUG_EQUIP");
            }
        }

        /// <summary>
        /// ほかの子のところまでついて歩く（座っている時だけ、行けなければ何もしない）
        /// </summary>
        /// <param name="_leader"></param>
        public bool Follow(GardenPug _leader)
        {
            if (!IsIdle || _leader == null || _leader == this)
            {
                return false;
            }

            List<Vector2Int> path = map.FindPath(cell, _leader.Cell, MaxFollowSteps);
            // 相手と同じマスには入らず、となりで止まる
            if (path.Count > 0 && path[path.Count - 1] == _leader.Cell)
            {
                path.RemoveAt(path.Count - 1);
            }
            if (path.Count == 0)
            {
                return false;
            }

            route.Clear();
            foreach (Vector2Int step in path)
            {
                route.Enqueue(step);
            }
            setState(eState.Walk);
            return true;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="_character"></param>
        /// <param name="_sprites"></param>
        /// <param name="_map"></param>
        /// <param name="_cell"></param>
        /// <param name="_size"></param>
        private void setup(CharacterData _character, CharacterSprites _sprites, GardenMap _map, Vector2Int _cell, float _size)
        {
            sprites = _sprites;
            character = _character;
            map = _map;
            cell = _cell;
            position = map.GetTopFacePosition(cell);

            image = GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = true;
            frameAnimation = gameObject.AddComponent<SpriteFrameAnimation>();

            // 足元を上面の中心に合わせる（Goal のパグと同じ）
            rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.08f);
            rect.sizeDelta = new Vector2(_size, _size);

            bubble = createBubble(_character.Name);
            bubble.SetActive(false);
            equippedMark = createEquippedMark();
            equippedMark.SetActive(false);
            PugAura.Attach(rect, _character.Tier, _size);

            // 起動直後に全員が同時に動かないようずらす
            sitTime = Random.Range(0f, MaxSitTime);
            setState(eState.Sit);
            apply(0f);
        }

        /// <summary>
        /// 名前の吹き出し
        /// </summary>
        /// <param name="_name"></param>
        private GameObject createBubble(string _name)
        {
            var go = new GameObject("bubble", typeof(RectTransform), typeof(Image), typeof(ContentSizeFitter), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(transform, false);
            var bubbleRect = (RectTransform)go.transform;
            bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0.5f, 0f);
            bubbleRect.pivot = new Vector2(0.5f, 0f);
            bubbleRect.anchoredPosition = new Vector2(0f, BubbleY);

            Image back = go.GetComponent<Image>();
            back.color = BubbleColor;
            back.raycastTarget = false;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset((int)BubblePadding.x, (int)BubblePadding.x, (int)BubblePadding.y, (int)BubblePadding.y);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            createText(go.transform, _name, BubbleFontSize, BubbleTextColor);

            // 名前の下に「つれていく」ボタン（吹き出しは横並びなので名前の右）
            var buttonGo = new GameObject("equip", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            buttonGo.transform.SetParent(go.transform, false);
            Image buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = ResourceManager.Instance.Load<Sprite>(EquipButtonPath);
            buttonImage.type = Image.Type.Sliced;
            var element = buttonGo.GetComponent<LayoutElement>();
            element.preferredWidth = EquipButtonSize.x;
            element.preferredHeight = EquipButtonSize.y;
            equipButton = buttonGo.AddComponent<MumuButton>();
            equipButton.targetGraphic = buttonImage;
            equipButton.SetOnClick(onClickEquipButton);
            equipText = createText(buttonGo.transform, "", BubbleFontSize * 0.85f, BubbleTextColor);
            var textRect = (RectTransform)equipText.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>
        /// 連れていく子の頭の上の★
        /// </summary>
        private GameObject createEquippedMark()
        {
            var go = new GameObject("equipped", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var markRect = (RectTransform)go.transform;
            markRect.anchorMin = markRect.anchorMax = new Vector2(0.5f, 0f);
            markRect.anchoredPosition = new Vector2(0f, EquippedMarkY);
            markRect.sizeDelta = new Vector2(EquippedMarkSize, EquippedMarkSize);
            Image image = go.GetComponent<Image>();
            image.sprite = ResourceManager.Instance.Load<Sprite>(EquippedMarkPath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return go;
        }

        /// <summary>
        /// 文字を作る
        /// </summary>
        /// <param name="_parent"></param>
        /// <param name="_text"></param>
        /// <param name="_size"></param>
        /// <param name="_color"></param>
        private static TextMeshProUGUI createText(Transform _parent, string _text, float _size, Color _color)
        {
            var go = new GameObject("text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = _text;
            text.fontSize = _size;
            text.color = _color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        /// <summary>
        /// 座る・歩く・喜ぶ・お昼寝
        /// </summary>
        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            stateTime += delta;
            float hop = 0f;

            switch (state)
            {
                case eState.Sit:
                    if (stateTime >= sitTime)
                    {
                        if (Random.value < SleepRate)
                        {
                            sleepTime = Random.Range(MinSleepTime, MaxSleepTime);
                            setState(eState.Sleep);
                        }
                        else
                        {
                            planWalk();
                        }
                    }
                    break;
                case eState.Walk:
                    walk(delta);
                    break;
                case eState.Happy:
                    hop = Mathf.Abs(Mathf.Sin(stateTime / HappyDuration * Mathf.PI * 2f)) * HopHeight;
                    if (stateTime >= HappyDuration)
                    {
                        hideBubbleTime = Time.unscaledTime + BubbleDuration - HappyDuration;
                        sitAgain();
                    }
                    break;
                case eState.Sleep:
                    if (stateTime >= nextZzzTime)
                    {
                        nextZzzTime = stateTime + ZzzInterval;
                        spawnZzz();
                    }
                    if (stateTime >= sleepTime)
                    {
                        sitAgain();
                    }
                    break;
            }
            if (bubble.activeSelf && state != eState.Happy && Time.unscaledTime >= hideBubbleTime)
            {
                bubble.SetActive(false);
            }
            apply(hop);
        }

        /// <summary>
        /// しばらく座る
        /// </summary>
        private void sitAgain()
        {
            sitTime = Random.Range(MinSitTime, MaxSitTime);
            setState(eState.Sit);
        }

        /// <summary>
        /// となりの草へ数マス歩く道を決める（行けなければ座ったまま）
        /// </summary>
        private void planWalk()
        {
            route.Clear();
            Vector2Int current = cell;
            Vector2Int previous = cell;
            int steps = Random.Range(1, MaxWalkSteps + 1);
            var candidates = new List<Vector2Int>(4);
            for (int i = 0; i < steps; i++)
            {
                candidates.Clear();
                foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    Vector2Int next = current + d;
                    if (map.IsWalkable(next) && next != previous)
                    {
                        candidates.Add(next);
                    }
                }
                if (candidates.Count == 0)
                {
                    break;
                }
                previous = current;
                current = candidates[Random.Range(0, candidates.Count)];
                route.Enqueue(current);
            }

            if (route.Count == 0)
            {
                sitAgain();
                return;
            }
            setState(eState.Walk);
        }

        /// <summary>
        /// 道に沿って歩く（着いたら座る）
        /// </summary>
        /// <param name="_delta"></param>
        private void walk(float _delta)
        {
            float step = WalkSpeed * _delta;
            while (step > 0f && route.Count > 0)
            {
                Vector2 target = map.GetTopFacePosition(route.Peek());
                Vector2 toTarget = target - position;
                float distance = toTarget.magnitude;
                if (Mathf.Abs(toTarget.x) > 0.01f)
                {
                    rect.localScale = new Vector3(toTarget.x < 0f ? -1f : 1f, 1f, 1f);
                }

                if (distance <= step)
                {
                    position = target;
                    cell = route.Dequeue();
                    step -= distance;
                }
                else
                {
                    position += toTarget / distance * step;
                    step = 0f;
                }
            }

            if (route.Count == 0)
            {
                sitAgain();
            }
        }

        /// <summary>
        /// 寝ている時の z を浮かべる
        /// </summary>
        private void spawnZzz()
        {
            TextMeshProUGUI text = createText(transform, "z", BubbleFontSize, ZzzColor);
            var zRect = (RectTransform)text.transform;
            zRect.anchorMin = zRect.anchorMax = new Vector2(0.5f, 0f);
            zRect.anchoredPosition = new Vector2(10f, BubbleY - 10f);
            zRect.sizeDelta = new Vector2(30f, 30f);
            zRect.localScale = new Vector3(rect.localScale.x, 1f, 1f);
            UiTween.MoveFade(zRect, text, new Vector2(10f, ZzzRise), ZzzDuration, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// 状態を変えてアニメーションを切り替え
        /// </summary>
        /// <param name="_state"></param>
        private void setState(eState _state)
        {
            state = _state;
            stateTime = 0f;
            nextZzzTime = 0f;
            switch (_state)
            {
                case eState.Walk:
                    play(sprites.Walk, WalkFps);
                    break;
                case eState.Happy:
                    rect.localScale = Vector3.one;
                    play(sprites.Happy, HappyFps);
                    break;
                case eState.Sleep:
                    // 目を閉じたコマで止める
                    playing = null;
                    frameAnimation.Play(new[] { sprites.Sit[Mathf.Min(SleepFrameIndex, sprites.Sit.Length - 1)] }, SitFps);
                    break;
                default:
                    rect.localScale = Vector3.one;
                    play(sprites.Sit, SitFps);
                    break;
            }
        }

        /// <summary>
        /// コマ送りを切り替え（同じなら続ける）
        /// </summary>
        /// <param name="_frames"></param>
        /// <param name="_fps"></param>
        private void play(Sprite[] _frames, float _fps)
        {
            if (playing == _frames)
            {
                return;
            }
            playing = _frames;
            frameAnimation.Play(_frames, _fps);
        }

        /// <summary>
        /// 位置を反映（吹き出しは左右反転させない）
        /// </summary>
        /// <param name="_hop"></param>
        private void apply(float _hop)
        {
            rect.anchoredPosition = position + new Vector2(0f, _hop);
            bubble.transform.localScale = new Vector3(rect.localScale.x, 1f, 1f);
        }
    }
}
